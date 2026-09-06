using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioTimingRecoveryTests
  {
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void FourPlayersMustAllFinishBeforeJoinRegardlessOfArrivalOrder()
    {
      var connected = new HashSet<int> { 1, 2, 3, 4 };
      var barrier = new ScenarioCompletionBarrier(connected, 180);
      foreach (int player in new[] { 3, 1, 4 })
      {
        Assert.That(barrier.Complete(player), Is.True);
        Assert.That(barrier.Evaluate(connected, 10), Is.False);
      }
      Assert.That(barrier.Complete(3), Is.False, "Duplicate reports must not replace another player's work.");
      Assert.That(barrier.Complete(99), Is.False);
      Assert.That(barrier.Evaluate(connected, 20), Is.False);
      barrier.Complete(2);
      Assert.That(barrier.Evaluate(connected, 21), Is.True);
      Assert.That(barrier.TimedOut, Is.False);
    }

    [Test]
    public void DepartedParticipantReleasesJoinOnlyAfterRemainingPlayersFinish()
    {
      var connected = new HashSet<int> { 1, 2, 3, 4 };
      var barrier = new ScenarioCompletionBarrier(connected, 180);
      barrier.Complete(1);
      barrier.Complete(3);
      connected.Remove(2);
      Assert.That(barrier.Evaluate(connected, 10), Is.False);
      barrier.Complete(4);
      Assert.That(barrier.Evaluate(connected, 11), Is.True);
    }

    [Test]
    public void MissingReportHasBoundedRecoveryAndCannotAffectNextVisit()
    {
      var connected = new HashSet<int> { 1, 2, 3, 4 };
      var first = new ScenarioCompletionBarrier(connected, 180);
      first.Complete(1);
      Assert.That(first.Evaluate(connected, 179), Is.False);
      Assert.That(first.Evaluate(connected, 180), Is.True);
      Assert.That(first.TimedOut, Is.True);
      var next = new ScenarioCompletionBarrier(connected, 400);
      first.Complete(2);
      first.Complete(3);
      first.Complete(4);
      Assert.That(next.Evaluate(connected, 200), Is.False);
    }

    [Test]
    public void RevisitedDialogueRejectsOldReplyAndDoesNotAdvanceAnotherPlayer()
    {
      var go = new GameObject("late-presentation-reply");
      try
      {
        var controller = go.AddComponent<ScenarioController>();
        Field("_currentGraph").SetValue(controller, new ScenarioGraph { Identifier = "timing" });
        var mode = Field("_executionMode");
        mode.SetValue(controller, Enum.Parse(mode.FieldType, "ServerAuthoritative"));
        var callbacks = (Dictionary<int, Action>)Field("_branchDialogueAdvanceInterceptors").GetValue(controller);
        var replies = new List<int>();
        var tokens = new Dictionary<int, string>();
        for (int i = 1; i <= 4; i++)
        {
          int client = i;
          callbacks[client] = () => replies.Add(client);
          tokens[client] = (string)Call(controller, "BeginBranchPresentation", client, "D");
        }
        string previous = tokens[2];
        tokens[2] = (string)Call(controller, "BeginBranchPresentation", 2, "D");
        Assert.That(Call(controller, "TryAdvanceFromPresentation", 2, "timing", "D", previous), Is.False);
        Assert.That(Call(controller, "TryAdvanceFromPresentation", 3, "timing", "D", tokens[2]), Is.False);
        Assert.That(Call(controller, "TryAdvanceFromPresentation", 2, "old-graph", "D", tokens[2]), Is.False);
        Assert.That(replies, Is.Empty);
        Assert.That(Call(controller, "TryAdvanceFromPresentation", 2, "timing", "D", tokens[2]), Is.True);
        Assert.That(replies, Is.EqualTo(new[] { 2 }));
      }
      finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void MissingChoiceDisplayReleasesSlotWithoutSuccessfulAnswer()
    {
      var go = new GameObject("failed-choice-display");
      IEnumerator routine = null;
      try
      {
        var controller = go.AddComponent<ScenarioController>();
        Field("_currentGraph").SetValue(controller, new ScenarioGraph { Identifier = "timing" });
        var type = typeof(ScenarioController).GetNestedType("BranchOptionSelection", BindingFlags.NonPublic);
        object selection = Activator.CreateInstance(type, true);
        type.GetField("OptionCount").SetValue(selection, 2);
        routine = (IEnumerator)Call(controller, "RunBranchPrompt",
          (Action)(() => throw new InvalidOperationException("display unavailable")), selection, "C", (int?)1);
        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(type.GetField("Resolved").GetValue(selection), Is.False);
        Assert.That(Field("_branchPromptActive").GetValue(controller), Is.False);
        Assert.That(Field("_branchOptionInterceptor").GetValue(controller), Is.Null);
        Assert.That(controller.RecoveryNotes, Has.Count.EqualTo(1));
      }
      finally
      {
        (routine as IDisposable)?.Dispose();
        UnityEngine.Object.DestroyImmediate(go);
      }
    }

    [TestCase(null)]
    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidOrMissingDeadlineIsFinite(float? configured)
    {
      var method = typeof(ScenarioController).GetMethod("ResolveRecoveryWait", BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(method.Invoke(null, new object[] { configured }), Is.EqualTo(180f));
    }

    [UnityTest]
    public IEnumerator ValidatorKeepWaitingRecoversWithoutFakingCompletionSignal()
    {
      var go = new GameObject("bounded-validator");
      try
      {
        var controller = go.AddComponent<ScenarioController>();
        var graph = new ScenarioGraph { Identifier = "timing" };
        Field("_currentGraph").SetValue(controller, graph);
        var node = new ScenarioValidatorNode
        {
          Identifier = "wait", WaitForCondition = true, WaitTimeoutSeconds = 0.01f,
          RootConditions = new List<ScenarioValidatorRootCondition> {
            new ScenarioValidatorRootCondition { Condition = ScenarioValidatorCondition.PlayerCountGreaterThan, TargetCount = 100 }
          },
          OnWaitTimeout = ScenarioValidatorWaitTimeoutBehavior.KeepWaiting
        };
        var type = typeof(ScenarioController).GetNestedType("BranchChainContext", BindingFlags.NonPublic);
        var context = Activator.CreateInstance(type, new object[] { null, false });
        yield return (IEnumerator)Call(controller, "ExecuteValidatorGateCore", node, context);
        Assert.That(controller.HasActiveScenario, Is.True);
        Assert.That(controller.RecoveryNotes, Is.Not.Empty);
      }
      finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    private static FieldInfo Field(string name) => typeof(ScenarioController).GetField(name, Private);
    private static object Call(ScenarioController controller, string name, params object[] args)
      => typeof(ScenarioController).GetMethod(name, Private).Invoke(controller, args);
  }
}
