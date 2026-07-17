using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  /// <summary>
  /// Declares requirement providers that can be inspected without entering
  /// play mode.  Implementations must be deterministic and must not register
  /// runtime state while enumerating their entries.
  /// </summary>
  public interface IScenarioRequirementStaticProvider
  {
    IEnumerable<ScenarioRequirementStaticProviderEntry> GetStaticProviders();
  }

  public sealed class ScenarioRequirementStaticProviderEntry
  {
    public ScenarioRequirementKey Key { get; }
    public IReadOnlyList<ScenarioRequirementCapability> Capabilities { get; }
    public ScenarioRequirementAuthority Authority { get; }
    public string ProviderIdentifier { get; }

    public ScenarioRequirementStaticProviderEntry(
      ScenarioRequirementKey key,
      IEnumerable<ScenarioRequirementCapability> capabilities,
      ScenarioRequirementAuthority authority,
      string providerIdentifier)
    {
      Key = key;
      Capabilities = (capabilities ?? Array.Empty<ScenarioRequirementCapability>()).Distinct().OrderBy(value => value).ToArray();
      Authority = authority;
      ProviderIdentifier = providerIdentifier ?? string.Empty;
    }
  }
}
