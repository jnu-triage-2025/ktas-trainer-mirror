using UnityEngine;
using TriageTrainer.Entity;

namespace TriageTrainer.Patient
{
  public class PatientTypeBFemaleState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeBFemaleTreatmentDisplayState treatmentDisplayState = new PatientTypeBFemaleTreatmentDisplayState();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;

    protected override void ConfigureRuntimeReferences(PatientController controller)
    {
      controller.SetOxygenMaskAttachmentPointFromPatientComponent(FindSingleOxygenInterface());
    }
  }
}
