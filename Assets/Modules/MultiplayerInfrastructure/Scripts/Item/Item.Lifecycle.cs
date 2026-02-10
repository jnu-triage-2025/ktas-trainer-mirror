using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item
  {
    private void Awake()
    {
      if (_itemData == null || !_itemData.IsValid())
      {
        if (_itemBaseModel == null)
        {
          Debug.LogError("Item requires a valid Item Base Model or Item Data.");
          return;
        }

        _itemData = new ItemData(_itemBaseModel, _itemIcon);
      }
      else if (_itemIcon != null)
      {
        _itemData.SetIcon(_itemIcon);
      }

      if (string.IsNullOrEmpty(_itemIdentifier))
        _itemIdentifier = _itemData != null ? _itemData.identifier : string.Empty;
    }
  }
}
