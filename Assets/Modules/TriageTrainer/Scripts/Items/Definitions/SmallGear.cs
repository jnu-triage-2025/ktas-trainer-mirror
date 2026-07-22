namespace TriageTrainer.ItemDefinitions
{
  public class SmallGear : MedicalItem
  {
    public const string Identifier = "small_gear";
    public const string DisplayName = "소형 톱니";
    public const string Description = "시계 제작에 사용되는 소형 톱니입니다.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;
  }
}
