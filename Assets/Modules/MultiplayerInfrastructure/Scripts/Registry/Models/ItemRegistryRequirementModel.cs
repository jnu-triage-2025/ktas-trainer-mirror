using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct ItemRegistryRequirement
  {
    public Type itemDefinition;
    public Sprite itemSprite;
  }
}
