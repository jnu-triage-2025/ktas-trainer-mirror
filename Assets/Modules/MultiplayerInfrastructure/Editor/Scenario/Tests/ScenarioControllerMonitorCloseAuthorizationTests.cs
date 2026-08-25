using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// B/C 환자 모니터 닫기 완료 승인(CanAcceptPatientBCMonitorClose)이 실행 모드와 무관하게
  /// 동작하는지 검증한다. 그래프에 미지원 노드(EntityPresetSpawn 등)가 있으면 릴레이가 권위
  /// 실행을 거부하고 호환 실행 경로(Local 모드)로 폴백하는데, 이 경로에서도 호스트가 그래프를
  /// 실행하므로 모드 게이트로 거부하면 닫기 완료가 불가능해진다.
  /// </summary>
  public sealed class ScenarioControllerMonitorCloseAuthorizationTests
  {
    private const string CloseSignal = "sig.close_vital_ui_b";

    [Test]
    public void MonitorCloseIsAcceptedInLocalCompatibilityModeWithActiveGraphAndRoleBranch()
    {
      var gameObject = new GameObject("scenario-monitor-close-local-mode-test");
      try
      {
        var controller = gameObject.AddComponent<ScenarioController>();
        SetExecutionMode(controller, "Local");
        SetCurrentGraph(controller, "patient_b_c_ct");
        AddActiveRoleBranch(controller, 0);

        Assert.That(controller.CanAcceptPatientBCMonitorClose(0, CloseSignal), Is.True,
          "호환 실행 경로(Local 모드)에서도 활성 그래프/롤 브랜치/미발행 신호면 승인해야 한다.");
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void MonitorCloseIsAcceptedInServerAuthoritativeMode()
    {
      var gameObject = new GameObject("scenario-monitor-close-authoritative-test");
      try
      {
        var controller = gameObject.AddComponent<ScenarioController>();
        SetExecutionMode(controller, "ServerAuthoritative");
        SetCurrentGraph(controller, "patient_b_c_ct");
        AddActiveRoleBranch(controller, 3);

        Assert.That(controller.CanAcceptPatientBCMonitorClose(3, CloseSignal), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void MonitorCloseIsRejectedForDifferentGraph()
    {
      var gameObject = new GameObject("scenario-monitor-close-other-graph-test");
      try
      {
        var controller = gameObject.AddComponent<ScenarioController>();
        SetCurrentGraph(controller, "patient_a_critical");
        AddActiveRoleBranch(controller, 0);

        Assert.That(controller.CanAcceptPatientBCMonitorClose(0, CloseSignal), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void MonitorCloseIsRejectedWithoutActiveRoleBranch()
    {
      var gameObject = new GameObject("scenario-monitor-close-no-role-branch-test");
      try
      {
        var controller = gameObject.AddComponent<ScenarioController>();
        SetCurrentGraph(controller, "patient_b_c_ct");

        Assert.That(controller.CanAcceptPatientBCMonitorClose(0, CloseSignal), Is.False,
          "활성 역할 브랜치에 참여하지 않은 발신자는 승인하지 않는다.");
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }

    [Test]
    public void MonitorCloseIsRejectedWhenSignalIsAlreadyRaised()
    {
      var gameObject = new GameObject("scenario-monitor-close-already-raised-test");
      try
      {
        Registry.Registry.Register(RegistryType.RuntimeState, CloseSignal, true);
        var controller = gameObject.AddComponent<ScenarioController>();
        SetCurrentGraph(controller, "patient_b_c_ct");
        AddActiveRoleBranch(controller, 0);

        Assert.That(controller.CanAcceptPatientBCMonitorClose(0, CloseSignal), Is.False,
          "이미 발행된 신호의 이중 완료는 승인하지 않는다.");
      }
      finally
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, CloseSignal);
        Object.DestroyImmediate(gameObject);
      }
    }

    private static void SetExecutionMode(ScenarioController controller, string modeName)
    {
      var field = typeof(ScenarioController).GetField(
        "_executionMode", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null);
      field.SetValue(controller, System.Enum.Parse(field.FieldType, modeName));
    }

    private static void SetCurrentGraph(ScenarioController controller, string graphIdentifier)
    {
      var field = typeof(ScenarioController).GetField(
        "_currentGraph", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null);
      field.SetValue(controller, new ScenarioGraph { Identifier = graphIdentifier });
    }

    private static void AddActiveRoleBranch(ScenarioController controller, int clientId)
    {
      var field = typeof(ScenarioController).GetField(
        "_activeRoleBranchDepthByClientId", BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null);
      var dictionary = (Dictionary<int, int>)field.GetValue(controller);
      dictionary[clientId] = 1;
    }
  }
}
