namespace TriageTrainer.Patient
{
  /// <summary>
  /// 시나리오1의 환자 유형 A에 대한 치료 표시 상태를 정의
  /// </summary>
  public class PatientTypeATreatmentDisplayState : PatientTreatmentDisplayStateABC
  {
    public readonly PatientTreatmentDisplayModel DisplaySupports = new PatientTreatmentDisplayModel
    {
      Syringe18GInsertedIntoLeftArm = true,
      Syringe18GInsertedIntoRightArm = true,
      CentralVenousCatheterInsertedIntoSubclavian = true,
      LaryngoscopeInserted = true,
      EndotrachealTubeStyletInserted = true,
      EndotrachealTubeInsertDone = true,
      AmbuBagAttachedToEndotrachealTube = true,
      GauzePatchedOnThorax = true,
      GauzeDressingDoneOnThorax = true,
      NasalCannulaApplied = true,
      TPieceAttachedToNasalCannula = true,
      CervicalCollarOnNeck = true,
    };

    public PatientTreatmentDisplayModel DisplayState = new PatientTreatmentDisplayModel();
  }
}
