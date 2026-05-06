using System.Collections.Generic;

namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// 환자 상태 기술자
  /// 이 클래스는 환자의 생체 상태를 정의합니다. 환자 엔티티 생성에 필요한 데이터, 환자 모니터에 제공하는 데이터,
  /// 환자 소생에 필요한 조치에 관한 데이터 일체를 포함합니다.
  /// 이후 구현 상황에 따라서 사후 평가 과정에서도 사용할 수 있도록 확장할 수 있습니다.
  /// </summary>
  public class PatientDescriptor
  {
    /// <summary>
    /// 환자 식별에 사용되는 식별자입니다. (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여
    /// 이 식별자가 자동으로 부여되도록 설계해야합니다.)
    /// </summary>
    public string identifier;

    /// <summary>
    /// 환자의 성명입니다. (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여
    /// 이 이름이 자동으로 부여되도록 설계해야합니다.)
    /// </summary>
    public string name;

    /// <summary>
    /// 환자의 성별입니다. Sex 열거형에 선언된 두 개 값(Male, Female) 중 하나를 가집니다.
    /// (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여 이 성별이 자동으로 부여되도록 설계해야합니다.)
    /// </summary>    
    public Sex sex;

    /// <summary>
    /// 환자의 나이입니다. (구현되지 않음: 추후 구현될 환자 자동 생성기에 의하여 이 나이가 자동으로 부여되도록 설계해야합니다.)
    /// </summary>
    public int age;

    /// <summary>
    /// 환자의 혈액형입니다. BloodType 열거형에 선언된 값 중 하나를 가집니다.
    /// </summary>
    public BloodType bloodType;

    /// <summary>
    /// 환자의 혈압입니다. BloodPressure 클래스에 정의된 두 개의 값(sBP; 수축기, dBP; 이완기)을 가집니다.
    /// </summary>
    public BloodPressure bloodPressure;

    /// <summary>
    /// 환자의 맥박 정보입니다. BloodPulse 클래스에 의해 정의된 분당 맥박 수와 맥박 세기 데이터를 가집니다.
    /// </summary>
    public BloodPulse pulse;

    /// <summary>
    /// 환자의 피부 상태입니다. Skin 클래스에 의해 정의된 피부 색조와 피부 표면의 온도 유형 데이터를 가집니다.
    /// 주의할 문제가 없다면 이 값은 Skin.Default 값을 가집니다.
    /// </summary>
    public Skin skin;

    /// <summary>
    /// 환자의 체온입니다. BodyTemperature 클래스에 의해 정의된 섭씨 온도 데이터를 가집니다.
    /// </summary>
    public BodyTemperature bodyTemperature;

    /// <summary>
    /// 환자에게 발생한 건강 문제 목록입니다. HealthProblem 클래스에 의해 정의되며,
    /// 게임 내부에서는 각 문제에 대하여 적절하게 조치가 이루어져야 합니다.
    /// </summary>
    public List<HealthProblem> healthProblem;

    /// <summary>
    /// 환자의 의식 상태입니다. Consciousness 클래스에 의해 정의된 GCS 점수, LOC 레이블, 동공 반응 데이터를 가집니다.
    /// 주의할 문제가 없다면 이 값은 Consciousness.Default 값을 가집니다.
    /// </summary>
    public Consciousness consciousness;

    /// <summary>
    /// 환자의 호흡 정보입니다. Respiration 클래스에 의해 정의된 분당 호흡 수와 호흡의 질을 가집니다.
    /// </summary>
    public Respiration respiration;

    /// <summary>
    /// 정맥주사를 통해 투여가 요구되는 약물 목록입니다. 아무것도 투여하지 않아도 된다면 이 리스트는 비어있습니다.
    /// </summary>
    public List<RequiredDrug> requiredDrugs;

    /// <summary>
    /// 환자가 심정지 상태인지 여부입니다. 심정지 상태라면 true, 아니면 false입니다.
    /// 심정지 상태라면 인게임에서 심폐소생술(CPR)이 요구됩니다.
    /// </summary>
    public bool isCardiacArrest;

    /// <summary>
    /// 환자 모니터 표시용 파라미터 묶음입니다.
    /// PatientMonitor와 동일한 구조(ECG/ART/CVP/PLETH/Numerics/NIBP/Temperature/ST)를 사용합니다.
    /// </summary>
    public PatientMonitorParameters monitorParameters = PatientMonitorParameters.Default;
  }
}
