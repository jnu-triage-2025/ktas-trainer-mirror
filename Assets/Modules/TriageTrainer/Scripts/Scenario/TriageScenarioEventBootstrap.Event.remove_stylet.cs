using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_RemoveStylet()
    {
      Register("remove_stylet", Event_RemoveStylet);
    }

    private IEnumerator Event_RemoveStylet()
    {
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, false);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, true);
      EmitSystemMessage("환자 A 기관내관 스타일렛 제거 연출을 적용했습니다.");
      yield break;
    }
  }
}
