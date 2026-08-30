using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public class PlayerCharacterModelObjectExample : MonoBehaviour, IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter => Vector3.zero;
    public Animator Animator => null;
    public PlayerCharacterModelAnimatorControllerObject AnimatorControllerObject => null;
    public Transform HeldItemAttachPoint => null;
    public PlayerCharacterModelHoldingItemAttachPoint HeldItemAttachPointObject => null;
  }
}
