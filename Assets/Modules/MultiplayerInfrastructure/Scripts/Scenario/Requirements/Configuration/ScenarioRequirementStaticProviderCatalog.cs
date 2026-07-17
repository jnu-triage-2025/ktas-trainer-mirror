using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  [CreateAssetMenu(menuName = "Multiplayer Infrastructure/Scenario Requirement Static Provider Catalog")]
  public sealed class ScenarioRequirementStaticProviderCatalog : ScriptableObject, IScenarioRequirementStaticProvider
  {
    [Serializable]
    public sealed class Entry
    {
      public ScenarioRequirementKind Kind;
      public string Identifier;
      public ScenarioRequirementAuthority Authority = ScenarioRequirementAuthority.Any;
      public string ProviderIdentifier;
      public List<ScenarioRequirementCapability> Capabilities = new List<ScenarioRequirementCapability>();
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();

    public IEnumerable<ScenarioRequirementStaticProviderEntry> GetStaticProviders()
    {
      foreach (var entry in _entries.Where(value => value != null).OrderBy(value => value.Kind).ThenBy(value => value.Identifier, StringComparer.Ordinal))
      {
        if (string.IsNullOrWhiteSpace(entry.Identifier)) continue;
        yield return new ScenarioRequirementStaticProviderEntry(
          new ScenarioRequirementKey(entry.Kind, entry.Identifier.Trim()),
          entry.Capabilities,
          entry.Authority,
          entry.ProviderIdentifier);
      }
    }
  }
}
