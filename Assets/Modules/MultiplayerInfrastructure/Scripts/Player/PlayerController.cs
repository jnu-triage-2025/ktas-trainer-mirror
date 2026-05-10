using FishNet.Object;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  [RequireComponent(typeof(InteractableEntityResolver))]
  public partial class PlayerController : NetworkBehaviour
  {
    [SerializeField] private Entity.Entity _playerEntity;
    public Entity.Entity PlayerEntity => _playerEntity;

    NearbyInteractablesDetector _interactiveDetector;
    public NearbyInteractablesDetector InteractiveDetector => _interactiveDetector;
    InteractableEntityResolver _interactionResolver;
    public InteractableEntityResolver InteractionResolver => _interactionResolver;
    
    void Awake()
    {
      Awake_GameObject();
      Awake_PlayerModel();
      Awake_Movement();
      Awake_Camera();
      Awake_Raycast();
      Awake_Visibility();

      if (_playerEntity == null)
        _playerEntity = new Entity.Entity();
      
      _interactionResolver = GetComponent<InteractableEntityResolver>();
    }

    void Start()
    {
      Start_Input();
      Start_Inventory();
      Start_Hotbar();
      Start_Item();
    }

    void Update()
    {
      if (!IsOwner) return;
      Update_Input();
      Update_Movement();
      Update_Raycast();
      Update_Inventory();
      Update_Item();
    }

    void LateUpdate()
    {
      LateUpdate_Camera();  
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      OnStartClient_AnyPeer();   // 모든 클라이언트 — owner 여부 무관
      if (!IsOwner) return;
      
      OnStartClient_Network();
      OnStartClient_Camera();
      OnStartClient_Crosshair();
      OnStartClient_Interactables();
      OnStartClient_Dialogue();
      OnStartClient_Quest();
      OnClientStart_EscapeMenu();
    }

    public override void OnStopClient()
    {
      OnStopClient_AnyPeer();    // 모든 클라이언트 — owner 여부 무관
      base.OnStopClient();
    }
  }
}
