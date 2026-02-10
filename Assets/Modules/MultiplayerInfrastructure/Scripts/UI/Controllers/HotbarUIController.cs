using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class HotbarUIController : UIControllerABC
  {
    [Header("Config")]
    [SerializeField] private int hotbarSlotCount = 9;
    
    [Header("References")]
    [SerializeField] private UIDocument _uiDocument;

    private HotbarControl _hotbar;
    [Header("State")]
    [SerializeField] private int _selectedSlot = 0;

    public event Action OnSelectedSlotChanged;

    public int SelectedSlot
    {
      get { return _selectedSlot; }
    }

    public void SetupHotbarUI()
    {
      if (_uiDocument.IsUnityNull())
        _uiDocument = GetComponent<UIDocument>();

      if (_uiDocument.IsUnityNull())
      {
        Debug.LogError("[HotbarUIController] UIDocument is null");
        return;
      }
      
      var root = _uiDocument.rootVisualElement;
      _hotbar = root.Q<HotbarControl>("hotbar-root");

      if (_hotbar.IsUnityNull())
      {
        Debug.LogError("[HotbarUIController] Hotbar is null");
        return;
      }

      _hotbar.Initialize(Math.Clamp(hotbarSlotCount, HotbarControl.MinSlotSize, HotbarControl.MaxSlotSize));
      // _hotbar.BindInventory(Inventory);
      _hotbar.SetSelectedIndex(0);
      _hotbar.OnSlotSelected += OnHotbarSlotSelected;
    }
    protected virtual void Awake()
    {
      base.Awake();
    }
    private void OnHotbarSlotSelected(int slot)
    {
      _selectedSlot = slot;
      OnSelectedSlotChanged?.Invoke();
    }

    private void SyncHotbar()
    {
      if (_hotbar.IsUnityNull()) return;
      // _hotbar.BindInventory(Inventory);
      _hotbar.ForceRefresh();
    }

    public void SetSelectedIndex(int index) => _hotbar?.SetSelectedIndex(index);
    public void CycleSelection(int direction) => _hotbar?.CycleSelection(direction);
    public void BindInventory(IReadOnlyList<InventorySlotModelDTO> inventory) => _hotbar?.BindInventory(inventory);
  }
}
