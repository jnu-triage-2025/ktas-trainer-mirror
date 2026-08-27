using System.Collections;
using MultiplayerInfrastructure.Quest;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// `patient_a_critical` 단계별 상호작용 개방 이벤트.
  ///
  /// <para>
  /// 개방은 환자 엔티티의 활성 플래그가 아니라 플레이어별 퀘스트 상태 플래그 풀에 기록한다. 환자
  /// 인스턴스는 모든 플레이어가 공유하므로, 엔티티 활성 플래그로 열면 담당이 아닌 플레이어에게도
  /// 같은 상호작용이 노출되고 서버에서 켠 값은 원격 클라이언트로 복제되지도 않는다. 플래그 풀은
  /// 서버 권위로 기록되어 전 피어에 복제되고, 판정은 각 피어가 자기 플레이어 기준으로 수행한다.
  /// </para>
  ///
  /// <para>
  /// 역할 브랜치 안에서 실행되는 이벤트는 그 브랜치의 `requiredPlayerTags` 와 같은 역할에만 건다.
  /// 메인 흐름에서 실행되는 이벤트는 이후 병렬 브랜치가 역할을 다시 배정하므로 전원에게 건다.
  /// </para>
  /// </summary>
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ActivatePatientAStageInteractions()
    {
      Register("activate_patient_a_vital_assess", Event_ActivatePatientAVitalAssess);
      Register("activate_patient_a_avpu_gcs_assess", Event_ActivatePatientAAvpuGcsAssess);
      Register("activate_patient_a_arrest_actions", Event_ActivatePatientAArrestActions);
      Register("activate_patient_a_stylet_removal", Event_ActivatePatientAStyletRemoval);
      Register("activate_patient_a_tpiece_attach", Event_ActivatePatientATpieceAttach);
      Register("activate_patient_a_cpr2_actions", Event_ActivatePatientACpr2Actions);
      Register("activate_patient_a_clothing_removal", Event_ActivatePatientAClothingRemoval);
    }

    /// <summary>P003 의 `nurse_b` 브랜치. 활력징후 사정을 담당 간호사에게만 연다.</summary>
    private IEnumerator Event_ActivatePatientAVitalAssess()
    {
      PlayerQuestStateFlagService.SetForTag(
        "nurse_b", PatientACriticalQuestStateFlags.VitalAssess);
      yield break;
    }

    /// <summary>P003 의 `nurse_c` 브랜치. 의식상태 사정을 담당 간호사에게만 연다.</summary>
    private IEnumerator Event_ActivatePatientAAvpuGcsAssess()
    {
      PlayerQuestStateFlagService.SetForTag(
        "nurse_c", PatientACriticalQuestStateFlags.AvpuGcsAssess);
      yield break;
    }

    /// <summary>메인 흐름. CPR 1주기 역할은 이후 병렬 노드가 배정하므로 전원에게 연다.</summary>
    private IEnumerator Event_ActivatePatientAArrestActions()
    {
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.ArrestPulseAssess);
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.Cpr1Actions);
      yield break;
    }

    /// <summary>P004 의 삽관 보조 브랜치(`nurse_b` 또는 `nurse_a`).</summary>
    private IEnumerator Event_ActivatePatientAStyletRemoval()
    {
      PlayerQuestStateFlagService.SetForAnyTag(
        StyletRemovalRoleTags, PatientACriticalQuestStateFlags.StyletRemoval);
      yield break;
    }

    /// <summary>P004 의 `nurse_a` 브랜치. T-piece 산소 연결.</summary>
    private IEnumerator Event_ActivatePatientATpieceAttach()
    {
      PlayerQuestStateFlagService.SetForTag(
        "nurse_a", PatientACriticalQuestStateFlags.TpieceAttach);
      yield break;
    }

    /// <summary>메인 흐름. CPR 2주기 역할은 이후 병렬 노드가 배정하므로 전원에게 연다.</summary>
    private IEnumerator Event_ActivatePatientACpr2Actions()
    {
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.Cpr2Actions);
      yield break;
    }

    /// <summary>메인 흐름. 의복 제거.</summary>
    private IEnumerator Event_ActivatePatientAClothingRemoval()
    {
      PlayerQuestStateFlagService.SetForAll(PatientACriticalQuestStateFlags.ClothingRemoval);
      yield break;
    }

    /// <summary>P004 의 `Q010` 브랜치가 요구하는 역할(matchMode: Any)과 같은 목록이다.</summary>
    private static readonly string[] StyletRemovalRoleTags = { "nurse_b", "nurse_a" };
  }
}
