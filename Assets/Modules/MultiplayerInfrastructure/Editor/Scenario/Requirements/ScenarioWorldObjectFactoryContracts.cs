#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  public enum ScenarioWorldObjectOperationKind { Create, Configure, Bind, MoveToScene, MarkOrphan, DeleteGenerated, NoChange, Blocked }
  public enum ScenarioWorldObjectPlanRisk { None, Warning, Destructive, Blocked }

  public sealed class ScenarioRequirementGenerationConfiguration
  {
    public Vector3? Position { get; }
    public Vector3? RotationEuler { get; }
    public Vector3? Scale { get; }
    public string TargetScenePath { get; }
    public string TargetSceneGuid { get; }
    public ScenarioRequirementGenerationConfiguration(Vector3? position, Vector3? rotationEuler, Vector3? scale, string targetScenePath, string targetSceneGuid)
    { Position = position; RotationEuler = rotationEuler; Scale = scale; TargetScenePath = targetScenePath ?? string.Empty; TargetSceneGuid = targetSceneGuid ?? string.Empty; }
  }

  public sealed class ScenarioWorldObjectFieldChange
  {
    public string FieldPath { get; }
    public string BeforeValue { get; }
    public string AfterValue { get; }
    public ScenarioWorldObjectFieldChange(string fieldPath, string beforeValue, string afterValue) { FieldPath = fieldPath; BeforeValue = beforeValue; AfterValue = afterValue; }
  }

  public sealed class ScenarioWorldObjectCreationPlan
  {
    public string PlanIdentity { get; }
    public string ScenarioIdentifier { get; }
    public ScenarioRequirementKey RequirementKey { get; }
    public ScenarioWorldObjectOperationKind Operation { get; }
    public string TargetScenePath { get; }
    public string TargetSceneGuid { get; }
    public ScenarioRequirementScope TargetSceneRole { get; }
    public string FactoryIdentifier { get; }
    public int FactoryVersion { get; }
    public string ExistingObjectIdentity { get; }
    public string Reason { get; }
    public ScenarioWorldObjectPlanRisk Risk { get; }
    public bool IsBlocked => Operation == ScenarioWorldObjectOperationKind.Blocked;
    public IReadOnlyList<ScenarioWorldObjectFieldChange> Changes { get; }
    public ScenarioGeneratedWorldObject ExistingMarker { get; }
    internal ScenarioWorldObjectCreationPlan(string planIdentity, string scenarioIdentifier, ScenarioRequirementKey key, ScenarioWorldObjectOperationKind operation, string targetScenePath, string targetSceneGuid, ScenarioRequirementScope role, string factoryIdentifier, int factoryVersion, string existingObjectIdentity, string reason, ScenarioWorldObjectPlanRisk risk, IEnumerable<ScenarioWorldObjectFieldChange> changes, ScenarioGeneratedWorldObject existingMarker)
    { PlanIdentity = planIdentity; ScenarioIdentifier = scenarioIdentifier ?? string.Empty; RequirementKey = key; Operation = operation; TargetScenePath = targetScenePath; TargetSceneGuid = targetSceneGuid; TargetSceneRole = role; FactoryIdentifier = factoryIdentifier; FactoryVersion = factoryVersion; ExistingObjectIdentity = existingObjectIdentity; Reason = reason; Risk = risk; Changes = new ReadOnlyCollection<ScenarioWorldObjectFieldChange>((changes ?? Array.Empty<ScenarioWorldObjectFieldChange>()).ToArray()); ExistingMarker = existingMarker; }
  }

  public sealed class ScenarioWorldObjectCreationPlanSet
  {
    public IReadOnlyList<ScenarioWorldObjectCreationPlan> Plans { get; }
    public bool CanApply => Plans.Count > 0 && Plans.All(value => !value.IsBlocked);
    public ScenarioWorldObjectCreationPlanSet(IEnumerable<ScenarioWorldObjectCreationPlan> plans) => Plans = new ReadOnlyCollection<ScenarioWorldObjectCreationPlan>((plans ?? Array.Empty<ScenarioWorldObjectCreationPlan>()).OrderBy(value => value.TargetSceneGuid, StringComparer.Ordinal).ThenBy(value => value.Operation).ThenBy(value => value.RequirementKey).ToArray());
  }

  public sealed class ScenarioWorldObjectFactoryContext
  {
    public Scene TargetScene { get; }
    public ScenarioRequirementSceneComposition Composition { get; }
    public ScenarioRequirementGenerationConfiguration Configuration { get; }
    public bool IsMainStage { get; }
    public ScenarioWorldObjectFactoryContext(Scene targetScene, ScenarioRequirementSceneComposition composition, ScenarioRequirementGenerationConfiguration configuration) { TargetScene = targetScene; Composition = composition; Configuration = configuration; IsMainStage = PrefabStageUtility.GetCurrentPrefabStage() == null; }
  }

  public interface IScenarioWorldObjectFactory
  {
    string Identifier { get; }
    int Version { get; }
    IReadOnlyList<ScenarioRequirementKind> SupportedKinds { get; }
    bool Supports(ScenarioRequirementDescriptor requirement, ScenarioRequirementGenerationConfiguration configuration, out string reason);
    GameObject Apply(ScenarioWorldObjectCreationPlan plan, ScenarioWorldObjectFactoryContext context, GameObject existingObject);
  }

  public static class ScenarioWorldObjectFactoryRegistry
  {
    private static readonly Dictionary<string, IScenarioWorldObjectFactory> Factories = new Dictionary<string, IScenarioWorldObjectFactory>(StringComparer.Ordinal);
    public static IReadOnlyCollection<IScenarioWorldObjectFactory> All => new ReadOnlyCollection<IScenarioWorldObjectFactory>(Factories.Values.OrderBy(value => value.Identifier, StringComparer.Ordinal).ToArray());
    public static bool Register(IScenarioWorldObjectFactory factory, out string diagnostic)
    {
      diagnostic = string.Empty;
      if (factory == null || string.IsNullOrWhiteSpace(factory.Identifier))
      {
        diagnostic = "Factory identifier is null or empty.";
        return false;
      }
      if (Factories.TryGetValue(factory.Identifier, out var existing))
      {
        if (existing.GetType() == factory.GetType() && existing.Version == factory.Version)
        {
          diagnostic = string.Empty;
          return true;
        }
        diagnostic = "Factory identifier is already registered with a different implementation.";
        return false;
      }
      Factories.Add(factory.Identifier, factory);
      return true;
    }
    public static bool TryGet(string identifier, out IScenarioWorldObjectFactory factory) => Factories.TryGetValue(identifier ?? string.Empty, out factory);
    public static void ClearForTests() => Factories.Clear();
  }

  [InitializeOnLoad]
  internal static class ScenarioBuiltInFactoryRegistration
  {
    static ScenarioBuiltInFactoryRegistration() { ScenarioWorldObjectFactoryRegistry.Register(new WaypointAnchorFactory(), out _); }
  }
}
#endif
