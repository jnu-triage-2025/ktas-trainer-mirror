using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 키 설정 / 그래픽 설정을 하나의 창에서 탭으로 통합한 설정 UI 컨트롤러입니다.
  ///
  /// 기존의 독립적인 <c>KeyConfigUIController</c> / <c>GraphicsSettingsUIController</c> 및
  /// 각각의 UIDocument를 대체합니다. Escape 메뉴의 단일 "설정" 버튼이 이 컨트롤러를 오버레이로 push합니다.
  ///
  /// 구조:
  ///   - UXML(SettingsUI.uxml)은 헤더/탭바/컨텐츠영역/푸터의 뼈대만 제공합니다.
  ///   - 탭 버튼과 각 탭의 컨텐츠(키 설정 UI, 그래픽 설정 UI)는 이 컨트롤러가 코드로 동적 생성합니다.
  ///     (Unity 에디터의 인스펙터/에디터 컴포넌트는 런타임에서 사용할 수 없으므로,
  ///      가변적으로 나타나는 설정 화면은 UI Toolkit + 코드 동적 생성으로 구현합니다.)
  ///
  /// detached rootVisualElement 대응: network-spawned 프리팹의 자식으로 배치되어 UIDocument가
  /// rootVisualElement를 재생성하면 Awake 캐시가 무효화되므로, OnEnable/표시 시점마다 재바인딩합니다.
  /// (InventoryUIController / GameEscapeMenuUIController 와 동일한 규약.)
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public partial class SettingsUIController : UIControllerABC, IUIOverlay
  {
    public enum SettingsTab
    {
      Key,
      Graphics,
    }

    [SerializeField] private float _sortingOrder = DefaultsUIDocument.SettingsUISortOrder;

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    private UIDocument _document;
    private VisualElement _root;
    private Button _closeButton;
    private VisualElement _tabBar;
    private VisualElement _tabContent;
    private Label _statusLabel;

    private readonly Dictionary<SettingsTab, Button> _tabButtons = new();
    private SettingsTab _activeTab = SettingsTab.Key;
    private bool _isVisible;

    // 각 탭 컨텐츠 루트(지연 생성 후 캐시). 탭 전환 시 파괴하지 않고 detach/attach 한다.
    private VisualElement _keyTabContent;
    private VisualElement _graphicsTabContent;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 라이프사이클
    // ──────────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
      base.Awake();

      _document = GetComponent<UIDocument>();
      if (_document == null)
      {
        Debug.LogError("[SettingsUI] UIDocument 컴포넌트를 찾을 수 없습니다.");
        return;
      }
      _document.sortingOrder = _sortingOrder;

      InitKeyData();

      BindViewToCurrentDocumentRoot();
      SetVisible(false);
    }

    private void OnEnable()
    {
      BindViewToCurrentDocumentRoot();

      if (!_isVisible)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
    }

    private void Update()
    {
      // 키 리바인딩 대기 입력 처리(키 설정 탭이 활성이고 표시 중일 때만).
      UpdateKeyRebinding();
    }

    protected override void OnDestroy()
    {
      DetachChrome();
      DetachKeyTab();
      DetachGraphicsTab();
      base.OnDestroy();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 뷰 바인딩 (detached root 대응)
    // ──────────────────────────────────────────────────────────────────────────
    private void BindViewToCurrentDocumentRoot()
    {
      if (_document == null)
        _document = GetComponent<UIDocument>();

      var docRoot = _document != null ? _document.rootVisualElement : null;
      if (docRoot == null)
        return;

      var newRoot = docRoot.Q<VisualElement>("settings-root");

      // 이미 현재 live root에 붙어 있는 동일 root면 재바인딩 불필요.
      if (_root != null && _root == newRoot && _root.panel != null)
        return;

      DetachChrome();

      _root        = newRoot;
      _closeButton = docRoot.Q<Button>("close-button");
      _tabBar      = docRoot.Q<VisualElement>("tab-bar");
      _tabContent  = docRoot.Q<VisualElement>("tab-content");
      _statusLabel = docRoot.Q<Label>("status-label");

      if (_root == null)
        Debug.LogError("[SettingsUI] 'settings-root'를 UXML에서 찾을 수 없습니다.");

      if (_closeButton != null)
        _closeButton.clicked += HandleCloseClicked;
      else
        Debug.LogError("[SettingsUI] close-button을 UXML에서 찾을 수 없습니다.");

      // 탭 컨텐츠 캐시는 root가 바뀌면 무효이므로 폐기 후 재생성한다.
      DetachKeyTab();
      DetachGraphicsTab();
      _keyTabContent = null;
      _graphicsTabContent = null;

      BuildTabBar();
      ShowTab(_activeTab, force: true);
    }

    private void DetachChrome()
    {
      if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 탭 바 / 탭 전환
    // ──────────────────────────────────────────────────────────────────────────
    private void BuildTabBar()
    {
      if (_tabBar == null)
        return;

      _tabBar.Clear();
      _tabButtons.Clear();

      AddTabButton(SettingsTab.Key, "키 설정");
      AddTabButton(SettingsTab.Graphics, "그래픽 설정");
    }

    private void AddTabButton(SettingsTab tab, string label)
    {
      var button = new Button(() => ShowTab(tab)) { text = label };
      button.AddToClassList("settings__tab-button");
      _tabBar.Add(button);
      _tabButtons[tab] = button;
    }

    private void ShowTab(SettingsTab tab, bool force = false)
    {
      if (!force && _activeTab == tab && _tabContent != null && _tabContent.childCount > 0)
      {
        UpdateTabButtonStates();
        return;
      }

      _activeTab = tab;

      if (_tabContent == null)
        return;

      _tabContent.Clear();

      switch (tab)
      {
        case SettingsTab.Key:
          _tabContent.Add(EnsureKeyTabContent());
          RefreshKeyTab();
          break;
        case SettingsTab.Graphics:
          _tabContent.Add(EnsureGraphicsTabContent());
          RefreshGraphicsTab();
          break;
      }

      UpdateTabButtonStates();

      // 탭 전환으로 새로 추가된 컨텐츠(슬라이더/버튼 등)에도 히트테스트를 복구한다.
      // (표시 중일 때만. 숨김 상태에서는 SetVisible이 Ignore로 유지한다.)
      if (_isVisible)
      {
        SetDocumentRootInteractable(_document, true);
        NeutralizeNonInteractiveLabels();
      }
    }

    /// <summary>
    /// 설명/상태/제목 등 <b>비인터랙션 라벨</b>의 pickingMode를 Ignore로 되돌린다.
    ///
    /// <see cref="UIControllerABC.SetDocumentRootInteractable"/>는 표시 시 서브트리 전체를
    /// PickingMode.Position으로 복구하는데, 이때 텍스트 라벨도 Position이 되어 실제로는 빈
    /// 영역까지 포인터 이벤트를 흡수한다. 전체 화면 레이아웃에서 이 라벨들이 슬라이더/버튼 위를
    /// 덮으면 하위 인터랙션 요소의 클릭/드래그가 막힌다. 라벨은 클릭 대상이 아니므로 Ignore로
    /// 되돌려 이벤트가 통과하도록 한다.
    ///
    /// 주의: 키 설정의 키 레이블(KeyConfigEntryElement 내부 Label)은 클릭 대상이므로 제외해야 한다.
    /// 그 라벨들은 이 설정 창의 컨텐츠 트리 내부(KeyConfigEntryElement) 소유이며, 여기서는
    /// 설정 창 자체가 직접 만든 라벨(제목/설명/상태/POV 값)만 대상으로 한다.
    /// </summary>
    private void NeutralizeNonInteractiveLabels()
    {
      if (_root == null)
        return;

      // 설정 창이 직접 생성한 비인터랙션 라벨 클래스들.
      NeutralizeLabelsByClass("settings__title");
      NeutralizeLabelsByClass("settings__status");
      NeutralizeLabelsByClass("settings__section-title");
      NeutralizeLabelsByClass("settings__section-desc");
      NeutralizeLabelsByClass("settings__panel-title");
      NeutralizeLabelsByClass("settings__pov-value");
    }

    private void NeutralizeLabelsByClass(string className)
    {
      _root.Query<Label>(className: className).ForEach(label =>
      {
        label.pickingMode = PickingMode.Ignore;
      });
    }

    private void UpdateTabButtonStates()
    {
      foreach (var pair in _tabButtons)
      {
        bool active = pair.Key == _activeTab;
        pair.Value.EnableInClassList("settings__tab-button--active", active);
      }
    }

    private void SetStatusText(string text)
    {
      if (_statusLabel != null)
        _statusLabel.text = text ?? string.Empty;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 표시/숨김 & IUIOverlay
    // ──────────────────────────────────────────────────────────────────────────
    public void Show() => SetVisible(true);
    public void Hide() => SetVisible(false);
    public void Toggle() => SetVisible(!_isVisible);

    private void SetVisible(bool visible)
    {
      _isVisible = visible;

      if (!visible)
        CancelRebinding();

      // 표시할 때는 캐시된 root가 detach되었을 수 있으므로 재바인딩을 시도한다.
      if (visible && (_root == null || _root.panel == null))
        BindViewToCurrentDocumentRoot();

      if (_root != null)
      {
        _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _root.style.opacity = visible ? 1f : 0f;
      }

      // 탭 컨텐츠(슬라이더 등 인터랙션 요소 포함)를 먼저 구성한 뒤에 히트테스트를 복구해야
      // 나중에 생성된 요소까지 pickingMode가 올바르게 적용된다.
      if (visible)
      {
        SetStatusText(string.Empty);
        ShowTab(_activeTab, force: true);
      }

      // rootVisualElement 중립화(히트테스트 제어)는 _root 유무와 무관하게 항상 수행.
      // 표시 시점: 트리 구성이 끝난 뒤에 호출하여 슬라이더/버튼 등 전체가 Position이 되도록 한다.
      SetDocumentRootInteractable(_document, visible);

      // 표시 시 비인터랙션 라벨을 Ignore로 되돌려, 라벨이 슬라이더/버튼 클릭을 가로채지 않게 한다.
      if (visible)
        NeutralizeNonInteractiveLabels();
    }

    public void OnOverlayPushed()
    {
      Show();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      Hide();
      OverlayPopped?.Invoke();
    }

    private void HandleCloseClicked()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        Hide();
    }
  }
}
