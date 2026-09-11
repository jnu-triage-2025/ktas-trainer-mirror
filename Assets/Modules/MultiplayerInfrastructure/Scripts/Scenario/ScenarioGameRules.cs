using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  [Flags]
  public enum CareZoneMissingEquipmentFallback
  {
    None = 0,
    WallSuction = 1 << 0,
    Oxyflowmeter = 1 << 1,
    Defibrillator = 1 << 2,
  }

  /// <summary>시나리오 실행에 적용되는 서버 게임 규칙.</summary>
  public static class ScenarioGameRules
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeDefaults()
    {
      DEBUG_INT_CPR_PLAYING_ESCAPE_KEY = false;
      MissingCareZoneEquipmentFallback = CareZoneMissingEquipmentFallback.Defibrillator;
      ShowRecognitionMicrophoneUnavailableGuidance = false;
    }

    /// <summary>
    /// true이면 Parallel/PlayerAssignedTag 게이트에서 요구 태그가 충족되지 않아도 흐름을 계속 진행한다.
    /// 기본값은 데모/단독 진행을 위해 true이며, false이면 그래프에 정의된 원래 태그 게이트를 엄격히 적용한다.
    /// </summary>
    public static bool IgnoreTagAssignFullSatisfactionOnScenarioPlay { get; set; } = true;

    /// <summary>
    /// true이면 한 플레이어에게 여러 ByRole 브랜치가 배정된 경우 해당 브랜치들을 순차 실행하도록 허용한다.
    /// </summary>
    public static bool AllowMultipleRoleBranchesForSinglePlayer { get; set; } = true;

    /// <summary>
    /// true이면 로컬 플레이어가 CPR 수행 애니메이션과 위치 고정 상태를 Left Shift로 임시 해제할 수 있다.
    /// 이 값은 디버깅 편의를 위한 표현 규칙일 뿐이며, 시나리오의 CPR 진행·완료 상태는 변경하지 않는다.
    /// </summary>
    public static bool DEBUG_INT_CPR_PLAYING_ESCAPE_KEY { get; set; }

    /// <summary>의식 확인 상호작용에서 로컬 마이크 음량 입력을 허용한다.</summary>
    public static bool UseMicInRecognitionCheck { get; private set; }

    /// <summary>의식 확인의 직접 상호작용 경로를 숨긴다. 마이크 경로가 켜진 경우에만 허용된다.</summary>
    public static bool DisableInteractionInRecognitionCheck { get; private set; }

    /// <summary>의식 확인에서 마이크를 사용할 수 없을 때의 안내 문구를 표시한다.</summary>
    public static bool ShowRecognitionMicrophoneUnavailableGuidance { get; set; }

    public static CareZoneMissingEquipmentFallback MissingCareZoneEquipmentFallback { get; private set; }
      = CareZoneMissingEquipmentFallback.Defibrillator;

    public static bool AllowsMissingCareZoneEquipmentFallback(CareZoneMissingEquipmentFallback equipment)
      => (MissingCareZoneEquipmentFallback & equipment) == equipment;

    public static bool TrySetMissingCareZoneEquipmentFallback(string value, out string error)
    {
      if (value == null)
      {
        error = "CareZoneMissingEquipmentFallback must be a comma-separated list of wall_suction, oxyflowmeter, defibrillator, or none.";
        return false;
      }

      CareZoneMissingEquipmentFallback parsed = CareZoneMissingEquipmentFallback.None;
      string[] values = value.Split(',');
      for (int i = 0; i < values.Length; i++)
      {
        string entry = values[i].Trim();
        if (string.Equals(entry, "none", StringComparison.OrdinalIgnoreCase))
        {
          if (values.Length != 1)
          {
            error = "CareZoneMissingEquipmentFallback cannot combine none with equipment values.";
            return false;
          }
          continue;
        }

        if (string.Equals(entry, "wall_suction", StringComparison.OrdinalIgnoreCase))
          parsed |= CareZoneMissingEquipmentFallback.WallSuction;
        else if (string.Equals(entry, "oxyflowmeter", StringComparison.OrdinalIgnoreCase))
          parsed |= CareZoneMissingEquipmentFallback.Oxyflowmeter;
        else if (string.Equals(entry, "defibrillator", StringComparison.OrdinalIgnoreCase))
          parsed |= CareZoneMissingEquipmentFallback.Defibrillator;
        else
        {
          error = "CareZoneMissingEquipmentFallback must be a comma-separated list of wall_suction, oxyflowmeter, defibrillator, or none.";
          return false;
        }
      }

      MissingCareZoneEquipmentFallback = parsed;
      error = null;
      return true;
    }

    public static string FormatMissingCareZoneEquipmentFallback()
    {
      if (MissingCareZoneEquipmentFallback == CareZoneMissingEquipmentFallback.None)
        return "none";

      string[] values = new string[3];
      int count = 0;
      if (AllowsMissingCareZoneEquipmentFallback(CareZoneMissingEquipmentFallback.WallSuction))
        values[count++] = "wall_suction";
      if (AllowsMissingCareZoneEquipmentFallback(CareZoneMissingEquipmentFallback.Oxyflowmeter))
        values[count++] = "oxyflowmeter";
      if (AllowsMissingCareZoneEquipmentFallback(CareZoneMissingEquipmentFallback.Defibrillator))
        values[count++] = "defibrillator";
      return string.Join(",", values, 0, count);
    }

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
