using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports
{
  public partial class RegisteringMultiplayerInfrastructureSupport
  {
    [Header("Entity Preset Registry")]
    [SerializeField] private EntityPresetRegistryRequirementsSO _entityPresetRegistryRequirementsSO;

    private void Awake_EntityPreset()
    {
      RegisterAllEntityPresets();
      ValidateEntityPresetResources();
    }

    private void RegisterAllEntityPresets()
    {
      if (_entityPresetRegistryRequirementsSO == null)
      {
        Debug.LogWarning("[RegisteringMultiplayerInfrastructureSupport] EntityPresetRegistryRequirementsSO is not assigned.", this);
        return;
      }

      var requirements = _entityPresetRegistryRequirementsSO.entityPresetRegistryRequirements;
      if (requirements == null || requirements.Length == 0)
      {
        Debug.LogWarning("[RegisteringMultiplayerInfrastructureSupport] No entity preset registry requirements configured.", this);
        return;
      }

      int registeredCount = 0;
      var seenIdentifiers = new HashSet<string>(System.StringComparer.Ordinal);
      for (int i = 0; i < requirements.Length; i++)
      {
        var req = requirements[i];
        if (string.IsNullOrWhiteSpace(req.identifier))
        {
          Debug.LogWarning($"[RegisteringMultiplayerInfrastructureSupport] EntityPreset[{i}] has empty identifier.", this);
          continue;
        }

        if (req.prefab == null)
        {
          Debug.LogWarning($"[RegisteringMultiplayerInfrastructureSupport] EntityPreset[{i}] '{req.identifier}' has null prefab.", this);
          continue;
        }

        if (!seenIdentifiers.Add(req.identifier))
        {
          Debug.LogWarning($"[RegisteringMultiplayerInfrastructureSupport] EntityPreset[{i}] '{req.identifier}' is duplicated. Later value will overwrite previous registration.", this);
        }

        Registry.RegisterEntityPreset(
          req.identifier,
          req.fallbackEntityType,
          req.prefab,
          req.displayName,
          req.isNetworked,
          req.childDetachments);
        registeredCount++;
      }

      Debug.Log($"[RegisteringMultiplayerInfrastructureSupport] Registered {registeredCount} entity preset entries.", this);
    }

    private void ValidateEntityPresetResources()
    {
      if (_entityPresetRegistryRequirementsSO == null)
        return;

      var requirements = _entityPresetRegistryRequirementsSO.entityPresetRegistryRequirements;
      if (requirements == null || requirements.Length == 0)
        return;

      Debug.Log($"[RegisteringMultiplayerInfrastructureSupport] Entity preset resource validation completed ({requirements.Length} entries).", this);
    }
  }
}
