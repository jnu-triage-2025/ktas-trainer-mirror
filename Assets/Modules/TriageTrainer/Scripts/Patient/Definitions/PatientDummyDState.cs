using UnityEngine;

namespace TriageTrainer.Patient
{
  /// <summary>
  /// 시나리오의 분류 전용 D 더미 환자 상태입니다.
  /// </summary>
  public class PatientDummyDState : PatientStateABC
  {
    [SerializeField]
    private PatientDummyDTreatmentDisplayState treatmentDisplayState = new();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;
  }
}
