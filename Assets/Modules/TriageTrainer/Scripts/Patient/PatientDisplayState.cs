using UnityEngine;

namespace TriageTrainer.Patient
{
  public partial class PatientDisplayState : MonoBehaviour
  {
    [Header("Treatment Display")]
    [SerializeField] private PatientTreatmentDisplayModel displaySupports = new PatientTreatmentDisplayModel();
    [SerializeField] private PatientTreatmentDisplayModel displayState = new PatientTreatmentDisplayModel();
    [SerializeField] private GameObject patientModelGameObject;
    [SerializeField] private PatientTreatmentDisplayingChildGameObjects childGameObjects = new PatientTreatmentDisplayingChildGameObjects();

    public ref PatientTreatmentDisplayModel DisplaySupports => ref displaySupports;
    public ref PatientTreatmentDisplayModel DisplayState => ref displayState;
    public GameObject PatientModelGameObject
    {
      get => patientModelGameObject;
      set => patientModelGameObject = value;
    }

    public PatientTreatmentDisplayingChildGameObjects ChildGameObjects
    {
      get => childGameObjects;
      set => childGameObjects = value;
    }
  }
}
