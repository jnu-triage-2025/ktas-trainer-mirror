namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 동공 반사 결과를 표현합니다.
  /// </summary>
  public enum PupillaryResponse
  {
    /// <summary>
    /// 정상
    /// </summary>
    Normal,

    /// <summary>
    /// 비정상: 구체화되지 않았습니다. 동공 반사의 실제 구현 상황에 따라 세개 값(좌측만 미반응, 우측만 미반응, 양측 모두 미반응)으로 나누어질 수도 있습니다.
    /// </summary>
    Abnormal,

    // LeftNotResponding,
    // RightNotResponding,
    // BothNotResponding,
  }
}
