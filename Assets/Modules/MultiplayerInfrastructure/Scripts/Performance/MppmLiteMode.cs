using System;
using System.Collections.Generic;
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
    private static readonly Dictionary<int, BehaviourState> DisabledBehaviours = new();
    private static readonly Dictionary<int, RendererState> DisabledRenderers = new();
    private static readonly Dictionary<int, AudioSourceState> DisabledAudioSources = new();
    private static readonly Dictionary<int, ParticleSystemState> StoppedParticleSystems = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      IsActive = false;
      RestoreVisualStates();
      RestoreRuntimeState();
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
        StripSceneVisuals(SceneManager.GetSceneAt(i));
    }

    /// <summary>
    /// 동적으로 생성된 객체의 표현 컴포넌트를 Lite 클론에서 비활성화합니다.
    /// 원래 상태는 기록되며 Lite 해제 시 복원됩니다.
    /// </summary>
    public static void StripVisuals(GameObject root)
    {
      if (!IsActive || root == null)
        return;

      DisableBehaviours(root.GetComponentsInChildren<UnityEngine.Camera>(true));
      DisableBehaviours(root.GetComponentsInChildren<UIDocument>(true));
      DisableRenderers(root.GetComponentsInChildren<Renderer>(true));
      DisableBehaviours(root.GetComponentsInChildren<Light>(true));
      DisableBehaviours(root.GetComponentsInChildren<ReflectionProbe>(true));
      DisableBehaviours(root.GetComponentsInChildren<Volume>(true));

      foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
      {
        int id = source.GetInstanceID();
        if (!DisabledAudioSources.ContainsKey(id))
          DisabledAudioSources.Add(id, new AudioSourceState(source, source.enabled, source.isPlaying));
        source.Stop();
        source.enabled = false;
      }

      foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
      {
        int id = particle.GetInstanceID();
        if (!StoppedParticleSystems.ContainsKey(id))
          StoppedParticleSystems.Add(id, new ParticleSystemState(particle, particle.isPlaying));
        particle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
      }
    }

    internal static void Deactivate()
    {
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      IsActive = false;
      RestoreVisualStates();
      RestoreRuntimeState();
      _guard = null;
    }

    private static void StripSceneVisuals(Scene scene)
    {
      if (!IsActive || !scene.IsValid() || !scene.isLoaded)
        return;

      foreach (var root in scene.GetRootGameObjects())
        StripVisuals(root);
    }

    private static void DisableBehaviours<T>(T[] components) where T : Behaviour
    {
      foreach (var component in components)
      {
        if (component == null)
          continue;

        int id = component.GetInstanceID();
        if (!DisabledBehaviours.ContainsKey(id))
          DisabledBehaviours.Add(id, new BehaviourState(component, component.enabled));
        component.enabled = false;
      }
    }

    private static void DisableRenderers(Renderer[] renderers)
    {
      foreach (var renderer in renderers)
      {
        if (renderer == null)
          continue;

        int id = renderer.GetInstanceID();
        if (!DisabledRenderers.ContainsKey(id))
          DisabledRenderers.Add(id, new RendererState(renderer, renderer.enabled));
        renderer.enabled = false;
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

    private static void RestoreVisualStates()
    {
      foreach (var state in DisabledBehaviours.Values)
      {
        if (state.Component != null)
          state.Component.enabled = state.Enabled;
      }
      foreach (var state in DisabledRenderers.Values)
      {
        if (state.Renderer != null)
          state.Renderer.enabled = state.Enabled;
      }
      foreach (var state in DisabledAudioSources.Values)
      {
        if (state.Source == null)
          continue;
        state.Source.enabled = state.Enabled;
        if (state.WasPlaying && state.Enabled)
          state.Source.Play();
      }
      foreach (var state in StoppedParticleSystems.Values)
      {
        if (state.System != null && state.WasPlaying)
          state.System.Play(withChildren: true);
      }

      DisabledBehaviours.Clear();
      DisabledRenderers.Clear();
      DisabledAudioSources.Clear();
      StoppedParticleSystems.Clear();
    }

#if UNITY_INCLUDE_TESTS
    internal static void ActivateForTests()
    {
      ResetState();
      IsActive = true;
      CaptureRuntimeState();
    }

    internal static void DeactivateForTests() => Deactivate();
#endif

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

    private readonly struct BehaviourState
    {
      public readonly Behaviour Component;
      public readonly bool Enabled;
      public BehaviourState(Behaviour component, bool enabled)
      {
        Component = component;
        Enabled = enabled;
      }
    }

    private readonly struct RendererState
    {
      public readonly Renderer Renderer;
      public readonly bool Enabled;
      public RendererState(Renderer renderer, bool enabled)
      {
        Renderer = renderer;
        Enabled = enabled;
      }
    }

    private readonly struct AudioSourceState
    {
      public readonly AudioSource Source;
      public readonly bool Enabled;
      public readonly bool WasPlaying;
      public AudioSourceState(AudioSource source, bool enabled, bool wasPlaying)
      {
        Source = source;
        Enabled = enabled;
        WasPlaying = wasPlaying;
      }
    }

    private readonly struct ParticleSystemState
    {
      public readonly ParticleSystem System;
      public readonly bool WasPlaying;
      public ParticleSystemState(ParticleSystem system, bool wasPlaying)
      {
        System = system;
        WasPlaying = wasPlaying;
      }
    }
  }

  /// <summary>Lite 프로세스 종료 시 전역 및 표현 상태를 복원합니다.</summary>
  internal sealed class MppmLiteRuntimeGuard : MonoBehaviour
  {
    private void OnDestroy() => MppmLiteMode.Deactivate();
  }
}
