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

    [Header("Attachment Display")]
    [SerializeField] private bool intravenousStandAttached;
    [SerializeField] private GameObject intravenousStandReference;
    [SerializeField] private bool intravenousHangerAttached;
    [SerializeField] private GameObject intravenousHangerReference;
    [SerializeField] private bool intravenousFluidAttached;
    [SerializeField] private GameObject intravenousFluidReference;

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

    public bool IntravenousStandAttached
    {
      get => intravenousStandAttached;
      set => intravenousStandAttached = value;
    }

    public GameObject IntravenousStandReference
    {
      get => intravenousStandReference;
      set => intravenousStandReference = value;
    }

    public bool IntravenousHangerAttached
    {
      get => intravenousHangerAttached;
      set => intravenousHangerAttached = value;
    }

    public GameObject IntravenousHangerReference
    {
      get => intravenousHangerReference;
      set => intravenousHangerReference = value;
    }

    public bool IntravenousFluidAttached
    {
      get => intravenousFluidAttached;
      set => intravenousFluidAttached = value;
    }

    public GameObject IntravenousFluidReference
    {
      get => intravenousFluidReference;
      set => intravenousFluidReference = value;
    }
  }
}
