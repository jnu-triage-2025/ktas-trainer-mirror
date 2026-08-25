using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 키보드 레이아웃을 시각화하는 VisualElement입니다.
  /// 각 키를 흰색 배경 + 회색 테두리 VisualElement로 직접 그립니다.
  ///
  /// 레이어 구성:
  ///   Layer 0 - 키 오버레이 (할당된 기능 레이블, 클릭 영역)
  ///   Layer 1 - 키 이름 레이블 (Q, W, E … 등 작은 텍스트)
  /// </summary>
  [UxmlElement]
  public partial class KeyboardLayoutElement : VisualElement
  {
    // ──────────────────────────────────────────────────────────────────────────
    // 키 배치 기준 치수 — 키 위치 퍼센트 계산에 사용하는 내부 레이아웃 단위
    // 키 좌표 원본 기준: 1245 × 381 (가로/세로 전체 레이아웃 크기)
    // ──────────────────────────────────────────────────────────────────────────
    private const float LayoutW = 1245f;
    private const float LayoutH = 381f;
    private const float LayoutOffsetX = 15f;
    private const float LayoutOffsetY = 15f;

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트 – 키보드에서 할당된 키를 클릭할 때 발생 (actionId 전달)
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>할당된 키를 클릭했을 때 해당 기능의 actionId를 인자로 전달합니다.</summary>
    public event Action<string> OnAssignedKeyClicked;

    /// <summary>할당 여부에 관계없이 키를 클릭할 때 KeyCode를 전달합니다.</summary>
    public event Action<KeyCode> OnKeyClicked;

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 상태
    // ──────────────────────────────────────────────────────────────────────────
    private readonly Dictionary<KeyCode, VisualElement> _keyOverlays = new();
    private readonly Dictionary<KeyCode, string> _keyToActionId = new();
    private readonly Dictionary<KeyCode, string> _keyToActionDisplay = new();

    // ──────────────────────────────────────────────────────────────────────────
    // 생성자
    // ──────────────────────────────────────────────────────────────────────────
    public KeyboardLayoutElement()
    {
      AddToClassList("keyboard-layout");
      RegisterCallback<AttachToPanelEvent>(OnAttach);
      RegisterCallback<DetachFromPanelEvent>(OnDetach);
    }

    // 키보드 높이는 USS의 .key-config__keyboard { height: ... } 로 직접 조정합니다.
    // C#에서는 높이를 건드리지 않습니다.

    private void OnAttach(AttachToPanelEvent _) => Build();
    private void OnDetach(DetachFromPanelEvent _) { }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 키 바인딩 목록으로 오버레이를 갱신합니다.
    /// 이미 화면에 올라온 후 호출해도 됩니다.
    /// </summary>
    public void SetBindings(IEnumerable<KeyBindingEntry> entries)
    {
      _keyToActionId.Clear();
      _keyToActionDisplay.Clear();

      if (entries != null)
      {
        foreach (var entry in entries)
        {
          if (entry.boundKey == KeyCode.None)
            continue;
          _keyToActionId[entry.boundKey] = entry.actionId;
          _keyToActionDisplay[entry.boundKey] = entry.actionDisplayName;
        }
      }

      RefreshOverlays();
    }

    /// <summary>특정 키를 강조(하이라이트) 표시합니다.</summary>
    public void HighlightKey(KeyCode key)
    {
      ClearAllHighlights();
      if (_keyOverlays.TryGetValue(key, out var overlay))
      {
        overlay.AddToClassList("keyboard-layout__key--highlighted");
      }
    }

    /// <summary>모든 강조 표시를 제거합니다.</summary>
    public void ClearAllHighlights()
    {
      foreach (var overlay in _keyOverlays.Values)
      {
        overlay.RemoveFromClassList("keyboard-layout__key--highlighted");
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 초기 빌드
    // ──────────────────────────────────────────────────────────────────────────
    private void Build()
    {
      Clear();
      _keyOverlays.Clear();

      // ── 오버레이 컨테이너 ─────────────────────────────────────────────────
      var overlayContainer = new VisualElement();
      overlayContainer.AddToClassList("keyboard-layout__overlay-container");
      Add(overlayContainer);

      // ── 각 키 오버레이 생성 ───────────────────────────────────────────────
      foreach (var info in KeyInfoTable)
      {
        var keyEl = BuildKeyOverlay(info);
        overlayContainer.Add(keyEl);
        _keyOverlays[info.KeyCode] = keyEl;
      }

      // Build() 전에 SetBindings()가 호출된 경우 여기서 반영
      RefreshOverlays();
    }

    private VisualElement BuildKeyOverlay(in KeyInfo info)
    {
      // 백분율 좌표 계산
      float left = (info.X + LayoutOffsetX) / LayoutW * 100f;
      float top = (info.Y + LayoutOffsetY) / LayoutH * 100f;
      float w = info.W / LayoutW * 100f;
      float h = info.H / LayoutH * 100f;

      var keyEl = new VisualElement();
      keyEl.AddToClassList("keyboard-layout__key");

      keyEl.style.position = Position.Absolute;
      keyEl.style.left = Length.Percent(left);
      keyEl.style.top = Length.Percent(top);
      keyEl.style.width = Length.Percent(w);
      keyEl.style.height = Length.Percent(h);

      // 키 이름 레이블 (하단 우측, 작은 텍스트)
      var nameLabel = new Label(info.Label);
      nameLabel.AddToClassList("keyboard-layout__key-name");
      keyEl.Add(nameLabel);

      // 기능 레이블 (중앙, 할당 시 표시)
      var actionLabel = new Label();
      actionLabel.AddToClassList("keyboard-layout__key-action");
      actionLabel.style.display = DisplayStyle.None;
      keyEl.Add(actionLabel);

      // 클릭 이벤트
      var keyCode = info.KeyCode;
      keyEl.RegisterCallback<ClickEvent>(_ => HandleKeyClick(keyCode));

      return keyEl;
    }

    private void HandleKeyClick(KeyCode keyCode)
    {
      OnKeyClicked?.Invoke(keyCode);

      if (_keyToActionId.TryGetValue(keyCode, out var actionId))
      {
        OnAssignedKeyClicked?.Invoke(actionId);
      }
    }

    private void RefreshOverlays()
    {
      foreach (var (keyCode, overlay) in _keyOverlays)
      {
        var actionLabel = overlay.Q<Label>(className: "keyboard-layout__key-action");
        if (actionLabel == null)
          continue;

        if (_keyToActionDisplay.TryGetValue(keyCode, out var displayName))
        {
          actionLabel.text = displayName;
          actionLabel.style.display = DisplayStyle.Flex;
          overlay.AddToClassList("keyboard-layout__key--assigned");
        }
        else
        {
          actionLabel.text = string.Empty;
          actionLabel.style.display = DisplayStyle.None;
          overlay.RemoveFromClassList("keyboard-layout__key--assigned");
        }
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 키 위치 데이터 표 (레이아웃 기준 좌표)
    // 좌표 의미: X, Y = 키 사각형의 좌상단 (내부 그룹 기준, 오프셋 15px 별도 적용)
    //            W, H = 키 너비/높이 (레이아웃 단위 px)
    // ──────────────────────────────────────────────────────────────────────────
    private readonly struct KeyInfo
    {
      public readonly KeyCode KeyCode;
      public readonly string Label;
      public readonly float X;
      public readonly float Y;
      public readonly float W;
      public readonly float H;

      public KeyInfo(KeyCode keyCode, string label, float x, float y, float w, float h)
      {
        KeyCode = keyCode;
        Label = label;
        X = x;
        Y = y;
        W = w;
        H = h;
      }
    }

    // 52×52 기본 키 높이/너비
    private static readonly KeyInfo[] KeyInfoTable =
    {
      // ── 기능 키 행 (y=1) ────────────────────────────────────────────────
      new(KeyCode.Escape,       "Esc",    1f,     1f,     52f,   52f),
      new(KeyCode.F1,           "F1",     109f,   1f,     52f,   52f),
      new(KeyCode.F2,           "F2",     163f,   1f,     52f,   52f),
      new(KeyCode.F3,           "F3",     217f,   1f,     52f,   52f),
      new(KeyCode.F4,           "F4",     271f,   1f,     52f,   52f),
      new(KeyCode.F5,           "F5",     352f,   1f,     52f,   52f),
      new(KeyCode.F6,           "F6",     406f,   1f,     52f,   52f),
      new(KeyCode.F7,           "F7",     460f,   1f,     52f,   52f),
      new(KeyCode.F8,           "F8",     514f,   1f,     52f,   52f),
      new(KeyCode.F9,           "F9",     595f,   1f,     52f,   52f),
      new(KeyCode.F10,          "F10",    649f,   1f,     52f,   52f),
      new(KeyCode.F11,          "F11",    703f,   1f,     52f,   52f),
      new(KeyCode.F12,          "F12",    757f,   1f,     52f,   52f),
      new(KeyCode.Print,        "PrtSc",  824.5f, 1f,     52f,   52f),
      new(KeyCode.ScrollLock,   "ScrLk",  878.5f, 1f,     52f,   52f),
      new(KeyCode.Pause,        "Pause",  932.5f, 1f,     52f,   52f),

      // ── 숫자 행 (y=82) ──────────────────────────────────────────────────
      new(KeyCode.BackQuote,    "~",      1f,     82f,    52f,   52f),
      new(KeyCode.Alpha1,       "1",      55f,    82f,    52f,   52f),
      new(KeyCode.Alpha2,       "2",      109f,   82f,    52f,   52f),
      new(KeyCode.Alpha3,       "3",      163f,   82f,    52f,   52f),
      new(KeyCode.Alpha4,       "4",      217f,   82f,    52f,   52f),
      new(KeyCode.Alpha5,       "5",      271f,   82f,    52f,   52f),
      new(KeyCode.Alpha6,       "6",      325f,   82f,    52f,   52f),
      new(KeyCode.Alpha7,       "7",      379f,   82f,    52f,   52f),
      new(KeyCode.Alpha8,       "8",      433f,   82f,    52f,   52f),
      new(KeyCode.Alpha9,       "9",      487f,   82f,    52f,   52f),
      new(KeyCode.Alpha0,       "0",      541f,   82f,    52f,   52f),
      new(KeyCode.Minus,        "-",      595f,   82f,    52f,   52f),
      new(KeyCode.Equals,       "=",      649f,   82f,    52f,   52f),
      new(KeyCode.Backspace,    "BSpc",   703f,   82f,    106f,  52f),
      new(KeyCode.Insert,       "Ins",    824.5f, 82f,    52f,   52f),
      new(KeyCode.Home,         "Home",   878.5f, 82f,    52f,   52f),
      new(KeyCode.PageUp,       "PgUp",   932.5f, 82f,    52f,   52f),
      new(KeyCode.Numlock,      "NmLk",   1000f,  82f,    52f,   52f),
      new(KeyCode.KeypadDivide, "/",      1054f,  82f,    52f,   52f),
      new(KeyCode.KeypadMultiply,"*",     1108f,  82f,    52f,   52f),
      new(KeyCode.KeypadMinus,  "-",      1162f,  82f,    52f,   52f),

      // ── QWERTY 행 (y=136) ───────────────────────────────────────────────
      new(KeyCode.Tab,          "Tab",    1f,     136f,   79f,   52f),
      new(KeyCode.Q,            "Q",      82f,    136f,   52f,   52f),
      new(KeyCode.W,            "W",      136f,   136f,   52f,   52f),
      new(KeyCode.E,            "E",      190f,   136f,   52f,   52f),
      new(KeyCode.R,            "R",      244f,   136f,   52f,   52f),
      new(KeyCode.T,            "T",      298f,   136f,   52f,   52f),
      new(KeyCode.Y,            "Y",      352f,   136f,   52f,   52f),
      new(KeyCode.U,            "U",      406f,   136f,   52f,   52f),
      new(KeyCode.I,            "I",      460f,   136f,   52f,   52f),
      new(KeyCode.O,            "O",      514f,   136f,   52f,   52f),
      new(KeyCode.P,            "P",      568f,   136f,   52f,   52f),
      new(KeyCode.LeftBracket,  "[",      622f,   136f,   52f,   52f),
      new(KeyCode.RightBracket, "]",      676f,   136f,   52f,   52f),
      new(KeyCode.Backslash,    "\\",     730f,   136f,   79f,   52f),
      new(KeyCode.Delete,       "Del",    824.5f, 136f,   52f,   52f),
      new(KeyCode.End,          "End",    878.5f, 136f,   52f,   52f),
      new(KeyCode.PageDown,     "PgDn",   932.5f, 136f,   52f,   52f),
      new(KeyCode.Keypad7,      "7",      1000f,  136f,   52f,   52f),
      new(KeyCode.Keypad8,      "8",      1054f,  136f,   52f,   52f),
      new(KeyCode.Keypad9,      "9",      1108f,  136f,   52f,   52f),
      new(KeyCode.KeypadPlus,   "+",      1162f,  136f,   52f,   106f), // 2행 걸침

      // ── 홈 행 (y=190) ───────────────────────────────────────────────────
      new(KeyCode.CapsLock,     "Caps",   1f,     190f,   92.5f, 52f),
      new(KeyCode.A,            "A",      95.5f,  190f,   52f,   52f),
      new(KeyCode.S,            "S",      149.5f, 190f,   52f,   52f),
      new(KeyCode.D,            "D",      203.5f, 190f,   52f,   52f),
      new(KeyCode.F,            "F",      257.5f, 190f,   52f,   52f),
      new(KeyCode.G,            "G",      311.5f, 190f,   52f,   52f),
      new(KeyCode.H,            "H",      365.5f, 190f,   52f,   52f),
      new(KeyCode.J,            "J",      419.5f, 190f,   52f,   52f),
      new(KeyCode.K,            "K",      473.5f, 190f,   52f,   52f),
      new(KeyCode.L,            "L",      527.5f, 190f,   52f,   52f),
      new(KeyCode.Semicolon,    ";",      581.5f, 190f,   52f,   52f),
      new(KeyCode.Quote,        "'",      635.5f, 190f,   52f,   52f),
      new(KeyCode.Return,       "Enter",  689.5f, 190f,   119.5f,52f),
      new(KeyCode.Keypad4,      "4",      1000f,  190f,   52f,   52f),
      new(KeyCode.Keypad5,      "5",      1054f,  190f,   52f,   52f),
      new(KeyCode.Keypad6,      "6",      1108f,  190f,   52f,   52f),

      // ── Shift 행 (y=244) ────────────────────────────────────────────────
      new(KeyCode.LeftShift,    "Shift",  1f,     244f,   119.5f,52f),
      new(KeyCode.Z,            "Z",      122.5f, 244f,   52f,   52f),
      new(KeyCode.X,            "X",      176.5f, 244f,   52f,   52f),
      new(KeyCode.C,            "C",      230.5f, 244f,   52f,   52f),
      new(KeyCode.V,            "V",      284.5f, 244f,   52f,   52f),
      new(KeyCode.B,            "B",      338.5f, 244f,   52f,   52f),
      new(KeyCode.N,            "N",      392.5f, 244f,   52f,   52f),
      new(KeyCode.M,            "M",      446.5f, 244f,   52f,   52f),
      new(KeyCode.Comma,        ",",      500.5f, 244f,   52f,   52f),
      new(KeyCode.Period,       ".",      554.5f, 244f,   52f,   52f),
      new(KeyCode.Slash,        "/",      608.5f, 244f,   52f,   52f),
      new(KeyCode.RightShift,   "Shift",  662.5f, 244f,   146.5f,52f),
      new(KeyCode.UpArrow,      "Up",     878.5f, 244f,   52f,   52f),
      new(KeyCode.Keypad1,      "1",      1000f,  244f,   52f,   52f),
      new(KeyCode.Keypad2,      "2",      1054f,  244f,   52f,   52f),
      new(KeyCode.Keypad3,      "3",      1108f,  244f,   52f,   52f),
      new(KeyCode.KeypadEnter,  "Enter",  1162f,  244f,   52f,   106f), // 2행 걸침

      // ── 컨트롤 행 (y=298) ───────────────────────────────────────────────
      new(KeyCode.LeftControl,  "Ctrl",   1f,     298f,   65.5f, 52f),
      new(KeyCode.LeftWindows,  "Win",    68.5f,  298f,   65.5f, 52f),
      new(KeyCode.LeftAlt,      "Alt",    136f,   298f,   65.5f, 52f),
      new(KeyCode.Space,        "Space",  203.5f, 298f,   335.5f,52f),
      new(KeyCode.RightAlt,     "Alt",    541f,   298f,   65.5f, 52f),
      new(KeyCode.RightWindows, "Win",    608.5f, 298f,   65.5f, 52f),
      new(KeyCode.Menu,         "Menu",   676f,   298f,   65.5f, 52f),
      new(KeyCode.RightControl, "Ctrl",   743.5f, 298f,   65.5f, 52f),
      new(KeyCode.LeftArrow,    "Left",   824.5f, 298f,   52f,   52f),
      new(KeyCode.DownArrow,    "Down",   878.5f, 298f,   52f,   52f),
      new(KeyCode.RightArrow,   "Right",  932.5f, 298f,   52f,   52f),
      new(KeyCode.Keypad0,      "0",      1000f,  298f,   106f,  52f),
      new(KeyCode.KeypadPeriod, ".",      1108f,  298f,   52f,   52f),
    };
  }
}
