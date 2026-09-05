using MultiplayerInfrastructure.Performance;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Performance.Tests
{
  /// <summary>씬 전환 뒤에도 사용자가 정한 화면 모드와 창 크기가 유지되는지에 대한 결정 규칙을 검증합니다.</summary>
  public class DisplaySettingsPersistenceTests
  {
    [Test]
    public void DisplayIsAppliedOnlyOnceInASession()
    {
      Assert.That(TexturePerformanceService.ShouldApplyDisplayOnLoad(false, false), Is.True);
      Assert.That(TexturePerformanceService.ShouldApplyDisplayOnLoad(false, true), Is.False,
        "씬 전환으로 다시 깨어난 서비스는 디스플레이 설정을 되돌리지 않아야 합니다.");
    }

    [Test]
    public void DisplayIsNeverAppliedInMppmLite()
    {
      Assert.That(TexturePerformanceService.ShouldApplyDisplayOnLoad(true, false), Is.False);
      Assert.That(TexturePerformanceService.ShouldApplyDisplayOnLoad(true, true), Is.False);
    }

    private static GraphicsSettingsData Windowed(int width, int height)
    {
      var settings = GraphicsQualityPresets.Create(GraphicsQualityProfile.Medium);
      settings.WindowMode = DisplayWindowMode.Windowed;
      settings.ResolutionWidth = width;
      settings.ResolutionHeight = height;
      settings.Sanitize();
      return settings;
    }

    [Test]
    public void ResizedWindow_IsPersistedWhenWindowed()
    {
      var settings = Windowed(1920, 1080);

      Assert.That(TexturePerformanceService.ShouldPersistWindowSize(settings, FullScreenMode.Windowed, 1280, 720),
        Is.True);
    }

    [Test]
    public void UnchangedWindow_IsNotPersisted()
    {
      var settings = Windowed(1920, 1080);

      Assert.That(TexturePerformanceService.ShouldPersistWindowSize(settings, FullScreenMode.Windowed, 1920, 1080),
        Is.False);
    }

    [TestCase(DisplayWindowMode.FullScreen, FullScreenMode.ExclusiveFullScreen)]
    [TestCase(DisplayWindowMode.FullScreen, FullScreenMode.FullScreenWindow)]
    [TestCase(DisplayWindowMode.BorderlessWindow, FullScreenMode.FullScreenWindow)]
    public void FullScreenSizes_AreNotPersistedAsWindowSize(DisplayWindowMode stored, FullScreenMode actual)
    {
      var settings = Windowed(1920, 1080);
      settings.WindowMode = stored;

      Assert.That(TexturePerformanceService.ShouldPersistWindowSize(settings, actual, 2560, 1440), Is.False);
    }

    [Test]
    public void WindowedSettingWithFullScreenRuntime_IsNotPersisted()
    {
      // 저장은 창 모드인데 실제 화면이 아직 전체 화면이면(전환 도중) 그 크기를 창 크기로 저장하지 않는다.
      var settings = Windowed(1920, 1080);

      Assert.That(TexturePerformanceService.ShouldPersistWindowSize(settings, FullScreenMode.FullScreenWindow, 2560, 1440),
        Is.False);
    }

    [TestCase(639, 720)]
    [TestCase(1280, 359)]
    [TestCase(0, 0)]
    public void OutOfRangeSizes_AreNotPersisted(int width, int height)
    {
      var settings = Windowed(1920, 1080);

      Assert.That(TexturePerformanceService.ShouldPersistWindowSize(settings, FullScreenMode.Windowed, width, height),
        Is.False);
    }

    [Test]
    public void NullSettings_AreNotPersisted()
    {
      Assert.That(TexturePerformanceService.ShouldPersistWindowSize(null, FullScreenMode.Windowed, 1280, 720), Is.False);
    }
  }
}
