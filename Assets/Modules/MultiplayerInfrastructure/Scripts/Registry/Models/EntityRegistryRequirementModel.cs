using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct EntityRegistryRequirement
  {
    public string identifier;
    public UnityEngine.Object objectRef;
  }
}
