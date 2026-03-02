using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 키 설정 목록의 항목 하나를 나타내는 VisualElement입니다.
  /// 좌측에 기능 이름, 우측에 할당된 키 이름을 표시하며,
  /// 강조(Focused) 상태와 선택(Selected) 상태를 지원합니다.
  /// </summary>
  [UxmlElement]
  public partial class KeyConfigEntryElement : VisualElement
  {
    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>항목을 클릭했을 때 해당 항목의 actionId가 전달됩니다.</summary>
    public event Action<string> OnEntryClicked;

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 요소
    // ──────────────────────────────────────────────────────────────────────────
    private Label _actionLabel;
    private Label _keyLabel;
    private KeyBindingEntry _entry;

    // ──────────────────────────────────────────────────────────────────────────
    // 생성자
    // ──────────────────────────────────────────────────────────────────────────
    public KeyConfigEntryElement()
    {
      AddToClassList("key-config-entry");
      BuildLayout();
      RegisterCallback<ClickEvent>(OnClick);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>항목 데이터를 바인딩하고 UI를 갱신합니다.</summary>
    public void Bind(KeyBindingEntry entry)
    {
      _entry = entry;
      _actionLabel.text = entry.actionDisplayName ?? entry.actionId ?? string.Empty;
      _keyLabel.text = entry.boundKey == KeyCode.None ? "—" : KeyCodeToLabel(entry.boundKey);
    }

    /// <summary>포커스(강조) 상태를 설정합니다. 스크롤 이동은 호출자가 담당합니다.</summary>
    public void SetFocused(bool focused)
    {
      if (focused)
        AddToClassList("key-config-entry--focused");
      else
        RemoveFromClassList("key-config-entry--focused");
    }

    /// <summary>선택 상태를 설정합니다.</summary>
    public void SetSelected(bool selected)
    {
      if (selected)
        AddToClassList("key-config-entry--selected");
      else
        RemoveFromClassList("key-config-entry--selected");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 레이아웃 구성
    // ──────────────────────────────────────────────────────────────────────────
    private void BuildLayout()
    {
      _actionLabel = new Label();
      _actionLabel.AddToClassList("key-config-entry__action");

      _keyLabel = new Label();
      _keyLabel.AddToClassList("key-config-entry__key");

      Add(_actionLabel);
      Add(_keyLabel);
    }

    private void OnClick(ClickEvent _)
    {
      if (_entry != null)
        OnEntryClicked?.Invoke(_entry.actionId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // KeyCode → 표시 문자열 변환
    // ──────────────────────────────────────────────────────────────────────────
    private static string KeyCodeToLabel(KeyCode key)
    {
      return key switch
      {
        KeyCode.Alpha0 => "0", KeyCode.Alpha1 => "1", KeyCode.Alpha2 => "2",
        KeyCode.Alpha3 => "3", KeyCode.Alpha4 => "4", KeyCode.Alpha5 => "5",
        KeyCode.Alpha6 => "6", KeyCode.Alpha7 => "7", KeyCode.Alpha8 => "8",
        KeyCode.Alpha9 => "9",
        KeyCode.BackQuote => "~",
        KeyCode.Minus => "-", KeyCode.Equals => "=",
        KeyCode.LeftBracket => "[", KeyCode.RightBracket => "]",
        KeyCode.Backslash => "\\", KeyCode.Semicolon => ";", KeyCode.Quote => "'",
        KeyCode.Comma => ",", KeyCode.Period => ".", KeyCode.Slash => "/",
        KeyCode.Keypad0 => "Num 0", KeyCode.Keypad1 => "Num 1",
        KeyCode.Keypad2 => "Num 2", KeyCode.Keypad3 => "Num 3",
        KeyCode.Keypad4 => "Num 4", KeyCode.Keypad5 => "Num 5",
        KeyCode.Keypad6 => "Num 6", KeyCode.Keypad7 => "Num 7",
        KeyCode.Keypad8 => "Num 8", KeyCode.Keypad9 => "Num 9",
        KeyCode.KeypadDivide => "Num /", KeyCode.KeypadMultiply => "Num *",
        KeyCode.KeypadMinus => "Num -", KeyCode.KeypadPlus => "Num +",
        KeyCode.KeypadPeriod => "Num .", KeyCode.KeypadEnter => "Num Enter",
        KeyCode.UpArrow => "↑", KeyCode.DownArrow => "↓",
        KeyCode.LeftArrow => "←", KeyCode.RightArrow => "→",
        KeyCode.LeftShift => "L.Shift", KeyCode.RightShift => "R.Shift",
        KeyCode.LeftControl => "L.Ctrl", KeyCode.RightControl => "R.Ctrl",
        KeyCode.LeftAlt => "L.Alt", KeyCode.RightAlt => "R.Alt",
        KeyCode.LeftWindows => "L.Win", KeyCode.RightWindows => "R.Win",
        KeyCode.Return => "Enter",
        KeyCode.Backspace => "Backspace",
        KeyCode.Delete => "Delete", KeyCode.Insert => "Insert",
        KeyCode.Home => "Home", KeyCode.End => "End",
        KeyCode.PageUp => "Page Up", KeyCode.PageDown => "Page Down",
        KeyCode.Escape => "Esc",
        KeyCode.Tab => "Tab",
        KeyCode.CapsLock => "Caps Lock",
        KeyCode.Space => "Space",
        KeyCode.Print => "Print Screen",
        KeyCode.ScrollLock => "Scroll Lock",
        KeyCode.Pause => "Pause",
        KeyCode.Numlock => "Num Lock",
        KeyCode.Menu => "Menu",
        _ => key.ToString(),
      };
    }
  }
}
