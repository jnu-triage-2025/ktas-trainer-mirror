using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    public static void RegisterEntityPreset(EntityPresetDefinition definition)
    {
      if (definition == null
          || string.IsNullOrWhiteSpace(definition.Identifier)
          || definition.Prefab == null)
      {
        return;
      }

      Register(RegistryType.EntityPreset, definition.Identifier, definition);
    }

    public static void RegisterEntityPreset(
      string identifier,
      EntityType entityType,
      GameObject prefab,
      string displayName = null,
      bool isNetworked = false)
    {
      RegisterEntityPreset(new EntityPresetDefinition(
        identifier,
        entityType,
        prefab,
        displayName,
        isNetworked));
    }

    public static bool TryGetEntityPreset(string identifier, out EntityPresetDefinition definition)
      => TryGet(RegistryType.EntityPreset, identifier, out definition);

    public static void UnregisterEntityPreset(string identifier)
      => Unregister(RegistryType.EntityPreset, identifier);

    public static IReadOnlyDictionary<string, EntityPresetDefinition> GetAllEntityPresets()
      => GetAll<EntityPresetDefinition>(RegistryType.EntityPreset);

    public static bool TrySpawnEntityPreset(
      string identifier,
      Vector3 position,
      Quaternion rotation,
      out GameObject spawned,
      out EntityDescriptor descriptor,
      out string error)
    {
      spawned = null;
      descriptor = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(identifier))
      {
        error = "Entity preset identifier is required.";
        return false;
      }

      if (!TryGetEntityPreset(identifier, out var preset) || preset == null)
      {
        error = $"Entity preset '{identifier}' is not registered.";
        return false;
      }

      if (preset.Prefab == null)
      {
        error = $"Entity preset '{identifier}' has no prefab.";
        return false;
      }

      spawned = UnityEngine.Object.Instantiate(preset.Prefab, position, rotation);
      if (spawned == null)
      {
        error = $"Failed to spawn entity preset '{identifier}'.";
        return false;
      }

      string runtimeEntityIdentifier = BuildEntityPresetRuntimeIdentifier(identifier);
      string displayName = !string.IsNullOrWhiteSpace(preset.DisplayName)
        ? preset.DisplayName
        : spawned.name;

      RegisterEntity(runtimeEntityIdentifier, preset.EntityType, spawned, displayName, isNetworked: preset.IsNetworked);

      if (!TryGetEntity(runtimeEntityIdentifier, out descriptor) || descriptor == null)
      {
        error = $"Entity preset '{identifier}' spawned but registry registration failed.";
        return false;
      }

      return true;
    }

    private static string BuildEntityPresetRuntimeIdentifier(string presetIdentifier)
      => $"entitypreset:{presetIdentifier}:{Guid.NewGuid():N}";
  }
}
