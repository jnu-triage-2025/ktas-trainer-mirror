using TriageTrainer.Definitions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TriageTrainer.Registry
{
  public class KeyboardConfigurationRegistry : MonoBehaviour
  {
    public static KeyCode InteractInteractableObject = DefaultsKeyConfiguration.InteractInteractableObject;
    public static KeyCode OpenChatUI = DefaultsKeyConfiguration.OpenChatUI;
    public static KeyCode SendChat = DefaultsKeyConfiguration.SendChat;
    public static KeyCode CloseChatUI = DefaultsKeyConfiguration.CloseChatUI;
  }
}
