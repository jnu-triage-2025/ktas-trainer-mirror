using FishNet;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using TriageTrainer.Entity.LineConnection;
using UnityEngine;

namespace TriageTrainer.Entity.OxyLine
{
  /// <summary>
  /// Exposes one oxygen-line connection action from either endpoint of a configured pair.
  /// Both instances must reference each other. The action is unavailable until the required
  /// patient display object is active and the associated wall flowmeter is attached.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class OxyLinePairInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional
  {
    [Header("Oxygen line pair")]
    [SerializeField] private OxyLineConnectionPoint _localEndpoint;
    [SerializeField] private OxyLinePairInteractable _counterpart;
    [SerializeField] private WallAttachedOxyflowmeter _oxyflowmeter;
    [SerializeField] private GameObject _requiredActiveDisplay;
    [SerializeField] private string _displayText = "T피스에 산소 연결";

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => null;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public bool CanInteract(Transform interactor)
    {
      return interactor != null
             && interactor.GetComponentInParent<PlayerController>() != null
             && TryResolveEndpoints(out _, out _);
    }

    public void Interact(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null || !TryResolveEndpoints(out var local, out var remote))
        return;

      // 기획 참고(대화 기록): "T-piece가 활성화되어있고, oxyflowmeter가 is attached되어있다면
      // oxyflowmeter 혹은 t-piece 둘 중 한 쪽이라도 interactable detector에 부딪히면
      // 'T피스에 산소 연결' 인터렉션 등록. 이 인터렉션을 실행하면 T피스와 oxyflowmeter
      // 사이에 oxy line이 연결됨". B/C에는 T-piece 대신 비강 캐뉼라를 적용한다.
      if (InstanceFinder.IsOffline)
      {
        var service = FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
        service?.TryCreateAutomaticConnection(local, remote);
      }
      else
      {
        // The endpoint RPC validates distance, ownership and capacity on the server.
        remote.RequestAuthoritativeConnection(local);
      }

      player.RefreshInteractableHintsNow();
    }

    private bool TryResolveEndpoints(out OxyLineConnectionPoint local, out OxyLineConnectionPoint remote)
    {
      local = _localEndpoint != null ? _localEndpoint : GetComponent<OxyLineConnectionPoint>();
      remote = _counterpart != null
        ? (_counterpart._localEndpoint != null
          ? _counterpart._localEndpoint
          : _counterpart.GetComponent<OxyLineConnectionPoint>())
        : null;

      return local != null
             && remote != null
             && local.isActiveAndEnabled
             && remote.isActiveAndEnabled
             && _counterpart != null
             && _counterpart.isActiveAndEnabled
             && (_requiredActiveDisplay == null || _requiredActiveDisplay.activeInHierarchy)
             && (_counterpart._requiredActiveDisplay == null || _counterpart._requiredActiveDisplay.activeInHierarchy)
             && IsAttachedFlowmeter(_oxyflowmeter)
             && IsAttachedFlowmeter(_counterpart._oxyflowmeter)
             && !local.IsPhysicallyConnectedTo(remote);
    }

    private static bool IsAttachedFlowmeter(WallAttachedOxyflowmeter flowmeter) =>
      flowmeter != null && flowmeter.IsAttached;
  }
}
