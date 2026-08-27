namespace TriageTrainer.ItemDefinitions
{
  /// <summary>
  /// 조립된 양커 팁(조합 완료). 시나리오 구식 산출물명 `yankauer_ready` 에 대응한다.
  /// 석션 라인 + 양커 석션 팁을 조합하여 만든다.
  /// 아이템 리소스(아이콘/모델)는 `yankauer` 를 복사해 사용한다.
  /// </summary>
  public class YankauerSuctionReady : MedicalItem
  {
    public new const string Identifier = "yankauer_suction_ready";
    public new const string DisplayName = "조립된 양커 팁";
    public new const string Description = "석션 라인에 양커 팁이 연결되어 흡인 준비가 완료된 상태입니다.";
  }
}
