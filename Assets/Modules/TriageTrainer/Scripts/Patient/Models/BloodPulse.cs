namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 맥박을 표현합니다.
  /// </summary>
  public class BloodPulse
  {

    /// <summary>
    /// 분당 맥박수
    /// </summary>
    public int rate;

    /// <summary>
    /// 맥박의 세기 유형을 BloodPulseForceType에 의해 정의된 유형에 따라 표현합니다.
    /// </summary>
    public BloodPulseForceType forceType;

    /// <summary>
    /// 맥박의 속도 유형을 분당 맥박수 값으로부터 계산하여 반환합니다. 반환값은 BloodPulseSpeedType에 의해 정의된 유형에 따라 표현됩니다.
    /// </summary>
    public BloodPulseSpeedType SpeedType
    {
      get
      {
        if (rate < 60) return BloodPulseSpeedType.Bradycardia;
        if (rate <= 100) return BloodPulseSpeedType.Normal;
        return BloodPulseSpeedType.Tachycardia;
      }
    }
  }
}
