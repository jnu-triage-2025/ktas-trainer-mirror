namespace TriageTrainer.ItemDefinitions
{
  public class IntravenousSet : MedicalItem
  {
    public new const string Identifier = "intravenous_set";
    public new const string DisplayName = "수액세트";
    public new const string Description = "수액 주입시 사용합니다.";

    public new const bool HasDurability = true;
    public new const bool EnabledDeltaDurability = true;
    public new const int MaxDurability = 16;
    public new const int DeltaDurabilityOnUse = -1;
  }
}
