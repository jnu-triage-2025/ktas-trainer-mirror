namespace TriageTrainer.ItemDefinitions
{
  public class VitalSet : MedicalItem
  {
    public override string Identifier   => "vital_set";
    public override string DisplayName  => "활력징후 측정도구";
    public override string Description  => "활력징후를 측정하기 위한 혈압계, 청진기, 체온계입니다.";
  }
}
