using System;
using MultiplayerInfrastructure.Commons;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [Serializable]
  public class NPCScenarioInteractDefinition
  {
    [SerializeField] private string _displayText = "시나리오 시작";
    [SerializeField] private IconSpriteReference _displayIcon = new();
    [SerializeField] private Color _displayColor = Color.white;
    [SerializeField] private string _scenarioIdentifier;
    [SerializeField] private string _scenarioStartNodeIdentifier;

    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon?.Resolve();
    public Color DisplayColor => _displayColor;
    public string ScenarioIdentifier => _scenarioIdentifier;
    public string ScenarioStartNodeIdentifier => _scenarioStartNodeIdentifier;

    public bool IsValid => !string.IsNullOrWhiteSpace(_scenarioIdentifier);

    public NPCScenarioInteractDefinition Clone()
    {
      return new NPCScenarioInteractDefinition
      {
        _displayText = _displayText,
        _displayIcon = _displayIcon?.Clone(),
        _displayColor = _displayColor,
        _scenarioIdentifier = _scenarioIdentifier,
        _scenarioStartNodeIdentifier = _scenarioStartNodeIdentifier
      };
    }
  }
}
