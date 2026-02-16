using UnityEngine;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Definitions;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item
  {
    private void Awake()
    {
      bool hasValidBaseModel = _itemBaseModel != null && !string.IsNullOrWhiteSpace(_itemBaseModel.identifier);
      bool hasValidItemData = _itemData != null && _itemData.IsValid();

      if (hasValidBaseModel)
      {
        _itemData = new ItemData(_itemBaseModel, _itemIcon);
        hasValidItemData = _itemData != null && _itemData.IsValid();
      }
      else
      {
        if (hasValidItemData && _itemIcon != null)
          _itemData.SetIcon(_itemIcon);
      }

      if (string.IsNullOrWhiteSpace(_itemIdentifier))
        _itemIdentifier = _itemData != null ? _itemData.identifier : string.Empty;

      if (hasValidItemData && !_itemData.HasIcon)
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

      if (!hasValidBaseModel && !hasValidItemData)
      {
        string componentName = this != null ? GetType().Name : "UnknownComponent";
        Debug.LogWarning($"[{componentName}] Item registration skipped: insufficient data. Neither valid ItemBaseModelSO nor valid ItemData was found.", this);
        return;
      }

      if (string.IsNullOrWhiteSpace(_itemIdentifier))
      {
        string componentName = this != null ? GetType().Name : "UnknownComponent";
        Debug.LogWarning($"[{componentName}] Item registration skipped: identifier is empty.", this);
        return;
      }

      Registry.Registry.Register(RegistryType.Item, _itemIdentifier, this);
    }
  }
}
