using FishNet.Object;
using TriageTrainer.Scripts.Connection;
using UnityEngine;

namespace TriageTrainer.Scripts.Player
{
  public partial class PlayerController : NetworkBehaviour
  {
    void OnStartClient_Network()
    {
      if (!IsOwner) gameObject.GetComponent<PlayerController>().enabled = false;
      
      CurrentSessionPlayInfoRegistry.Instance.RegisterPlayerController(this);
      CurrentSessionPlayInfoRegistry.Instance.RegisterLocalCameraHolder(this.transform);
      Debug.Log($"PlayerController OnStartClient_Network registered: {this}", this);
    }
  }
}
