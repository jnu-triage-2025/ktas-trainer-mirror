using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public interface IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter { get; }
    public Animator Animator { get; }
    public PlayerCharacterModelAnimatorControllerObject AnimatorControllerObject { get; }
    public Transform HeldItemAttachPoint { get; }
    public PlayerCharacterModelHoldingItemAttachPoint HeldItemAttachPointObject { get; }
  }
}
