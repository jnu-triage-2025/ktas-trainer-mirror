using FishNet.Object;
using UnityEngine;

namespace TriageTrainer.Entity.AEDLine
{
  /// <summary>
  /// Marks the position where an AED line can be connected.
  /// This component is network-aware; connection behavior will be implemented separately.
  /// </summary>
  public sealed class AEDLineConnectionPoint : NetworkBehaviour
  {
    /// <summary>
    /// Returns the NetworkObject that owns this point, including a parent object.
    /// </summary>
    public NetworkObject OwningNetworkObject => GetComponentInParent<NetworkObject>();
  }
}
