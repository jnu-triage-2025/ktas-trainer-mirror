using UnityEngine;
using MultiplayerInfrastructure.Player;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public class PlayerCharacterModelBrian : MonoBehaviour, IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter => new Vector3(0, 1, 0);
  }
}
