namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 노르에피네프린이 든 5cc 주사기(카테터 없음) 조합 완제품입니다.
  /// Syringe5cc + NorepinephrineAmpule 를 조합하여 만든다.
  /// </summary>
  public class Norepinephrine5ccSyringe : MedicalItem
  {
    public new const string Identifier   = "norepinephrine_5cc_syringe";
    public new const string DisplayName  = "노르에피네프린이 든 5cc 주사기";
    public new const string Description  = "5cc 주사기에 노르에피네프린이 준비되어 있습니다.";
  }
}
