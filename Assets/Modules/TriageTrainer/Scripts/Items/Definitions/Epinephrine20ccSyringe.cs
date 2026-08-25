namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 에피네프린이 든 20cc 주사기(카테터 없음) 조합 완제품입니다.
  /// Syringe20cc + EpinephrineAmpule 를 조합하여 만든다.
  /// </summary>
  public class Epinephrine20ccSyringe : MedicalItem
  {
    public new const string Identifier = "epinephrine_20cc_syringe";
    public new const string DisplayName = "에피네프린이 든 20cc 주사기";
    public new const string Description = "20cc 주사기에 에피네프린이 준비되어 있습니다.";
  }
}
