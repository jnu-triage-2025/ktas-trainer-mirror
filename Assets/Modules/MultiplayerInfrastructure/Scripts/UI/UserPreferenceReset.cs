using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Audio;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.TTS;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 이 기기에 저장된 사용자 설정을 한꺼번에 지우고 기본값으로 되돌립니다.
  ///
  /// 저장 항목은 각 담당 클래스가 자기 키를 지우는 방식으로 모읍니다. 저장 키를 새로 추가하면
  /// 담당 클래스에 지우기 메서드를 두고 <see cref="ClearStoredValues"/>에 등록해야 초기화에 포함됩니다.
  /// 키 목록은 <c>UserPreferenceResetTests</c>가 고정하고 있으므로 거기도 같이 고쳐야 합니다.
  ///
  /// Unity가 스스로 남기는 값(Screenmanager 해상도 등), 서드파티 모듈의 저장값, 세션 로그 파일은
  /// 다루지 않습니다. 저장소 전체를 지우는 <c>PlayerPrefs.DeleteAll</c>은 그 값들까지 지우므로 쓰지 않습니다.
  /// </summary>
  public static class UserPreferenceReset
  {
    /// <summary>
    /// <see cref="ResetAll"/>이 끝난 뒤 알립니다. 저장값을 화면에 되비추고 있는 곳(시작 화면의 이름 입력란 등)이
    /// 표시를 비우는 용도입니다.
    /// </summary>
    public static event Action ResetCompleted;

    /// <summary>
    /// 저장소에서 사용자 설정 키를 모두 지웁니다. 실행 중인 값은 건드리지 않습니다.
    /// </summary>
    /// <param name="keyBindings">저장된 키 바인딩을 지울 때 쓰는 기능 목록입니다. null이면 키 바인딩은 건너뜁니다.</param>
    public static void ClearStoredValues(IReadOnlyList<KeyBindingEntry> keyBindings)
    {
      DialogueSkipPreference.ClearStoredValue();
      TTSEnginePreference.ClearStoredValue();
      AudioVolumeSettings.ClearStoredValues();
      AudioVolumePreferenceService.ClearStoredValue();
      AudioDevicePreferenceService.ClearStoredValue();
      TexturePerformanceService.ClearStoredValue();
      UIScalePreferenceService.ClearStoredValue();
      PlayerDisplayNamePreference.Clear();
      if (keyBindings != null)
        KeyBindingRepository.DeleteAll(keyBindings);
    }

    /// <summary>
    /// 저장된 값을 모두 지우고, 지금 실행 중인 세션에도 기본값을 적용합니다.
    ///
    /// 키 바인딩은 저장값만 지웁니다. 메모리에 올라온 바인딩 목록은 설정 창이 들고 있으므로 호출자가 되돌립니다.
    /// 씬에 없는 서비스는 만들어서라도 적용하되, UI 배율 서비스만은 씬에 배치된 PanelSettings가 있어야
    /// 뜻이 있으므로 등록된 경우에만 되돌립니다.
    /// </summary>
    public static void ResetAll(IReadOnlyList<KeyBindingEntry> keyBindings)
    {
      // 저장소를 먼저 비워 둔다. 아래 적용 단계 중 하나가 실패하더라도 다음 실행은 기본값으로 시작한다.
      ClearStoredValues(keyBindings);

      DialogueSkipPreference.ResetToDefault();
      TTSEnginePreference.ResetToDefault();

      // 효과음·배경음 배율을 전체 볼륨 서비스보다 먼저 되돌린다. 둘 다 AudioListener.volume을 쓰므로
      // 마지막에 적용된 값이 전체 볼륨 서비스의 값이 되게 한다.
      AudioVolumeSettings.ResetToDefault();
      AudioVolumePreferenceService.GetOrCreateInstance().ResetToDefault();
      AudioDevicePreferenceService.GetOrCreateInstance().ResetToDefault();
      TexturePerformanceService.GetOrCreateInstance().ResetAllToDefault();

      var uiScale = Registry.Registry.Get<UIScalePreferenceService>(
        RegistryType.Service, Registry.Registry.TypeKey<UIScalePreferenceService>());
      if (uiScale != null)
        uiScale.ResetToDefault();

      ResetCompleted?.Invoke();
    }
  }
}
