using TriageTrainer.Definitions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TriageTrainer.Scripts.HIDInput
{
  public class KeyboardConfigurationRegistry : MonoBehaviour
  {
    public KeyCode InteractInteractiveGameObject = DefaultsKeyConfiguration.InteractInteractiveGameObject;
    public KeyCode OpenChatUI = DefaultsKeyConfiguration.OpenChatUI;
    public KeyCode SendChat = DefaultsKeyConfiguration.SendChat;
    public KeyCode CloseChatUI = DefaultsKeyConfiguration.CloseChatUI;
    
    private static KeyboardConfigurationRegistry _instance;
    public static KeyboardConfigurationRegistry Instance
    {
      get
      {
        if (_instance == null)
        {
          GameObject go = new GameObject("KeyboardConfigurationRegistry");
          DontDestroyOnLoad(go);
          _instance = go.AddComponent<KeyboardConfigurationRegistry>();
        }
        return _instance;
      }
    }
  }
}
