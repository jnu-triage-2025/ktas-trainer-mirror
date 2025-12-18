using TriageTrainer.Registry;
using UnityEngine;
using TriageTrainer.UI;

namespace TriageTrainer.Player
{
  public partial class PlayerController
  {
    [Header("Key Configuration")]
    [SerializeField] private KeyCode _keyMovingRunning = KeyCode.LeftControl;
    [SerializeField] private KeyCode _keyToggleInventory = KeyCode.E;
    [SerializeField] private KeyCode _keySwitchCameraViewMode = KeyCode.P;
    [SerializeField] private KeyCode _keyOpenEscMenu = KeyCode.Escape;
    [SerializeField] private KeyCode _keyToggleChat = KeyCode.T;
    [SerializeField] private KeyCode _keyInteractInteractableObject = KeyboardConfigurationRegistry.InteractInteractableObject;
    [SerializeField] private KeyCode _keyCloseUI = KeyCode.Escape;

    /**
     * 키 입력 핸들링을 막아야 하는 상황에서 이 플래그를 참으로 설정할 것
     * i.e. 채팅창 오픈
     */
    [SerializeField] private bool _keyHandlingLockedByChatUI = false;
    private bool _KeyHandlingLocked
    {
      // And operation in keyHandlingLocked*
      get => _keyHandlingLockedByChatUI;
    }

    public void Update_Input()
    {
      HandleToggleChat();
      if (_KeyHandlingLocked) return;

      HandleInteractInteractableObject();
      HandleHotbarControlInput();
      HandleToggleInventory();
      HandleCloseWithEscape();
      HandleSwitchCameraViewMode();
    }

    private void HandleToggleInventory()
    {
      var inventory = UIControlRegistry.Get<InventoryUIController>();
      if (inventory == null) return;

      if (Input.GetKeyDown(_keyToggleInventory))
      {
        if (UIOverlayStackManager.Instance.IsTop(inventory))
          UIOverlayStackManager.Instance.Pop(inventory);
        else
          UIOverlayStackManager.Instance.Push(inventory);
      }
    }

    private void HandleCloseWithEscape()
    {
      var inventory = UIControlRegistry.Get<InventoryUIController>();
      if (inventory == null) return;

      if (Input.GetKeyDown(KeyCode.Escape) && UIOverlayStackManager.Instance.IsTop(inventory))
        UIOverlayStackManager.Instance.Pop(inventory);
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

    private void HandleToggleChat()
    {
      if (Input.GetKeyDown(_keyToggleChat))
      {
        
      }
    }

    private void HandleHotbarControlInput()
    {
      // Selection using numkey
      HandleHotbarInputNumkey();

      // Selection using mouse wheel
      if (_detector.InteractableNearbyExists) return;
      HandleHotbarInputMouseWheel();
    }
  }
}
