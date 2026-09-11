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

    /// <summary>
    /// 사용자가 선택한 화면 모드입니다. 플랫폼별 실제 <see cref="UnityEngine.FullScreenMode"/>는
    /// 적용 시점에 <see cref="DisplayWindowModes.ResolveFullScreenMode"/>로 결정합니다.
    /// 필드 초기값을 Unspecified로 두어, 이 항목이 없던 저장값을 읽을 때
    /// <see cref="Sanitize"/>가 <see cref="FullScreenMode"/>로부터 모드를 복원할 수 있게 합니다.
    /// </summary>
    public DisplayWindowMode WindowMode = DisplayWindowMode.Unspecified;

    /// <summary>
    /// <see cref="WindowMode"/>를 현재 플랫폼에서 적용한 결과입니다. <see cref="Sanitize"/>가 갱신하며,
    /// <see cref="WindowMode"/>가 저장되기 전 설정의 하위 호환 용도로도 읽습니다.
    /// </summary>
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
    [Range(0.5f, 1.5f)] public float Gamma = 1f;

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
      WindowMode = DisplayWindowModes.Sanitize(WindowMode, FullScreenMode);
      FullScreenMode = DisplayWindowModes.ResolveFullScreenMode(WindowMode, Application.platform);
      RefreshRate = Mathf.Clamp(RefreshRate, 0, 1000);
      FrameRateLimit = Mathf.Clamp(FrameRateLimit, 0, 1000);
      RenderScale = Mathf.Clamp(RenderScale, 0.5f, 2f);
      FieldOfView = Mathf.Clamp(FieldOfView, 40f, 100f);
      Gamma = Mathf.Clamp(Gamma, 0.5f, 1.5f);
      TextureMipmapLimit = Mathf.Clamp(TextureMipmapLimit, 0, 3);
      TextureStreamingBudgetMb = Mathf.Clamp(TextureStreamingBudgetMb, 64, 2048);
      ShadowDistance = Mathf.Clamp(ShadowDistance, 0f, 200f);
      ShadowCascades = ShadowCascades <= 1 ? 1 : ShadowCascades <= 2 ? 2 : 4;
      AdditionalLightsPerObject = Mathf.Clamp(AdditionalLightsPerObject, 0, 8);
      LodBias = Mathf.Clamp(LodBias, 0.25f, 4f);
      MaximumLodLevel = Mathf.Clamp(MaximumLodLevel, 0, 3);
      PixelLightCount = Mathf.Clamp(PixelLightCount, 0, 8);

      // 정수로 저장된 열거형은 값이 바뀐 다른 버전의 저장값을 그대로 들여올 수 있다. 열거형에 없는 값은
      // URP 에셋이나 QualitySettings에 그대로 들어가므로 기본값으로 바꾼다. 프로파일만은 나머지 값을 보존하는
      // 사용자 지정으로 본다.
      Profile = SanitizeEnum(Profile, GraphicsQualityProfile.Custom);
      AntiAliasing = SanitizeEnum(AntiAliasing, GraphicsAntiAliasing.Disabled);
      AnisotropicFiltering = SanitizeEnum(AnisotropicFiltering, AnisotropicFiltering.Disable);
      ShadowResolution = SanitizeEnum(ShadowResolution, GraphicsShadowResolution.Low);
      AdditionalLights = SanitizeEnum(AdditionalLights, GraphicsAdditionalLights.PerVertex);
    }

    /// <summary>열거형에 없는 저장값을 <paramref name="fallback"/>으로 바꿉니다.</summary>
    private static T SanitizeEnum<T>(T value, T fallback) where T : struct, Enum
      => Enum.IsDefined(typeof(T), value) ? value : fallback;
  }
}
