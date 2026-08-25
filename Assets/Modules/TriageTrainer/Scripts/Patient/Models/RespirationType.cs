namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 환자의 호흡 상태를 정의합니다.
  /// </summary>
  public enum RespirationType
  {
    /// <summary>
    /// 규칙적
    /// </summary>
    Regular,

    /// <summary>
    /// 불규칙적
    /// </summary>
    Irregular,

    /// <summary>
    /// 임종호흡
    /// </summary>
    ChainStocks,
  }
}
