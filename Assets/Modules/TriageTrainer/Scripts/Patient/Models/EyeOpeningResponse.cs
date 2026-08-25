namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// GCS(Glasgow Coma Scale)의 E(Eye Opening, 눈뜨기 반응) 세부 항목을 표현합니다.
  /// 값은 해당 GCS 점수(1~4점)와 동일하게 매핑되어 있습니다.
  /// </summary>
  public enum EyeOpeningResponse
  {
    /// <summary>
    /// 1점(None): 어떤 자극(통증 포함)을 주어도 눈을 뜨지 않는 상태.
    /// </summary>
    None = 1,

    /// <summary>
    /// 2점(To pressure/pain): 말로 해서는 안 되고, 손톱 끝을 누르는 등의 통증 자극을 주어야 눈을 뜨는 상태.
    /// </summary>
    ToPressure = 2,

    /// <summary>
    /// 3점(To sound/speech): 이름을 부르거나 말을 걸면 눈을 뜨는 상태.
    /// </summary>
    ToSound = 3,

    /// <summary>
    /// 4점(Spontaneous): 자극이 없어도 스스로 눈을 뜨고 있는 상태.
    /// </summary>
    Spontaneous = 4,
  }
}
