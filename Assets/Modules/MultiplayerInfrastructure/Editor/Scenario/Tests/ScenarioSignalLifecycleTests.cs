using System;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 신호가 내려간 뒤의 상태 복구를 검증한다.
  ///
  /// 시나리오 신호는 sticky 지만 게임플레이가 도중에 내리기도 한다(예: 처치 단계를 다시 시작하며
  /// 이전 결과 신호를 지우는 경우). 신호를 내린 뒤에도 발신자와 누적 집계가 올바른 상태로
  /// 남아 있어야, 같은 처치를 다시 수행했을 때 게이트가 정상적으로 열린다.
  ///
  /// 전제: EditMode 에서는 FishNet 이 비활성이고 <see cref="ScenarioNetworkRelay"/> 인스턴스가
  /// 없으므로 Raise/Clear 가 로컬 폴백 경로로 동기 실행된다.
  /// </summary>
  public sealed class ScenarioSignalLifecycleTests
  {
    private const string Output = "test.lifecycle.output";
    private const string EventName = "TreatmentApplied";
    private const string EventKey = "NasalCannulaApplied";

    private static readonly string[] TouchedSignals =
    {
      Output,
      "test.lifecycle.count_a",
      "test.lifecycle.count_b",
      "test.lifecycle.counted",
    };

    [SetUp]
    public void SetUp() => ResetAll();

    [TearDown]
    public void TearDown() => ResetAll();

    private static void ResetAll()
    {
      ScenarioEntityStateSignalBindings.ClearAll();
      ScenarioSignalCounters.ClearAll();
      ScenarioServerInternalSignalRegistry.ClearAll();
      foreach (string signal in TouchedSignals)
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(signal));
    }

    /// <summary>테스트용 상태 이벤트 소스. 등록된 리스너를 그대로 보관하고 수동으로 발생시킨다.</summary>
    private sealed class FakeStateEventSource : IScenarioEntityStateEventSource
    {
      private readonly List<(string EventName, string Key, Action<string> Callback)> _listeners = new();

      public int ListenerCount => _listeners.Count;

      public IReadOnlyList<string> GetStateEventNames() => new[] { EventName };

      public bool RegisterStateEventListener(string eventName, string key, Action<string> onFired)
      {
        if (!string.Equals(eventName, EventName, StringComparison.Ordinal) || onFired == null)
          return false;

        _listeners.Add((eventName, key, onFired));
        return true;
      }

      public void UnregisterStateEventListeners(string eventName)
      {
        if (string.IsNullOrWhiteSpace(eventName))
        {
          _listeners.Clear();
          return;
        }

        for (int index = _listeners.Count - 1; index >= 0; index--)
        {
          if (string.Equals(_listeners[index].EventName, eventName, StringComparison.Ordinal))
            _listeners.RemoveAt(index);
        }
      }

      public void Fire(string key)
      {
        foreach (var listener in _listeners.ToArray())
        {
          if (string.IsNullOrEmpty(listener.Key) || string.Equals(listener.Key, key, StringComparison.Ordinal))
            listener.Callback(key);
        }
      }
    }

    [Test]
    public void ConsumedOnceBindingIsRearmedAfterItsSignalIsCleared()
    {
      var source = new FakeStateEventSource();
      Assert.That(ScenarioEntityStateSignalBindings.Register(
        "binding", source, EventName, EventKey, Output, consumeOnce: true), Is.True);

      source.Fire(EventKey);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True);
      Assert.That(source.ListenerCount, Is.Zero, "1회성 바인딩은 발신 뒤 소스에서 떨어져야 한다.");

      // 게임플레이가 처치 단계를 다시 시작하며 결과 신호를 내리는 상황이다.
      ScenarioInteractionSignals.Clear(Output);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False);
      Assert.That(source.ListenerCount, Is.EqualTo(1),
        "신호를 내렸으면 발신자가 다시 붙어 있어야 한다. 그렇지 않으면 이 신호를 기다리는 "
        + "Validator 게이트를 열 수 있는 주체가 하나도 남지 않는다.");

      source.Fire(EventKey);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True,
        "같은 처치를 다시 수행하면 신호가 다시 올라가야 한다.");
    }

    [Test]
    public void ConsumedOnceBindingStillFiresOnlyOncePerArming()
    {
      var source = new FakeStateEventSource();
      ScenarioEntityStateSignalBindings.Register(
        "binding", source, EventName, EventKey, Output, consumeOnce: true);

      source.Fire(EventKey);
      ScenarioInteractionSignals.Clear(Output);
      source.Fire(EventKey);
      source.Fire(EventKey);

      Assert.That(source.ListenerCount, Is.Zero,
        "재무장 뒤에도 1회성 계약은 그대로여서, 한 번 발신하면 다시 떨어져야 한다.");
    }

    [Test]
    public void SiblingBindingsOnTheSameEventSurviveAConsume()
    {
      var source = new FakeStateEventSource();
      ScenarioEntityStateSignalBindings.Register(
        "first", source, EventName, EventKey, Output, consumeOnce: true);
      ScenarioEntityStateSignalBindings.Register(
        "second", source, EventName, null, "test.lifecycle.count_a", consumeOnce: false);

      source.Fire(EventKey);

      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True);
      Assert.That(ScenarioInteractionSignals.IsRaised("test.lifecycle.count_a"), Is.True);
      Assert.That(source.ListenerCount, Is.EqualTo(1),
        "소비된 바인딩만 떨어지고 같은 이벤트의 다른 바인딩은 유지되어야 한다.");
    }

    [Test]
    public void CounterDropsSignalsThatWereClearedAgain()
    {
      ScenarioSignalCounters.Register("counter", "test.lifecycle.count_", 2, "test.lifecycle.counted");

      ScenarioInteractionSignals.Raise("test.lifecycle.count_a");
      Assert.That(ScenarioInteractionSignals.IsRaised("test.lifecycle.counted"), Is.False);

      // 첫 신호가 다시 내려갔으므로 임계치를 채운 것으로 봐서는 안 된다.
      ScenarioInteractionSignals.Clear("test.lifecycle.count_a");
      ScenarioInteractionSignals.Raise("test.lifecycle.count_b");

      Assert.That(ScenarioInteractionSignals.IsRaised("test.lifecycle.counted"), Is.False,
        "내려간 신호를 계속 세면 더 이상 참이 아닌 조건으로 출력 신호가 발신된다.");

      ScenarioInteractionSignals.Raise("test.lifecycle.count_a");
      Assert.That(ScenarioInteractionSignals.IsRaised("test.lifecycle.counted"), Is.True,
        "두 신호가 실제로 함께 올라가면 발신되어야 한다.");
    }

    [Test]
    public void InternalSignalRegisterInvokesCallbackWhenAlreadyResolved()
    {
      bool invoked = false;

      // resolve 가 register 보다 먼저 도착하는, 이 레지스트리가 존재하는 이유인 순서다.
      ScenarioServerInternalSignalRegistry.Resolve("@m", "test.lifecycle.internal");
      bool consumed = ScenarioServerInternalSignalRegistry.Register(
        "@m", "test.lifecycle.internal", () => invoked = true);

      Assert.That(consumed, Is.True);
      Assert.That(invoked, Is.True,
        "이미 resolve 된 신호에도 콜백을 실행해야 한다. 콜백만 기다리는 호출부는 그렇지 않으면 영원히 멈춘다.");
    }

    [Test]
    public void InternalSignalRegisterStillDefersCallbackUntilResolved()
    {
      bool invoked = false;

      bool consumed = ScenarioServerInternalSignalRegistry.Register(
        "@m", "test.lifecycle.internal", () => invoked = true);

      Assert.That(consumed, Is.False);
      Assert.That(invoked, Is.False);

      ScenarioServerInternalSignalRegistry.Resolve("@m", "test.lifecycle.internal");
      Assert.That(invoked, Is.True);
    }

    [Test]
    public void TriggerZoneClearsPerRunStateForANewScenarioRun()
    {
      var zoneObject = new GameObject("signal-only-zone");
      try
      {
        var zone = AddActivatedZone(zoneObject);
        SetPrivate(zone, "_hasTriggered", true);
        RaisedEntities(zone).Add("patient_b");

        ScenarioTriggerZone.ResetAllForNewScenarioRun();

        Assert.That(RaisedEntities(zone), Is.Empty,
          "새 실행에서는 같은 엔티티가 다시 진입했을 때 대상별 신호를 다시 올려야 한다.");
        Assert.That(GetPrivate<bool>(zone, "_hasTriggered"), Is.False,
          "신호 전용 존은 실행마다 다시 발신해야 한다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(zoneObject);
      }
    }

    [Test]
    public void TriggerZoneThatStartsAScenarioKeepsItsTriggeredFlag()
    {
      var zoneObject = new GameObject("scenario-start-zone");
      try
      {
        var zone = AddActivatedZone(zoneObject);
        SetPrivate(zone, "_cachedGraph", new ScenarioGraph { Identifier = "graph" });
        SetPrivate(zone, "_hasTriggered", true);

        ScenarioTriggerZone.ResetAllForNewScenarioRun();

        Assert.That(GetPrivate<bool>(zone, "_hasTriggered"), Is.True,
          "시나리오를 시작시키는 존이 1회 트리거 상태를 잃으면 방금 띄운 시나리오를 다시 시작시킬 수 있다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(zoneObject);
      }
    }

    /// <summary>
    /// 존을 붙인 뒤 활성 전이를 강제해 OnEnable 이 확실히 실행되게 한다.
    /// ResetAllForNewScenarioRun 은 OnEnable 에서 등록된 활성 존만 대상으로 하므로,
    /// 등록 시점이 보장되지 않으면 검사 자체가 무의미해진다.
    /// </summary>
    private static ScenarioTriggerZone AddActivatedZone(GameObject host)
    {
      host.SetActive(false);
      var zone = host.AddComponent<ScenarioTriggerZone>();
      host.SetActive(true);
      return zone;
    }

    private static FieldInfo Field(string name)
      => typeof(ScenarioTriggerZone).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
         ?? throw new MissingFieldException(nameof(ScenarioTriggerZone), name);

    private static void SetPrivate(ScenarioTriggerZone zone, string name, object value)
      => Field(name).SetValue(zone, value);

    private static T GetPrivate<T>(ScenarioTriggerZone zone, string name)
      => (T)Field(name).GetValue(zone);

    private static HashSet<string> RaisedEntities(ScenarioTriggerZone zone)
      => GetPrivate<HashSet<string>>(zone, "_perEntityRaised");
  }
}
