using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <c>SignalCounter</c> 노드가 등록한 카운터를 관리한다. 접두사로 시작하는 서로 다른(distinct)
  /// 시나리오 신호의 개수를 세어, 임계치에 도달하면 출력 신호를 발신한다.
  ///
  /// <para>
  /// 시나리오 신호는 sticky 이며 <c>OnSignalRegistered</c> 는 각 신호의 최초 등록 시 1회만 발생한다.
  /// 따라서 "같은 신호 N번" 이 아니라 "접두사 매칭 신호의 distinct 개수" 를 센다. 등록 시점에 이미
  /// 올라와 있던 매칭 신호도 초기 카운트에 포함한다.
  /// </para>
  ///
  /// <para>
  /// 임계치 도달 시 출력 신호를 <c>Raise</c> 하고 해당 카운터를 자동 제거(1회성)한다.
  /// 재진입/중복 발신 방지를 위해 <see cref="ScenarioConditionalSignalListeners"/> 와 동일한 큐 기반
  /// 디스패치 방어를 사용한다.
  /// </para>
  /// </summary>
  public static class ScenarioSignalCounters
  {
    private sealed class Counter
    {
      public string Identifier;
      public string Prefix;        // 정규화된 접두사
      public int Threshold;
      public string Output;        // 정규화된 출력 신호
      public readonly HashSet<string> Matched = new(StringComparer.Ordinal);
      public Func<IReadOnlyCollection<string>> ExpectedSignals;
    }

    private static readonly Dictionary<string, Counter> Counters = new(StringComparer.Ordinal);

    // 재진입/호스트 이중 전달 방어(ScenarioConditionalSignalListeners 와 동일 전략).
    private static readonly Queue<string> PendingSignals = new();
    private static bool _isDispatching;

    static ScenarioSignalCounters()
    {
      ScenarioInteractionSignals.OnSignalRegistered += HandleSignal;
      ScenarioInteractionSignals.OnSignalCleared += HandleSignalCleared;
    }

    /// <summary>카운터를 등록한다. 동일 식별자 재등록은 교체한다. 등록 즉시 임계치를 충족하면 바로 발신한다.</summary>
    public static bool Register(string identifier, string sourcePrefix, int threshold, string output)
      => Register(identifier, sourcePrefix, threshold, output, null);

    public static bool Register(
      string identifier,
      string sourcePrefix,
      int threshold,
      string output,
      Func<IReadOnlyCollection<string>> expectedSignals)
    {
      if (string.IsNullOrWhiteSpace(identifier)
          || string.IsNullOrWhiteSpace(sourcePrefix)
          || string.IsNullOrWhiteSpace(output))
        return false;

      int effectiveThreshold = threshold < 1 ? 1 : threshold;
      string id = identifier.Trim();

      var counter = new Counter
      {
        Identifier = id,
        Prefix = ScenarioInteractionSignals.Normalize(sourcePrefix),
        Threshold = effectiveThreshold,
        Output = ScenarioInteractionSignals.Normalize(output),
        ExpectedSignals = expectedSignals,
      };

      // 이미 올라와 있는 매칭 신호를 초기 카운트에 포함한다.
      foreach (var raised in ScenarioInteractionSignals.GetRaisedSignalsWithPrefix(counter.Prefix))
        counter.Matched.Add(raised);

      Counters[id] = counter;

      // 등록 즉시 임계치 충족 시 발신(디스패치 루프를 통해 재진입 안전하게).
      if (counter.Matched.Count >= counter.Threshold)
        EnqueueImmediateFire(counter);

      return true;
    }

    public static bool Unregister(string identifier)
      => !string.IsNullOrWhiteSpace(identifier) && Counters.Remove(identifier.Trim());

    public static void ClearAll()
    {
      Counters.Clear();
      PendingSignals.Clear();
      _isDispatching = false;
    }

    /// <summary>플레이어가 연결을 끊으면 기대 신호 집합이 바뀔 수 있는 카운터를 다시 평가한다.</summary>
    public static void RefreshDynamicThresholds()
    {
      // 고정 임계치 카운터는 신호가 도착할 때만 상태가 바뀌므로 Dispatch 가 이미 처리한다.
      // 로스터 기반 카운터만 재평가 대상이며, 그런 카운터가 없으면 스냅샷도 뜨지 않는다.
      // (이 메서드는 접속 상태 변화 시점과 저빈도 안전망에서 호출된다.)
      bool hasDynamicCounter = false;
      foreach (var counter in Counters.Values)
      {
        if (counter.ExpectedSignals == null)
          continue;
        hasDynamicCounter = true;
        break;
      }

      if (!hasDynamicCounter)
        return;

      foreach (var counter in Counters.Values.ToArray())
      {
        if (counter.ExpectedSignals != null)
          FireIfReady(counter);
      }
    }

    private static void EnqueueImmediateFire(Counter counter)
    {
      // 등록 즉시 임계치를 충족한 카운터를 발신한다. 진행 중(_isDispatching)이면 현재 디스패치
      // 루프가 곧 처리하므로 아무 것도 하지 않는다(FireIfReady 는 Dispatch 경로에서 재평가됨).
      if (_isDispatching)
        return;

      _isDispatching = true;
      try
      {
        FireIfReady(counter);

        // FireIfReady 의 Raise 가 OnSignalRegistered → HandleSignal 재진입을 유발하지만
        // _isDispatching 가드로 큐에만 쌓인다. 여기서 큐를 끝까지 배수하여 후속 카운터/체이닝을
        // 정상 처리한다.
        while (PendingSignals.Count > 0)
          Dispatch(PendingSignals.Dequeue());
      }
      finally
      {
        _isDispatching = false;
      }
    }

    private static void HandleSignal(string signal)
    {
      if (string.IsNullOrWhiteSpace(signal))
        return;

      if (PendingSignals.Contains(signal))
        return;
      PendingSignals.Enqueue(signal);
      if (_isDispatching)
        return;

      _isDispatching = true;
      try
      {
        while (PendingSignals.Count > 0)
          Dispatch(PendingSignals.Dequeue());
      }
      finally
      {
        _isDispatching = false;
      }
    }

    /// <summary>
    /// 내려간 신호를 누적 집계에서 제거한다.
    /// Matched 는 "현재 올라가 있는 매칭 신호"를 뜻하므로, 신호가 내려갔는데도 남겨 두면
    /// 더 이상 참이 아닌 조건으로 임계치를 채워 출력 신호를 잘못 발신하게 된다.
    /// </summary>
    private static void HandleSignalCleared(string signal)
    {
      if (string.IsNullOrWhiteSpace(signal) || Counters.Count == 0)
        return;

      foreach (var counter in Counters.Values)
        counter.Matched.Remove(signal);
    }

    private static void Dispatch(string signal)
    {
      // 접두사 매칭 카운터에 신호를 반영하고, 임계치 도달 카운터를 발신한다.
      // 순회 중 Counters 가 변경될 수 있으므로 스냅샷 키로 순회한다.
      var keys = new List<string>(Counters.Keys);
      foreach (var key in keys)
      {
        if (!Counters.TryGetValue(key, out var counter))
          continue;
        if (!signal.StartsWith(counter.Prefix, StringComparison.Ordinal))
          continue;

        counter.Matched.Add(signal);
        FireIfReady(counter);
      }
    }

    private static void FireIfReady(Counter counter)
    {
      // 카운터는 각 피어의 RuntimeState 변경을 관찰하지만, 완료 신호는 서버만 계산하여
      // 발신해야 한다. 클라이언트가 이를 RaiseAuthoritative 경로로 되돌려 보내면 서버 전용
      // 출력으로 거부되고, 클라이언트 측 게이트도 다음 단계로 진행하지 못한다.
      if (!ScenarioNetworkRelay.CanEmitServerOwnedSignalOutput())
        return;

      if (counter.ExpectedSignals != null)
      {
        var expected = counter.ExpectedSignals() ?? Array.Empty<string>();
        if (expected.Count == 0 || expected.Any(signal => !counter.Matched.Contains(signal)))
          return;
      }
      else if (counter.Matched.Count < counter.Threshold)
        return;

      // 1회성: 발신 전에 제거하여 재진입/중복 발신을 방지한다.
      Counters.Remove(counter.Identifier);
      ScenarioInteractionSignals.Raise(counter.Output);
    }
  }
}
