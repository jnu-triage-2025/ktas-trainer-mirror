using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>
  /// 사용자에게 노출되는 화면 모드입니다.
  ///
  /// Unity의 <see cref="FullScreenMode"/>는 플랫폼마다 지원 범위가 다릅니다.
  /// (ExclusiveFullScreen은 Windows 전용, MaximizedWindow는 macOS 전용이며 다른 플랫폼에서는
  /// 조용히 FullScreenWindow로 대체됩니다.) 그래서 설정에는 사용자의 의도를 이 열거형으로 저장하고,
  /// 적용 시점에 <see cref="DisplayWindowModes.ResolveFullScreenMode"/>로 플랫폼에 맞는 값으로 변환합니다.
  /// 정수 값이 PlayerPrefs JSON에 그대로 저장되므로 값을 바꾸지 않습니다.
  /// </summary>
  public enum DisplayWindowMode
  {
    /// <summary>
    /// 이 항목이 없던 구버전 저장값입니다. <see cref="GraphicsSettingsData.Sanitize"/>가
    /// 구버전 <see cref="FullScreenMode"/> 값으로부터 실제 모드를 복원합니다.
    /// </summary>
    Unspecified = 0,

    /// <summary>테두리가 있는 일반 창입니다.</summary>
    Windowed = 1,

    /// <summary>테두리 없이 화면을 가득 채우는 창입니다. 다른 창으로 전환이 빠릅니다.</summary>
    BorderlessWindow = 2,

    /// <summary>전체 화면입니다. Windows에서는 전용(exclusive) 전체 화면을 사용합니다.</summary>
    FullScreen = 3,
  }

  /// <summary><see cref="DisplayWindowMode"/>의 기본값, 플랫폼 변환, 표시 문자열을 제공합니다.</summary>
  public static class DisplayWindowModes
  {
    /// <summary>저장값이 없을 때와 프리셋을 새로 만들 때 사용하는 기본 화면 모드입니다.</summary>
    public const DisplayWindowMode Default = DisplayWindowMode.FullScreen;

    /// <summary>설정 UI에 노출하는 순서입니다.</summary>
    public static readonly DisplayWindowMode[] Selectable =
    {
      DisplayWindowMode.Windowed,
      DisplayWindowMode.BorderlessWindow,
      DisplayWindowMode.FullScreen,
    };

    /// <summary>Unity가 전용 전체 화면(ExclusiveFullScreen)을 실제로 지원하는 플랫폼인지 확인합니다.</summary>
    public static bool SupportsExclusiveFullScreen(RuntimePlatform platform)
      => platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor;

    /// <summary>저장된 의도를 해당 플랫폼에서 실제로 적용할 <see cref="FullScreenMode"/>로 변환합니다.</summary>
    public static FullScreenMode ResolveFullScreenMode(DisplayWindowMode mode, RuntimePlatform platform)
    {
      switch (mode)
      {
        case DisplayWindowMode.Windowed:
          return FullScreenMode.Windowed;
        case DisplayWindowMode.BorderlessWindow:
          return FullScreenMode.FullScreenWindow;
        case DisplayWindowMode.FullScreen:
          return SupportsExclusiveFullScreen(platform)
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.FullScreenWindow;
        default:
          return ResolveFullScreenMode(Default, platform);
      }
    }

    /// <summary>
    /// <see cref="DisplayWindowMode"/>가 저장되기 전의 설정에서 <see cref="FullScreenMode"/> 값을 화면 모드로 복원합니다.
    /// 구버전 기본값이던 FullScreenWindow는 새 기본값과 같은 전체 화면으로 취급합니다.
    /// </summary>
    public static DisplayWindowMode FromLegacyFullScreenMode(FullScreenMode legacy)
    {
      switch (legacy)
      {
        case FullScreenMode.Windowed:
          return DisplayWindowMode.Windowed;
        case FullScreenMode.MaximizedWindow:
          return DisplayWindowMode.BorderlessWindow;
        default:
          return DisplayWindowMode.FullScreen;
      }
    }

    /// <summary>범위를 벗어난 값이나 <see cref="DisplayWindowMode.Unspecified"/>를 유효한 모드로 정리합니다.</summary>
    public static DisplayWindowMode Sanitize(DisplayWindowMode mode, FullScreenMode legacy)
      => IsSelectable(mode) ? mode : FromLegacyFullScreenMode(legacy);

    public static bool IsSelectable(DisplayWindowMode mode)
      => Array.IndexOf(Selectable, mode) >= 0;

    public static string Label(DisplayWindowMode mode)
    {
      switch (mode)
      {
        case DisplayWindowMode.Windowed:
          return "창 모드";
        case DisplayWindowMode.BorderlessWindow:
          return "테두리 없는 창 모드";
        case DisplayWindowMode.FullScreen:
          return "전체 화면";
        default:
          return Label(Default);
      }
    }

    public static DisplayWindowMode FromLabel(string label)
    {
      foreach (var mode in Selectable)
      {
        if (Label(mode) == label)
          return mode;
      }
      return Default;
    }

    /// <summary>
    /// 전용 전체 화면이 없는 플랫폼에서 사용자에게 보여 줄 안내입니다.
    /// 지원 플랫폼이면 null을 돌려줍니다.
    /// </summary>
    public static string PlatformNote(RuntimePlatform platform)
      => SupportsExclusiveFullScreen(platform)
        ? null
        : "이 플랫폼은 전용 전체 화면을 지원하지 않아 전체 화면과 테두리 없는 창 모드가 같은 방식으로 표시됩니다.";
  }
}
