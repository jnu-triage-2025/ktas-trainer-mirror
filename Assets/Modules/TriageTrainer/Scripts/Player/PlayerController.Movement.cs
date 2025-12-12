using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

namespace TriageTrainer.Scripts.Player
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

    [Header("CameraHolder Configuration")]
    [SerializeField] private float _rotatingSpeed = 2.0f;
    [SerializeField] private float _cameraHolderPositionYOffset = 1.0f;

    [SerializeField] private float _minLookXAngle = -45.0f;
    [SerializeField] private float _maxLookXAngle = 45.0f;
    
    [Header("KeyConfiguration")]
    [SerializeField] private KeyCode _runningKey = KeyCode.LeftControl;
    
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

    void Awake_Movement()
    {
      _characterController = GetComponent<CharacterController>();
      InitializeCameraHolder();
      LockCursor();
    }

    void Update_Movement()
    {
      ComputeMovement();
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
      ComputeMovementPlayerObject();
      ComputeMovementCameraHolder();
    }
    
    void ComputeMovementPlayerObject()
    {
      _isRunning = Input.GetKey(_runningKey);

      _forwardSpeed = transform.TransformDirection(Vector3.forward);
      _rightSpeed = transform.TransformDirection(Vector3.right);
      
      float curSpeedX = canMove ? (_isRunning ? _runningSpeed : _walkingSpeed) * Input.GetAxis("Vertical") : 0;
      float curSpeedY = canMove ? (_isRunning ? _runningSpeed : _walkingSpeed) * Input.GetAxis("Horizontal") : 0;
      float movementDirectionY = _moveDirection.y;
      _moveDirection = (_forwardSpeed * curSpeedX) + (_rightSpeed * curSpeedY);

      if (Input.GetButton("Jump") && canMove && _characterController.isGrounded)
      {
        _moveDirection.y = _jumpSpeed;
      }
      else
      {
        _moveDirection.y = movementDirectionY;
      }

      if (!_characterController.isGrounded)
      {
        _moveDirection.y -= _gravity * Time.deltaTime;
      }
      
      _characterController.Move(_moveDirection * Time.deltaTime);
    }

    void ComputeMovementCameraHolder()
    {
      if (_cameraHolderTransform.IsUnityNull()) return;
      if (!canMove) return;
      
      _rotationX += -Input.GetAxis("Mouse Y") * _rotatingSpeed;
      _rotationX = Mathf.Clamp(_rotationX, _minLookXAngle, _maxLookXAngle);
      _cameraHolderTransform.localRotation = Quaternion.Euler(_rotationX, 0, 0);
      transform.Rotate(0, Input.GetAxis("Mouse X") * _rotatingSpeed, 0);
    }

    public void EnterUIOverlayMode()
    {
      LockCursor();
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
