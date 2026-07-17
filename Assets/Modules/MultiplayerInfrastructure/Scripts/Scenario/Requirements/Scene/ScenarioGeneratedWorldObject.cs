using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  [DisallowMultipleComponent]
  public sealed class ScenarioGeneratedWorldObject : MonoBehaviour
  {
    [SerializeField] private int _schemaVersion = 1;
    [SerializeField] private string _scenarioIdentifier;
    [SerializeField] private string _manifestFingerprint;
    [SerializeField] private string _compositionIdentifier;
    [SerializeField] private ScenarioRequirementKind _requirementKind;
    [SerializeField] private string _requirementIdentifier;
    [SerializeField] private string _factoryIdentifier;
    [SerializeField] private int _factoryVersion;
    [SerializeField] private string _targetSceneGuid;
    [SerializeField] private ScenarioRequirementScope _targetSceneRole;
    [SerializeField] private string _generationInputFingerprint;
    [SerializeField] private bool _isOrphan;

    public int SchemaVersion => _schemaVersion;
    public string ScenarioIdentifier => _scenarioIdentifier;
    public string ManifestFingerprint => _manifestFingerprint;
    public string CompositionIdentifier => _compositionIdentifier;
    public ScenarioRequirementKind RequirementKind => _requirementKind;
    public string RequirementIdentifier => _requirementIdentifier;
    public string FactoryIdentifier => _factoryIdentifier;
    public int FactoryVersion => _factoryVersion;
    public string TargetSceneGuid => _targetSceneGuid;
    public ScenarioRequirementScope TargetSceneRole => _targetSceneRole;
    public string GenerationInputFingerprint => _generationInputFingerprint;
    public bool IsOrphan => _isOrphan;

    public bool TryGetKey(out ScenarioRequirementKey key)
    {
      key = default;
      if (string.IsNullOrWhiteSpace(_requirementIdentifier)) return false;
      key = new ScenarioRequirementKey(_requirementKind, _requirementIdentifier.Trim());
      return true;
    }

    public string Identity => string.Join("|", _compositionIdentifier ?? string.Empty, _targetSceneGuid ?? string.Empty, TryGetKey(out var key) ? key.ToString() : string.Empty);

    public void Configure(
      string scenarioIdentifier,
      string manifestFingerprint,
      string compositionIdentifier,
      ScenarioRequirementKey key,
      string factoryIdentifier,
      int factoryVersion,
      string targetSceneGuid,
      ScenarioRequirementScope targetSceneRole,
      string generationInputFingerprint)
    {
      _scenarioIdentifier = scenarioIdentifier ?? string.Empty;
      _manifestFingerprint = manifestFingerprint ?? string.Empty;
      _compositionIdentifier = compositionIdentifier ?? string.Empty;
      _requirementKind = key.Kind;
      _requirementIdentifier = key.Identifier;
      _factoryIdentifier = factoryIdentifier ?? string.Empty;
      _factoryVersion = factoryVersion;
      _targetSceneGuid = targetSceneGuid ?? string.Empty;
      _targetSceneRole = targetSceneRole;
      _generationInputFingerprint = generationInputFingerprint ?? string.Empty;
      _isOrphan = false;
    }

    public void MarkOrphan() => _isOrphan = true;
  }
}
