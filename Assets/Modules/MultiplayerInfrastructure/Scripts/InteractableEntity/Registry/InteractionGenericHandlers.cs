using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// 레지스트리가 범용 종류(Action, Signal, ItemSubmission, StartScenario)에 대해 만드는 핸들러의 공통 부분.
  /// 표시 정보는 유효 정의에서 읽고, 수행 뒤 완료 신호 발신과 가시성 처리를 레지스트리에 맡긴다.
  /// </summary>
  internal abstract class RegistryInteractBase : IInteract, IInteractDisplayIcons, IInteractDisplayPriority,
    IQuestPresentationTarget, IInteractorConditional, IDisposable
  {
    protected readonly InteractionRegistryEntry Entry;
    private readonly List<Sprite> _iconBuffer = new List<Sprite>();

    protected RegistryInteractBase(InteractionRegistryEntry entry)
    {
      Entry = entry ?? throw new ArgumentNullException(nameof(entry));
    }

    public abstract InteractionKind Kind { get; }

    protected InteractionDefinition Definition => Entry.Definition;

    public string DisplayText => string.IsNullOrWhiteSpace(Definition?.Display?.Text) ? DefaultDisplayText : Definition.Display.Text;

    protected virtual string DefaultDisplayText => "상호작용";

    public Sprite DisplayIcon
    {
      get
      {
        var icons = Definition?.Display?.IconIdentifiers;
        if (icons != null)
        {
          for (int i = 0; i < icons.Count; i++)
          {
            var sprite = ResolveIcon(icons[i]);
            if (sprite != null)
              return sprite;
          }
        }
        return DefaultIcon;
      }
    }

    protected virtual Sprite DefaultIcon => null;

    public IReadOnlyList<Sprite> DisplayIcons
    {
      get
      {
        _iconBuffer.Clear();
        var icons = Definition?.Display?.IconIdentifiers;
        if (icons != null)
        {
          for (int i = 0; i < icons.Count; i++)
          {
            var sprite = ResolveIcon(icons[i]);
            if (sprite != null)
              _iconBuffer.Add(sprite);
          }
        }
        return _iconBuffer;
      }
    }

    public bool AllowDisplayIconFallback => Definition?.Display?.AllowIconFallback ?? true;
    public Color DisplayColor => Definition?.Display?.Color ?? Color.white;
    public int DisplayPriority => Definition?.Display?.Priority ?? 0;
    public string PresentationEntityIdentifier => Entry.Address.EntityIdentifier;
    public string InteractionIdentifier => Entry.Address.InteractionIdentifier;

    public virtual bool CanInteract(Transform interactor)
      => Definition != null && ResolvePlayer(interactor) != null;

    public void Interact(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player == null || Definition == null || !CanInteract(interactor))
        return;

      if (!HasRequiredItems(player))
      {
        ShowMissingItemDialogue(Definition);
        return;
      }

      if (!TryConsumeItems(player))
        return;

      if (!Execute(player, interactor))
        return;

      if (RaisesCompletionSignal && !string.IsNullOrWhiteSpace(Definition.CompletionSignal))
        ScenarioInteractionSignals.Raise(Definition.CompletionSignal);

      InteractionRegistry.NotifyInteracted(this, player);
    }

    /// <summary>수행 본문. false 면 완료 처리(신호, 가시성)를 하지 않는다.</summary>
    protected abstract bool Execute(PlayerController player, Transform interactor);

    protected virtual bool RaisesCompletionSignal => true;

    /// <summary>정의가 다시 병합되었을 때 파생 상태를 갱신한다.</summary>
    public virtual void Refresh() { }

    public virtual void Dispose() { }

    protected static PlayerController ResolvePlayer(Transform interactor)
    {
      if (interactor == null)
        return null;
      return interactor.GetComponentInParent<PlayerController>() ?? interactor.GetComponent<PlayerController>();
    }

    protected GameObject ResolveEntityObject()
      => Registry.Registry.TryGetEntity(Entry.Address.EntityIdentifier, out var descriptor) ? descriptor?.GameObject : null;

    private static Sprite ResolveIcon(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;
      var sprite = Registry.Registry.Get<Sprite>(RegistryType.IconSprite, identifier.Trim());
      // 아이템 아이콘(Textures/Items/<id>)도 식별자로 쓸 수 있게 관례 경로 로드까지 시도한다.
      return sprite != null ? sprite : Registry.Registry.GetOrLoadIconSprite(identifier.Trim());
    }

    private bool HasRequiredItems(PlayerController player)
    {
      var required = Definition.RequiredItems;
      if (required == null)
        return true;
      for (int i = 0; i < required.Count; i++)
      {
        var requirement = required[i];
        if (requirement != null && requirement.IsValid && player.CountItemInInventory(requirement.ItemIdentifier) < requirement.Count)
          return false;
      }
      return true;
    }

    private bool TryConsumeItems(PlayerController player)
    {
      var consume = Definition.ConsumeItems;
      if (consume == null || consume.Count == 0)
        return true;

      for (int i = 0; i < consume.Count; i++)
      {
        var requirement = consume[i];
        if (requirement == null || !requirement.IsValid)
          continue;
        if (player.CountItemInInventory(requirement.ItemIdentifier) < requirement.Count)
        {
          ShowMissingItemDialogue(Definition);
          return false;
        }
      }

      for (int i = 0; i < consume.Count; i++)
      {
        var requirement = consume[i];
        if (requirement == null || !requirement.IsValid)
          continue;
        if (requirement.Count == 1)
        {
          if (!player.TryConsumeItemUse(requirement.ItemIdentifier, out var receipt))
            return false;
          player.CompleteConsumedItemUse(receipt, accepted: true);
        }
        else if (player.RemoveItemFromInventory(requirement.ItemIdentifier, requirement.Count) != requirement.Count)
        {
          return false;
        }
      }
      return true;
    }

    /// <summary>요구 물품이 없을 때의 안내. 정의의 부가 문자열(missingItemDialogue, findItemDialogue)이 있으면 그것을 쓴다.</summary>
    internal static void ShowMissingItemDialogue(InteractionDefinition definition)
    {
      var dialogue = Registry.Registry.Get<DialoguePanelUIController>(
        RegistryType.UI, Registry.Registry.TypeKey<DialoguePanelUIController>());
      if (dialogue == null)
        return;
      string missing = definition?.GetExtra("missingItemDialogue");
      string find = definition?.GetExtra("findItemDialogue");
      dialogue.TryPresentTransientDialogue("{PLAYER_NAME}",
        string.IsNullOrWhiteSpace(missing) ? "(필요한 물품을 갖고 있지 않다.)" : $"({missing})");
      dialogue.TryPresentTransientDialogue("{PLAYER_NAME}",
        string.IsNullOrWhiteSpace(find) ? "(필요한 물품을 찾자.)" : $"({find})");
    }

    internal static RegistryInteractBase Create(InteractionRegistryEntry entry)
    {
      switch (entry?.Definition?.Kind)
      {
        case InteractionKind.Action: return new RegistryActionInteract(entry);
        case InteractionKind.Signal: return new RegistrySignalInteract(entry);
        case InteractionKind.ItemSubmission: return new RegistryItemSubmissionInteract(entry);
        case InteractionKind.StartScenario: return new RegistryStartScenarioInteract(entry);
        default: return null;
      }
    }
  }

  /// <summary>월드 오브젝트를 켜고 끄며 완료 신호를 올리는 액션. 시나리오 데이터의 <c>kind: Action</c> 정의가 만든다.</summary>
  internal sealed class RegistryActionInteract : RegistryInteractBase
  {
    public RegistryActionInteract(InteractionRegistryEntry entry) : base(entry) { }

    public override InteractionKind Kind => InteractionKind.Action;

    protected override bool Execute(PlayerController player, Transform interactor)
    {
      var root = ResolveEntityObject();
      SetActive(root, Definition.ActivateObjects, true);
      SetActive(root, Definition.DeactivateObjects, false);
      return true;
    }

    private static void SetActive(GameObject root, List<string> paths, bool active)
    {
      if (root == null || paths == null)
        return;
      for (int i = 0; i < paths.Length(); i++)
      {
        string path = paths[i];
        if (string.IsNullOrWhiteSpace(path))
          continue;
        var child = root.transform.Find(path.Trim());
        if (child != null)
          child.gameObject.SetActive(active);
        else
          Debug.LogWarning($"[InteractionRegistry] Action object path '{path}' not found under '{root.name}'.", root);
      }
    }
  }

  internal static class ListExtensions
  {
    public static int Length(this List<string> list) => list?.Count ?? 0;
  }

  /// <summary>완료 신호만 올리는 인터렉션.</summary>
  internal sealed class RegistrySignalInteract : RegistryInteractBase
  {
    public RegistrySignalInteract(InteractionRegistryEntry entry) : base(entry) { }

    public override InteractionKind Kind => InteractionKind.Signal;

    protected override bool Execute(PlayerController player, Transform interactor) => true;
  }

  /// <summary>시나리오 그래프를 시작하는 인터렉션. 기존 NPC 시나리오 인터렉트를 대신한다.</summary>
  internal sealed class RegistryStartScenarioInteract : RegistryInteractBase
  {
    public RegistryStartScenarioInteract(InteractionRegistryEntry entry) : base(entry) { }

    public override InteractionKind Kind => InteractionKind.StartScenario;

    protected override string DefaultDisplayText => "시나리오 시작";

    protected override Sprite DefaultIcon
      => Registry.Registry.Get<Sprite>(RegistryType.IconSprite, Commons.IconSpriteIdentifiers.ScenarioDefault);

    protected override bool Execute(PlayerController player, Transform interactor)
    {
      string scenarioIdentifier = Definition.ScenarioIdentifier?.Trim();
      if (string.IsNullOrWhiteSpace(scenarioIdentifier))
      {
        Debug.LogWarning($"[InteractionRegistry] '{Entry.Address}' has no scenarioIdentifier.");
        return false;
      }
      if (ScenarioController.Instance == null)
      {
        Debug.LogWarning($"[InteractionRegistry] '{Entry.Address}' cannot start scenario: ScenarioController.Instance is null.");
        return false;
      }
      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out var graph, out string error))
      {
        Debug.LogWarning($"[InteractionRegistry] '{Entry.Address}' failed to load scenario '{scenarioIdentifier}': {error}");
        return false;
      }

      int? ownerClientId = null;
      var networkObject = interactor != null ? interactor.GetComponentInParent<NetworkObject>() : null;
      if (networkObject != null && networkObject.Owner.IsValid)
        ownerClientId = networkObject.Owner.ClientId;
      ScenarioController.Instance.StartScenario(graph, Definition.StartNodeIdentifier, ownerClientId);
      return true;
    }
  }

  /// <summary>
  /// 아이템 제출 인터렉션. 제출 UI 는 <see cref="ItemSubmissionInteractable"/> 컴포넌트를 요구하므로,
  /// 엔티티 하위에 그 컴포넌트를 만들어 위임한다. 완료 신호는 컴포넌트가 올린다.
  /// </summary>
  internal sealed class RegistryItemSubmissionInteract : RegistryInteractBase
  {
    private ItemSubmissionInteractable _component;
    private PlayerController _lastPlayer;

    public RegistryItemSubmissionInteract(InteractionRegistryEntry entry) : base(entry)
    {
      ItemSubmissionInteractable.SubmissionCompleted += HandleSubmissionCompleted;
      // 엔티티가 이미 있으면 컴포넌트를 미리 만들어 둔다. 제출 UI 와 완료 통지가 식별자로 이 컴포넌트를 찾기 때문이다.
      EnsureComponent();
    }

    public override InteractionKind Kind => InteractionKind.ItemSubmission;

    protected override string DefaultDisplayText => "제출하기";

    protected override bool RaisesCompletionSignal => false;

    public override bool CanInteract(Transform interactor)
    {
      if (!base.CanInteract(interactor))
        return false;
      var component = EnsureComponent();
      return component != null && component.CanInteract(interactor);
    }

    protected override bool Execute(PlayerController player, Transform interactor)
    {
      var component = EnsureComponent();
      if (component == null)
        return false;
      _lastPlayer = player;
      component.Interact(interactor);
      // 제출 UI 가 열렸을 뿐 아직 완료되지 않았다. 완료 처리는 SubmissionCompleted 에서 한다.
      return false;
    }

    public override void Refresh()
    {
      var component = EnsureComponent();
      if (component != null)
        Configure(component);
    }

    public override void Dispose()
    {
      ItemSubmissionInteractable.SubmissionCompleted -= HandleSubmissionCompleted;
      if (_component != null)
      {
        UnityEngine.Object.Destroy(_component.gameObject);
        _component = null;
      }
    }

    private void HandleSubmissionCompleted(ItemSubmissionInteractable interactable, PlayerController player)
    {
      if (interactable == null || !ReferenceEquals(interactable, _component))
        return;
      InteractionRegistry.NotifyInteracted(this, player ?? _lastPlayer);
    }

    private ItemSubmissionInteractable EnsureComponent()
    {
      if (_component != null)
        return _component;

      var root = ResolveEntityObject();
      if (root == null)
        return null;

      var child = new GameObject($"{Entry.Address.EntityIdentifier}_Submission_{Entry.Address.InteractionIdentifier}");
      child.transform.SetParent(root.transform, worldPositionStays: false);
      _component = child.AddComponent<ItemSubmissionInteractable>();
      Configure(_component);
      return _component;
    }

    private void Configure(ItemSubmissionInteractable component)
    {
      var submission = Definition.Submission;
      var requirements = new List<ItemRequirement>();
      if (submission?.RequiredItems != null)
      {
        foreach (var requirement in submission.RequiredItems)
        {
          if (requirement != null && requirement.IsValid)
            requirements.Add(new ItemRequirement(requirement.ItemIdentifier, requirement.Count));
        }
      }

      component.Configure(
        Entry.Address.InteractionIdentifier,
        new ItemSubmissionDefinition
        {
          displayText = DisplayText,
          title = string.IsNullOrWhiteSpace(submission?.Title) ? "아이템 제출" : submission.Title,
          submitButtonText = string.IsNullOrWhiteSpace(submission?.SubmitButtonText) ? "제출" : submission.SubmitButtonText,
          requiredItems = requirements,
          completionSignalIdentifier = Definition.CompletionSignal,
          // 수행 뒤 노출은 레지스트리의 afterInteract 가 정한다. 컴포넌트가 스스로 잠그면 재시도 정책이 두 곳에 갈린다.
          consumeOnce = false
        },
        displayIcon: DisplayIcon,
        displayColor: DisplayColor,
        enabled: true);
    }
  }
}
