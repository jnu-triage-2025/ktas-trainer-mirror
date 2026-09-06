using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>조건 절의 종류. 관찰자(플레이어) 단위 조건과 전역 조건이 함께 있다.</summary>
  public enum ScenarioConditionType
  {
    /// <summary>관찰자가 역할 태그를 가진다.</summary>
    PlayerHasTag,
    /// <summary>관찰자의 퀘스트 상태 플래그 풀에 플래그가 있다.</summary>
    PlayerHasQuestFlag,
    /// <summary>관찰자의 퀘스트 목록에서 퀘스트가 지정 상태다.</summary>
    PlayerHasQuest,
    /// <summary>관찰자의 인벤토리에 아이템이 수량 이상 있다.</summary>
    PlayerHasItem,
    /// <summary>관찰자 코드가 노출한 조건 값(<see cref="IConditionStateProvider"/>)을 비교한다.</summary>
    PlayerState,
    /// <summary>관찰자와 엔티티 사이 거리가 지정 미터 이내다.</summary>
    PlayerWithinDistance,
    /// <summary>시나리오 신호(sig.*)가 올라가 있다.</summary>
    SignalRaised,
    /// <summary>레지스트리에 식별자가 등록되어 있다.</summary>
    RegistryContains,
    /// <summary>엔티티가 태그를 가진다.</summary>
    EntityHasTag,
    /// <summary>엔티티 코드가 노출한 조건 값(<see cref="IConditionStateProvider"/>)을 비교한다.</summary>
    EntityState,
    /// <summary>접속 플레이어 수(선택적으로 태그 보유자 수)를 비교한다.</summary>
    PlayerCount,
    /// <summary>지정 시나리오가 실행 중이다.</summary>
    ScenarioActive,
    /// <summary>하위 조건 절을 AND/OR로 묶는다.</summary>
    Group
  }

  public enum ScenarioConditionMatchMode
  {
    All,
    Any
  }

  public enum ScenarioConditionCompare
  {
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
  }

  public enum ScenarioQuestConditionState
  {
    /// <summary>퀘스트가 존재하고 완료되지 않았다. 완료 목표 식별자가 있으면 그 목표가 현재 진행 대상이어야 한다.</summary>
    Active,
    /// <summary>퀘스트가 존재하고 완료되었다.</summary>
    Completed,
    /// <summary>퀘스트가 목록에 없다.</summary>
    Absent
  }

  /// <summary>
  /// 엔티티 참조. 식별자 하나를 가리키거나, 태그를 가진 모든 엔티티를 가리킨다.
  /// </summary>
  public sealed class ScenarioEntityReference
  {
    public string Identifier { get; set; }
    public string Tag { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Identifier) && string.IsNullOrWhiteSpace(Tag);
    public bool IsTagReference => string.IsNullOrWhiteSpace(Identifier) && !string.IsNullOrWhiteSpace(Tag);

    public static ScenarioEntityReference ForIdentifier(string identifier)
      => new ScenarioEntityReference { Identifier = identifier?.Trim() };

    public static ScenarioEntityReference ForTag(string tag)
      => new ScenarioEntityReference { Tag = tag?.Trim() };

    public ScenarioEntityReference Clone()
      => new ScenarioEntityReference { Identifier = Identifier, Tag = Tag };

    /// <summary>지정 엔티티 식별자가 이 참조에 해당하는지 판정한다.</summary>
    public bool Matches(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return false;
      if (!string.IsNullOrWhiteSpace(Identifier))
        return string.Equals(Identifier.Trim(), entityIdentifier.Trim(), StringComparison.Ordinal);
      if (!string.IsNullOrWhiteSpace(Tag))
        return global::MultiplayerInfrastructure.Tag.PlayerTagService.HasTagOnIdentifier(entityIdentifier.Trim(), Tag.Trim());
      return false;
    }

    public override string ToString()
      => !string.IsNullOrWhiteSpace(Identifier) ? "id:" + Identifier : (!string.IsNullOrWhiteSpace(Tag) ? "tag:" + Tag : "(empty)");
  }

  /// <summary>
  /// 시나리오 진행 상태와 코드가 노출한 값을 참·거짓으로 판정하는 조건 절.
  /// 인터렉션 가시성과 Validator 노드가 같은 형식을 쓴다. 판정은 <see cref="ScenarioConditionEvaluator"/>가 수행한다.
  /// </summary>
  public sealed class ScenarioCondition
  {
    public ScenarioConditionType Type { get; set; } = ScenarioConditionType.SignalRaised;

    /// <summary>true 이면 판정 결과를 뒤집는다.</summary>
    public bool Negate { get; set; }

    /// <summary>PlayerHasTag, EntityHasTag, PlayerCount(선택)의 태그.</summary>
    public string Tag { get; set; }

    /// <summary>PlayerHasQuestFlag의 플래그.</summary>
    public string Flag { get; set; }

    public string QuestIdentifier { get; set; }
    public ScenarioQuestConditionState QuestState { get; set; } = ScenarioQuestConditionState.Active;
    public string CompletionCriteriaIdentifier { get; set; }

    /// <summary>PlayerHasItem의 아이템 식별자.</summary>
    public string ItemIdentifier { get; set; }

    /// <summary>PlayerHasItem의 최소 수량, PlayerCount의 비교 대상 수.</summary>
    public int Count { get; set; } = 1;

    /// <summary>PlayerState, EntityState가 조회할 조건 키.</summary>
    public string Key { get; set; }

    /// <summary>조건 키의 하위 구분자(예: 팔 방향, 신호 이름).</summary>
    public string Qualifier { get; set; }

    public ScenarioConditionCompare Compare { get; set; } = ScenarioConditionCompare.Equal;

    /// <summary>PlayerState, EntityState의 비교 값(문자열로 표기; bool, 숫자, 문자열을 해석한다). 비우면 true 와 비교한다.</summary>
    public string Value { get; set; }

    /// <summary>PlayerWithinDistance, EntityHasTag, EntityState의 대상 엔티티.</summary>
    public ScenarioEntityReference Entity { get; set; }

    /// <summary>PlayerWithinDistance의 거리(미터).</summary>
    public float Meters { get; set; } = 3f;

    /// <summary>SignalRaised의 신호 식별자('sig.' 접두사는 자동 정규화).</summary>
    public string Signal { get; set; }

    public RegistryType RegistryType { get; set; } = RegistryType.RuntimeState;

    /// <summary>RegistryContains의 식별자.</summary>
    public string Identifier { get; set; }

    /// <summary>ScenarioActive의 시나리오 식별자.</summary>
    public string ScenarioIdentifier { get; set; }

    /// <summary>Group의 결합 방식.</summary>
    public ScenarioConditionMatchMode MatchMode { get; set; } = ScenarioConditionMatchMode.All;

    /// <summary>Group의 하위 조건 절.</summary>
    public List<ScenarioCondition> Conditions { get; set; } = new List<ScenarioCondition>();

    /// <summary>관찰자(플레이어)가 있어야 판정할 수 있는 종류인지.</summary>
    public bool RequiresPlayer
    {
      get
      {
        switch (Type)
        {
          case ScenarioConditionType.PlayerHasTag:
          case ScenarioConditionType.PlayerHasQuestFlag:
          case ScenarioConditionType.PlayerHasQuest:
          case ScenarioConditionType.PlayerHasItem:
          case ScenarioConditionType.PlayerState:
          case ScenarioConditionType.PlayerWithinDistance:
            return true;
          case ScenarioConditionType.Group:
            if (Conditions == null)
              return false;
            for (int i = 0; i < Conditions.Count; i++)
            {
              if (Conditions[i] != null && Conditions[i].RequiresPlayer)
                return true;
            }
            return false;
          default:
            return false;
        }
      }
    }

    public static bool AnyRequiresPlayer(IReadOnlyList<ScenarioCondition> conditions)
    {
      if (conditions == null)
        return false;
      for (int i = 0; i < conditions.Count; i++)
      {
        if (conditions[i] != null && conditions[i].RequiresPlayer)
          return true;
      }
      return false;
    }

    public ScenarioCondition Clone()
    {
      var clone = new ScenarioCondition
      {
        Type = Type,
        Negate = Negate,
        Tag = Tag,
        Flag = Flag,
        QuestIdentifier = QuestIdentifier,
        QuestState = QuestState,
        CompletionCriteriaIdentifier = CompletionCriteriaIdentifier,
        ItemIdentifier = ItemIdentifier,
        Count = Count,
        Key = Key,
        Qualifier = Qualifier,
        Compare = Compare,
        Value = Value,
        Entity = Entity?.Clone(),
        Meters = Meters,
        Signal = Signal,
        RegistryType = RegistryType,
        Identifier = Identifier,
        ScenarioIdentifier = ScenarioIdentifier,
        MatchMode = MatchMode,
        Conditions = new List<ScenarioCondition>()
      };
      if (Conditions != null)
      {
        for (int i = 0; i < Conditions.Count; i++)
        {
          if (Conditions[i] != null)
            clone.Conditions.Add(Conditions[i].Clone());
        }
      }
      return clone;
    }

    public static List<ScenarioCondition> CloneList(IReadOnlyList<ScenarioCondition> conditions)
    {
      var list = new List<ScenarioCondition>();
      if (conditions == null)
        return list;
      for (int i = 0; i < conditions.Count; i++)
      {
        if (conditions[i] != null)
          list.Add(conditions[i].Clone());
      }
      return list;
    }

    // ── 자주 쓰는 조건 절의 생성 도우미 ─────────────────────────────────────

    public static ScenarioCondition PlayerTag(string tag)
      => new ScenarioCondition { Type = ScenarioConditionType.PlayerHasTag, Tag = tag };

    public static ScenarioCondition QuestFlag(string flag)
      => new ScenarioCondition { Type = ScenarioConditionType.PlayerHasQuestFlag, Flag = flag };

    public static ScenarioCondition Quest(string questIdentifier, ScenarioQuestConditionState state = ScenarioQuestConditionState.Active, string completionCriteriaIdentifier = null)
      => new ScenarioCondition
      {
        Type = ScenarioConditionType.PlayerHasQuest,
        QuestIdentifier = questIdentifier,
        QuestState = state,
        CompletionCriteriaIdentifier = completionCriteriaIdentifier
      };

    public static ScenarioCondition Item(string itemIdentifier, int count = 1)
      => new ScenarioCondition { Type = ScenarioConditionType.PlayerHasItem, ItemIdentifier = itemIdentifier, Count = count };

    public static ScenarioCondition Raised(string signal, bool negate = false)
      => new ScenarioCondition { Type = ScenarioConditionType.SignalRaised, Signal = signal, Negate = negate };

    public static ScenarioCondition State(ScenarioEntityReference entity, string key, string value = null, string qualifier = null,
      ScenarioConditionCompare compare = ScenarioConditionCompare.Equal)
      => new ScenarioCondition
      {
        Type = ScenarioConditionType.EntityState,
        Entity = entity,
        Key = key,
        Qualifier = qualifier,
        Value = value,
        Compare = compare
      };

    public static ScenarioCondition PlayerStateIs(string key, string value = null, string qualifier = null,
      ScenarioConditionCompare compare = ScenarioConditionCompare.Equal)
      => new ScenarioCondition
      {
        Type = ScenarioConditionType.PlayerState,
        Key = key,
        Qualifier = qualifier,
        Value = value,
        Compare = compare
      };

    public static ScenarioCondition Active(string scenarioIdentifier, bool negate = false)
      => new ScenarioCondition { Type = ScenarioConditionType.ScenarioActive, ScenarioIdentifier = scenarioIdentifier, Negate = negate };

    public static ScenarioCondition AnyOf(params ScenarioCondition[] conditions)
      => new ScenarioCondition { Type = ScenarioConditionType.Group, MatchMode = ScenarioConditionMatchMode.Any, Conditions = new List<ScenarioCondition>(conditions) };

    public static ScenarioCondition AllOf(params ScenarioCondition[] conditions)
      => new ScenarioCondition { Type = ScenarioConditionType.Group, MatchMode = ScenarioConditionMatchMode.All, Conditions = new List<ScenarioCondition>(conditions) };
  }
}
