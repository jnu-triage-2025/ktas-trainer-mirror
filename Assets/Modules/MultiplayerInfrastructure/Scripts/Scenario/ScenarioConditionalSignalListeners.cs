using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>게임플레이가 올린 신호를 관찰해, 선언된 전제 신호가 모두 있을 때만 후속 신호를 발생시킨다.</summary>
  public static class ScenarioConditionalSignalListeners
  {
    private sealed class Listener { public string Source; public string Output; public string[] Required; public bool ConsumeOnce; }
    private static readonly Dictionary<string, Listener> Listeners = new(StringComparer.Ordinal);

    // 재진입 방지: HandleSignal 내부의 Raise 가 동기적으로 OnSignalRegistered 를 다시 발생시켜
    // HandleSignal 이 재진입하면, 순환 리스너(output==source, ConsumeOnce=false)에서 스택 오버플로우가
    // 발생하고, 호스트에서는 서버 경로 + 미러 RPC 로 동일 신호가 두 번 도착해 중복 Raise 가 일어난다.
    // 진행 중에는 신호를 큐에 쌓고, 단일 디스패치 루프가 순차 처리하여 두 문제를 함께 방어한다.
    private static readonly Queue<string> PendingSignals = new();
    private static bool _isDispatching;

    static ScenarioConditionalSignalListeners() => ScenarioInteractionSignals.OnSignalRegistered += HandleSignal;

    public static void Register(string identifier, string source, string output, IEnumerable<string> required, bool consumeOnce)
    {
      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(output))
        return;
      Listeners[identifier.Trim()] = new Listener
      {
        Source = ScenarioInteractionSignals.Normalize(source),
        Output = ScenarioInteractionSignals.Normalize(output),
        Required = (required ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(ScenarioInteractionSignals.Normalize).Distinct(StringComparer.Ordinal).ToArray(),
        ConsumeOnce = consumeOnce
      };
    }
    public static bool Unregister(string identifier) => !string.IsNullOrWhiteSpace(identifier) && Listeners.Remove(identifier.Trim());

    public static void ClearAll()
    {
      Listeners.Clear();
      PendingSignals.Clear();
      _isDispatching = false;
    }

    private static void HandleSignal(string signal)
    {
      if (string.IsNullOrWhiteSpace(signal))
        return;

      // 호스트(서버=클라)에서는 서버 권위 기록 + 미러 ObserversRpc 로 동일 신호가 연달아
      // 두 번 도착한다. 한 디스패치 사이클 내에서 같은 신호가 이미 큐에 있으면 중복을 제거해,
      // ConsumeOnce=false 리스너의 중복 Raise 를 막는다. 서로 다른 output 체이닝은 그대로 유지된다.
      if (PendingSignals.Contains(signal))
        return;

      // 재진입 시 즉시 처리하지 않고 큐에 넣어, 최상위 디스패치 루프가 순차 처리한다.
      PendingSignals.Enqueue(signal);
      if (_isDispatching)
        return;

      _isDispatching = true;
      try
      {
        while (PendingSignals.Count > 0)
        {
          Dispatch(PendingSignals.Dequeue());
        }
      }
      finally
      {
        _isDispatching = false;
      }
    }

    private static void Dispatch(string signal)
    {
      // 매칭 스냅샷을 먼저 확보한다(순회 중 Listeners 가 Remove 로 변경될 수 있음).
      var matched = Listeners
        .Where(pair => pair.Value.Source == signal && pair.Value.Required.All(ScenarioInteractionSignals.IsRaised))
        .Select(pair => pair.Key)
        .ToArray();

      foreach (var key in matched)
      {
        // 재진입/중복 디스패치로 이미 제거되었을 수 있으므로 TryGetValue 로 방어한다.
        if (!Listeners.TryGetValue(key, out var listener))
          continue;

        // 리스너 출력은 그래프 엔진이 계산하는 서버 전용 신호다. 클라이언트는 서버가
        // 미러링한 출력만 받아야 하며, 여기서 다시 서버로 보고하면 권한 검사에서 거부된다.
        if (!ScenarioNetworkRelay.CanEmitServerOwnedSignalOutput())
          continue;

        if (listener.ConsumeOnce)
          Listeners.Remove(key);
        // Raise 는 OnSignalRegistered 를 동기 발생시키지만, _isDispatching 가드로 재진입이
        // 큐잉되므로 여기서는 스택이 깊어지지 않고 output 체이닝이 순차 처리된다.
        ScenarioInteractionSignals.Raise(listener.Output);
      }
    }
  }
}
