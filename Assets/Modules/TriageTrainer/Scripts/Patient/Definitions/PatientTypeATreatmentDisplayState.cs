using System;
using UnityEngine;

namespace TriageTrainer.Patient
{
  /// <summary>
  /// 시나리오1의 환자 유형 A에 대한 치료 표시 상태를 정의
  /// </summary>
  [Serializable]
  public class PatientTypeATreatmentDisplayState : PatientTreatmentDisplayStateABC
  {
    [field: SerializeField]
    public override PatientTreatmentDisplayModel DisplaySupports { get; set; } = new PatientTreatmentDisplayModel
    {
      Syringe18GInsertedIntoLeftArm = true,
      Syringe18GInsertedIntoRightArm = true,
      CentralVenousCatheterInsertedIntoSubclavian = true,
      LaryngoscopeInserted = true,
      EndotrachealTubeStyletInserted = true,
      EndotrachealTubeInsertDone = true,
      TPieceAttachedToNasalCannula = true,
      AmbuBagAttachedToEndotrachealTube = true,
      GauzePatchedOnThorax = true,
      GauzeDressingDoneOnThorax = true,
      NasalCannulaApplied = true,
      CervicalCollarOnNeck = true,
    };

    [field: SerializeField]
    public override PatientTreatmentDisplayModel DisplayState { get; set; } = new PatientTreatmentDisplayModel();

    [field: SerializeField]
    public override GameObject PatientModelGameObject { get; set; }

    [field: SerializeField]
    public override PatientTreatmentDisplayingChildGameObjects ChildGameObjects { get; set; } = new PatientTreatmentDisplayingChildGameObjects();
  }
}
