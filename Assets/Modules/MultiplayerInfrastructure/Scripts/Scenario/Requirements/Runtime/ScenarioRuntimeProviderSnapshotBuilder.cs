using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioRuntimeProviderSnapshotBuilder
  {
    public static ScenarioRequirementProviderSnapshot Build(ScenarioRequirementSceneComposition composition = null)
    {
      var providers = new List<ScenarioRequirementProvider>();
      var diagnostics = new List<ScenarioRequirementValidationDiagnostic>();
      var completeness = new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>
      {
        // These kinds may still be supplied by the legacy Registry API.  Its
        // entries lack owner identity, so runtime can establish presence but
        // cannot make a definitive duplicate conclusion.
        [ScenarioRequirementKind.Npc] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.SpatialAnchor] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.Interactable] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.Entity] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.SpawnPoint] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.EntityPreset] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.EventHandler] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.RuntimeSignal] = ScenarioRequirementEvidenceCompleteness.Partial
      };
      AddSceneProviders(providers, diagnostics, composition);
      AddRegistryEvidence(providers, diagnostics);
      AddOwnerAwareEvidence(providers);
      return new ScenarioRequirementProviderSnapshot(providers, diagnostics, completeness);
    }

    private static void AddOwnerAwareEvidence(List<ScenarioRequirementProvider> providers)
    {
      foreach (var record in ScenarioRequirementRuntimeRegistrationRegistry.GetAll())
      {
        providers.Add(new ScenarioRequirementProvider(
          record.Owner is Component component
            ? GetPhysicalIdentifier(component)
            : "runtime-registration:" + record.Token.Generation + ":" + record.Token.Sequence,
          new ScenarioRequirementKey(record.Kind, record.Identifier),
          record.ScenePath,
          ScenarioRequirementScope.AnyLoadedScene,
          record.HierarchyPath,
          record.Capabilities,
          record.IsActive,
          record.IsEnabled,
          record.IsActive && record.IsEnabled,
          record.Origin));
      }
    }

    private static void AddRegistryEvidence(List<ScenarioRequirementProvider> providers, List<ScenarioRequirementValidationDiagnostic> diagnostics)
    {
      // Retain raw Registry evidence for backwards compatibility.  Its lack
      // of owner identity is represented by Partial completeness above, which
      // prevents duplicate claims while still allowing an existing provider
      // to satisfy a requirement.
      AddRegistryType(providers, RegistryType.Npc, ScenarioRequirementKind.Npc, new[] { ScenarioRequirementCapability.RegisteredNpcComponent, ScenarioRequirementCapability.ResolvableNpcMoveTarget, ScenarioRequirementCapability.ProvidesPosition });
      AddRegistryType(providers, RegistryType.Waypoint, ScenarioRequirementKind.SpatialAnchor, new[] { ScenarioRequirementCapability.ProvidesPosition });
      AddRegistryType(providers, RegistryType.InteractableEntity, ScenarioRequirementKind.Interactable, new[] { ScenarioRequirementCapability.Interactable });
      AddRegistryType(providers, RegistryType.Entity, ScenarioRequirementKind.Entity, new[] { ScenarioRequirementCapability.RegisteredEntity });
      AddRegistryType(providers, RegistryType.EntityPreset, ScenarioRequirementKind.EntityPreset, new[] { ScenarioRequirementCapability.SpawnablePreset });
      AddRegistryType(providers, RegistryType.ScenarioEvent, ScenarioRequirementKind.EventHandler, new[] { ScenarioRequirementCapability.InvokableEventHandler });
      AddRegistryType(providers, RegistryType.RuntimeState, ScenarioRequirementKind.RuntimeSignal, Array.Empty<ScenarioRequirementCapability>());
      diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR607", "RegistryOwnerIdentityUnavailable", ScenarioRequirementDiagnosticSeverity.Info, "Registry evidence is present, but current Registry API does not expose registration owner identity; runtime duplicate conclusions remain Indeterminate."));
    }

    private static void AddRegistryType(List<ScenarioRequirementProvider> providers, RegistryType type, ScenarioRequirementKind kind, IReadOnlyList<ScenarioRequirementCapability> capabilities)
    {
      foreach (var entry in GetEntries(type).OrderBy(value => value.Key, StringComparer.Ordinal))
      {
        if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null) continue;
        providers.Add(new ScenarioRequirementProvider("registry:" + type + ":" + entry.Key, new ScenarioRequirementKey(kind, entry.Key.Trim()), string.Empty, ScenarioRequirementScope.AnyLoadedScene, string.Empty, capabilities, true, true, true, ScenarioRequirementProviderOrigin.RegistryPreloaderDeclaration));
      }
    }

    private static IReadOnlyDictionary<string, object> GetEntries(RegistryType type)
    {
      switch (type)
      {
        case RegistryType.Npc: return MultiplayerInfrastructure.Registry.Registry.GetAll<GameObject>(type).ToDictionary(value => value.Key, value => (object)value.Value, StringComparer.Ordinal);
        case RegistryType.Waypoint: return MultiplayerInfrastructure.Registry.Registry.GetAll<Vector3>(type).ToDictionary(value => value.Key, value => (object)value.Value, StringComparer.Ordinal);
        case RegistryType.Entity: return MultiplayerInfrastructure.Registry.Registry.GetAll<EntityDescriptor>(type).ToDictionary(value => value.Key, value => (object)value.Value, StringComparer.Ordinal);
        case RegistryType.InteractableEntity: return MultiplayerInfrastructure.Registry.Registry.GetAll<MonoBehaviour>(type).ToDictionary(value => value.Key, value => (object)value.Value, StringComparer.Ordinal);
        case RegistryType.EntityPreset: return MultiplayerInfrastructure.Registry.Registry.GetAll<EntityPresetDefinition>(type).ToDictionary(value => value.Key, value => (object)value.Value, StringComparer.Ordinal);
        case RegistryType.ScenarioEvent: return MultiplayerInfrastructure.Registry.Registry.GetAllScenarioEvents().ToDictionary(value => value.Key, value => (object)value.Value, StringComparer.Ordinal);
        case RegistryType.RuntimeState: return MultiplayerInfrastructure.Registry.Registry.GetAll<object>(type);
        default: return new Dictionary<string, object>(StringComparer.Ordinal);
      }
    }


    private static void AddSceneProviders(List<ScenarioRequirementProvider> providers, List<ScenarioRequirementValidationDiagnostic> diagnostics, ScenarioRequirementSceneComposition composition)
    {
      for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
      {
        var scene = SceneManager.GetSceneAt(sceneIndex);
        if (!scene.IsValid() || !scene.isLoaded) continue;
        var role = composition?.Scenes.FirstOrDefault(value => value.ScenePath == scene.path)?.Role ?? ScenarioRequirementScope.AnyLoadedScene;
        foreach (var root in scene.GetRootGameObjects())
        {
          foreach (var component in root.GetComponentsInChildren<Component>(true))
          {
            if (component == null) continue;
            if (component is WaypointAnchor waypoint) AddSceneProvider(providers, waypoint, ScenarioRequirementKind.SpatialAnchor, waypoint.Identifier, role, waypoint.SupportsHighlight ? new[] { ScenarioRequirementCapability.ProvidesPosition, ScenarioRequirementCapability.HighlightableWaypoint } : new[] { ScenarioRequirementCapability.ProvidesPosition });
            else if (component is Npc npc) AddSceneProvider(providers, npc, ScenarioRequirementKind.Npc, npc.Identifier, role, new[] { ScenarioRequirementCapability.ResolvableNpcMoveTarget, ScenarioRequirementCapability.RegisteredNpcComponent, ScenarioRequirementCapability.ProvidesPosition });
            else if (component is ItemSubmissionInteractable submission) AddSceneProvider(providers, submission, ScenarioRequirementKind.Interactable, submission.Identifier, role, new[] { ScenarioRequirementCapability.Interactable, ScenarioRequirementCapability.ItemSubmissionTarget });
            else if (component is ScenarioInteractable scenarioInteractable) AddSceneProvider(providers, scenarioInteractable, ScenarioRequirementKind.Interactable, scenarioInteractable.Identifier, role, new[] { ScenarioRequirementCapability.Interactable });
            else AddProviderBackedEntity(providers, component, role);
          }
        }
      }
    }

    private static void AddProviderBackedEntity(List<ScenarioRequirementProvider> providers, Component component, ScenarioRequirementScope role)
    {
      var requestedCapabilities = new[]
      {
        ScenarioRequirementCapability.PatientMedicalStateTarget,
        ScenarioRequirementCapability.ScenarioEntityInitTarget,
        ScenarioRequirementCapability.ScenarioTriageAssessTarget
      };
      foreach (var provider in ScenarioRuntimeCapabilityProviderRegistry.GetAll())
      {
        var capabilities = requestedCapabilities.Where(capability => provider.Supports(component, capability)).ToArray();
        if (capabilities.Length == 0 || !provider.TryGetIdentifier(component, out var identifier) || string.IsNullOrWhiteSpace(identifier)) continue;
        AddSceneProvider(providers, component, ScenarioRequirementKind.Entity, identifier, role, capabilities);
      }
    }

    private static void AddSceneProvider(List<ScenarioRequirementProvider> providers, Component component, ScenarioRequirementKind kind, string identifier, ScenarioRequirementScope role, IEnumerable<ScenarioRequirementCapability> capabilities)
    {
      if (string.IsNullOrWhiteSpace(identifier)) return;
      providers.Add(new ScenarioRequirementProvider(
        GetPhysicalIdentifier(component),
        new ScenarioRequirementKey(kind, identifier.Trim()),
        component.gameObject.scene.path,
        role,
        GetHierarchyPath(component.transform),
        capabilities,
        component.gameObject.activeInHierarchy,
        !(component is Behaviour behaviour) || behaviour.enabled,
        component.gameObject.activeInHierarchy && (!(component is Behaviour enabledBehaviour) || enabledBehaviour.enabled),
        ScenarioRequirementProviderOrigin.SceneComponent));
    }

    private static string GetPhysicalIdentifier(Component component)
      // Names are not unique among siblings.  The instance ID remains stable
      // throughout this runtime session and is shared by scene and owner-aware
      // registration evidence for this exact component.
      => "runtime:" + component.GetType().FullName + ":" + component.GetInstanceID();

    private static string GetHierarchyPath(Transform transform)
    {
      var stack = new Stack<string>();
      for (var cursor = transform; cursor != null; cursor = cursor.parent) stack.Push(cursor.name);
      return string.Join("/", stack);
    }
  }
}
