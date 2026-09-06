using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  public sealed class ScenarioBranchScreenRecoveryTests
  {
    private GameObject _object;
    private ScenarioController _controller;
    private readonly List<IEnumerator> _routines = new();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
      _object = new GameObject("branch-screen-recovery");
      _controller = _object.AddComponent<ScenarioController>();
      Set("_currentGraph", new ScenarioGraph { Identifier = "screen-recovery" });
      Set("_activeOptions", new List<ScenarioChoiceOption> { new ScenarioChoiceOption() });
    }

    [TearDown]
    public void TearDown()
    {
      foreach (var routine in _routines)
        (routine as IDisposable)?.Dispose();
      _routines.Clear();
      UnityEngine.Object.DestroyImmediate(_object);
    }

    [Test]
    public void LocalPromptRegistersInputBeforePresentationEvenWithAssignedOwner()
    {
      var routine = Prompt(42, () => _controller.SubmitLocalOptionSelection(0));
      Assert.That(routine.MoveNext(), Is.False,
        "A synchronous selection during presentation must resolve the prompt in local mode.");
      Assert.That(Get("_branchPromptActive"), Is.False);
      Assert.That(Get("_branchOptionInterceptor"), Is.Null);
    }

    [Test]
    public void LocalPromptWaitsForDialogueWithoutDisplayingOverIt()
    {
      Set("_branchDialogueActive", true);
      bool displayed = false;
      var routine = Prompt(null, () => displayed = true);
      Assert.That(routine.MoveNext(), Is.True);
      Assert.That(displayed, Is.False);
      Set("_branchDialogueActive", false);
      Assert.That(routine.MoveNext(), Is.True);
      Assert.That(displayed, Is.True);
      _controller.SubmitLocalOptionSelection(0);
      Assert.That(routine.MoveNext(), Is.False);
    }

    [Test]
    public void DialogueWaitsForLocalPromptBeforeRegisteringItsInput()
    {
      Set("_branchPromptActive", true);
      var contextType = typeof(ScenarioController).GetNestedType("BranchChainContext", BindingFlags.NonPublic);
      var context = Activator.CreateInstance(contextType, new object[] { null, false });
      var routine = (IEnumerator)typeof(ScenarioController).GetMethod("ExecuteDialogueNodeInBranch", Private)
        .Invoke(_controller, new object[] { new ScenarioDialogueNode { Identifier = "D" }, context });
      _routines.Add(routine);
      Assert.That(routine.MoveNext(), Is.True);
      Assert.That((Dictionary<int, Action>)Get("_branchDialogueAdvanceInterceptors"), Is.Empty);
      Set("_branchPromptActive", false);
      Assert.That(routine.MoveNext(), Is.True);
      Assert.That((Dictionary<int, Action>)Get("_branchDialogueAdvanceInterceptors"), Is.Not.Empty);
    }

    [Test]
    public void FourRemotePlayersHaveIndependentSlotsAndCancelledQueueReleases()
    {
      var mode = typeof(ScenarioController).GetField("_executionMode", Private);
      mode.SetValue(_controller, Enum.Parse(mode.FieldType, "ServerAuthoritative"));
      var prompts = new List<IEnumerator>();
      for (int client = 1; client <= 4; client++)
      {
        var prompt = Prompt(client, () => Assert.Fail("Remote prompts must not display on the server UI."));
        prompts.Add(prompt);
        Assert.That(prompt.MoveNext(), Is.True);
      }
      Assert.That((HashSet<int>)Get("_activeRemoteBranchPromptClients"), Has.Count.EqualTo(4));
      Assert.That((IDictionary)Get("_remoteBranchChoiceSelections"), Has.Count.EqualTo(4));
      var queued = Prompt(1, () => Assert.Fail("Queued prompt was displayed."));
      Assert.That(queued.MoveNext(), Is.True);
      Assert.That((IDictionary)Get("_remoteBranchChoiceSelections"), Has.Count.EqualTo(4));
      ((HashSet<int>)Get("_cancelledBranchClientIds")).Add(1);
      Assert.That(queued.MoveNext(), Is.False);
      Assert.That(prompts[0].MoveNext(), Is.False);
      Assert.That((HashSet<int>)Get("_activeRemoteBranchPromptClients"), Has.Count.EqualTo(3));
    }

    private IEnumerator Prompt(int? owner, Action present)
    {
      var selectionType = typeof(ScenarioController).GetNestedType("BranchOptionSelection", BindingFlags.NonPublic);
      var selection = Activator.CreateInstance(selectionType, true);
      selectionType.GetField("OptionCount").SetValue(selection, 1);
      var routine = (IEnumerator)typeof(ScenarioController).GetMethod("RunBranchPrompt", Private)
        .Invoke(_controller, new object[] { present, selection, "choice", owner });
      _routines.Add(routine);
      return routine;
    }

    private object Get(string name) => typeof(ScenarioController).GetField(name, Private).GetValue(_controller);
    private void Set(string name, object value) => typeof(ScenarioController).GetField(name, Private).SetValue(_controller, value);
  }
}
