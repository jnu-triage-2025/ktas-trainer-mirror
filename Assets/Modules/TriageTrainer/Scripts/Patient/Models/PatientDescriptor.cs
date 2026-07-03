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
    /// 의도된 트리아지 등급(정답)입니다. 시나리오 설계자가 이 환자에 대해 기대하는 KTAS 등급을 지정합니다.
    /// 트리아지 평가 시 <see cref="assessedTriage"/> 와 비교해 정답 여부를 판정하는 데 사용됩니다.
    /// </summary>
    public TriageLevel intendedTriage = TriageLevel.Unassessed;

    /// <summary>
    /// 플레이어가 실제로 평가한 트리아지 등급(현재 상태값)입니다.
    /// 아직 평가되지 않았으면 <see cref="TriageLevel.Unassessed"/> 입니다.
    /// </summary>
    public TriageLevel assessedTriage = TriageLevel.Unassessed;

  }
}
