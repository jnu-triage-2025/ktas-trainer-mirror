namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 생리식염수가 든 20cc 주사기(카테터 없음) 조합 완제품입니다.
  /// Syringe20cc + NormalSaline20ml 를 조합하여 만든다.
  /// </summary>
  public class NormalSaline20ccSyringe : MedicalItem
  {
    public const string Identifier   = "normal_saline_20cc_syringe";
    public const string DisplayName  = "생리식염수가 든 20cc 주사기";
    public const string Description  = "20cc 주사기에 생리식염수가 준비되어 있습니다.";
  }
}
