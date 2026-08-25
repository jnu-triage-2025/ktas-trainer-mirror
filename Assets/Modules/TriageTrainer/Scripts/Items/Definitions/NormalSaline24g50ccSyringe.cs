namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 생리식염수가 든 24g 카테터 + 50cc 주사기 조합 완제품입니다.
  /// Cannula24g + Syringe50cc + NormalSaline20ml 를 조합하여 만든다.
  /// </summary>
  public class NormalSaline24g50ccSyringe : MedicalItem
  {
    public new const string Identifier = "normal_saline_24g_50cc_syringe";
    public new const string DisplayName = "생리식염수가 든 24g 50cc 주사기";
    public new const string Description = "24게이지 카테터가 연결된 50cc 주사기에 생리식염수가 준비되어 있습니다.";
  }
}
