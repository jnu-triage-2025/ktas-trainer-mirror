using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;

namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  [IntendedMissingItemSpriteAttribute]
  public class TutorialDecoyKimGangsanMail : MedicalItem
  {
    public new const string Identifier = "tutorial_delivery_decoy_kim_gangsan_mail";
    public new const string DisplayName = "우편: 김강산님";
    public new const string Description = "김강산님 앞으로 온 우편물입니다.";

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
