namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 맥박의 속도 유형을 정의합니다. 
  /// </summary>
  public enum BloodPulseSpeedType
  {
    /// <summary>
    /// 서맥, 맥박수가 60회/분 미만일 때를 표현합니다.
    /// </summary>
    Bradycardia,  // < 60

    /// <summary>
    /// 정상, 맥박수가 60회/분 이상, 100회/분 이하일 때를 표현합니다.
    /// </summary>
    Normal,       // 60 <= x <= 100

    /// <summary>
    /// 빈맥, 맥박수가 100회/분 초과일 때를 표현합니다. 
    /// (구현 팁: 과다출혈시의 맥박은 주로 [140, 170]회/분 범위에서 관찰됩니다.
    /// 이후 이 값으로부터 구체적인 맥박수를 결정해야 하는 경우, 특별한 이유가 아니라면
    /// 이 범위의 값으로 결정해야 합니다.)
    /// </summary>
    Tachycardia,  // > 100 (most case: 140 < x < 170)
  }
}
