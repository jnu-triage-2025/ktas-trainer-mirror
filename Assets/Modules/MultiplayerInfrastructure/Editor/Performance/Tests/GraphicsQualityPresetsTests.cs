using MultiplayerInfrastructure.Performance;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MultiplayerInfrastructure.Editor.Performance.Tests
{
  public class GraphicsQualityPresetsTests
  {
    [Test]
    public void DefaultProfile_IsAlwaysLow()
    {
      Assert.That(GraphicsQualityPresets.DefaultProfile, Is.EqualTo(GraphicsQualityProfile.Low));
      Assert.That(GraphicsQualityPresets.Create(GraphicsQualityProfile.Custom).Profile,
        Is.EqualTo(GraphicsQualityProfile.Low));
    }

    [TestCase(GraphicsQualityProfile.VeryLow)]
    [TestCase(GraphicsQualityProfile.Low)]
    [TestCase(GraphicsQualityProfile.Medium)]
    [TestCase(GraphicsQualityProfile.High)]
    [TestCase(GraphicsQualityProfile.VeryHigh)]
    public void EveryPreset_IsValidAndKeepsItsIdentity(GraphicsQualityProfile profile)
    {
      var settings = GraphicsQualityPresets.Create(profile);

      Assert.That(settings.Profile, Is.EqualTo(profile));
      Assert.That(settings.RenderScale, Is.InRange(0.5f, 2f));
      Assert.That(settings.TextureMipmapLimit, Is.InRange(0, 3));
      Assert.That(settings.ShadowCascades,
        Is.EqualTo(1).Or.EqualTo(2).Or.EqualTo(4));
      Assert.That(settings.TextureStreamingBudgetMb, Is.InRange(64, 2048));
    }

    [Test]
    public void Presets_IncreaseVisualQualityMonotonically()
    {
      var veryLow = GraphicsQualityPresets.Create(GraphicsQualityProfile.VeryLow);
      var low = GraphicsQualityPresets.Create(GraphicsQualityProfile.Low);
      var medium = GraphicsQualityPresets.Create(GraphicsQualityProfile.Medium);
      var high = GraphicsQualityPresets.Create(GraphicsQualityProfile.High);
      var veryHigh = GraphicsQualityPresets.Create(GraphicsQualityProfile.VeryHigh);

      Assert.That(veryLow.RenderScale, Is.LessThan(low.RenderScale));
      Assert.That(low.RenderScale, Is.LessThan(medium.RenderScale));
      Assert.That(medium.RenderScale, Is.LessThan(high.RenderScale));
      Assert.That(veryLow.TextureMipmapLimit, Is.GreaterThan(low.TextureMipmapLimit));
      Assert.That(low.TextureMipmapLimit, Is.GreaterThan(medium.TextureMipmapLimit));
      Assert.That(medium.TextureMipmapLimit, Is.GreaterThanOrEqualTo(high.TextureMipmapLimit));
      Assert.That(high.TextureMipmapLimit, Is.GreaterThan(veryHigh.TextureMipmapLimit));
    }

    [Test]
    public void LiteMode_IsMoreRestrictiveThanVeryLow()
    {
      var lite = MppmLiteMode.CreateSettings();

      Assert.That(lite.RenderScale, Is.EqualTo(0.5f));
      Assert.That(lite.FrameRateLimit, Is.EqualTo(15));
      Assert.That(lite.TextureMipmapLimit, Is.EqualTo(3));
      Assert.That(lite.TextureStreamingBudgetMb, Is.EqualTo(64));
      Assert.That(lite.Shadows, Is.False);
      Assert.That(lite.PostProcessing, Is.False);
      Assert.That(lite.AdditionalLights, Is.EqualTo(GraphicsAdditionalLights.Disabled));
    }

    [TestCase(GraphicsAntiAliasing.Disabled, 0)]
    [TestCase(GraphicsAntiAliasing.MSAA2x, 2)]
    [TestCase(GraphicsAntiAliasing.MSAA4x, 4)]
    [TestCase(GraphicsAntiAliasing.MSAA8x, 8)]
    public void QualitySettingsAntiAliasing_UsesUnityQualityApiValues(
      GraphicsAntiAliasing value, int expected)
    {
      Assert.That(TexturePerformanceService.ToQualitySettingsAntiAliasing(value), Is.EqualTo(expected));
    }

    [Test]
    public void Clone_ProducesIndependentSettings()
    {
      var original = GraphicsQualityPresets.Create(GraphicsQualityProfile.Low);
      var clone = original.Clone();
      clone.RenderScale = 2f;

      Assert.That(original.RenderScale, Is.EqualTo(0.75f));
      Assert.That(clone.RenderScale, Is.EqualTo(2f));
    }

    [Test]
    public void ProjectBaseline_IsConfiguredAsLow()
    {
      var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
        "Assets/Settings/PC_RPAsset.asset");

      Assert.That(asset, Is.Not.Null);
      Assert.That(asset.renderScale, Is.EqualTo(0.75f).Within(0.001f));
      Assert.That(asset.supportsHDR, Is.False);
      Assert.That(asset.supportsCameraDepthTexture, Is.False);
      Assert.That(asset.supportsCameraOpaqueTexture, Is.False);
      Assert.That(asset.mainLightShadowmapResolution, Is.EqualTo(512));
      Assert.That(asset.shadowDistance, Is.EqualTo(20f).Within(0.001f));
      Assert.That(asset.shadowCascadeCount, Is.EqualTo(1));

      Assert.That(QualitySettings.globalTextureMipmapLimit, Is.EqualTo(2));
      Assert.That(QualitySettings.lodBias, Is.EqualTo(0.75f).Within(0.001f));
      Assert.That(QualitySettings.maximumLODLevel, Is.EqualTo(1));
    }
  }
}
