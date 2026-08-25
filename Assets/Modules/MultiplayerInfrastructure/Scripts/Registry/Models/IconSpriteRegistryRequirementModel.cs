using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct IconSpriteRegistryRequirement
  {
    public string identifier;
    public Sprite sprite;
  }
}
