using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public enum ScenarioRequirementsValidationProfileMode { Authoring, Development, Production }

  [Serializable]
  public sealed class ScenarioPattern
  {
    [SerializeField] private string _pattern;
    [SerializeField] private bool _isExact;
    public string Pattern => _pattern ?? string.Empty;
    public bool IsExact => _isExact;
  }

  [Serializable]
  public sealed class ScenarioSceneRoleEntry
  {
    [SerializeField] private string _scenePath;
    [SerializeField] private string _sceneGuid;
    [SerializeField] private ScenarioRequirementScope _role = ScenarioRequirementScope.AnyLoadedScene;
    [SerializeField] private int _minimumLoadCount = 1;
    [SerializeField] private int _maximumLoadCount = 1;
    [SerializeField] private bool _optional;
    [SerializeField] private ScenarioRequirementAuthority _authority = ScenarioRequirementAuthority.Any;

    public string ScenePath => _scenePath ?? string.Empty;
    public string SceneGuid => _sceneGuid ?? string.Empty;
    public ScenarioRequirementScope Role => _role;
    public int MinimumLoadCount => _minimumLoadCount;
    public int MaximumLoadCount => _maximumLoadCount;
    public bool Optional => _optional;
    public ScenarioRequirementAuthority Authority => _authority;

#if UNITY_EDITOR
    internal void SynchronizeSceneGuid()
    {
      if (string.IsNullOrWhiteSpace(_scenePath)) return;
      _sceneGuid = UnityEditor.AssetDatabase.AssetPathToGUID(_scenePath) ?? string.Empty;
    }
#endif
  }

  [Serializable]
  public sealed class ScenarioRequirementsValidationProfile
  {
    [SerializeField] private ScenarioRequirementsValidationProfileMode _mode = ScenarioRequirementsValidationProfileMode.Authoring;
    [SerializeField] private bool _failOnIndeterminate;
    [SerializeField] private bool _requireProfileForBuild;
    [SerializeField] private bool _emitJsonReport;
    [SerializeField] private string _reportPath;

    public ScenarioRequirementsValidationProfileMode Mode => _mode;
    public bool FailOnIndeterminate => _failOnIndeterminate;
    public bool RequireProfileForBuild => _requireProfileForBuild;
    public bool EmitJsonReport => _emitJsonReport;
    public string ReportPath => _reportPath ?? string.Empty;
  }

  [CreateAssetMenu(fileName = "ScenarioSceneCompositionProfile", menuName = "Multiplayer Infrastructure/Scenario Scene Composition Profile")]
  public sealed class ScenarioSceneCompositionProfile : ScriptableObject
  {
    [SerializeField] private string _identifier;
    [SerializeField] private List<ScenarioPattern> _scenarioSelectors = new List<ScenarioPattern>();
    [SerializeField] private List<ScenarioSceneRoleEntry> _scenes = new List<ScenarioSceneRoleEntry>();
    [SerializeField] private ScenarioRequirementsValidationProfile _validationProfile = new ScenarioRequirementsValidationProfile();

    public string Identifier => _identifier ?? string.Empty;
    public IReadOnlyList<ScenarioPattern> ScenarioSelectors => new ReadOnlyCollection<ScenarioPattern>(_scenarioSelectors ?? new List<ScenarioPattern>());
    public IReadOnlyList<ScenarioSceneRoleEntry> Scenes => new ReadOnlyCollection<ScenarioSceneRoleEntry>(_scenes ?? new List<ScenarioSceneRoleEntry>());
    public ScenarioRequirementsValidationProfile ValidationProfile => _validationProfile ?? new ScenarioRequirementsValidationProfile();

    public bool Matches(string scenarioIdentifier)
    {
      var exact = ScenarioSelectors.Where(value => value != null && value.IsExact && string.Equals(value.Pattern, scenarioIdentifier, StringComparison.Ordinal)).ToArray();
      if (exact.Length > 0) return true;
      return ScenarioSelectors.Any(value => value != null && !value.IsExact && WildcardMatch(value.Pattern, scenarioIdentifier));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      foreach (var scene in _scenes ?? new List<ScenarioSceneRoleEntry>()) scene?.SynchronizeSceneGuid();
    }
#endif

    private static bool WildcardMatch(string pattern, string value)
    {
      if (string.IsNullOrEmpty(pattern)) return false;
      var parts = pattern.Split('*');
      if (parts.Length == 1) return string.Equals(pattern, value, StringComparison.Ordinal);
      var position = 0;
      if (!value.StartsWith(parts[0], StringComparison.Ordinal)) return false;
      position = parts[0].Length;
      for (var index = 1; index < parts.Length - 1; index++)
      {
        var found = value.IndexOf(parts[index], position, StringComparison.Ordinal);
        if (found < 0) return false;
        position = found + parts[index].Length;
      }
      return value.Substring(position).EndsWith(parts[parts.Length - 1], StringComparison.Ordinal);
    }
  }
}
