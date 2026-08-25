using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_PlayerAMoveToTriage()
    {
      Register("player_a_move_to_triage", Event_PlayerAMoveToTriage);
    }

    private IEnumerator Event_PlayerAMoveToTriage()
    {
      ResolveRuntimeReferencesIfNeeded();

      if (_playerAMoveToTriageDurationSeconds > 0f)
      {
        yield return MoveTransformTo(_nurseATransform, _playerATriagePoint, _playerAMoveToTriageDurationSeconds);
      }
      else
      {
        SnapTransformTo(_nurseATransform, _playerATriagePoint);
      }

      EmitSystemMessage("간호사 A를 트리아지 구역으로 이동시켰습니다.");
      yield break;
    }
  }
}
