using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Registry;
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

    [Test]
    public void EmptyInitialActiveRoleRosterKeepsByRoleParallelWaiting()
    {
      var gameObject = new GameObject("scenario-empty-active-roster-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var currentGraph = typeof(ScenarioController).GetField(
        "_currentGraph",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var waitForRoster = typeof(ScenarioController).GetMethod(
        "WaitForInitialActiveRoleRoster",
        BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        currentGraph.SetValue(controller, new ScenarioGraph
        {
          Identifier = "empty-roster-test",
          ActiveRoleTags = new[] { "nurse_a" },
          SkipAbsentRoleBranches = true
        });
        var node = new ScenarioParallelNode
        {
          Identifier = "ROLE_PARALLEL",
          AllocationType = ScenarioParallelAllocationType.ByRole,
          WaitMode = ScenarioWaitMode.All
        };

        Assert.That(waitForRoster, Is.Not.Null);
        var routine = (IEnumerator)waitForRoster.Invoke(controller, new object[] { node });
        Assert.That(routine.MoveNext(), Is.True,
          "빈 초기 roster에서는 Parallel 할당을 시작하지 않고 역할 등록을 기다려야 합니다.");
        Assert.That(routine.Current, Is.Null);
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [TestCase(true, 1, true)]
    [TestCase(false, 1, false)]
    [TestCase(true, 0, false)]
    [TestCase(true, 2, false)]
    public void MultipleActiveRolesAreAcceptedOnlyForSinglePlayerDebug(
      bool enabled,
      int activePlayerCount,
      bool expected)
    {
      var shouldAllow = typeof(ScenarioController).GetMethod(
        "ShouldAllowMultipleActiveRolesForSinglePlayer",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(shouldAllow, Is.Not.Null);
      Assert.That(shouldAllow.Invoke(null, new object[] { enabled, activePlayerCount }), Is.EqualTo(expected));
    }

    [Test]
    public void BranchQuestControlExecutionAcceptsExplicitOwner()
    {
      var executeQuestControl = typeof(ScenarioController).GetMethod(
        "ExecuteQuestControlNode",
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new[] { typeof(ScenarioQuestControlNode), typeof(int?) },
        null);

      Assert.That(executeQuestControl, Is.Not.Null,
        "Role-parallel QuestControl must receive the branch owner instead of consulting only scenario ownership.");
    }

    [Test]
    public void BranchDialogueAdvanceIsConsumedWithoutAdvancingMainScenario()
    {
      var gameObject = new GameObject("scenario-branch-dialogue-advance-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var interceptors = typeof(ScenarioController).GetField(
        "_branchDialogueAdvanceInterceptors",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var tryAdvance = typeof(ScenarioController).GetMethod(
        "TryAdvanceBranchDialogue",
        BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(interceptors, Is.Not.Null);
        Assert.That(tryAdvance, Is.Not.Null);

        bool advanced = false;
        var values = (Dictionary<int, Action>)interceptors.GetValue(controller);
        values[42] = () => advanced = true;

        Assert.That(tryAdvance.Invoke(controller, new object[] { 42 }), Is.EqualTo(true));
        Assert.That(advanced, Is.True);
        Assert.That(tryAdvance.Invoke(controller, new object[] { 7 }), Is.EqualTo(false));
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void DynamicRosterCounterRequiresExactExpectedPlayerSignals()
    {
      const string output = "test.roster.complete";
      try
      {
        ScenarioSignalCounters.Register(
          "roster",
          "test.roster.arrival_",
          4,
          output,
          () => new[]
          {
            ScenarioInteractionSignals.Normalize("test.roster.arrival_alice"),
            ScenarioInteractionSignals.Normalize("test.roster.arrival_bob")
          });

        ScenarioInteractionSignals.Raise("test.roster.arrival_alice");
        ScenarioInteractionSignals.Raise("test.roster.arrival_patient");
        Assert.That(ScenarioInteractionSignals.IsRaised(output), Is.False);

        ScenarioInteractionSignals.Raise("test.roster.arrival_bob");
        Assert.That(ScenarioInteractionSignals.IsRaised(output), Is.True);
      }
      finally
      {
        ClearRosterCounterSignals(output);
      }
    }

    [Test]
    public void DynamicRosterCounterTreatsMultipleRolesOnOnePlayerAsOneArrival()
    {
      const string output = "test.roster.single-player.complete";
      try
      {
        ScenarioSignalCounters.Register(
          "single-player-roster",
          "test.roster.arrival_",
          4,
          output,
          () => new[]
          {
            ScenarioInteractionSignals.Normalize("test.roster.arrival_alice")
          });

        ScenarioInteractionSignals.Raise("test.roster.arrival_alice");

        Assert.That(ScenarioInteractionSignals.IsRaised(output), Is.True,
          "한 플레이어가 여러 역할을 맡아도 트리아지 도착 신호는 플레이어당 한 번이면 충분해야 합니다.");
      }
      finally
      {
        ScenarioSignalCounters.ClearAll();
        Registry.Registry.Unregister(RegistryType.RuntimeState,
          ScenarioInteractionSignals.Normalize("test.roster.arrival_alice"));
        Registry.Registry.Unregister(RegistryType.RuntimeState,
          ScenarioInteractionSignals.Normalize(output));
      }
    }

    [Test]
    public void DynamicRosterCounterReevaluatesAfterDisconnectShrink()
    {
      const string output = "test.roster.complete";
      bool bobConnected = true;
      try
      {
        ScenarioSignalCounters.Register(
          "roster",
          "test.roster.arrival_",
          4,
          output,
          () => bobConnected
            ? new[]
            {
              ScenarioInteractionSignals.Normalize("test.roster.arrival_alice"),
              ScenarioInteractionSignals.Normalize("test.roster.arrival_bob")
            }
            : new[] { ScenarioInteractionSignals.Normalize("test.roster.arrival_alice") });

        ScenarioInteractionSignals.Raise("test.roster.arrival_alice");
        Assert.That(ScenarioInteractionSignals.IsRaised(output), Is.False);

        bobConnected = false;
        ScenarioSignalCounters.RefreshDynamicThresholds();
        Assert.That(ScenarioInteractionSignals.IsRaised(output), Is.True);
      }
      finally
      {
        ClearRosterCounterSignals(output);
      }
    }

    private static void ClearRosterCounterSignals(string output)
    {
      ScenarioSignalCounters.ClearAll();
      foreach (var signal in new[]
               {
                 "test.roster.arrival_alice",
                 "test.roster.arrival_bob",
                 "test.roster.arrival_patient",
                 output
               })
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, ScenarioInteractionSignals.Normalize(signal));
      }
    }

    private static IEnumerator Probe(Action onMoveNext)
    {
      onMoveNext?.Invoke();
      yield return null;
    }
  }
}
