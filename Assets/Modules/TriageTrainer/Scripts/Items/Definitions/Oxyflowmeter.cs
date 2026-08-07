namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 멸균증류수가 담긴 습윤병이 연결된 산소 유량계(조합 완료).
  /// 유량계(flowmeter) + 멸균증류수가 담긴 습윤병(humidifier_sterile_distilled_water_bottle)을 조합하여 만든다.
  /// 아이템 리소스(아이콘/모델)는 `oxyflowmeter` 전용 에셋을 사용한다.
  /// </summary>
  public class Oxyflowmeter : MedicalItem
  {
    public new const string Identifier   = "oxyflowmeter";
    public new const string DisplayName  = "멸균증류수가 담긴 습윤병이 연결된 산소 유량계";
    public new const string Description  = "멸균증류수가 담긴 습윤병이 연결되어 벽면에 장착할 준비가 완료된 산소 유량계입니다.";
  }
}
