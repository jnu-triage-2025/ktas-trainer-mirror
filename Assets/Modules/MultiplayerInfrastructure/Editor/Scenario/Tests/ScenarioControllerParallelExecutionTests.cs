using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Registry;
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
    public void WaitUntilDoneInvokeEventYieldsManagedRoutineWithoutStartingNestedCoroutine()
    {
      const string eventIdentifier = "test.manual-entry.wait-until-done";
      var gameObject = new GameObject("scenario-wait-invoke-event-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var executeInvokeEvent = typeof(ScenarioController).GetMethod(
        "ExecuteInvokeEventNode",
        BindingFlags.Instance | BindingFlags.NonPublic);
      ScenarioEventIdentifierRegistry.Register(eventIdentifier, () => Probe(null));

      try
      {
        Assert.That(executeInvokeEvent, Is.Not.Null);
        var node = new ScenarioInvokeEventNode
        {
          Identifier = "WAIT_EVENT",
          EventIdentifier = eventIdentifier,
          MoveNextBehavior = ScenarioInvokeEventMoveNextBehavior.WaitUntilDone
        };
        var routine = (IEnumerator)executeInvokeEvent.Invoke(controller, new object[] { node });

        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.Current, Is.InstanceOf<IEnumerator>(),
          "WaitUntilDone 이벤트는 Unity에 중첩 코루틴을 다시 등록하지 않고 관리 루틴을 직접 yield해야 합니다.");
        Assert.That(routine.Current, Is.Not.InstanceOf<Coroutine>());
      }
      finally
      {
        ScenarioEventIdentifierRegistry.Unregister(eventIdentifier);
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void ParallelBranchWaitBlocksExternalGlobalAdvance()
    {
      var gameObject = new GameObject("scenario-parallel-advance-block-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var currentGraph = typeof(ScenarioController).GetField(
        "_currentGraph",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var currentNode = typeof(ScenarioController).GetField(
        "_currentNode",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var blockDepth = typeof(ScenarioController).GetField(
        "_parallelAdvanceBlockDepth",
        BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(currentGraph, Is.Not.Null);
        Assert.That(currentNode, Is.Not.Null);
        Assert.That(blockDepth, Is.Not.Null);

        var parallel = new ScenarioParallelNode
        {
          Identifier = "P_B_CARE",
          NextIdentifier = "B_COMPLETE",
          WaitMode = ScenarioWaitMode.All
        };
        var next = new ScenarioStateUpdateNode
        {
          Identifier = "B_COMPLETE"
        };
        var graph = new ScenarioGraph { Identifier = "parallel-advance-block-test" };
        graph.Add(parallel);
        graph.Add(next);
        currentGraph.SetValue(controller, graph);
        currentNode.SetValue(controller, parallel);
        blockDepth.SetValue(controller, 1);

        controller.Advance();

        Assert.That(currentNode.GetValue(controller), Is.SameAs(parallel),
          "WaitMode.All 병렬 분기가 완료되기 전에는 외부 Advance가 다음 노드로 진행하면 안 됩니다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void ParallelAdvanceBlockReleaseNeverDropsBelowZero()
    {
      var gameObject = new GameObject("scenario-parallel-advance-release-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var blockDepth = typeof(ScenarioController).GetField(
        "_parallelAdvanceBlockDepth",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var releaseBlock = typeof(ScenarioController).GetMethod(
        "ReleaseParallelAdvanceBlock",
        BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(blockDepth, Is.Not.Null);
        Assert.That(releaseBlock, Is.Not.Null);

        blockDepth.SetValue(controller, 0);
        releaseBlock.Invoke(controller, null);

        Assert.That(blockDepth.GetValue(controller), Is.EqualTo(0),
          "시나리오 종료가 병렬 차단 상태를 초기화한 뒤에도 코루틴 정리 경로가 카운터를 음수로 만들면 안 됩니다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void AssignedBranchesAreStartedInDefinitionOrderAfterPredecessorCompletes()
    {
      var sequencer = new BranchSequencerHarness();
      var first = sequencer.AddBranch();
      var second = sequencer.AddBranch();
      var sequence = sequencer.Run();

      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine }),
        "첫 분기만 시작되어야 합니다.");
      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine }),
        "첫 분기가 활동 중인 동안에는 다음 분기를 시작하지 않아야 합니다.");

      first.Complete();
      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine, second.Routine }),
        "첫 분기가 끝나면 정의 순서대로 다음 분기를 시작해야 합니다.");

      second.Complete();
      Assert.That(sequence.MoveNext(), Is.False, "모든 분기가 끝나면 순차 실행기가 종료되어야 합니다.");
    }

    [Test]
    public void IdleBranchWaitingForOthersLetsNextBranchOfSamePlayerStart()
    {
      var sequencer = new BranchSequencerHarness();
      var first = sequencer.AddBranch();
      var second = sequencer.AddBranch();
      var third = sequencer.AddBranch();
      var sequence = sequencer.Run();

      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine }));

      // 첫 분기가 자기 몫을 끝내고 다른 담당자를 기다리는 idle 상태가 되면 다음 태그의 분기를 시작한다.
      first.SetIdle(true);
      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine, second.Routine }),
        "다른 참여자를 기다리는 idle 분기는 끝난 것과 같이 취급해 다음 분기를 시작해야 합니다.");

      // 두 번째 분기가 활동 중이면 첫 분기가 idle 이어도 세 번째 분기는 시작하지 않는다.
      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine, second.Routine }),
        "활동 중인 분기가 남아 있으면 세 번째 분기를 시작하지 않아야 합니다.");

      second.Complete();
      Assert.That(sequence.MoveNext(), Is.True);
      Assert.That(sequencer.StartedRoutines, Is.EqualTo(new[] { first.Routine, second.Routine, third.Routine }));

      // 게이트가 열려 첫 분기가 다시 활동하더라도 이미 시작한 분기는 그대로 진행한다.
      first.SetIdle(false);
      third.Complete();
      Assert.That(sequence.MoveNext(), Is.True,
        "idle 이었던 분기가 아직 끝나지 않았으면 순차 실행기도 끝나지 않아야 합니다.");

      first.Complete();
      Assert.That(sequence.MoveNext(), Is.False);
    }

    /// <summary>
    /// <c>ScenarioController.RunSequentiallyUnlessIdle</c> 을 리플렉션으로 구동하는 시험 도우미.
    /// 분기 코루틴은 실제로 실행하지 않고, 시작 요청 순서와 추적기 플래그만 다룬다.
    /// </summary>
    private sealed class BranchSequencerHarness
    {
      private static readonly Type TrackerType = typeof(ScenarioController).GetNestedType(
        "BranchCompletionTracker", BindingFlags.NonPublic);
      private static readonly Type SequencedBranchType = typeof(ScenarioController).GetNestedType(
        "SequencedBranch", BindingFlags.NonPublic);
      private static readonly MethodInfo RunMethod = typeof(ScenarioController).GetMethod(
        "RunSequentiallyUnlessIdle", BindingFlags.Static | BindingFlags.NonPublic);

      private readonly List<Branch> _branches = new();

      public List<IEnumerator> StartedRoutines { get; } = new();

      public Branch AddBranch()
      {
        Assert.That(TrackerType, Is.Not.Null, "BranchCompletionTracker 중첩 형식이 있어야 합니다.");
        var branch = new Branch(Probe(null), Activator.CreateInstance(TrackerType, nonPublic: true));
        _branches.Add(branch);
        return branch;
      }

      public IEnumerator Run()
      {
        Assert.That(SequencedBranchType, Is.Not.Null, "SequencedBranch 중첩 형식이 있어야 합니다.");
        Assert.That(RunMethod, Is.Not.Null, "RunSequentiallyUnlessIdle 정적 실행기가 있어야 합니다.");

        var constructor = SequencedBranchType.GetConstructors(
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];
        var entries = Array.CreateInstance(SequencedBranchType, _branches.Count);
        for (var index = 0; index < _branches.Count; index++)
        {
          entries.SetValue(
            constructor.Invoke(new[] { _branches[index].Routine, _branches[index].Tracker }),
            index);
        }

        Func<IEnumerator, Coroutine> start = routine =>
        {
          StartedRoutines.Add(routine);
          return null;
        };
        return (IEnumerator)RunMethod.Invoke(null, new object[] { entries, start });
      }

      public sealed class Branch
      {
        private static readonly FieldInfo CompletedField = TrackerType?.GetField("Completed");
        private static readonly FieldInfo IdleField = TrackerType?.GetField("IdleWaitingForOthers");

        public Branch(IEnumerator routine, object tracker)
        {
          Routine = routine;
          Tracker = tracker;
        }

        public IEnumerator Routine { get; }
        public object Tracker { get; }

        public void Complete()
        {
          Assert.That(CompletedField, Is.Not.Null);
          CompletedField.SetValue(Tracker, true);
        }

        public void SetIdle(bool idle)
        {
          Assert.That(IdleField, Is.Not.Null, "추적기에 IdleWaitingForOthers 필드가 있어야 합니다.");
          IdleField.SetValue(Tracker, idle);
        }
      }
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

    [TestCase(true, true)]
    [TestCase(false, false)]
    public void MultipleActiveRolesFollowTheRoleBranchGameRule(bool enabled, bool expected)
    {
      var shouldAllow = typeof(ScenarioController).GetMethod(
        "ShouldAllowMultipleActiveRoles",
        BindingFlags.Static | BindingFlags.NonPublic);

      Assert.That(shouldAllow, Is.Not.Null);
      Assert.That(shouldAllow.Invoke(null, new object[] { enabled }), Is.EqualTo(expected));
    }

    [Test]
    public void ActiveRoleBranchesAssignEveryHeldRoleToSinglePlayer()
    {
      var branches = CreateNurseRoleBranches();
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        branches,
        NurseRoleSet(),
        new Dictionary<string, int>
        {
          ["nurse_a"] = 7,
          ["nurse_b"] = 7,
          ["nurse_c"] = 7,
          ["nurse_d"] = 7
        },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.True, error);
      foreach (var branch in branches)
      {
        Assert.That(allocation[branch], Is.EqualTo(7),
          "한 플레이어가 네 역할을 모두 맡으면 네 브랜치가 모두 그 클라이언트에 배정되어야 합니다.");
      }
    }

    [Test]
    public void ActiveRoleBranchesAssignOnlyHeldRolesToSinglePlayer()
    {
      var branches = CreateNurseRoleBranches();
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        branches,
        NurseRoleSet(),
        new Dictionary<string, int>
        {
          ["nurse_a"] = 7,
          ["nurse_c"] = 7
        },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.True, error);
      Assert.That(allocation[branches[0]], Is.EqualTo(7));
      Assert.That(allocation[branches[2]], Is.EqualTo(7));
      Assert.That(allocation[branches[1]], Is.Null,
        "부여되지 않은 역할(nurse_b)의 브랜치는 배정 없이 스킵되어야 합니다.");
      Assert.That(allocation[branches[3]], Is.Null,
        "부여되지 않은 역할(nurse_d)의 브랜치는 배정 없이 스킵되어야 합니다.");
    }

    [Test]
    public void ActiveRoleBranchesFailForAbsentRoleWhenSkipIsDisabled()
    {
      var branches = CreateNurseRoleBranches();
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        branches,
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7 },
        skipAbsentRoleBranches: false,
        allocation,
        out var error);

      Assert.That(assigned, Is.False);
      Assert.That(error, Does.Contain(branches[1].Identifier));
    }

    [Test]
    public void ActiveRoleBranchesFailWhenHolderDoesNotSatisfyBranch()
    {
      var branches = CreateNurseRoleBranches();
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();
      var method = FindTryAssignActiveRoleBranchesMethod();
      var args = new object[]
      {
        branches,
        NurseRoleSet(),
        new Dictionary<string, int>
        {
          ["nurse_a"] = 7,
          ["nurse_b"] = 7,
          ["nurse_c"] = 7,
          ["nurse_d"] = 7
        },
        true,
        (Func<ScenarioParallelBranch, int, bool>)((branch, clientId) => false),
        allocation,
        null
      };

      var assigned = (bool)method.Invoke(null, args);

      Assert.That(assigned, Is.False);
      Assert.That((string)args[6], Does.Contain("does not strictly satisfy"));
    }

    [Test]
    public void ActiveRoleBranchesRequireExactlyOneActiveRoleTag()
    {
      var branch = new ScenarioParallelBranch
      {
        Identifier = "MULTI_ROLE",
        RequiredPlayerTags = new[] { "nurse_a", "nurse_b" }
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        new[] { branch },
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7, ["nurse_b"] = 7 },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.False);
      Assert.That(error, Does.Contain("exactly one activeRoleTag"));
    }

    [Test]
    public void ActiveRoleBranchesAllowMultiRoleFallbackWhenMatchModeIsAny()
    {
      var branch = new ScenarioParallelBranch
      {
        Identifier = "INTUBATION",
        RequiredPlayerTags = new[] { "nurse_b", "nurse_a" },
        RequiredPlayerTagsMatchMode = ScenarioPlayerTagMatchMode.Any
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      // 두 역할 모두 연결되어 있으면 선언 순서상 첫 번째 역할(nurse_b)의 홀더에게 배정한다.
      var assigned = InvokeTryAssignActiveRoleBranches(
        new[] { branch },
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7, ["nurse_b"] = 9 },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.True, error);
      Assert.That(allocation[branch], Is.EqualTo(9));
    }

    [Test]
    public void ActiveRoleBranchesFallBackToLaterRoleWhenFirstHolderIsAbsent()
    {
      var branch = new ScenarioParallelBranch
      {
        Identifier = "INTUBATION",
        RequiredPlayerTags = new[] { "nurse_b", "nurse_a" },
        RequiredPlayerTagsMatchMode = ScenarioPlayerTagMatchMode.Any
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      // nurse_b가 배정되지 않았다면 nurse_a가 브랜치를 대신 수행한다.
      var assigned = InvokeTryAssignActiveRoleBranches(
        new[] { branch },
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7 },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.True, error);
      Assert.That(allocation[branch], Is.EqualTo(7));
    }

    [Test]
    public void ActiveRoleBranchesSkipMultiRoleBranchWhenNoHolderExists()
    {
      var branch = new ScenarioParallelBranch
      {
        Identifier = "IV_LINE",
        RequiredPlayerTags = new[] { "nurse_d", "nurse_c" },
        RequiredPlayerTagsMatchMode = ScenarioPlayerTagMatchMode.Any
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        new[] { branch },
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7, ["nurse_b"] = 7 },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.True, error);
      Assert.That(allocation[branch], Is.Null,
        "어느 선언 역할의 홀더도 없으면 부재 브랜치로 스킵되어야 합니다.");
    }

    [Test]
    public void ActiveRoleBranchesFailMultiRoleBranchWithoutHolderWhenSkipDisabled()
    {
      var branch = new ScenarioParallelBranch
      {
        Identifier = "IV_LINE",
        RequiredPlayerTags = new[] { "nurse_d", "nurse_c" },
        RequiredPlayerTagsMatchMode = ScenarioPlayerTagMatchMode.Any
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        new[] { branch },
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7 },
        skipAbsentRoleBranches: false,
        allocation,
        out var error);

      Assert.That(assigned, Is.False);
      Assert.That(error, Does.Contain("IV_LINE"));
    }

    [Test]
    public void ActiveRoleBranchesStillFailMultiRoleBranchWhenMatchModeIsAll()
    {
      var branch = new ScenarioParallelBranch
      {
        Identifier = "MULTI_ROLE_ALL",
        RequiredPlayerTags = new[] { "nurse_a", "nurse_b" },
        RequiredPlayerTagsMatchMode = ScenarioPlayerTagMatchMode.All
      };
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      var assigned = InvokeTryAssignActiveRoleBranches(
        new[] { branch },
        NurseRoleSet(),
        new Dictionary<string, int> { ["nurse_a"] = 7, ["nurse_b"] = 7 },
        skipAbsentRoleBranches: true,
        allocation,
        out var error);

      Assert.That(assigned, Is.False);
      Assert.That(error, Does.Contain("exactly one activeRoleTag"));
    }

    private static ScenarioParallelBranch[] CreateNurseRoleBranches()
      => new[]
      {
        new ScenarioParallelBranch { Identifier = "A_RECOG_Q", RequiredPlayerTags = new[] { "nurse_a" } },
        new ScenarioParallelBranch { Identifier = "B_VITAL_Q", RequiredPlayerTags = new[] { "nurse_b" } },
        new ScenarioParallelBranch { Identifier = "C_PUPIL_Q", RequiredPlayerTags = new[] { "nurse_c" } },
        new ScenarioParallelBranch { Identifier = "D_OXY_Q", RequiredPlayerTags = new[] { "nurse_d" } }
      };

    private static HashSet<string> NurseRoleSet()
      => new HashSet<string>(StringComparer.Ordinal) { "nurse_a", "nurse_b", "nurse_c", "nurse_d" };

    private static MethodInfo FindTryAssignActiveRoleBranchesMethod()
    {
      var method = typeof(ScenarioController).GetMethod(
        "TryAssignActiveRoleBranches",
        BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(method, Is.Not.Null,
        "ByRole 활성 역할 배정은 테스트 가능한 정적 헬퍼로 분리되어 있어야 합니다.");
      return method;
    }

    private static bool InvokeTryAssignActiveRoleBranches(
      IReadOnlyList<ScenarioParallelBranch> branches,
      ISet<string> activeRoleTags,
      IReadOnlyDictionary<string, int> holderClientIdByRole,
      bool skipAbsentRoleBranches,
      IDictionary<ScenarioParallelBranch, int?> allocation,
      out string error)
    {
      var args = new object[]
      {
        branches,
        activeRoleTags,
        holderClientIdByRole,
        skipAbsentRoleBranches,
        (Func<ScenarioParallelBranch, int, bool>)((branch, clientId) => true),
        allocation,
        null
      };
      var result = (bool)FindTryAssignActiveRoleBranchesMethod().Invoke(null, args);
      error = (string)args[6];
      return result;
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
    public void ParallelBranchDialogueDoesNotAutoAdvance()
    {
      var gameObject = new GameObject("scenario-branch-dialogue-no-auto-advance-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var currentGraph = typeof(ScenarioController).GetField(
        "_currentGraph",
        BindingFlags.Instance | BindingFlags.NonPublic);
      var contextType = typeof(ScenarioController).GetNestedType(
        "BranchChainContext",
        BindingFlags.NonPublic);
      var executeDialogue = typeof(ScenarioController).GetMethod(
        "ExecuteDialogueNodeInBranch",
        BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(currentGraph, Is.Not.Null);
        Assert.That(contextType, Is.Not.Null);
        Assert.That(executeDialogue, Is.Not.Null,
          "Parallel branch dialogues must use the dedicated serialized input-wait executor.");

        currentGraph.SetValue(controller, new ScenarioGraph { Identifier = "branch-dialogue-test" });
        // BranchChainContext 는 private 중첩 타입이지만 생성자 자체는 public 이므로
        // NonPublic 만으로는 바인딩되지 않는다. 시그니처로 직접 찾아 호출한다.
        var contextConstructor = contextType.GetConstructor(
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
          null,
          new[] { typeof(int?) },
          null);
        Assert.That(contextConstructor, Is.Not.Null,
          "BranchChainContext must expose an owner-client constructor.");
        var context = contextConstructor.Invoke(new object[] { null });
        var dialogue = new ScenarioDialogueNode
        {
          Identifier = "DIALOGUE",
          AutoAdvanceSeconds = 0.01f
        };
        var routine = (IEnumerator)executeDialogue.Invoke(controller, new[] { dialogue, context });

        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.Current, Is.TypeOf<WaitUntil>(),
          "Parallel branch dialogues must wait for player input regardless of AutoAdvanceSeconds.");
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
