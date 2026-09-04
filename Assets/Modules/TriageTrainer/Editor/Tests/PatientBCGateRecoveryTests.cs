using System.IO;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class PatientBCGateRecoveryTests
  {
    [Test]
    public void CollaborativeWaitGatesForceAdvanceAfterTheirTimeout()
    {
      var path = Path.Combine(Application.dataPath,
        "Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.scenario.json");
      var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
      var gates = graph.Nodes.Values
        .OfType<ScenarioValidatorNode>()
        .Where(node => node.WaitForCondition)
        .ToArray();

      Assert.That(gates, Is.Not.Empty);
      Assert.That(gates.All(node => node.WaitTimeoutSeconds is > 0f), Is.True,
        "협업 게이트에는 무기한 대기를 방지하는 양수 타임아웃이 필요합니다.");
      Assert.That(gates.All(node => node.OnWaitTimeout == ScenarioValidatorWaitTimeoutBehavior.ForceAdvance), Is.True,
        "신호 누락·권한 거부·담당자 이탈 이후에도 다른 참여자의 진행을 막지 않아야 합니다.");
    }
  }
}
