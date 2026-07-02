#nullable enable
using System;

namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 환자의 의식 상태를 표현합니다.
  /// </summary>
  public class Consciousness : ICloneable, IEquatable<Consciousness>
  {
    /// <summary>
    /// GCS(Glasgow Coma Scale) 점수입니다.
    /// </summary>
    public int gcs;

    /// <summary>
    /// 동공 반사 상태입니다. PupillaryResponse에 의해 정의된 값 중 하나를 가집니다.
    /// </summary>
    public PupillaryResponse pupillaryResponse;

    /// <summary>
    /// 의식수준 5단계(LOC; Level of Consciousness) 값입니다. LOCLabel에 의해 정의된 값 중 하나를 갖습니다.
    /// </summary>
    public LOCLabel locLabel;

    /// <summary>
    /// 의식 상태 유형으로, GCS로부터 유도되어 반환됩니다.
    /// (NOTE: 별도의 이상치 검증 로직이 구현되지 않았습니다. gcs 필드가 이상치라면 GCSLabel.NA를 반환합니다.)
    /// </summary>
    public GCSLabel GcsLabel
    {
      get
      {
        if (gcs < 3) return GCSLabel.NA; // GCS 최저점은 3점(E1/V1/M1)
        if (gcs <= 8) return GCSLabel.Severe;
        if (gcs <= 12) return GCSLabel.Moderate;
        if (gcs <= 15) return GCSLabel.Mild;
        return GCSLabel.NA;
      }
    }

    /// <summary>
    /// 정상 상태일 때의 이 클래스 객체의 값이 미리 정의되어 있습니다. <br />
    /// - gcs: 15 <br />
    /// - pupillaryResponse: PupillaryResponse.Normal <br />
    /// - locLabel: LOCLabel.Alert <br />
    /// 이 속성을 호출하면 새 객체를 생성하여 반환합니다.
    /// </summary>
    public static Consciousness Default
    {
      get
      {
        return new Consciousness
        {
          gcs = 15,
          pupillaryResponse = PupillaryResponse.Normal,
          locLabel = LOCLabel.Alert,
        };
      }
    }

    public object Clone()
    {
      return new Consciousness
      {
        gcs = gcs,
        pupillaryResponse = pupillaryResponse,
        locLabel = locLabel
      };
    }

    public bool Equals(Consciousness? other)
    {
      if (other is null) return false;
      return gcs == other.gcs && pupillaryResponse == other.pupillaryResponse && locLabel == other.locLabel;
    }
  }
}
