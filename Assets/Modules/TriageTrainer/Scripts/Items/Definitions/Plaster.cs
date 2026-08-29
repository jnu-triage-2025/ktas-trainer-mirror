namespace TriageTrainer.ItemDefinitions
{
  public class Plaster : MedicalItem
  {
    public new const string Identifier = "plaster";
    public new const string DisplayName = "플라스터";
    public new const string Description = "고정용 플라스터.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;

    public new const bool HasDurability = true;
    public new const bool EnabledDeltaDurability = true;
    public new const int MaxDurability = 16;
    public new const int DeltaDurabilityOnUse = -1;
  }
}
