#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  internal sealed class WaypointAnchorFactory : IScenarioWorldObjectFactory
  {
    public string Identifier => "mi.waypoint-anchor";
    public int Version => 1;
    public IReadOnlyList<ScenarioRequirementKind> SupportedKinds { get; } = new[] { ScenarioRequirementKind.SpatialAnchor };

    public bool Supports(ScenarioRequirementDescriptor requirement, ScenarioRequirementGenerationConfiguration configuration, out string reason)
    {
      reason = string.Empty;
      if (requirement.Kind != ScenarioRequirementKind.SpatialAnchor) { reason = "Factory only supports SpatialAnchor."; return false; }
      if (configuration == null || string.IsNullOrWhiteSpace(configuration.TargetScenePath)) { reason = "Target scene is required."; return false; }
      return ScenarioWorldObjectConfigurationValidation.Validate(configuration, positionRequired: true, out reason);
    }

    public GameObject Apply(ScenarioWorldObjectCreationPlan plan, ScenarioWorldObjectFactoryContext context, GameObject existingObject)
    {
      var target = existingObject;
      if (target == null)
      {
        target = new GameObject("Waypoint_" + plan.RequirementKey.Identifier);
        Undo.RegisterCreatedObjectUndo(target, "Create Scenario Waypoint");
        SceneManager.MoveGameObjectToScene(target, context.TargetScene);
      }

      var anchor = target.GetComponent<WaypointAnchor>();
      if (anchor == null) anchor = Undo.AddComponent<WaypointAnchor>(target);
      Undo.RecordObject(anchor, "Configure Scenario Waypoint");
      anchor.ConfigureIdentifier(plan.RequirementKey.Identifier);
      if (context.Configuration.Position.HasValue)
      {
        Undo.RecordObject(target.transform, "Configure Scenario Waypoint Position");
        target.transform.position = context.Configuration.Position.Value;
      }
      if (context.Configuration.RotationEuler.HasValue)
      {
        Undo.RecordObject(target.transform, "Configure Scenario Waypoint Rotation");
        target.transform.eulerAngles = context.Configuration.RotationEuler.Value;
      }
      if (context.Configuration.Scale.HasValue)
      {
        Undo.RecordObject(target.transform, "Configure Scenario Waypoint Scale");
        target.transform.localScale = context.Configuration.Scale.Value;
      }

      var marker = target.GetComponent<ScenarioGeneratedWorldObject>();
      if (marker == null) marker = Undo.AddComponent<ScenarioGeneratedWorldObject>(target);
      Undo.RecordObject(marker, "Configure Scenario Generated Marker");
      marker.Configure(plan.ScenarioIdentifier, plan.ManifestFingerprint, context.Composition.Identifier,
        plan.RequirementKey, Identifier, Version, plan.TargetSceneGuid, plan.TargetSceneRole, plan.PlanIdentity);
      EditorUtility.SetDirty(target);
      return target;
    }
  }
}
#endif
