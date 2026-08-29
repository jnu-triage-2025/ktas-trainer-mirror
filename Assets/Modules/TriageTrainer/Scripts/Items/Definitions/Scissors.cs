namespace TriageTrainer.ItemDefinitions
{
  public class Scissors : MedicalItem
  {
    public new const string Identifier = "scissors";
    public new const string DisplayName = "가위";
    public new const string Description = "의복 제거 시 사용합니다.";

    public new const bool HasDurability = true;
    public new const bool EnabledDeltaDurability = true;
    public new const int MaxDurability = 16;
    public new const int DeltaDurabilityOnUse = -1;
  }
}
