using FishNet.Object;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Performance;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  [RequireComponent(typeof(InteractableEntityResolver))]
  public partial class PlayerController : NetworkBehaviour, IScenarioIdentifiedEntity
  {
    [SerializeField] private Entity.Entity _playerEntity;
    public Entity.Entity PlayerEntity => _playerEntity;
    public string ScenarioEntityIdentifier => UserIdentifier;

    private NearbyInteractablesDetector _interactiveDetector;
    public NearbyInteractablesDetector InteractiveDetector => _interactiveDetector;
    private InteractableEntityResolver _interactionResolver;
    public InteractableEntityResolver InteractionResolver => _interactionResolver;

    private void Awake()
    {
      Awake_GameObject();
      Awake_PlayerModel();
      Awake_ReposableCarry();
      Awake_Animation();
      Awake_Movement();
      Awake_Camera();
      Awake_Raycast();
      Awake_Visibility();

      if (_playerEntity == null)
        _playerEntity = new Entity.Entity();

      _interactionResolver = GetComponent<InteractableEntityResolver>();
    }

    private void Start()
    {
      // 인벤토리 슬롯 초기화는 소유 여부와 무관하게 필요하다(서버/원격에서도 슬롯 데이터 유지).
      Start_Inventory();
    }



    private void Update()
    {
      if (IsServerStarted)
        UpdateServerWorldItemTransforms();

      if (!IsOwner)
        return;
      Update_Input();
      Update_Movement();
      Update_ReposableCarry();
      Update_Animation();
      Update_Raycast();
      Update_Inventory();
      Update_Item();
      Update_PlaceableItemPreview();
    }

    private void LateUpdate()
    {
      LateUpdate_Camera();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      OnStartClient_AnyPeer();   // 모든 클라이언트 — owner 여부 무관
      MppmLiteMode.StripVisuals(gameObject);
      if (!IsOwner)
        return;

      OnStartClient_UIOverlaySync();

      OnStartClient_Network();
      OnStartClient_Camera();
      OnStartClient_Crosshair();
      OnStartClient_Interactables();
      OnStartClient_Dialogue();
      OnStartClient_Quest();
      OnClientStart_EscapeMenu();

      // UI 바인딩(핫바/아이템/입력)은 로컬 소유자 전용이다.
      // 원격 플레이어 인스턴스가 로컬 핫바 UI를 자신의 슬롯으로 재바인딩하면
      // 중복 구독과 잘못된 인벤토리 표시가 발생한다.
      // (FishNet 은 Start 안에서 IsOwner 사용을 금지하므로 여기서 호출한다.)
      Start_Inventory(); // 슬롯이 아직 없으면 먼저 초기화(멱등).
      Start_Input();
      Start_Hotbar();
      Start_Item();
    }

    public override void OnStopClient()
    {
      OnStopClient_UIOverlaySync();
      OnStopClient_AnyPeer();    // 모든 클라이언트 — owner 여부 무관
      base.OnStopClient();
    }
  }
}
