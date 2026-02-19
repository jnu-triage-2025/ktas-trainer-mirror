using FishNet;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public static class ItemSpawnUtility
  {
    public static bool TrySpawnDroppedItem(ItemData itemData, Vector3 position, Vector3 forward, out string error)
    {
      error = string.Empty;

      if (itemData == null || !itemData.IsValid() || itemData.currCount <= 0)
      {
        error = "Invalid item data.";
        return false;
      }

      if (!Registry.Registry.TryGet<Item>(RegistryType.Item, itemData.identifier, out var templateItem) || templateItem == null)
      {
        error = $"Item '{itemData.identifier}' is not available for world spawn.";
        return false;
      }

      var instance = Object.Instantiate(templateItem.gameObject, position, Quaternion.identity);
      if (instance == null)
      {
        error = "Failed to instantiate item object.";
        return false;
      }

      if (!instance.TryGetComponent<Item>(out var itemComponent) || itemComponent == null)
      {
        Object.Destroy(instance);
        error = "Instantiated object is missing Item component.";
        return false;
      }

      itemComponent.ApplyRuntimeItemData(itemData);

      if (instance.TryGetComponent<Rigidbody>(out var rigidbody) && rigidbody != null)
      {
        Vector3 impulseDirection = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        rigidbody.AddForce((impulseDirection + Vector3.up * 0.2f) * 2.75f, ForceMode.Impulse);
      }

      if (instance.TryGetComponent<NetworkObject>(out var networkObject) && networkObject != null)
      {
        var serverManager = InstanceFinder.ServerManager;
        if (serverManager == null || !serverManager.Started)
        {
          Object.Destroy(instance);
          error = "Server is not running; cannot spawn networked item.";
          return false;
        }

        serverManager.Spawn(networkObject);
      }

      return true;
    }
  }
}
