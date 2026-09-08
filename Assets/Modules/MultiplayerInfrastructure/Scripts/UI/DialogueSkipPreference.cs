using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using System;
using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 대화 텍스트가 한 글자씩 재생되는 동안 클릭·스페이스바 등 진행 입력으로 재생을 건너뛰고
  /// 전체 문장을 바로 표시할 수 있게 할지 정하는 설정 항목입니다.
  ///
  /// 값은 PlayerPrefs에 남으므로 게임을 다시 켜도 유지됩니다. 기본값은 허용(true)이며,
  /// 끄면 <see cref="DialoguePanelUIController"/>가 재생 중의 진행 입력을 소비만 하고
  /// 재생을 끝까지 이어 갑니다. 재생이 끝난 뒤의 진행·선택 입력은 설정과 무관하게 동작하므로
  /// 시나리오 흐름 자체는 영향을 받지 않습니다.
  ///
  /// 이 클래스는 사용자 입력에 의한 건너뛰기만 다룹니다. 컨트롤러가 내부적으로 재생을 끝내는
  /// 경로(<see cref="DialoguePanelUIController.SkipTyping"/> 직접 호출 등)는 이 설정을 보지 않습니다.
  /// </summary>
  public static class DialogueSkipPreference
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.DialogueSkipEnabled";

    /// <summary>저장된 값이 없을 때의 기본값입니다. 기존 동작(건너뛰기 허용)을 유지합니다.</summary>
    public const bool DefaultEnabled = true;

    /// <summary>true면 재생 중 진행 입력으로 대화 연출을 건너뛸 수 있습니다.</summary>
    public static bool IsEnabled { get; private set; } = DefaultEnabled;

    /// <summary>설정이 바뀔 때 새 값을 알립니다. 값이 그대로면 발생하지 않습니다.</summary>
    public static event Action<bool> EnabledChanged;

    /// <summary>설정 값을 즉시 적용하고 저장합니다.</summary>
    public static void SetEnabled(bool enabled)
    {
      bool changed = IsEnabled != enabled;
      IsEnabled = enabled;

      PlayerPrefs.SetInt(PlayerPrefsKey, enabled ? 1 : 0);
      PlayerPrefs.Save();

      if (changed)
        EnabledChanged?.Invoke(enabled);
    }

    /// <summary>
    /// 저장된 값을 읽어 적용합니다.
    ///
    /// 대화창이 첫 입력을 받기 전에 끝나야 하므로 씬을 불러오기 전에 실행합니다.
    /// 값이 손상되어 있거나 없으면 <see cref="DefaultEnabled"/>를 씁니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void LoadAndApply()
    {
      IsEnabled = ReadStoredValue();
    }

    /// <summary>PlayerPrefs에 저장된 값을 읽습니다. 저장된 값이 없으면 기본값을 돌려줍니다.</summary>
    public static bool ReadStoredValue()
    {
      return PlayerPrefs.GetInt(PlayerPrefsKey, DefaultEnabled ? 1 : 0) != 0;
    }

    /// <summary>저장된 값을 지웁니다. 다음 <see cref="LoadAndApply"/>부터 기본값이 쓰입니다.</summary>
    public static void ClearStoredValue()
    {
      PlayerPrefs.DeleteKey(PlayerPrefsKey);
      PlayerPrefs.Save();
    }
  }
}
