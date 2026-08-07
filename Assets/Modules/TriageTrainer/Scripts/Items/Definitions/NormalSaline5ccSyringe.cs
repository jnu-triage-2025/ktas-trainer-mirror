namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 생리식염수가 든 5cc 주사기(카테터 없음) 조합 완제품입니다.
  /// Syringe5cc + NormalSaline20ml 를 조합하여 만든다.
  /// </summary>
  public class NormalSaline5ccSyringe : MedicalItem
  {
    public new const string Identifier   = "normal_saline_5cc_syringe";
    public new const string DisplayName  = "생리식염수가 든 5cc 주사기";
    public new const string Description  = "5cc 주사기에 생리식염수가 준비되어 있습니다.";
  }
}
