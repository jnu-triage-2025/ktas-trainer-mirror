namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 에피네프린이 든 50cc 주사기(카테터 없음) 조합 완제품입니다.
  /// Syringe50cc + EpinephrineAmpule 를 조합하여 만든다.
  /// </summary>
  public class Epinephrine50ccSyringe : MedicalItem
  {
    public new const string Identifier = "epinephrine_50cc_syringe";
    public new const string DisplayName = "에피네프린이 든 50cc 주사기";
    public new const string Description = "50cc 주사기에 에피네프린이 준비되어 있습니다.";
  }
}
