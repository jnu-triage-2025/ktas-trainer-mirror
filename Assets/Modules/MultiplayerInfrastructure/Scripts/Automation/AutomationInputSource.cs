#if UNITY_E2E || UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Automation
{
  // All mutations and snapshots are owned by the Unity main thread.
  public sealed class AutomationInputSource : IPlayerInputSource
  {
    private readonly HashSet<KeyCode> _held = new();
    private readonly HashSet<KeyCode> _down = new();
    private readonly HashSet<KeyCode> _up = new();
    private readonly Queue<(KeyCode key, bool pressed)> _transitions = new();
    private readonly Dictionary<KeyCode, double> _expiry = new();
    private readonly Dictionary<string, float> _axes = new();
    private Vector2 _pendingLook;
    private Vector2 _pendingScroll;
    private float _horizontal, _vertical;
    public Vector3 mousePosition { get; private set; }
    public Vector2 mouseScrollDelta { get; private set; }
    public bool anyKeyDown => _down.Count > 0;
    internal System.Collections.Generic.IEnumerable<KeyCode> DownKeys => _down;
    internal System.Collections.Generic.IEnumerable<KeyCode> UpKeys => _up;

    public void Enqueue(KeyCode key, bool pressed, double expiresAt)
    {
      if (_transitions.Count >= 256) throw new InvalidOperationException("INPUT_QUEUE_FULL");
      _transitions.Enqueue((key, pressed));
      if (pressed) _expiry[key] = expiresAt;
    }

    public void Look(Vector2 delta) => _pendingLook += delta;
    public void Renew(KeyCode key, double expiresAt) { if (_held.Contains(key)) _expiry[key] = expiresAt; }
    public void Scroll(Vector2 delta) => _pendingScroll += delta;
    public void Pointer(Vector2 position) => mousePosition = position;

    public void BeginFrame(double now, float deltaTime)
    {
      _down.Clear();
      _up.Clear();
      // A transition for the same key is delayed to the next frame, preserving taps.
      var changed = new HashSet<KeyCode>();
      int count = _transitions.Count;
      for (int i = 0; i < count; i++)
      {
        var next = _transitions.Peek();
        if (changed.Contains(next.key)) break;
        _transitions.Dequeue();
        changed.Add(next.key);
        if (next.pressed)
        {
          if (_expiry.TryGetValue(next.key, out double end) && end > now && _held.Add(next.key))
            _down.Add(next.key);
        }
        else if (_held.Remove(next.key)) _up.Add(next.key);
      }
      foreach (var pair in _expiry)
        if (pair.Value <= now && _held.Remove(pair.Key)) _up.Add(pair.Key);
      _axes["Mouse X"] = _pendingLook.x;
      _axes["Mouse Y"] = _pendingLook.y;
      mouseScrollDelta = _pendingScroll;
      _axes["Mouse ScrollWheel"] = _pendingScroll.y * 0.1f;
      _pendingLook = _pendingScroll = Vector2.zero;
      _horizontal = Smooth(_horizontal, GetAxisRaw("Horizontal"), deltaTime);
      _vertical = Smooth(_vertical, GetAxisRaw("Vertical"), deltaTime);
    }

    // InputManager.asset: sensitivity=3, gravity=3, snap=true for movement axes.
    private static float Smooth(float current, float target, float dt)
    {
      if (target != 0 && Mathf.Sign(current) != Mathf.Sign(target)) current = 0;
      return Mathf.MoveTowards(current, target, 3 * dt);
    }
    public float GetAxis(string name) => name == "Horizontal" ? _horizontal
      : name == "Vertical" ? _vertical : GetAxisRaw(name);
    public float GetAxisRaw(string name)
    {
      if (name == "Horizontal") return (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow) ? 1 : 0)
        - (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow) ? 1 : 0);
      if (name == "Vertical") return (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow) ? 1 : 0)
        - (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow) ? 1 : 0);
      return _axes.TryGetValue(name, out float value) ? value : 0;
    }
    private static KeyCode Button(string name) => name switch
    {
      "Jump" => KeyCode.Space,
      "Fire1" => KeyCode.Mouse0,
      "Fire2" => KeyCode.Mouse1,
      "Fire3" => KeyCode.LeftShift,
      "Submit" => KeyCode.Return,
      "Cancel" => KeyCode.Escape,
      _ => throw new ArgumentException("Unsupported button: " + name)
    };
    public bool GetButton(string name) => GetKey(Button(name));
    public bool GetButtonDown(string name) => GetKeyDown(Button(name));
    public bool GetButtonUp(string name) => GetKeyUp(Button(name));
    public bool GetKey(KeyCode key) => _held.Contains(key);
    public bool GetKeyDown(KeyCode key) => _down.Contains(key);
    public bool GetKeyUp(KeyCode key) => _up.Contains(key);
    public bool GetMouseButton(int button) => GetKey(KeyCode.Mouse0 + button);
    public bool GetMouseButtonDown(int button) => GetKeyDown(KeyCode.Mouse0 + button);
    public bool GetMouseButtonUp(int button) => GetKeyUp(KeyCode.Mouse0 + button);
    public void ReleaseAll()
    {
      _held.Clear(); _down.Clear(); _up.Clear(); _transitions.Clear(); _expiry.Clear(); _axes.Clear();
      _horizontal = _vertical = 0;
      _pendingLook = _pendingScroll = mouseScrollDelta = Vector2.zero;
    }
  }
}
#endif
