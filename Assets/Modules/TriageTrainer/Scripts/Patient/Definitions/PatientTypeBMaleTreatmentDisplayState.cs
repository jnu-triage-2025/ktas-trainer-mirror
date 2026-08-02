using System;
using UnityEngine;

namespace TriageTrainer.Patient
{
  /// <summary>
  /// 시나리오1의 환자 유형 B(남성)에 대한 치료 표시 상태를 정의
  /// </summary>
  [Serializable]
  public class PatientTypeBMaleTreatmentDisplayState : PatientTreatmentDisplayStateABC
  {
    [field: SerializeField]
    public override PatientTreatmentDisplayModel DisplaySupports { get; set; } = new PatientTreatmentDisplayModel
    {
      Syringe20GInsertedIntoRightArm = true,
      NasalCannulaApplied = true,
      GauzePatchedOnLeftArm = true,
      GauzeDressingDoneOnLeftArm = true,
      GauzePatchedOnLeftEyebrow = true,
      GauzeDressingDoneOnLeftEyebrow = true,
    };

    [field: SerializeField]
    public override PatientTreatmentDisplayModel DisplayState { get; set; } = new PatientTreatmentDisplayModel();

    [field: SerializeField]
    public override GameObject PatientModelGameObject { get; set; }

    [field: SerializeField]
    public override PatientTreatmentDisplayingChildGameObjects ChildGameObjects { get; set; } = new PatientTreatmentDisplayingChildGameObjects();
  }
}
