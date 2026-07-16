using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using MultiplayerInfrastructure.Scenario.Requirements;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// "요구 아이템을 들고 있으면 상호작용하여 제출할 수 있는" 독립 Interactable 컴포넌트.
  ///
  /// 사용 시나리오(예): 의사 NPC 또는 접수대에 부착 → 플레이어가 상호작용 → 제출 UI 가 열리고
  /// 요구 아이템을 넣고 제출 → 인벤토리에서 소모 → 서버 세션 전역 신호(sig.*)를 올린다.
  ///
  /// 설정 우선순위: 인스펙터의 프리셋 기본값(<see cref="_definition"/>)을 기본으로 하되,
  /// 시나리오 그래프 노드가 런타임에 요구 아이템/완료 신호/활성 상태를 덮어쓸 수 있다
  /// (<see cref="ApplyDefinitionOverride"/>, <see cref="SetEnabled"/>).
  ///
  /// 이 컴포넌트는 <see cref="RegistryType.InteractableEntity"/> 와 <see cref="RegistryType.Entity"/> 에
  /// 식별자로 등록되어, 그래프 노드가 식별자로 이 인스턴스를 찾아 사전 설정할 수 있게 한다.
  /// 프리팹으로 만들어 <see cref="EntityPresetDefinition"/> 으로 등록하면, EntityPresetSpawn 노드나
  /// 전용 ItemSubmissionConfig 노드로 스폰/사전설정할 수 있다.
  /// </summary>
  [DisallowMultipleComponent]
  public class ItemSubmissionInteractable : Interactable, IInteractorConditional, IInteractToggleable
  {
    [Header("Item Submission")]
    [SerializeField] private string _identifier;

    [Tooltip("요구 아이템/완료 신호/표시의 프리셋 기본값. 그래프 노드가 런타임에 덮어쓸 수 있다.")]
    [SerializeField] private ItemSubmissionDefinition _definition = new ItemSubmissionDefinition();

    [Tooltip("시작 시 상호작용 가능 여부. 그래프 노드로 활성/비활성 전환할 수 있다.")]
    [SerializeField] private bool _enabled = true;

    [Header("Display Override (optional)")]
    [SerializeField] private Sprite _displayIconOverride;
    [SerializeField] private bool _hasDisplayColorOverride;
    [SerializeField] private Color _displayColorOverride = Color.white;

    private ItemSubmissionDefinition _runtimeDefinition;
    private bool _completed;
    private string _registeredIdentifier;
    private ScenarioRequirementRuntimeRegistrationHandle _runtimeEvidence;
    private bool _initialized;

    public string Identifier => _identifier;
    public ItemSubmissionDefinition Definition => _runtimeDefinition ?? _definition;
    public bool IsCompleted => _completed;

    public override string DisplayText
    {
      get
      {
        string text = Definition?.displayText;
        return string.IsNullOrWhiteSpace(text) ? "제출하기" : text;
      }
    }

    public override Sprite DisplayIcon => _displayIconOverride != null ? _displayIconOverride : base.DisplayIcon;
    public override Color DisplayColor => _hasDisplayColorOverride ? _displayColorOverride : base.DisplayColor;

    private void Awake()
    {
      EnsureInitialized();
      RegisterToRegistry();
    }

    private void OnEnable() => RegisterToRegistry();
    private void OnDisable() => UnregisterFromRegistry();
    private void OnDestroy() => UnregisterFromRegistry();

    private void EnsureInitialized()
    {
      if (_initialized)
        return;

      _runtimeDefinition = _definition != null ? _definition.Clone() : new ItemSubmissionDefinition();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "item_submission");

      _initialized = true;
    }

    /// <summary>
    /// 코드(예: <see cref="Entity.Npc"/>)에서 이 컴포넌트를 프로그래밍 방식으로 구성한다.
    /// 인스펙터 없이 AddComponent 로 생성한 뒤 이 메서드로 식별자/정의/표시/활성 상태를 설정한다.
    /// 식별자 변경 시 레지스트리에 재등록한다.
    /// </summary>
    public void Configure(
      string identifier,
      ItemSubmissionDefinition definition,
      Sprite displayIcon = null,
      Color? displayColor = null,
      bool enabled = true)
    {
      // 이미 Awake 에서 등록되어 있을 수 있으므로 먼저 해제한다.
      UnregisterFromRegistry();

      _initialized = true;

      if (!string.IsNullOrWhiteSpace(identifier))
        _identifier = identifier;
      else if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "item_submission");

      _definition = definition != null ? definition.Clone() : new ItemSubmissionDefinition();
      _runtimeDefinition = _definition.Clone();
      _completed = false;
      _enabled = enabled;

      if (displayIcon != null)
        _displayIconOverride = displayIcon;

      if (displayColor.HasValue)
      {
        _hasDisplayColorOverride = true;
        _displayColorOverride = displayColor.Value;
      }

      RegisterToRegistry();
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Registry.Register(RegistryType.InteractableEntity, _registeredIdentifier, this);
      Registry.Registry.RegisterEntity(_registeredIdentifier, EntityType.ScenarioInteractable, gameObject, displayName: gameObject.name);
      _runtimeEvidence = ScenarioRequirementRuntimeRegistrationRegistry.Register(RegistryType.InteractableEntity, ScenarioRequirementKind.Interactable, _registeredIdentifier, this, new[] { ScenarioRequirementCapability.Interactable, ScenarioRequirementCapability.ItemSubmissionTarget });
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Registry.Unregister(RegistryType.InteractableEntity, _registeredIdentifier);
      Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _runtimeEvidence.Dispose();
      _registeredIdentifier = null;
    }

    // ── IInteractorConditional ────────────────────────────────────────────────

    public bool CanInteract(Transform interactor)
    {
      if (!_enabled)
        return false;

      if (_completed && (Definition?.consumeOnce ?? true))
        return false;

      return true;
    }

    // ── IInteract ─────────────────────────────────────────────────────────────

    public override void Interact(Transform interactor)
    {
      if (!CanInteract(interactor))
        return;

      var player = interactor != null
        ? (interactor.GetComponentInParent<PlayerController>() ?? interactor.GetComponent<PlayerController>())
        : null;

      if (player == null)
      {
        Debug.LogWarning("[ItemSubmissionInteractable] interactor 에서 PlayerController 를 찾지 못했습니다.", this);
        return;
      }

      var controller = Registry.Registry.Get<ItemSubmissionUIController>(
        RegistryType.UI, Registry.Registry.TypeKey<ItemSubmissionUIController>());

      if (controller == null)
      {
        Debug.LogWarning("[ItemSubmissionInteractable] ItemSubmissionUIController 가 레지스트리에 없습니다.", this);
        return;
      }

      controller.Open(this, player);
    }

    // ── 런타임 사전 설정 API (그래프 노드에서 호출) ─────────────────────────────

    /// <summary>요구 아이템/완료 신호/표시를 런타임에 덮어쓴다(그래프 노드 오버라이드).</summary>
    public void ApplyDefinitionOverride(ItemSubmissionDefinition definition)
    {
      if (definition == null)
        return;

      _runtimeDefinition = definition.Clone();
      _completed = false;
      RefreshHints();
    }

    /// <summary>요구 아이템 목록만 덮어쓴다.</summary>
    public void SetRequiredItems(IReadOnlyList<ItemRequirement> requiredItems)
    {
      _runtimeDefinition ??= (_definition != null ? _definition.Clone() : new ItemSubmissionDefinition());
      _runtimeDefinition.requiredItems = new List<ItemRequirement>();
      if (requiredItems != null)
      {
        for (int i = 0; i < requiredItems.Count; i++)
          _runtimeDefinition.requiredItems.Add(requiredItems[i]);
      }
      _completed = false;
      RefreshHints();
    }

    /// <summary>제출 완료 시 올릴 서버 세션 전역 신호를 설정한다.</summary>
    public void SetCompletionSignal(string signalIdentifier)
    {
      _runtimeDefinition ??= (_definition != null ? _definition.Clone() : new ItemSubmissionDefinition());
      _runtimeDefinition.completionSignalIdentifier = signalIdentifier;
    }

    /// <summary>상호작용 활성/비활성 전환(그래프 노드에서 사용).</summary>
    public void SetEnabled(bool enabled)
    {
      _enabled = enabled;
      RefreshHints();
    }

    /// <summary>완료 상태를 재설정한다(consumeOnce 이더라도 다시 제출 가능하게 함).</summary>
    public void ResetCompletion()
    {
      _completed = false;
      RefreshHints();
    }

    /// <summary>
    /// UI 에서 제출이 성공적으로 완료(아이템 소모 완료)되었을 때 호출된다.
    /// 서버 세션 전역 신호를 올리고, consumeOnce 이면 이후 상호작용을 잠근다.
    /// </summary>
    internal void NotifySubmissionCompleted()
    {
      var def = Definition;
      if (def != null && !string.IsNullOrWhiteSpace(def.completionSignalIdentifier))
      {
        Scenario.ScenarioInteractionSignals.Raise(def.completionSignalIdentifier);
      }

      if (def?.consumeOnce ?? true)
        _completed = true;

      RefreshHints();
    }

    private static void RefreshHints()
    {
      var player = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      player?.RefreshInteractableHintsNow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "item_submission");
    }
#endif
  }
}
