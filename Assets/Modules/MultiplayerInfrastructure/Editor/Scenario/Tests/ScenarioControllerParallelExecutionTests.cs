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

    private static IEnumerator Probe(Action onMoveNext)
    {
      onMoveNext?.Invoke();
      yield return null;
    }
  }
}
