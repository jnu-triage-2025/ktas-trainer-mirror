using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.FishNetSupports
{
  public interface IPlayerSpawnPointProvider
  {
    public string Identifier { get; }
    public Transform SpawnTransform { get; }
    public bool IsAvailable { get; }
  }

  public static class PlayerSpawnPointRegistry
  {
    private static readonly List<IPlayerSpawnPointProvider> Providers = new List<IPlayerSpawnPointProvider>();
    private static readonly List<IPlayerSpawnPointProvider> PendingProviders = new List<IPlayerSpawnPointProvider>();

    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      Providers.Clear();
      PendingProviders.Clear();
      Changed = null;
    }

    public static void Register(IPlayerSpawnPointProvider provider)
    {
      if (provider == null)
        return;

      PruneDestroyedProviders(Providers);
      PruneDestroyedProviders(PendingProviders);
      if (Providers.Contains(provider) || PendingProviders.Contains(provider))
        return;

      for (int i = Providers.Count - 1; i >= 0; i--)
      {
        var existing = Providers[i];
        if (existing == null
            || existing is UnityEngine.Object unityObject && unityObject == null)
        {
          Providers.RemoveAt(i);
          continue;
        }
        if (ReferenceEquals(existing, provider))
          continue;
        if (string.Equals(existing.Identifier, provider.Identifier, StringComparison.Ordinal))
        {
          Debug.LogError($"[PlayerSpawnPointRegistry] Duplicate provider identifier '{provider.Identifier}'. Registration is pending until the active provider leaves.");
          PendingProviders.Add(provider);
          return;
        }
      }

      Providers.Add(provider);
      Changed?.Invoke();
    }

    public static void Unregister(IPlayerSpawnPointProvider provider)
    {
      if (provider == null)
        return;

      PendingProviders.Remove(provider);
      if (Providers.Remove(provider))
        PromoteUniquePendingProvider(provider.Identifier);
    }

    public static bool TryGet(string identifier, out Transform spawnTransform)
    {
      spawnTransform = null;

      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      if (Registry.Registry.TryGet<Transform>(RegistryType.SpawnPoint, identifier, out var registryTransform)
          && registryTransform != null)
      {
        spawnTransform = registryTransform;
        return true;
      }

      for (int i = Providers.Count - 1; i >= 0; i--)
      {
        var provider = Providers[i];
        if (provider == null
            || provider is UnityEngine.Object unityObject && unityObject == null)
        {
          Providers.RemoveAt(i);
          continue;
        }

        if (!provider.IsAvailable)
          continue;

        if (!string.Equals(provider.Identifier, identifier, StringComparison.Ordinal))
          continue;

        var transform = provider.SpawnTransform;
        if (transform == null)
          continue;

        spawnTransform = transform;
        return true;
      }

      return false;
    }

    private static void PromoteUniquePendingProvider(string identifier)
    {
      PruneDestroyedProviders(PendingProviders);
      IPlayerSpawnPointProvider candidate = null;
      foreach (var provider in PendingProviders)
      {
        if (!string.Equals(provider.Identifier, identifier, StringComparison.Ordinal))
          continue;
        if (candidate != null)
        {
          Debug.LogError($"[PlayerSpawnPointRegistry] Multiple pending successors for identifier '{identifier}'. Promotion was rejected.");
          return;
        }
        candidate = provider;
      }

      if (candidate == null)
        return;
      PendingProviders.Remove(candidate);
      Providers.Add(candidate);
      Changed?.Invoke();
    }

    private static void PruneDestroyedProviders(List<IPlayerSpawnPointProvider> providers)
    {
      for (int i = providers.Count - 1; i >= 0; i--)
      {
        var provider = providers[i];
        if (provider == null || provider is UnityEngine.Object unityObject && unityObject == null)
          providers.RemoveAt(i);
      }
    }
  }
}
