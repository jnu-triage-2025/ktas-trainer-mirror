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
    /// (NOTE: 이 값은 <see cref="eyeOpening"/>, <see cref="verbal"/>, <see cref="motor"/> 세 필드의 합과
    /// 별개로 저장되는 필드입니다. E/V/M 세부 항목을 사정한 경우 <see cref="GcsFromComponents"/> 를 통해
    /// 세부 항목 합계를 확인할 수 있으며, 필요 시 이 필드와 일치하도록 별도로 동기화해야 합니다.)
    /// </summary>
    public int gcs;

    /// <summary>
    /// GCS의 E(Eye Opening, 눈뜨기 반응) 세부 항목입니다. EyeOpeningResponse에 의해 정의된 값 중 하나를 가집니다.
    /// </summary>
    public EyeOpeningResponse eyeOpening;

    /// <summary>
    /// GCS의 V(Verbal Response, 언어 반응) 세부 항목입니다. VerbalResponse에 의해 정의된 값 중 하나를 가집니다.
    /// </summary>
    public VerbalResponse verbal;

    /// <summary>
    /// GCS의 M(Motor Response, 운동 반응) 세부 항목입니다. MotorResponse에 의해 정의된 값 중 하나를 가집니다.
    /// </summary>
    public MotorResponse motor;

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
    /// E(<see cref="eyeOpening"/>) + V(<see cref="verbal"/>) + M(<see cref="motor"/>) 세부 항목 점수의 합입니다.
    /// GCS 세부 사정(E/V/M)을 완료한 경우 이 값이 <see cref="gcs"/> 필드와 일치해야 합니다.
    /// </summary>
    public int GcsFromComponents => (int)eyeOpening + (int)verbal + (int)motor;

    /// <summary>
    /// 정상 상태일 때의 이 클래스 객체의 값이 미리 정의되어 있습니다. <br />
    /// - gcs: 15 <br />
    /// - eyeOpening: EyeOpeningResponse.Spontaneous(4점) <br />
    /// - verbal: VerbalResponse.Oriented(5점) <br />
    /// - motor: MotorResponse.ObeysCommands(6점) <br />
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
          eyeOpening = EyeOpeningResponse.Spontaneous,
          verbal = VerbalResponse.Oriented,
          motor = MotorResponse.ObeysCommands,
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
        eyeOpening = eyeOpening,
        verbal = verbal,
        motor = motor,
        pupillaryResponse = pupillaryResponse,
        locLabel = locLabel
      };
    }

    public bool Equals(Consciousness? other)
    {
      if (other is null) return false;
      return gcs == other.gcs
        && eyeOpening == other.eyeOpening
        && verbal == other.verbal
        && motor == other.motor
        && pupillaryResponse == other.pupillaryResponse
        && locLabel == other.locLabel;
    }
  }
}
