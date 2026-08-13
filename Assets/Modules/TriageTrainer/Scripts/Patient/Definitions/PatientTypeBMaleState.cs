using UnityEngine;
using TriageTrainer.Entity;

namespace TriageTrainer.Patient
{
  public class PatientTypeBMaleState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeBMaleTreatmentDisplayState treatmentDisplayState = new PatientTypeBMaleTreatmentDisplayState();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;

    protected override void ConfigureRuntimeReferences(PatientController controller)
    {
      controller.SetOxygenMaskAttachmentPointFromPatientComponent(FindSingleOxygenInterface());
    }
  }
}
