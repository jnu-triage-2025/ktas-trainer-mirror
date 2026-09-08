using UnityEngine;

namespace MultiplayerInfrastructure.Automation
{
  public interface IPlayerInputSource
  {
    float GetAxis(string name);
    float GetAxisRaw(string name);
    bool GetButton(string name);
    bool GetButtonDown(string name);
    bool GetButtonUp(string name);
    bool GetKey(KeyCode key);
    bool GetKeyDown(KeyCode key);
    bool GetKeyUp(KeyCode key);
    bool GetMouseButton(int button);
    bool GetMouseButtonDown(int button);
    bool GetMouseButtonUp(int button);
    Vector3 mousePosition { get; }
    Vector2 mouseScrollDelta { get; }
    bool anyKeyDown { get; }
  }

  public sealed class LegacyPlayerInputSource : IPlayerInputSource
  {
    public float GetAxis(string name) => UnityEngine.Input.GetAxis(name);
    public float GetAxisRaw(string name) => UnityEngine.Input.GetAxisRaw(name);
    public bool GetButton(string name) => UnityEngine.Input.GetButton(name);
    public bool GetButtonDown(string name) => UnityEngine.Input.GetButtonDown(name);
    public bool GetButtonUp(string name) => UnityEngine.Input.GetButtonUp(name);
    public bool GetKey(KeyCode key) => UnityEngine.Input.GetKey(key);
    public bool GetKeyDown(KeyCode key) => UnityEngine.Input.GetKeyDown(key);
    public bool GetKeyUp(KeyCode key) => UnityEngine.Input.GetKeyUp(key);
    public bool GetMouseButton(int button) => UnityEngine.Input.GetMouseButton(button);
    public bool GetMouseButtonDown(int button) => UnityEngine.Input.GetMouseButtonDown(button);
    public bool GetMouseButtonUp(int button) => UnityEngine.Input.GetMouseButtonUp(button);
    public Vector3 mousePosition => UnityEngine.Input.mousePosition;
    public Vector2 mouseScrollDelta => UnityEngine.Input.mouseScrollDelta;
    public bool anyKeyDown => UnityEngine.Input.anyKeyDown;
  }

  public static class PlayerInput
  {
    private static readonly IPlayerInputSource Legacy = new LegacyPlayerInputSource();
#if UNITY_E2E || UNITY_EDITOR
    internal static IPlayerInputSource Override;
    private static IPlayerInputSource Current => Override ?? Legacy;
#else
    private static IPlayerInputSource Current => Legacy;
#endif
    public static float GetAxis(string name) => Current.GetAxis(name);
    public static float GetAxisRaw(string name) => Current.GetAxisRaw(name);
    public static bool GetButton(string name) => Current.GetButton(name);
    public static bool GetButtonDown(string name) => Current.GetButtonDown(name);
    public static bool GetButtonUp(string name) => Current.GetButtonUp(name);
    public static bool GetKey(KeyCode key) => Current.GetKey(key);
    public static bool GetKeyDown(KeyCode key) => Current.GetKeyDown(key);
    public static bool GetKeyUp(KeyCode key) => Current.GetKeyUp(key);
    public static bool GetMouseButton(int button) => Current.GetMouseButton(button);
    public static bool GetMouseButtonDown(int button) => Current.GetMouseButtonDown(button);
    public static bool GetMouseButtonUp(int button) => Current.GetMouseButtonUp(button);
    public static Vector3 mousePosition => Current.mousePosition;
    public static Vector2 mouseScrollDelta => Current.mouseScrollDelta;
    public static bool anyKeyDown => Current.anyKeyDown;
  }
}
