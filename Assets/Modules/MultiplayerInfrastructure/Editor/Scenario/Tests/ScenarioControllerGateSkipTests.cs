using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 운영자 게이트 건너뛰기(<see cref="ScenarioController.RequestGateSkip"/>)가 대기 중인 Validator
  /// 게이트를 실제로 풀어 주는지 검증한다. 발신처가 없는 신호를 기다리는 게이트는 타임아웃이 없으면
  /// 영원히 열리지 않으므로, 이 요청이 유일한 탈출구다.
  /// </summary>
  public sealed class ScenarioControllerGateSkipTests
  {
    private const string NeverRaisedSignal = "sig.gate_skip_test_never_raised";

    [Test]
    public void RequestGateSkipIsRejectedWithoutActiveGraph()
    {
      var gameObject = new GameObject("scenario-gate-skip-no-graph-test");
      try
      {
        var controller = gameObject.AddComponent<ScenarioController>();

        Assert.That(controller.RequestGateSkip(out string error), Is.False);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void RequestGateSkipIsRejectedOnClientPresentationPeer()
    {
      var gameObject = new GameObject("scenario-gate-skip-presentation-test");
      try
      {
        var controller = gameObject.AddComponent<ScenarioController>();
        SetPrivateField(controller, "_currentGraph", new ScenarioGraph { Identifier = "gate_skip_test" });
        var modeField = GetPrivateField("_executionMode");
        modeField.SetValue(controller, System.Enum.Parse(modeField.FieldType, "ClientPresentation"));

        Assert.That(controller.RequestGateSkip(out string error), Is.False,
          "표시 전용 피어는 서버 커서를 따라가므로 건너뛰기를 받아들이지 않는다.");
        Assert.That(error, Is.Not.Null.And.Not.Empty);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void WaitingBranchValidatorGateIsReleasedBySkipRequest()
    {
      var gameObject = new GameObject("scenario-gate-skip-branch-gate-test");
      try
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, NeverRaisedSignal);

        var controller = gameObject.AddComponent<ScenarioController>();
        var gate = BuildNeverSatisfiedGate("GATE", waitTimeoutSeconds: null);
        var graph = new ScenarioGraph { Identifier = "gate_skip_test" };
        graph.Add(gate);
        SetPrivateField(controller, "_currentGraph", graph);

        // 브랜치 게이트 경로는 전역 Advance/EndScenario 를 일으키지 않아 EditMode 에서 안전하게 구동할 수 있다.
        var routine = InvokeBranchGate(controller, gate);

        Assert.That(Pump(routine), Is.True, "게이트는 조건이 없으면 대기 상태여야 한다.");
        Assert.That(Pump(routine), Is.True, "타임아웃이 없는 게이트는 스스로 끝나지 않는다.");

        Assert.That(controller.RequestGateSkip(out string error), Is.True, error);

        Assert.That(PumpUntilDone(routine, 8), Is.True,
          "건너뛰기 요청 뒤에는 조건이 충족되지 않아도 게이트가 풀려야 한다.");
      }
      finally
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, NeverRaisedSignal);
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void GateStartedAfterSkipRequestKeepsWaiting()
    {
      var gameObject = new GameObject("scenario-gate-skip-later-gate-test");
      try
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, NeverRaisedSignal);

        var controller = gameObject.AddComponent<ScenarioController>();
        var gate = BuildNeverSatisfiedGate("GATE_LATER", waitTimeoutSeconds: null);
        var graph = new ScenarioGraph { Identifier = "gate_skip_test" };
        graph.Add(gate);
        SetPrivateField(controller, "_currentGraph", graph);

        Assert.That(controller.RequestGateSkip(out string error), Is.True, error);

        // 요청 이후에 시작한 게이트는 새 세대 번호를 기억하므로 영향을 받지 않아야 한다.
        var routine = InvokeBranchGate(controller, gate);
        Assert.That(PumpUntilDone(routine, 8), Is.False,
          "건너뛰기 요청 이전의 세대 번호는 그 뒤에 시작한 게이트를 풀지 않아야 한다.");
      }
      finally
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, NeverRaisedSignal);
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void SkipRequestReleasesGateWithDeclaredTimeoutBeforeItExpires()
    {
      var gameObject = new GameObject("scenario-gate-skip-timed-gate-test");
      try
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, NeverRaisedSignal);

        var controller = gameObject.AddComponent<ScenarioController>();
        // 운영 그래프처럼 긴 타임아웃(600초)이 걸린 게이트도 운영자가 기다리지 않고 풀 수 있어야 한다.
        var gate = BuildNeverSatisfiedGate("GATE_TIMED", waitTimeoutSeconds: 600f);
        gate.OnWaitTimeout = ScenarioValidatorWaitTimeoutBehavior.ForceAdvance;
        var graph = new ScenarioGraph { Identifier = "gate_skip_test" };
        graph.Add(gate);
        SetPrivateField(controller, "_currentGraph", graph);

        var routine = InvokeBranchGate(controller, gate);
        Assert.That(PumpUntilDone(routine, 4), Is.False,
          "타임아웃이 아직 지나지 않은 게이트는 대기 중이어야 한다.");

        Assert.That(controller.RequestGateSkip(out string error), Is.True, error);
        Assert.That(PumpUntilDone(routine, 8), Is.True,
          "타임아웃이 남아 있어도 건너뛰기 요청은 게이트를 즉시 풀어야 한다.");
      }
      finally
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, NeverRaisedSignal);
        Object.DestroyImmediate(gameObject);
      }
    }

    private static ScenarioValidatorNode BuildNeverSatisfiedGate(string identifier, float? waitTimeoutSeconds)
    {
      return new ScenarioValidatorNode
      {
        Identifier = identifier,
        WaitForCondition = true,
        WaitTimeoutSeconds = waitTimeoutSeconds,
        OnWaitTimeout = ScenarioValidatorWaitTimeoutBehavior.KeepWaiting,
        OnFailure = ScenarioValidatorOnFailure.Ignore,
        RootConditions = new List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.RegistryContains,
            ValidationRules = new List<ScenarioValidatorRule>
            {
              new ScenarioValidatorRule
              {
                Type = ScenarioValidatorRuleType.Registry,
                Condition = ScenarioValidatorRuleCondition.Contains,
                RegistryType = RegistryType.RuntimeState,
                RegistryIdentifier = NeverRaisedSignal
              }
            }
          }
        }
      };
    }

    private static IEnumerator InvokeBranchGate(ScenarioController controller, ScenarioValidatorNode gate)
    {
      var method = typeof(ScenarioController).GetMethod(
        "ExecuteValidatorGate", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null, "ExecuteValidatorGate 가 존재해야 한다.");
      var contextType = typeof(ScenarioController).GetNestedType("BranchChainContext", BindingFlags.NonPublic);
      Assert.That(contextType, Is.Not.Null, "BranchChainContext 가 존재해야 한다.");
      // 담당자가 지정되지 않은 브랜치 컨텍스트는 취소 판정 대상이 아니므로 게이트가 순수하게 조건만 기다린다.
      object context = System.Activator.CreateInstance(contextType, new object[] { null, false });
      return (IEnumerator)method.Invoke(controller, new[] { gate, context });
    }

    /// <summary>
    /// 중첩 코루틴을 한 프레임만큼 진행시킨다. 아직 대기 중이면 true, 끝났으면 false 를 돌려준다.
    /// </summary>
    private static bool Pump(IEnumerator routine)
    {
      object pending = routine.Current;
      if (pending is IEnumerator inner && Pump(inner))
        return true;
      if (pending is CustomYieldInstruction instruction && instruction.keepWaiting)
        return true;
      return routine.MoveNext();
    }

    /// <summary>최대 <paramref name="maxSteps"/> 프레임 안에 코루틴이 끝나면 true.</summary>
    private static bool PumpUntilDone(IEnumerator routine, int maxSteps)
    {
      for (int step = 0; step < maxSteps; step++)
      {
        if (!Pump(routine))
          return true;
      }

      return false;
    }

    private static FieldInfo GetPrivateField(string name)
    {
      var field = typeof(ScenarioController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null, $"{name} 필드가 존재해야 한다.");
      return field;
    }

    private static void SetPrivateField(ScenarioController controller, string name, object value)
    {
      GetPrivateField(name).SetValue(controller, value);
    }
  }
}
