using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>사용자에게 노출되는 종합 그래픽 품질 프리셋입니다.</summary>
  public enum GraphicsQualityProfile
  {
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh,
    Custom,
  }

  public enum GraphicsAntiAliasing
  {
    Disabled = 1,
    MSAA2x = 2,
    MSAA4x = 4,
    MSAA8x = 8,
  }

  public enum GraphicsShadowResolution
  {
    Low = 512,
    Medium = 1024,
    High = 2048,
    VeryHigh = 4096,
  }

  public enum GraphicsAdditionalLights
  {
    Disabled,
    PerVertex,
    PerPixel,
  }

  /// <summary>
  /// 저장·복제 가능한 그래픽 설정 스냅샷입니다. 디스플레이, 품질, URP 및
  /// 일반적인 렌더링 옵션을 한 곳에서 관리합니다.
  /// </summary>
  [Serializable]
  public class GraphicsSettingsData
  {
    public GraphicsQualityProfile Profile = GraphicsQualityProfile.Low;

    [Header("Display")]
    public int ResolutionWidth = 1920;
    public int ResolutionHeight = 1080;
    public FullScreenMode FullScreenMode = FullScreenMode.FullScreenWindow;
    public int RefreshRate = 60;
    public bool VSync;
    public int FrameRateLimit = 60;

    [Header("Rendering")]
    [Range(0.5f, 2f)] public float RenderScale = 0.75f;
    public bool Hdr;
    public bool DepthTexture;
    public bool OpaqueTexture;
    public GraphicsAntiAliasing AntiAliasing = GraphicsAntiAliasing.Disabled;
    public bool PostProcessing;
    public bool DynamicResolution;
    [Range(40f, 100f)] public float FieldOfView = 60f;

    [Header("Textures")]
    [Range(0, 3)] public int TextureMipmapLimit = 2;
    public AnisotropicFiltering AnisotropicFiltering = AnisotropicFiltering.Disable;
    public bool TextureStreaming = true;
    [Range(64, 2048)] public int TextureStreamingBudgetMb = 256;

    [Header("Lighting and shadows")]
    public bool Shadows = true;
    public GraphicsShadowResolution ShadowResolution = GraphicsShadowResolution.Low;
    [Range(0f, 200f)] public float ShadowDistance = 20f;
    [Range(1, 4)] public int ShadowCascades = 1;
    public bool SoftShadows;
    public GraphicsAdditionalLights AdditionalLights = GraphicsAdditionalLights.PerVertex;
    [Range(0, 8)] public int AdditionalLightsPerObject = 2;
    public bool AdditionalLightShadows;

    [Header("Detail and effects")]
    [Range(0.25f, 4f)] public float LodBias = 0.75f;
    [Range(0, 3)] public int MaximumLodLevel = 1;
    public int PixelLightCount = 1;
    public bool RealtimeReflectionProbes;
    public bool ReflectionProbeBlending;
    public bool ReflectionProbeBoxProjection;
    public bool SoftParticles;

    public GraphicsSettingsData Clone()
      => JsonUtility.FromJson<GraphicsSettingsData>(JsonUtility.ToJson(this));

    public void Sanitize()
    {
      ResolutionWidth = Mathf.Clamp(ResolutionWidth, 640, 16384);
      ResolutionHeight = Mathf.Clamp(ResolutionHeight, 360, 8640);
      RefreshRate = Mathf.Clamp(RefreshRate, 0, 1000);
      FrameRateLimit = Mathf.Clamp(FrameRateLimit, 0, 1000);
      RenderScale = Mathf.Clamp(RenderScale, 0.5f, 2f);
      FieldOfView = Mathf.Clamp(FieldOfView, 40f, 100f);
      TextureMipmapLimit = Mathf.Clamp(TextureMipmapLimit, 0, 3);
      TextureStreamingBudgetMb = Mathf.Clamp(TextureStreamingBudgetMb, 64, 2048);
      ShadowDistance = Mathf.Clamp(ShadowDistance, 0f, 200f);
      ShadowCascades = ShadowCascades <= 1 ? 1 : ShadowCascades <= 2 ? 2 : 4;
      AdditionalLightsPerObject = Mathf.Clamp(AdditionalLightsPerObject, 0, 8);
      LodBias = Mathf.Clamp(LodBias, 0.25f, 4f);
      MaximumLodLevel = Mathf.Clamp(MaximumLodLevel, 0, 3);
      PixelLightCount = Mathf.Clamp(PixelLightCount, 0, 8);
    }
  }
}
