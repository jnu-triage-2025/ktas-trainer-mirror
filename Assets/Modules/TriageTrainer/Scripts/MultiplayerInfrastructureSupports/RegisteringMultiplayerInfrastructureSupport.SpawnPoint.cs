using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public partial class RegisteringMultiplayerInfrastructureSupport : MonoBehaviour
  {
    private const string DefaultCommonSpawnPointIdentifier = "spawnpoint-commons";

    private void Awake_SpawnPoint()
    {
      Registry.Register(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.DefaultCommonSpawnPoint,
        DefaultCommonSpawnPointIdentifier);
    }
  }
}
