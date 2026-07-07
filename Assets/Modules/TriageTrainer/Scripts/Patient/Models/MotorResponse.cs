namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// GCS(Glasgow Coma Scale)의 M(Motor Response, 운동 반응) 세부 항목을 표현합니다.
  /// 값은 해당 GCS 점수(1~6점)와 동일하게 매핑되어 있습니다.
  /// </summary>
  public enum MotorResponse
  {
    /// <summary>
    /// 1점(None): 어떤 자극에도 움직임이 전혀 없는 상태.
    /// </summary>
    None = 1,

    /// <summary>
    /// 2점(Extension): 통증 자극 시 팔다리를 뻣뻣하게 펴거나 밖으로 꼬는 상태(뇌제거 강직).
    /// </summary>
    Extension = 2,

    /// <summary>
    /// 3점(Abnormal flexion): 통증 자극 시 팔을 가슴 쪽으로 비정상적으로 굽히는 상태(뇌피질 제거 강직).
    /// </summary>
    AbnormalFlexion = 3,

    /// <summary>
    /// 4점(Normal flexion/Withdrawal): 아픈 부위를 만지면 아픈 곳에서 몸을 떼려고 움츠러드는 상태.
    /// </summary>
    Withdrawal = 4,

    /// <summary>
    /// 5점(Localizing): 통증 자극을 주었을 때, 아픈 부위를 치우려고 환자의 손이 통증 지점까지 올라오는 상태.
    /// </summary>
    Localizing = 5,

    /// <summary>
    /// 6점(Obeys commands): "손을 들어보세요", "주먹 쥐어보세요" 같은 간단한 지시를 정확히 수행하는 상태.
    /// </summary>
    ObeysCommands = 6,
  }
}
