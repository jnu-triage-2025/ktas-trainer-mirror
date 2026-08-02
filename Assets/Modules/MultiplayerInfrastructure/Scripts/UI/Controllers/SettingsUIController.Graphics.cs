using System;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>통합 설정 창의 그래픽 프로파일 및 세부 옵션 탭입니다.</summary>
  public partial class SettingsUIController
  {
    private OverflowScrollView _graphicsScroll;
    private GraphicsSettingsData _pendingGraphics;
    private DropdownField _profileField;
    private bool _graphicsFormInitializing;

    private VisualElement _uiScaleOptionsContainer;
    private readonly System.Collections.Generic.List<UIScaleOptionElement> _uiScaleOptionElements = new();
    private Slider _povSlider;
    private Label _povValueLabel;
    private bool _povSliderInitializing;

    private VisualElement EnsureGraphicsTabContent()
    {
      if (_graphicsTabContent != null)
        return _graphicsTabContent;

      _graphicsScroll = new OverflowScrollView();
      _graphicsScroll.AddToClassList("settings__graphics-tab-wrapper");
      _graphicsTabContent = _graphicsScroll;
      RefreshGraphicsTab();
      return _graphicsTabContent;
    }

    private void RefreshGraphicsTab()
    {
      if (_graphicsScroll == null)
        return;

      var service = GetGraphicsService();
      _pendingGraphics = service?.CurrentSettings ??
                         GraphicsQualityPresets.Create(GraphicsQualityPresets.DefaultProfile);
      BuildGraphicsForm();
    }

    private void DetachGraphicsTab()
    {
      foreach (var element in _uiScaleOptionElements)
        element.OnOptionSelected -= HandleUIScaleOptionSelected;
      _uiScaleOptionElements.Clear();

      if (_povSlider != null)
        _povSlider.UnregisterValueChangedCallback(OnPovSliderChanged);

      _graphicsScroll = null;
      _profileField = null;
      _uiScaleOptionsContainer = null;
      _povSlider = null;
      _povValueLabel = null;
    }

    private void BuildGraphicsForm()
    {
      if (_graphicsScroll == null || _pendingGraphics == null)
        return;

      _graphicsFormInitializing = true;
      try
      {
        _graphicsScroll.Content.Clear();

        BuildProfileSection();
        BuildDisplaySection();
        BuildRenderingSection();
        BuildTextureSection();
        BuildLightingSection();
        BuildDetailSection();
        BuildAccessibilitySection();
        BuildGraphicsActions();
      }
      finally
      {
        _graphicsFormInitializing = false;
      }
    }

    private void BuildProfileSection()
    {
      var section = AddSection("품질 프로파일", "매우 낮음부터 매우 높음까지 전체 옵션 묶음을 선택합니다. 기본값은 항상 낮음입니다.");
      _profileField = new DropdownField(
        new System.Collections.Generic.List<string>
        {
          "매우 낮음", "낮음", "보통", "높음", "매우 높음", "사용자 지정",
        },
        ProfileLabel(_pendingGraphics.Profile));
      _profileField.RegisterValueChangedCallback(evt =>
      {
        if (_graphicsFormInitializing)
          return;

        var selected = ProfileFromLabel(evt.newValue);
        if (selected == GraphicsQualityProfile.Custom)
        {
          _profileField.SetValueWithoutNotify(ProfileLabel(_pendingGraphics.Profile));
          return;
        }

        var display = _pendingGraphics;
        _pendingGraphics = GraphicsQualityPresets.Create(selected);
        CopyDisplaySettings(display, _pendingGraphics);
        BuildGraphicsForm();
        SetStatusText($"'{ProfileLabel(selected)}' 프로파일을 불러왔습니다. 적용 버튼을 눌러 저장하세요.");
      });
      AddRow(section, "프리셋", _profileField);
    }

    private void BuildDisplaySection()
    {
      var section = AddSection("디스플레이", "해상도, 화면 모드, 동기화 및 프레임 제한을 설정합니다.");
      AddInt(section, "가로 해상도", _pendingGraphics.ResolutionWidth, 640, 16384,
        value => _pendingGraphics.ResolutionWidth = value, affectsProfile: false);
      AddInt(section, "세로 해상도", _pendingGraphics.ResolutionHeight, 360, 8640,
        value => _pendingGraphics.ResolutionHeight = value, affectsProfile: false);
      AddEnum(section, "화면 모드", _pendingGraphics.FullScreenMode,
        value => _pendingGraphics.FullScreenMode = (FullScreenMode)value, affectsProfile: false);
      AddInt(section, "주사율 (0=자동)", _pendingGraphics.RefreshRate, 0, 1000,
        value => _pendingGraphics.RefreshRate = value, affectsProfile: false);
      AddToggle(section, "수직 동기화", _pendingGraphics.VSync,
        value => _pendingGraphics.VSync = value);
      AddInt(section, "프레임 제한 (0=무제한)", _pendingGraphics.FrameRateLimit, 0, 1000,
        value => _pendingGraphics.FrameRateLimit = value);
    }

    private void BuildRenderingSection()
    {
      var section = AddSection("렌더링", "내부 해상도와 URP 카메라 버퍼 및 후처리를 설정합니다.");
      AddFloat(section, "렌더 스케일", _pendingGraphics.RenderScale, 0.5f, 2f,
        value => _pendingGraphics.RenderScale = value);
      AddEnum(section, "안티앨리어싱", _pendingGraphics.AntiAliasing,
        value => _pendingGraphics.AntiAliasing = (GraphicsAntiAliasing)value);
      AddToggle(section, "HDR", _pendingGraphics.Hdr, value => _pendingGraphics.Hdr = value);
      AddToggle(section, "Depth Texture", _pendingGraphics.DepthTexture, value => _pendingGraphics.DepthTexture = value);
      AddToggle(section, "Opaque Texture", _pendingGraphics.OpaqueTexture, value => _pendingGraphics.OpaqueTexture = value);
      AddToggle(section, "포스트 프로세싱", _pendingGraphics.PostProcessing, value => _pendingGraphics.PostProcessing = value);
      AddToggle(section, "동적 해상도", _pendingGraphics.DynamicResolution, value => _pendingGraphics.DynamicResolution = value);
      AddFloat(section, "카메라 시야각 (FOV)", _pendingGraphics.FieldOfView, 40f, 100f,
        value => _pendingGraphics.FieldOfView = value, affectsProfile: false);
    }

    private void BuildTextureSection()
    {
      var section = AddSection("텍스처", "텍스처 해상도, 비등방성 필터링과 스트리밍 메모리 예산을 설정합니다.");
      AddInt(section, "Mip 제한 (0=원본, 3=1/8)", _pendingGraphics.TextureMipmapLimit, 0, 3,
        value => _pendingGraphics.TextureMipmapLimit = value);
      AddEnum(section, "비등방성 필터링", _pendingGraphics.AnisotropicFiltering,
        value => _pendingGraphics.AnisotropicFiltering = (AnisotropicFiltering)value);
      AddToggle(section, "Texture Streaming", _pendingGraphics.TextureStreaming,
        value => _pendingGraphics.TextureStreaming = value);
      AddInt(section, "Streaming 예산 (MB)", _pendingGraphics.TextureStreamingBudgetMb, 64, 2048,
        value => _pendingGraphics.TextureStreamingBudgetMb = value);
    }

    private void BuildLightingSection()
    {
      var section = AddSection("조명과 그림자", "실시간 조명, 그림자 품질과 추가 광원 비용을 설정합니다.");
      AddToggle(section, "그림자", _pendingGraphics.Shadows, value => _pendingGraphics.Shadows = value);
      AddEnum(section, "그림자 해상도", _pendingGraphics.ShadowResolution,
        value => _pendingGraphics.ShadowResolution = (GraphicsShadowResolution)value);
      AddFloat(section, "그림자 거리", _pendingGraphics.ShadowDistance, 0f, 200f,
        value => _pendingGraphics.ShadowDistance = value);
      AddInt(section, "그림자 Cascade", _pendingGraphics.ShadowCascades, 1, 4,
        value => _pendingGraphics.ShadowCascades = value);
      AddToggle(section, "부드러운 그림자", _pendingGraphics.SoftShadows,
        value => _pendingGraphics.SoftShadows = value);
      AddEnum(section, "추가 광원", _pendingGraphics.AdditionalLights,
        value => _pendingGraphics.AdditionalLights = (GraphicsAdditionalLights)value);
      AddInt(section, "오브젝트당 추가 광원", _pendingGraphics.AdditionalLightsPerObject, 0, 8,
        value => _pendingGraphics.AdditionalLightsPerObject = value);
      AddToggle(section, "추가 광원 그림자", _pendingGraphics.AdditionalLightShadows,
        value => _pendingGraphics.AdditionalLightShadows = value);
    }

    private void BuildDetailSection()
    {
      var section = AddSection("디테일과 효과", "LOD, 픽셀 광원, 반사 및 파티클 효과를 설정합니다.");
      AddFloat(section, "LOD Bias", _pendingGraphics.LodBias, 0.25f, 4f,
        value => _pendingGraphics.LodBias = value);
      AddInt(section, "최대 LOD 레벨", _pendingGraphics.MaximumLodLevel, 0, 3,
        value => _pendingGraphics.MaximumLodLevel = value);
      AddInt(section, "Pixel Light 수", _pendingGraphics.PixelLightCount, 0, 8,
        value => _pendingGraphics.PixelLightCount = value);
      AddToggle(section, "실시간 Reflection Probe", _pendingGraphics.RealtimeReflectionProbes,
        value => _pendingGraphics.RealtimeReflectionProbes = value);
      AddToggle(section, "Reflection Probe Blending", _pendingGraphics.ReflectionProbeBlending,
        value => _pendingGraphics.ReflectionProbeBlending = value);
      AddToggle(section, "Reflection Probe Box Projection", _pendingGraphics.ReflectionProbeBoxProjection,
        value => _pendingGraphics.ReflectionProbeBoxProjection = value);
      AddToggle(section, "Soft Particles", _pendingGraphics.SoftParticles,
        value => _pendingGraphics.SoftParticles = value);
    }

    private void BuildAccessibilitySection()
    {
      var uiSection = AddSection("UI 배율", "화면에 표시되는 UI 요소의 크기를 조절합니다.");
      _uiScaleOptionsContainer = new VisualElement();
      _uiScaleOptionsContainer.AddToClassList("settings__graphics-options");
      uiSection.Add(_uiScaleOptionsContainer);
      PopulateUIScaleOptions();

      var povSection = AddSection("카메라 시점(POV)", "3인칭 카메라와 캐릭터 사이의 거리를 조절합니다.");
      var row = new VisualElement();
      row.AddToClassList("settings__pov-row");
      GetPovRange(out float min, out float max);
      _povSlider = new Slider(min, max) { showInputField = true };
      _povSlider.AddToClassList("settings__pov-slider");
      _povSlider.RegisterValueChangedCallback(OnPovSliderChanged);
      row.Add(_povSlider);
      _povValueLabel = new Label();
      _povValueLabel.AddToClassList("settings__pov-value");
      row.Add(_povValueLabel);
      povSection.Add(row);
      RefreshPovFromService();
    }

    private void BuildGraphicsActions()
    {
      var actions = new VisualElement();
      actions.AddToClassList("settings__actions");

      var reset = new Button(() =>
      {
        var display = _pendingGraphics;
        _pendingGraphics = GraphicsQualityPresets.Create(GraphicsQualityPresets.DefaultProfile);
        CopyDisplaySettings(display, _pendingGraphics);
        BuildGraphicsForm();
        SetStatusText("낮음 기본값을 불러왔습니다. 적용 버튼을 눌러 저장하세요.");
      }) { text = "낮음 기본값" };
      reset.AddToClassList("settings__secondary-btn");

      var apply = new Button(() =>
      {
        var service = GetGraphicsService();
        if (service == null)
        {
          SetStatusText("오류: 그래픽 성능 서비스를 찾을 수 없습니다.");
          return;
        }

        service.SetSettings(_pendingGraphics);
        _pendingGraphics = service.CurrentSettings;
        BuildGraphicsForm();
        SetStatusText("그래픽 설정을 적용하고 저장했습니다.");
      }) { text = "적용 및 저장" };
      apply.AddToClassList("settings__primary-btn");

      actions.Add(reset);
      actions.Add(apply);
      _graphicsScroll.Content.Add(actions);
    }

    private VisualElement AddSection(string title, string description)
    {
      var section = new VisualElement();
      section.AddToClassList("settings__section");
      if (_graphicsScroll.Content.childCount > 0)
        section.AddToClassList("settings__section--spaced");

      var titleLabel = new Label(title);
      titleLabel.AddToClassList("settings__section-title");
      section.Add(titleLabel);
      var descLabel = new Label(description);
      descLabel.AddToClassList("settings__section-desc");
      section.Add(descLabel);
      _graphicsScroll.Content.Add(section);
      return section;
    }

    private static void AddRow(VisualElement section, string label, VisualElement control)
    {
      var row = new VisualElement();
      row.AddToClassList("settings__graphics-field-row");
      var fieldLabel = new Label(label);
      fieldLabel.AddToClassList("settings__graphics-field-label");
      row.Add(fieldLabel);
      control.AddToClassList("settings__graphics-field-control");
      row.Add(control);
      section.Add(row);
    }

    private void AddToggle(VisualElement section, string label, bool value, Action<bool> setter, bool affectsProfile = true)
    {
      var field = new Toggle { value = value };
      field.RegisterValueChangedCallback(evt => ChangeDetail(() => setter(evt.newValue), affectsProfile));
      AddRow(section, label, field);
    }

    private void AddInt(VisualElement section, string label, int value, int min, int max,
      Action<int> setter, bool affectsProfile = true)
    {
      var field = new SliderInt(min, max) { value = value, showInputField = true };
      field.RegisterValueChangedCallback(evt => ChangeDetail(() => setter(evt.newValue), affectsProfile));
      AddRow(section, label, field);
    }

    private void AddFloat(VisualElement section, string label, float value, float min, float max,
      Action<float> setter, bool affectsProfile = true)
    {
      var field = new Slider(min, max) { value = value, showInputField = true };
      field.RegisterValueChangedCallback(evt => ChangeDetail(() => setter(evt.newValue), affectsProfile));
      AddRow(section, label, field);
    }

    private void AddEnum(VisualElement section, string label, Enum value, Action<Enum> setter, bool affectsProfile = true)
    {
      var field = new EnumField(value);
      field.RegisterValueChangedCallback(evt => ChangeDetail(() => setter(evt.newValue), affectsProfile));
      AddRow(section, label, field);
    }

    private void ChangeDetail(Action mutation, bool affectsProfile)
    {
      if (_graphicsFormInitializing)
        return;
      mutation();
      if (affectsProfile)
      {
        _pendingGraphics.Profile = GraphicsQualityProfile.Custom;
        _profileField?.SetValueWithoutNotify(ProfileLabel(GraphicsQualityProfile.Custom));
      }
      SetStatusText("변경 사항이 있습니다. 적용 버튼을 눌러 저장하세요.");
    }

    private static void CopyDisplaySettings(GraphicsSettingsData source, GraphicsSettingsData destination)
    {
      if (source == null || destination == null)
        return;
      destination.ResolutionWidth = source.ResolutionWidth;
      destination.ResolutionHeight = source.ResolutionHeight;
      destination.FullScreenMode = source.FullScreenMode;
      destination.RefreshRate = source.RefreshRate;
      destination.FieldOfView = source.FieldOfView;
    }

    private void PopulateUIScaleOptions()
    {
      _uiScaleOptionElements.Clear();
      foreach (var scale in new[] { UIScale.Level1, UIScale.Level2, UIScale.Level3, UIScale.Level4 })
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
      foreach (var element in _uiScaleOptionElements)
        element.SetActive(element.BoundScale == scale);
      var service = GetUIScaleService();
      if (service != null)
      {
        service.SetScale(scale);
        SetStatusText($"UI 배율을 '{UIScaleLabel(scale)}'(으)로 변경했습니다.");
      }
    }

    private void RefreshUIScaleFromService()
    {
      var current = GetUIScaleService()?.CurrentScale ?? UIScale.Level2;
      foreach (var element in _uiScaleOptionElements)
        element.SetActive(element.BoundScale == current);
    }

    private void OnPovSliderChanged(ChangeEvent<float> evt)
    {
      if (_povSliderInitializing)
        return;
      UpdatePovValueLabel(evt.newValue);
      var service = GetCameraDistanceService();
      if (service != null)
        service.SetDistance(evt.newValue);
      else
        ResolveCamera()?.SetThirdPersonDistance(evt.newValue);
    }

    private void RefreshPovFromService()
    {
      if (_povSlider == null)
        return;
      GetPovRange(out float min, out float max);
      _povSliderInitializing = true;
      try
      {
        _povSlider.lowValue = min;
        _povSlider.highValue = max;
        var service = GetCameraDistanceService();
        var camera = ResolveCamera();
        float value = service != null ? service.ResolveEffectiveDistance() :
          camera != null ? camera.DesiredThirdPersonDistance : (min + max) * 0.5f;
        _povSlider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
      }
      finally
      {
        _povSliderInitializing = false;
      }
      UpdatePovValueLabel(_povSlider.value);
    }

    private static void GetPovRange(out float min, out float max)
    {
      var service = GetCameraDistanceService();
      if (service != null)
      {
        service.GetDistanceRange(out min, out max);
        return;
      }
      var camera = ResolveCamera();
      min = camera != null ? camera.MinThirdPersonDistance : 1f;
      max = camera != null ? camera.MaxThirdPersonDistance : 8f;
    }

    private static MainCameraController ResolveCamera()
      => MainCameraController.Instance != null ? MainCameraController.Instance :
        Registry.Registry.Get<MainCameraController>(RegistryType.Service,
          Registry.Registry.TypeKey<MainCameraController>());

    private void UpdatePovValueLabel(float value)
    {
      if (_povValueLabel != null)
        _povValueLabel.text = $"{value:0.0} m";
    }

    private static TexturePerformanceService GetGraphicsService()
      => Registry.Registry.Get<TexturePerformanceService>(RegistryType.Service,
        Registry.Registry.TypeKey<TexturePerformanceService>());

    private static CameraDistancePreferenceService GetCameraDistanceService()
      => Registry.Registry.Get<CameraDistancePreferenceService>(RegistryType.Service,
        Registry.Registry.TypeKey<CameraDistancePreferenceService>());

    private static UIScalePreferenceService GetUIScaleService()
      => Registry.Registry.Get<UIScalePreferenceService>(RegistryType.Service,
        Registry.Registry.TypeKey<UIScalePreferenceService>());

    private static string ProfileLabel(GraphicsQualityProfile profile) => profile switch
    {
      GraphicsQualityProfile.VeryLow => "매우 낮음",
      GraphicsQualityProfile.Low => "낮음",
      GraphicsQualityProfile.Medium => "보통",
      GraphicsQualityProfile.High => "높음",
      GraphicsQualityProfile.VeryHigh => "매우 높음",
      _ => "사용자 지정",
    };

    private static GraphicsQualityProfile ProfileFromLabel(string label) => label switch
    {
      "매우 낮음" => GraphicsQualityProfile.VeryLow,
      "낮음" => GraphicsQualityProfile.Low,
      "보통" => GraphicsQualityProfile.Medium,
      "높음" => GraphicsQualityProfile.High,
      "매우 높음" => GraphicsQualityProfile.VeryHigh,
      _ => GraphicsQualityProfile.Custom,
    };

    private static string UIScaleLabel(UIScale scale) => scale switch
    {
      UIScale.Level1 => "1단계",
      UIScale.Level2 => "2단계",
      UIScale.Level3 => "3단계",
      UIScale.Level4 => "4단계",
      _ => scale.ToString(),
    };
  }
}
