using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using Modules.TriageTrainer.Scripts.Camera;
using Modules.TriageTrainer.Scripts.Connection;
using Modules.TriageTrainer.Scripts.PlayerInteractiveGameObject;
using UnityEngine;

[RequireComponent(typeof(PlayerInteractionResolver))]
[RequireComponent(typeof(PlayerInteractiveDetector))]
public class PlayerController : NetworkBehaviour
{
  PlayerInteractiveDetector _interactiveDetector;
  public PlayerInteractiveDetector InteractiveDetector => _interactiveDetector;
  PlayerInteractionResolver _interactionResolver;
  public PlayerInteractionResolver InteractionResolver => _interactionResolver;

  //
  public float walkingSpeed = 7.5f;
  public float runningSpeed = 11.5f;
  public float jumpSpeed = 8.0f;
  public float gravity = 20.0f;
  public float lookSpeed = 2.0f;
  public float lookXLimit = 45.0f;

  CharacterController characterController;
  Vector3 moveDirection = Vector3.zero;
  float rotationX = 0;

  [HideInInspector]
  public bool canMove = true;
  bool isRunning = false;
  bool cameraLocked = false;

  [SerializeField]
  private float cameraYOffset = 0.4f;
  private Camera playerCamera;

  [Header("Animator")]
  public Animator anim;

  void Awake()
  {
    characterController = GetComponent<CharacterController>();
    _interactiveDetector = GetComponent<PlayerInteractiveDetector>();
    _interactionResolver = GetComponent<PlayerInteractionResolver>();
    LockPlayerCamera(true);
  }

  public void LockPlayerCamera(bool _lock)
  {
    if (_lock)
    {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
      cameraLocked = true;
    }
    else
    {
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
      cameraLocked = false;
    }
  }

  // Update is called once per frame
  void Update()
  {
    isRunning = Input.GetKey(KeyCode.LeftShift);

    Vector3 forward = transform.TransformDirection(Vector3.forward);
    Vector3 right = transform.TransformDirection(Vector3.right);

    float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
    float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
    float movementDirectionY = moveDirection.y;
    moveDirection = (forward * curSpeedX) + (right * curSpeedY);
    
    if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
    {
      moveDirection.y = jumpSpeed;
    }
    else
    {
      moveDirection.y = movementDirectionY;
    }

    if (!characterController.isGrounded)
    {
      moveDirection.y -= gravity * Time.deltaTime;
    }

    characterController.Move(moveDirection * Time.deltaTime);

    if (canMove && playerCamera != null)
    {
      rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
      rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
      playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
      transform.Rotate(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }
  }

  public override void OnStartClient()
  {
    base.OnStartClient();

    if (IsOwner)
    {
      playerCamera = Camera.main;
      playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z);
      playerCamera.transform.SetParent(transform);
    }
    else
    {
      gameObject.GetComponent<PlayerController>().enabled = false;
    }

    CurrentSessionPlayInfoRegistry.Instance.RegisterPlayerController(this);
    CurrentSessionPlayInfoRegistry.Instance.RegisterLocalCameraHolder(this.transform);
  }
}
