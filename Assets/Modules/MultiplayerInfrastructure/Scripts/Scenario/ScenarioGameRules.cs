namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>시나리오 실행에 적용되는 서버 게임 규칙.</summary>
  public static class ScenarioGameRules
  {
    /// <summary>
    /// true이면 Parallel/PlayerAssignedTag 게이트에서 요구 태그가 충족되지 않아도 흐름을 계속 진행한다.
    /// 기본값은 데모/단독 진행을 위해 true이며, false이면 그래프에 정의된 원래 태그 게이트를 엄격히 적용한다.
    /// </summary>
    public static bool IgnoreTagAssignFullSatisfactionOnScenarioPlay { get; set; } = true;

    /// <summary>
    /// true이면 한 플레이어에게 여러 ByRole 브랜치가 배정된 경우 해당 브랜치들을 순차 실행하도록 허용한다.
    /// </summary>
    public static bool AllowMultipleRoleBranchesForSinglePlayer { get; set; } = true;

    /// <summary>의식 확인 상호작용에서 로컬 마이크 음량 입력을 허용한다.</summary>
    public static bool UseMicInRecognitionCheck { get; private set; }

    /// <summary>의식 확인의 직접 상호작용 경로를 숨긴다. 마이크 경로가 켜진 경우에만 허용된다.</summary>
    public static bool DisableInteractionInRecognitionCheck { get; private set; }

    public static bool TrySetUseMicInRecognitionCheck(bool value, out string error)
    {
      if (!value && DisableInteractionInRecognitionCheck)
      {
        error = "UseMicInRecognitionCheck cannot be false while DisableInteractionInRecognitionCheck is true.";
        return false;
      }

      UseMicInRecognitionCheck = value;
      error = null;
      return true;
    }

    public static bool TrySetDisableInteractionInRecognitionCheck(bool value, out string error)
    {
      if (value && !UseMicInRecognitionCheck)
      {
        error = "DisableInteractionInRecognitionCheck cannot be true while UseMicInRecognitionCheck is false.";
        return false;
      }

      DisableInteractionInRecognitionCheck = value;
      error = null;
      return true;
    }
  }
}
