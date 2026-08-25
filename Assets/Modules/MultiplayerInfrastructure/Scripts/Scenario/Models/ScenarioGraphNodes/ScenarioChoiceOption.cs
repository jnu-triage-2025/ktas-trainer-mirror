using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// IInteractable과 호환 가능하도록 설계됨
  /// </summary>
  public sealed class ScenarioChoiceOption
  {
    public string DisplayText { get; set; }
    public string DisplayIconIdentifier { get; set; }
    public Color DisplayColor { get; set; }

    public string NextNodeIdentifier { get; set; }
  }
}
