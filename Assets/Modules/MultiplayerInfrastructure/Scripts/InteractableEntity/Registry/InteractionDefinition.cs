using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>인터렉션 정의의 종류. 범용 종류는 레지스트리가 핸들러를 만들고, Custom 은 엔티티 코드가 핸들러를 준다.</summary>
  public enum InteractionKind
  {
    Custom,
    Action,
    Signal,
    ItemSubmission,
    StartScenario
  }

  /// <summary>수행 뒤 가시성 처리. 소비 완료 개념을 대신한다.</summary>
  public enum InteractionAfterInteract
  {
    None,
    HideForPlayer,
    HideForAll
  }

  /// <summary>힌트 표시 정보. 아이콘은 IconSprite 레지스트리 식별자로 적는다.</summary>
  public sealed class InteractionDisplay
  {
    public string Text { get; set; }
    public List<string> IconIdentifiers { get; set; } = new List<string>();
    public Color? Color { get; set; }
    public bool AllowIconFallback { get; set; } = true;
    public int Priority { get; set; }

    /// <summary>데이터가 우선순위를 명시했는지. 병합에서 코드 값을 덮어쓸지 결정한다.</summary>
    public bool PrioritySpecified { get; set; }

    public InteractionDisplay Clone()
      => new InteractionDisplay
      {
        Text = Text,
        IconIdentifiers = IconIdentifiers != null ? new List<string>(IconIdentifiers) : new List<string>(),
        Color = Color,
        AllowIconFallback = AllowIconFallback,
        Priority = Priority,
        PrioritySpecified = PrioritySpecified
      };
  }

  public sealed class InteractionItemRequirement
  {
    public string ItemIdentifier { get; set; }
    public int Count { get; set; } = 1;

    public InteractionItemRequirement() { }

    public InteractionItemRequirement(string itemIdentifier, int count)
    {
      ItemIdentifier = itemIdentifier;
      Count = count <= 0 ? 1 : count;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(ItemIdentifier) && Count > 0;

    public InteractionItemRequirement Clone() => new InteractionItemRequirement(ItemIdentifier, Count);
  }

  /// <summary>ItemSubmission 종류의 제출 UI 설정.</summary>
  public sealed class InteractionSubmissionSettings
  {
    public string Title { get; set; }
    public string SubmitButtonText { get; set; }
    public List<InteractionItemRequirement> RequiredItems { get; set; } = new List<InteractionItemRequirement>();

    public InteractionSubmissionSettings Clone()
    {
      var clone = new InteractionSubmissionSettings { Title = Title, SubmitButtonText = SubmitButtonText };
      if (RequiredItems != null)
      {
        foreach (var each in RequiredItems)
        {
          if (each != null)
            clone.RequiredItems.Add(each.Clone());
        }
      }
      return clone;
    }
  }

  /// <summary>인터렉션 주소. 엔티티 식별자와 인터렉션 식별자의 쌍이며 유일성 범위는 엔티티 안이다.</summary>
  public readonly struct InteractionAddress : IEquatable<InteractionAddress>
  {
    public const char Separator = '/';

    public string EntityIdentifier { get; }
    public string InteractionIdentifier { get; }

    public InteractionAddress(string entityIdentifier, string interactionIdentifier)
    {
      EntityIdentifier = entityIdentifier?.Trim() ?? string.Empty;
      InteractionIdentifier = interactionIdentifier?.Trim() ?? string.Empty;
    }

    public bool IsValid => EntityIdentifier.Length > 0 && InteractionIdentifier.Length > 0;

    public string Key => EntityIdentifier + Separator + InteractionIdentifier;

    public static bool TryParse(string key, out InteractionAddress address)
    {
      address = default;
      if (string.IsNullOrWhiteSpace(key))
        return false;
      int index = key.LastIndexOf(Separator);
      if (index <= 0 || index >= key.Length - 1)
        return false;
      address = new InteractionAddress(key.Substring(0, index), key.Substring(index + 1));
      return address.IsValid;
    }

    public bool Equals(InteractionAddress other)
      => string.Equals(EntityIdentifier, other.EntityIdentifier, StringComparison.Ordinal)
         && string.Equals(InteractionIdentifier, other.InteractionIdentifier, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is InteractionAddress other && Equals(other);

    public override int GetHashCode()
    {
      unchecked
      {
        return (StringComparer.Ordinal.GetHashCode(EntityIdentifier) * 397)
               ^ StringComparer.Ordinal.GetHashCode(InteractionIdentifier);
      }
    }

    public override string ToString() => Key;
  }

  /// <summary>
  /// 인터렉션 정의 레코드. 코드 리터럴과 시나리오 데이터가 같은 형식을 쓰며, 데이터 정의는
  /// 같은 주소의 코드 정의 위에 <see cref="MergeOverlay"/> 규칙으로 병합된다.
  /// </summary>
  public sealed class InteractionDefinition
  {
    /// <summary>소유 엔티티 참조. 코드 리터럴은 식별자 참조만 쓰고, 데이터는 태그 참조도 쓸 수 있다.</summary>
    public ScenarioEntityReference Entity { get; set; } = new ScenarioEntityReference();

    public string InteractionIdentifier { get; set; }

    public InteractionKind Kind { get; set; } = InteractionKind.Custom;

    /// <summary>데이터 정의가 명시했는지. 병합에서 코드 값을 덮어쓸지 결정한다.</summary>
    public bool KindSpecified { get; set; }

    /// <summary>Custom 종류에서 코드 핸들러를 찾는 키. 코드 리터럴이 핸들러와 함께 등록한다.</summary>
    public string HandlerKey { get; set; }

    public InteractionDisplay Display { get; set; } = new InteractionDisplay();

    /// <summary>범용 종류가 수행 완료 시 올릴 신호. Custom 은 핸들러가 스스로 올린다.</summary>
    public string CompletionSignal { get; set; }

    public InteractionAfterInteract AfterInteract { get; set; } = InteractionAfterInteract.None;
    public bool AfterInteractSpecified { get; set; }

    /// <summary>수행에 필요하지만 소비하지 않는 아이템. 없으면 수행 시 안내 대사를 띄우고 중단한다(노출은 유지).</summary>
    public List<InteractionItemRequirement> RequiredItems { get; set; } = new List<InteractionItemRequirement>();

    /// <summary>수행 시 인벤토리에서 소비할 아이템.</summary>
    public List<InteractionItemRequirement> ConsumeItems { get; set; } = new List<InteractionItemRequirement>();

    /// <summary>
    /// 핸들러가 해석하는 부가 문자열(예: missingItemDialogue, findItemDialogue, actionDialogue, resultDialogue).
    /// 키는 소문자 카멜 표기를 쓴다.
    /// </summary>
    public Dictionary<string, string> Extras { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>조건도 오버라이드도 없을 때의 가시성. 기본값 false.</summary>
    public bool InitialVisible { get; set; }
    public bool InitialVisibleSpecified { get; set; }

    public List<ScenarioCondition> VisibilityConditions { get; set; } = new List<ScenarioCondition>();
    public ScenarioConditionMatchMode MatchMode { get; set; } = ScenarioConditionMatchMode.All;

    public InteractionSubmissionSettings Submission { get; set; }

    /// <summary>StartScenario 종류가 시작할 시나리오.</summary>
    public string ScenarioIdentifier { get; set; }
    public string StartNodeIdentifier { get; set; }

    /// <summary>Action 종류가 수행 뒤 켤/끌 엔티티 하위 오브젝트 경로(Transform.Find 규칙).</summary>
    public List<string> ActivateObjects { get; set; } = new List<string>();
    public List<string> DeactivateObjects { get; set; } = new List<string>();

    public InteractionAddress AddressFor(string entityIdentifier)
      => new InteractionAddress(entityIdentifier, InteractionIdentifier);

    public bool HasVisibilityConditions => VisibilityConditions != null && VisibilityConditions.Count > 0;

    public InteractionDefinition Clone()
    {
      var clone = new InteractionDefinition
      {
        Entity = Entity?.Clone() ?? new ScenarioEntityReference(),
        InteractionIdentifier = InteractionIdentifier,
        Kind = Kind,
        KindSpecified = KindSpecified,
        HandlerKey = HandlerKey,
        Display = Display?.Clone() ?? new InteractionDisplay(),
        CompletionSignal = CompletionSignal,
        AfterInteract = AfterInteract,
        AfterInteractSpecified = AfterInteractSpecified,
        InitialVisible = InitialVisible,
        InitialVisibleSpecified = InitialVisibleSpecified,
        VisibilityConditions = ScenarioCondition.CloneList(VisibilityConditions),
        MatchMode = MatchMode,
        Submission = Submission?.Clone(),
        ScenarioIdentifier = ScenarioIdentifier,
        StartNodeIdentifier = StartNodeIdentifier,
        ActivateObjects = ActivateObjects != null ? new List<string>(ActivateObjects) : new List<string>(),
        DeactivateObjects = DeactivateObjects != null ? new List<string>(DeactivateObjects) : new List<string>(),
        Extras = Extras != null ? new Dictionary<string, string>(Extras, StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal)
      };
      if (RequiredItems != null)
      {
        foreach (var each in RequiredItems)
        {
          if (each != null)
            clone.RequiredItems.Add(each.Clone());
        }
      }
      if (ConsumeItems != null)
      {
        foreach (var each in ConsumeItems)
        {
          if (each != null)
            clone.ConsumeItems.Add(each.Clone());
        }
      }
      return clone;
    }

    /// <summary>
    /// 이 정의(기반) 위에 다른 정의(덮개)를 얹은 새 정의를 만든다. 덮개가 명시한 필드만 덮어쓴다.
    /// 목록 필드는 덮개에 항목이 있을 때만 통째로 교체한다.
    /// </summary>
    public InteractionDefinition MergeOverlay(InteractionDefinition overlay)
    {
      var merged = Clone();
      if (overlay == null)
        return merged;

      if (overlay.KindSpecified)
        merged.Kind = overlay.Kind;
      if (!string.IsNullOrWhiteSpace(overlay.HandlerKey))
        merged.HandlerKey = overlay.HandlerKey;

      if (overlay.Display != null)
      {
        if (!string.IsNullOrWhiteSpace(overlay.Display.Text))
          merged.Display.Text = overlay.Display.Text;
        if (overlay.Display.IconIdentifiers != null && overlay.Display.IconIdentifiers.Count > 0)
          merged.Display.IconIdentifiers = new List<string>(overlay.Display.IconIdentifiers);
        if (overlay.Display.Color.HasValue)
          merged.Display.Color = overlay.Display.Color;
        if (overlay.Display.PrioritySpecified)
        {
          merged.Display.Priority = overlay.Display.Priority;
          merged.Display.PrioritySpecified = true;
        }
        merged.Display.AllowIconFallback = overlay.Display.AllowIconFallback;
      }

      if (!string.IsNullOrWhiteSpace(overlay.CompletionSignal))
        merged.CompletionSignal = overlay.CompletionSignal;
      if (overlay.AfterInteractSpecified)
      {
        merged.AfterInteract = overlay.AfterInteract;
        merged.AfterInteractSpecified = true;
      }
      if (overlay.RequiredItems != null && overlay.RequiredItems.Count > 0)
      {
        merged.RequiredItems = new List<InteractionItemRequirement>();
        foreach (var each in overlay.RequiredItems)
        {
          if (each != null)
            merged.RequiredItems.Add(each.Clone());
        }
      }
      if (overlay.Extras != null)
      {
        foreach (var pair in overlay.Extras)
          merged.Extras[pair.Key] = pair.Value;
      }
      if (overlay.ConsumeItems != null && overlay.ConsumeItems.Count > 0)
      {
        merged.ConsumeItems = new List<InteractionItemRequirement>();
        foreach (var each in overlay.ConsumeItems)
        {
          if (each != null)
            merged.ConsumeItems.Add(each.Clone());
        }
      }
      if (overlay.InitialVisibleSpecified)
      {
        merged.InitialVisible = overlay.InitialVisible;
        merged.InitialVisibleSpecified = true;
      }
      if (overlay.VisibilityConditions != null && overlay.VisibilityConditions.Count > 0)
      {
        merged.VisibilityConditions = ScenarioCondition.CloneList(overlay.VisibilityConditions);
        merged.MatchMode = overlay.MatchMode;
      }
      if (overlay.Submission != null)
        merged.Submission = overlay.Submission.Clone();
      if (!string.IsNullOrWhiteSpace(overlay.ScenarioIdentifier))
        merged.ScenarioIdentifier = overlay.ScenarioIdentifier;
      if (!string.IsNullOrWhiteSpace(overlay.StartNodeIdentifier))
        merged.StartNodeIdentifier = overlay.StartNodeIdentifier;
      if (overlay.ActivateObjects != null && overlay.ActivateObjects.Count > 0)
        merged.ActivateObjects = new List<string>(overlay.ActivateObjects);
      if (overlay.DeactivateObjects != null && overlay.DeactivateObjects.Count > 0)
        merged.DeactivateObjects = new List<string>(overlay.DeactivateObjects);

      return merged;
    }

    /// <summary>부가 문자열을 읽는다. 없으면 null.</summary>
    public string GetExtra(string key)
      => Extras != null && !string.IsNullOrWhiteSpace(key) && Extras.TryGetValue(key, out var value) ? value : null;

    /// <summary>코드 리터럴 선언에서 자주 쓰는 생성 도우미.</summary>
    public static InteractionDefinition Code(string entityIdentifier, string interactionIdentifier, string displayText,
      bool initialVisible = false, InteractionKind kind = InteractionKind.Custom)
      => new InteractionDefinition
      {
        Entity = ScenarioEntityReference.ForIdentifier(entityIdentifier),
        InteractionIdentifier = interactionIdentifier,
        Kind = kind,
        KindSpecified = true,
        Display = new InteractionDisplay { Text = displayText },
        InitialVisible = initialVisible,
        InitialVisibleSpecified = true
      };
  }

  /// <summary>
  /// 코드 리터럴 선언 하나. Custom 종류는 핸들러(<see cref="IInteract"/> 구현체)를 함께 준다.
  /// 범용 종류는 핸들러를 비워 두면 레지스트리가 만든다.
  /// </summary>
  public sealed class InteractionDeclaration
  {
    public InteractionDefinition Definition { get; }
    public IInteract Handler { get; }

    public InteractionDeclaration(InteractionDefinition definition, IInteract handler = null)
    {
      Definition = definition ?? throw new ArgumentNullException(nameof(definition));
      Handler = handler;
    }
  }

  /// <summary>
  /// 엔티티 컴포넌트가 자기 인터렉션을 코드 리터럴로 선언하는 규약. 엔티티 초기화 사이클에서
  /// <see cref="InteractionRegistry.DeclareCode(string, IInteractionDefinitionSource)"/> 가 한 번 호출한다.
  /// </summary>
  public interface IInteractionDefinitionSource
  {
    IEnumerable<InteractionDeclaration> DeclareInteractions();
  }

  /// <summary>
  /// 시나리오 데이터가 handlerKey 로만 선언한 Custom 정의의 핸들러를 엔티티 코드가 만들어 주는 규약.
  /// 레지스트리는 대상 엔티티 GameObject 의 구현체에 차례로 묻는다.
  /// </summary>
  public interface IInteractionHandlerFactory
  {
    bool TryCreateInteractionHandler(InteractionDefinition definition, out IInteract handler);
  }

  /// <summary>
  /// 레지스트리에 주소를 두지 않는 순수 월드 인터렉션(연결 지점, 설치 지점, 획득 등)을 표시하는 마커.
  /// 힌트 수집 경로가 미등록 경고를 내지 않는다.
  /// </summary>
  public interface IInteractionRegistryExempt
  {
  }

  /// <summary>
  /// Custom 핸들러가 수행 성공 여부를 레지스트리에 알리는 선택 규약. 구현하지 않으면 레지스트리는
  /// 수행 뒤 가시성 처리(<see cref="InteractionAfterInteract"/>)를 적용하지 않는다.
  /// </summary>
  public interface IInteractOutcomeSource
  {
    bool LastInteractSucceeded { get; }
  }
}
