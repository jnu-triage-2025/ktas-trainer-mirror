namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 준비된 산소 유량계(조합 완료). 시나리오 구식 산출물명 `oxyflowmeter` 에 대응한다.
  /// 유량계(flowmeter) + 멸균증류수(sterile_distilled_water)를 조합하여 만든다.
  /// 아이템 리소스(아이콘/모델)는 `oxyflowmeter` 전용 에셋을 사용한다.
  /// </summary>
  public class Oxyflowmeter : MedicalItem
  {
    public const string Identifier   = "oxyflowmeter";
    public const string DisplayName  = "준비된 산소 유량계";
    public const string Description  = "벽면에 장착할 준비가 완료된 산소 유량계입니다.";
  }
}
