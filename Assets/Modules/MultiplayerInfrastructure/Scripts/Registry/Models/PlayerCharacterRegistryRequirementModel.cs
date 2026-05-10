using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct PlayerCharacterRegistryRequirement
  {
    public string identifier;
    public GameObject prefab;
  }
}
