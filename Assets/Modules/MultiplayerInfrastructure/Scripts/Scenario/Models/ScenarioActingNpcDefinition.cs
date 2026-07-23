using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioActingNpcType
  {
    Npc
  }

  public enum ScenarioActingNpcInteractionType
  {
    StartScenario,
    ItemSubmission
  }

  /// <summary>
  /// 시나리오가 시작 또는 그래프 진행 중 preset으로 만들고 종료 시 선택적으로 정리할 Acting NPC 정의.
  /// 외형과 네트워크 프리팹은 preset catalog가 담당하고, 인스턴스별 데이터는 이 정의가 담당한다.
  /// </summary>
  public sealed class ScenarioActingNpcDefinition
  {
    public string Identifier { get; set; }
    public ScenarioActingNpcType ActingNpcType { get; set; } = ScenarioActingNpcType.Npc;
    public string PresetIdentifier { get; set; }
    public string DisplayName { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public bool SpawnOnStart { get; set; } = true;
    public bool DespawnOnScenarioEnd { get; set; } = true;
    public IReadOnlyList<ScenarioActingNpcInteractionDefinition> Interactions { get; set; }
      = Array.Empty<ScenarioActingNpcInteractionDefinition>();
  }

  public sealed class ScenarioActingNpcInteractionDefinition
  {
    public string Identifier { get; set; }
    public ScenarioActingNpcInteractionType InteractionType { get; set; }
    public string DisplayText { get; set; }
    public string IconIdentifier { get; set; }
    public string ScenarioIdentifier { get; set; }
    public string ScenarioStartNodeIdentifier { get; set; }
    public string Title { get; set; }
    public string SubmitButtonText { get; set; }
    public IReadOnlyList<ScenarioActingNpcItemRequirement> RequiredItems { get; set; }
      = Array.Empty<ScenarioActingNpcItemRequirement>();
    public string CompletionSignalIdentifier { get; set; }
    public bool ConsumeOnce { get; set; } = true;
    public bool Enabled { get; set; } = true;
  }

  public sealed class ScenarioActingNpcItemRequirement
  {
    public string ItemIdentifier { get; set; }
    public int Count { get; set; } = 1;
  }
}
