using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private HotbarUIController _hotbarUI;

    private void Start_Hotbar()
    {
      if (_hotbarUI == null)
        _hotbarUI = Registry.Registry.Get<HotbarUIController>(RegistryType.UI, Registry.Registry.TypeKey<HotbarUIController>());

      if (_hotbarUI == null)
      {
        Debug.LogWarning("[PlayerController] HotbarUIController is not available. Skipping hotbar startup for now.");
        return;
      }

      _hotbarUI.SetupHotbarUI();
      _hotbarUI.BindInventory(_slots);
    }

    private void HandleHotbarInputNumkey()
    {
      if (!TryGetHotbarNumkeyIndexDown(out int slotIndex))
        return;

#if UNITY_EDITOR && DEBUG
      Debug.Log($"[PlayerController] Hotbar slot {slotIndex} selected via numkey");
#endif
      _hotbarUI.SetSelectedIndex(slotIndex);
    }

    /// <summary>
    /// 이번 프레임에 눌린 핫바 숫자 키(1~9, 0)에 대응하는 핫바 슬롯 인덱스를 구한다.
    /// 0 키는 마지막 슬롯(인덱스 9)에 대응하며, 핫바 슬롯 수를 넘어서는 키는 무시한다.
    /// </summary>
    /// <returns>대응하는 키가 이번 프레임에 눌렸으면 true.</returns>
    private bool TryGetHotbarNumkeyIndexDown(out int hotbarSlotIndex)
    {
      hotbarSlotIndex = -1;
      if (_hotbarUI == null)
        return false;

      int slotCount = _hotbarUI.SlotCount;

      for (int i = 0; i < 9 && i < slotCount; i++)
      {
        if (!Input.GetKeyDown(KeyCode.Alpha1 + i))
          continue;
        hotbarSlotIndex = i;
        return true;
      }

      if (slotCount > 9 && Input.GetKeyDown(KeyCode.Alpha0))
      {
        hotbarSlotIndex = 9;
        return true;
      }

      return false;
    }

    /// <summary>
    /// 인벤토리가 열려 있는 동안의 핫바 숫자 키 입력을 처리한다.
    /// 커서가 올라가 있는 인벤토리 슬롯과 해당 핫바 슬롯의 아이템을 서로 맞바꾼다.
    /// </summary>
    /// <returns>이번 프레임 입력을 인벤토리가 소비했으면 true.</returns>
    private bool HandleInventoryHotbarSwapInput()
    {
      if (_inventoryUI == null || !UIOverlayStack.IsTop(_inventoryUI))
        return false;

      if (!TryGetHotbarNumkeyIndexDown(out int slotIndex))
        return false;

      _inventoryUI.TrySwapHoveredSlotWithHotbarSlot(slotIndex);
      return true;
    }

    private void HandleHotbarInputMouseWheel()
    {
      float scroll = Input.mouseScrollDelta.y;
      if (Mathf.Abs(scroll) > Mathf.Epsilon)
      {
        _hotbarUI.CycleSelection(scroll > 0 ? -1 : 1);
      }
    }
  }
}
