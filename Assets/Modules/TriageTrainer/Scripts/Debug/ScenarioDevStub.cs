using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.DebugTools
{
  /// <summary>
  /// IndevScene 등 개발 씬에서, 시나리오가 요구하는 인터랙션 대상/트리거존을 간이로 대체하는
  /// 디버그 스텁. <see cref="ScenarioDevStubSpawner"/> 가 자동 생성한다.
  ///
  /// <para>두 가지 모드</para>
  /// <list type="bullet">
  /// <item><b>Interactable</b>: 원기둥(Cylinder). 플레이어가 상호작용하면 신호를 올린다.</item>
  /// <item><b>TriggerZone</b>: 통과형 넓은 큐브(isTrigger). 플레이어가 들어오면 신호를 올린다.</item>
  /// </list>
  ///
  /// 어느 모드든 지정한 식별자로 레지스트리에 엔티티를 등록하여, 사전 검증(Preflight)의
  /// InteractionTarget 점검을 충족시키고, 인터랙션/통과 시 게이트 신호(sig.*)를 올린다.
  ///
  /// 디버그 전용이며 빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class ScenarioDevStub : MonoBehaviour, IInteractable, IInteract, IInteractionRegistryExempt
  {
    public enum StubMode
    {
      Interactable,
      TriggerZone
    }

    [SerializeField] private StubMode _mode = StubMode.Interactable;

    [Tooltip("레지스트리에 등록하고 신호를 파생할 식별자(예: vital_set, Triage_zone_Trigger).")]
    [SerializeField] private string _identifier;

    [Tooltip("통과/상호작용 시 올릴 신호 목록(sig. 접두사 제외). 비우면 식별자 기반 신호를 자동 사용한다.")]
    [SerializeField] private string[] _signals;

    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private bool _logToConsole = true;

    private string _registeredIdentifier;
    private bool _fired;

    #region IInteractable / IInteract

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => $"[DEV] {_identifier}";
    public Sprite DisplayIcon => null;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.magenta;
    public string Identifier => _identifier;

    public void Interact(Transform interactor)
    {
      if (_mode != StubMode.Interactable)
      {
        return;
      }

      RaiseSignals();
    }

    #endregion

    /// <summary>스포너가 생성 직후 설정하는 초기화 진입점.</summary>
    public void Configure(StubMode mode, string identifier, string[] signals)
    {
      _mode = mode;
      _identifier = identifier;
      _signals = signals;
    }

    private void OnEnable() => RegisterToRegistry();
    private void OnDisable() => UnregisterFromRegistry();
    private void OnDestroy() => UnregisterFromRegistry();

    private void OnTriggerEnter(Collider other)
    {
      if (_mode != StubMode.TriggerZone)
      {
        return;
      }

      if (!string.IsNullOrEmpty(_playerTag) && !other.CompareTag(_playerTag))
      {
        return;
      }

      RaiseSignals();
    }

    [ContextMenu("Raise Signals Now")]
    public void RaiseSignals()
    {
      if (_triggerOnce && _fired)
      {
        return;
      }
      _fired = true;

      if (_signals != null && _signals.Length > 0)
      {
        foreach (var signal in _signals)
        {
          Raise(signal);
        }
      }
      else if (!string.IsNullOrWhiteSpace(_identifier))
      {
        // 신호 미지정 시 식별자 기반 신호를 폭넓게 올린다.
        // 인터랙터블은 click_<id> 게이트를 충족시키고, 트리거존은 enter_<id> 류를 충족시킨다.
        Raise(_identifier);
        Raise("click_" + _identifier);
        Raise("select_" + _identifier);
        Raise("enter_" + _identifier);
      }
    }

    private void Raise(string signal)
    {
      if (string.IsNullOrWhiteSpace(signal))
      {
        return;
      }

      ScenarioInteractionSignals.Raise(signal);
      if (_logToConsole)
      {
        Debug.Log($"[ScenarioDevStub] Raised '{ScenarioInteractionSignals.Normalize(signal)}' (from '{_identifier}')", this);
      }
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
      {
        return;
      }

      _registeredIdentifier = _identifier;
      var entityType = _mode == StubMode.TriggerZone
        ? EntityType.ScenarioTriggerZone
        : EntityType.ScenarioInteractable;
      MultiplayerInfrastructure.Registry.Registry.RegisterEntity(
        _registeredIdentifier, entityType, gameObject, displayName: gameObject.name);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
      {
        return;
      }

      MultiplayerInfrastructure.Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }
  }
}
