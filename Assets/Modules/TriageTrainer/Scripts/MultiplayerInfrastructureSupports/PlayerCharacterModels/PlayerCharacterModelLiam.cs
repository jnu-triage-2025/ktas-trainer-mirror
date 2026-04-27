using UnityEngine;
using MultiplayerInfrastructure.Player;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public class PlayerCharacterModelLiam : MonoBehaviour, IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter => new Vector3(0, 1, 0);
  }
}
