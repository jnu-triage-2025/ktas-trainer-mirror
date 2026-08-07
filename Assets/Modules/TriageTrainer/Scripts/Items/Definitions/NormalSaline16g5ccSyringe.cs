namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 생리식염수가 든 16g 카테터 + 5cc 주사기 조합 완제품입니다.
  /// Cannula16g + Syringe5cc + NormalSaline20ml 를 조합하여 만든다.
  /// </summary>
  public class NormalSaline16g5ccSyringe : MedicalItem
  {
    public new const string Identifier   = "normal_saline_16g_5cc_syringe";
    public new const string DisplayName  = "생리식염수가 든 16g 5cc 주사기";
    public new const string Description  = "16게이지 카테터가 연결된 5cc 주사기에 생리식염수가 준비되어 있습니다.";
  }
}
