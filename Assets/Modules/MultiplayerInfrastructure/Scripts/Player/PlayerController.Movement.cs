using FishNet.Object;
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
    [SerializeField] private float _runningSpeed = 11.5f;
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
    [SerializeField] private bool _isRunning = false;
    public bool IsRunning => _isRunning;
    [SerializeField] private bool _isCursorLocked = false;
    public bool IsCursorLocked => _isCursorLocked;

    [Header("References")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraHolderTransform;
    public Transform CameraHolderTransform => _cameraHolderTransform;

    private Transform _forcedFollowAnchor;
    private bool _jumpAnimationRequestedThisFrame;

    // 시나리오 등 스크립트가 플레이어 위치를 직접 제어하는 동안 true.
    // 이 동안 입력 기반 이동(ComputeMovementPlayerObject)은 억제되지만
    // CharacterController.velocity 기반 walk 애니메이션은 정상 동작한다.
    private bool _scriptedMovementActive;

    public bool IsMovementPositionOverridden => _forcedFollowAnchor != null;

    public Vector3 CurrentMoveInputVector
      => new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

    void Awake_Movement()
    {
      _characterController = GetComponent<CharacterController>();
      InitializeCameraHolder();
      LockCursor();
    }

    void Update_Movement()
    {
      _jumpAnimationRequestedThisFrame = false;
      ComputeMovement();
      FollowForcedAnchor();
      UpdateSpectateFollowTarget();
    }

    Vector3 _forwardSpeed;
    Vector3 _rightSpeed;

    void InitializeCameraHolder()
    {
      GameObject go = new GameObject("CameraHolder");
      go.transform.SetParent(transform);
      go.transform.localPosition = Vector3.zero;
      go.transform.localRotation = Quaternion.identity;
      _cameraHolderTransform = go.transform;
    }

    void ComputeMovement()
    {
      if (IsSpectator)
        ComputeSpectatorMovement();
      else
        ComputeMovementPlayerObject();

      ComputeMovementCameraHolder();
    }
    
    void ComputeMovementPlayerObject()
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

      _isRunning = Input.GetKey(_keyMovingRunning);

      _forwardSpeed = transform.TransformDirection(Vector3.forward);
      _rightSpeed = transform.TransformDirection(Vector3.right);
      
      float curSpeedX = canMove ? (_isRunning ? _runningSpeed : _walkingSpeed) * Input.GetAxis("Vertical") : 0;
      float curSpeedY = canMove ? (_isRunning ? _runningSpeed : _walkingSpeed) * Input.GetAxis("Horizontal") : 0;
      float movementDirectionY = _moveDirection.y;
      _moveDirection = (_forwardSpeed * curSpeedX) + (_rightSpeed * curSpeedY);

if (Input.GetButton("Jump") && canMove && _characterController.isGrounded)
       {
         _moveDirection.y = _jumpSpeed;
         if (Input.GetButtonDown("Jump"))
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

    void ComputeSpectatorMovement()
    {
      if (!canMove) return;
      if (_isSpectateFollowing) return;

      _forwardSpeed = transform.TransformDirection(Vector3.forward);
      _rightSpeed = transform.TransformDirection(Vector3.right);

      float curSpeedX = _spectatorMoveSpeed * Input.GetAxis("Vertical");
      float curSpeedY = _spectatorMoveSpeed * Input.GetAxis("Horizontal");

      float vertical = 0f;
      if (Input.GetKey(KeyCode.Space)) vertical += 1f;
      if (Input.GetKey(_keySpectatorFlyDown)) vertical -= 1f;

      Vector3 velocity = (_forwardSpeed * curSpeedX) + (_rightSpeed * curSpeedY) + (Vector3.up * (_spectatorVerticalSpeed * vertical));
      _characterController.Move(velocity * Time.deltaTime);
    }

    void ComputeMovementCameraHolder()
    {
      if (_cameraHolderTransform.IsUnityNull()) return;
      if (!canMove) return;
      if (_isSpectateFollowing) return;
      
      _rotationX += -Input.GetAxis("Mouse Y") * _rotatingSpeed;
      _rotationX = Mathf.Clamp(_rotationX, _minLookXAngle, _maxLookXAngle);
      _cameraHolderTransform.localRotation = Quaternion.Euler(_rotationX, 0, 0);
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

    void UpdateSpectateFollowTarget()
    {
      if (!_isSpectateFollowing) return;

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

    void ChangeCursorLock(bool locking)
    {
      Cursor.lockState = locking ? CursorLockMode.Locked : CursorLockMode.None;
      Cursor.visible = !locking;
      _isCursorLocked = locking;
    }
  }
}
