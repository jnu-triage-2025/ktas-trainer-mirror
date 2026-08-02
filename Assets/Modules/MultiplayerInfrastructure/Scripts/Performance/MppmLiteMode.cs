using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>
  /// MPPM 추가 Editor 인스턴스를 사람의 조작 없이 저메모리 논리 클라이언트로 전환합니다.
  /// Main Editor와 일반 Player 빌드에는 절대로 적용하지 않습니다.
  /// </summary>
  public static class MppmLiteMode
  {
    public const string FullClientTag = "FullClient";
    public static bool IsActive { get; private set; }
    private static LiteRuntimeState _savedState;
    private static bool _hasSavedState;
    private static MppmLiteRuntimeGuard _guard;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      RestoreRuntimeState();
      IsActive = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_EDITOR
      IsActive = ShouldActivate(
        runningInEditor: true,
        isMainEditor: CurrentPlayer.IsMainEditor,
        tags: CurrentPlayer.ReadOnlyTags());
#else
      IsActive = false;
#endif

      if (!IsActive)
        return;

      CaptureRuntimeState();
      TexturePerformanceService.ApplyToUnity(CreateSettings(), applyDisplay: false);
      AudioListener.pause = true;
      AudioListener.volume = 0f;
      SceneManager.sceneLoaded += HandleSceneLoaded;
      EnsureRuntimeGuard();
      ApplySceneOverrides();
      Debug.Log("[MPPM Lite] 가상 플레이어 저메모리 모드를 자동 적용했습니다.");
    }

    internal static bool ShouldActivate(bool runningInEditor, bool isMainEditor, string[] tags)
    {
      if (!runningInEditor || isMainEditor)
        return false;
      return tags == null || Array.IndexOf(tags, FullClientTag) < 0;
    }

    public static GraphicsSettingsData CreateSettings()
    {
      var settings = GraphicsQualityPresets.Create(GraphicsQualityProfile.VeryLow);
      settings.Profile = GraphicsQualityProfile.VeryLow;
      settings.RenderScale = 0.5f;
      settings.Shadows = false;
      settings.PostProcessing = false;
      settings.DepthTexture = false;
      settings.OpaqueTexture = false;
      settings.Hdr = false;
      settings.TextureMipmapLimit = 3;
      settings.TextureStreaming = true;
      settings.TextureStreamingBudgetMb = 64;
      settings.AdditionalLights = GraphicsAdditionalLights.Disabled;
      settings.FrameRateLimit = 15;
      settings.VSync = false;
      return settings;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode _) => StripSceneVisuals(scene);

    internal static void ApplySceneOverrides()
    {
      if (!IsActive)
        return;

      for (int i = 0; i < SceneManager.sceneCount; i++)
        StripSceneVisuals(SceneManager.GetSceneAt(i), scheduleUnload: false);
      ScheduleUnusedAssetUnload();
    }

    /// <summary>동적으로 생성된 객체의 표현 계층을 Lite 클론에서 즉시 제거합니다.</summary>
    public static void StripVisuals(GameObject root)
    {
      if (!IsActive || root == null)
        return;

      foreach (var camera in root.GetComponentsInChildren<UnityEngine.Camera>(true))
        camera.enabled = false;

      DestroyComponents(root.GetComponentsInChildren<UIDocument>(true));
      DestroyComponents(root.GetComponentsInChildren<Renderer>(true));
      DestroyComponents(root.GetComponentsInChildren<MeshFilter>(true));
      DestroyComponents(root.GetComponentsInChildren<Light>(true));
      DestroyComponents(root.GetComponentsInChildren<ReflectionProbe>(true));
      DestroyComponents(root.GetComponentsInChildren<Volume>(true));

      foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
      {
        source.Stop();
        UnityEngine.Object.Destroy(source);
      }

      foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
      {
        particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        UnityEngine.Object.Destroy(particle);
      }

      ScheduleUnusedAssetUnload();
    }

    internal static void Deactivate()
    {
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      RestoreRuntimeState();
      IsActive = false;
      _guard = null;
    }

    private static void StripSceneVisuals(Scene scene, bool scheduleUnload = true)
    {
      if (!IsActive || !scene.IsValid() || !scene.isLoaded)
        return;

      foreach (var root in scene.GetRootGameObjects())
        StripVisuals(root);

      if (scheduleUnload)
        ScheduleUnusedAssetUnload();
    }

    private static void DestroyComponents<T>(T[] components) where T : Component
    {
      foreach (var component in components)
      {
        if (component != null)
          UnityEngine.Object.Destroy(component);
      }
    }

    private static void EnsureRuntimeGuard()
    {
      if (_guard != null)
        return;

      _guard = UnityEngine.Object.FindFirstObjectByType<MppmLiteRuntimeGuard>(FindObjectsInactive.Include);
      if (_guard != null)
        return;

      var guardObject = new GameObject("[MPPM Lite Runtime Guard]");
      guardObject.hideFlags = HideFlags.HideAndDontSave;
      UnityEngine.Object.DontDestroyOnLoad(guardObject);
      _guard = guardObject.AddComponent<MppmLiteRuntimeGuard>();
    }

    private static void ScheduleUnusedAssetUnload()
    {
      if (_guard != null)
        _guard.ScheduleUnusedAssetUnload();
    }

    private static void CaptureRuntimeState()
    {
      if (_hasSavedState)
        return;

      _savedState = LiteRuntimeState.Capture();
      _hasSavedState = true;
    }

    private static void RestoreRuntimeState()
    {
      if (!_hasSavedState)
        return;

      _savedState.Restore();
      GraphicsPipelineRuntimeAsset.Release(_savedState.RenderPipeline);
      _hasSavedState = false;
    }

    private struct LiteRuntimeState
    {
      public bool AudioPaused;
      public float AudioVolume;
      public int VSyncCount;
      public int TargetFrameRate;
      public int TextureMipmapLimit;
      public AnisotropicFiltering AnisotropicFiltering;
      public bool StreamingMipmapsActive;
      public float StreamingMipmapsMemoryBudget;
      public int AntiAliasing;
      public UnityEngine.ShadowQuality Shadows;
      public UnityEngine.ShadowResolution ShadowResolution;
      public float ShadowDistance;
      public int ShadowCascades;
      public float LodBias;
      public int MaximumLodLevel;
      public int PixelLightCount;
      public bool RealtimeReflectionProbes;
      public bool SoftParticles;
      public RenderPipelineAsset RenderPipeline;

      public static LiteRuntimeState Capture() => new()
      {
        AudioPaused = AudioListener.pause,
        AudioVolume = AudioListener.volume,
        VSyncCount = QualitySettings.vSyncCount,
        TargetFrameRate = Application.targetFrameRate,
        TextureMipmapLimit = QualitySettings.globalTextureMipmapLimit,
        AnisotropicFiltering = QualitySettings.anisotropicFiltering,
        StreamingMipmapsActive = QualitySettings.streamingMipmapsActive,
        StreamingMipmapsMemoryBudget = QualitySettings.streamingMipmapsMemoryBudget,
        AntiAliasing = QualitySettings.antiAliasing,
        Shadows = QualitySettings.shadows,
        ShadowResolution = QualitySettings.shadowResolution,
        ShadowDistance = QualitySettings.shadowDistance,
        ShadowCascades = QualitySettings.shadowCascades,
        LodBias = QualitySettings.lodBias,
        MaximumLodLevel = QualitySettings.maximumLODLevel,
        PixelLightCount = QualitySettings.pixelLightCount,
        RealtimeReflectionProbes = QualitySettings.realtimeReflectionProbes,
        SoftParticles = QualitySettings.softParticles,
        RenderPipeline = QualitySettings.renderPipeline,
      };

      public void Restore()
      {
        AudioListener.pause = AudioPaused;
        AudioListener.volume = AudioVolume;
        QualitySettings.vSyncCount = VSyncCount;
        Application.targetFrameRate = TargetFrameRate;
        QualitySettings.globalTextureMipmapLimit = TextureMipmapLimit;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering;
        QualitySettings.streamingMipmapsActive = StreamingMipmapsActive;
        QualitySettings.streamingMipmapsMemoryBudget = StreamingMipmapsMemoryBudget;
        QualitySettings.antiAliasing = AntiAliasing;
        QualitySettings.shadows = Shadows;
        QualitySettings.shadowResolution = ShadowResolution;
        QualitySettings.shadowDistance = ShadowDistance;
        QualitySettings.shadowCascades = ShadowCascades;
        QualitySettings.lodBias = LodBias;
        QualitySettings.maximumLODLevel = MaximumLodLevel;
        QualitySettings.pixelLightCount = PixelLightCount;
        QualitySettings.realtimeReflectionProbes = RealtimeReflectionProbes;
        QualitySettings.softParticles = SoftParticles;
      }
    }
  }

  /// <summary>표현 컴포넌트 제거 뒤 미사용 에셋을 한 번만 묶어서 언로드합니다.</summary>
  internal sealed class MppmLiteRuntimeGuard : MonoBehaviour
  {
    private Coroutine _unloadCoroutine;

    public void ScheduleUnusedAssetUnload()
    {
      if (_unloadCoroutine == null)
        _unloadCoroutine = StartCoroutine(UnloadUnusedAssetsNextFrame());
    }

    private IEnumerator UnloadUnusedAssetsNextFrame()
    {
      yield return null;
      yield return Resources.UnloadUnusedAssets();
      _unloadCoroutine = null;
    }

    private void OnDestroy() => MppmLiteMode.Deactivate();
  }
}
