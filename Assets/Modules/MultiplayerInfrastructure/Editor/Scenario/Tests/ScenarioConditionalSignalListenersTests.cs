using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 조건부 시나리오 신호 리스너의 등록/조건/제거/정리와 재진입·중복 방어를 검증한다.
  ///
  /// 전제: EditMode 에서는 FishNet 네트워크가 비활성이고 <see cref="ScenarioNetworkRelay"/>
  /// 인스턴스가 없으므로 <see cref="ScenarioInteractionSignals.Raise"/> 는 로컬 폴백 경로로
  /// <c>RegisterLocal</c> 을 직접 호출한다. 따라서 RuntimeState 레지스트리 기록과
  /// <c>OnSignalRegistered</c> 이벤트가 동기적으로 동작한다.
  /// </summary>
  public sealed class ScenarioConditionalSignalListenersTests
  {
    // 테스트마다 고유 접두 신호를 써 레지스트리/리스너 상태 누수를 피한다.
    private const string Source = "test.src";
    private const string Output = "test.out";
    private const string Required = "test.req";

    private readonly List<string> _touched = new();

    [SetUp]
    public void SetUp()
    {
      ScenarioConditionalSignalListeners.ClearAll();
      ClearSignals();
    }

    [TearDown]
    public void TearDown()
    {
      ScenarioConditionalSignalListeners.ClearAll();
      ClearSignals();
    }

    private void ClearSignals()
    {
      foreach (var raw in new[] { Source, Output, Required, "test.a", "test.b", "test.c" })
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(raw));
      }
    }

    [Test]
    public void ConditionMet_RaisesOutputOnce()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, new[] { Required }, consumeOnce: true);

      // 선행 조건 충족 후 원본 신호 발생.
      ScenarioInteractionSignals.Raise(Required);
      ScenarioInteractionSignals.Raise(Source);

      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True, "조건 충족 시 output 이 발생해야 한다");
    }

    [Test]
    public void ConditionNotMet_DoesNotRaiseOutput()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, new[] { Required }, consumeOnce: true);

      // 선행 조건 없이 원본 신호만 발생.
      ScenarioInteractionSignals.Raise(Source);

      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False, "선행 조건 미충족 시 output 이 발생하면 안 된다");
    }

    [Test]
    public void SourceBeforePrerequisiteIsReevaluatedWhenPrerequisiteArrives()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, new[] { Required }, true);
      ScenarioInteractionSignals.Raise(Source);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False);
      ScenarioInteractionSignals.Raise(Required);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True);
    }

    [Test]
    public void LateRegistrationReplaysCompletedState()
    {
      ScenarioInteractionSignals.Raise(Source);
      ScenarioInteractionSignals.Raise(Required);
      ScenarioConditionalSignalListeners.Register("L", Source, Output, new[] { Required }, true);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True);
    }

    [Test]
    public void ClearedSourceCannotCompleteAfterLaterPrerequisite()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, new[] { Required }, true);
      ScenarioInteractionSignals.Raise(Source);
      ScenarioInteractionSignals.Clear(Source);
      ScenarioInteractionSignals.Raise(Required);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False);
      ScenarioInteractionSignals.Raise(Source);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True);
    }

    [Test]
    public void RepeatedPrerequisiteDoesNotCountSameSourceTwice()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, new[] { Required }, false);
      ScenarioInteractionSignals.Raise(Source);
      ScenarioInteractionSignals.Raise(Required);
      ScenarioInteractionSignals.Clear(Output);
      ScenarioInteractionSignals.Clear(Required);
      ScenarioInteractionSignals.Raise(Required);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False);
    }

    [Test]
    public void Unregister_StopsReacting()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, System.Array.Empty<string>(), consumeOnce: true);
      Assert.That(ScenarioConditionalSignalListeners.Unregister("L"), Is.True);

      ScenarioInteractionSignals.Raise(Source);

      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False, "제거된 리스너는 이후 이벤트에 반응하면 안 된다");
    }

    [Test]
    public void ClearAll_RemovesAllListeners()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, System.Array.Empty<string>(), consumeOnce: true);
      ScenarioConditionalSignalListeners.ClearAll();

      ScenarioInteractionSignals.Raise(Source);

      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False, "ClearAll 이후 리스너는 반응하면 안 된다");
    }

    [Test]
    public void DuplicateRegister_ReplacesPreviousDefinition()
    {
      // 동일 식별자로 재등록하면 정의가 교체된다(output 이 test.a → test.b 로 변경).
      ScenarioConditionalSignalListeners.Register("L", Source, "test.a", System.Array.Empty<string>(), consumeOnce: true);
      ScenarioConditionalSignalListeners.Register("L", Source, "test.b", System.Array.Empty<string>(), consumeOnce: true);

      ScenarioInteractionSignals.Raise(Source);

      Assert.That(ScenarioInteractionSignals.IsRaised("test.a"), Is.False, "교체된 이전 정의의 output 은 발생하면 안 된다");
      Assert.That(ScenarioInteractionSignals.IsRaised("test.b"), Is.True, "교체된 최신 정의의 output 이 발생해야 한다");
    }

    [Test]
    public void ConsumeOnce_DoesNotReRaiseOnRepeatedSourceClearAndRaise()
    {
      ScenarioConditionalSignalListeners.Register("L", Source, Output, System.Array.Empty<string>(), consumeOnce: true);

      ScenarioInteractionSignals.Raise(Source);
      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.True);

      // output 을 내리고 원본을 다시 올려도, ConsumeOnce 리스너는 이미 제거되어 재발생하지 않는다.
      ScenarioInteractionSignals.Clear(Output);
      ScenarioInteractionSignals.Clear(Source);
      ScenarioInteractionSignals.Raise(Source);

      Assert.That(ScenarioInteractionSignals.IsRaised(Output), Is.False, "ConsumeOnce 리스너는 1회 발생 후 재발생하면 안 된다");
    }

    [Test]
    public void SelfChainingListener_DoesNotStackOverflow()
    {
      // output == source, ConsumeOnce=false 인 순환 리스너. 재진입 큐 가드가 없으면
      // Raise → OnSignalRegistered → Dispatch 가 무한 재귀하여 스택 오버플로우가 발생한다.
      // 가드가 있으면 최초 전이 1회만 처리되고(RegisterLocal 멱등화), 예외 없이 종료된다.
      ScenarioConditionalSignalListeners.Register("L", Source, Source, System.Array.Empty<string>(), consumeOnce: false);

      Assert.DoesNotThrow(() => ScenarioInteractionSignals.Raise(Source));
      Assert.That(ScenarioInteractionSignals.IsRaised(Source), Is.True);
    }

    [Test]
    public void OutputChaining_TriggersDownstreamListener()
    {
      // A 의 output 이 B 의 source 인 정당한 체이닝: A 발생 시 B 의 output 까지 순차 전파된다.
      ScenarioConditionalSignalListeners.Register("A", "test.a", "test.b", System.Array.Empty<string>(), consumeOnce: true);
      ScenarioConditionalSignalListeners.Register("B", "test.b", "test.c", System.Array.Empty<string>(), consumeOnce: true);

      ScenarioInteractionSignals.Raise("test.a");

      Assert.That(ScenarioInteractionSignals.IsRaised("test.b"), Is.True, "1차 output 이 발생해야 한다");
      Assert.That(ScenarioInteractionSignals.IsRaised("test.c"), Is.True, "체이닝된 2차 output 이 발생해야 한다");
    }
  }
}
