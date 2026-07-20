using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Entity;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <c>EntityStateSignalBinding</c> 노드가 등록한 "엔티티 상태 이벤트 → 시나리오 신호" 바인딩을 추적·정리한다.
  ///
  /// <para>
  /// 실제 리스너 등록은 대상 엔티티의 <see cref="IScenarioEntityStateEventSource"/> 에 이루어지며,
  /// 이 레지스트리는 각 바인딩의 콜백까지 보관하여 시나리오 종료 시 또는 동일 식별자 재등록 시
  /// 정확히 재구성한다. 소스는 이벤트명 단위 해제만 제공하므로, 특정 바인딩을 제거할 때는
  /// 해당 (소스, 이벤트) 리스너를 모두 지운 뒤 남은 바인딩을 다시 등록한다.
  /// </para>
  ///
  /// <para>
  /// 콜백에서 <see cref="ScenarioInteractionSignals.Raise"/> 를 호출한다. 신호 발신 자체가 서버 권위
  /// 라우팅이므로, 이벤트가 서버(호스트) 컨텍스트에서 발생하는 한 모든 피어에 일관되게 전파된다.
  /// </para>
  /// </summary>
  public static class ScenarioEntityStateSignalBindings
  {
    private sealed class Binding
    {
      public string Identifier;
      public IScenarioEntityStateEventSource Source;
      public string EventName;
      public string EventKey;
      public string OutputSignal;
      public bool ConsumeOnce;
      public bool Consumed;
    }

    private static readonly Dictionary<string, Binding> Bindings = new(StringComparer.Ordinal);

    /// <summary>
    /// 바인딩을 등록한다. 동일 <paramref name="bindingIdentifier"/> 가 이미 있으면 먼저 해제 후 교체한다.
    /// </summary>
    /// <returns>대상 소스가 이벤트를 인식하여 등록에 성공하면 true.</returns>
    public static bool Register(
      string bindingIdentifier,
      IScenarioEntityStateEventSource source,
      string eventName,
      string eventKey,
      string outputSignalIdentifier,
      bool consumeOnce)
    {
      if (string.IsNullOrWhiteSpace(bindingIdentifier)
          || source == null
          || string.IsNullOrWhiteSpace(eventName)
          || string.IsNullOrWhiteSpace(outputSignalIdentifier))
        return false;

      string id = bindingIdentifier.Trim();

      // 동일 식별자 재등록: 기존 바인딩을 먼저 해제한다.
      Unregister(id);

      var binding = new Binding
      {
        Identifier = id,
        Source = source,
        EventName = eventName,
        EventKey = string.IsNullOrWhiteSpace(eventKey) ? null : eventKey,
        OutputSignal = ScenarioInteractionSignals.Normalize(outputSignalIdentifier),
        ConsumeOnce = consumeOnce,
        Consumed = false,
      };

      if (!AttachToSource(binding))
        return false;

      Bindings[id] = binding;
      return true;
    }

    /// <summary>바인딩을 해제한다. 같은 (소스, 이벤트)의 남은 바인딩은 재등록하여 유지한다.</summary>
    public static bool Unregister(string bindingIdentifier)
    {
      if (string.IsNullOrWhiteSpace(bindingIdentifier))
        return false;

      string id = bindingIdentifier.Trim();
      if (!Bindings.TryGetValue(id, out var removed))
        return false;

      Bindings.Remove(id);
      ReattachSourceEvent(removed.Source, removed.EventName);
      return true;
    }

    /// <summary>시나리오 시작/종료 시 모든 바인딩을 정리한다.</summary>
    public static void ClearAll()
    {
      // 소스별 이벤트 리스너를 정리한다(중복 호출은 무해).
      var seen = new HashSet<(IScenarioEntityStateEventSource, string)>();
      foreach (var binding in Bindings.Values)
      {
        if (binding.Source != null && seen.Add((binding.Source, binding.EventName)))
          binding.Source.UnregisterStateEventListeners(binding.EventName);
      }

      Bindings.Clear();
    }

    /// <summary>단일 바인딩의 콜백을 대상 소스에 등록한다.</summary>
    private static bool AttachToSource(Binding binding)
    {
      Action<string> callback = firedKey =>
      {
        if (binding.ConsumeOnce && binding.Consumed)
          return;

        if (binding.ConsumeOnce)
        {
          binding.Consumed = true;
          // 1회성: 발신 전에 바인딩을 제거하여 재진입/중복 발신을 방지한다.
          // (Unregister 가 같은 소스/이벤트의 남은 바인딩을 재구성한다.)
          Unregister(binding.Identifier);
        }

        ScenarioInteractionSignals.Raise(binding.OutputSignal);
      };

      return binding.Source.RegisterStateEventListener(binding.EventName, binding.EventKey, callback);
    }

    /// <summary>
    /// 특정 (소스, 이벤트)의 리스너를 모두 지운 뒤, 레지스트리에 남아 있는 동일 (소스, 이벤트)
    /// 바인딩을 다시 등록한다. 소스가 이벤트명 단위 해제만 지원하므로 필요한 재구성이다.
    /// </summary>
    private static void ReattachSourceEvent(IScenarioEntityStateEventSource source, string eventName)
    {
      if (source == null || string.IsNullOrWhiteSpace(eventName))
        return;

      source.UnregisterStateEventListeners(eventName);

      foreach (var binding in Bindings.Values)
      {
        if (ReferenceEquals(binding.Source, source)
            && string.Equals(binding.EventName, eventName, StringComparison.Ordinal))
        {
          AttachToSource(binding);
        }
      }
    }
  }
}
