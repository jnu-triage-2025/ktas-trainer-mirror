using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 키 설정 UI의 UIDocument 컨트롤러입니다.
  ///
  /// 역할:
  ///   - 좌측 ScrollView에 <see cref="KeyConfigEntryElement"/> 목록을 동적으로 생성합니다.
  ///   - 우측 <see cref="KeyboardLayoutElement"/>에 현재 바인딩을 반영합니다.
  ///   - 키보드 키 클릭 → 좌측 목록 스크롤 및 강조
  ///   - 목록 항목 클릭 → 키보드에서 해당 키 강조
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class KeyConfigUIController : UIControllerABC, IUIOverlay
  {
    // ──────────────────────────────────────────────────────────────────────────
    // Inspector 설정
    // ──────────────────────────────────────────────────────────────────────────
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.EscapeMenuUISortOrder + 1f;

    /// <summary>
    /// Inspector에서 직접 편집하거나 런타임에 <see cref="SetBindings"/>로 교체할 수 있는 바인딩 목록입니다.
    /// </summary>
    [SerializeField] private List<KeyBindingEntry> _bindings = new()
    {
      new KeyBindingEntry("move_forward",   "앞으로 이동",    KeyCode.W),
      new KeyBindingEntry("move_backward",  "뒤로 이동",      KeyCode.S),
      new KeyBindingEntry("move_left",      "좌측 이동",      KeyCode.A),
      new KeyBindingEntry("move_right",     "우측 이동",      KeyCode.D),
      new KeyBindingEntry("run",            "달리기",         KeyCode.LeftShift),
      new KeyBindingEntry("jump",           "점프",           KeyCode.Space),
      new KeyBindingEntry("interact",       "상호작용",       KeyCode.E),
      new KeyBindingEntry("inventory",      "인벤토리",       KeyCode.I),
      new KeyBindingEntry("map",            "지도",           KeyCode.M),
      new KeyBindingEntry("chat",           "채팅",           KeyCode.Return),
    };

    // ──────────────────────────────────────────────────────────────────────────
    // IUIOverlay 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    public event Action OverlayPushed;
    public event Action OverlayPopped;

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 참조
    // ──────────────────────────────────────────────────────────────────────────
    private UIDocument _document;
    private VisualElement _root;
    private Button _closeButton;
    private ScrollView _scroll;
    private KeyboardLayoutElement _keyboard;

    private readonly List<KeyConfigEntryElement> _entryElements = new();
    private readonly Dictionary<string, KeyConfigEntryElement> _actionIdToEntry = new();

    private bool _isVisible;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
      base.Awake();

      _document = GetComponent<UIDocument>();
      if (_document == null)
      {
        Debug.LogError("[KeyConfigUI] UIDocument 컴포넌트를 찾을 수 없습니다.");
        return;
      }
      _document.sortingOrder = _sortingOrder;

      var root = _document.rootVisualElement;
      _root = root?.Q<VisualElement>("key-config-root");
      _closeButton = root?.Q<Button>("close-button");
      _scroll = root?.Q<ScrollView>("binding-scroll");
      _keyboard = root?.Q<KeyboardLayoutElement>("keyboard-layout");

      if (_closeButton != null)
        _closeButton.clicked += HandleCloseClicked;

      if (_keyboard != null)
        _keyboard.OnAssignedKeyClicked += HandleKeyboardKeyClicked;

      Populate(_bindings);
      SetVisible(false);
    }

    private void OnDestroy()
    {
      if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
      if (_keyboard != null)    _keyboard.OnAssignedKeyClicked -= HandleKeyboardKeyClicked;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>UI를 표시합니다.</summary>
    public void Show() => SetVisible(true);

    /// <summary>UI를 숨깁니다.</summary>
    public void Hide() => SetVisible(false);

    /// <summary>표시/숨김 토글합니다.</summary>
    public void Toggle() => SetVisible(!_isVisible);

    /// <summary>
    /// 바인딩 목록을 교체하고 UI를 다시 구성합니다.
    /// 런타임에 설정이 변경된 경우 호출합니다.
    /// </summary>
    public void SetBindings(IReadOnlyList<KeyBindingEntry> bindings)
    {
      _bindings.Clear();
      if (bindings != null) _bindings.AddRange(bindings);
      Populate(_bindings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 초기화 & 목록 구성
    // ──────────────────────────────────────────────────────────────────────────
    private void Populate(IReadOnlyList<KeyBindingEntry> bindings)
    {
      if (_scroll == null) return;

      // 기존 항목 정리
      foreach (var el in _entryElements)
        el.OnEntryClicked -= HandleEntryClicked;
      _entryElements.Clear();
      _actionIdToEntry.Clear();
      _scroll.Clear();

      // 새 항목 생성
      foreach (var binding in bindings)
      {
        var entry = new KeyConfigEntryElement();
        entry.Bind(binding);
        entry.OnEntryClicked += HandleEntryClicked;

        _scroll.Add(entry);
        _entryElements.Add(entry);
        _actionIdToEntry[binding.actionId] = entry;
      }

      // 키보드 오버레이 갱신
      _keyboard?.SetBindings(bindings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>키보드에서 할당된 키를 클릭 → 좌측 목록 해당 항목 강조 & 스크롤</summary>
    private void HandleKeyboardKeyClicked(string actionId)
    {
      FocusEntry(actionId);
    }

    /// <summary>좌측 목록 항목 클릭 → 키보드에서 해당 키 강조</summary>
    private void HandleEntryClicked(string actionId)
    {
      // 목록 선택 상태 업데이트
      foreach (var el in _entryElements)
        el.SetSelected(false);

      if (_actionIdToEntry.TryGetValue(actionId, out var entry))
        entry.SetSelected(true);

      // 키보드에서 해당 키 강조
      var binding = _bindings.Find(b => b.actionId == actionId);
      if (binding != null && binding.boundKey != KeyCode.None)
        _keyboard?.HighlightKey(binding.boundKey);
      else
        _keyboard?.ClearAllHighlights();
    }

    /// <summary>actionId에 해당하는 항목을 강조하고 스크롤합니다.</summary>
    private void FocusEntry(string actionId)
    {
      // 이전 포커스 해제
      foreach (var el in _entryElements)
        el.SetFocused(false);

      if (!_actionIdToEntry.TryGetValue(actionId, out var target)) return;

      // 포커스 설정
      target.SetFocused(true);

      // 목록 내 스크롤 이동 (UI Toolkit 지연 레이아웃 처리)
      target.schedule.Execute(() =>
      {
        if (_scroll == null) return;

        // 항목의 로컬 좌표를 ScrollView 좌표로 변환
        var itemPos = target.worldBound;
        var scrollPos = _scroll.worldBound;

        if (itemPos == Rect.zero || scrollPos == Rect.zero) return;

        float itemTop = itemPos.y - scrollPos.y + _scroll.scrollOffset.y;
        float itemBot = itemTop + itemPos.height;
        float viewTop = _scroll.scrollOffset.y;
        float viewBot = viewTop + _scroll.contentViewport.layout.height;

        if (itemTop < viewTop)
        {
          _scroll.scrollOffset = new Vector2(0f, itemTop - 4f);
        }
        else if (itemBot > viewBot)
        {
          _scroll.scrollOffset = new Vector2(0f, itemBot - _scroll.contentViewport.layout.height + 4f);
        }
      }).StartingIn(0);
    }

    private void HandleCloseClicked()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        Hide();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 표시/숨김
    // ──────────────────────────────────────────────────────────────────────────
    private void SetVisible(bool visible)
    {
      _isVisible = visible;
      if (_root == null) return;

      _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      // USS 기본값 opacity: 0 을 런타임에서 override
      _root.style.opacity = visible ? 1f : 0f;
    }

    public void OnOverlayPushed()
    {
      Show();
      Registry.Registry.Get<Player.PlayerController>(RegistryType.Entity, Registry.Registry.TypeKey<Player.PlayerController>())?.EnterUIOverlayMode();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      Hide();
      Registry.Registry.Get<Player.PlayerController>(RegistryType.Entity, Registry.Registry.TypeKey<Player.PlayerController>())?.ExitUIOverlayMode();
      OverlayPopped?.Invoke();
    }
  }
}
