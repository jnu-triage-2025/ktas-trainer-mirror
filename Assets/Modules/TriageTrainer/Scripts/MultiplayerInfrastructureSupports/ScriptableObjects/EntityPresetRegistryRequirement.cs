using System;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [Serializable]
  public struct EntityPresetRegistryRequirement
  {
    public string identifier;
    public EntityType entityType;
    public GameObject prefab;
    public string displayName;
    public bool isNetworked;
  }
}
