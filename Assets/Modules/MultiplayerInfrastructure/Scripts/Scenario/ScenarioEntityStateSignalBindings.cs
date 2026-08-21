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
          // 1회성: 발신 전에 소비 표시를 하고 소스에서 떼어 내 재진입/중복 발신을 막는다.
          // 레지스트리에서 지우지는 않는다. 신호가 나중에 Clear 되면 발신자가 하나도 남지
          // 않아 그 신호를 기다리는 게이트가 영구히 막히므로, 재무장할 수 있게 보관한다.
          // (ReattachSourceEvent 가 같은 소스/이벤트의 남은 바인딩을 재구성한다.)
          binding.Consumed = true;
          ReattachSourceEvent(binding.Source, binding.EventName);
        }

        ScenarioInteractionSignals.Raise(binding.OutputSignal);
      };

      return binding.Source.RegisterStateEventListener(binding.EventName, binding.EventKey, callback);
    }

    /// <summary>
    /// 지워진 신호를 출력으로 갖는 1회성 바인딩을 다시 무장한다.
    ///
    /// <para>
    /// 1회성 바인딩은 첫 발신 뒤 소스에서 떨어져 있다. 게임플레이가 그 신호를 내리면
    /// (예: 처치 단계를 다시 시작하며 이전 결과 신호를 지우는 경우) 그 신호를 올릴 주체가
    /// 하나도 남지 않아, 신호를 기다리는 Validator 게이트가 영구히 열리지 않는다.
    /// 신호가 내려간 시점에 다시 붙여 두어야 같은 처치를 다시 수행했을 때 게이트가 통과된다.
    /// </para>
    /// </summary>
    internal static void RearmConsumedBindingsForSignal(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId) || Bindings.Count == 0)
        return;

      List<Binding> rearmed = null;
      foreach (var binding in Bindings.Values)
      {
        if (!binding.Consumed
            || !string.Equals(binding.OutputSignal, normalizedSignalId, StringComparison.Ordinal))
          continue;

        binding.Consumed = false;
        (rearmed ??= new List<Binding>()).Add(binding);
      }

      if (rearmed == null)
        return;

      // 소스는 이벤트명 단위 해제만 제공하므로 (소스, 이벤트) 묶음마다 한 번씩 재구성한다.
      var reattached = new HashSet<(IScenarioEntityStateEventSource, string)>();
      foreach (var binding in rearmed)
      {
        if (binding.Source != null && reattached.Add((binding.Source, binding.EventName)))
          ReattachSourceEvent(binding.Source, binding.EventName);
      }
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
        if (!binding.Consumed
            && ReferenceEquals(binding.Source, source)
            && string.Equals(binding.EventName, eventName, StringComparison.Ordinal))
        {
          AttachToSource(binding);
        }
      }
    }
  }
}
