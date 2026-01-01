namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 신체의 체온을 표현합니다.
  /// </summary>
  public class BodyTemperature
  {
    /// <summary>
    /// 실제 환자의 체온 값입니다.
    /// </summary>
    public float celsius;

    /// <summary>
    /// 이 속성은 환자의 체온 값으로부터 산출된 발열 유형입니다. BodyTemperatureType에 의해 정의된 발열 유형을 반환합니다. <br />
    /// - (-INF, 36.0) : 저체온증(BodyTemperatureType.Hypothermia) <br />
    /// - [36.0, 37.5] : 정상 범위(BodyTemperatureType.Normal) <br />
    /// - (37.5, 38.0] : 발열(BodyTemperatureType.Fever) <br />
    /// - (38.0, INF)  : 고열(BodyTemperatureType.HighFever)
    /// </summary>
    public BodyTemperatureType type
    {
      get
      {
        if (celsius < 36.0f) return BodyTemperatureType.Hypothermia;
        if (celsius <= 37.5f) return BodyTemperatureType.Normal;
        if (celsius <= 38.0f) return BodyTemperatureType.Fever;
        return BodyTemperatureType.HighFever;
      }
    }
  }
}
