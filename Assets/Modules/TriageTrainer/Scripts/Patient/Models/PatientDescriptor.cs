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

  }
}
