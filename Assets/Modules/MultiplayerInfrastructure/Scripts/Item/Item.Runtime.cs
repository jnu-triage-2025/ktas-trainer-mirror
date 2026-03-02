using UnityEngine;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Definitions;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item
  {
    /// <summary>
    /// 런타임에 아이템 데이터를 교체합니다. 드롭/스폰 시 템플릿 인스턴스에 실제 수량을 반영할 때 사용합니다.
    /// </summary>
    public void ApplyRuntimeItemData(ItemData data)
    {
      if (data == null || !data.IsValid()) return;

      _itemData = new ItemData(data);
      _itemIdentifier = _itemData.identifier;

      if (!_itemData.HasIcon)
      {
        string textureIdentifier = string.IsNullOrWhiteSpace(_itemData.ItemTextureIdentifier)
          ? _itemData.identifier
          : _itemData.ItemTextureIdentifier;

        if (!string.IsNullOrWhiteSpace(textureIdentifier))
        {
          Sprite icon = Resources.Load<Sprite>($"{DefaultsResource.ItemTexturesPath}/{textureIdentifier}");
          icon ??= Resources.Load<Sprite>(textureIdentifier);
          if (icon != null) _itemData.SetIcon(icon);
        }
      }

      if (!string.IsNullOrWhiteSpace(_itemIdentifier))
        Registry.Registry.Register(RegistryType.Item, _itemIdentifier, this);
    }
  }
}
