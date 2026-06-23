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
      => TrySpawnEntityPreset(identifier, position, rotation, desiredEntityIdentifier, null, out spawned, out descriptor, out error);

    /// <summary>
    /// 엔티티 프리셋을 스폰한다.
    /// <paramref name="desiredEntityIdentifier"/> 가 지정되면 루트 인스턴스를 해당 식별자로 등록한다(미지정 시 GUID).
    /// <paramref name="childDetachments"/> 가 지정되면, 컨테이너 프리팹의 해당 자식 <b>NetworkObject</b>를
    /// 루트로 분리(ungroup)하여 각각 독립 엔티티로 스폰·등록한다. 분리는 NetworkObject 에 대해서만 허용되며,
    /// (네트워크 프리셋 + 서버 컨텍스트인 경우) 각 분리 객체를 FishNet ServerManager.Spawn 으로 개별 복제한다.
    /// 런타임에 환자↔침대처럼 위계가 없는 객체들을 한 프리팹으로 배치해두고 스폰 시 독립 루트로 푸는 용도다.
    /// 모든 변경은 opt-in: childDetachments 가 비어 있으면 기존 단일 객체 스폰과 동일하다(하위호환).
    /// </summary>
    public static bool TrySpawnEntityPreset(
      string identifier,
      Vector3 position,
      Quaternion rotation,
      string desiredEntityIdentifier,
      IReadOnlyList<(string childPath, string spawnedEntityIdentifier)> childDetachments,
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

      spawned = UnityEngine.Object.Instantiate(preset.Prefab, position, rotation);
      if (spawned == null)
      {
        error = $"Failed to spawn entity preset '{identifier}'.";
        return false;
      }

      // 1) 자식 NetworkObject 분리(ungroup) — 루트 스폰 전에 수행하여 nested 스폰을 피한다.
      if (childDetachments != null && childDetachments.Count > 0)
      {
        foreach (var (childPath, childId) in childDetachments)
        {
          DetachAndSpawnChildNetworkObject(spawned.transform, childPath, childId, identifier);
        }
      }

      // 2) 루트 인스턴스: 식별자 주입(자가 등록 전) → (네트워크면) 서버 스폰 → 레지스트리 등록.
      var rootReceiver = spawned.GetComponentInChildren<ISpawnedEntityIdentifierReceiver>(true);
      rootReceiver?.ApplySpawnedEntityIdentifier(runtimeEntityIdentifier);

      if (preset.IsNetworked)
      {
        NetworkSpawnIfServer(spawned, identifier);
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

    /// <summary>
    /// 컨테이너 자식 중 childPath 에 해당하는 NetworkObject 를 루트로 분리하여 독립 스폰·등록한다.
    /// NetworkObject 가 아니거나 찾지 못하면 분리하지 않고 경고만 남긴다(컨테이너 위계에 잔류).
    /// </summary>
    private static void DetachAndSpawnChildNetworkObject(
      Transform containerRoot,
      string childPath,
      string spawnedEntityIdentifier,
      string presetIdentifier)
    {
      if (containerRoot == null || string.IsNullOrWhiteSpace(childPath))
      {
        return;
      }

      Transform child = containerRoot.Find(childPath);
      if (child == null)
      {
        // 경로로 못 찾으면 이름 기준 1단계 탐색 폴백.
        for (int i = 0; i < containerRoot.childCount; i++)
        {
          if (string.Equals(containerRoot.GetChild(i).name, childPath, StringComparison.Ordinal))
          {
            child = containerRoot.GetChild(i);
            break;
          }
        }
      }

      if (child == null)
      {
        Debug.LogWarning($"[Registry] Preset '{presetIdentifier}': child '{childPath}' not found for detachment. Skipped.");
        return;
      }

      var childNob = child.GetComponent<NetworkObject>();
      if (childNob == null)
      {
        // NetworkObject 만 분리 허용.
        Debug.LogWarning($"[Registry] Preset '{presetIdentifier}': child '{childPath}' is not a NetworkObject. Detachment skipped.");
        return;
      }

      // 루트로 분리(월드 위치 유지). 분리 후에는 컨테이너 위계에 속하지 않는 독립 루트가 된다.
      child.SetParent(null, true);

      string childRuntimeId = !string.IsNullOrWhiteSpace(spawnedEntityIdentifier)
        ? spawnedEntityIdentifier.Trim()
        : BuildEntityPresetRuntimeIdentifier($"{presetIdentifier}:child");

      // 식별자 주입: 분리된 자식이 식별자 수신 인터페이스를 구현하면, 자가 등록(OnStartClient/SetIdentifier 등)
      // 전에 식별자를 전달한다. 분리 대상 NetworkObject 는 보통 자체적으로 RegisterEntity 하므로
      // 엔티티 타입은 그 컴포넌트가 결정한다(여기서 타입을 임의 지정하지 않는다).
      var childReceiver = child.GetComponent<ISpawnedEntityIdentifierReceiver>()
                          ?? child.GetComponentInChildren<ISpawnedEntityIdentifierReceiver>(true);
      if (childReceiver != null)
      {
        childReceiver.ApplySpawnedEntityIdentifier(childRuntimeId);
      }
      else
      {
        Debug.LogWarning(
          $"[Registry] Preset '{presetIdentifier}': detached child '{childPath}' has no ISpawnedEntityIdentifierReceiver; " +
          $"identifier '{childRuntimeId}' could not be injected. The child must self-register or implement the receiver.");
      }

      // 자식 NetworkObject 는 서버에서 개별 복제 스폰.
      NetworkSpawnIfServer(child.gameObject, $"{presetIdentifier}:{childPath}");
    }

    private static void NetworkSpawnIfServer(GameObject go, string contextLabel)
    {
      if (go == null)
      {
        return;
      }

      var nob = go.GetComponent<NetworkObject>();
      if (nob == null)
      {
        return;
      }

      if (InstanceFinder.IsServerStarted)
      {
        InstanceFinder.ServerManager.Spawn(go);
      }
      else
      {
        Debug.LogWarning(
          $"[Registry] '{contextLabel}' is networked but spawn was requested off-server. Instance will not be replicated.");
      }
    }

    private static string BuildEntityPresetRuntimeIdentifier(string presetIdentifier)
      => $"entitypreset:{presetIdentifier}:{Guid.NewGuid():N}";
  }
}
