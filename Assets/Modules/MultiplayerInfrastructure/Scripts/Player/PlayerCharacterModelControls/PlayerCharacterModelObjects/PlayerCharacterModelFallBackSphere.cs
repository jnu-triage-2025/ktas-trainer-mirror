using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public class PlayerCharacterModelFallBackSphere : MonoBehaviour, IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter => Vector3.zero;
    public Animator Animator => null;
    public Transform HeldItemAttachPoint => null;
  }
}
