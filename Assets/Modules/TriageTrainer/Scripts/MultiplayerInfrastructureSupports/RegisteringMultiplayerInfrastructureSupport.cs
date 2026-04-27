using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public partial class RegisteringMultiplayerInfrastructureSupport : MonoBehaviour
  {
    private void Awake()
    {
      Awake_Item();
      Awake_PlayerModel();
    }
  }
}
