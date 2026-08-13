using UnityEngine;
using TriageTrainer.Entity;

namespace TriageTrainer.Patient
{
  public class PatientTypeBFemaleState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeBFemaleTreatmentDisplayState treatmentDisplayState = new PatientTypeBFemaleTreatmentDisplayState();

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;
    public override bool RestoresLegacyPatientControllerDefaultsOnInspectorReset => true;

    protected override void ConfigureRuntimeReferences(PatientController controller)
    {
      controller.SetOxygenMaskAttachmentPointFromPatientComponent(FindSingleOxygenInterface());
    }
  }
}
