using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
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
    private const float GammaVolumePriority = 10000f;
    private GraphicsSettingsData _currentSettings;
    private static Volume _gammaVolume;
    private static LiftGammaGain _liftGammaGain;

    public event Action<TextureQuality> OnQualityChanged;
    public event Action<GraphicsSettingsData> OnSettingsChanged;

    public GraphicsSettingsData CurrentSettings => _currentSettings?.Clone();
    public GraphicsQualityProfile CurrentProfile
      => _currentSettings?.Profile ?? GraphicsQualityPresets.RuntimeDefaultProfile;
    public TextureQuality CurrentQuality
      => (TextureQuality)Mathf.Clamp(_currentSettings?.TextureMipmapLimit ?? 2, 0, 3);

    /// <summary>
    /// 이 프로세스에서 디스플레이(해상도·화면 모드) 설정을 이미 한 번 적용했는지 나타냅니다.
    /// 창과 화면 모드는 씬이 바뀌어도 그대로 남으므로, 씬마다 새로 깨어나는 서비스가 저장값을 다시 적용하면
    /// 사용자가 그 사이에 바꾼 창 크기나 모드가 되돌아갑니다. 그래서 시작 시 한 번만 적용하고, 이후에는
    /// 설정 화면에서 명시적으로 적용할 때만 바꿉니다.
    /// </summary>
    private static bool _displayAppliedThisSession;

    /// <summary>씬에 배치된 서비스가 없을 때 <see cref="GetOrCreateInstance"/>가 만든 임시 인스턴스인지 나타냅니다.</summary>
    private bool _isRuntimeFallback;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
      _displayAppliedThisSession = false;
    }

    /// <summary>등록되어 있으면 서비스 인스턴스를, 아니면 null을 돌려줍니다.</summary>
    public static TexturePerformanceService Instance
      => Registry.Registry.Get<TexturePerformanceService>(
        RegistryType.Service, Registry.Registry.TypeKey<TexturePerformanceService>());

    /// <summary>
    /// 씬에 배치된 서비스가 아직 로드되지 않았어도 설정 화면에서 바로 쓸 수 있게 합니다.
    ///
    /// IntroScene에는 서비스가 없고 SystemOverlayScene이 게임 진입 시에 추가 로드되므로, 시작 화면에서도
    /// 그래픽 설정을 읽고 저장할 수 있도록 필요하면 지속되는 런타임 서비스를 만듭니다. 이후 씬에 배치된
    /// 서비스가 깨어나면 임시 인스턴스는 정리되고 역할을 넘겨받습니다.
    /// </summary>
    public static TexturePerformanceService GetOrCreateInstance()
    {
      var service = Instance;
      if (service != null)
        return service;

      service = FindAnyObjectByType<TexturePerformanceService>();
      if (service != null)
      {
        Registry.Registry.Register(
          RegistryType.Service,
          Registry.Registry.TypeKey<TexturePerformanceService>(),
          service);
        return service;
      }

      var host = new GameObject(nameof(TexturePerformanceService));
      DontDestroyOnLoad(host);
      var created = host.AddComponent<TexturePerformanceService>();
      created._isRuntimeFallback = true;
      return created;
    }

    /// <summary>
    /// 첫 씬에 서비스가 없어도(IntroScene) 저장된 디스플레이 설정이 시작 시 적용되게 합니다.
    /// 첫 씬에 서비스가 배치되어 있으면 그 Awake가 이미 적용했으므로 아무 일도 하지 않습니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedSettingsOnStartup()
    {
      if (MppmLiteMode.IsActive || _displayAppliedThisSession)
        return;
      GetOrCreateInstance();
    }

    private void Awake()
    {
      // 시작 화면에서 만든 임시 서비스가 남아 있으면, 씬에 배치된 이 인스턴스가 역할을 이어받는다.
      var registered = Instance;
      if (registered != null && registered != this && registered._isRuntimeFallback)
        Destroy(registered.gameObject);

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
      DestroyGammaVolume();
      // 다른 인스턴스가 이미 등록을 넘겨받았으면 그 등록을 지우지 않는다.
      if (ReferenceEquals(Instance, this))
      {
        Registry.Registry.Unregister(
          RegistryType.Service,
          Registry.Registry.TypeKey<TexturePerformanceService>());
      }
    }

    private void Update() => PersistWindowSizeIfResized();

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
      _displayAppliedThisSession = true;
      EnsureWindowSizeApplied(_currentSettings);
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

      bool applyDisplay = ShouldApplyDisplayOnLoad(MppmLiteMode.IsActive, _displayAppliedThisSession);
      ApplyToUnity(_currentSettings, applyDisplay);
      if (applyDisplay)
      {
        _displayAppliedThisSession = true;
        EnsureWindowSizeApplied(_currentSettings);
      }
      Debug.Log($"[GraphicsPerformance] 설정 불러오기: {_currentSettings.Profile}" +
                (MppmLiteMode.IsActive ? " (MPPM Lite)" : string.Empty) +
                (applyDisplay ? string.Empty : " (디스플레이 설정은 유지)"));
    }

    /// <summary>
    /// 불러온 설정의 디스플레이 부분을 적용할지 결정합니다. MPPM Lite에서는 적용하지 않고,
    /// 같은 프로세스에서 이미 한 번 적용했으면 씬 전환으로 다시 깨어난 서비스가 되돌리지 않게 합니다.
    /// </summary>
    public static bool ShouldApplyDisplayOnLoad(bool mppmLiteActive, bool alreadyAppliedThisSession)
      => !mppmLiteActive && !alreadyAppliedThisSession;

    private int _observedWindowWidth;
    private int _observedWindowHeight;
    private float _windowSizeStableSince;
    private bool _windowSizePending;
    private const float WindowSizeSettleSeconds = 0.5f;

    /// <summary>
    /// 창 모드에서 사용자가 창 가장자리를 끌어 크기를 바꾸면, 크기가 잠시 안정된 뒤 그 값을 해상도 설정으로 저장합니다.
    /// 저장만 하고 다시 적용하지는 않으므로 창이 움직이지 않으며, 설정 화면과 다음 실행에 반영됩니다.
    /// </summary>
    private void PersistWindowSizeIfResized()
    {
      if (Application.isEditor || _currentSettings == null || _windowSizeRetry != null)
        return;

      int width = Screen.width;
      int height = Screen.height;
      if (width != _observedWindowWidth || height != _observedWindowHeight)
      {
        _observedWindowWidth = width;
        _observedWindowHeight = height;
        _windowSizeStableSince = Time.unscaledTime;
        _windowSizePending = true;
        return;
      }

      if (!_windowSizePending || Time.unscaledTime - _windowSizeStableSince < WindowSizeSettleSeconds)
        return;

      _windowSizePending = false;
      if (!ShouldPersistWindowSize(_currentSettings, Screen.fullScreenMode, width, height))
        return;

      _currentSettings.ResolutionWidth = width;
      _currentSettings.ResolutionHeight = height;
      Save(_currentSettings);
      OnSettingsChanged?.Invoke(CurrentSettings);
      Debug.Log($"[GraphicsPerformance] 창 크기 변경을 저장: {width}x{height}");
    }

    /// <summary>
    /// 관찰된 창 크기를 설정으로 저장할지 결정합니다. 창 모드로 저장되어 있고 실제로도 창 모드이며,
    /// 저장된 해상도와 다르고 허용 범위 안일 때만 저장합니다.
    /// </summary>
    public static bool ShouldPersistWindowSize(GraphicsSettingsData settings, FullScreenMode actualMode,
      int width, int height)
    {
      if (settings == null || settings.WindowMode != DisplayWindowMode.Windowed)
        return false;
      if (actualMode != FullScreenMode.Windowed)
        return false;
      if (width < 640 || height < 360 || width > 16384 || height > 8640)
        return false;
      return width != settings.ResolutionWidth || height != settings.ResolutionHeight;
    }

    private Coroutine _windowSizeRetry;

    /// <summary>
    /// 창 모드에서는 Screen.SetResolution 직후에 창 크기가 바뀌지 않는 경우가 있다.
    /// (전체 화면에서 창 모드로 바꾸는 프레임에 이전 창 크기가 복원되거나, 크기를 줄이는 요청이 그대로 무시된다.)
    /// 몇 프레임에 걸쳐 실제 크기를 확인하고, 아직 요청한 크기와 다르면 같은 요청을 다시 보낸다.
    /// 에디터의 Game 뷰는 SetResolution의 영향을 받지 않으므로 플레이어에서만 동작한다.
    /// </summary>
    private void EnsureWindowSizeApplied(GraphicsSettingsData settings)
    {
      if (Application.isEditor || settings == null || !isActiveAndEnabled)
        return;
      if (settings.WindowMode != DisplayWindowMode.Windowed)
        return;

      if (_windowSizeRetry != null)
        StopCoroutine(_windowSizeRetry);
      _windowSizeRetry = StartCoroutine(RetryWindowSize(settings.ResolutionWidth, settings.ResolutionHeight));
    }

    private System.Collections.IEnumerator RetryWindowSize(int width, int height)
    {
      const int maxAttempts = 3;
      for (int attempt = 0; attempt < maxAttempts; attempt++)
      {
        // 창 크기 변경은 다음 프레임에 반영되므로 한 프레임을 온전히 건너뛴 뒤 확인한다.
        yield return null;
        yield return null;

        if (Screen.fullScreenMode != FullScreenMode.Windowed)
          break;
        if (Screen.width == width && Screen.height == height)
          break;

        Debug.Log($"[GraphicsPerformance] 창 크기 재요청 ({attempt + 1}/{maxAttempts}): " +
                  $"{Screen.width}x{Screen.height} -> {width}x{height}");
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
      }
      _windowSizeRetry = null;
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
      ApplyGamma(settings);

      if (applyDisplay)
      {
        var refresh = settings.RefreshRate > 0
          ? new RefreshRate { numerator = (uint)settings.RefreshRate, denominator = 1 }
          : Screen.currentResolution.refreshRateRatio;
        // 저장된 화면 모드 의도를 실행 중인 플랫폼이 지원하는 FullScreenMode로 바꿔 적용한다.
        // (Windows만 전용 전체 화면을 지원하고, macOS와 Linux는 테두리 없는 창으로 대체된다.)
        Screen.SetResolution(
          settings.ResolutionWidth,
          settings.ResolutionHeight,
          DisplayWindowModes.ResolveFullScreenMode(settings.WindowMode, Application.platform),
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
          cameraData.renderPostProcessing = settings.PostProcessing || !Mathf.Approximately(settings.Gamma, 1f);
      }

      var mainCamera = MainCameraController.Instance?.Camera;
      if (mainCamera != null)
        mainCamera.fieldOfView = settings.FieldOfView;
    }

    private static void ApplyGamma(GraphicsSettingsData settings)
    {
      if (settings == null || MppmLiteMode.IsActive)
        return;

      EnsureGammaVolume();
      if (_liftGammaGain == null)
        return;

      _liftGammaGain.gamma.overrideState = true;
      _liftGammaGain.gamma.value = new Vector4(settings.Gamma, settings.Gamma, settings.Gamma, 0f);
    }

    private static void EnsureGammaVolume()
    {
      if (_liftGammaGain != null)
        return;

      var volumeObject = new GameObject("[Runtime Gamma Volume]");
      volumeObject.hideFlags = HideFlags.DontSave;
      DontDestroyOnLoad(volumeObject);
      _gammaVolume = volumeObject.AddComponent<Volume>();
      _gammaVolume.isGlobal = true;
      _gammaVolume.priority = GammaVolumePriority;
      _gammaVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
      _gammaVolume.profile.hideFlags = HideFlags.DontSave;
      _liftGammaGain = _gammaVolume.profile.Add<LiftGammaGain>(true);
    }

    private static void DestroyGammaVolume()
    {
      if (_gammaVolume == null)
        return;

      var volumeObject = _gammaVolume.gameObject;
      _gammaVolume = null;
      _liftGammaGain = null;
      if (Application.isPlaying)
        Destroy(volumeObject);
      else
        DestroyImmediate(volumeObject);
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
      destination.WindowMode = source.WindowMode;
      destination.FullScreenMode = source.FullScreenMode;
      destination.RefreshRate = source.RefreshRate;
      destination.FieldOfView = source.FieldOfView;
      destination.Gamma = source.Gamma;
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
