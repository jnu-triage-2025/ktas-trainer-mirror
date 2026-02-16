using FishNet.Object;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController : NetworkBehaviour
  {
    void OnStartClient_Network()
    {
      if (!IsOwner) gameObject.GetComponent<PlayerController>().enabled = false;

      if (IsOwner)
      {
        Registry.Registry.Register(RegistryType.Entity, Registry.Registry.TypeKey<PlayerController>(), this);
        Debug.Log($"[PlayerController] registered into Registry (owner): {this}", this);

        var cam = MainCameraController.Instance ?? Registry.Registry.Get<MainCameraController>(RegistryType.Entity, Registry.Registry.TypeKey<MainCameraController>());
        if (cam != null)
          cam.SetTarget(this);
      }
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      PlayerGamemodeService.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
      PlayerGamemodeService.UnregisterPlayer(this);
      base.OnStopServer();
    }
  }
}
