namespace TriageTrainer.ItemDefinitions
{
  public class TinIngot : MedicalItem
  {
    public const string Identifier = "tin_ingot";
    public const string DisplayName = "주석 주괴";
    public const string Description = "시계 제작에 사용되는 주석 주괴입니다.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;
  }
}
