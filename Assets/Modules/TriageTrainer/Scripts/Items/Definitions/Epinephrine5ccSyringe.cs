namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 에피네프린이 든 5cc 주사기(카테터 없음) 조합 완제품입니다.
  /// Syringe5cc + EpinephrineAmpule 를 조합하여 만든다.
  /// (구 `epinephrine_syringe` 를 명명 규칙에 맞춰 대체한 아이템)
  /// </summary>
  public class Epinephrine5ccSyringe : MedicalItem
  {
    public new const string Identifier   = "epinephrine_5cc_syringe";
    public new const string DisplayName  = "에피네프린이 든 5cc 주사기";
    public new const string Description  = "5cc 주사기에 에피네프린이 준비되어 있습니다.";
  }
}
