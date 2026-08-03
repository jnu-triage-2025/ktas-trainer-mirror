using System;
using System.Reflection;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>
  /// 종합 그래픽 설정을 저장하고 즉시 적용하는 서비스입니다.
  /// 기존 텍스처 품질 API는 하위 호환을 위해 유지합니다.
  /// </summary>
  public class TexturePerformanceService : MonoBehaviour
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.GraphicsSettings.v2";
    private GraphicsSettingsData _currentSettings;

    public event Action<TextureQuality> OnQualityChanged;
    public event Action<GraphicsSettingsData> OnSettingsChanged;

    public GraphicsSettingsData CurrentSettings => _currentSettings?.Clone();
    public GraphicsQualityProfile CurrentProfile
      => _currentSettings?.Profile ?? GraphicsQualityPresets.RuntimeDefaultProfile;
    public TextureQuality CurrentQuality
      => (TextureQuality)Mathf.Clamp(_currentSettings?.TextureMipmapLimit ?? 2, 0, 3);

    private void Awake()
    {
      Registry.Registry.Register(
        RegistryType.Service,
        Registry.Registry.TypeKey<TexturePerformanceService>(),
        this);

      LoadAndApply();
      SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      Registry.Registry.Unregister(
        RegistryType.Service,
        Registry.Registry.TypeKey<TexturePerformanceService>());
    }

    public void SetProfile(GraphicsQualityProfile profile)
    {
      if (profile == GraphicsQualityProfile.Custom)
        return;

      var next = GraphicsQualityPresets.Create(profile);
      PreserveDisplaySettings(_currentSettings, next);
      SetSettings(next);
    }

    public void SetSettings(GraphicsSettingsData settings)
    {
      if (settings == null)
        throw new ArgumentNullException(nameof(settings));

      _currentSettings = settings.Clone();
      _currentSettings.Sanitize();
      ApplyToUnity(_currentSettings, applyDisplay: true);
      Save(_currentSettings);

      OnQualityChanged?.Invoke(CurrentQuality);
      OnSettingsChanged?.Invoke(CurrentSettings);
      Debug.Log($"[GraphicsPerformance] 설정 적용: {_currentSettings.Profile}");
    }

    /// <summary>기존 텍스처 설정 호출자를 위한 호환 API입니다.</summary>
    public void SetQuality(TextureQuality quality)
    {
      var next = CurrentSettings ?? GraphicsQualityPresets.Create(GraphicsQualityPresets.RuntimeDefaultProfile);
      next.Profile = GraphicsQualityProfile.Custom;
      next.TextureMipmapLimit = (int)quality;
      SetSettings(next);
    }

    public void ResetToDefault() => SetProfile(GraphicsQualityPresets.RuntimeDefaultProfile);

    public void LoadAndApply()
    {
      _currentSettings = Load();

      // MPPM Lite는 사용자 저장값보다 우선하며 PlayerPrefs를 덮어쓰지 않는다.
      if (MppmLiteMode.IsActive)
        _currentSettings = MppmLiteMode.CreateSettings();

      ApplyToUnity(_currentSettings, applyDisplay: !MppmLiteMode.IsActive);
      Debug.Log($"[GraphicsPerformance] 설정 불러오기: {_currentSettings.Profile}" +
                (MppmLiteMode.IsActive ? " (MPPM Lite)" : string.Empty));
    }

    public void ReapplySceneSettings() => ApplySceneSettings(_currentSettings);

    private void HandleSceneLoaded(Scene _, LoadSceneMode __) => ApplySceneSettings(_currentSettings);

    internal static void ApplyToUnity(GraphicsSettingsData settings, bool applyDisplay)
    {
      if (settings == null)
        return;

      settings.Sanitize();
      QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
      Application.targetFrameRate = settings.VSync ? -1 :
        settings.FrameRateLimit <= 0 ? -1 : settings.FrameRateLimit;
      QualitySettings.globalTextureMipmapLimit = settings.TextureMipmapLimit;
      QualitySettings.anisotropicFiltering = settings.AnisotropicFiltering;
#if UNITY_EDITOR_OSX
      // Main Editor는 기존 macOS shader-progress crash 우회를 유지한다.
      // 카메라를 끄는 MPPM Lite clone만 제한된 예산의 streaming을 사용한다.
      QualitySettings.streamingMipmapsActive = MppmLiteMode.IsActive && settings.TextureStreaming;
#else
      QualitySettings.streamingMipmapsActive = settings.TextureStreaming;
#endif
      QualitySettings.streamingMipmapsMemoryBudget = settings.TextureStreamingBudgetMb;
      QualitySettings.antiAliasing = ToQualitySettingsAntiAliasing(settings.AntiAliasing);
      QualitySettings.shadows = settings.Shadows
        ? UnityEngine.ShadowQuality.All
        : UnityEngine.ShadowQuality.Disable;
      QualitySettings.shadowResolution = ToUnityShadowResolution(settings.ShadowResolution);
      QualitySettings.shadowDistance = settings.ShadowDistance;
      QualitySettings.shadowCascades = settings.ShadowCascades;
      QualitySettings.lodBias = settings.LodBias;
      QualitySettings.maximumLODLevel = settings.MaximumLodLevel;
      QualitySettings.pixelLightCount = settings.PixelLightCount;
      QualitySettings.realtimeReflectionProbes = settings.RealtimeReflectionProbes;
      QualitySettings.softParticles = settings.SoftParticles;

      ApplyUrpSettings(settings);
      ApplySceneSettings(settings);

      if (applyDisplay)
      {
        var refresh = settings.RefreshRate > 0
          ? new RefreshRate { numerator = (uint)settings.RefreshRate, denominator = 1 }
          : Screen.currentResolution.refreshRateRatio;
        Screen.SetResolution(
          settings.ResolutionWidth,
          settings.ResolutionHeight,
          settings.FullScreenMode,
          refresh);
      }
    }

    private static void ApplyUrpSettings(GraphicsSettingsData settings)
    {
      var asset = GraphicsPipelineRuntimeAsset.GetOrCreate();
      if (asset == null)
        return;

      asset.renderScale = settings.RenderScale;
      asset.supportsHDR = settings.Hdr;
      asset.supportsCameraDepthTexture = settings.DepthTexture;
      asset.supportsCameraOpaqueTexture = settings.OpaqueTexture;
      asset.msaaSampleCount = (int)settings.AntiAliasing;
      SetUrpProperty(asset, "supportsMainLightShadows", settings.Shadows);
      asset.mainLightShadowmapResolution = (int)settings.ShadowResolution;
      asset.shadowDistance = settings.ShadowDistance;
      asset.shadowCascadeCount = settings.ShadowCascades;
      SetUrpProperty(asset, "additionalLightsRenderingMode", settings.AdditionalLights switch
      {
        GraphicsAdditionalLights.Disabled => LightRenderingMode.Disabled,
        GraphicsAdditionalLights.PerVertex => LightRenderingMode.PerVertex,
        _ => LightRenderingMode.PerPixel,
      });
      asset.maxAdditionalLightsCount = settings.AdditionalLightsPerObject;
      SetUrpProperty(asset, "supportsAdditionalLightShadows",
        settings.Shadows && settings.AdditionalLightShadows);
      SetUrpProperty(asset, "supportsSoftShadows", settings.Shadows && settings.SoftShadows);
      SetUrpProperty(asset, "reflectionProbeBlending", settings.ReflectionProbeBlending);
      SetUrpProperty(asset, "reflectionProbeBoxProjection", settings.ReflectionProbeBoxProjection);
      SetUrpProperty(asset, "reflectionProbeAtlas",
        settings.ReflectionProbeBlending || settings.ReflectionProbeBoxProjection);
    }

    private static void SetUrpProperty<T>(UniversalRenderPipelineAsset asset, string propertyName, T value)
    {
      var property = typeof(UniversalRenderPipelineAsset).GetProperty(
        propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      property?.SetValue(asset, value);
    }

    private static void ApplySceneSettings(GraphicsSettingsData settings)
    {
      if (settings == null)
        return;

      // Main camera 외의 overlay/보조 카메라도 자체 HDR/MSAA RenderTexture를 만들 수 있다.
      // MPPM을 포함한 저사양 프로파일에서는 모든 카메라에 동일한 버퍼 정책을 적용한다.
      foreach (var camera in FindAllCameras())
      {
        if (camera == null)
          continue;

        camera.allowHDR = settings.Hdr;
        camera.allowMSAA = settings.AntiAliasing != GraphicsAntiAliasing.Disabled;
        camera.allowDynamicResolution = settings.DynamicResolution;
        if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
          cameraData.renderPostProcessing = settings.PostProcessing;
      }

      var mainCamera = MainCameraController.Instance?.Camera;
      if (mainCamera != null)
        mainCamera.fieldOfView = settings.FieldOfView;
    }

    private static UnityEngine.Camera[] FindAllCameras()
      => UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);

    public static int ToQualitySettingsAntiAliasing(GraphicsAntiAliasing value)
      => value == GraphicsAntiAliasing.Disabled ? 0 : (int)value;

    private static UnityEngine.ShadowResolution ToUnityShadowResolution(GraphicsShadowResolution value)
      => value switch
      {
        GraphicsShadowResolution.Low => UnityEngine.ShadowResolution.Low,
        GraphicsShadowResolution.Medium => UnityEngine.ShadowResolution.Medium,
        GraphicsShadowResolution.High => UnityEngine.ShadowResolution.High,
        _ => UnityEngine.ShadowResolution.VeryHigh,
      };

    private static void PreserveDisplaySettings(GraphicsSettingsData source, GraphicsSettingsData destination)
    {
      if (source == null || destination == null)
        return;

      destination.ResolutionWidth = source.ResolutionWidth;
      destination.ResolutionHeight = source.ResolutionHeight;
      destination.FullScreenMode = source.FullScreenMode;
      destination.RefreshRate = source.RefreshRate;
      destination.FieldOfView = source.FieldOfView;
    }

    private static void Save(GraphicsSettingsData settings)
    {
      PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(settings));
      PlayerPrefs.Save();
    }

    private static GraphicsSettingsData Load()
    {
      if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        return GraphicsQualityPresets.Create(GraphicsQualityPresets.RuntimeDefaultProfile);

      try
      {
        var loaded = JsonUtility.FromJson<GraphicsSettingsData>(PlayerPrefs.GetString(PlayerPrefsKey));
        if (loaded != null)
        {
          loaded.Sanitize();
          return loaded;
        }
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[GraphicsPerformance] 저장 설정을 읽지 못해 보통 기본값을 사용합니다: {exception.Message}");
      }

      return GraphicsQualityPresets.Create(GraphicsQualityPresets.RuntimeDefaultProfile);
    }
  }

  internal static class GraphicsPipelineRuntimeAsset
  {
    private static UniversalRenderPipelineAsset _runtimeAsset;
    private static RenderPipelineAsset _sourceAsset;

    public static UniversalRenderPipelineAsset GetOrCreate()
    {
      if (_runtimeAsset != null)
        return _runtimeAsset;

      var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
      if (source == null)
        return null;

      _sourceAsset = source;
      _runtimeAsset = UnityEngine.Object.Instantiate(source);
      _runtimeAsset.name = source.name + " (Runtime Graphics Settings)";
      _runtimeAsset.hideFlags = HideFlags.DontSave;
      QualitySettings.renderPipeline = _runtimeAsset;
      return _runtimeAsset;
    }

    public static void Release(RenderPipelineAsset restoreAsset)
    {
      // 이미 생성된 런타임 복제본을 캡처한 호출자도 파괴 예정 객체를 다시 지정하지 않게 한다.
      QualitySettings.renderPipeline = restoreAsset == _runtimeAsset ? _sourceAsset : restoreAsset;
      if (_runtimeAsset != null)
      {
        if (Application.isPlaying)
          UnityEngine.Object.Destroy(_runtimeAsset);
        else
          UnityEngine.Object.DestroyImmediate(_runtimeAsset);
      }
      _runtimeAsset = null;
      _sourceAsset = null;
    }
  }
}
