using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// SettingsUIController의 "키 설정" 탭 구현.
  /// 기존 KeyConfigUIController의 로직/요소(KeyConfigEntryElement, KeyboardLayoutElement)를 재사용합니다.
  /// </summary>
  public partial class SettingsUIController
  {
    // 기본 바인딩. KeyConfigUIController의 기본값을 계승합니다.
    private readonly List<KeyBindingEntry> _bindings = new()
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
      new KeyBindingEntry("camera_distance_modifier", "POV 조정 수정자", KeyCode.LeftAlt),
      new KeyBindingEntry(DefaultsKeyConfiguration.ShowPlayerListActionId, "접속자 목록 보기",
        DefaultsKeyConfiguration.ShowPlayerList),
    };

    private List<KeyBindingEntry> _defaultBindings;

    private ScrollView _keyScroll;
    private OverflowScrollView _keyOverflowScroll;
    private KeyboardLayoutElement _keyboard;
    private readonly List<KeyConfigEntryElement> _entryElements = new();
    private readonly Dictionary<string, KeyConfigEntryElement> _actionIdToEntry = new();

    /// <summary>현재 리바인딩 대기 중인 actionId. null이면 비활성 상태.</summary>
    private string _rebindingActionId;

    private void InitKeyData()
    {
      _defaultBindings = new List<KeyBindingEntry>(_bindings.Count);
      foreach (var entry in _bindings)
        _defaultBindings.Add(new KeyBindingEntry(entry.actionId, entry.actionDisplayName, entry.boundKey));

      // 저장된 바인딩 불러오기(없으면 기본값 유지).
      KeyBindingRepository.LoadInto(_bindings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 탭 컨텐츠 생성
    // ──────────────────────────────────────────────────────────────────────────
    private VisualElement EnsureKeyTabContent()
    {
      if (_keyTabContent != null)
        return _keyTabContent;

      var content = new VisualElement();
      content.AddToClassList("settings__key-content");

      // 좌측: 기능 목록
      var listPanel = new VisualElement();
      listPanel.AddToClassList("settings__key-list-panel");

      var listTitle = new Label("기능");
      listTitle.AddToClassList("settings__panel-title");
      listPanel.Add(listTitle);

      _keyOverflowScroll = new OverflowScrollView();
      _keyOverflowScroll.AddToClassList("settings__key-scroll");
      _keyScroll = _keyOverflowScroll.ScrollView;
      listPanel.Add(_keyOverflowScroll);

      content.Add(listPanel);

      // 우측: 키보드 시각화
      var keyboardPanel = new VisualElement();
      keyboardPanel.AddToClassList("settings__key-keyboard-panel");

      var keyboardTitle = new Label("키보드");
      keyboardTitle.AddToClassList("settings__panel-title");
      keyboardPanel.Add(keyboardTitle);

      _keyboard = new KeyboardLayoutElement();
      _keyboard.AddToClassList("settings__key-keyboard");
      _keyboard.OnAssignedKeyClicked += HandleKeyboardKeyClicked;
      keyboardPanel.Add(_keyboard);

      content.Add(keyboardPanel);

      // 하단: 초기화 버튼
      var actions = new VisualElement();
      actions.AddToClassList("settings__actions");
      var resetButton = new Button(HandleResetClicked) { text = "기본값으로 초기화" };
      resetButton.AddToClassList("settings__secondary-btn");
      actions.Add(resetButton);

      // 목록 패널 하단에 액션을 붙이면 세로 정렬이 어색하므로, content 최하단에 별도 행으로.
      var wrapper = new VisualElement();
      wrapper.AddToClassList("settings__key-tab-wrapper");
      wrapper.Add(content);
      wrapper.Add(actions);

      _keyTabContent = wrapper;

      PopulateKeyList(_bindings);
      return _keyTabContent;
    }

    private void RefreshKeyTab()
    {
      if (_keyTabContent == null)
        return;

      PopulateKeyList(_bindings);
    }

    private void DetachKeyTab()
    {
      if (_keyboard != null)
        _keyboard.OnAssignedKeyClicked -= HandleKeyboardKeyClicked;

      foreach (var el in _entryElements)
      {
        el.OnEntryClicked -= HandleEntryClicked;
        el.OnRebindRequested -= HandleRebindRequested;
      }
      _entryElements.Clear();
      _actionIdToEntry.Clear();
      _keyboard = null;
      _keyScroll = null;
      _keyOverflowScroll = null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 목록 구성
    // ──────────────────────────────────────────────────────────────────────────
    private void PopulateKeyList(IReadOnlyList<KeyBindingEntry> bindings)
    {
      if (_keyScroll == null)
        return;

      foreach (var el in _entryElements)
      {
        el.OnEntryClicked -= HandleEntryClicked;
        el.OnRebindRequested -= HandleRebindRequested;
      }
      _entryElements.Clear();
      _actionIdToEntry.Clear();
      _keyScroll.Clear();

      foreach (var binding in bindings)
      {
        var entry = new KeyConfigEntryElement();
        entry.Bind(binding);
        entry.OnEntryClicked += HandleEntryClicked;
        entry.OnRebindRequested += HandleRebindRequested;

        _keyScroll.Add(entry);
        _entryElements.Add(entry);
        _actionIdToEntry[binding.actionId] = entry;
      }

      _keyboard?.SetBindings(bindings);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 리바인딩 입력 처리 (SettingsUIController.Update에서 호출)
    // ──────────────────────────────────────────────────────────────────────────
    private void UpdateKeyRebinding()
    {
      if (_rebindingActionId == null || !_isVisible || _activeTab != SettingsTab.Key)
        return;

      // ESC → 리바인딩 취소
      if (Input.GetKeyDown(KeyCode.Escape))
      {
        CancelRebinding();
        return;
      }

      if (!Input.anyKeyDown)
        return;

      foreach (KeyCode candidate in System.Enum.GetValues(typeof(KeyCode)))
      {
        if (candidate >= KeyCode.Mouse0 && candidate <= KeyCode.Mouse6)
          continue;
        if (candidate >= KeyCode.JoystickButton0)
          continue;
        if (!Input.GetKeyDown(candidate))
          continue;

        ApplyRebinding(_rebindingActionId, candidate);
        return;
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ──────────────────────────────────────────────────────────────────────────
    private void HandleKeyboardKeyClicked(string actionId) => FocusEntry(actionId);

    private void HandleRebindRequested(string actionId)
    {
      if (_rebindingActionId != null && _rebindingActionId != actionId)
        CancelRebinding();

      _rebindingActionId = actionId;

      if (_actionIdToEntry.TryGetValue(actionId, out var el))
        el.SetRebinding(true);

      SetStatusText("새 키를 누르세요. (ESC: 취소)");
    }

    private void CancelRebinding()
    {
      if (_rebindingActionId == null)
        return;

      if (_actionIdToEntry.TryGetValue(_rebindingActionId, out var el))
        el.SetRebinding(false);

      _rebindingActionId = null;
      SetStatusText(string.Empty);
    }

    private void ApplyRebinding(string actionId, KeyCode newKey)
    {
      var entry = _bindings.Find(b => b.actionId == actionId);
      if (entry == null)
      {
        CancelRebinding();
        return;
      }

      entry.boundKey = newKey;

      KeyBindingRepository.SaveEntry(entry);
      KeyBindingRepository.Flush();

      if (_actionIdToEntry.TryGetValue(actionId, out var el))
      {
        el.SetRebinding(false);
        el.RefreshKeyLabel();
      }

      _keyboard?.SetBindings(_bindings);
      _rebindingActionId = null;

      SetStatusText($"'{entry.actionDisplayName}' 키가 변경되었습니다.");
    }

    private void HandleResetClicked()
    {
      if (_defaultBindings == null)
        return;

      CancelRebinding();
      RestoreDefaultKeyBindings();
      KeyBindingRepository.DeleteAll(_bindings);
      PopulateKeyList(_bindings);
      SetStatusText("키 설정을 기본값으로 초기화했습니다.");
    }

    /// <summary>메모리에 올라온 바인딩을 기본값으로 되돌립니다. 저장소와 화면은 건드리지 않습니다.</summary>
    private void RestoreDefaultKeyBindings()
    {
      if (_defaultBindings == null)
        return;

      for (int i = 0; i < _bindings.Count; i++)
      {
        var def = _defaultBindings.Find(d => d.actionId == _bindings[i].actionId);
        if (def != null)
          _bindings[i].boundKey = def.boundKey;
      }
    }

    private void HandleEntryClicked(string actionId)
    {
      foreach (var el in _entryElements)
        el.SetSelected(false);

      if (_actionIdToEntry.TryGetValue(actionId, out var entry))
        entry.SetSelected(true);

      var binding = _bindings.Find(b => b.actionId == actionId);
      if (binding != null && binding.boundKey != KeyCode.None)
        _keyboard?.HighlightKey(binding.boundKey);
      else
        _keyboard?.ClearAllHighlights();
    }

    private void FocusEntry(string actionId)
    {
      foreach (var el in _entryElements)
        el.SetFocused(false);

      if (!_actionIdToEntry.TryGetValue(actionId, out var target))
        return;

      target.SetFocused(true);

      target.schedule.Execute(() =>
      {
        if (_keyScroll == null)
          return;

        var itemPos = target.worldBound;
        var scrollPos = _keyScroll.worldBound;
        if (itemPos == Rect.zero || scrollPos == Rect.zero)
          return;

        float itemTop = itemPos.y - scrollPos.y + _keyScroll.scrollOffset.y;
        float itemBot = itemTop + itemPos.height;
        float viewTop = _keyScroll.scrollOffset.y;
        float viewBot = viewTop + _keyScroll.contentViewport.layout.height;

        if (itemTop < viewTop)
          _keyScroll.scrollOffset = new Vector2(0f, itemTop - 4f);
        else if (itemBot > viewBot)
          _keyScroll.scrollOffset = new Vector2(0f, itemBot - _keyScroll.contentViewport.layout.height + 4f);
      }).StartingIn(0);
    }
  }
}
