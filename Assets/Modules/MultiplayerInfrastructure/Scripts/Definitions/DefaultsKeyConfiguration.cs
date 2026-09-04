using UnityEngine;

namespace MultiplayerInfrastructure.Definitions
{
  public class DefaultsKeyConfiguration
  {
    public const KeyCode InteractInteractableObject = KeyCode.F;
    public const KeyCode OpenChatUI = KeyCode.T;
    public const KeyCode OpenChatUIWithCommand = KeyCode.Slash;
    public const KeyCode SendChat = KeyCode.Return;
    public const KeyCode CloseChatUI = KeyCode.Escape;
    public const KeyCode OpenQuestUI = KeyCode.J;
    public const KeyCode DropHeldItem = KeyCode.Q;
    /// <summary>세션 접속자 목록 오버레이를 누르고 있는 동안 표시하는 키의 기본값입니다.</summary>
    public const KeyCode ShowPlayerList = KeyCode.Tab;
    /// <summary>접속자 목록 키의 KeyBindingRepository 액션 식별자입니다.</summary>
    public const string ShowPlayerListActionId = "player_list";
  }
}
