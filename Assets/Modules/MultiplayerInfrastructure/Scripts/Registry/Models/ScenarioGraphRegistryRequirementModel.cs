using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [Serializable]
  public struct ScenarioGraphRegistryRequirement
  {
    public string identifier;
    public TextAsset scenarioGraphAsset;
  }
}
