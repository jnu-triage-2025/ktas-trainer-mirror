using System;
using System.Collections;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioControllerParallelExecutionTests
  {
    [Test]
    public void BranchAdvanceSuppressionIsReleasedWhileCoroutineIsYielded()
    {
      var gameObject = new GameObject("scenario-parallel-suppression-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var suppressionDepth = typeof(ScenarioController).GetField(
        "_globalAdvanceSuppressionDepth",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var runSuppressed = typeof(ScenarioController).GetMethod(
        "RunWithGlobalAdvanceSuppressed",
        BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(suppressionDepth, Is.Not.Null);
        Assert.That(runSuppressed, Is.Not.Null);

        bool suppressedDuringMoveNext = false;
        var probe = Probe(() =>
          suppressedDuringMoveNext = (int)suppressionDepth.GetValue(controller) > 0);
        var wrapper = (IEnumerator)runSuppressed.Invoke(controller, new object[] { probe });

        Assert.That(wrapper.MoveNext(), Is.True);
        Assert.That(suppressedDuringMoveNext, Is.True,
          "분기 실행기가 Advance를 호출하는 순간에는 전역 진행이 억제되어야 합니다.");
        Assert.That((int)suppressionDepth.GetValue(controller), Is.Zero,
          "분기가 yield한 동안에는 메인 시나리오 진행 억제가 해제되어야 합니다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void AssignedBranchesAreYieldedInDefinitionOrder()
    {
      var runSequentially = typeof(ScenarioController).GetMethod(
        "RunSequentially",
        BindingFlags.Static | BindingFlags.NonPublic);
      var first = Probe(null);
      var second = Probe(null);

      Assert.That(runSequentially, Is.Not.Null);
      var sequence = (IEnumerator)runSequentially.Invoke(null, new object[]
      {
        new[] { first, second }
      });

      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequence.Current, Is.SameAs(first));
      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequence.Current, Is.SameAs(second));
      Assert.That(sequence.MoveNext(), Is.False);
    }

    [TestCase(true, 1, "patient_b_c_ct", "scen_b_nurse_arrivals", "quest_arrival_triage_area_", "all_nurses_arrived_triage", 1)]
    [TestCase(false, 1, "patient_b_c_ct", "scen_b_nurse_arrivals", "quest_arrival_triage_area_", "all_nurses_arrived_triage", 4)]
    [TestCase(true, 2, "patient_b_c_ct", "scen_b_nurse_arrivals", "quest_arrival_triage_area_", "all_nurses_arrived_triage", 4)]
    [TestCase(true, 1, "other", "scen_b_nurse_arrivals", "quest_arrival_triage_area_", "all_nurses_arrived_triage", 4)]
    [TestCase(true, 1, "patient_b_c_ct", "other", "quest_arrival_triage_area_", "all_nurses_arrived_triage", 4)]
    public void NurseArrivalThresholdIsReducedOnlyForSinglePlayerDebugMode(
      bool enabled,
      int activePlayerCount,
      string graphIdentifier,
      string counterIdentifier,
      string sourcePrefix,
      string outputSignal,
      int expected)
    {
      var resolveThreshold = typeof(ScenarioController).GetMethod(
        "ResolveSignalCounterThreshold",
        BindingFlags.Static | BindingFlags.NonPublic);
      var node = new ScenarioSignalCounterNode
      {
        CounterIdentifier = counterIdentifier,
        SourceSignalPrefix = sourcePrefix,
        OutputSignalIdentifier = outputSignal,
        Threshold = 4
      };

      Assert.That(resolveThreshold, Is.Not.Null);
      var actual = (int)resolveThreshold.Invoke(null, new object[]
      {
        graphIdentifier,
        node,
        activePlayerCount,
        enabled
      });

      Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase(true, ScenarioParallelAllocationType.ByRole, ScenarioWaitMode.All, true)]
    [TestCase(false, ScenarioParallelAllocationType.ByRole, ScenarioWaitMode.All, false)]
    [TestCase(true, ScenarioParallelAllocationType.ByRole, ScenarioWaitMode.Any, false)]
    [TestCase(true, ScenarioParallelAllocationType.ByRole, ScenarioWaitMode.None, false)]
    [TestCase(true, ScenarioParallelAllocationType.SelfAll, ScenarioWaitMode.All, false)]
    public void MultipleRoleBranchesAreAllowedForAnyPlayerCountInAllWaitMode(
      bool enabled,
      ScenarioParallelAllocationType allocationType,
      ScenarioWaitMode waitMode,
      bool expected)
    {
      var shouldAllow = typeof(ScenarioController).GetMethod(
        "ShouldAllowMultipleRoleBranches",
        BindingFlags.Static | BindingFlags.NonPublic);
      var node = new ScenarioParallelNode
      {
        AllocationType = allocationType,
        WaitMode = waitMode
      };

      Assert.That(shouldAllow, Is.Not.Null);
      var actual = (bool)shouldAllow.Invoke(null, new object[]
      {
        node,
        enabled
      });

      Assert.That(actual, Is.EqualTo(expected));
    }

    private static IEnumerator Probe(Action onMoveNext)
    {
      onMoveNext?.Invoke();
      yield return null;
    }
  }
}
