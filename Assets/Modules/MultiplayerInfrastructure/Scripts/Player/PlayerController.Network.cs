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
        CurrentSessionPlayInfoRegistry.Register<PlayerController>(this);
        Debug.Log($"[PlayerController] registered into CurrentSessionRegistry (owner): {this}", this);

        var cam = MainCameraController.Instance ?? CurrentSessionPlayInfoRegistry.Get<MainCameraController>();
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
