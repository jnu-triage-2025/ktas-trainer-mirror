using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FishNet;
using MultiplayerInfrastructure.Quest;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 서버가 집계해 내려 주는 공동 진행 게이트(여러 참여자가 모두 끝내야 넘어가는 waitMode: All
  /// 병렬 노드)의 스냅샷. <see cref="Active"/> 가 false 이면 게이트가 열려 대기가 끝났다는 뜻이다.
  /// </summary>
  [Serializable]
  public sealed class ScenarioGroupGateSnapshot
  {
    [JsonPropertyName("graph")]
    public string GraphIdentifier { get; set; }

    [JsonPropertyName("parallel")]
    public string ParallelNodeIdentifier { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;

    [JsonPropertyName("participants")]
    public List<ScenarioGroupGateParticipantSnapshot> Participants { get; set; } = new();

    [JsonIgnore]
    public string GateIdentifier => (GraphIdentifier ?? string.Empty) + "/" + (ParallelNodeIdentifier ?? string.Empty);
  }

  [Serializable]
  public sealed class ScenarioGroupGateParticipantSnapshot
  {
    [JsonPropertyName("clientId")]
    public int ClientId { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("left")]
    public bool Left { get; set; }

    /// <summary>이 참여자의 분기가 발행하고 아직 회수하지 않은 퀘스트 식별자.</summary>
    [JsonPropertyName("questIds")]
    public List<string> QuestIds { get; set; } = new();
  }

  /// <summary>
  /// 서버가 RPC 로 전달한 공동 진행 게이트 스냅샷의 클라이언트측 읽기 모델.
  ///
  /// <para>
  /// <see cref="ScenarioParallelAssignmentState"/> 와 같은 위치에 있는 복제 결과이며 그래프 실행 권위는
  /// 갖지 않는다. QuestManager 는 이 상태를 읽어 로컬 참여자의 퀘스트에
  /// <see cref="QuestGroupWaitStatus"/> 를 붙이고, 분기 퀘스트가 모두 회수된 뒤에도 대기 중이면
  /// 자리 표시 퀘스트를 합성한다.
  /// </para>
  /// </summary>
  public static class ScenarioGroupGateState
  {
    private static readonly Dictionary<string, ScenarioGroupGateSnapshot> GatesByKey = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>게이트가 추가·갱신·제거될 때마다 발생한다.</summary>
    public static event Action Changed;

    /// <summary>
    /// 로컬 클라이언트 식별자를 얻는 방법. 기본값은 FishNet 연결에서 읽고, 테스트에서는 바꿔 끼운다.
    /// </summary>
    public static Func<int?> LocalClientIdProvider { get; set; } = ResolveLocalClientIdFromNetwork;

    public static void ResetLocalClientIdProvider() => LocalClientIdProvider = ResolveLocalClientIdFromNetwork;

    public static IReadOnlyCollection<ScenarioGroupGateSnapshot> ActiveGates => GatesByKey.Values;

    public static void Apply(ScenarioGroupGateSnapshot snapshot)
    {
      if (snapshot == null
          || string.IsNullOrWhiteSpace(snapshot.GraphIdentifier)
          || string.IsNullOrWhiteSpace(snapshot.ParallelNodeIdentifier))
        return;

      string key = CreateKey(snapshot.GraphIdentifier, snapshot.ParallelNodeIdentifier);
      if (snapshot.Active)
      {
        snapshot.Participants ??= new List<ScenarioGroupGateParticipantSnapshot>();
        GatesByKey[key] = snapshot;
      }
      else if (!GatesByKey.Remove(key))
      {
        return;
      }

      Changed?.Invoke();
    }

    public static bool TryApplyJson(string snapshotJson)
    {
      if (string.IsNullOrWhiteSpace(snapshotJson))
        return false;

      try
      {
        var snapshot = JsonSerializer.Deserialize<ScenarioGroupGateSnapshot>(snapshotJson, JsonOptions);
        if (snapshot == null)
          return false;

        Apply(snapshot);
        return true;
      }
      catch (JsonException ex)
      {
        Debug.LogWarning($"[ScenarioGroupGateState] Ignored malformed group gate snapshot: {ex.Message}");
        return false;
      }
    }

    public static string Serialize(ScenarioGroupGateSnapshot snapshot)
      => snapshot == null ? null : JsonSerializer.Serialize(snapshot, JsonOptions);

    public static bool TryGet(string graphIdentifier, string parallelNodeIdentifier, out ScenarioGroupGateSnapshot snapshot)
      => GatesByKey.TryGetValue(CreateKey(graphIdentifier, parallelNodeIdentifier), out snapshot);

    public static void ClearGraph(string graphIdentifier)
    {
      if (string.IsNullOrWhiteSpace(graphIdentifier))
        return;

      var prefix = graphIdentifier + "\n";
      var keys = GatesByKey.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
      if (keys.Length == 0)
        return;

      foreach (var key in keys)
        GatesByKey.Remove(key);

      Changed?.Invoke();
    }

    /// <summary>모든 게이트를 지운다. 세션 종료와 테스트 정리에서 사용한다. 구독자는 유지한다.</summary>
    public static void ClearAll()
    {
      if (GatesByKey.Count == 0)
        return;

      GatesByKey.Clear();
      Changed?.Invoke();
    }

    /// <summary>
    /// 로컬 참여자의 분기가 발행한 퀘스트에 대한 공동 진행 상태. 그런 게이트가 없으면 null.
    /// </summary>
    public static QuestGroupWaitStatus GetStatusForQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId) || GatesByKey.Count == 0)
        return null;

      int? localClientId = GetLocalClientId();
      if (!localClientId.HasValue)
        return null;

      foreach (var gate in GatesByKey.Values)
      {
        var local = FindParticipant(gate, localClientId.Value);
        if (local?.QuestIds != null && local.QuestIds.Contains(questId, StringComparer.Ordinal))
          return BuildStatus(gate, localClientId);
      }

      return null;
    }

    /// <summary>로컬 참여자가 자기 몫을 끝냈지만 다른 참여자를 아직 기다리는 게이트들.</summary>
    public static List<QuestGroupWaitStatus> GetLocalWaitingStatuses()
    {
      var result = new List<QuestGroupWaitStatus>();
      if (GatesByKey.Count == 0)
        return result;

      int? localClientId = GetLocalClientId();
      if (!localClientId.HasValue)
        return result;

      foreach (var gate in GatesByKey.Values)
      {
        var status = BuildStatus(gate, localClientId);
        if (status.IsWaitingForOthers)
          result.Add(status);
      }

      return result;
    }

    public static QuestGroupWaitStatus BuildStatus(ScenarioGroupGateSnapshot gate, int? localClientId)
    {
      var participants = new List<QuestGroupWaitParticipant>();
      List<string> localQuestIds = null;
      if (gate?.Participants != null)
      {
        for (int i = 0; i < gate.Participants.Count; i++)
        {
          var each = gate.Participants[i];
          if (each == null)
            continue;

          bool isLocal = localClientId.HasValue && each.ClientId == localClientId.Value;
          participants.Add(new QuestGroupWaitParticipant
          {
            ClientId = each.ClientId,
            DisplayName = each.DisplayName,
            Role = each.Role,
            Completed = each.Completed,
            Left = each.Left,
            IsLocal = isLocal
          });

          if (isLocal)
            localQuestIds = each.QuestIds != null ? new List<string>(each.QuestIds) : new List<string>();
        }
      }

      return new QuestGroupWaitStatus(gate?.GraphIdentifier, gate?.GateIdentifier, participants, localQuestIds);
    }

    private static ScenarioGroupGateParticipantSnapshot FindParticipant(ScenarioGroupGateSnapshot gate, int clientId)
    {
      if (gate?.Participants == null)
        return null;

      for (int i = 0; i < gate.Participants.Count; i++)
      {
        var each = gate.Participants[i];
        if (each != null && each.ClientId == clientId)
          return each;
      }

      return null;
    }

    private static int? GetLocalClientId()
    {
      try
      {
        return LocalClientIdProvider?.Invoke();
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
        return null;
      }
    }

    private static int? ResolveLocalClientIdFromNetwork()
    {
      // 미접속 상태의 Connection 은 null 이 아니라 EmptyConnection(ClientId -1) 이다.
      var connection = InstanceFinder.ClientManager?.Connection;
      if (connection == null || connection.ClientId < 0)
        return null;

      return connection.ClientId;
    }

    private static string CreateKey(string graphIdentifier, string parallelNodeIdentifier)
      => (graphIdentifier ?? string.Empty) + "\n" + (parallelNodeIdentifier ?? string.Empty);
  }
}
