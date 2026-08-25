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

    public static event Action Changed;

    public static void Register(IPlayerSpawnPointProvider provider)
    {
      if (provider == null)
        return;

      if (Providers.Contains(provider))
        return;

      Providers.Add(provider);
      Changed?.Invoke();
    }

    public static void Unregister(IPlayerSpawnPointProvider provider)
    {
      if (provider == null)
        return;

      if (!Providers.Remove(provider))
        return;
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
        if (provider == null)
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
  }
}
