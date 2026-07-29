using FishNet.Object;
using UnityEngine;

namespace TriageTrainer.Entity.OxyLine
{
  /// <summary>
  /// Marks the position where an oxygen line can be connected.
  /// Connection behavior will be implemented separately.
  /// </summary>
  public sealed class OxyLineConnectionPoint : NetworkBehaviour
  {
    /// <summary>
    /// Returns the NetworkObject that owns this point, including a parent object.
    /// This keeps the point usable as a child marker of a networked prefab.
    /// </summary>
    public NetworkObject OwningNetworkObject => GetComponentInParent<NetworkObject>();
  }
}
