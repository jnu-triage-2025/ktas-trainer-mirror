using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public interface IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter { get; }
    public Animator Animator { get; }
    public Transform HeldItemAttachPoint { get; }
  }
}
