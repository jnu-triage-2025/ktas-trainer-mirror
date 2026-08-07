using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;

namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  [IntendedMissingItemSpriteAttribute]
  public class TutorialDecoy8909 : MedicalItem
  {
    public new const string Identifier = "tutorial_delivery_decoy_8909";
    public new const string DisplayName = "택배: 8909";
    public new const string Description = "수취인 이름이 없는 택배입니다.";

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
