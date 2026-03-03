namespace TriageTrainer.ItemDefinitions
{
  public class Electrode : MedicalItem
  {
    public override string Identifier   => "electrode";
    public override string DisplayName  => "전극";
    public override string Description  => "심전도를 읽기 위해 환자에게 부착할 전극입니다.";
  }
}
