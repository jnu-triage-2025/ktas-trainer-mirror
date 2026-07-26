using System.Collections.Generic;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// SettingsUIController의 "그래픽 설정" 탭 구현.
  /// 기존 GraphicsSettingsUIController의 텍스처 품질 로직을 재사용하고,
  /// 추가로 3인칭 카메라 POV(거리) 조정 슬라이더를 제공합니다.
  /// </summary>
  public partial class SettingsUIController
  {
    private VisualElement _graphicsOptionsContainer;
    private readonly List<TextureQualityOptionElement> _textureOptionElements = new();
    private TextureQuality _pendingQuality;
    private bool _hasPendingQualityChange;

    private VisualElement _uiScaleOptionsContainer;
    private readonly List<UIScaleOptionElement> _uiScaleOptionElements = new();

    private Slider _povSlider;
    private Label _povValueLabel;
    private bool _povSliderInitializing;

    // ──────────────────────────────────────────────────────────────────────────
    // 탭 컨텐츠 생성
    // ──────────────────────────────────────────────────────────────────────────
    private VisualElement EnsureGraphicsTabContent()
    {
      if (_graphicsTabContent != null)
        return _graphicsTabContent;

      var content = new OverflowScrollView();
      content.AddToClassList("settings__graphics-tab-wrapper");

      // ── 텍스처 품질 섹션 ─────────────────────────────────────
      var textureSection = new VisualElement();
      textureSection.AddToClassList("settings__section");

      var textureTitle = new Label("텍스처 품질");
      textureTitle.AddToClassList("settings__section-title");
      textureSection.Add(textureTitle);

      var textureDesc = new Label("게임 텍스처의 해상도 수준을 조절합니다. 낮을수록 성능이 향상됩니다.");
      textureDesc.AddToClassList("settings__section-desc");
      textureSection.Add(textureDesc);

      _graphicsOptionsContainer = new VisualElement();
      _graphicsOptionsContainer.AddToClassList("settings__graphics-options");
      textureSection.Add(_graphicsOptionsContainer);

      content.Content.Add(textureSection);

      // ── UI 배율 섹션 ────────────────────────────────────────
      var uiScaleSection = new VisualElement();
      uiScaleSection.AddToClassList("settings__section");
      uiScaleSection.AddToClassList("settings__section--spaced");

      var uiScaleTitle = new Label("UI 배율");
      uiScaleTitle.AddToClassList("settings__section-title");
      uiScaleSection.Add(uiScaleTitle);

      var uiScaleDesc = new Label("화면에 표시되는 UI 요소의 크기를 조절합니다. 단계가 높을수록 UI가 크게 표시됩니다.");
      uiScaleDesc.AddToClassList("settings__section-desc");
      uiScaleSection.Add(uiScaleDesc);

      _uiScaleOptionsContainer = new VisualElement();
      _uiScaleOptionsContainer.AddToClassList("settings__graphics-options");
      uiScaleSection.Add(_uiScaleOptionsContainer);

      content.Content.Add(uiScaleSection);

      // ── 카메라 시점(POV) 섹션 ────────────────────────────────
      var povSection = new VisualElement();
      povSection.AddToClassList("settings__section");
      povSection.AddToClassList("settings__section--spaced");

      var povTitle = new Label("카메라 시점(POV)");
      povTitle.AddToClassList("settings__section-title");
      povSection.Add(povTitle);

      var povDesc = new Label("3인칭 카메라와 캐릭터 사이의 거리를 조절합니다. (게임 중 Alt + 마우스 휠로도 조정 가능)");
      povDesc.AddToClassList("settings__section-desc");
      povSection.Add(povDesc);

      var povRow = new VisualElement();
      povRow.AddToClassList("settings__pov-row");

      GetPovRange(out float min, out float max);
      _povSlider = new Slider(min, max) { showInputField = false };
      _povSlider.AddToClassList("settings__pov-slider");
      _povSlider.RegisterValueChangedCallback(OnPovSliderChanged);
      povRow.Add(_povSlider);

      _povValueLabel = new Label();
      _povValueLabel.AddToClassList("settings__pov-value");
      povRow.Add(_povValueLabel);

      povSection.Add(povRow);
      content.Content.Add(povSection);

      // 텍스처 품질 옵션 채우기
      PopulateTextureOptions();

      // UI 배율 옵션 채우기
      PopulateUIScaleOptions();

      _graphicsTabContent = content;
      return _graphicsTabContent;
    }

    private void RefreshGraphicsTab()
    {
      if (_graphicsTabContent == null)
        return;

      RefreshTextureFromService();
      RefreshUIScaleFromService();
      RefreshPovFromService();
    }

    private void DetachGraphicsTab()
    {
      foreach (var el in _textureOptionElements)
        el.OnOptionSelected -= HandleTextureOptionSelected;
      _textureOptionElements.Clear();

      foreach (var el in _uiScaleOptionElements)
        el.OnOptionSelected -= HandleUIScaleOptionSelected;
      _uiScaleOptionElements.Clear();

      if (_povSlider != null)
        _povSlider.UnregisterValueChangedCallback(OnPovSliderChanged);

      _graphicsOptionsContainer = null;
      _uiScaleOptionsContainer = null;
      _povSlider = null;
      _povValueLabel = null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 텍스처 품질
    // ──────────────────────────────────────────────────────────────────────────
    private void PopulateTextureOptions()
    {
      if (_graphicsOptionsContainer == null)
        return;

      _graphicsOptionsContainer.Clear();
      _textureOptionElements.Clear();

      var qualities = new[]
      {
        TextureQuality.Ultra,
        TextureQuality.High,
        TextureQuality.Medium,
        TextureQuality.Low,
      };

      foreach (var quality in qualities)
      {
        var element = new TextureQualityOptionElement();
        element.Bind(quality);
        element.OnOptionSelected += HandleTextureOptionSelected;
        _textureOptionElements.Add(element);
        _graphicsOptionsContainer.Add(element);
      }

      RefreshTextureFromService();
    }

    private void HandleTextureOptionSelected(TextureQuality quality)
    {
      _pendingQuality = quality;
      _hasPendingQualityChange = true;

      foreach (var el in _textureOptionElements)
        el.SetActive(el.BoundQuality == quality);

      // 텍스처 품질은 선택 즉시 적용/저장한다(별도 적용 버튼 없이 즉시 반영).
      var service = GetTextureService();
      if (service != null)
      {
        service.SetQuality(quality);
        _hasPendingQualityChange = false;
        SetStatusText($"텍스처 품질을 '{QualityLabel(quality)}'(으)로 변경했습니다.");
      }
      else
      {
        SetStatusText("오류: 텍스처 성능 서비스를 찾을 수 없습니다.");
      }
    }

    private void RefreshTextureFromService()
    {
      var service = GetTextureService();
      var current = service != null ? service.CurrentQuality : TextureQuality.High;

      _pendingQuality = current;
      _hasPendingQualityChange = false;

      foreach (var el in _textureOptionElements)
        el.SetActive(el.BoundQuality == current);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UI 배율
    // ──────────────────────────────────────────────────────────────────────────
    private void PopulateUIScaleOptions()
    {
      if (_uiScaleOptionsContainer == null)
        return;

      _uiScaleOptionsContainer.Clear();
      _uiScaleOptionElements.Clear();

      var scales = new[]
      {
        UIScale.Level1,
        UIScale.Level2,
        UIScale.Level3,
        UIScale.Level4,
      };

      foreach (var scale in scales)
      {
        var element = new UIScaleOptionElement();
        element.Bind(scale);
        element.OnOptionSelected += HandleUIScaleOptionSelected;
        _uiScaleOptionElements.Add(element);
        _uiScaleOptionsContainer.Add(element);
      }

      RefreshUIScaleFromService();
    }

    private void HandleUIScaleOptionSelected(UIScale scale)
    {
      foreach (var el in _uiScaleOptionElements)
        el.SetActive(el.BoundScale == scale);

      // UI 배율은 선택 즉시 적용/저장한다(별도 적용 버튼 없이 즉시 반영).
      var service = GetUIScaleService();
      if (service != null)
      {
        service.SetScale(scale);
        SetStatusText($"UI 배율을 '{UIScaleLabel(scale)}'(으)로 변경했습니다.");
      }
      else
      {
        SetStatusText("오류: UI 배율 서비스를 찾을 수 없습니다.");
      }
    }

    private void RefreshUIScaleFromService()
    {
      var service = GetUIScaleService();
      var current = service != null ? service.CurrentScale : UIScale.Level2;

      foreach (var el in _uiScaleOptionElements)
        el.SetActive(el.BoundScale == current);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 카메라 POV(거리)
    // ──────────────────────────────────────────────────────────────────────────
    private void OnPovSliderChanged(ChangeEvent<float> evt)
    {
      if (_povSliderInitializing)
        return;

      UpdatePovValueLabel(evt.newValue);

      var service = GetCameraDistanceService();
      if (service != null)
      {
        service.SetDistance(evt.newValue);
        SetStatusText($"카메라 거리를 {evt.newValue:0.0}(으)로 설정했습니다.");
      }
      else
      {
        // 서비스가 없으면 카메라에 직접 적용(저장은 되지 않음).
        var cam = ResolveCamera();
        if (cam != null)
          cam.SetThirdPersonDistance(evt.newValue);
      }
    }

    private void RefreshPovFromService()
    {
      if (_povSlider == null)
        return;

      GetPovRange(out float min, out float max);

      // 초기화 중 발행되는 ChangeEvent가 서비스 저장을 트리거하지 않도록 플래그로 가드한다.
      // 예외가 나더라도 플래그가 true로 고착되지 않도록 finally에서 반드시 해제한다.
      _povSliderInitializing = true;
      try
      {
        _povSlider.lowValue = min;
        _povSlider.highValue = max;

        float current = ResolveCurrentPov(min, max);
        _povSlider.value = Mathf.Clamp(current, min, max);
      }
      finally
      {
        _povSliderInitializing = false;
      }

      UpdatePovValueLabel(_povSlider.value);
    }

    private float ResolveCurrentPov(float min, float max)
    {
      var service = GetCameraDistanceService();
      if (service != null)
        return service.ResolveEffectiveDistance();

      var cam = ResolveCamera();
      if (cam != null)
        return cam.DesiredThirdPersonDistance;

      return (min + max) * 0.5f;
    }

    private void GetPovRange(out float min, out float max)
    {
      var service = GetCameraDistanceService();
      if (service != null)
      {
        service.GetDistanceRange(out min, out max);
        return;
      }

      var cam = ResolveCamera();
      if (cam != null)
      {
        min = cam.MinThirdPersonDistance;
        max = cam.MaxThirdPersonDistance;
        return;
      }

      // CameraHolder 인스펙터 기본값과 동일한 폴백.
      min = 1.0f;
      max = 8.0f;
    }

    private static MainCameraController ResolveCamera()
    {
      if (MainCameraController.Instance != null)
        return MainCameraController.Instance;

      return Registry.Registry.Get<MainCameraController>(
        RegistryType.Service, Registry.Registry.TypeKey<MainCameraController>());
    }

    private void UpdatePovValueLabel(float value)
    {
      if (_povValueLabel != null)
        _povValueLabel.text = $"{value:0.0} m";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 서비스 조회
    // ──────────────────────────────────────────────────────────────────────────
    private static TexturePerformanceService GetTextureService()
      => Registry.Registry.Get<TexturePerformanceService>(
           RegistryType.Service, Registry.Registry.TypeKey<TexturePerformanceService>());

    private static CameraDistancePreferenceService GetCameraDistanceService()
      => Registry.Registry.Get<CameraDistancePreferenceService>(
           RegistryType.Service, Registry.Registry.TypeKey<CameraDistancePreferenceService>());

    private static UIScalePreferenceService GetUIScaleService()
      => Registry.Registry.Get<UIScalePreferenceService>(
           RegistryType.Service, Registry.Registry.TypeKey<UIScalePreferenceService>());

    private static string QualityLabel(TextureQuality quality) => quality switch
    {
      TextureQuality.Ultra  => "매우 높음",
      TextureQuality.High   => "높음",
      TextureQuality.Medium => "보통",
      TextureQuality.Low    => "낮음",
      _                     => quality.ToString(),
    };

    private static string UIScaleLabel(UIScale scale) => scale switch
    {
      UIScale.Level1 => "1단계",
      UIScale.Level2 => "2단계",
      UIScale.Level3 => "3단계",
      UIScale.Level4 => "4단계",
      _              => scale.ToString(),
    };
  }
}
