using MultiplayerInfrastructure.Definitions;
using UnityEngine;

namespace MultiplayerInfrastructure.Item
{
  public partial class Item
  {
    public Sprite Icon => _itemData != null && _itemData.ItemTexture != null
      ? _itemData.ItemTexture
      : DefaultsResource.FallbackSprite;
  }
}
