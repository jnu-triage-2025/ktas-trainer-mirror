using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using System;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>
  /// UI 배율(스케일) 사용자 설정을 관리하는 서비스입니다.
  ///
  /// 역할:
  ///   - <see cref="UIScale"/> 열거형 단계에 따라 <see cref="PanelSettings.scale"/> 배율을 적용합니다.
  ///   - <see cref="PlayerPrefs"/>를 통해 설정을 저장하고 불러옵니다.
  ///   - 설정 변경 시 <see cref="OnScaleChanged"/> 이벤트를 발행합니다.
  ///   - <see cref="RegistryType.Service"/>에 등록되어 설정 UI 등 외부에서 조회 가능합니다.
  ///
  /// 게임 내 모든 UIDocument가 하나의 공유 <see cref="PanelSettings"/>를 사용하므로,
  /// 이 에셋의 scale 계수를 조정하면 전체 UI 배율이 함께 변경됩니다.
  /// scaleMode를 바꾸지 않고 scale 계수만 곱하므로 기존 레이아웃 설계가 유지됩니다.
  ///
  /// 씬에 하나만 배치하세요(TexturePerformanceService와 동일한 시스템 오브젝트에 두는 것을 권장).
  /// <see cref="_panelSettings"/>를 인스펙터에서 지정하면 해당 에셋을 사용하고,
  /// 비어 있으면 활성 UIDocument에서 자동으로 찾습니다.
  /// </summary>
  public class UIScalePreferenceService : MonoBehaviour
  {
    // ──────────────────────────────────────────────────────────────────────────
    // 상수
    // ──────────────────────────────────────────────────────────────────────────
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.UIScale";
    private static readonly UIScale DefaultScale = UIScale.Level2;

    // ──────────────────────────────────────────────────────────────────────────
    // 인스펙터
    // ──────────────────────────────────────────────────────────────────────────
    [SerializeField]
    [Tooltip("배율을 적용할 공유 PanelSettings. 비워두면 활성 UIDocument에서 자동으로 찾습니다.")]
    private PanelSettings _panelSettings;

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>UI 배율이 변경되었을 때 새 배율 단계가 전달됩니다.</summary>
    public event Action<UIScale> OnScaleChanged;

    // ──────────────────────────────────────────────────────────────────────────
    // 상태
    // ──────────────────────────────────────────────────────────────────────────
    private UIScale _currentScale;

    /// <summary>현재 적용된 UI 배율 단계입니다.</summary>
    public UIScale CurrentScale => _currentScale;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 라이프사이클
    // ──────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
      Registry.Registry.Register(
        RegistryType.Service,
        Registry.Registry.TypeKey<UIScalePreferenceService>(),
        this
      );

      LoadAndApply();
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(
        RegistryType.Service,
        Registry.Registry.TypeKey<UIScalePreferenceService>()
      );
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// UI 배율을 설정하고 즉시 적용한 뒤 PlayerPrefs에 저장합니다.
    /// </summary>
    /// <param name="scale">적용할 배율 단계</param>
    public void SetScale(UIScale scale)
    {
      if (_currentScale == scale)
        return;

      _currentScale = scale;
      ApplyToPanel(scale);
      Save(scale);

      OnScaleChanged?.Invoke(scale);
      Debug.Log($"[UIScalePreferenceService] UI 배율 변경: {scale} (scale={ToScaleFactor(scale):0.00})");
    }

    /// <summary>
    /// 저장된 설정을 불러와 적용합니다.
    /// Awake에서 자동으로 호출됩니다.
    /// </summary>
    public void LoadAndApply()
    {
      var scale = Load();
      _currentScale = scale;
      ApplyToPanel(scale);
      Debug.Log($"[UIScalePreferenceService] UI 배율 불러오기: {scale}");
    }

    /// <summary>
    /// 배율 단계에 대응하는 실제 <see cref="PanelSettings.scale"/> 계수를 반환합니다.
    /// </summary>
    public static float ToScaleFactor(UIScale scale) => scale switch
    {
      UIScale.Level1 => 0.75f,
      UIScale.Level2 => 1.0f,
      UIScale.Level3 => 1.25f,
      UIScale.Level4 => 1.5f,
      _ => 1.0f,
    };

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ──────────────────────────────────────────────────────────────────────────

    private void ApplyToPanel(UIScale scale)
    {
      var panel = ResolvePanelSettings();
      if (panel == null)
      {
        Debug.LogWarning("[UIScalePreferenceService] 적용할 PanelSettings를 찾을 수 없습니다. UI 배율이 적용되지 않았습니다.");
        return;
      }

      panel.scale = ToScaleFactor(scale);
    }

    private PanelSettings ResolvePanelSettings()
    {
      if (_panelSettings != null)
        return _panelSettings;

      // 인스펙터 참조가 없으면 활성 UIDocument에서 공유 PanelSettings를 찾아 캐시한다.
      var doc = FindAnyObjectByType<UIDocument>();
      if (doc != null && doc.panelSettings != null)
      {
        _panelSettings = doc.panelSettings;
        return _panelSettings;
      }

      return null;
    }

    private static void Save(UIScale scale)
    {
      PlayerPrefs.SetInt(PlayerPrefsKey, (int)scale);
      PlayerPrefs.Save();
    }

    private static UIScale Load()
    {
      if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        return DefaultScale;

      var raw = PlayerPrefs.GetInt(PlayerPrefsKey, (int)DefaultScale);
      return Enum.IsDefined(typeof(UIScale), raw)
        ? (UIScale)raw
        : DefaultScale;
    }
  }
}
