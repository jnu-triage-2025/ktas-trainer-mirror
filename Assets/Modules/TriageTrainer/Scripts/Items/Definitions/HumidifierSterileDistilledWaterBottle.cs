namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 멸균증류수가 담긴 습윤병(조합 완료).
  /// 습윤병(humidifier_bottle) + 멸균증류수(sterile_distilled_water)를 조합하여 만든다.
  /// 아이템 리소스(아이콘/모델)는 humidifier_bottle 에셋을 재사용한다.
  /// </summary>
  public class HumidifierSterileDistilledWaterBottle : MedicalItem
  {
    public const string Identifier   = "humidifier_sterile_distilled_water_bottle";
    public const string DisplayName  = "멸균증류수가 담긴 습윤병";
    public const string Description  = "멸균증류수가 채워진 습윤병입니다. 유량계와 결합하여 산소 유량계를 완성합니다.";
  }
}
