namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 식염수 수액 세트(조합 완료). 시나리오 구식 산출물명 `ns1_ready` 에 대응한다.
  /// 생리식염수 1L + 수액세트를 조합하여 만든다.
  /// 아이템 리소스(아이콘/모델)는 `intravenous_set` 를 복사해 사용한다.
  /// </summary>
  public class NormalSalineIntravenousReady : MedicalItem
  {
    public new const string Identifier   = "normal_saline_intravenous_ready";
    public new const string DisplayName  = "준비된 생리식염수 1L 수액백";
    public new const string Description  = "준비된 생리식염수 1L 수액백";
  }
}
