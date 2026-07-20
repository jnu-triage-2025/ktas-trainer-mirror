#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  public sealed class ScenarioRequirementsWindow : EditorWindow
  {
    [SerializeField] private TextAsset _scenario;
    [SerializeField] private TextAsset _sidecar;
    [SerializeField] private ScenarioSceneCompositionProfile _compositionProfile;
    [SerializeField] private ScenarioRequirementScope _openSceneRole = ScenarioRequirementScope.AnyLoadedScene;
    [SerializeField] private bool _hideSatisfied = true;
    private Vector2 _scroll;
    private ScenarioRequirementValidationReport _report;
    private ScenarioRequirementManifest _manifest;
    private ScenarioWorldObjectCreationPlanSet _plan;
    private ScenarioRequirementKey _selectedBindingKey;
    private UnityEngine.Object _selectedBindingTarget;
    private readonly Dictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> _generationConfigurations = new Dictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration>();
    private readonly Dictionary<ScenarioRequirementKey, Vector3> _generationPositions = new Dictionary<ScenarioRequirementKey, Vector3>();
    private readonly Dictionary<ScenarioRequirementKey, string> _generationScenes = new Dictionary<ScenarioRequirementKey, string>();
    private readonly HashSet<string> _approvedGeneratedDeletions = new HashSet<string>(StringComparer.Ordinal);
    private string _error;

    /// <summary>
    /// Opens the requirements workflow in its own window. The graph editor uses this
    /// entry point so both tools operate on the same scenario asset.
    /// </summary>
    public static void Open(TextAsset scenario = null)
    {
      var window = GetWindow<ScenarioRequirementsWindow>("Scenario Requirements");
      if (scenario != null)
        window._scenario = scenario;
      window.Show();
      window.Focus();
    }

    private void OnGUI()
    {
      EditorGUILayout.LabelField("Scenario Ingame Requirements", EditorStyles.boldLabel);
      _scenario = (TextAsset)EditorGUILayout.ObjectField("Scenario", _scenario, typeof(TextAsset), false);
      _sidecar = (TextAsset)EditorGUILayout.ObjectField("Requirements Sidecar", _sidecar, typeof(TextAsset), false);
      _compositionProfile = (ScenarioSceneCompositionProfile)EditorGUILayout.ObjectField("Composition Profile", _compositionProfile, typeof(ScenarioSceneCompositionProfile), false);
      _openSceneRole = (ScenarioRequirementScope)EditorGUILayout.EnumPopup("Open Scene Role", _openSceneRole);
      _hideSatisfied = EditorGUILayout.Toggle("Hide Satisfied", _hideSatisfied);
      using (new EditorGUI.DisabledScope(_scenario == null))
        if (GUILayout.Button("Compile and Validate", GUILayout.Height(28f))) Validate();
      using (new EditorGUI.DisabledScope(_report == null))
      {
        if (GUILayout.Button("Build Generation Preview", GUILayout.Height(24f))) BuildPreview();
        using (new EditorGUI.DisabledScope(_plan == null || !_plan.CanApply))
          if (GUILayout.Button("Apply Generated Objects", GUILayout.Height(24f))) ApplyPlan();
      }

      if (!string.IsNullOrWhiteSpace(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
      if (_report == null) return;
      EditorGUILayout.LabelField($"Results: {_report.Results.Count}, diagnostics: {_report.Diagnostics.Count}");
      if (_plan != null)
      {
        EditorGUILayout.LabelField($"Generation plan: {_plan.Plans.Count}, applyable: {_plan.CanApply}");
        foreach (var plan in _plan.Plans)
        {
          EditorGUILayout.HelpBox($"{plan.Operation} {plan.RequirementKey}: {plan.Reason}", plan.IsBlocked ? MessageType.Error : plan.Risk == ScenarioWorldObjectPlanRisk.Warning ? MessageType.Warning : MessageType.Info);
          if (plan.ExistingMarker != null && plan.ExistingMarker.IsOrphan)
          {
            var approved = _approvedGeneratedDeletions.Contains(plan.ExistingMarker.Identity);
            var updated = EditorGUILayout.ToggleLeft($"Approve permanent deletion: {plan.ExistingMarker.Identity}", approved);
            if (updated) _approvedGeneratedDeletions.Add(plan.ExistingMarker.Identity);
            else _approvedGeneratedDeletions.Remove(plan.ExistingMarker.Identity);
          }
        }
      }
      if (_report != null) DrawGenerationConfigurationEditor();
      _scroll = EditorGUILayout.BeginScrollView(_scroll);
      foreach (var result in _report.Results)
      {
        if (_hideSatisfied && result.Status == ScenarioRequirementValidationStatus.Satisfied) continue;
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
          EditorGUILayout.LabelField($"{result.Status}  {result.Requirement.Key}", EditorStyles.boldLabel);
          EditorGUILayout.LabelField("Capabilities", string.Join(", ", result.Requirement.Capabilities));
           EditorGUILayout.LabelField("Providers", result.Providers.Count.ToString());
           if (result.Status != ScenarioRequirementValidationStatus.Satisfied)
           {
             EditorGUILayout.LabelField("Binding target");
             _selectedBindingTarget = EditorGUILayout.ObjectField(_selectedBindingTarget, typeof(UnityEngine.Object), true);
             if (GUILayout.Button("Bind Selected Target"))
             {
               _selectedBindingKey = result.Requirement.Key;
               BindSelectedTarget(result.Requirement.Key, _selectedBindingTarget);
             }
           }
           foreach (var occurrence in result.Requirement.Occurrences)
             EditorGUILayout.LabelField($"{occurrence.NodeIdentifier}.{occurrence.FieldPath}", EditorStyles.miniLabel);
           foreach (var diagnostic in result.Diagnostics) EditorGUILayout.HelpBox($"{diagnostic.Code} {diagnostic.Message}", ToMessageType(diagnostic.Severity));
        }
      }
      EditorGUILayout.EndScrollView();
    }

    private void Validate()
    {
      try
      {
        _error = null;
        var scenarioPath = AssetDatabase.GetAssetPath(_scenario);
        var sourceBytes = File.ReadAllBytes(scenarioPath);
        var graph = ScenarioGraphLoader.LoadFromJson(_scenario.text);
        ScenarioRequirementsDocument sidecar = null;
        if (_sidecar != null)
        {
          var loaded = ScenarioRequirementsLoader.LoadSidecar(_sidecar.text);
          if (!loaded.IsValid) throw new InvalidOperationException(string.Join(" | ", loaded.Diagnostics.Select(value => value.Code + " " + value.Message)));
          sidecar = loaded.Document;
        }
        var manifest = ScenarioRequirementCompiler.Compile(graph, sourceBytes, sidecar, new ScenarioRequirementCompilationContext(DateTime.UtcNow));
        _manifest = manifest;
        var scenes = GetCompositionScenes();
        var composition = new ScenarioRequirementSceneComposition("open-scenes", scenes);
        var snapshot = ScenarioRequirementsSceneScanner.Scan(manifest, composition);
        _report = ScenarioRequirementValidationEngine.Validate(manifest, snapshot, composition);
        _plan = null;
      }
      catch (Exception ex)
      {
        _report = null;
        _manifest = null;
        _error = ex.Message;
      }
    }

    private void BuildPreview()
    {
      if (_report == null) return;
      var composition = new ScenarioRequirementSceneComposition(_compositionProfile != null ? _compositionProfile.Identifier : "open-scenes", GetCompositionScenes());
      RefreshGenerationConfigurations();
      _plan = _manifest != null
        ? ScenarioRequirementsApplyPlanner.CreatePlan(_manifest, composition, _generationConfigurations, _approvedGeneratedDeletions)
        : ScenarioRequirementsApplyPlanner.CreatePlan(_report.ScenarioIdentifier, _report.Results.Select(value => value.Requirement), composition, _generationConfigurations);
    }

    private void ApplyPlan()
    {
      if (_plan == null || !_plan.CanApply) return;
      var composition = new ScenarioRequirementSceneComposition(_compositionProfile != null ? _compositionProfile.Identifier : "open-scenes", GetCompositionScenes());
      RefreshGenerationConfigurations();
      var result = ScenarioRequirementsApplyService.Apply(_plan, composition, _generationConfigurations);
      if (!result.Succeeded) _error = result.Error;
      else Validate();
    }

    private static MessageType ToMessageType(ScenarioRequirementDiagnosticSeverity severity)
      => severity >= ScenarioRequirementDiagnosticSeverity.Error ? MessageType.Error : severity == ScenarioRequirementDiagnosticSeverity.Warning ? MessageType.Warning : MessageType.Info;

    private void DrawGenerationConfigurationEditor()
    {
      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
      {
        EditorGUILayout.LabelField("Generation Configuration", EditorStyles.boldLabel);
        foreach (var result in _report.Results.Where(value => value.Requirement.BindingHint != null && (value.Requirement.BindingHint.Mode == ScenarioRequirementBindingMode.GeneratedSceneObject || value.Requirement.BindingHint.Mode == ScenarioRequirementBindingMode.PrefabInstance)))
        {
          var key = result.Requirement.Key;
          if (!_generationPositions.TryGetValue(key, out var position)) position = Vector3.zero;
          if (!_generationScenes.TryGetValue(key, out var scenePath)) scenePath = GetCompositionScenes().FirstOrDefault()?.ScenePath ?? string.Empty;
          EditorGUILayout.LabelField(key.ToString());
          position = EditorGUILayout.Vector3Field("Position", position);
          scenePath = EditorGUILayout.TextField("Target Scene", scenePath);
          _generationPositions[key] = position;
          _generationScenes[key] = scenePath;
        }
      }
    }

    private void RefreshGenerationConfigurations()
    {
      foreach (var result in _report.Results)
      {
        var binding = result.Requirement.BindingHint;
        if (binding == null || (binding.Mode != ScenarioRequirementBindingMode.GeneratedSceneObject && binding.Mode != ScenarioRequirementBindingMode.PrefabInstance)) continue;
        var scenePath = _generationScenes.TryGetValue(result.Requirement.Key, out var configured) ? configured : GetCompositionScenes().FirstOrDefault()?.ScenePath ?? string.Empty;
        var position = _generationPositions.TryGetValue(result.Requirement.Key, out var configuredPosition) ? configuredPosition : Vector3.zero;
        _generationConfigurations[result.Requirement.Key] = new ScenarioRequirementGenerationConfiguration(position, null, null, scenePath, AssetDatabase.AssetPathToGUID(scenePath));
      }
    }

    private void BindSelectedTarget(ScenarioRequirementKey key, UnityEngine.Object target)
    {
      if (target == null)
      {
        _error = "Select a scene GameObject or Component before binding.";
        return;
      }
      var targetObject = target as GameObject ?? (target as Component)?.gameObject;
      if (targetObject == null || !targetObject.scene.IsValid() || !targetObject.scene.isLoaded)
      {
        _error = "Binding target must be an object in a loaded scene.";
        return;
      }
      var binding = targetObject.scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<ScenarioRequirementsSceneBinding>(true))
        .FirstOrDefault();
      if (binding == null)
      {
        var owner = new GameObject("Scenario Requirements Bindings");
        UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(owner, targetObject.scene);
        binding = owner.AddComponent<ScenarioRequirementsSceneBinding>();
        Undo.RegisterCreatedObjectUndo(owner, "Create Scenario Requirements Binding");
      }
      Undo.RecordObject(binding, "Bind Scenario Requirement");
      binding.SetCompositionIdentifier(_compositionProfile != null ? _compositionProfile.Identifier : string.Empty);
      binding.AddOrUpdateBinding(key, target, "Bound from Scenario Requirements window.");
      EditorUtility.SetDirty(binding);
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(binding.gameObject.scene);
      _error = null;
      Validate();
    }

    private IEnumerable<ScenarioRequirementCompositionScene> GetCompositionScenes()
    {
      if (_compositionProfile != null)
      {
        return _compositionProfile.Scenes
          .Where(value => value != null && !string.IsNullOrWhiteSpace(value.ScenePath))
          .Select(value => new ScenarioRequirementCompositionScene(value.ScenePath, value.SceneGuid, value.Role))
          .ToArray();
      }
      return Enumerable.Range(0, SceneManager.sceneCount)
        .Select(index => SceneManager.GetSceneAt(index))
        .Where(scene => scene.IsValid() && scene.isLoaded && !string.IsNullOrWhiteSpace(scene.path))
        .Select(scene => new ScenarioRequirementCompositionScene(scene.path, AssetDatabase.AssetPathToGUID(scene.path), _openSceneRole))
        .ToArray();
    }
  }
}
#endif
