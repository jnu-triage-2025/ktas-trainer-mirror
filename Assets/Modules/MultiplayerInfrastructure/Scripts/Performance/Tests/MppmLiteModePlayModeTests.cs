#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MultiplayerInfrastructure.Performance.Tests
{
  public sealed class MppmLiteModePlayModeTests
  {
    [Test]
    public void MainEditor_IsNeverMistakenForLiteClone()
    {
      Assert.That(MppmLiteMode.IsActive, Is.False);
    }

    [Test]
    public void AdditionalEditor_ActivatesLiteWithoutManualTagging()
    {
      Assert.That(MppmLiteMode.ShouldActivate(true, false, new string[0]), Is.True);
    }

    [Test]
    public void FullClientTag_OptsAdditionalEditorOutOfLite()
    {
      Assert.That(MppmLiteMode.ShouldActivate(true, false,
        new[] { MppmLiteMode.FullClientTag }), Is.False);
    }

    [Test]
    public void StandaloneBuild_CanNeverActivateMppmLite()
    {
      Assert.That(MppmLiteMode.ShouldActivate(false, false, new string[0]), Is.False);
    }

    [Test]
    public void LiteSettings_ApplyToQualitySettingsAndRuntimeUrpAsset()
    {
      var settings = MppmLiteMode.CreateSettings();
      TexturePerformanceService.ApplyToUnity(settings, applyDisplay: false);

      var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
      Assert.That(asset, Is.Not.Null);
      Assert.That(asset.renderScale, Is.EqualTo(0.5f).Within(0.001f));
      Assert.That(asset.supportsHDR, Is.False);
      Assert.That(asset.supportsMainLightShadows, Is.False);
      Assert.That(asset.additionalLightsRenderingMode, Is.EqualTo(LightRenderingMode.Disabled));
      Assert.That(QualitySettings.globalTextureMipmapLimit, Is.EqualTo(3));
      Assert.That(QualitySettings.streamingMipmapsMemoryBudget, Is.EqualTo(64));
      Assert.That(QualitySettings.antiAliasing, Is.EqualTo(0));
      Assert.That(asset.msaaSampleCount, Is.EqualTo(1));
      Assert.That(Application.targetFrameRate, Is.EqualTo(15));
    }
  }
}
#endif
