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
    // 기능 구현 예약: AEDLine 연결 동작은 별도 작업자가 담당한다.
    /// <summary>
    /// Returns the NetworkObject that owns this point, including a parent object.
    /// </summary>
    public NetworkObject OwningNetworkObject => GetComponentInParent<NetworkObject>();
  }
}
