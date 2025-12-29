using MultiplayerInfrastructure.Definitions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerInfrastructure.Registry
{
  public class KeyboardConfigurationRegistry : MonoBehaviour
  {
    public static KeyCode InteractInteractableObject = DefaultsKeyConfiguration.InteractInteractableObject;
    public static KeyCode OpenChatUI = DefaultsKeyConfiguration.OpenChatUI;
    public static KeyCode OpenChatUIWithCommand = DefaultsKeyConfiguration.OpenChatUIWithCommand;
    public static KeyCode SendChat = DefaultsKeyConfiguration.SendChat;
    public static KeyCode CloseChatUI = DefaultsKeyConfiguration.CloseChatUI;
    public static KeyCode OpenQuestUI = DefaultsKeyConfiguration.OpenQuestUI;
  }
}
