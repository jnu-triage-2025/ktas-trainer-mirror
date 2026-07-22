namespace TriageTrainer.ItemDefinitions
{
  public class HandyClock : MedicalItem
  {
    public const string Identifier = "handy_clock";
    public const string DisplayName = "시계";
    public const string Description = "주석 주괴, 소형 톱니, 소형 사슬로 제작한 시계입니다.";

    public new const bool IsStackable = false;
    public new const int MaxStackCount = 1;
  }
}
