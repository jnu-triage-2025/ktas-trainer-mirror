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

    // 시나리오 실행을 fire하는 interactable의 기본 아이콘은 message-circle 입니다.
    // NPC 정의에서 아이콘을 명시적으로 지정하면 그 값으로 덮어써집니다.
    public Sprite DisplayIcon => _displayIcon?.Resolve(IconSpriteIdentifiers.ScenarioDefault);
    public bool AllowDisplayIconFallback => !(_displayIcon?.IsExplicitNone ?? false);
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
