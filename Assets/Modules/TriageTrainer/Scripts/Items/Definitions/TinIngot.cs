using MultiplayerInfrastructure.ItemSystem;
namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  public class TinIngot : MedicalItem
  {
    public new const string Identifier = "tin_ingot";
    public new const string DisplayName = "주석 주괴";
    public new const string Description = "시계 제작에 사용되는 주석 주괴입니다.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;
  }
}
