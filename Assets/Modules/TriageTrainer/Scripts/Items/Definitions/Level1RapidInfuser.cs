using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;

namespace TriageTrainer.ItemDefinitions
{
  /// <summary>월드에 Level 1 급속 주입기를 배치하는 아이템.</summary>
  public sealed class Level1RapidInfuser : MedicalItem
  {
    // 설치체는 Resources 모델이 아닌 EntityPreset에 등록된 엔티티 프리팹으로 스폰한다.
    private const string WorldEntityPresetIdentifier = "level1_rapid_infuser";
    public const string Identifier = "level1_rapid_infuser";
    public const string DisplayName = "Level 1 급속 주입기";
    public const string Description = "우클릭하여 바라보는 방향 앞에 급속 주입기를 배치합니다.";
    public new const bool IsStackable = false;
    public new const int MaxStackCount = 1;

    public override ActionResult OnUse(
      PlayerController player,
      MultiplayerInfrastructure.Entity.Entity target)
    {
      if (player != null)
        player.RequestPlaceHeldEntityPreset(Identifier, WorldEntityPresetIdentifier);
      return ActionResult.Cancelled;
    }
  }
}
