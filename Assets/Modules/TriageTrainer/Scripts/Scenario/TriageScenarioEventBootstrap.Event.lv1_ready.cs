using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_Lv1Ready()
    {
      Register("lv1_ready", Event_Lv1Ready);
    }

    private IEnumerator Event_Lv1Ready()
    {
      SetActiveIfPresent(_level1ReadyVisual, true);
      EmitSystemMessage("Level 1 장비 준비 상태 연출을 적용했습니다.");
      yield break;
    }
  }
}
