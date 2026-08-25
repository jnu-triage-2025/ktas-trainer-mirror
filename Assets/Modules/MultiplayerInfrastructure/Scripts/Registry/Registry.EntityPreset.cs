using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public static partial class Registry
  {
    // 재귀 스폰 시 순환 참조(A→B→A)로 인한 무한 루프를 막기 위한 진행 중 프리셋 집합.
    [ThreadStatic] private static HashSet<string> _entityPresetSpawnStack;

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
      bool isNetworked = false,
      IReadOnlyList<EntityPresetChildReference> childReferences = null)
    {
      RegisterEntityPreset(new EntityPresetDefinition(
        identifier,
        entityType,
        prefab,
        displayName,
        isNetworked,
        childReferences));
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
    ///
    /// 동작:
    ///  1) 프리셋의 프리팹을 (position, rotation) 에 인스턴스화하고, 자가 등록 컴포넌트
    ///     (ISpawnedEntityIdentifierReceiver, 예: PatientController/MovingPatientBedController)가 있으면
    ///     식별자를 주입한다. 없으면 프리셋의 fallbackEntityType 으로 직접 등록한다.
    ///  2) 프리셋에 하위 참조(ChildReferences)가 있으면, 각 하위를 <b>등록된 다른 EntityPreset</b>으로 재귀 스폰한다.
    ///     - unwrapOnSpawn=false: 하위 인스턴스를 루트의 자식으로 부착한다.
    ///     - unwrapOnSpawn=true : 하위 인스턴스를 루트와 동일 계층(형제 루트)에 둔다(독립 루트). 환자+침대 결합 스폰용.
    ///  3) 네트워크 프리셋(IsNetworked=true)이고 서버 컨텍스트이면, NetworkObject 를 FishNet ServerManager.Spawn 으로 복제한다.
    ///
    /// <paramref name="desiredEntityIdentifier"/> 가 지정되면 루트 인스턴스를 해당 식별자로 등록한다(미지정 시 GUID).
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
      return TrySpawnEntityPresetInternal(
        identifier,
        position,
        rotation,
        desiredEntityIdentifier,
        parentForHierarchy: null,
        out spawned,
        out descriptor,
        out error);
    }

    /// <summary>
    /// 내부 재귀 스폰 구현.
    /// <paramref name="parentForHierarchy"/> 가 지정되면(=비-unwrap 하위 스폰) 스폰된 루트를 해당 부모의 자식으로 부착한다.
    /// null 이면 월드 루트로 둔다(최상위 호출 또는 unwrap 하위).
    /// </summary>
    private static bool TrySpawnEntityPresetInternal(
      string identifier,
      Vector3 position,
      Quaternion rotation,
      string desiredEntityIdentifier,
      Transform parentForHierarchy,
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

      _entityPresetSpawnStack ??= new HashSet<string>(StringComparer.Ordinal);
      if (!_entityPresetSpawnStack.Add(identifier))
      {
        error = $"Entity preset '{identifier}' has a circular child reference. Spawn aborted.";
        return false;
      }

      try
      {
        string runtimeEntityIdentifier = !string.IsNullOrWhiteSpace(desiredEntityIdentifier)
          ? desiredEntityIdentifier.Trim()
          : BuildEntityPresetRuntimeIdentifier(identifier);

        spawned = UnityEngine.Object.Instantiate(preset.Prefab, position, rotation);
        if (spawned == null)
        {
          error = $"Failed to spawn entity preset '{identifier}'.";
          return false;
        }
        MultiplayerInfrastructure.Performance.MppmLiteMode.StripVisuals(spawned);

        // 1) 계층 부착(비-unwrap 하위 스폰일 때만).
        //    FishNet 규칙: "루트를 스폰하면 그 아래 이미 nested 된 NetworkObject 도 함께 스폰된다."
        //    따라서 비-unwrap 하위는 자신의 NetworkSpawn 보다 "먼저" 부모(루트) 아래로 옮겨, 루트 스폰 시
        //    함께 복제되도록 한다(스폰 후 nested 시 ownership 명시 필요 등 타이밍 문제를 피한다).
        //    NetworkObject 가 prefab-nested 가 아니라 "루트"인 상태에서 reparent 하는 것이므로 런타임 reparent 가 허용된다.
        if (parentForHierarchy != null)
        {
          spawned.transform.SetParent(parentForHierarchy, worldPositionStays: true);
        }

        // 2) 식별자 주입: 자가 등록 컴포넌트가 있으면 자가 등록(OnStartClient/SetIdentifier) 전에 식별자를 전달한다.
        var rootReceiver = spawned.GetComponentInChildren<ISpawnedEntityIdentifierReceiver>(true);
        rootReceiver?.ApplySpawnedEntityIdentifier(runtimeEntityIdentifier);

        // 3) 네트워크 복제: 루트가 NetworkObject 이고 네트워크 프리셋이면 서버에서 스폰한다.
        //    하위 참조 스폰(4)보다 "먼저" 수행한다 → unwrap 하위는 독립 루트로 따로 스폰되고,
        //    비-unwrap 하위는 이미 이 루트 아래로 옮겨진 뒤 자신을 스폰하므로(아래 4에서 parent 선행),
        //    어느 경우에도 "스폰된 루트 아래로 사후 nested" 가 발생하지 않는다.
        if (preset.IsNetworked)
        {
          NetworkSpawnIfServer(spawned, identifier);
        }

        // 4) 하위 참조 스폰(재귀). 루트의 월드 위치를 기준점으로, 비-unwrap 은 루트의 자식으로(부모 선행 부착),
        //    unwrap 은 루트와 동일 계층(형제 독립 루트)으로 둔다.
        if (preset.ChildReferences != null && preset.ChildReferences.Count > 0)
        {
          SpawnChildPresetReferences(spawned.transform, preset, identifier, runtimeEntityIdentifier);
        }

        // 5) 레지스트리 등록. 자가 등록 컴포넌트가 있으면 소유권은 그 컴포넌트에 있고(EntityType 도 컴포넌트가 결정),
        //    없으면 프리셋이 fallbackEntityType 으로 직접 등록한다.
        if (rootReceiver == null)
        {
          string displayName = !string.IsNullOrWhiteSpace(preset.DisplayName)
            ? preset.DisplayName
            : spawned.name;

          if (preset.EntityType == EntityType.Undefined)
          {
            Debug.LogWarning(
              $"[Registry] Entity preset '{identifier}' 는 자가 등록 컴포넌트가 없어 폴백 등록되지만 " +
              "fallbackEntityType 이 Undefined 입니다. 단순 프리팹이면 적절한 EntityType 을 지정하세요.");
          }

          RegisterEntity(runtimeEntityIdentifier, preset.EntityType, spawned, displayName, isNetworked: preset.IsNetworked);

          if (!TryGetEntity(runtimeEntityIdentifier, out descriptor) || descriptor == null)
          {
            error = $"Entity preset '{identifier}' spawned but registry registration failed.";
            return false;
          }

          return true;
        }

        // 자가 등록 컴포넌트가 소유: 동기 등록(비네트워크)이면 디스크립터가 즉시 잡히고,
        // 비동기(네트워크 OnStartClient)면 아직 null 일 수 있다(스폰 자체는 성공).
        TryGetEntity(runtimeEntityIdentifier, out descriptor);
        return true;
      }
      finally
      {
        _entityPresetSpawnStack.Remove(identifier);
      }
    }

    /// <summary>
    /// 프리셋의 하위 참조(ChildReferences)를 각각 등록된 EntityPreset 으로 재귀 스폰한다.
    /// 루트의 위치/회전을 기준점으로 사용하며, unwrap 여부에 따라 계층(자식 vs 형제 루트)을 결정한다.
    /// </summary>
    private static void SpawnChildPresetReferences(
      Transform rootTransform,
      EntityPresetDefinition rootPreset,
      string rootIdentifier,
      string rootRuntimeIdentifier)
    {
      if (rootTransform == null || rootPreset?.ChildReferences == null)
      {
        return;
      }

      // unwrap 된 하위는 루트와 동일 계층(루트의 부모)에 두어 "또 다른 루트"가 되게 한다.
      Transform unwrapParent = rootTransform.parent;
      Vector3 basePosition = rootTransform.position;
      Quaternion baseRotation = rootTransform.rotation;

      foreach (var child in rootPreset.ChildReferences)
      {
        if (string.IsNullOrWhiteSpace(child.childPresetIdentifier))
        {
          continue;
        }

        if (string.Equals(child.childPresetIdentifier, rootIdentifier, StringComparison.Ordinal))
        {
          Debug.LogWarning($"[Registry] Entity preset '{rootIdentifier}' references itself as a child. Skipped.");
          continue;
        }

        // 비-unwrap 하위는 스폰 후 루트의 자식으로 부착되므로 부모 계층을 루트로 지정한다.
        Transform parentForChild = child.unwrapOnSpawn ? null : rootTransform;

        if (!TrySpawnEntityPresetInternal(
              child.childPresetIdentifier,
              basePosition,
              baseRotation,
              child.spawnedEntityIdentifier,
              parentForChild,
              out var childGo,
              out _,
              out var childError))
        {
          Debug.LogWarning(
            $"[Registry] Entity preset '{rootIdentifier}': child preset '{child.childPresetIdentifier}' spawn failed: {childError}");
          continue;
        }

        // 부모 연결 요청 시, 하위가 IEntityPresetParentLinkReceiver 를 구현하면 부모(루트) 런타임 식별자를 전달한다.
        // 결합의 구체 의미(예: 환자침대가 부모 환자를 누임)는 하위 구현체가 결정한다(엔진은 식별자 전달만).
        if (childGo != null && child.linkChildToParent && !string.IsNullOrWhiteSpace(rootRuntimeIdentifier))
        {
          var linkReceiver = childGo.GetComponent<IEntityPresetParentLinkReceiver>()
                             ?? childGo.GetComponentInChildren<IEntityPresetParentLinkReceiver>(true);
          if (linkReceiver != null)
          {
            linkReceiver.ApplyParentEntityIdentifier(rootRuntimeIdentifier);
          }
          else
          {
            Debug.LogWarning(
              $"[Registry] Entity preset '{rootIdentifier}': child '{child.childPresetIdentifier}' requested parent link " +
              "but does not implement IEntityPresetParentLinkReceiver. Link skipped.");
          }
        }

        if (childGo != null && child.unwrapOnSpawn)
        {
          // 명시적으로 형제 루트 계층에 둔다(루트의 부모; 루트가 최상위면 월드 루트).
          childGo.transform.SetParent(unwrapParent, worldPositionStays: true);
        }
      }
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
      else if (InstanceFinder.IsOffline)
      {
        // 오프라인 실행에서는 Instantiate 된 로컬 인스턴스가 최종 인스턴스다.
        // 복제 대상이 없으므로 NetworkObject 스폰이나 경고가 필요하지 않다.
        return;
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
