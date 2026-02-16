using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private HotbarUIController _hotbarUI;

    void Start_Hotbar()
    {
      if (_hotbarUI == null)
        _hotbarUI = Registry.Registry.Get<HotbarUIController>(RegistryType.UI, Registry.Registry.TypeKey<HotbarUIController>());
      _hotbarUI.SetupHotbarUI();
      _hotbarUI.BindInventory(_slots);
    }

    void HandleHotbarInputNumkey()
    {
      for (int i = 0; i < 9; i++)
      {
        if (Input.GetKeyDown(KeyCode.Alpha1 + i))
        {
#if UNITY_EDITOR
          Debug.Log($"[PlayerController] Hotbar slot {i} selected via numkey");
#endif
          _hotbarUI.SetSelectedIndex(i);
          break;
        }
      }
    }

    void HandleHotbarInputMouseWheel()
    {
      float scroll = Input.mouseScrollDelta.y;
      if (Mathf.Abs(scroll) > Mathf.Epsilon)
      {
        _hotbarUI.CycleSelection(scroll > 0 ? -1 : 1);
      }
    }
  }
}
