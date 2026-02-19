using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item
  {
    public void ApplyRuntimeItemData(ItemData data)
    {
      if (data == null)
        return;

      _itemData = new ItemData(data);
      _itemIdentifier = _itemData.identifier;

      if (_itemData != null && !_itemData.HasIcon)
      {
        string textureIdentifier = string.IsNullOrWhiteSpace(_itemData.ItemTextureIdentifier)
          ? _itemData.identifier
          : _itemData.ItemTextureIdentifier;

        if (!string.IsNullOrWhiteSpace(textureIdentifier))
        {
          Sprite icon = Resources.Load<Sprite>($"{DefaultsResource.ItemTexturesPath}/{textureIdentifier}");
          icon ??= Resources.Load<Sprite>(textureIdentifier);
          if (icon != null)
            _itemData.SetIcon(icon);
        }
      }

      if (!string.IsNullOrWhiteSpace(_itemIdentifier))
        Registry.Registry.Register(RegistryType.Item, _itemIdentifier, this);
    }
  }
}
