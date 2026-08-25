using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct PlayerCharacterRegistryRequirement
  {
    public string identifier;
    public GameObject prefab;
  }
}
