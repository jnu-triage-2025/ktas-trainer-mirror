using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 공동 진행 게이트의 서버측 집계기.
  ///
  /// <para>
  /// waitMode: All 병렬 노드가 서로 다른 두 명 이상에게 분기를 배정하면, 한 사람이 자기 분기를
  /// 끝내도 나머지가 끝날 때까지 다음 노드로 넘어가지 못한다. 이 집계기는 분기마다 어떤 퀘스트를
  /// 발행했고(회수한 것은 제외) 분기가 끝났는지를 기록해, 변화가 있을 때마다
  /// <see cref="ScenarioGroupGateSnapshot"/> 을 <see cref="Changed"/> 로 내보낸다. 컨트롤러는
  /// 그 스냅샷을 로컬 상태에 반영하고 표시 클라이언트에 중계한다.
  /// </para>
  /// </summary>
  public sealed class ScenarioGroupGateTracker
  {
    /// <summary>분기 하나의 참여 기록. 한 담당자가 분기를 여럿 맡으면 기록도 여럿이다.</summary>
    public sealed class Participant
    {
      private readonly ScenarioGroupGateTracker _owner;
      private readonly List<string> _questIds = new();

      internal Participant(ScenarioGroupGateTracker owner, int clientId, string branchIdentifier, string role)
      {
        _owner = owner;
        ClientId = clientId;
        BranchIdentifier = branchIdentifier ?? string.Empty;
        Role = role;
      }

      public int ClientId { get; }
      public string BranchIdentifier { get; }
      public string Role { get; }
      public bool Completed { get; private set; }
      public bool Left { get; private set; }
      public IReadOnlyList<string> QuestIds => _questIds;

      /// <summary>분기 안의 QuestControl 노드가 실행될 때 호출해 발행/회수된 퀘스트를 기록한다.</summary>
      public void RecordQuestOperation(ScenarioQuestOperationType operation, string questId)
      {
        if (string.IsNullOrWhiteSpace(questId))
          return;

        bool changed;
        switch (operation)
        {
          case ScenarioQuestOperationType.Add:
          case ScenarioQuestOperationType.Update:
            changed = !_questIds.Contains(questId, StringComparer.Ordinal);
            if (changed)
              _questIds.Add(questId);
            break;
          case ScenarioQuestOperationType.Remove:
            changed = _questIds.Remove(questId);
            break;
          default:
            changed = false;
            break;
        }

        if (changed)
          _owner.NotifyChanged();
      }

      /// <summary>분기 체인이 끝났을 때 호출한다. 이탈로 취소된 분기는 <paramref name="left"/> 로 표시한다.</summary>
      public void MarkCompleted(bool left)
      {
        if (Completed)
          return;

        Completed = true;
        Left = left;
        _owner.NotifyChanged();
      }
    }

    private readonly Dictionary<ScenarioParallelBranch, Participant> _participantsByBranch = new();
    private readonly List<Participant> _participants = new();

    private ScenarioGroupGateTracker(string graphIdentifier, string parallelNodeIdentifier)
    {
      GraphIdentifier = graphIdentifier;
      ParallelNodeIdentifier = parallelNodeIdentifier;
    }

    public string GraphIdentifier { get; }
    public string ParallelNodeIdentifier { get; }
    public bool IsClosed { get; private set; }
    public IReadOnlyList<Participant> Participants => _participants;

    /// <summary>스냅샷이 바뀔 때마다 호출된다. 닫힘 스냅샷(<see cref="ScenarioGroupGateSnapshot.Active"/> = false)도 여기로 나간다.</summary>
    public Action<ScenarioGroupGateSnapshot> Changed { get; set; }

    /// <summary>클라이언트 식별자로 표시 이름을 찾는다. null 이나 빈 값을 돌려주면 역할, 그다음 클라이언트 번호로 대체한다.</summary>
    public Func<int, string> DisplayNameResolver { get; set; }

    /// <summary>
    /// 배정표가 서로 다른 두 명 이상을 포함하는 waitMode: All 병렬 노드에 대해서만 집계기를 만든다.
    /// 그 외의 병렬 노드는 한 사람이 끝내면 바로 넘어가므로 기다림이 생기지 않는다.
    /// </summary>
    public static ScenarioGroupGateTracker TryCreate(
      string graphIdentifier,
      ScenarioParallelNode node,
      IReadOnlyDictionary<ScenarioParallelBranch, int?> allocation)
    {
      if (node == null
          || allocation == null
          || node.WaitMode != ScenarioWaitMode.All
          || string.IsNullOrWhiteSpace(graphIdentifier)
          || string.IsNullOrWhiteSpace(node.Identifier))
        return null;

      int distinctClients = allocation
        .Where(pair => pair.Key != null && pair.Value.HasValue)
        .Select(pair => pair.Value.Value)
        .Distinct()
        .Count();
      if (distinctClients < 2)
        return null;

      var tracker = new ScenarioGroupGateTracker(graphIdentifier, node.Identifier);
      var branches = node.Branches ?? Array.Empty<ScenarioParallelBranch>();
      for (int i = 0; i < branches.Count; i++)
      {
        var branch = branches[i];
        if (branch == null || !allocation.TryGetValue(branch, out var clientId) || !clientId.HasValue)
          continue;

        var participant = new Participant(tracker, clientId.Value, branch.Identifier, ResolveRole(branch));
        tracker._participantsByBranch[branch] = participant;
        tracker._participants.Add(participant);
      }

      return tracker;
    }

    public Participant GetParticipant(ScenarioParallelBranch branch)
      => branch != null && _participantsByBranch.TryGetValue(branch, out var participant) ? participant : null;

    /// <summary>현재 상태를 한 번 내보낸다. 병렬 노드가 분기를 시작하기 직전에 참여자 목록을 알리는 용도.</summary>
    public void Publish() => NotifyChanged();

    /// <summary>병렬 노드가 끝났을 때 호출한다. 닫힘 스냅샷을 내보내고 이후 변경은 무시한다.</summary>
    public void Close()
    {
      if (IsClosed)
        return;

      IsClosed = true;
      var snapshot = BuildSnapshot();
      snapshot.Active = false;
      Changed?.Invoke(snapshot);
    }

    public ScenarioGroupGateSnapshot BuildSnapshot()
    {
      var snapshot = new ScenarioGroupGateSnapshot
      {
        GraphIdentifier = GraphIdentifier,
        ParallelNodeIdentifier = ParallelNodeIdentifier,
        Active = !IsClosed
      };

      // 같은 담당자가 분기를 여럿 맡으면 한 참여자로 합친다. 모든 분기가 끝나야 완료로 본다.
      // GroupBy 는 첫 등장 순서를 유지하므로 그래프의 분기 순서가 참여자 순서가 된다.
      foreach (var group in _participants.GroupBy(participant => participant.ClientId))
      {
        var members = group.ToList();
        snapshot.Participants.Add(new ScenarioGroupGateParticipantSnapshot
        {
          ClientId = group.Key,
          DisplayName = ResolveDisplayName(group.Key, members),
          Role = members.Select(member => member.Role).FirstOrDefault(role => !string.IsNullOrWhiteSpace(role)),
          Completed = members.All(member => member.Completed),
          Left = members.Any(member => member.Left),
          QuestIds = members.SelectMany(member => member.QuestIds).Distinct(StringComparer.Ordinal).ToList()
        });
      }

      return snapshot;
    }

    private static string ResolveRole(ScenarioParallelBranch branch)
      => branch?.RequiredPlayerTags?.FirstOrDefault(tag => !string.IsNullOrWhiteSpace(tag))?.Trim();

    private string ResolveDisplayName(int clientId, List<Participant> members)
    {
      string name = null;
      try
      {
        name = DisplayNameResolver?.Invoke(clientId);
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }

      if (!string.IsNullOrWhiteSpace(name))
        return name.Trim();

      var role = members.Select(member => member.Role).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
      return string.IsNullOrWhiteSpace(role) ? $"플레이어 {clientId}" : role;
    }

    private void NotifyChanged()
    {
      if (IsClosed)
        return;

      Changed?.Invoke(BuildSnapshot());
    }
  }
}
