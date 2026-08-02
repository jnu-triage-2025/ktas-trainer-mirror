using System;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>
  /// 텍스처 성능 품질을 관리하는 서비스입니다.
  ///
  /// 역할:
  ///   - <see cref="TextureQuality"/> 열거형에 따라 <see cref="QualitySettings.masterTextureLimit"/>을 적용합니다.
  ///   - <see cref="PlayerPrefs"/>를 통해 설정을 저장하고 불러옵니다.
  ///   - 설정 변경 시 <see cref="OnQualityChanged"/> 이벤트를 발행합니다.
  ///   - Registry.Entity에 등록되어 외부에서 조회 가능합니다.
  ///
  /// 씬에 하나만 배치하세요.
  /// </summary>
  public class TexturePerformanceService : MonoBehaviour
  {
    // ──────────────────────────────────────────────────────────────────────────
    // 상수
    // ──────────────────────────────────────────────────────────────────────────
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.TextureQuality";
    private static readonly TextureQuality DefaultQuality = TextureQuality.High;

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>텍스처 품질이 변경되었을 때 새 품질 값이 전달됩니다.</summary>
    public event Action<TextureQuality> OnQualityChanged;

    // ──────────────────────────────────────────────────────────────────────────
    // 상태
    // ──────────────────────────────────────────────────────────────────────────
    private TextureQuality _currentQuality;

    /// <summary>현재 적용된 텍스처 품질 단계입니다.</summary>
    public TextureQuality CurrentQuality => _currentQuality;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 라이프사이클
    // ──────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
      Registry.Registry.Register(
        RegistryType.Service,
        Registry.Registry.TypeKey<TexturePerformanceService>(),
        this
      );

      LoadAndApply();
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(
        RegistryType.Service,
        Registry.Registry.TypeKey<TexturePerformanceService>()
      );
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 텍스처 품질을 설정하고 즉시 적용한 뒤 PlayerPrefs에 저장합니다.
    /// </summary>
    /// <param name="quality">적용할 품질 단계</param>
    public void SetQuality(TextureQuality quality)
    {
      if (_currentQuality == quality) return;

      _currentQuality = quality;
      ApplyToUnity(quality);
      Save(quality);

      OnQualityChanged?.Invoke(quality);
      Debug.Log($"[TexturePerformanceService] 텍스처 품질 변경: {quality} (masterTextureLimit={QualitySettings.globalTextureMipmapLimit})");
    }

    /// <summary>
    /// 저장된 설정을 불러와 적용합니다.
    /// Awake에서 자동으로 호출됩니다.
    /// </summary>
    public void LoadAndApply()
    {
      var quality = Load();
      _currentQuality = quality;
      ApplyToUnity(quality);
      Debug.Log($"[TexturePerformanceService] 텍스처 품질 불러오기: {quality}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ──────────────────────────────────────────────────────────────────────────

    private static void ApplyToUnity(TextureQuality quality)
    {
      // TextureQuality 열거형 값이 masterTextureLimit과 1:1 대응합니다.
      QualitySettings.globalTextureMipmapLimit = (int)quality;

      // Unity 6000.2 macOS Editor의 TextureStreamingManager가 씬 로드 직후
      // Material shader compilation을 동기 실행하면 native modal progress
      // backend에서 MPPM virtual player가 crash할 수 있다. Editor에서만
      // streaming을 끄고 실제 Player 빌드에서는 기존 동작을 유지한다.
#if UNITY_EDITOR_OSX
      QualitySettings.streamingMipmapsActive = false;
#else
      QualitySettings.streamingMipmapsActive = true;
#endif
    }

    private static void Save(TextureQuality quality)
    {
      PlayerPrefs.SetInt(PlayerPrefsKey, (int)quality);
      PlayerPrefs.Save();
    }

    private static TextureQuality Load()
    {
      if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        return DefaultQuality;

      var raw = PlayerPrefs.GetInt(PlayerPrefsKey, (int)DefaultQuality);
      return Enum.IsDefined(typeof(TextureQuality), raw)
        ? (TextureQuality)raw
        : DefaultQuality;
    }
  }
}
