using FishNet.Connection;
using FishNet.Object;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [TargetRpc]
    internal void TargetGrantCommandItem(NetworkConnection connection, string itemIdentifier, int count)
    {
      if (!IsOwner || count <= 0)
        return;
      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
        return;
      item.CurrentStackCount = count;
      TryAddItemToInventory(item, out var leftover);
      if (leftover != null && leftover.CurrentStackCount > 0)
        TryDropItemInFront(leftover);
    }
  }
}
