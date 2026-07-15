using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    public List<QuestCompletionCriteria> CompletionCriteria { get; set; }
    public QuestScopeType Scope { get; set; }
    public bool IsTracked { get; set; }
    public bool IsTrackable { get; set; } = true;
    public bool IsAutoComplete { get; set; }
    public string WaypointIdentifier { get; set; }

    public QuestData()
    {
      DefinitionIdentifier = string.Empty;
      Progress = new QuestProgressValue(0, 1);
      CompletionCriteria = new List<QuestCompletionCriteria>();
      Scope = QuestScopeType.Player;
      WaypointIdentifier = string.Empty;
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
      CompletionCriteria = new List<QuestCompletionCriteria>();
      Scope = QuestScopeType.Player;
      IsTracked = isTracked;
      IsTrackable = true;
      IsAutoComplete = false;
      WaypointIdentifier = waypointIdentifier ?? string.Empty;
    }

    public QuestData Clone()
    {
      return new QuestData(Id, Title, Description, QuestContent, IsTracked, WaypointIdentifier)
      {
        DefinitionIdentifier = DefinitionIdentifier ?? string.Empty,
        Progress = Progress?.Clone() ?? new QuestProgressValue(0, 1),
        Completed = Completed,
        CompletionCriteria = CloneCriteria(CompletionCriteria),
        Scope = Scope,
        IsTrackable = IsTrackable,
        IsAutoComplete = IsAutoComplete
      };
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
    public QuestCompletionCriteriaType Type { get; set; } = QuestCompletionCriteriaType.InventoryContains;
    public string ItemId { get; set; }
    public string SignalId { get; set; }
    public string DisplayTextContent { get; set; }
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
        Type = Type,
        ItemId = ItemId,
        SignalId = SignalId,
        DisplayTextContent = DisplayTextContent,
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
    AllOf,
    AnyOf
  }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum QuestScopeType
  {
    Player,
    Global
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
    public bool IsTrackedByDefault { get; set; }
    public QuestScopeType Scope { get; set; } = QuestScopeType.Player;
    public List<QuestCompletionCriteria> CompletionCriteria { get; set; } = new();

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
        IsTrackedByDefault = IsTrackedByDefault,
        Scope = Scope,
        CompletionCriteria = new List<QuestCompletionCriteria>()
      };

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
