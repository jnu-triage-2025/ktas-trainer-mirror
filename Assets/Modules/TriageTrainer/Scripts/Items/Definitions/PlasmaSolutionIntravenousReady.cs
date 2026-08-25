namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 혈장 수액 세트(조합 완료). 시나리오 구식 산출물명 `ps1_ready` 에 대응한다.
  /// 플라즈마 솔루션 1L + 수액세트를 조합하여 만든다.
  /// 아이템 리소스(아이콘/모델)는 `intravenous_set` 를 복사해 사용한다.
  /// </summary>
  public class PlasmaSolutionIntravenousReady : MedicalItem
  {
    public new const string Identifier = "plasma_solution_intravenous_ready";
    public new const string DisplayName = "준비된 플라즈마 솔루션 1L 수액백";
    public new const string Description = "준비된 플라즈마 솔루션 1L 수액백";
  }
}
