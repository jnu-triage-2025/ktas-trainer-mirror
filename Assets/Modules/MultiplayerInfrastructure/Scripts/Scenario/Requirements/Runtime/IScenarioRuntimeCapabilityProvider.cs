using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public interface IScenarioRuntimeCapabilityProvider
  {
    string Identifier { get; }
    bool TryGetIdentifier(object target, out string identifier);
    bool Supports(object target, ScenarioRequirementCapability capability);
  }

  public static class ScenarioRuntimeCapabilityProviderRegistry
  {
    private static readonly Dictionary<string, IScenarioRuntimeCapabilityProvider> Providers = new Dictionary<string, IScenarioRuntimeCapabilityProvider>(StringComparer.Ordinal);

    public static bool Register(IScenarioRuntimeCapabilityProvider provider, out string error)
    {
      error = string.Empty;
      if (provider == null || string.IsNullOrWhiteSpace(provider.Identifier)) { error = "Provider identifier is empty."; return false; }
      if (Providers.ContainsKey(provider.Identifier)) { error = "Provider identifier is already registered: " + provider.Identifier; return false; }
      Providers.Add(provider.Identifier, provider);
      return true;
    }

    public static bool Supports(object target, ScenarioRequirementCapability capability)
    {
      foreach (var provider in Providers.Values)
        if (provider.Supports(target, capability)) return true;
      return false;
    }

    public static IReadOnlyList<IScenarioRuntimeCapabilityProvider> GetAll()
      => Providers.Values.OrderBy(value => value.Identifier, StringComparer.Ordinal).ToArray();

    public static void Reset() => Providers.Clear();

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration() => Reset();
  }
}
