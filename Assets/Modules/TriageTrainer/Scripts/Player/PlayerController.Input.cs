using TriageTrainer.Registry;
using UnityEngine;
using TriageTrainer.UI;

namespace TriageTrainer.Player
{
  public partial class PlayerController
  {
    [Header("Key Configuration")]
    [SerializeField] private KeyCode _runningKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode _toggleInventory = KeyCode.E;
    public void Update_Input()
    {
      HandleToggleInventory();
      HandleCloseWithEscape();
    }

    private void HandleToggleInventory()
    {
      var inventory = UIControlRegistry.Get<InventoryUIController>();
      if (inventory == null) return;

      if (Input.GetKeyDown(_toggleInventory))
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
  }
}
