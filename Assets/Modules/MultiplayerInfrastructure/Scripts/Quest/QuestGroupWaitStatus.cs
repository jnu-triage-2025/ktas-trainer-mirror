using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// 여러 참여자가 각자의 몫을 끝내야 다음 단계로 넘어가는 구간에서, 한 퀘스트가 놓인 공동 진행 상태.
  ///
  /// <para>
  /// 서버는 waitMode: All 병렬 노드에서 서로 다른 참여자에게 배정된 분기의 완료를 집계해
  /// <see cref="Scenario.ScenarioGroupGateSnapshot"/> 으로 내려 주고,
  /// <see cref="Scenario.ScenarioGroupGateState"/> 가 그것을 로컬 참여자 기준으로 이 값으로 정리한다.
  /// 퀘스트 HUD 와 저널은 이 값만 보고 "다른 플레이어가 완료할 때까지 기다리기(n/N)" 문구와
  /// 참여자 목록을 그린다.
  /// </para>
  /// </summary>
  public sealed class QuestGroupWaitStatus
  {
    public const string WaitingDisplayTextFormat = "다른 플레이어가 완료할 때까지 기다리기({0}/{1})";
    public const string PlaceholderQuestIdPrefix = "group-wait::";

    public QuestGroupWaitStatus(
      string graphIdentifier,
      string gateIdentifier,
      IReadOnlyList<QuestGroupWaitParticipant> participants,
      IReadOnlyList<string> localQuestIds)
    {
      GraphIdentifier = graphIdentifier ?? string.Empty;
      GateIdentifier = gateIdentifier ?? string.Empty;
      Participants = participants ?? Array.Empty<QuestGroupWaitParticipant>();
      LocalQuestIds = localQuestIds ?? Array.Empty<string>();

      int completed = 0;
      bool hasLocal = false;
      bool localCompleted = false;
      for (int i = 0; i < Participants.Count; i++)
      {
        var participant = Participants[i];
        if (participant == null)
          continue;

        if (participant.CountsAsCompleted)
          completed++;

        if (participant.IsLocal)
        {
          hasLocal = true;
          localCompleted |= participant.CountsAsCompleted;
        }
      }

      CompletedCount = completed;
      HasLocalParticipant = hasLocal;
      LocalParticipantCompleted = localCompleted;
    }

    /// <summary>이 게이트를 실행하는 시나리오 그래프 식별자.</summary>
    public string GraphIdentifier { get; }

    /// <summary>게이트 식별자(그래프 식별자와 병렬 노드 식별자의 조합).</summary>
    public string GateIdentifier { get; }

    /// <summary>함께 완료해야 하는 참여자 목록. 서버가 배정한 분기 순서를 유지한다.</summary>
    public IReadOnlyList<QuestGroupWaitParticipant> Participants { get; }

    /// <summary>로컬 참여자의 분기가 발행하고 아직 회수하지 않은 퀘스트 식별자.</summary>
    public IReadOnlyList<string> LocalQuestIds { get; }

    /// <summary>자기 몫을 끝낸(또는 이탈해 완료로 간주되는) 참여자 수.</summary>
    public int CompletedCount { get; }

    public int TotalCount => Participants.Count;

    public bool HasLocalParticipant { get; }

    /// <summary>로컬 참여자가 자기 몫을 끝냈는지 여부.</summary>
    public bool LocalParticipantCompleted { get; }

    public bool AllCompleted => TotalCount == 0 || CompletedCount >= TotalCount;

    /// <summary>나는 끝냈지만 아직 끝내지 않은 참여자가 남아 있는 상태.</summary>
    public bool IsWaitingForOthers => LocalParticipantCompleted && !AllCompleted;

    public string WaitingDisplayText => string.Format(WaitingDisplayTextFormat, CompletedCount, TotalCount);

    /// <summary>
    /// 분기가 발행한 퀘스트가 모두 회수된 뒤에도 대기 상태를 보여 주기 위해 QuestManager 가
    /// 합성하는 자리 표시 퀘스트의 식별자.
    /// </summary>
    public string PlaceholderQuestId => PlaceholderQuestIdPrefix + GateIdentifier;

    public static bool IsPlaceholderQuestId(string questId)
      => !string.IsNullOrEmpty(questId) && questId.StartsWith(PlaceholderQuestIdPrefix, StringComparison.Ordinal);
  }

  /// <summary>공동 진행 게이트의 참여자 한 명.</summary>
  public sealed class QuestGroupWaitParticipant
  {
    public int ClientId { get; set; }
    public string DisplayName { get; set; }
    public string Role { get; set; }
    public bool Completed { get; set; }

    /// <summary>접속이 끊겨 분기가 취소된 참여자. 게이트는 이 분기를 완료한 것으로 본다.</summary>
    public bool Left { get; set; }

    public bool IsLocal { get; set; }

    public bool CountsAsCompleted => Completed || Left;
  }
}
