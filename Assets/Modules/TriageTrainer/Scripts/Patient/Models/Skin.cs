using System;

namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 피부 표면에 관한 정보를 표현합니다.
  /// </summary>
  public class Skin : ICloneable, IEquatable<Skin>
  {
    /// <summary>
    /// 육안으로 확인 가능한 피부 색조 정보를 표현합니다. SkinColorHue에 의해 정의된 "창백함(SkinColorHue.Pale)" 유형, 혹은 "정상(SkinColorHue.Normal)" 유형 둘 중 하나를 갖습니다.
    /// </summary>
    public SkinColorHue colorHue;

    /// <summary>
    /// 피부 표면의 온도 수준을 표현합니다.
    /// </summary>
    /// <remarks>
    /// 이 값은 단순화되었습니다:<br />
    /// 이 값은 체온 데이터와는 다르게 단순화되었습니다. 피부 표면의 온도는 혈액 순환 상황을 확인하기 위해 사용하므로 구체적인 수치로 정의되지 않습니다.
    /// SkinTemperatureType에 의해 정의된 유형 중 하나를 가집니다.
    /// </remarks>
    public SkinTemperatureType temperatureType;

    /// <summary>
    /// 정상 상태일 때의 이 클래스 객체의 값이 미리 정의되어 있습니다.
    /// colorHue: SkinColorHue.Normal <br />
    /// temperatureType: SkinTemperatureType.Normal <br />
    /// 이 속성을 호출하면 새 객체를 생성하여 반환합니다.
    /// </summary>
    static public Skin Default
    {
      get
      {
        return new Skin
        {
          colorHue = SkinColorHue.Normal,
          temperatureType = SkinTemperatureType.Normal,
        };
      }
    }

    public object Clone()
    {
      return new Skin
      {
        colorHue = colorHue,
        temperatureType = temperatureType
      };
    }

    public bool Equals(Skin? other)
    {
      if (other is null) return false;
      return colorHue == other.colorHue && temperatureType == other.temperatureType;
    }
  }
}
