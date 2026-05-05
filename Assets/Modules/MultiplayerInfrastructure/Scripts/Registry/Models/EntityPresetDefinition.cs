using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public sealed class EntityPresetDefinition
  {
    public string Identifier { get; }
    public EntityType EntityType { get; }
    public GameObject Prefab { get; }
    public string DisplayName { get; }
    public bool IsNetworked { get; }

    public EntityPresetDefinition(
      string identifier,
      EntityType entityType,
      GameObject prefab,
      string displayName = null,
      bool isNetworked = false)
    {
      Identifier = identifier;
      EntityType = entityType;
      Prefab = prefab;
      DisplayName = displayName;
      IsNetworked = isNetworked;
    }
  }
}
