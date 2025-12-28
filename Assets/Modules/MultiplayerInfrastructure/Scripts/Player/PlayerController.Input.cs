using System;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Key Configuration")]
    [SerializeField] private KeyCode _keyMovingRunning = KeyCode.LeftControl;
    [SerializeField] private KeyCode _keyToggleInventory = KeyCode.E;
    [SerializeField] private KeyCode _keySwitchCameraViewMode = KeyCode.P;
    [SerializeField] private KeyCode _keyOpenEscMenu = KeyCode.Escape;
    [SerializeField] private KeyCode _keyToggleChat = KeyboardConfigurationRegistry.OpenChatUI;
    [SerializeField] private KeyCode _keyToggleCommand = KeyboardConfigurationRegistry.OpenChatUIWithCommand;
    [SerializeField] private KeyCode _keyInteractInteractableObject = KeyboardConfigurationRegistry.InteractInteractableObject;
    [SerializeField] private KeyCode _keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode _keySpectatorFlyDown = KeyCode.LeftShift;

    /**
     * 키 입력 핸들링을 막아야 하는 상황에서 이 플래그를 참으로 설정할 것
     * i.e. 채팅창 오픈
     */
    [SerializeField] private bool _keyHandlingLockedByChatUI = false;
    [SerializeField] private bool _keyHandlingLockedByInventoryUI = false;
    [SerializeField] private bool _keyHandlingLockedByDialogueUI = false;
    private bool _KeyHandlingLocked => _keyHandlingLockedByChatUI || _keyHandlingLockedByInventoryUI || _keyHandlingLockedByDialogueUI;

    void Start_Input()
    {
      RegisterOverlayLock(UIControlRegistry.Get<ChatUIController>(), locked => _keyHandlingLockedByChatUI = locked);
      RegisterOverlayLock(UIControlRegistry.Get<InventoryUIController>(), locked => _keyHandlingLockedByInventoryUI = locked);
      RegisterOverlayLock(UIControlRegistry.Get<DialoguePanelUIController>(), locked => _keyHandlingLockedByDialogueUI = locked);
    }

    private void RegisterOverlayLock(IUIOverlay overlay, Action<bool> setLocked)
    {
      if (overlay == null || setLocked == null) return;

      overlay.OverlayPushed += () => setLocked(true);
      overlay.OverlayPopped += () => setLocked(false);
    }

    public void Update_Input()
    {
      HandleChatInput();
      HandleEscape();
      HandleDialogueInput();

      if (_KeyHandlingLocked) return;

      if (IsSpectator)
      {
        HandleSpectatorInput();
        return;
      }

      HandleInteractInteractableObject();
      HandleHotbarControlInput();
      HandleToggleInventory();
      HandleSwitchCameraViewMode();
    }

    private void HandleEscape()
    {
      if (!Input.GetKeyDown(_keyEscape)) return;
      if (UIOverlayStack.IsEmpty()) return;

      UIOverlayStack.Pop();
    }

    private void HandleToggleInventory()
    {
      var inventory = UIControlRegistry.Get<InventoryUIController>();
      if (inventory.IsUnityNull()) return;

      if (Input.GetKeyDown(_keyToggleInventory))
      {
        if (UIOverlayStack.IsTop(inventory))
          UIOverlayStack.Pop();
        else
          UIOverlayStack.Push(inventory);
      }
    }

    private void HandleSwitchCameraViewMode()
    {
      if (Input.GetKeyDown(_keySwitchCameraViewMode)) SwitchCameraViewMode();
    }

    private void HandleInteractInteractableObject()
    {
      if (Input.GetKeyDown(_keyInteractInteractableObject)) TryInteractWithSelection();
      HandleInteractablesSelectionInput();
    }

    private void HandleSpectatorInput()
    {
      HandleSpectatorFollowInput();
      HandleSwitchCameraViewMode();
    }

    private void HandleSpectatorFollowInput()
    {
      if (Input.GetMouseButtonDown(1))
        TryStartSpectateFollowUnderCursor();

      if (_isSpectateFollowing && Input.GetKeyDown(_keySpectatorFlyDown))
        StopSpectateFollow();
    }

    private void TryStartSpectateFollowUnderCursor()
    {
      var cam = UnityEngine.Camera.main;
      if (cam == null) return;

      Ray ray = cam.ScreenPointToRay(Input.mousePosition);
      if (!Physics.Raycast(ray, out RaycastHit hit, 150f)) return;
      if (hit.collider == null) return;

      var target = hit.collider.GetComponentInParent<PlayerController>();
      if (target == null || target == this) return;

      BeginSpectateFollow(target);
    }

    private void HandleChatInput()
    {
      var chat = UIControlRegistry.Get<ChatUIController>();
      if (chat.IsUnityNull()) return;

      if (Input.GetKeyDown(_keyToggleChat))
      {
        chat.Open();
        return;
      }

      if (Input.GetKeyDown(_keyToggleCommand))
      {
        chat.OpenWithCommandStart();
        return;
      }

      if (!chat.IsOpen)
        return;

      if (Input.GetKeyDown(KeyboardConfigurationRegistry.SendChat))
      {
        chat.HandleSubmitKey();
        return;
      }

      if (Input.GetKeyDown(KeyboardConfigurationRegistry.CloseChatUI))
      {
        chat.HandleCancelKey();
      }
    }

    private void HandleHotbarControlInput()
    {
      // Selection using numkey
      HandleHotbarInputNumkey();

      if (_detector.IsUnityNull()) return;
      // Selection using mouse wheel
      if (_detector.InteractableNearbyExists) return;
      HandleHotbarInputMouseWheel();
    }

    private void HandleDialogueInput()
    {
      if (_dialoguePanelUIController.IsUnityNull()) return;
      if (!UIOverlayStack.IsTop(_dialoguePanelUIController)) return;

      // TODO: 더 고려할 사항:
      // 다이얼로그를 빠르게 넘기려고 하다가 첫 번째 선택지가 선택됨
      if (
        Input.GetKeyDown(_keyInteractInteractableObject) ||
        Input.GetMouseButtonDown(0)
      )
      {
        _dialoguePanelUIController.TrySelectCurrentOption();
      }

      HandleInteractablesSelectionInput();
    }
  }
}
