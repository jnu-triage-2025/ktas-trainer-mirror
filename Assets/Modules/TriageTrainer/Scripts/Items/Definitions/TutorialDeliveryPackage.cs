using MultiplayerInfrastructure.ItemSystem;
namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  public class TutorialDeliveryPackage : MedicalItem
  {
    public new const string Identifier = "tutorial_delivery_package";
    public new const string DisplayName = "택배";
    public new const string Description = "튜토리얼 안내자에게 전달할 택배입니다.";

    public new const bool IsStackable = false;
    public new const int MaxStackCount = 1;
  }
}
