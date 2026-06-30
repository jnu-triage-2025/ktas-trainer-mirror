using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 서버 권위 내부 신호를 관리하는 FIFO 레지스트리.
  ///
  /// register/resolve 어느 쪽이 먼저 오더라도 나중에 들어온 반대쪽과 매칭된다.
  /// targetId 는 `@s`(self) 또는 `@m`(server) 같은 서버 내부 대상 식별자와
  /// 일반 플레이어 식별자 모두를 허용한다.
  /// </summary>
  public static class ScenarioServerInternalSignalRegistry
  {
    public const string SelfTarget = "@s";
    public const string ServerTarget = "@m";

    private static readonly object Sync = new object();
    private static readonly Dictionary<string, Queue<Action>> WaitingResolvers = new Dictionary<string, Queue<Action>>(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> PendingSignals = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    /// 대상과 신호 식별자를 정규화한다. target 이 비어 있으면 서버 대상(@m)으로 본다.
    /// </summary>
    public static string NormalizeTarget(string targetId)
    {
      if (string.IsNullOrWhiteSpace(targetId))
      {
        return ServerTarget;
      }

      return targetId.Trim();
    }

    /// <summary>
    /// 수신 측이 신호 대기를 등록한다. 이미 같은 신호가 resolve 된 상태라면 즉시 콜백을 실행한다.
    /// </summary>
    public static bool Register(string targetId, string signalId, Action onResolved)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return false;
      }

      var key = BuildKey(targetId, signalId);

      lock (Sync)
      {
        if (TryConsumePendingSignalLocked(key))
        {
          return true;
        }

        if (!WaitingResolvers.TryGetValue(key, out var waiters))
        {
          waiters = new Queue<Action>();
          WaitingResolvers[key] = waiters;
        }

        waiters.Enqueue(onResolved);
        return false;
      }
    }

    /// <summary>
    /// 발신 측이 신호를 resolve 한다. 이미 같은 신호를 기다리는 수신자가 있으면 즉시 콜백을 실행한다.
    /// </summary>
    public static bool Resolve(string targetId, string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return false;
      }

      var key = BuildKey(targetId, signalId);

      lock (Sync)
      {
        if (TryConsumeWaitingResolverLocked(key, out var resolver))
        {
          resolver?.Invoke();
          return true;
        }

        if (!PendingSignals.TryGetValue(key, out var count))
        {
          count = 0;
        }

        PendingSignals[key] = count + 1;
        return false;
      }
    }

    /// <summary>특정 대상/신호 쌍을 큐에서 제거한다.</summary>
    public static void Clear(string targetId, string signalId)
    {
      if (string.IsNullOrWhiteSpace(signalId))
      {
        return;
      }

      var key = BuildKey(targetId, signalId);

      lock (Sync)
      {
        WaitingResolvers.Remove(key);
        PendingSignals.Remove(key);
      }
    }

    /// <summary>모든 내부 신호 상태를 초기화한다.</summary>
    public static void ClearAll()
    {
      lock (Sync)
      {
        WaitingResolvers.Clear();
        PendingSignals.Clear();
      }
    }

    private static string BuildKey(string targetId, string signalId)
      => $"{NormalizeTarget(targetId)}::{signalId.Trim()}";

    private static bool TryConsumePendingSignalLocked(string key)
    {
      if (!PendingSignals.TryGetValue(key, out var count) || count <= 0)
      {
        return false;
      }

      if (count == 1)
      {
        PendingSignals.Remove(key);
      }
      else
      {
        PendingSignals[key] = count - 1;
      }

      return true;
    }

    private static bool TryConsumeWaitingResolverLocked(string key, out Action resolver)
    {
      resolver = null;

      if (!WaitingResolvers.TryGetValue(key, out var waiters) || waiters.Count == 0)
      {
        return false;
      }

      resolver = waiters.Dequeue();
      if (waiters.Count == 0)
      {
        WaitingResolvers.Remove(key);
      }

      return true;
    }
  }
}