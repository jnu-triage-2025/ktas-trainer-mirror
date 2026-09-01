using FishNet;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.OxyLine
{
  /// <summary>
  /// 설정된 쌍의 어느 한쪽 종단점에서 산소 라인 연결 동작을 제공한다.
  /// 두 인스턴스는 서로를 참조해야 한다. 필수 환자 표시 오브젝트가 활성 상태이고
  /// 연결된 벽면 플로미터가 부착되기 전까지는 이 동작을 사용할 수 없다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class OxyLinePairInteractable : MonoBehaviour, IInteractable, IInteract,
    IInteractorConditional, IQuestPresentationTarget
  {
    [Header("Oxygen line pair")]
    [SerializeField] private OxyLineConnectionPoint _localEndpoint;
    [SerializeField] private OxyLinePairInteractable _counterpart;
    [SerializeField] private WallAttachedOxyflowmeter _oxyflowmeter;
    [SerializeField] private GameObject _requiredActiveDisplay;
    [SerializeField] private string _displayText = "T피스에 산소 연결";

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public string PresentationEntityIdentifier =>
      GetComponentInParent<PatientController>()?.Identifier
      ?? _oxyflowmeter?.EntityIdentifier
      ?? string.Empty;
    public string InteractionIdentifier => "connect_oxygen_line";
    public Sprite DisplayIcon => null;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public bool CanInteract(Transform interactor)
    {
      return interactor != null
             && interactor.GetComponentInParent<PlayerController>() is { } player
             && TryResolveEndpoints(out _, out _);
    }

    public void Interact(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null || !TryResolveEndpoints(out var local, out var remote))
        return;

      if (player.CountItemInInventory(TriageTrainer.ItemDefinitions.O2Line.Identifier) < 1)
      {
        ShowMissingOxygenLineDialogue();
        return;
      }

      if (player.RemoveItemFromInventory(TriageTrainer.ItemDefinitions.O2Line.Identifier, 1) != 1)
        return;

      // 기획 참고(대화 기록): "T-piece가 활성화되어있고, oxyflowmeter가 is attached되어있다면
      // oxyflowmeter 혹은 t-piece 둘 중 한 쪽이라도 interactable detector에 부딪히면
      // 'T피스에 산소 연결' 인터렉션 등록. 이 인터렉션을 실행하면 T피스와 oxyflowmeter
      // 사이에 oxy line이 연결됨". B/C에는 T-piece 대신 비강 캐뉼라를 적용한다.
      if (InstanceFinder.IsOffline)
      {
        var service = LineConnectionService.TopologyService
                      ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
        service?.TryCreateAutomaticConnection(local, remote);
      }
      else
      {
        var service = LineConnectionService.TopologyService
                      ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
        if (player.IsServerStarted)
          service?.TryCreateAutomaticConnection(local, remote);
        else
          ScenarioNetworkRelay.RequestLineTopologyChange(
            local.ConnectionIdentifier, remote.ConnectionIdentifier, connected: true);
      }

      player.RefreshInteractableHintsNow();
    }

    private static void ShowMissingOxygenLineDialogue()
    {
      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<MultiplayerInfrastructure.UI.DialoguePanelUIController>(
        MultiplayerInfrastructure.Registry.RegistryType.UI,
        MultiplayerInfrastructure.Registry.Registry.TypeKey<MultiplayerInfrastructure.UI.DialoguePanelUIController>());
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(산소줄을 갖고 있지 않다.)");
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(산소줄을 찾자.)");
    }

    private bool TryResolveEndpoints(out OxyLineConnectionPoint local, out OxyLineConnectionPoint remote)
    {
      local = _localEndpoint != null ? _localEndpoint : GetComponent<OxyLineConnectionPoint>();
      remote = _counterpart != null
        ? (_counterpart._localEndpoint != null
          ? _counterpart._localEndpoint
          : _counterpart.GetComponent<OxyLineConnectionPoint>())
        : null;

      var patient = GetComponentInParent<PatientController>();
      var resolvedFlowmeter = _oxyflowmeter != null
        ? _oxyflowmeter
        : patient?.ConnectedOxyflowmeter;
      if (remote == null)
        remote = resolvedFlowmeter?.OxyLineConnectionPoint;

      return local != null
             && remote != null
             && local.isActiveAndEnabled
             && remote.isActiveAndEnabled
             && (_counterpart == null || _counterpart.isActiveAndEnabled)
             && (_requiredActiveDisplay == null || _requiredActiveDisplay.activeInHierarchy)
             && (_counterpart == null
                 || _counterpart._requiredActiveDisplay == null
                 || _counterpart._requiredActiveDisplay.activeInHierarchy)
             && IsAttachedFlowmeter(resolvedFlowmeter)
             && (_counterpart == null || IsAttachedFlowmeter(_counterpart._oxyflowmeter))
             && !local.IsPhysicallyConnectedTo(remote);
    }

    private static bool IsAttachedFlowmeter(WallAttachedOxyflowmeter flowmeter) =>
      flowmeter != null && flowmeter.IsAttached;
  }
}
