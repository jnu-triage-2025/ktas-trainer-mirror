namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 의식수준 5단계(LOC; Level of Consciousness)를 표현합니다.
  /// </summary>
  public enum LOCLabel
  {
    /// <summary>
    /// Alert: 명료
    /// </summary>
    Alert,

    /// <summary>
    /// Drowsy: 기면
    /// </summary>
    Drowsy,

    /// <summary>
    /// Stupor: 혼미
    /// </summary>
    Stupor,

    /// <summary>
    /// SemiComa: 반혼수
    /// </summary>
    SemiComa,

    /// <summary>
    /// Coma: 혼수
    /// </summary>
    Coma,
  }
}
