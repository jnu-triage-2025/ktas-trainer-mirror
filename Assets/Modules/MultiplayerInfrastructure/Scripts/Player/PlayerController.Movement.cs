using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  [RequireComponent(typeof(CharacterController))]
  public partial class PlayerController : NetworkBehaviour
  {
    // Movement Configuration
    [Header("PlayerObject Configuration")]
    [SerializeField] private float _walkingSpeed = 7.5f;
    [SerializeField] private float _jumpSpeed = 8.0f;
    [SerializeField] private float _gravity = 20.0f;
    [SerializeField] private float _spectatorMoveSpeed = 10.0f;
    [SerializeField] private float _spectatorVerticalSpeed = 10.0f;

    [Header("CameraHolder Configuration")]
    [SerializeField] private float _rotatingSpeed = 2.0f;
    [SerializeField] private float _cameraHolderPositionYOffset = 1.0f;

    [SerializeField] private float _minLookXAngle = -45.0f;
    [SerializeField] private float _maxLookXAngle = 45.0f;


    [Header("State Descriptions")]
    [SerializeField] private Vector3 _moveDirection = Vector3.zero;

    [SerializeField] private float _rotationX = 0;

    public bool canMove = true;

    // Awake 시점에 직렬화된 _walkingSpeed 값을 보관하여 /speed default 로 복원할 때 사용한다.
    private float _defaultWalkingSpeed;
    public float WalkingSpeed => _walkingSpeed;
    public float DefaultWalkingSpeed => _defaultWalkingSpeed;

    private const float DefaultRunningSpeedMultiplier = 1.5f;
    private static float _serverRunningSpeedMultiplier = DefaultRunningSpeedMultiplier;
    private readonly SyncVar<float> _runningSpeedMultiplier = new(DefaultRunningSpeedMultiplier);
    public float RunningSpeedMultiplier => _runningSpeedMultiplier.Value;
    public static float ServerRunningSpeedMultiplier => _serverRunningSpeedMultiplier;

    [SerializeField] private bool _isRunning = false;
    public bool IsRunning => _isRunning;
    [SerializeField] private bool _isCursorLocked = false;
    public bool IsCursorLocked => _isCursorLocked;

    [Header("References")]
    [SerializeField] private CharacterController _characterController;

    // 플레이어 측 카메라 부착점. 이전에는 "CameraHolder"라는 이름의 Transform이었으나,
    // 카메라 래퍼(CameraHolder)와 책임을 분리하기 위해 CameraAttachPoint 컴포넌트로 대체되었다.
    [SerializeField] private CameraAttachPoint _cameraAttachPoint;
    public CameraAttachPoint CameraAttachPoint => _cameraAttachPoint;

    private Transform _forcedFollowAnchor;
    private UnityEngine.Object _activeRidableControl;
    private readonly HashSet<UnityEngine.Object> _movementSuppressionOwners = new();
    private bool _jumpAnimationRequestedThisFrame;

    // 시나리오 등 스크립트가 플레이어 위치를 직접 제어하는 동안 true.
    // 이 동안 입력 기반 이동(ComputeMovementPlayerObject)은 억제되지만
    // CharacterController.velocity 기반 walk 애니메이션은 정상 동작한다.
    private bool _scriptedMovementActive;

    public bool IsMovementPositionOverridden => _forcedFollowAnchor != null;
    public bool IsRidableControlActive => _activeRidableControl != null;
    public bool IsMovementSuppressed => _movementSuppressionOwners.Count > 0;
    private bool CanProcessMovementInput => canMove && !IsMovementSuppressed;

    public Vector3 CurrentMoveInputVector
      => new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

    private void Awake_Movement()
    {
      _characterController = GetComponent<CharacterController>();
      _defaultWalkingSpeed = _walkingSpeed;
      InitializeCameraAttachPoint();
      LockCursor();
    }

    /// <summary>
    /// 서버에서 걷기 속도를 변경하고 모든 클라이언트에 동기화한다.
    /// </summary>
    internal void ApplyWalkingSpeedServer(float value)
    {
      if (!IsServerInitialized)
        return;

      _walkingSpeed = value;
      RpcApplyWalkingSpeed(value);
      ApplyWalkingSpeedLocal(value);
    }

    /// <summary>
    /// 서버 전체의 달리기 배율을 변경하고, 현재 및 이후 플레이어에게 동기화한다.
    /// </summary>
    public static void ApplyRunningSpeedMultiplierServer(float value)
    {
      _serverRunningSpeedMultiplier = value;

      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      foreach (var player in players)
      {
        if (player != null && player.IsServerInitialized)
          player._runningSpeedMultiplier.Value = value;
      }
    }

    private void InitializeRunningSpeedMultiplierServer()
    {
      if (IsServerInitialized)
        _runningSpeedMultiplier.Value = _serverRunningSpeedMultiplier;
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcApplyWalkingSpeed(float value)
    {
      ApplyWalkingSpeedLocal(value);
    }

    private void ApplyWalkingSpeedLocal(float value)
    {
      _walkingSpeed = value;
    }

    private void Update_Movement()
    {
      _jumpAnimationRequestedThisFrame = false;
      ComputeMovement();
      FollowForcedAnchor();
      UpdateSpectateFollowTarget();
    }

    private Vector3 _forwardSpeed;
    private Vector3 _rightSpeed;

    private void InitializeCameraAttachPoint()
    {
      // 발밑(y=0)이 아니라 눈높이 정도로 올려, 카메라 충돌 검사가 플레이어 콜라이더 내부에서
      // 시작되어 카메라가 강제로 1인칭으로 당겨지는 현상을 방지한다.
      _cameraAttachPoint = CameraAttachPoint.Create(transform, _cameraHolderPositionYOffset);
    }

    private void ComputeMovement()
    {
      if (IsSpectator)
        ComputeSpectatorMovement();
      else
        ComputeMovementPlayerObject();

      ComputeMovementCameraHolder();
    }

    private void ComputeMovementPlayerObject()
    {
      if (_forcedFollowAnchor != null)
      {
        _moveDirection = Vector3.zero;
        return;
      }

      // 스크립트 이동이 활성화된 동안에는 입력 기반 이동을 억제한다.
      // 실제 이동은 ApplyScriptedMove(...)를 통해 외부에서 구동된다.
      if (_scriptedMovementActive)
      {
        _moveDirection = Vector3.zero;
        return;
      }

      KeyCode runningKey = KeyBindingRepository.GetBoundKey("run", _keyMovingRunning);
      _isRunning = Input.GetKey(runningKey);

      _forwardSpeed = transform.TransformDirection(Vector3.forward);
      _rightSpeed = transform.TransformDirection(Vector3.right);

      float movementSpeed = _isRunning ? _walkingSpeed * _runningSpeedMultiplier.Value : _walkingSpeed;
      float curSpeedX = CanProcessMovementInput ? movementSpeed * Input.GetAxis("Vertical") : 0;
      float curSpeedY = CanProcessMovementInput ? movementSpeed * Input.GetAxis("Horizontal") : 0;
      float movementDirectionY = _moveDirection.y;
      _moveDirection = (_forwardSpeed * curSpeedX) + (_rightSpeed * curSpeedY);

      if (IsJumpInputHeld() && CanProcessMovementInput && _characterController.isGrounded)
      {
        _moveDirection.y = _jumpSpeed;
        if (IsJumpInputPressedThisFrame())
          _jumpAnimationRequestedThisFrame = true;
      }
      else if (!_characterController.isGrounded)
      {
        // Apply gravity and retain vertical velocity from jump/fall
        _moveDirection.y = movementDirectionY - _gravity * Time.deltaTime;
      }
      else
      {
        // Grounded but not jumping: clamp vertical velocity to zero to prevent
        // tiny negative values that can cause isGrounded to flicker.
        _moveDirection.y = 0f;
      }

      _characterController.Move(_moveDirection * Time.deltaTime);
    }

    private void ComputeSpectatorMovement()
    {
      if (!CanProcessMovementInput)
        return;
      if (_isSpectateFollowing)
        return;

      _forwardSpeed = transform.TransformDirection(Vector3.forward);
      _rightSpeed = transform.TransformDirection(Vector3.right);

      float curSpeedX = _spectatorMoveSpeed * Input.GetAxis("Vertical");
      float curSpeedY = _spectatorMoveSpeed * Input.GetAxis("Horizontal");

      float vertical = 0f;
      if (IsSpectatorAscendInputHeld())
        vertical += 1f;
      if (Input.GetKey(_keySpectatorFlyDown))
        vertical -= 1f;

      Vector3 velocity = (_forwardSpeed * curSpeedX) + (_rightSpeed * curSpeedY) + (Vector3.up * (_spectatorVerticalSpeed * vertical));
      _characterController.Move(velocity * Time.deltaTime);
    }

    private void ComputeMovementCameraHolder()
    {
      if (_cameraAttachPoint.IsUnityNull())
        return;
      if (!CanProcessMovementInput)
        return;
      if (_isSpectateFollowing)
        return;

      _rotationX += -Input.GetAxis("Mouse Y") * _rotatingSpeed;
      _rotationX = Mathf.Clamp(_rotationX, _minLookXAngle, _maxLookXAngle);
      _cameraAttachPoint.SetPitch(_rotationX);
      transform.Rotate(0, Input.GetAxis("Mouse X") * _rotatingSpeed, 0);
    }

    private void FollowForcedAnchor()
    {
      if (_forcedFollowAnchor == null)
        return;

      transform.position = _forcedFollowAnchor.position;
    }

    public void SetForcedFollowAnchor(Transform anchor)
    {
      _forcedFollowAnchor = anchor;
      if (_forcedFollowAnchor == null)
        return;

      _moveDirection = Vector3.zero;
      transform.position = _forcedFollowAnchor.position;
    }

    public void ClearForcedFollowAnchor(Transform anchor = null)
    {
      if (anchor != null && _forcedFollowAnchor != anchor)
        return;

      _forcedFollowAnchor = null;
      _moveDirection = Vector3.zero;
    }

    public void SetMovementSuppressed(UnityEngine.Object owner, bool suppressed)
    {
      if (owner == null)
        return;

      if (suppressed)
        _movementSuppressionOwners.Add(owner);
      else
        _movementSuppressionOwners.Remove(owner);

      _moveDirection = Vector3.zero;
    }

    internal void SetRidableControlActive(UnityEngine.Object control)
    {
      _activeRidableControl = control;
    }

    internal void ClearRidableControlActive(UnityEngine.Object control)
    {
      if (control != null && _activeRidableControl != control)
        return;

      _activeRidableControl = null;
    }

    public void AlignYawTo(Vector3 worldForward)
    {
      worldForward.y = 0f;
      if (worldForward.sqrMagnitude <= 0.0001f)
        return;

      transform.rotation = Quaternion.LookRotation(worldForward.normalized, Vector3.up);
    }

    /// <summary>
    /// 스크립트(시나리오 등)에 의한 이동 제어를 시작한다.
    /// 이 동안 입력 기반 이동은 억제되며, 실제 이동은 <see cref="ApplyScriptedMove"/> 로 구동한다.
    /// </summary>
    public void BeginScriptedMovement()
    {
      _scriptedMovementActive = true;
      _moveDirection = Vector3.zero;
    }

    /// <summary>
    /// 스크립트 이동 제어를 종료하고 입력 기반 이동으로 복귀한다.
    /// </summary>
    public void EndScriptedMovement()
    {
      _scriptedMovementActive = false;
      _moveDirection = Vector3.zero;
    }

    /// <summary>
    /// 스크립트 이동 1프레임 분의 변위를 적용한다.
    /// <paramref name="horizontalDelta"/> 는 이번 프레임에 이동할 수평 변위(월드 기준, y 무시).
    /// <paramref name="applyGravity"/> 가 true면 접지 전까지 중력을 누적 적용한다.
    /// CharacterController.Move 를 사용하므로 velocity 기반 walk 애니메이션이 자동으로 재생된다.
    /// </summary>
    public void ApplyScriptedMove(Vector3 horizontalDelta, bool applyGravity)
    {
      if (_characterController == null)
        return;

      horizontalDelta.y = 0f;

      Vector3 motion = horizontalDelta;

      if (applyGravity)
      {
        if (_characterController.isGrounded && _moveDirection.y < 0f)
          _moveDirection.y = 0f;

        _moveDirection.y -= _gravity * Time.deltaTime;
        motion.y = _moveDirection.y * Time.deltaTime;
      }
      else
      {
        _moveDirection.y = 0f;
      }

      _characterController.Move(motion);
    }

    private void UpdateSpectateFollowTarget()
    {
      if (!_isSpectateFollowing)
        return;

      if (_spectateFollowTarget == null)
        StopSpectateFollow();
    }

    public void EnterUIOverlayMode()
    {
      UnlockCursor();
      canMove = false;
    }

    public void ExitUIOverlayMode()
    {
      LockCursor();
      canMove = true;
    }

    public void LockCursor() { ChangeCursorLock(true); }
    public void UnlockCursor() { ChangeCursorLock(false); }

    private void ChangeCursorLock(bool locking)
    {
      Cursor.lockState = locking ? CursorLockMode.Locked : CursorLockMode.None;
      Cursor.visible = !locking;
      _isCursorLocked = locking;
    }
  }
}
