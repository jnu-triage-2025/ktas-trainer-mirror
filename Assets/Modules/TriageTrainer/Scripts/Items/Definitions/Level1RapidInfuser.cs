using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;

namespace TriageTrainer.ItemDefinitions
{
  /// <summary>월드에 Level 1 급속 주입기를 배치하는 아이템.</summary>
  public sealed class Level1RapidInfuser : MedicalItem
  {
    public const string Identifier = "level1_rapid_infuser";
    public const string DisplayName = "Level 1 급속 주입기";
    public const string Description = "우클릭하여 바라보는 방향 앞에 급속 주입기를 배치합니다.";
    public new const bool IsStackable = false;
    public new const int MaxStackCount = 1;

    public override ActionResult OnUse(PlayerController player, Entity target)
    {
      if (player != null)
        player.RequestPlaceHeldEntityPreset(Identifier, Identifier);
      return ActionResult.Cancelled;
    }
  }
}
