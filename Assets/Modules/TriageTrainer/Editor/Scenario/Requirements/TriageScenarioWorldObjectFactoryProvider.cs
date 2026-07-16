#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario.Requirements;
using MultiplayerInfrastructure.Scenario.Requirements.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrainer.Scenario.Requirements.Editor
{
  [InitializeOnLoad]
  internal static class TriageScenarioFactoryCatalogRegistration
  {
    static TriageScenarioFactoryCatalogRegistration()
    {
      var guids = AssetDatabase.FindAssets("t:TriageScenarioFactoryCatalog");
      foreach (var guid in guids.OrderBy(value => value, StringComparer.Ordinal))
      {
        var catalog = AssetDatabase.LoadAssetAtPath<TriageScenarioFactoryCatalog>(AssetDatabase.GUIDToAssetPath(guid));
        if (catalog == null) continue;
        if (!TriageScenarioWorldObjectFactoryProvider.Register(catalog, out var error) && !string.IsNullOrWhiteSpace(error))
          Debug.LogError("[ScenarioRequirements] " + error);
      }
    }
  }

  public sealed class TriageScenarioWorldObjectFactoryProvider
  {
    public static bool Register(TriageScenarioFactoryCatalog catalog, out string error)
    {
      error = string.Empty;
      if (catalog == null) { error = "Triage factory catalog is null."; return false; }
      foreach (var entry in catalog.Entries)
      {
        if (entry == null || string.IsNullOrWhiteSpace(entry.Identifier) || entry.Prefab == null) { error = "Catalog contains an invalid factory entry."; return false; }
      }
      var duplicate = catalog.Entries.GroupBy(value => value.Identifier, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
      if (duplicate != null) { error = "Catalog contains duplicate factory identifier: " + duplicate.Key; return false; }
      foreach (var entry in catalog.Entries)
      {
        if (!ScenarioWorldObjectFactoryRegistry.Register(new TriageCatalogFactory(catalog, entry), out error)) return false;
      }
      return true;
    }
  }

  internal sealed class TriageCatalogFactory : IScenarioWorldObjectFactory
  {
    private readonly TriageScenarioFactoryCatalog _catalog;
    private readonly TriageScenarioFactoryEntry _entry;
    public TriageCatalogFactory(TriageScenarioFactoryCatalog catalog, TriageScenarioFactoryEntry entry) { _catalog = catalog; _entry = entry; }
    public string Identifier => _entry.Identifier;
    public int Version => 1;
    public IReadOnlyList<ScenarioRequirementKind> SupportedKinds => new[] { _entry.IsPatient ? ScenarioRequirementKind.Entity : ScenarioRequirementKind.Npc };
    public bool Supports(ScenarioRequirementDescriptor requirement, ScenarioRequirementGenerationConfiguration configuration, out string reason)
    { reason = string.Empty; if (requirement.BindingHint == null || requirement.BindingHint.Mode != ScenarioRequirementBindingMode.PrefabInstance) { reason = "Triage catalog factory requires PrefabInstance binding."; return false; } if (configuration == null || string.IsNullOrWhiteSpace(configuration.TargetScenePath)) { reason = "Target scene is required."; return false; } if (_entry.IsPatient ? requirement.Kind != ScenarioRequirementKind.Entity : requirement.Kind != ScenarioRequirementKind.Npc) { reason = "Factory kind does not match requirement."; return false; } return true; }
    public GameObject Apply(ScenarioWorldObjectCreationPlan plan, ScenarioWorldObjectFactoryContext context, GameObject existingObject)
    {
      var target = existingObject;
      if (target == null)
      {
        target = (GameObject)PrefabUtility.InstantiatePrefab(_entry.Prefab, context.TargetScene);
        Undo.RegisterCreatedObjectUndo(target, "Create Triage Scenario Object");
        SceneManager.MoveGameObjectToScene(target, context.TargetScene);
      }
      Undo.RecordObject(target.transform, "Configure Triage Scenario Object");
      if (context.Configuration.Position.HasValue) target.transform.position = context.Configuration.Position.Value;
      if (context.Configuration.RotationEuler.HasValue) target.transform.eulerAngles = context.Configuration.RotationEuler.Value;
      if (context.Configuration.Scale.HasValue) target.transform.localScale = context.Configuration.Scale.Value;
      var marker = target.GetComponent<ScenarioGeneratedWorldObject>() ?? Undo.AddComponent<ScenarioGeneratedWorldObject>(target);
      Undo.RecordObject(marker, "Configure Triage Generated Marker");
      marker.Configure(plan.ScenarioIdentifier, plan.PlanIdentity, context.Composition.Identifier, plan.RequirementKey, Identifier, Version, plan.TargetSceneGuid, plan.TargetSceneRole, plan.PlanIdentity);
      PrefabUtility.RecordPrefabInstancePropertyModifications(target.transform);
      PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
      EditorUtility.SetDirty(target);
      return target;
    }
  }
}
#endif
