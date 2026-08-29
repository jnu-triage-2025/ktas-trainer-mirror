using System.Collections;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string StartChestCompressionEventIdentifier = "start_chest_compression";

    // 시나리오 그래프가 참조하지 않는 연출 전용 이벤트다. 권위 피어가 판정한 CPR 라운드를
    // 표시 피어에 전달하기 위한 통지 경로로만 사용한다.
    private const string PatientACprRoundOnePresentationEventIdentifier =
      "present_patient_a_cpr_round_one";
    private const string PatientACprRoundTwoPresentationEventIdentifier =
      "present_patient_a_cpr_round_two";

    private void RegisterEvent_StartChestCompression()
    {
      Register(StartChestCompressionEventIdentifier, Event_StartChestCompression);
      Register(PatientACprRoundOnePresentationEventIdentifier, Event_PresentPatientACprRoundOne);
      Register(PatientACprRoundTwoPresentationEventIdentifier, Event_PresentPatientACprRoundTwo);
    }

    private IEnumerator Event_StartChestCompression()
    {
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, true);
      PlayPatientAChestCompressionAnimation();

      // E028/E033 은 역할 브랜치 밖의 InvokeEvent 노드라서 그래프를 순회하는 권위 피어에서만
      // 실행된다. 통지하지 않으면 가슴압박에 상호작용한 피어 외에는 CPR 연출을 볼 수 없다.
      // 라운드마다 가슴압박 역할이 다른데 표시 피어에는 이 노드가 CurrentNode 로 남지 않으므로,
      // 라운드 판정을 표시 피어에 맡기지 않고 라운드별 연출 이벤트로 구분해서 통지한다.
      ScenarioNetworkRelay.InvokePresentationEventAuthoritative(
        ResolvePatientACprRoundPresentationEventIdentifier());
      Debug.Log("[EmitSystemMessage] 가슴 압박 애니메이션을 시작했습니다.");
      yield break;
    }

    private IEnumerator Event_PresentPatientACprRoundOne()
      => PresentPatientACprRound(PatientACprRoundOneNurseTag);

    private IEnumerator Event_PresentPatientACprRoundTwo()
      => PresentPatientACprRound(PatientACprRoundTwoNurseTag);

    /// <summary>
    /// 표시 전용 피어에서 CPR 연출만 재생한다. 그래프 진행은 권위 피어가 담당하므로 이 경로는
    /// 시나리오 상태를 바꾸지 않는다.
    /// </summary>
    private IEnumerator PresentPatientACprRound(string nurseTag)
    {
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, true);
      PlayPatientAChestCompressionAnimation(nurseTag);
      yield break;
    }
  }
}
