using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_BcdToTriage()
    {
      Register("B_C_D_to_triage", Event_BcdToTriage);
    }

    private IEnumerator Event_BcdToTriage()
    {
      ResolveRuntimeReferencesIfNeeded();

      if (_instantMoveBcd)
      {
        SnapTransformTo(_nurseBTransform, _triageArrivalPoint);
        SnapTransformTo(_nurseCTransform, _triageArrivalPoint);
        SnapTransformTo(_nurseDTransform, _triageArrivalPoint);
      }
      else
      {
        yield return MoveTransformTo(_nurseBTransform, _triageArrivalPoint, _bcdMoveDurationSeconds);
        yield return MoveTransformTo(_nurseCTransform, _triageArrivalPoint, _bcdMoveDurationSeconds);
        yield return MoveTransformTo(_nurseDTransform, _triageArrivalPoint, _bcdMoveDurationSeconds);
      }

      EmitSystemMessage("간호사 B/C/D가 트리아지 구역으로 이동했습니다.");
      yield break;
    }
  }
}
