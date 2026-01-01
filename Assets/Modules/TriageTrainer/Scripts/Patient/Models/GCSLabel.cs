namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// Glasgow Coma Scale (GCS) 점수 분포 별 유형을 표현합니다.
  /// </summary>
  public enum GCSLabel
  {
    /// <summary>
    /// 중증(3점 ~ 8점)
    /// </summary>
    Severe,   // 3 - 8

    /// <summary>
    /// 중등도(9점 ~ 12점)
    /// </summary>
    Moderate, // 9 - 12

    /// <summary>
    /// 경증(13점 ~ 15점)
    /// </summary>
    Mild,     // 13 - 15

    /// <summary>
    /// 이상치: 값이 올바르지 않음
    /// </summary>
    NA,
  }
}
