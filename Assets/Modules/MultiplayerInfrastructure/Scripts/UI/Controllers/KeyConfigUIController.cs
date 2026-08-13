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
    /// PlayerPrefs에 저장된 값이 있으면 Awake 시점에 덮어씁니다.
    /// </summary>
    [SerializeField] private List<KeyBindingEntry> _bindings = new()
    {
      new KeyBindingEntry("move_forward",   "앞으로 이동",    KeyCode.W),
      new KeyBindingEntry("move_backward",  "뒤로 이동",      KeyCode.S),
      new KeyBindingEntry("move_left",      "좌측 이동",      KeyCode.A),
      new KeyBindingEntry("move_right",     "우측 이동",      KeyCode.D),
      new KeyBindingEntry("run",            "달리기",         KeyCode.LeftControl),
      new KeyBindingEntry("dismount",       "탈것 내리기",    KeyCode.LeftShift),
      new KeyBindingEntry("jump",           "점프",           KeyCode.Space),
      new KeyBindingEntry("interact",       "상호작용",       KeyCode.E),
      new KeyBindingEntry("inventory",      "인벤토리",       KeyCode.I),
      new KeyBindingEntry("map",            "지도",           KeyCode.M),
      new KeyBindingEntry("chat",           "채팅",           KeyCode.Return),
    };

    // 기본(초기) 바인딩 복원용 복사본 — 런타임에 자동 생성됩니다
    private List<KeyBindingEntry> _defaultBindings;

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
    private Button _resetButton;
    private ScrollView _scroll;
    private KeyboardLayoutElement _keyboard;

    private readonly List<KeyConfigEntryElement> _entryElements = new();
    private readonly Dictionary<string, KeyConfigEntryElement> _actionIdToEntry = new();

    private bool _isVisible;

    /// <summary>현재 리바인딩 대기 중인 actionId. null이면 리바인딩 비활성 상태.</summary>
    private string _rebindingActionId;

    // ──────────────────────────────────────────────────────────────────────────
    // Unity 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    protected override void Awake()
    {
      base.Awake();

      // 기본 바인딩 복사본 보존
      _defaultBindings = new List<KeyBindingEntry>(_bindings.Count);
      foreach (var entry in _bindings)
        _defaultBindings.Add(new KeyBindingEntry(entry.actionId, entry.actionDisplayName, entry.boundKey));

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
      _resetButton = root?.Q<Button>("reset-button");
      _scroll = root?.Q<ScrollView>("binding-scroll");
      _keyboard = root?.Q<KeyboardLayoutElement>("keyboard-layout");

      if (_closeButton != null)
        _closeButton.clicked += HandleCloseClicked;

      if (_resetButton != null)
        _resetButton.clicked += HandleResetClicked;

      if (_keyboard != null)
        _keyboard.OnAssignedKeyClicked += HandleKeyboardKeyClicked;

      // 저장된 바인딩 불러오기 (없으면 기본값 유지)
      KeyBindingRepository.LoadInto(_bindings);

      Populate(_bindings);
      SetVisible(false);
    }

    private void OnEnable()
    {
      // UIDocument가 rootVisualElement를 (재)생성한 뒤 숨김 상태로 확실히 중립화한다.
      // (Awake 시점 캐시가 detached되어 컨텐츠 root가 pickable로 남는 문제를 방지.)
      if (!_isVisible)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
    }

    private void Update()
    {
      if (_rebindingActionId == null || !_isVisible) return;

      // ESC → 리바인딩 취소
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        CancelRebinding();
        return;
      }

      // 어떤 키든 눌리면 캡처
      if (!Input.anyKeyDown) return;

      // 입력된 키 탐색
      foreach (KeyCode candidate in System.Enum.GetValues(typeof(KeyCode)))
      {
        // 마우스 버튼 제외
        if (candidate >= KeyCode.Mouse0 && candidate <= KeyCode.Mouse6) continue;
        // 조이스틱 제외
        if (candidate >= KeyCode.JoystickButton0) continue;
        if (!Input.GetKeyDown(candidate)) continue;

        ApplyRebinding(_rebindingActionId, candidate);
        return;
      }
    }

    protected override void OnDestroy()
    {
      if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
      if (_resetButton != null)  _resetButton.clicked -= HandleResetClicked;
      if (_keyboard != null)     _keyboard.OnAssignedKeyClicked -= HandleKeyboardKeyClicked;
      base.OnDestroy();
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
      CancelRebinding();
      _bindings.Clear();
      if (bindings != null) _bindings.AddRange(bindings);
      Populate(_bindings);
    }

    /// <summary>
    /// 모든 바인딩을 초기(기본) 값으로 되돌리고 저장합니다.
    /// </summary>
    public void ResetToDefaults()
    {
      if (_defaultBindings == null) return;
      CancelRebinding();
      for (int i = 0; i < _bindings.Count; i++)
      {
        var def = _defaultBindings.Find(d => d.actionId == _bindings[i].actionId);
        if (def != null) _bindings[i].boundKey = def.boundKey;
      }
      KeyBindingRepository.DeleteAll(_bindings);
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
      {
        el.OnEntryClicked -= HandleEntryClicked;
        el.OnRebindRequested -= HandleRebindRequested;
      }
      _entryElements.Clear();
      _actionIdToEntry.Clear();
      _scroll.Clear();

      // 새 항목 생성
      foreach (var binding in bindings)
      {
        var entry = new KeyConfigEntryElement();
        entry.Bind(binding);
        entry.OnEntryClicked += HandleEntryClicked;
        entry.OnRebindRequested += HandleRebindRequested;

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

    /// <summary>리바인딩 요청 (키 레이블 클릭) → 대기 상태 진입</summary>
    private void HandleRebindRequested(string actionId)
    {
      // 다른 항목이 이미 대기 중이면 취소
      if (_rebindingActionId != null && _rebindingActionId != actionId)
        CancelRebinding();

      _rebindingActionId = actionId;

      // 해당 엔트리 시각 상태 전환
      if (_actionIdToEntry.TryGetValue(actionId, out var el))
        el.SetRebinding(true);
    }

    /// <summary>리바인딩 취소</summary>
    private void CancelRebinding()
    {
      if (_rebindingActionId == null) return;

      if (_actionIdToEntry.TryGetValue(_rebindingActionId, out var el))
        el.SetRebinding(false);

      _rebindingActionId = null;
    }

    /// <summary>리바인딩 확정 → 데이터 갱신, 저장, UI 갱신</summary>
    private void ApplyRebinding(string actionId, KeyCode newKey)
    {
      var entry = _bindings.Find(b => b.actionId == actionId);
      if (entry == null)
      {
        CancelRebinding();
        return;
      }

      entry.boundKey = newKey;

      // 저장
      KeyBindingRepository.SaveEntry(entry);
      KeyBindingRepository.Flush();

      // UI 갱신
      if (_actionIdToEntry.TryGetValue(actionId, out var el))
      {
        el.SetRebinding(false);
        el.RefreshKeyLabel();
      }

      _keyboard?.SetBindings(_bindings);

      _rebindingActionId = null;

      Debug.Log($"[KeyConfig] '{actionId}' → {newKey} 저장 완료");
    }

    private void HandleResetClicked() => ResetToDefaults();

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
      if (!visible) CancelRebinding();

      // 표시할 때 캐시된 _root가 detached되었을 수 있으므로 재바인딩을 시도한다.
      // (network-spawned 프리팹의 UIDocument rootVisualElement 재생성 대응.
      //  Awake 시점에 한 번만 캐시하면 화면에 반영되지 않는 문제 방지.)
      if (visible && (_root == null || _root.panel == null))
        RebindToCurrentDocumentRoot();

      // rootVisualElement 중립화는 _root(자식) 유무와 무관하게 항상 수행한다.
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
      var root = _document != null ? _document.rootVisualElement : null;
      if (root == null) return;

      var newRoot = root.Q<VisualElement>("key-config-root");
      if (_root == newRoot && _root != null && _root.panel != null)
        return;

      // 이전 구독 해제
      if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
      if (_resetButton != null) _resetButton.clicked -= HandleResetClicked;
      if (_keyboard != null)    _keyboard.OnAssignedKeyClicked -= HandleKeyboardKeyClicked;

      _root = newRoot;
      _closeButton = root.Q<Button>("close-button");
      _resetButton = root.Q<Button>("reset-button");
      _scroll = root.Q<ScrollView>("binding-scroll");
      _keyboard = root.Q<KeyboardLayoutElement>("keyboard-layout");

      if (_closeButton != null) _closeButton.clicked += HandleCloseClicked;
      if (_resetButton != null) _resetButton.clicked += HandleResetClicked;
      if (_keyboard != null)    _keyboard.OnAssignedKeyClicked += HandleKeyboardKeyClicked;

      Populate(_bindings);
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
  }
}
