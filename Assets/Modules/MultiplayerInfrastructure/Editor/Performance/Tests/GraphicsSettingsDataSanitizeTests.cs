using MultiplayerInfrastructure.Performance;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Performance.Tests
{
  /// <summary>
  /// 저장된 그래픽 설정의 열거형 값이 현재 정의에 없을 때 기본값으로 정리되는지 검증합니다.
  /// 정리하지 않으면 다른 버전이 남긴 정수가 URP 에셋과 QualitySettings에 그대로 들어갑니다.
  /// </summary>
  public class GraphicsSettingsDataSanitizeTests
  {
    [Test]
    public void UndefinedEnumValues_FallBackToDefaults()
    {
      var settings = GraphicsQualityPresets.Create(GraphicsQualityProfile.High);
      settings.Profile = (GraphicsQualityProfile)99;
      settings.AntiAliasing = (GraphicsAntiAliasing)3;
      settings.AnisotropicFiltering = (AnisotropicFiltering)42;
      settings.ShadowResolution = (GraphicsShadowResolution)777;
      settings.AdditionalLights = (GraphicsAdditionalLights)(-1);

      settings.Sanitize();

      Assert.That(settings.Profile, Is.EqualTo(GraphicsQualityProfile.Custom));
      Assert.That(settings.AntiAliasing, Is.EqualTo(GraphicsAntiAliasing.Disabled));
      Assert.That(settings.AnisotropicFiltering, Is.EqualTo(AnisotropicFiltering.Disable));
      Assert.That(settings.ShadowResolution, Is.EqualTo(GraphicsShadowResolution.Low));
      Assert.That(settings.AdditionalLights, Is.EqualTo(GraphicsAdditionalLights.PerVertex));
    }

    [Test]
    public void DefinedEnumValues_AreKept()
    {
      var settings = GraphicsQualityPresets.Create(GraphicsQualityProfile.VeryHigh);
      settings.AntiAliasing = GraphicsAntiAliasing.MSAA8x;
      settings.AnisotropicFiltering = AnisotropicFiltering.ForceEnable;
      settings.ShadowResolution = GraphicsShadowResolution.VeryHigh;
      settings.AdditionalLights = GraphicsAdditionalLights.PerPixel;

      settings.Sanitize();

      Assert.That(settings.Profile, Is.EqualTo(GraphicsQualityProfile.VeryHigh));
      Assert.That(settings.AntiAliasing, Is.EqualTo(GraphicsAntiAliasing.MSAA8x));
      Assert.That(settings.AnisotropicFiltering, Is.EqualTo(AnisotropicFiltering.ForceEnable));
      Assert.That(settings.ShadowResolution, Is.EqualTo(GraphicsShadowResolution.VeryHigh));
      Assert.That(settings.AdditionalLights, Is.EqualTo(GraphicsAdditionalLights.PerPixel));
    }

    [Test]
    public void SanitizedSettings_SurviveJsonRoundTrip()
    {
      var settings = GraphicsQualityPresets.Create(GraphicsQualityProfile.Medium);
      settings.AntiAliasing = (GraphicsAntiAliasing)3;

      var loaded = JsonUtility.FromJson<GraphicsSettingsData>(JsonUtility.ToJson(settings));
      loaded.Sanitize();

      Assert.That(loaded.AntiAliasing, Is.EqualTo(GraphicsAntiAliasing.Disabled));
      Assert.That(loaded.Profile, Is.EqualTo(GraphicsQualityProfile.Medium));
    }
  }
}
