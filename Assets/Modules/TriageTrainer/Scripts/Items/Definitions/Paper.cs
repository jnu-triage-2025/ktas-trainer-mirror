using MultiplayerInfrastructure.ItemSystem;

namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  [IntendedMissingItemSpriteAttribute]
  public class Paper : MedicalItem
  {
    public new const string Identifier = "paper";
    public new const string DisplayName = "종이";
    public new const string Description = "";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;
  }
}
