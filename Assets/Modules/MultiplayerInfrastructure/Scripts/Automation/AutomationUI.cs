#if UNITY_E2E || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Automation
{
  internal sealed class AutomationUI : IDisposable
  {
    private Mouse _mouse;
    private Keyboard _keyboard;
    private InputSystemUIInputModule _module;
    private GameObject _createdSystem;
    private readonly List<BaseInputModule> _disabled = new();
    private Vector2 _position;
    private bool _pressed;
    private double _pressedUntil;
#if UNITY_EDITOR
    private InputSettings _originalInputSettings;
    private InputSettings _automationInputSettings;
#endif
    internal JObject Diagnostics()
    {
      var position = _mouse != null ? _mouse.position.ReadValue() : Vector2.zero;
      return new JObject {
        ["applicationFocused"] = Application.isFocused,
        ["runInBackground"] = Application.runInBackground,
        ["backgroundBehavior"] = InputSystem.settings.backgroundBehavior.ToString(),
        ["eventSystemFocused"] = EventSystem.current != null && EventSystem.current.isFocused,
        ["moduleReady"] = IsReady,
        ["activeModule"] = EventSystem.current?.currentInputModule?.GetType().Name,
        ["cursorLock"] = UnityEngine.Cursor.lockState.ToString(),
        ["eventSystems"] = new JArray(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(system => new JObject {
          ["name"] = system.name, ["scene"] = system.gameObject.scene.name,
          ["active"] = system.isActiveAndEnabled, ["current"] = system == EventSystem.current,
          ["modules"] = new JArray(system.GetComponents<BaseInputModule>().Select(module => new JObject {
            ["type"] = module.GetType().Name, ["enabled"] = module.isActiveAndEnabled,
            ["pointEnabled"] = (module as InputSystemUIInputModule)?.point?.action?.enabled,
            ["clickEnabled"] = (module as InputSystemUIInputModule)?.leftClick?.action?.enabled
          }))
        })),
        ["panelRaycasters"] = new JArray(UnityEngine.Object.FindObjectsByType<PanelRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(raycaster => new JObject {
          ["name"] = raycaster.name, ["scene"] = raycaster.gameObject.scene.name,
          ["active"] = raycaster.isActiveAndEnabled, ["hasPanel"] = raycaster.panel != null
        })),
        ["virtualMouseEnabled"] = _mouse != null && _mouse.enabled,
        ["virtualKeyboardEnabled"] = _keyboard != null && _keyboard.enabled,
        ["virtualMouseCanRunInBackground"] = _mouse != null && _mouse.canRunInBackground,
        ["virtualKeyboardCanRunInBackground"] = _keyboard != null && _keyboard.canRunInBackground,
        ["virtualMousePosition"] = new JArray(position.x, position.y),
        ["requestedMousePosition"] = new JArray(_position.x, _position.y),
        ["virtualMousePressed"] = _mouse != null && _mouse.leftButton.isPressed,
        ["requestedPressed"] = _pressed,
        ["screenWidth"] = Screen.width, ["screenHeight"] = Screen.height
      };
    }

    internal static IEnumerable<(UIDocument doc, VisualElement element)> Elements(string document, string name, bool includeUnnamedText = false)
    {
      foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
      {
        if (document != null && doc.gameObject.name != document) continue;
        if (doc.rootVisualElement == null) continue;
        foreach (var element in doc.rootVisualElement.Query<VisualElement>().ToList())
          if ((!string.IsNullOrEmpty(element.name) || (includeUnnamedText && element is TextElement))
            && (name == null || element.name == name)) yield return (doc, element);
      }
    }
    internal static bool Visible(VisualElement e)
    {
      for (var p = e; p != null; p = p.parent)
        if (p.resolvedStyle.display == DisplayStyle.None || p.resolvedStyle.visibility == Visibility.Hidden || p.resolvedStyle.opacity <= 0) return false;
      return e.panel != null && e.worldBound.width > 0 && e.worldBound.height > 0;
    }
    internal static bool CenterOnScreen(VisualElement element)
    {
      if (element.panel == null) return false;
      var a = RuntimePanelUtils.ScreenToPanel(element.panel, Vector2.zero);
      var b = RuntimePanelUtils.ScreenToPanel(element.panel, new Vector2(Screen.width, Screen.height));
      var center = element.worldBound.center;
      return center.x >= Mathf.Min(a.x, b.x) && center.x < Mathf.Max(a.x, b.x)
        && center.y >= Mathf.Min(a.y, b.y) && center.y < Mathf.Max(a.y, b.y);
    }
    internal static JArray Query(JObject args)
    {
      var result = new JArray();
      foreach (var pair in Elements((string)args["documentId"], (string)args["automationId"], includeUnnamedText: true).Take(2000))
      {
        var e = pair.element;
        var rect = e.worldBound;
        var origin = e.panel != null ? RuntimePanelUtils.ScreenToPanel(e.panel, Vector2.zero) : Vector2.zero;
        var corner = e.panel != null ? RuntimePanelUtils.ScreenToPanel(e.panel, new Vector2(Screen.width, Screen.height)) : Vector2.one;
        var scale = corner - origin;
        ScrollView scroll = null;
        for (var ancestor = e.parent; ancestor != null; ancestor = ancestor.parent)
          if (ancestor is ScrollView candidate && CenterOnScreen(candidate)) { scroll = candidate; break; }
        var picked = e.panel?.Pick(rect.center);
        bool hit = picked == e || (picked != null && e.Contains(picked));
        result.Add(new JObject {
          ["documentId"] = pair.doc.gameObject.name, ["automationId"] = e.name,
          ["elementType"] = e.GetType().Name, ["text"] = (e as TextElement)?.text,
          ["value"] = e is TextField text ? text.value : e is Slider slider ? JToken.FromObject(slider.value) : e is Toggle toggle ? JToken.FromObject(toggle.value) : e is DropdownField dropdown ? dropdown.value : null,
          ["visible"] = Visible(e), ["enabled"] = e.enabledInHierarchy,
          ["interactable"] = Visible(e) && e.enabledInHierarchy && hit && CenterOnScreen(e),
          ["scrollContainerId"] = scroll?.name,
          ["screenWidth"] = Screen.width, ["screenHeight"] = Screen.height,
          ["screenCenter"] = new JObject {
            ["x"] = scale.x != 0 ? (rect.center.x - origin.x) / scale.x * Screen.width : 0,
            ["y"] = scale.y != 0 ? Screen.height - (rect.center.y - origin.y) / scale.y * Screen.height : 0
          },
          ["focused"] = e.focusController?.focusedElement == e,
          ["panelBounds"] = new JObject { ["x"] = rect.x, ["y"] = rect.y, ["width"] = rect.width, ["height"] = rect.height },
          ["uiRevision"] = Time.frameCount
        });
      }
      return result;
    }
    private static void EnsureDeviceEnabled(InputDevice device)
    {
      if (device.enabled) return;
      InputSystem.EnableDevice(device);
      if (!device.enabled) throw new InvalidOperationException("AUTOMATION_DEVICE_DISABLED");
    }
    private static void RegisterLayouts()
    {
      InputSystem.RegisterLayout(@"{""name"":""E2EBackgroundMouse"",""extend"":""Mouse"",""runInBackground"":""enabled""}");
      InputSystem.RegisterLayout(@"{""name"":""E2EBackgroundKeyboard"",""extend"":""Keyboard"",""runInBackground"":""enabled""}");
    }
    private void EnsureDevices()
    {
#if UNITY_EDITOR
      if (_automationInputSettings == null)
      {
        _originalInputSettings = InputSystem.settings;
        _automationInputSettings = UnityEngine.Object.Instantiate(_originalInputSettings);
        _automationInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        _automationInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = _automationInputSettings;
      }
#endif
      if (_mouse == null || _keyboard == null) RegisterLayouts();
      if (_mouse == null) {
        _mouse = (Mouse)InputSystem.AddDevice("E2EBackgroundMouse", "E2E Mouse");
      }
      if (_keyboard == null) {
        _keyboard = (Keyboard)InputSystem.AddDevice("E2EBackgroundKeyboard", "E2E Keyboard");
      }
      EnsureDeviceEnabled(_mouse);
      EnsureDeviceEnabled(_keyboard);
      if (_module != null && _module.isActiveAndEnabled) return;
      var system = EventSystem.current;
      if (system == null)
      {
        _createdSystem = new GameObject("E2E EventSystem");
        UnityEngine.Object.DontDestroyOnLoad(_createdSystem);
        system = _createdSystem.AddComponent<EventSystem>();
      }
      foreach (var module in system.GetComponents<BaseInputModule>())
        if (module.enabled) { _disabled.Add(module); module.enabled = false; }
      _module = system.gameObject.AddComponent<InputSystemUIInputModule>();
      _module.AssignDefaultActions();
      _module.actionsAsset.devices = new InputDevice[] { _mouse, _keyboard };
    }
    internal bool IsReady => _module != null && _module.isActiveAndEnabled
      && EventSystem.current != null && EventSystem.current.currentInputModule == _module;
    internal void Move(Vector2 screenPosition, bool pressed)
    {
      EnsureDevices();
      _position = screenPosition;
      _pressed = pressed;
      _pressedUntil = pressed ? Time.realtimeSinceStartupAsDouble + 2 : 0;
      InputSystem.QueueStateEvent(_mouse, new MouseState { position = _position, buttons = (ushort)(pressed ? 1 : 0) });
    }
    internal static JObject PointerButtonTarget(Vector2 screenPosition)
    {
      var matches = new List<JObject>();
      foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
      {
        var root = doc.rootVisualElement;
        if (root?.panel == null) continue;
        var position = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
        var hit = root.panel.Pick(position);
        if (hit == null || !(root == hit || root.Contains(hit))) continue;
        var button = hit as Button ?? hit.GetFirstAncestorOfType<Button>();
        if (button == null || string.IsNullOrEmpty(button.name) || !Visible(button) || !button.enabledInHierarchy) return null;
        if (Elements(doc.gameObject.name, button.name).Count(pair => Visible(pair.element)) != 1) return null;
        matches.Add(new JObject { ["documentId"] = doc.gameObject.name, ["automationId"] = button.name, ["elementType"] = "Button" });
      }
      // A point that hits multiple panels is ambiguous; never guess their rendering order.
      return matches.Count == 1 ? matches[0] : null;
    }
    internal void Activate(JObject args, bool pressed = true)
    {
      var matches = Elements((string)args["documentId"], (string)args["automationId"])
        .Where(p => Visible(p.element)).ToArray();
      if (matches.Length != 1) throw new InvalidOperationException(matches.Length == 0 ? "TARGET_NOT_FOUND" : "AMBIGUOUS_TARGET");
      var pair = matches[0];
      var e = pair.element;
      var hit = e.panel.Pick(e.worldBound.center);
      if (!CenterOnScreen(e) || !e.enabledInHierarchy || !(hit == e || (hit != null && e.Contains(hit)))) throw new InvalidOperationException("TARGET_NOT_INTERACTABLE");
      // Runtime panel mapping is affine for screen-space panels. Invert using sampled screen points.
      var origin = RuntimePanelUtils.ScreenToPanel(e.panel, Vector2.zero);
      var corner = RuntimePanelUtils.ScreenToPanel(e.panel, new Vector2(Screen.width, Screen.height));
      var p = e.worldBound.center - origin;
      var scale = corner - origin;
      var screen = new Vector2(p.x / scale.x * Screen.width, Screen.height - p.y / scale.y * Screen.height);
      Move(screen, pressed);
    }
    internal void Scroll(Vector2 notches)
    {
      EnsureDevices();
      InputSystem.QueueStateEvent(_mouse, new MouseState {
        position = _position, buttons = (ushort)(_pressed ? 1 : 0),
        scroll = notches * 120f
      });
    }
    internal void ExpirePointer(double now)
    {
      if (_pressed && now >= _pressedUntil) ReleasePointer();
    }
    internal void ReleasePointer()
    {
      _pressed = false; _pressedUntil = 0;
      if (_mouse != null) InputSystem.QueueStateEvent(_mouse, new MouseState { position = _position, buttons = 0 });
    }
    internal void Text(string text)
    {
      var fields = Elements(null, null).Select(pair => pair.element).OfType<TextField>()
        .Where(field => Visible(field) && field.enabledInHierarchy &&
          (field.focusController?.focusedElement == field ||
            (field.focusController?.focusedElement is VisualElement focused && field.Contains(focused)))).ToArray();
      if (fields.Length != 1) throw new InvalidOperationException("TARGET_NOT_INTERACTABLE");
      var target = fields[0].Q<VisualElement>(className: TextInputBaseField<string>.textInputUssName) ?? fields[0];
      // This is explicitly input_adapter mode: use normal TextField editing events, never assign its value.
      // InputSystem 1.14's UI module does not route virtual Keyboard text events to the legacy IMGUI text backend.
      foreach (char character in text)
      {
        using var down = KeyDownEvent.GetPooled(character, KeyCode.None, EventModifiers.None);
        target.SendEvent(down);
        using var up = KeyUpEvent.GetPooled(character, KeyCode.None, EventModifiers.None);
        target.SendEvent(up);
      }
    }
    public void Dispose()
    {
      if (_mouse != null) InputSystem.RemoveDevice(_mouse);
      if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
      if (_module != null) UnityEngine.Object.Destroy(_module);
      foreach (var module in _disabled) if (module != null) module.enabled = true;
      if (_createdSystem != null) UnityEngine.Object.Destroy(_createdSystem);
      _disabled.Clear(); _mouse = null; _keyboard = null; _module = null; _createdSystem = null;
#if UNITY_EDITOR
      if (_automationInputSettings != null)
      {
        if (InputSystem.settings == _automationInputSettings) InputSystem.settings = _originalInputSettings;
        UnityEngine.Object.Destroy(_automationInputSettings);
        _automationInputSettings = null;
        _originalInputSettings = null;
      }
#endif
    }
  }
}
#endif
