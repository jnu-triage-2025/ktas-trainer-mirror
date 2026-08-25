#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MultiplayerInfrastructure.Performance.Tests
{
  public sealed class MppmLiteModePlayModeTests
  {
    [TearDown]
    public void TearDown()
    {
      if (MppmLiteMode.IsActive)
        MppmLiteMode.DeactivateForTests();
    }

    [Test]
    public void MainEditor_IsNeverMistakenForLiteClone()
    {
      Assert.That(MppmLiteMode.IsActive, Is.False);
    }

    [Test]
    public void AdditionalEditor_ActivatesLiteWithoutManualTagging()
    {
      Assert.That(MppmLiteMode.ShouldActivate(true, false, new string[0]), Is.True);
      Assert.That(MppmLiteMode.ShouldUseHeadlessVisuals(true, new string[0]), Is.False);
    }

    [Test]
    public void HeadlessLiteTag_IsRequiredToHideMppmVisuals()
    {
      Assert.That(MppmLiteMode.ShouldUseHeadlessVisuals(true,
        new[] { MppmLiteMode.HeadlessLiteTag }), Is.True);
      Assert.That(MppmLiteMode.ShouldUseHeadlessVisuals(false,
        new[] { MppmLiteMode.HeadlessLiteTag }), Is.False);
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
      var cameraObject = new GameObject("MPPM Lite Camera");
      var camera = cameraObject.AddComponent<UnityEngine.Camera>();
      var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
      camera.allowHDR = true;
      camera.allowMSAA = true;
      cameraData.renderPostProcessing = true;

      try
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
        Assert.That(camera.allowHDR, Is.False);
        Assert.That(camera.allowMSAA, Is.False);
        Assert.That(cameraData.renderPostProcessing, Is.False);
        Assert.That(camera.enabled, Is.True);
      }
      finally
      {
        Object.DestroyImmediate(cameraObject);
      }
    }

    [Test]
    public void LiteLifecycle_DisablesAndRestoresVisualComponentsWithoutDestroyingThem()
    {
      var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
      var renderer = root.GetComponent<Renderer>();
      var document = root.AddComponent<UnityEngine.UIElements.UIDocument>();
      var audioSource = root.AddComponent<AudioSource>();
      var initiallyDisabledLight = root.AddComponent<Light>();
      initiallyDisabledLight.enabled = false;
      bool originalAudioPaused = AudioListener.pause;
      float originalAudioVolume = AudioListener.volume;

      try
      {
        MppmLiteMode.ActivateForTests();
        AudioListener.pause = !originalAudioPaused;
        AudioListener.volume = 0f;
        MppmLiteMode.StripVisuals(root);

        Assert.That(renderer, Is.Not.Null);
        Assert.That(document, Is.Not.Null);
        Assert.That(audioSource, Is.Not.Null);
        Assert.That(renderer.enabled, Is.False);
        Assert.That(document.enabled, Is.False);
        Assert.That(audioSource.enabled, Is.False);
        Assert.That(initiallyDisabledLight.enabled, Is.False);

        MppmLiteMode.DeactivateForTests();

        Assert.That(renderer.enabled, Is.True);
        Assert.That(document.enabled, Is.True);
        Assert.That(audioSource.enabled, Is.True);
        Assert.That(initiallyDisabledLight.enabled, Is.False);
        Assert.That(AudioListener.pause, Is.EqualTo(originalAudioPaused));
        Assert.That(AudioListener.volume, Is.EqualTo(originalAudioVolume).Within(0.001f));
      }
      finally
      {
        if (MppmLiteMode.IsActive)
          MppmLiteMode.DeactivateForTests();
        Object.DestroyImmediate(root);
      }
    }

    [Test]
    public void VisibleLiteSession_DoesNotDisableCameraOrRenderers()
    {
      var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
      var camera = root.AddComponent<UnityEngine.Camera>();
      var renderer = root.GetComponent<Renderer>();

      try
      {
        MppmLiteMode.ActivateForTests(headless: false);
        MppmLiteMode.StripVisuals(root);

        Assert.That(MppmLiteMode.IsActive, Is.True);
        Assert.That(MppmLiteMode.IsHeadless, Is.False);
        Assert.That(camera.enabled, Is.True);
        Assert.That(renderer.enabled, Is.True);
      }
      finally
      {
        if (MppmLiteMode.IsActive)
          MppmLiteMode.DeactivateForTests();
        Object.DestroyImmediate(root);
      }
    }
  }
}
#endif
