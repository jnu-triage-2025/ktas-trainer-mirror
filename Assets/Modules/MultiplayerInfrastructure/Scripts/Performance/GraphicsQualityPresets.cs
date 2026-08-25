using UnityEngine;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>프로젝트의 5단계 권장 그래픽 프리셋을 생성합니다.</summary>
  public static class GraphicsQualityPresets
  {
    /// <summary>에디터와 프로젝트 베이스라인에서 사용하는 기본 프로파일입니다.</summary>
    public const GraphicsQualityProfile DefaultProfile = GraphicsQualityProfile.Low;

    /// <summary>플레이어 설정이 없을 때 런타임에서 사용하는 기본 프로파일입니다.</summary>
    public const GraphicsQualityProfile RuntimeDefaultProfile = GraphicsQualityProfile.Medium;

    public static GraphicsSettingsData Create(GraphicsQualityProfile profile)
    {
      if (profile == GraphicsQualityProfile.Custom)
        profile = DefaultProfile;

      var settings = new GraphicsSettingsData
      {
        Profile = profile,
        ResolutionWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920,
        ResolutionHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080,
        FullScreenMode = FullScreenMode.FullScreenWindow,
        RefreshRate = 0,
        VSync = false,
      };

      ApplyPresetValues(settings, profile);
      return settings;
    }

    public static void ApplyPresetValues(GraphicsSettingsData s, GraphicsQualityProfile profile)
    {
      s.Profile = profile;

      switch (profile)
      {
        case GraphicsQualityProfile.VeryLow:
          Set(s, 0.60f, false, false, false, GraphicsAntiAliasing.Disabled, false,
            3, AnisotropicFiltering.Disable, 128, false, 512, 12f, 1, false,
            GraphicsAdditionalLights.Disabled, 0, false, 0.5f, 2, 0, false, false, false, false, 30);
          break;
        case GraphicsQualityProfile.Low:
          Set(s, 0.75f, false, false, false, GraphicsAntiAliasing.Disabled, false,
            2, AnisotropicFiltering.Disable, 256, true, 512, 20f, 1, false,
            GraphicsAdditionalLights.PerVertex, 2, false, 0.75f, 1, 1, false, false, false, false, 60);
          break;
        case GraphicsQualityProfile.Medium:
          Set(s, 0.85f, false, true, false, GraphicsAntiAliasing.MSAA2x, true,
            1, AnisotropicFiltering.Enable, 384, true, 1024, 35f, 2, true,
            GraphicsAdditionalLights.PerPixel, 4, false, 1f, 0, 2, false, true, true, true, 60);
          break;
        case GraphicsQualityProfile.High:
          Set(s, 1f, true, true, true, GraphicsAntiAliasing.MSAA4x, true,
            1, AnisotropicFiltering.ForceEnable, 512, true, 2048, 50f, 2, true,
            GraphicsAdditionalLights.PerPixel, 4, true, 1.5f, 0, 2, true, true, true, true, 0);
          break;
        case GraphicsQualityProfile.VeryHigh:
          Set(s, 1f, true, true, true, GraphicsAntiAliasing.MSAA8x, true,
            0, AnisotropicFiltering.ForceEnable, 768, true, 4096, 80f, 4, true,
            GraphicsAdditionalLights.PerPixel, 8, true, 2f, 0, 4, true, true, true, true, 0);
          break;
      }

      s.Sanitize();
    }

    private static void Set(
      GraphicsSettingsData s, float renderScale, bool hdr, bool depth, bool opaque,
      GraphicsAntiAliasing aa, bool postProcessing, int mipLimit, AnisotropicFiltering anisotropic,
      int streamingBudget, bool shadows, int shadowResolution, float shadowDistance, int cascades,
      bool softShadows, GraphicsAdditionalLights additionalLights, int lightsPerObject,
      bool additionalLightShadows, float lodBias, int maximumLod, int pixelLights,
      bool realtimeReflections, bool probeBlending, bool probeBoxProjection, bool softParticles,
      int frameRate)
    {
      s.RenderScale = renderScale;
      s.Hdr = hdr;
      s.DepthTexture = depth;
      s.OpaqueTexture = opaque;
      s.AntiAliasing = aa;
      s.PostProcessing = postProcessing;
      s.DynamicResolution = ProfileUsesDynamicResolution(s.Profile);
      s.TextureMipmapLimit = mipLimit;
      s.AnisotropicFiltering = anisotropic;
      s.TextureStreaming = true;
      s.TextureStreamingBudgetMb = streamingBudget;
      s.Shadows = shadows;
      s.ShadowResolution = (GraphicsShadowResolution)shadowResolution;
      s.ShadowDistance = shadowDistance;
      s.ShadowCascades = cascades;
      s.SoftShadows = softShadows;
      s.AdditionalLights = additionalLights;
      s.AdditionalLightsPerObject = lightsPerObject;
      s.AdditionalLightShadows = additionalLightShadows;
      s.LodBias = lodBias;
      s.MaximumLodLevel = maximumLod;
      s.PixelLightCount = pixelLights;
      s.RealtimeReflectionProbes = realtimeReflections;
      s.ReflectionProbeBlending = probeBlending;
      s.ReflectionProbeBoxProjection = probeBoxProjection;
      s.SoftParticles = softParticles;
      s.FrameRateLimit = frameRate;
    }

    private static bool ProfileUsesDynamicResolution(GraphicsQualityProfile profile)
      => profile == GraphicsQualityProfile.VeryLow || profile == GraphicsQualityProfile.Low;
  }
}
