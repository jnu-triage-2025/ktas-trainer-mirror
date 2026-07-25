using MultiplayerInfrastructure.ItemSystem;
namespace TriageTrainer.ItemDefinitions
{
  [IntendedMissing3DModelAttribute]
  public class SmallChain : MedicalItem
  {
    public const string Identifier = "small_chain";
    public const string DisplayName = "소형 사슬";
    public const string Description = "시계 제작에 사용되는 소형 사슬입니다.";

    public new const bool IsStackable = true;
    public new const int MaxStackCount = 64;
  }
}
