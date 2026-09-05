using MultiplayerInfrastructure.Performance;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Performance.Tests
{
  public class DisplayWindowModeTests
  {
    [Test]
    public void Default_IsFullScreen()
    {
      Assert.That(DisplayWindowModes.Default, Is.EqualTo(DisplayWindowMode.FullScreen));
      Assert.That(GraphicsQualityPresets.Create(GraphicsQualityPresets.RuntimeDefaultProfile).WindowMode,
        Is.EqualTo(DisplayWindowMode.FullScreen));
    }

    [Test]
    public void FreshSettings_SanitizeToFullScreen()
    {
      var settings = new GraphicsSettingsData();

      settings.Sanitize();

      Assert.That(settings.WindowMode, Is.EqualTo(DisplayWindowMode.FullScreen));
    }

    [TestCase(RuntimePlatform.WindowsPlayer)]
    [TestCase(RuntimePlatform.WindowsEditor)]
    public void FullScreen_UsesExclusiveModeOnWindows(RuntimePlatform platform)
    {
      Assert.That(DisplayWindowModes.SupportsExclusiveFullScreen(platform), Is.True);
      Assert.That(DisplayWindowModes.ResolveFullScreenMode(DisplayWindowMode.FullScreen, platform),
        Is.EqualTo(FullScreenMode.ExclusiveFullScreen));
    }

    [TestCase(RuntimePlatform.OSXPlayer)]
    [TestCase(RuntimePlatform.OSXEditor)]
    [TestCase(RuntimePlatform.LinuxPlayer)]
    [TestCase(RuntimePlatform.LinuxEditor)]
    public void FullScreen_FallsBackToFullScreenWindowElsewhere(RuntimePlatform platform)
    {
      Assert.That(DisplayWindowModes.SupportsExclusiveFullScreen(platform), Is.False);
      Assert.That(DisplayWindowModes.ResolveFullScreenMode(DisplayWindowMode.FullScreen, platform),
        Is.EqualTo(FullScreenMode.FullScreenWindow));
      Assert.That(DisplayWindowModes.PlatformNote(platform), Is.Not.Null.And.Not.Empty);
    }

    [TestCase(RuntimePlatform.WindowsPlayer)]
    [TestCase(RuntimePlatform.OSXPlayer)]
    [TestCase(RuntimePlatform.LinuxPlayer)]
    public void WindowedAndBorderless_ResolveTheSameOnEveryPlatform(RuntimePlatform platform)
    {
      Assert.That(DisplayWindowModes.ResolveFullScreenMode(DisplayWindowMode.Windowed, platform),
        Is.EqualTo(FullScreenMode.Windowed));
      Assert.That(DisplayWindowModes.ResolveFullScreenMode(DisplayWindowMode.BorderlessWindow, platform),
        Is.EqualTo(FullScreenMode.FullScreenWindow));
    }

    [Test]
    public void Unspecified_ResolvesLikeTheDefault()
    {
      foreach (RuntimePlatform platform in new[] { RuntimePlatform.WindowsPlayer, RuntimePlatform.OSXPlayer })
      {
        Assert.That(DisplayWindowModes.ResolveFullScreenMode(DisplayWindowMode.Unspecified, platform),
          Is.EqualTo(DisplayWindowModes.ResolveFullScreenMode(DisplayWindowModes.Default, platform)));
      }
    }

    [TestCase(FullScreenMode.Windowed, DisplayWindowMode.Windowed)]
    [TestCase(FullScreenMode.MaximizedWindow, DisplayWindowMode.BorderlessWindow)]
    [TestCase(FullScreenMode.FullScreenWindow, DisplayWindowMode.FullScreen)]
    [TestCase(FullScreenMode.ExclusiveFullScreen, DisplayWindowMode.FullScreen)]
    public void Sanitize_RestoresModeFromLegacyFullScreenMode(FullScreenMode legacy, DisplayWindowMode expected)
    {
      var settings = new GraphicsSettingsData
      {
        WindowMode = DisplayWindowMode.Unspecified,
        FullScreenMode = legacy,
      };

      settings.Sanitize();

      Assert.That(settings.WindowMode, Is.EqualTo(expected));
      Assert.That(settings.FullScreenMode,
        Is.EqualTo(DisplayWindowModes.ResolveFullScreenMode(expected, Application.platform)));
    }

    [Test]
    public void Sanitize_KeepsAnExplicitWindowModeOverTheLegacyField()
    {
      var settings = new GraphicsSettingsData
      {
        WindowMode = DisplayWindowMode.Windowed,
        FullScreenMode = FullScreenMode.ExclusiveFullScreen,
      };

      settings.Sanitize();

      Assert.That(settings.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
      Assert.That(settings.FullScreenMode, Is.EqualTo(FullScreenMode.Windowed));
    }

    [Test]
    public void Sanitize_ReplacesAnOutOfRangeStoredValue()
    {
      var settings = new GraphicsSettingsData
      {
        WindowMode = (DisplayWindowMode)42,
        FullScreenMode = FullScreenMode.Windowed,
      };

      settings.Sanitize();

      Assert.That(settings.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
    }

    [Test]
    public void LegacyJson_WithoutWindowMode_RestoresTheStoredFullScreenMode()
    {
      // 저장 키 v2의 초기 형식에는 WindowMode 항목이 없었다. Windowed = 3.
      var loaded = JsonUtility.FromJson<GraphicsSettingsData>("{\"FullScreenMode\":3}");

      loaded.Sanitize();

      Assert.That(loaded.WindowMode, Is.EqualTo(DisplayWindowMode.Windowed));
    }

    [Test]
    public void Clone_PreservesWindowMode()
    {
      var original = GraphicsQualityPresets.Create(GraphicsQualityProfile.Low);
      original.WindowMode = DisplayWindowMode.BorderlessWindow;

      var clone = original.Clone();

      Assert.That(clone.WindowMode, Is.EqualTo(DisplayWindowMode.BorderlessWindow));
    }

    [Test]
    public void Labels_RoundTripForEverySelectableMode()
    {
      Assert.That(DisplayWindowModes.Selectable, Is.EquivalentTo(new[]
      {
        DisplayWindowMode.Windowed,
        DisplayWindowMode.BorderlessWindow,
        DisplayWindowMode.FullScreen,
      }));

      foreach (var mode in DisplayWindowModes.Selectable)
        Assert.That(DisplayWindowModes.FromLabel(DisplayWindowModes.Label(mode)), Is.EqualTo(mode));

      Assert.That(DisplayWindowModes.FromLabel("없는 라벨"), Is.EqualTo(DisplayWindowModes.Default));
    }
  }
}
