using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct NPCRegistryRequirements
  {
    public string Identifier;
    public GameObject GameObjectRef;
  }
}
