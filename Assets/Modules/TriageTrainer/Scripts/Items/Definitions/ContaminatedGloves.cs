using MultiplayerInfrastructure.ItemSystem;

namespace TriageTrainer.ItemDefinitions
{
  [EquippableGlove]
  public class ContaminatedGloves : MedicalItem
  {
    /* Comment
     * Reserved: 멸균 장갑을 사용한 후, 오염된 장갑으로 시스템 상 교체될 소요가 있을 때 사용
     */
    public new const string Identifier = "contaminated_gloves";
    public new const string DisplayName = "오염된 장갑";
    public new const string Description = "";
  }
}
