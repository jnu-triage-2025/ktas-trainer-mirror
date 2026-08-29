using System.Collections;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string StopAmbuAndCompEventIdentifier = "stop_ambu_and_comp";

    private void RegisterEvent_StopAmbuAndComp()
    {
      Register(StopAmbuAndCompEventIdentifier, Event_StopAmbuAndComp);
    }

    private IEnumerator Event_StopAmbuAndComp()
    {
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
      CompletePatientACprCycle();

      // E031/E035 는 역할 브랜치 밖의 InvokeEvent 노드라서 그래프를 순회하는 권위 피어에서만
      // 실행된다. 반면 CPR 수행자의 위치 고정과 애니메이션은 가슴압박에 상호작용한 피어가 자기
      // 로컬 상태로 시작한다. 종료를 전달하지 않으면 그 피어에서는 앵커 고정과 연출이 풀리지
      // 않고, 다음 주기에서 이전 주기의 수행자 상태가 그대로 재사용된다.
      ScenarioNetworkRelay.InvokePresentationEventAuthoritative(StopAmbuAndCompEventIdentifier);
#if UNITY_EDITOR
      Debug.Log("[EmitSystemMessage] 앰부배깅/가슴압박 애니메이션을 중지했습니다.");
#endif
      yield break;
    }
  }
}
