namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 별도 사용 효과가 없는 단순 혈액백 아이템입니다.
  /// </summary>
  public class BloodBag : MedicalItem
  {
    public new const string Identifier  = "blood_bag";
    public new const string DisplayName = "혈액백";
    public new const string Description = "수혈에 사용하는 혈액이 담긴 백입니다.";
  }
}
