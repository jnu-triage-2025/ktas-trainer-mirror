using UnityEngine;
using MultiplayerInfrastructure.Player;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public class PlayerCharacterModelEthan : MonoBehaviour, IPlayerCharacterModelObject
  {
    public Vector3 CharacterControllerCenter => new Vector3(0, 1, 0);
  }
}
