namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 환자에게 요구되는 약물의 정보입니다. 정맥로를 통해 주입되어야 하는 약의 유형과 양을 표현하도록 의도되었습니다.
  /// </summary>
  public class RequiredDrug
  {
    /// <summary>
    /// 약 식별자
    /// </summary>
    public Drug drug;

    /// <summary>
    /// 양
    /// </summary>
    public int amount;
  }
}
