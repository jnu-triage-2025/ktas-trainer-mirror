using System;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private ChatUIController _chatUI;

    [Header("Key Configuration")]
    [SerializeField] private KeyCode _keyMovingRunning = KeyCode.LeftControl;
    [SerializeField] private KeyCode _keyToggleInventory = KeyCode.E;
    [SerializeField] private KeyCode _keySwitchCameraViewMode = KeyCode.P;
    [SerializeField] private KeyCode _keyOpenEscMenu = KeyCode.Escape;
    [SerializeField] private KeyCode _keyToggleChat = DefaultsKeyConfiguration.OpenChatUI;
    [SerializeField] private KeyCode _keyToggleCommand = DefaultsKeyConfiguration.OpenChatUIWithCommand;
    [SerializeField] private KeyCode _keyInteractInteractableObject = DefaultsKeyConfiguration.InteractInteractableObject;
    [SerializeField] private KeyCode _keyAdvanceDialogue = KeyCode.Space;
    [SerializeField] private KeyCode _keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode _keySpectatorFlyDown = KeyCode.LeftShift;
    [SerializeField] private KeyCode _keyOpenQuestUI = DefaultsKeyConfiguration.OpenQuestUI;
    [SerializeField] private KeyCode _keyDropHeldItem = DefaultsKeyConfiguration.DropHeldItem;

    /**
     * 키 입력 핸들링을 막아야 하는 상황에서 이 플래그를 참으로 설정할 것
     * i.e. 채팅창 오픈
     */
    [SerializeField] private bool _keyHandlingLockedByEscapeUI = false;
    [SerializeField] private bool _keyHandlingLockedByChatUI = false;
    [SerializeField] private bool _keyHandlingLockedByInventoryUI = false;
    [SerializeField] private bool _keyHandlingLockedByDialogueUI = false;
    private bool _KeyHandlingLocked =>
      _keyHandlingLockedByEscapeUI
      || _keyHandlingLockedByChatUI
      || _keyHandlingLockedByInventoryUI
      || _keyHandlingLockedByDialogueUI
      || !UIOverlayStack.IsEmpty();

    void Start_Input()
    {
      _chatUI = Registry.Registry.Get<ChatUIController>(RegistryType.UI, Registry.Registry.TypeKey<ChatUIController>());
      RegisterOverlayLock(_chatUI, locked => _keyHandlingLockedByChatUI = locked);
      RegisterOverlayLock(Registry.Registry.Get<InventoryUIController>(RegistryType.UI, Registry.Registry.TypeKey<InventoryUIController>()), locked => _keyHandlingLockedByInventoryUI = locked);
      RegisterOverlayLock(Registry.Registry.Get<GameEscapeMenuUIController>(RegistryType.UI, Registry.Registry.TypeKey<GameEscapeMenuUIController>()), locked => _keyHandlingLockedByEscapeUI = locked);
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
      var escapeConsumed = HandleEscape();
      HandleDialogueInput();
      HandleIntravenousLineConnectionModeExit();

      if (_keyHandlingLockedByInventoryUI) 
        if (HandleToggleInventory())
          return;

      if (_KeyHandlingLocked) return;

      if (!escapeConsumed)
        HandleEscapeMenuInput();

      if (IsSpectator)
      {
        HandleSpectatorInput();
        return;
      }
      
      HandleOpenQuestUIInput();

      HandleInteractInteractableObject();
      HandleHotbarControlInput();
      HandleToggleInventory();
      HandleSwitchCameraViewMode();
      HandleItemActionInput();
    }

    private bool HandleEscape()
    {
      if (!Input.GetKeyDown(_keyEscape)) return false;
      if (UIOverlayStack.IsEmpty()) return false;

      UIOverlayStack.Pop();
      return true;
    }

    /// <summary>
    /// 인벤토리를 인벤토리 토글 키를 이용해 열거나 닫으려고 시도할 때의 처리를 대응합니다.
    /// 
    /// 이 메서드는 몇 개 로직이 중첩되어있습니다:
    /// 이 메서드는 인벤토리 UI 표시에 변화가 있을 때 true를 반환합니다.
    /// 이것은 Update_Input 메서드에서 인벤토리 토글 메서드를 재활용할 수 있게 하기 위함입니다.
    /// 다른 비슷한 유형의 메서드와의 일관성을 떨어뜨리고, 코드 이해에 혼란을 줄 수 있으므로
    /// Open/Close 메서드를 분리하여 따로 대응할 것인지는 이후에 고려해야 합니다.
    /// </summary>
    /// <returns>bool 인벤토리 UI 표시에 변화가 있는가?</returns>
    private bool HandleToggleInventory()
    {
      if (_inventoryUI == null) return false;

      if (Input.GetKeyDown(_keyToggleInventory))
      {
        if (UIOverlayStack.IsTop(_inventoryUI))
          UIOverlayStack.Pop();
        else
          UIOverlayStack.Push(_inventoryUI);
        return true;
      }
      return false;
    }

    private void HandleSwitchCameraViewMode()
    {
      if (Input.GetKeyDown(_keySwitchCameraViewMode)) SwitchCameraViewMode();
    }

    private void HandleInteractInteractableObject()
    {
      // Dialogue progression is handled only in HandleDialogueInput.
      // Guard here to prevent a second TrySelectCurrentOption call in the same frame.
      if (!_dialoguePanelUIController.IsUnityNull() && UIOverlayStack.IsTop(_dialoguePanelUIController))
      {
        HandleInteractablesSelectionInput();
        return;
      }

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
      if (_chatUI.IsUnityNull()) return;

      bool hasOtherOverlay = !UIOverlayStack.IsEmpty() && !UIOverlayStack.IsTop(_chatUI);

      if (Input.GetKeyDown(_keyToggleChat))
      {
        if (hasOtherOverlay)
          return;

        _chatUI.Open();
        return;
      }

      if (Input.GetKeyDown(_keyToggleCommand))
      {
        if (hasOtherOverlay)
          return;

        _chatUI.OpenWithCommandStart();
        return;
      }

      if (!_chatUI.IsOpen)
        return;

      if (Input.GetKeyDown(DefaultsKeyConfiguration.SendChat))
      {
        _chatUI.HandleSubmitKey();
        return;
      }

      if (Input.GetKeyDown(DefaultsKeyConfiguration.CloseChatUI))
      {
        _chatUI.HandleCancelKey();
        return;
      }

      if (Input.GetKeyDown(KeyCode.UpArrow))
      {
        _chatUI.HandleHistoryPreviousKey();
        return;
      }

      if (Input.GetKeyDown(KeyCode.DownArrow))
      {
        _chatUI.HandleHistoryNextKey();
      }
    }

    private void HandleEscapeMenuInput()
    {
      if (Input.GetKeyDown(_keyOpenEscMenu))
      {
        if (UIOverlayStack.IsTop(_escapeMenuUIController))
          UIOverlayStack.Pop();
        else
          if (UIOverlayStack.IsEmpty())
            UIOverlayStack.Push(_escapeMenuUIController);
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

      // Dialogue advance keys are centralized here so all paths go through PlayerController.Input.
      if (
        Input.GetKeyDown(_keyInteractInteractableObject) ||
        Input.GetKeyDown(_keyAdvanceDialogue) ||
        Input.GetMouseButtonDown(0)
      )
      {
        _dialoguePanelUIController.TrySelectCurrentOption();
      }

      HandleInteractablesSelectionInput();
    }

    private void HandleItemActionInput()
    {
      if (Input.GetMouseButtonDown(0)) TriggerAttack();
      if (Input.GetMouseButtonDown(1)) TriggerUseItem();

      if (Input.GetKeyDown(KeyCode.LeftShift))
      {
        if (TryDropCarriedReposable(out _))
          RefreshInteractableHintsNow();
      }

      if (Input.GetKeyDown(_keyDropHeldItem)) DropHeldItem();
    }

    private void HandleOpenQuestUIInput()
    {
      if (Input.GetKeyDown(_keyOpenQuestUI))
      {
        if (_questUIController.IsUnityNull())
          _questUIController = FindQuestUIController();
        if (_questUIController.IsUnityNull()) return;

        if (UIOverlayStack.IsTop(_questUIController))
          UIOverlayStack.Pop();
        else
          UIOverlayStack.Push(_questUIController);
      }
    }

    private void HandleIntravenousLineConnectionModeExit()
    {
      if (!IsIntravenousLineConnectionMode)
        return;

      if (Input.GetKeyDown(_keySpectatorFlyDown))
        SetIntravenousLineConnectionMode(false);
    }
  }
}
