using System.Collections;
using System.Diagnostics;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_StartChestCompression()
    {
      Register("start_chest_compression", Event_StartChestCompression);
    }

    private IEnumerator Event_StartChestCompression()
    {
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, true);
      PlayPatientAChestCompressionAnimation();
      Debug.Log("[EmitSystemMessage] 가슴 압박 애니메이션을 시작했습니다.");
      yield break;
    }
  }
}
