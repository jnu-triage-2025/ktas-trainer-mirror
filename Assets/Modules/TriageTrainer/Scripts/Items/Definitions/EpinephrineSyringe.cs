namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 에피네프린이 든 주사기(약물 준비 완료).
  /// 5cc 주사기 + 에피네프린 앰플을 조합하여 만든다.
  /// </summary>
  public class EpinephrineSyringe : MedicalItem
  {
    public const string Identifier   = "epinephrine_syringe";
    public const string DisplayName  = "에피네프린 주사기";
    public const string Description  = "에피네프린 1mg 이 준비된 주사기입니다. 중심정맥관을 통해 투여합니다.";
  }
}
