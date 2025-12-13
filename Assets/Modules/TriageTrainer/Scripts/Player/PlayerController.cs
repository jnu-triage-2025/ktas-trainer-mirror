using FishNet.Object;
using TriageTrainer.Camera;
using TriageTrainer.InteractableEntity;
using UnityEngine;

namespace TriageTrainer.Player
{
  [RequireComponent(typeof(InteractableEntityResolver))]
  [RequireComponent(typeof(PlayerInteractiveDetector))]
  public partial class PlayerController : NetworkBehaviour
  {
    PlayerInteractiveDetector _interactiveDetector;
    public PlayerInteractiveDetector InteractiveDetector => _interactiveDetector;
    InteractableEntityResolver _interactionResolver;
    public InteractableEntityResolver InteractionResolver => _interactionResolver;
    
    void Awake()
    {
      Awake_Movement();
      Awake_Camera();
      
      _interactiveDetector = GetComponent<PlayerInteractiveDetector>();
      _interactionResolver = GetComponent<InteractableEntityResolver>();
    }

    void Update()
    {
      Update_Movement();
    }
    
    public override void OnStartClient()
    {
      base.OnStartClient();
      
      OnStartClient_Network();
    }
  }
}