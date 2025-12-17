using TriageTrainer.Registry;
using UnityEngine;
using TriageTrainer.UI;

namespace TriageTrainer.Player
{
  public partial class PlayerController
  {
    [Header("Key Configuration")]
    [SerializeField] private KeyCode _runningKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode _keyToggleInventory = KeyCode.E;
    [SerializeField] private KeyCode _keySwitchCameraViewMode = KeyCode.P;
    public void Update_Input()
    {
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
  }
}
