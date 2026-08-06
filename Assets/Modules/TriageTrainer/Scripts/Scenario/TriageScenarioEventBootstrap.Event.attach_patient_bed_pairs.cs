using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 프리셋 그룹 스폰(환자+침대 분리) 직후, 위계 없는 두 독립 객체를 "결합 상태"로 만드는 핸들러.
  ///
  /// 시나리오가 InvokeEvent "attach_patient_bed_pairs" 를 호출하면, 인스펙터에 구성된
  /// (환자 식별자, 침대 식별자) 쌍들에 대해 레지스트리에서 객체를 찾아 bed.TryReposeTarget(patient) 로
  /// 논리 결합을 재설정한다.
  /// </summary>
  public partial class TriageScenarioEventBootstrap : MonoBehaviour
  {
    [Serializable]
    public struct PatientBedPair
    {
      [Tooltip("결합할 환자 엔티티 식별자(예: patient_a)")]
      public string patientIdentifier;

      [Tooltip("결합할 침대 엔티티 식별자(예: bed_a)")]
      public string bedIdentifier;
    }

    [Header("attach_patient_bed_pairs")]
    [SerializeField] private List<PatientBedPair> _patientBedPairs = new();

    private void RegisterEvent_AttachPatientBedPairs()
    {
      Register("attach_patient_bed_pairs", Event_AttachPatientBedPairs);
    }

    private IEnumerator Event_AttachPatientBedPairs()
    {
      ResolveRuntimeReferencesIfNeeded();

      var pairs = _patientBedPairs;
      if (pairs == null || pairs.Count == 0)
      {
        pairs = new List<PatientBedPair>
        {
          new() { patientIdentifier = "patient_b", bedIdentifier = "bed_b" },
          new() { patientIdentifier = "patient_c", bedIdentifier = "bed_c" },
          new() { patientIdentifier = "patient_dummy_d_b", bedIdentifier = "bed_d_b" }
        };
      }

      int attached = 0;
      foreach (var pair in pairs)
      {
        if (TryAttachPatientToBedByIdentifier(pair.patientIdentifier, pair.bedIdentifier))
        {
          attached++;
        }
      }

      EmitSystemMessage($"환자-침대 결합 재설정 완료 ({attached}/{pairs.Count}).");

      yield break;
    }
  }
}
