using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// "요구 아이템을 들고 있으면 상호작용하여 제출할 수 있는" 독립 Interactable 컴포넌트.
  ///
  /// 사용 시나리오(예): 의사 NPC 또는 접수대에 부착 → 플레이어가 상호작용 → 제출 UI 가 열리고
  /// 요구 아이템을 넣고 제출 → 인벤토리에서 소모 → 서버 세션 전역 신호(sig.*)를 올린다.
  ///
  /// <para>
  /// 인터렉션 레지스트리 도입 뒤 이 컴포넌트는 인터렉션 정의의 출처가 아니다. 제출 인터렉션의 문구·요구 아이템·
  /// 완료 신호는 시나리오 데이터나 상시 카탈로그의 <c>kind: "ItemSubmission"</c> 정의가 정하고, 레지스트리 핸들러
  /// (<c>RegistryItemSubmissionInteract</c>)가 대상 엔티티 하위에 이 컴포넌트를 만들어 <see cref="Configure"/> 로
  /// 그 값을 넣는다. 제출 UI 를 여는 데 컴포넌트가 필요해서 남긴 위임처이므로 프리팹·씬에 직접 배치하지 않는다.
  /// </para>
  ///
  /// <para>
  /// 노출(누구에게 언제 보이는가)은 레지스트리가 판정한다. 직렬화 필드는 <see cref="Configure"/> 이전의 기본값일
  /// 뿐이고, 수행 뒤 잠금은 정의의 <c>afterInteract</c> 가 맡는다(그래서 <c>consumeOnce</c> 는 꺼진 채로 구성된다).
  /// </para>
  ///
  /// 이 컴포넌트는 <see cref="RegistryType.InteractableEntity"/> 와 <see cref="RegistryType.Entity"/> 에
  /// 식별자로 등록되어, 제출 UI 와 완료 통지가 식별자로 이 인스턴스를 찾을 수 있게 한다.
  /// </summary>
  [DisallowMultipleComponent]
  public class ItemSubmissionInteractable : Interactable, IInteractorConditional, IInteractToggleable
  {
    [Header("Item Submission")]
    [SerializeField] private string _identifier;

    [Tooltip("요구 아이템/완료 신호/표시의 기본값. 런타임에는 레지스트리 정의가 Configure 로 덮어쓴다.")]
    [SerializeField] private ItemSubmissionDefinition _definition = new ItemSubmissionDefinition();

    [Tooltip("시작 시 상호작용 가능 여부. 시나리오 노출은 이 값이 아니라 인터렉션 레지스트리가 판정한다.")]
    [SerializeField] private bool _enabled = true;

    [Header("Display Override (optional)")]
    [SerializeField] private Sprite _displayIconOverride;
    [SerializeField] private bool _hasDisplayColorOverride;
    [SerializeField] private Color _displayColorOverride = Color.white;

    private ItemSubmissionDefinition _runtimeDefinition;
    private bool _completed;
    private string _registeredIdentifier;
    private bool _initialized;

    public string Identifier => _identifier;
    public override string InteractionIdentifier => _identifier;
    public override string PresentationEntityIdentifier
    {
      get
      {
        var npc = GetComponentInParent<Entity.Npc>();
        return npc != null ? npc.Identifier : _identifier;
      }
    }
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
    /// 레지스트리 핸들러(<c>RegistryItemSubmissionInteract</c>)가 이 컴포넌트를 구성하는 진입점이다.
    /// AddComponent 로 생성한 뒤 이 메서드로 인터렉션 정의의 식별자/요구 아이템/완료 신호/표시를 옮겨 담는다.
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
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Registry.Unregister(RegistryType.InteractableEntity, _registeredIdentifier);
      Registry.Registry.UnregisterEntity(_registeredIdentifier);
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

    // ── 런타임 사전 설정 API ────────────────────────────────────────────────
    // 아래 네 API 를 호출하던 그래프 노드(ItemSubmissionConfig, NpcInteractControl)는 인터렉션 레지스트리
    // 도입과 함께 폐기했다. 지금 값은 모두 Configure 로 한 번에 들어오며, 남은 호출자는 없다.

    /// <summary>요구 아이템/완료 신호/표시를 런타임에 덮어쓴다.</summary>
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

    /// <summary>상호작용 활성/비활성 전환. 시나리오 노출 판정은 레지스트리가 하므로 코드 잠금 용도로만 쓴다.</summary>
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
    /// <summary>제출이 완료될 때 (컴포넌트, 제출한 플레이어) 를 전달한다. 레지스트리의 범용 제출 핸들러가 구독한다.</summary>
    public static event System.Action<ItemSubmissionInteractable, PlayerController> SubmissionCompleted;

    internal void NotifySubmissionCompleted() => NotifySubmissionCompleted(null);

    internal void NotifySubmissionCompleted(PlayerController player)
    {
      var def = Definition;
      if (def != null && !string.IsNullOrWhiteSpace(def.completionSignalIdentifier))
      {
        Scenario.ScenarioInteractionSignals.Raise(def.completionSignalIdentifier);
      }

      if (def?.consumeOnce ?? true)
        _completed = true;

      RefreshHints();
      SubmissionCompleted?.Invoke(this, player);
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
