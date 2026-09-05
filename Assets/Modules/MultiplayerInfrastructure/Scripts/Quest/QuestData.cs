using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Quest
{
  [Serializable]
  public class QuestData
  {
    public string Id { get; set; }
    public string DefinitionIdentifier { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string QuestContent { get; set; }
    public QuestProgressValue Progress { get; set; }
    public bool Completed { get; set; }
    public bool IsOrdinal { get; set; }
    public List<QuestCompletionCriteria> Tasks { get; set; }
    public List<QuestCompletionCriteria> CompletionCriteria { get; set; }
    public QuestScopeType Scope { get; set; }
    public bool IsTracked { get; set; }
    public bool IsTrackable { get; set; } = true;
    public bool IsAutoComplete { get; set; }
    /// <summary>세션이 종료된 뒤에도 이 퀘스트의 진행 상태를 유지할지 여부입니다.</summary>
    public bool PersistProgressOnSessionEnd { get; set; }
    public string WaypointIdentifier { get; set; }
    public List<QuestPresentationBinding> PresentationBindings { get; set; }

    [JsonIgnore]
    public string SourceScenarioIdentifier { get; set; }

    /// <summary>
    /// 여러 참여자가 함께 끝내야 넘어가는 구간에서 이 퀘스트가 놓인 공동 진행 상태.
    /// QuestManager 가 스냅샷을 내보낼 때 채우는 런타임 값이며 직렬화하지 않는다. 해당 구간이 아니면 null 이다.
    /// </summary>
    [JsonIgnore]
    public QuestGroupWaitStatus GroupWait { get; set; }

    /// <summary>분기 퀘스트가 모두 회수된 뒤 대기 상태만 보여 주기 위해 합성한 자리 표시 퀘스트인지 여부.</summary>
    [JsonIgnore]
    public bool IsGroupWaitPlaceholder { get; set; }

    public QuestData()
    {
      DefinitionIdentifier = string.Empty;
      Progress = new QuestProgressValue(0, 1);
      Tasks = new List<QuestCompletionCriteria>();
      CompletionCriteria = new List<QuestCompletionCriteria>();
      Scope = QuestScopeType.Player;
      WaypointIdentifier = string.Empty;
      PresentationBindings = new List<QuestPresentationBinding>();
    }

    public QuestData(string id, string title, string description, string questContent, bool isTracked = false, string waypointIdentifier = null)
    {
      Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
      DefinitionIdentifier = string.Empty;
      Title = title ?? string.Empty;
      Description = description ?? string.Empty;
      QuestContent = questContent ?? string.Empty;
      Progress = new QuestProgressValue(0, 1);
      Completed = false;
      Tasks = new List<QuestCompletionCriteria>();
      CompletionCriteria = new List<QuestCompletionCriteria>();
      Scope = QuestScopeType.Player;
      IsTracked = isTracked;
      IsTrackable = true;
      IsAutoComplete = false;
      WaypointIdentifier = waypointIdentifier ?? string.Empty;
      PresentationBindings = new List<QuestPresentationBinding>();
    }

    public QuestData Clone()
    {
      return new QuestData(Id, Title, Description, QuestContent, IsTracked, WaypointIdentifier)
      {
        DefinitionIdentifier = DefinitionIdentifier ?? string.Empty,
        Progress = Progress?.Clone() ?? new QuestProgressValue(0, 1),
        Completed = Completed,
        IsOrdinal = IsOrdinal,
        Tasks = CloneCriteria(Tasks),
        CompletionCriteria = CloneCriteria(CompletionCriteria),
        Scope = Scope,
        IsTrackable = IsTrackable,
        IsAutoComplete = IsAutoComplete,
        PersistProgressOnSessionEnd = PersistProgressOnSessionEnd,
        PresentationBindings = ClonePresentationBindings(PresentationBindings),
        SourceScenarioIdentifier = SourceScenarioIdentifier,
        GroupWait = GroupWait,
        IsGroupWaitPlaceholder = IsGroupWaitPlaceholder
      };
    }

    internal static List<QuestPresentationBinding> ClonePresentationBindings(
      IReadOnlyList<QuestPresentationBinding> bindings)
    {
      var list = new List<QuestPresentationBinding>();
      if (bindings == null)
        return list;

      for (int i = 0; i < bindings.Count; i++)
      {
        if (bindings[i] != null)
          list.Add(bindings[i].Clone());
      }

      return list;
    }

    private static List<QuestCompletionCriteria> CloneCriteria(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      var list = new List<QuestCompletionCriteria>();
      if (criteria == null)
        return list;

      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        if (each != null)
          list.Add(each.Clone());
      }
      return list;
    }
  }

  [Serializable]
  public sealed class QuestProgressValue
  {
    public static QuestProgressValue SingleStep => new QuestProgressValue(0, 1);

    public int Current { get; set; }
    public int Target { get; set; } = 1;

    public QuestProgressValue()
    {
    }

    public QuestProgressValue(int current, int target)
    {
      Current = Math.Max(0, current);
      Target = Math.Max(1, target);
      if (Current > Target)
        Current = Target;
    }

    public QuestProgressValue Clone()
    {
      return new QuestProgressValue(Current, Target);
    }

    public string ToDisplayText()
    {
      int safeTarget = Math.Max(1, Target);
      int safeCurrent = Math.Min(Math.Max(Current, 0), safeTarget);
      return $"{safeCurrent}/{safeTarget}";
    }
  }

  [Serializable]
  public sealed class QuestCompletionCriteria
  {
    public const float DefaultReachDistance = 4f;

    public string Identifier { get; set; }
    public QuestCompletionCriteriaType Type { get; set; } = QuestCompletionCriteriaType.InventoryContains;
    public string ItemId { get; set; }
    public string SignalId { get; set; }

    /// <summary>
    /// <see cref="QuestCompletionCriteriaType.InteractionSignalReceived"/> 조건을 어느 범위로 판정할지 지정합니다.
    /// 기본값인 <see cref="ScenarioSignalScope.Any"/>는 누가 올린 신호인지 구분하지 않으므로,
    /// 여러 참여자가 함께 달성하는 공동 목표에 적합합니다.
    /// <see cref="ScenarioSignalScope.Owner"/>는 이 퀘스트를 보유한 참여자가 직접 올린 신호만 인정하므로,
    /// 역할별로 한 사람이 수행해야 하는 목표가 다른 참여자의 행동으로 완료되는 것을 막습니다.
    /// </summary>
    public ScenarioSignalScope SignalScope { get; set; } = ScenarioSignalScope.Any;

    public string WaypointIdentifier { get; set; }
    public float ReachDistance { get; set; } = DefaultReachDistance;
    public string DisplayTextContent { get; set; }
    public string OnCompleteSignalIdentifier { get; set; }
    public int Count { get; set; } = 1;
    public List<QuestCompletionCriteria> Conditions { get; set; } = new();

    [JsonIgnore]
    public QuestProgressValue Progress { get; set; } = QuestProgressValue.SingleStep;

    [JsonIgnore]
    public bool Completed { get; set; }

    public QuestCompletionCriteria Clone(bool includeRuntimeState = true)
    {
      var copy = new QuestCompletionCriteria
      {
        Identifier = Identifier,
        Type = Type,
        ItemId = ItemId,
        SignalId = SignalId,
        SignalScope = SignalScope,
        WaypointIdentifier = WaypointIdentifier,
        ReachDistance = ReachDistance,
        DisplayTextContent = DisplayTextContent,
        OnCompleteSignalIdentifier = OnCompleteSignalIdentifier,
        Count = Count,
        Progress = includeRuntimeState ? Progress?.Clone() ?? QuestProgressValue.SingleStep : QuestProgressValue.SingleStep,
        Completed = includeRuntimeState && Completed,
        Conditions = new List<QuestCompletionCriteria>()
      };

      if (Conditions != null)
      {
        for (int i = 0; i < Conditions.Count; i++)
        {
          var child = Conditions[i];
          if (child != null)
            copy.Conditions.Add(child.Clone(includeRuntimeState));
        }
      }

      return copy;
    }
  }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum QuestCompletionCriteriaType
  {
    InventoryContains,
    InteractionSignalReceived,
    WaypointReached,
    AllOf,
    AnyOf
  }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum QuestScopeType
  {
    Player,
    Global
  }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum QuestPresentationActivation
  {
    WholeQuest,
    CompletionCriteria
  }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum QuestPresentationTargetType
  {
    Interaction,
    Npc,

    /// <summary>월드에 배치된 waypoint 앵커 위에 마크를 띄운다. NPC 머리 위 마크와 같은 표시 경로를 쓴다.</summary>
    Waypoint
  }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum QuestPresentationIconMode
  {
    ReplacePrimaryIcon
  }

  [Serializable]
  public sealed class QuestPresentationBinding
  {
    public QuestPresentationActivation Activation { get; set; } = QuestPresentationActivation.WholeQuest;
    public string CompletionCriteriaIdentifier { get; set; }
    public QuestPresentationTargetType TargetType { get; set; }
    public string EntityIdentifier { get; set; }
    public string InteractionIdentifier { get; set; }
    public string IconIdentifier { get; set; }
    public QuestPresentationIconMode IconMode { get; set; } = QuestPresentationIconMode.ReplacePrimaryIcon;
    public int Priority { get; set; }
    public bool ShowWhenUntracked { get; set; } = true;

    public QuestPresentationBinding Clone()
    {
      return new QuestPresentationBinding
      {
        Activation = Activation,
        CompletionCriteriaIdentifier = CompletionCriteriaIdentifier,
        TargetType = TargetType,
        EntityIdentifier = EntityIdentifier,
        InteractionIdentifier = InteractionIdentifier,
        IconIdentifier = IconIdentifier,
        IconMode = IconMode,
        Priority = Priority,
        ShowWhenUntracked = ShowWhenUntracked
      };
    }
  }

  [Serializable]
  public sealed class QuestDefinition
  {
    public string Identifier { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string QuestContent { get; set; }
    public string WaypointIdentifier { get; set; }
    public bool IsTrackable { get; set; } = true;
    public bool IsAutoComplete { get; set; }
    /// <summary>세션 종료 후에도 이 정의에서 생성된 퀘스트 진행 상태를 유지할지 여부입니다.</summary>
    public bool PersistProgressOnSessionEnd { get; set; }
    public bool IsTrackedByDefault { get; set; }
    public QuestScopeType Scope { get; set; } = QuestScopeType.Player;
    public bool IsOrdinal { get; set; }
    public List<QuestCompletionCriteria> Tasks { get; set; } = new();
    public List<QuestCompletionCriteria> CompletionCriteria { get; set; } = new();
    public List<QuestPresentationBinding> PresentationBindings { get; set; } = new();

    public QuestDefinition Clone()
    {
      var copy = new QuestDefinition
      {
        Identifier = Identifier,
        Title = Title,
        Description = Description,
        QuestContent = QuestContent,
        WaypointIdentifier = WaypointIdentifier,
        IsTrackable = IsTrackable,
        IsAutoComplete = IsAutoComplete,
        PersistProgressOnSessionEnd = PersistProgressOnSessionEnd,
        IsTrackedByDefault = IsTrackedByDefault,
        Scope = Scope,
        IsOrdinal = IsOrdinal,
        Tasks = new List<QuestCompletionCriteria>(),
        CompletionCriteria = new List<QuestCompletionCriteria>(),
        PresentationBindings = QuestData.ClonePresentationBindings(PresentationBindings)
      };

      if (Tasks != null)
      {
        for (int i = 0; i < Tasks.Count; i++)
        {
          var each = Tasks[i];
          if (each != null)
            copy.Tasks.Add(each.Clone(includeRuntimeState: false));
        }
      }

      if (CompletionCriteria != null)
      {
        for (int i = 0; i < CompletionCriteria.Count; i++)
        {
          var each = CompletionCriteria[i];
          if (each != null)
            copy.CompletionCriteria.Add(each.Clone(includeRuntimeState: false));
        }
      }

      return copy;
    }
  }

  [Serializable]
  public sealed class QuestDefinitionRegistryPayload
  {
    public string Namespace { get; set; }
    public List<QuestDefinition> Definitions { get; set; } = new();
  }

  public readonly struct QuestCriteriaEvaluationResult
  {
    public bool IsSatisfied { get; }
    public int Current { get; }
    public int Target { get; }

    public QuestCriteriaEvaluationResult(bool isSatisfied, int current, int target)
    {
      IsSatisfied = isSatisfied;
      Current = current;
      Target = target;
    }
  }

  internal sealed class QuestCriteriaEvaluationNode
  {
    public QuestCriteriaEvaluationResult Result { get; }
    public IReadOnlyList<QuestCriteriaEvaluationNode> Children { get; }

    public QuestCriteriaEvaluationNode(QuestCriteriaEvaluationResult result, IReadOnlyList<QuestCriteriaEvaluationNode> children = null)
    {
      Result = result;
      Children = children ?? Array.Empty<QuestCriteriaEvaluationNode>();
    }
  }
}
