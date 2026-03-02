using FishNet;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public static class ItemSpawnUtility
  {
    /// <summary>
    /// 아이템 데이터를 기반으로 월드에 아이템을 스폰합니다.
    /// Registry에 등록된 템플릿을 인스턴스화하고, ApplyRuntimeItemData로 실제 데이터를 반영합니다.
    /// </summary>
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
        error = $"Item '{itemData.identifier}' is not registered and cannot be world-spawned.";
        return false;
      }

      var instance = Object.Instantiate(templateItem.gameObject, position, Quaternion.identity);
      if (instance == null)
      {
        error = "Failed to instantiate item prefab.";
        return false;
      }

      if (!instance.TryGetComponent<Item>(out var itemComponent) || itemComponent == null)
      {
        Object.Destroy(instance);
        error = "Instantiated object is missing Item component.";
        return false;
      }

      itemComponent.ApplyRuntimeItemData(itemData);

      if (instance.TryGetComponent<Rigidbody>(out var rb) && rb != null)
      {
        Vector3 dir = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        rb.AddForce((dir + Vector3.up * 0.2f) * 2.75f, ForceMode.Impulse);
      }

      if (instance.TryGetComponent<NetworkObject>(out var nob) && nob != null)
      {
        var serverManager = InstanceFinder.ServerManager;
        if (serverManager == null || !serverManager.Started)
        {
          Object.Destroy(instance);
          error = "Server is not running. Cannot network-spawn item.";
          return false;
        }

        serverManager.Spawn(nob);
      }

      return true;
    }
  }
}
