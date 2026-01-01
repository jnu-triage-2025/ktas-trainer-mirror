namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 신체 온도의 유형을 표현합니다.
  /// </summary>
  public enum BodyTemperatureType
  {
    /// <summary>
    /// 저체온증, (-INF, 36.0) 범위를 표현합니다.
    /// </summary>
    Hypothermia,  // < 35.5

    /// <summary>
    /// 정상, [36.0, 37.5] 범위를 표현합니다.
    /// </summary>
    Normal,       // 35.5 <= x <= 37.5

    /// <summary>
    /// 발열, (37.5, 38.0] 범위를 표현합니다.
    /// </summary>
    Fever,        // 37.5 < x <= 38.0

    /// <summary>
    /// 고열, (38.0, INF) 범위를 표현합니다.
    /// </summary>
    HighFever,    // 38.0 < x
  }
}
