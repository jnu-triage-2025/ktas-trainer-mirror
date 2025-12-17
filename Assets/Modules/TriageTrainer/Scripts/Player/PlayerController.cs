using FishNet.Object;
using TriageTrainer.Camera;
using TriageTrainer.InteractableEntity;
using UnityEngine;

namespace TriageTrainer.Player
{
  [RequireComponent(typeof(InteractableEntityResolver))]
  public partial class PlayerController : NetworkBehaviour
  {
    NearbyInteractablesDetector _interactiveDetector;
    public NearbyInteractablesDetector InteractiveDetector => _interactiveDetector;
    InteractableEntityResolver _interactionResolver;
    public InteractableEntityResolver InteractionResolver => _interactionResolver;
    
    void Awake()
    {
      Awake_Movement();
      Awake_Camera();
      
      _interactionResolver = GetComponent<InteractableEntityResolver>();
    }

    void Start()
    {
      Start_Interactables();
      Start_Inventory();
    }

    void Update()
    {
      Update_Input();
      Update_Movement();
      Update_Interactables();
      Update_Inventory();
    }
    
    public override void OnStartClient()
    {
      base.OnStartClient();
      
      OnStartClient_Network();
    }
  }
}