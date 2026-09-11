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

      // 각 단계를 격리해 끝까지 진행한다. 앞 단계(캐릭터 모델 적용, 이름표 등)에서 예외가 새어 나오면
      // 뒤따르는 소유자 전용 바인딩이 통째로 건너뛰어져, UI 오버레이가 열려도 커서 잠금이 풀리지 않고
      // 입력 컨트롤러 참조도 비어 있는 채로 플레이가 시작된다(UI가 보이는데 아무것도 클릭되지 않는 상태).
      RunClientLifecycleStep(OnStartClient_AnyPeer, nameof(OnStartClient_AnyPeer));   // 모든 클라이언트 — owner 여부 무관
      RunClientLifecycleStep(() => MppmLiteMode.StripVisuals(gameObject), nameof(MppmLiteMode.StripVisuals));
      if (!IsOwner)
        return;

      RunClientLifecycleStep(OnStartClient_UIOverlaySync, nameof(OnStartClient_UIOverlaySync));

      RunClientLifecycleStep(OnStartClient_Network, nameof(OnStartClient_Network));
      RunClientLifecycleStep(OnStartClient_Camera, nameof(OnStartClient_Camera));
      RunClientLifecycleStep(OnStartClient_Crosshair, nameof(OnStartClient_Crosshair));
      RunClientLifecycleStep(OnStartClient_Interactables, nameof(OnStartClient_Interactables));
      RunClientLifecycleStep(OnStartClient_Dialogue, nameof(OnStartClient_Dialogue));
      RunClientLifecycleStep(OnStartClient_Quest, nameof(OnStartClient_Quest));
      RunClientLifecycleStep(OnClientStart_EscapeMenu, nameof(OnClientStart_EscapeMenu));

      // UI 바인딩(핫바/아이템/입력)은 로컬 소유자 전용이다.
      // 원격 플레이어 인스턴스가 로컬 핫바 UI를 자신의 슬롯으로 재바인딩하면
      // 중복 구독과 잘못된 인벤토리 표시가 발생한다.
      // (FishNet 은 Start 안에서 IsOwner 사용을 금지하므로 여기서 호출한다.)
      RunClientLifecycleStep(Start_Inventory, nameof(Start_Inventory)); // 슬롯이 아직 없으면 먼저 초기화(멱등).
      RunClientLifecycleStep(Start_Input, nameof(Start_Input));
      RunClientLifecycleStep(Start_Hotbar, nameof(Start_Hotbar));
      RunClientLifecycleStep(Start_Item, nameof(Start_Item));
    }

    public override void OnStopClient()
    {
      // 종료도 같은 이유로 격리한다. 오버레이 동기화 해제가 실패하면 정적 스택 구독이 남아
      // 파괴된 컨트롤러가 다음 세션의 커서/이동 상태를 계속 건드린다.
      RunClientLifecycleStep(OnStopClient_UIOverlaySync, nameof(OnStopClient_UIOverlaySync));
      RunClientLifecycleStep(OnStopClient_Dialogue, nameof(OnStopClient_Dialogue));
      RunClientLifecycleStep(OnStopClient_AnyPeer, nameof(OnStopClient_AnyPeer));    // 모든 클라이언트 — owner 여부 무관
      base.OnStopClient();
    }

    private void RunClientLifecycleStep(System.Action step, string stepName)
    {
      try
      {
        step();
      }
      catch (System.Exception exception)
      {
        Debug.LogError(
          $"[PlayerController] Client lifecycle step '{stepName}' threw; continuing with the remaining steps so UI/input binding is not skipped.",
          this);
        Debug.LogException(exception, this);
      }
    }
  }
}
