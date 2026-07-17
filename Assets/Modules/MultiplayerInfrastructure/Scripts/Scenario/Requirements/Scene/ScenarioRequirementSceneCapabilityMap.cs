using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  /// <summary>
  /// Single source of truth for the capabilities a concrete scene component
  /// supplies.  Editor scene scan, scene binding inference, and the runtime
  /// provider snapshot must resolve identical capabilities for the same
  /// component type; otherwise a requirement can pass Editor/build validation
  /// and then fail at runtime (proposal §6, acceptance criterion 11).
  /// </summary>
  public static class ScenarioRequirementSceneCapabilityMap
  {
    /// <summary>
    /// Returns the requirement kind and capability set for a supported scene
    /// component, or false when the component is not a scenario provider.
    /// </summary>
    public static bool TryResolve(Component component, out ScenarioRequirementKind kind, out ScenarioRequirementCapability[] capabilities)
      => TryResolve(component, out kind, out capabilities, out _);

    /// <summary>
    /// Returns the requirement kind, capability set, and the component's
    /// scenario identifier for a supported scene component, or false when the
    /// component is not a scenario provider.  Identifier resolution lives here
    /// alongside capabilities so Editor scan and runtime snapshot cannot resolve
    /// a different identifier for the same component (proposal §6, acceptance
    /// criterion 11).
    /// </summary>
    public static bool TryResolve(Component component, out ScenarioRequirementKind kind, out ScenarioRequirementCapability[] capabilities, out string identifier)
    {
      kind = default;
      capabilities = Array.Empty<ScenarioRequirementCapability>();
      identifier = null;
      if (component == null) return false;

      switch (component)
      {
        case WaypointAnchor waypoint:
          kind = ScenarioRequirementKind.SpatialAnchor;
          capabilities = WaypointCapabilities(waypoint.SupportsHighlight);
          identifier = waypoint.Identifier;
          return true;
        case Npc npc:
          kind = ScenarioRequirementKind.Npc;
          capabilities = NpcCapabilities();
          identifier = npc.Identifier;
          return true;
        case ItemSubmissionInteractable submission:
          kind = ScenarioRequirementKind.Interactable;
          capabilities = ItemSubmissionCapabilities();
          identifier = submission.Identifier;
          return true;
        case ScenarioInteractable interactable:
          kind = ScenarioRequirementKind.Interactable;
          capabilities = ScenarioInteractableCapabilities();
          identifier = interactable.Identifier;
          return true;
        default:
          return false;
      }
    }

    public static ScenarioRequirementCapability[] WaypointCapabilities(bool supportsHighlight)
    {
      var capabilities = new List<ScenarioRequirementCapability>
      {
        ScenarioRequirementCapability.ProvidesPosition,
        ScenarioRequirementCapability.RegisteredEntity
      };
      if (supportsHighlight) capabilities.Add(ScenarioRequirementCapability.HighlightableWaypoint);
      return capabilities.ToArray();
    }

    public static ScenarioRequirementCapability[] NpcCapabilities()
      => new[]
      {
        ScenarioRequirementCapability.ResolvableNpcMoveTarget,
        ScenarioRequirementCapability.RegisteredNpcComponent,
        ScenarioRequirementCapability.ProvidesPosition,
        ScenarioRequirementCapability.RegisteredEntity,
        ScenarioRequirementCapability.Interactable
      };

    public static ScenarioRequirementCapability[] ItemSubmissionCapabilities()
      => new[]
      {
        ScenarioRequirementCapability.Interactable,
        ScenarioRequirementCapability.ToggleableInteractable,
        ScenarioRequirementCapability.ItemSubmissionTarget,
        ScenarioRequirementCapability.RegisteredEntity
      };

    public static ScenarioRequirementCapability[] ScenarioInteractableCapabilities()
      => new[]
      {
        ScenarioRequirementCapability.Interactable,
        ScenarioRequirementCapability.RegisteredEntity
      };
  }
}
