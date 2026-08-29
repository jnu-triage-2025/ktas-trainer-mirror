using System.Collections;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string StartAmbuBaggingEventIdentifier = "start_ambubagging";

    private void RegisterEvent_StartAmbuBagging()
    {
      Register(StartAmbuBaggingEventIdentifier, Event_StartAmbuBagging);
    }

    private IEnumerator Event_StartAmbuBagging()
    {
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, true);

      // 앰부배깅 애니메이터는 각 피어의 씬 오브젝트다. 권위 피어에서만 켜면 나머지 피어에서는
      // 앰부배깅이 보이지 않고, 종료 통지만 도착해 끄는 쪽만 맞아떨어진다.
      // (클라이언트 컨텍스트에서 이 이벤트를 실행할 때는 통지가 그대로 반환되므로 재귀하지 않는다.)
      ScenarioNetworkRelay.InvokePresentationEventAuthoritative(StartAmbuBaggingEventIdentifier);
#if UNITY_EDITOR
      Debug.Log("[EmitSystemMessage] 앰부배깅 애니메이션을 시작했습니다.");
#endif
      yield break;
    }
  }
}
