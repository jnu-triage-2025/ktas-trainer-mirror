using FishNet.Object;
using TriageTrainer.Registry;
using UnityEngine;

namespace TriageTrainer.Player
{
  public partial class PlayerController : NetworkBehaviour
  {
    void OnStartClient_Network()
    {
      if (!IsOwner) gameObject.GetComponent<PlayerController>().enabled = false;
      
      CurrentSessionPlayInfoRegistry.Register<PlayerController>(this);
      Debug.Log($"[PlayerController] registered into CurrentSessionRegistry: {this}", this);
    }
  }
}
