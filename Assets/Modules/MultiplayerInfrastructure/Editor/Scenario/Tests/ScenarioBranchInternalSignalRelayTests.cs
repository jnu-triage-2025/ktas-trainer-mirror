using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 역할 브랜치가 올린 서버 내부 신호 Resolve 를 다른 피어에 반영하는 경로와,
  /// 브랜치 취소 시 Register 대기가 풀리는 동작을 검증한다.
  /// </summary>
  public sealed class ScenarioBranchInternalSignalRelayTests
  {
    private const string GraphIdentifier = "branch-internal-signal-test";
    private const string SignalIdentifier = "test.branch.internal";

    [SetUp]
    public void SetUp() => ScenarioServerInternalSignalRegistry.ClearAll();

    [TearDown]
    public void TearDown() => ScenarioServerInternalSignalRegistry.ClearAll();

    [Test]
    public void PeerResolveReleasesPendingRegister()
    {
      var gameObject = new GameObject("scenario-branch-internal-signal-peer-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var currentGraph = typeof(ScenarioController).GetField(
        "_currentGraph", BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(currentGraph, Is.Not.Null);
        currentGraph.SetValue(controller, new ScenarioGraph { Identifier = GraphIdentifier });

        bool resolved = false;
        ScenarioServerInternalSignalRegistry.Register("@m", SignalIdentifier, () => resolved = true);

        controller.ResolveBranchInternalSignalFromPeer("other-graph", "@m", SignalIdentifier, 42);
        Assert.That(resolved, Is.False,
          "다른 그래프를 지목한 Resolve 는 이 피어의 레지스트리에 반영되면 안 됩니다.");

        // 테스트 환경에는 클라이언트 연결이 없으므로 로컬 클라이언트 식별자는 int.MinValue 다.
        controller.ResolveBranchInternalSignalFromPeer(GraphIdentifier, "@m", SignalIdentifier, int.MinValue);
        Assert.That(resolved, Is.False,
          "발신 피어 자신은 이미 브랜치 안에서 Resolve 를 실행했으므로 건너뛰어야 합니다.");

        controller.ResolveBranchInternalSignalFromPeer(GraphIdentifier, "@m", SignalIdentifier, 42);
        Assert.That(resolved, Is.True,
          "다른 피어의 역할 브랜치가 올린 Resolve 는 이 피어에서 기다리는 Register 를 풀어야 합니다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void BranchRegisterWaitEndsWhenBranchIsCancelled()
    {
      var gameObject = new GameObject("scenario-branch-internal-signal-cancel-test");
      var controller = gameObject.AddComponent<ScenarioController>();
      var currentGraph = typeof(ScenarioController).GetField(
        "_currentGraph", BindingFlags.Instance | BindingFlags.NonPublic);
      var cancelledClients = typeof(ScenarioController).GetField(
        "_cancelledBranchClientIds", BindingFlags.Instance | BindingFlags.NonPublic);
      var contextType = typeof(ScenarioController).GetNestedType(
        "BranchChainContext", BindingFlags.NonPublic);
      var execute = typeof(ScenarioController).GetMethod(
        "ExecuteServerInternalSignalNode", BindingFlags.Instance | BindingFlags.NonPublic);

      try
      {
        Assert.That(currentGraph, Is.Not.Null);
        Assert.That(cancelledClients, Is.Not.Null);
        Assert.That(contextType, Is.Not.Null);
        Assert.That(execute, Is.Not.Null);

        currentGraph.SetValue(controller, new ScenarioGraph { Identifier = GraphIdentifier });
        var contextConstructor = contextType.GetConstructor(
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
          null,
          new[] { typeof(int?), typeof(bool) },
          null);
        Assert.That(contextConstructor, Is.Not.Null,
          "BranchChainContext must expose an owner-client constructor.");
        const int ownerClientId = 7;
        var context = contextConstructor.Invoke(new object[] { (int?)ownerClientId, true });
        var node = new ScenarioServerInternalSignalNode
        {
          Identifier = "WAIT_INTERNAL",
          TargetIdentifier = "@m",
          SignalIdentifier = SignalIdentifier,
          Operation = ScenarioServerInternalSignalOperationType.Register,
          WaitForResolution = true
        };

        var routine = (IEnumerator)execute.Invoke(controller, new[] { node, context });
        Assert.That(routine.MoveNext(), Is.True,
          "담당자가 활성 상태이면 Resolve 가 올 때까지 대기해야 합니다.");

        ((HashSet<int>)cancelledClients.GetValue(controller)).Add(ownerClientId);
        Assert.That(routine.MoveNext(), Is.False,
          "담당자가 이탈해 취소된 브랜치의 내부 신호 대기는 즉시 끝나야 합니다.");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void DeclaredResolveIsRecognisedOnlyForMatchingResolveNode()
    {
      var isDeclared = typeof(ScenarioNetworkRelay).GetMethod(
        "IsDeclaredInternalSignalResolve",
        BindingFlags.Static | BindingFlags.NonPublic,
        null,
        new[] { typeof(ScenarioGraph), typeof(string), typeof(string) },
        null);
      Assert.That(isDeclared, Is.Not.Null);

      var graph = new ScenarioGraph { Identifier = GraphIdentifier };
      graph.Add(new ScenarioServerInternalSignalNode
      {
        Identifier = "WAIT_INTERNAL",
        TargetIdentifier = "@m",
        SignalIdentifier = SignalIdentifier,
        Operation = ScenarioServerInternalSignalOperationType.Register
      });

      Assert.That((bool)isDeclared.Invoke(null, new object[] { graph, "@m", SignalIdentifier }), Is.False,
        "Register 노드만 있는 신호는 클라이언트 발 Resolve 를 받아 주면 안 됩니다.");

      graph.Add(new ScenarioServerInternalSignalNode
      {
        Identifier = "SIG_INTERNAL",
        TargetIdentifier = null,
        SignalIdentifier = " " + SignalIdentifier + " ",
        Operation = ScenarioServerInternalSignalOperationType.Resolve
      });

      Assert.That((bool)isDeclared.Invoke(null, new object[] { graph, "@m", SignalIdentifier }), Is.True,
        "비어 있는 대상은 서버 대상(@m)으로 정규화되고 신호 식별자의 공백은 무시되어야 합니다.");
      Assert.That((bool)isDeclared.Invoke(null, new object[] { graph, "@s", SignalIdentifier }), Is.False,
        "다른 대상을 지목한 Resolve 는 선언된 것으로 보면 안 됩니다.");
      Assert.That((bool)isDeclared.Invoke(null, new object[] { graph, "@m", "test.branch.other" }), Is.False);
    }

    [Test]
    public void InternalSignalWaitTimeoutRoundTripsThroughJson()
    {
      var graph = new ScenarioGraph { Identifier = "internal-signal-timeout-round-trip", DefaultEntrypoint = "wait" };
      graph.Add(new ScenarioServerInternalSignalNode
      {
        Identifier = "wait",
        SignalIdentifier = SignalIdentifier,
        Operation = ScenarioServerInternalSignalOperationType.Register,
        WaitForResolution = true,
        WaitTimeoutSeconds = 600f,
        NextIdentifier = "wait_forever"
      });
      graph.Add(new ScenarioServerInternalSignalNode
      {
        Identifier = "wait_forever",
        SignalIdentifier = SignalIdentifier,
        Operation = ScenarioServerInternalSignalOperationType.Register,
        WaitForResolution = true,
        WaitTimeoutSeconds = 0f
      });

      string json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);

      Assert.That(((ScenarioServerInternalSignalNode)reloaded.Nodes["wait"]).WaitTimeoutSeconds, Is.EqualTo(600f),
        "양수 타임아웃은 저장과 로드를 거쳐도 유지되어야 합니다.");
      Assert.That(((ScenarioServerInternalSignalNode)reloaded.Nodes["wait_forever"]).WaitTimeoutSeconds, Is.Null,
        "0 이하의 타임아웃은 무한 대기(null)로 정규화되어야 합니다.");
    }
  }
}
