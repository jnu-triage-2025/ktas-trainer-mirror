using MultiplayerInfrastructure.ItemSystem;
namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  public class SmallGear : MedicalItem
  {
    public new const string Identifier = "small_gear";
    public new const string DisplayName = "소형 톱니";
    public new const string Description = "시계 제작에 사용되는 소형 톱니입니다.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;
  }
}
