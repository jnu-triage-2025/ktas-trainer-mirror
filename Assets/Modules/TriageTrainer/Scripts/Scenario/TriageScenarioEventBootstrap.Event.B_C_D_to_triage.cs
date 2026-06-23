using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_BcdToTriage()
    {
      // disaster_intro 는 camelCase 식별자, patient_b_c_ct 는 snake_case 식별자를 사용하므로
      // 동일 핸들러를 두 식별자 모두에 등록한다.
      Register("B_C_D_to_triage", Event_BcdToTriage);
      Register("b_c_d_to_triage", Event_BcdToTriage);
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
