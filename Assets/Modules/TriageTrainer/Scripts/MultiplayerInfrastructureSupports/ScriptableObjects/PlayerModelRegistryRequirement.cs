using System;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [Serializable]
  public struct PlayerModelRegistryRequirement
  {
    public string identifier;
    public GameObject prefab;
  }
}
