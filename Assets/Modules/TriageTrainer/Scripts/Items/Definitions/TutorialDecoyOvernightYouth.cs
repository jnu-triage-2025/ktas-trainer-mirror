using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;

namespace TriageTrainer.ItemDefinitions
{
  public class TutorialDecoyOvernightYouth : MedicalItem
  {
    public const string Identifier = "tutorial_delivery_decoy_overnight_youth";
    public const string DisplayName = "배달: 밤샜음 청년";
    public const string Description = "모자님 앞 택배가 아닌 배달 물품입니다.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;

    /// <summary>
    /// 획득 시 기본 signal 대신 튜토리얼 공통 오답 signal을 발행한다.
    /// 시나리오 그래프는 이 signal을 DisinteractableDialogue로 감시한다.
    /// </summary>
    public override void OnGet(PlayerController player)
    {
      ScenarioInteractionSignals.Raise("tutorial-decoy-package-on-pickuped");
    }
  }
}
