namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioParallelAllocationType
  {
    SelfAll,
    RandomOneAll,
    SpreadRandom,
    SpreadOrdinary,

    /// <summary>
    /// 각 브랜치를 자격(requiredPlayerTags/forbiddenPlayerTags)에 맞는 <b>서로 다른</b> 플레이어에게
    /// 1:1로 분배합니다. 한 플레이어는 최대 하나의 브랜치에만 배정됩니다.
    /// 다인 협력 처치(예: 간호사 B=활력, C=GCS, D=석션 동시 수행)를 표현하기 위한 모드입니다.
    /// 자격 후보가 적은 브랜치부터 그리디로 배정하여 결정적(deterministic) 매칭을 보장합니다.
    /// </summary>
    ByRole
  }
}
