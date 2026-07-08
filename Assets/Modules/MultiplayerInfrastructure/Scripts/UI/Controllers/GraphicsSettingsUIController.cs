using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 그래픽 설정 UI의 UIDocument 컨트롤러입니다.
  ///
  /// 역할:
  ///   - <see cref="TextureQualityOptionElement"/> 타일 4개를 동적으로 생성합니다.
  ///   - 타일 클릭 시 선택 상태를 갱신하고 "적용 및 저장" 버튼 활성화를 조절합니다.
  ///   - "적용 및 저장" 클릭 시 <see cref="TexturePerformanceService"/>를 통해 설정을 저장합니다.
  ///   - <see cref="IUIOverlay"/>를 구현하여 <see cref="UIOverlayStack"/>으로 열고 닫습니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class GraphicsSettingsUIController : UIControllerABC, IUIOverlay
  {
    // ──────────────────────────────────────────────────────────────────────────
    // Inspector 설정
    // ──────────────────────────────────────────────────────────────────────────
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.GraphicsSettingsUISortOrder;

    // ──────────────────────────────────────────────────────────────────────────
    // IUIOverlay 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    public event Action OverlayPushed;
    public event Action OverlayPopped;

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 참조 — UIDocument 요소
    // ──────────────────────────────────────────────────────────────────────────
    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _optionsContainer;
    private Button _closeButton;
    private Button _applyButton;
    private Label _statusLabel;

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 상태
    // ──────────────────────────────────────────────────────────────────────────
    private readonly List<TextureQualityOptionElement> _optionElements = new();
    private TextureQuality _pendingQuality;
    private bool _hasPendingChange;
    private bool _isVisible;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 라이프사이클
    // ──────────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
      base.Awake();

      _document = GetComponent<UIDocument>();
      if (_document == null)
      {
        Debug.LogError("[GraphicsSettingsUI] UIDocument 컴포넌트를 찾을 수 없습니다.");
        return;
      }
      _document.sortingOrder = _sortingOrder;

      var docRoot = _document.rootVisualElement;
      _root             = docRoot?.Q<VisualElement>("graphics-settings-root");
      _optionsContainer = docRoot?.Q<VisualElement>("options-container");
      _closeButton      = docRoot?.Q<Button>("close-button");
      _applyButton      = docRoot?.Q<Button>("apply-button");
      _statusLabel      = docRoot?.Q<Label>("status-label");

      if (_closeButton != null)
        _closeButton.clicked += HandleCloseClicked;
      else
        Debug.LogError("[GraphicsSettingsUI] close-button을 UXML에서 찾을 수 없습니다.");

      if (_applyButton != null)
        _applyButton.clicked += HandleApplyClicked;
      else
        Debug.LogError("[GraphicsSettingsUI] apply-button을 UXML에서 찾을 수 없습니다.");

      PopulateOptions();
      SetVisible(false);
    }

    private void OnEnable()
    {
      if (!_isVisible)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
    }

    private void OnDestroy()
    {
      if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
      if (_applyButton != null) _applyButton.clicked -= HandleApplyClicked;

      foreach (var el in _optionElements)
        el.OnOptionSelected -= HandleOptionSelected;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>UI를 표시합니다.</summary>
    public void Show() => SetVisible(true);

    /// <summary>UI를 숨깁니다.</summary>
    public void Hide() => SetVisible(false);

    /// <summary>표시/숨김을 토글합니다.</summary>
    public void Toggle() => SetVisible(!_isVisible);

    // ──────────────────────────────────────────────────────────────────────────
    // IUIOverlay 구현
    // ──────────────────────────────────────────────────────────────────────────
    public void OnOverlayPushed()
    {
      RefreshFromService();
      Show();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      Hide();
      OverlayPopped?.Invoke();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 옵션 타일 구성
    // ──────────────────────────────────────────────────────────────────────────
    private void PopulateOptions()
    {
      if (_optionsContainer == null)
      {
        Debug.LogError("[GraphicsSettingsUI] options-container를 UXML에서 찾을 수 없습니다.");
        return;
      }

      _optionsContainer.Clear();
      _optionElements.Clear();

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
        element.OnOptionSelected += HandleOptionSelected;
        _optionElements.Add(element);
        _optionsContainer.Add(element);
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ──────────────────────────────────────────────────────────────────────────
    private void HandleOptionSelected(TextureQuality quality)
    {
      _pendingQuality   = quality;
      _hasPendingChange = true;

      // 선택 상태 갱신
      foreach (var el in _optionElements)
        el.SetActive(el.BoundQuality == quality);

      SetStatusText("변경 사항이 있습니다. \"적용 및 저장\"을 눌러 적용하세요.");
      Debug.Log($"[GraphicsSettingsUI] 선택됨: {quality}");
    }

    private void HandleApplyClicked()
    {
      if (!_hasPendingChange)
      {
        SetStatusText("변경할 설정이 없습니다.");
        return;
      }

      var service = GetService();
      if (service == null)
      {
        Debug.LogError("[GraphicsSettingsUI] TexturePerformanceService를 Registry에서 찾을 수 없습니다.");
        SetStatusText("오류: 성능 서비스를 찾을 수 없습니다.");
        return;
      }

      service.SetQuality(_pendingQuality);
      _hasPendingChange = false;
      SetStatusText("저장되었습니다.");
      Debug.Log($"[GraphicsSettingsUI] 텍스처 품질 적용 및 저장: {_pendingQuality}");
    }

    private void HandleCloseClicked()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        Hide();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UI 갱신 유틸
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// UI를 열 때 현재 서비스 상태와 동기화합니다.
    /// </summary>
    private void RefreshFromService()
    {
      var service = GetService();
      var current = service != null ? service.CurrentQuality : TextureQuality.High;

      _pendingQuality   = current;
      _hasPendingChange = false;

      foreach (var el in _optionElements)
        el.SetActive(el.BoundQuality == current);

      SetStatusText(string.Empty);
    }

    private void SetVisible(bool visible)
    {
      _isVisible = visible;

      // 표시할 때 캐시된 _root가 detached되었을 수 있으므로 재바인딩을 시도한다.
      // (network-spawned 프리팹의 UIDocument rootVisualElement 재생성 대응.)
      if (visible && (_root == null || _root.panel == null))
        RebindToCurrentDocumentRoot();

      // rootVisualElement 중립화는 _root(자식) 유무와 무관하게 항상 수행해야 한다.
      // (_root가 아직 null이어도 rootVisualElement는 존재할 수 있고, 이 처리가 누락되면
      //  숨김 상태의 패널 root가 화면 전체에서 포인터 이벤트를 계속 가로챈다.)
      SetDocumentRootInteractable(_document, visible);

      if (_root == null) return;

      _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      // USS 기본값 opacity: 0 을 런타임에서 override
      _root.style.opacity = visible ? 1f : 0f;
    }

    /// <summary>
    /// 현재 UIDocument.rootVisualElement 기준으로 요소를 재바인딩한다(detached root 대응).
    /// </summary>
    private void RebindToCurrentDocumentRoot()
    {
      var docRoot = _document != null ? _document.rootVisualElement : null;
      if (docRoot == null) return;

      var newRoot = docRoot.Q<VisualElement>("graphics-settings-root");
      if (_root == newRoot && _root != null && _root.panel != null)
        return;

      if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
      if (_applyButton != null) _applyButton.clicked -= HandleApplyClicked;

      _root             = newRoot;
      _optionsContainer = docRoot.Q<VisualElement>("options-container");
      _closeButton      = docRoot.Q<Button>("close-button");
      _applyButton      = docRoot.Q<Button>("apply-button");
      _statusLabel      = docRoot.Q<Label>("status-label");

      if (_closeButton != null) _closeButton.clicked += HandleCloseClicked;
      if (_applyButton != null) _applyButton.clicked += HandleApplyClicked;

      PopulateOptions();
    }

    private void SetStatusText(string text)
    {
      if (_statusLabel != null)
        _statusLabel.text = text;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 헬퍼
    // ──────────────────────────────────────────────────────────────────────────
    private static TexturePerformanceService GetService()
      => Registry.Registry.Get<TexturePerformanceService>(
        RegistryType.Service,
        Registry.Registry.TypeKey<TexturePerformanceService>()
      );

  }
}
