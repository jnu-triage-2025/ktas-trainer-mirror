using System.IO;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// 거부 사유 분류와, 클라이언트가 반드시 올릴 수 있어야 하는 상호작용 신호의 인가를 검증한다.
  /// </summary>
  public sealed class ScenarioClientSignalRejectionTests
  {
    [Test]
    public void UndeclaredSignalIsClassifiedAsIgnorable()
    {
      var authorization = new ScenarioClientSignalAuthorization();

      bool allowed = authorization.CanRaise(7, "player-a", "sig.gauze", out _, out var rejection);

      Assert.That(allowed, Is.False);
      Assert.That(rejection, Is.EqualTo(ScenarioClientSignalRejection.Undeclared),
        "게이트로 쓰이지 않는 일상적인 상호작용 신호는 경고나 발신자 통지 없이 무시되어야 합니다.");
    }

    [Test]
    public void ServerOnlySignalRejectionStaysReportable()
    {
      var graph = new ScenarioGraph { Identifier = "server-only-output" };
      graph.ClientSignalIdentifiers = new[] { "sig.apply_gauze_patient_b" };
      graph.Add(new ScenarioEntityStateSignalBindingNode
      {
        Identifier = "treatment-binding",
        OutputSignalIdentifier = "apply_gauze_patient_b"
      });
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(graph);

      bool allowed = authorization.CanRaise(
        7, "player-a", "sig.apply_gauze_patient_b", out _, out var rejection);

      Assert.That(allowed, Is.False);
      Assert.That(rejection, Is.EqualTo(ScenarioClientSignalRejection.ServerOnlySignal),
        "서버 권위 출력 신호의 위조 시도는 계속 경고와 발신자 통지 대상이어야 합니다.");
    }

    [Test]
    public void ClearWithoutCapabilityIsClassifiedAsIgnorable()
    {
      var authorization = new ScenarioClientSignalAuthorization();

      bool allowed = authorization.CanClear(7, "sig.arbitrary", out _, out var rejection);

      Assert.That(allowed, Is.False);
      Assert.That(rejection, Is.EqualTo(ScenarioClientSignalRejection.Undeclared));
    }

    [Test]
    public void PatientACriticalAuthorizesFlowmeterOperationSignals()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(LoadScenario("patient_a_critical"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.interact_oxyflow_wall", out _), Is.True,
        "공용 유량계 조작 신호는 V015_4 게이트 조건입니다.");
      Assert.That(
        authorization.CanRaise(7, "player-a", "sig.interact_oxyflow_wall_oxyflowmeter_zone_1", out _), Is.True,
        "유량계별 조작 신호는 산소 라인 생성과 회수 판정(IsAttachedInteractCompleted)의 근거이므로, "
        + "클라이언트가 조작했을 때에도 기록되어야 합니다.");
    }

    [Test]
    public void PatientACriticalAuthorizesAllTriageZoneEnterSignals()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(LoadScenario("patient_a_critical"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.quest_arrival_triage_area", out _), Is.True,
        "분류 구역 존의 공용 도착 신호는 모든 피어의 물리 트리거 경로에서 발신됩니다.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.arrive_triagearea", out _), Is.True,
        "분류 구역 존의 레거시 공용 도착 신호도 명시적으로 허용해야 합니다.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.quest_arrival_triage_area_player-a", out _), Is.True,
        "플레이어별 도착 신호는 접두사 선언으로 계속 허용해야 합니다.");
    }

    [Test]
    public void PatientACriticalAuthorizesItemSubmissionCompletionSignals()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(LoadScenario("patient_a_critical"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.pass_laryngoscope", out _), Is.True,
        "아이템 제출 완료 신호는 제출 UI 를 조작한 클라이언트가 올리므로 client-origin 입니다. "
        + "서버 전용으로 분류하면 호스트가 아닌 간호사는 인계 단계를 완료할 수 없습니다.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.pass_et_tube_ready", out _), Is.True);
    }

    [Test]
    public void TutorialAuthorizesSubmissionAndNpcInteractionSignals()
    {
      var authorization = new ScenarioClientSignalAuthorization();
      authorization.ConfigureScenario(LoadScenario("tutorial"));

      Assert.That(authorization.CanRaise(7, "player-a", "sig.tutorial.crafting.clock.submitted", out _), Is.True);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.tutorial.delivery.package.submitted", out _), Is.True);
      Assert.That(authorization.CanRaise(7, "player-a", "sig.tutorial_hat_talk_start", out _), Is.True,
        "ActingNpc 상호작용 완료 신호도 상호작용한 클라이언트가 올립니다.");
      Assert.That(authorization.CanRaise(7, "player-a", "sig.not-declared-by-tutorial", out _), Is.False);
    }

    private static ScenarioGraph LoadScenario(string identifier)
    {
      string path = Path.Combine(Application.dataPath, "Modules", "TriageTrainer", "Resources",
        "Scenario", identifier + ".scenario.json");
      Assert.That(File.Exists(path), Is.True, $"Scenario fixture not found: {path}");
      return ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path), validateWithSchema: true);
    }
  }
}
