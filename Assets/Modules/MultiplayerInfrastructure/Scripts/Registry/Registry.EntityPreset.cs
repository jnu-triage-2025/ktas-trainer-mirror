using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
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
      => TrySpawnEntityPreset(identifier, position, rotation, null, out spawned, out descriptor, out error);

    /// <summary>
    /// 엔티티 프리셋을 스폰한다.
    /// <paramref name="desiredEntityIdentifier"/> 가 지정되면 스폰 인스턴스를 해당 식별자로 등록하고,
    /// (네트워크 프리셋인 경우) 스폰 직후 인스턴스의 IScenarioSpawnIdentifiable 등에 식별자를 전달한다.
    /// 비어 있으면 기존처럼 GUID 기반 식별자가 부여된다(하위호환).
    /// 네트워크 프리셋(IsNetworked=true)이고 서버 컨텍스트이면 FishNet ServerManager.Spawn 으로 복제한다.
    /// </summary>
    public static bool TrySpawnEntityPreset(
      string identifier,
      Vector3 position,
      Quaternion rotation,
      string desiredEntityIdentifier,
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

      string runtimeEntityIdentifier = !string.IsNullOrWhiteSpace(desiredEntityIdentifier)
        ? desiredEntityIdentifier.Trim()
        : BuildEntityPresetRuntimeIdentifier(identifier);

      // 네트워크 프리셋은 스폰 전에 인스턴스가 자가 등록(OnStartClient 등)하기 전,
      // 식별자를 주입할 수 있도록 ISpawnedEntityIdentifierReceiver 로 전달한다.
      spawned = UnityEngine.Object.Instantiate(preset.Prefab, position, rotation);
      if (spawned == null)
      {
        error = $"Failed to spawn entity preset '{identifier}'.";
        return false;
      }

      // 인스턴스가 식별자 수신 인터페이스를 구현하면, 자가 등록 전에 식별자를 주입.
      var receiver = spawned.GetComponentInChildren<ISpawnedEntityIdentifierReceiver>(true);
      receiver?.ApplySpawnedEntityIdentifier(runtimeEntityIdentifier);

      // 네트워크 프리셋이며 서버 컨텍스트이면 FishNet 으로 복제 스폰.
      if (preset.IsNetworked)
      {
        var networkObject = spawned.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
          if (InstanceFinder.IsServerStarted)
          {
            InstanceFinder.ServerManager.Spawn(spawned);
          }
          else
          {
            Debug.LogWarning(
              $"[Registry] Entity preset '{identifier}' is networked but spawn was requested off-server. " +
              "Instance will not be replicated.");
          }
        }
      }

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
