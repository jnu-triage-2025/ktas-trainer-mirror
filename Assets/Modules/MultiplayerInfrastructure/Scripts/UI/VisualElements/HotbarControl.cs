using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class HotbarControl : VisualElement
  {
    public const int MinSlotSize = 1;
    public const int MaxSlotSize = 10;

    private readonly List<VisualElement> _slots = new();
    private IReadOnlyList<InventorySlotModelDTO> _inventory;
    private int _selectedIndex;

    [SerializeField] private int _hotbarSlotCount = 9;

    [UxmlAttribute("slot-count")]
    public int HotbarSlotCount
    {
      get => _hotbarSlotCount;
      set
      {
        var clamped = Mathf.Clamp(value, MinSlotSize, MaxSlotSize);
        if (_hotbarSlotCount == clamped) return;

        _hotbarSlotCount = clamped;
        if (panel != null) // only rebuild once we’re actually on-screen
        {
          Initialize(_hotbarSlotCount);
        }
      }
    }

    public int SlotCount { get; private set; } = 9;
    public Action<int> OnSlotSelected;

    public HotbarControl()
    {
      AddToClassList("hotbar");

      RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
      RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

      RegisterCallback<ClickEvent>(OnAnyClick);

      _selectedIndex = 0;
    }

    private void OnAttachToPanel(AttachToPanelEvent _)
    {
      Initialize(_hotbarSlotCount);
    }

    private void OnDetachFromPanel(DetachFromPanelEvent _)
    {
      // optional: cleanup if you subscribe to external events
    }

    public void Initialize(int slotCount)
    {
      slotCount = Mathf.Clamp(slotCount, MinSlotSize, MaxSlotSize);
      if (SlotCount != slotCount || _slots.Count == 0)
      {
        SlotCount = slotCount;
        BuildSlots();
      }
      else
      {
        RefreshSlots(); // inventory might have changed
      }
    }

    private void BuildSlots()
    {
      Clear();
      _slots.Clear();

      for (int i = 0; i < SlotCount; i++)
      {
        var slot = CreateSlotElement(i);
        _slots.Add(slot);
        Add(slot);
      }

      ApplySelectionVisuals();
    }

    private VisualElement CreateSlotElement(int index)
    {
      var slot = new VisualElement { name = $"hotbar-slot-{index}" };
      slot.AddToClassList("hotbar__slot");
      slot.userData = index;

      var label = new Label((index + 1).ToString())
      {
        pickingMode = PickingMode.Ignore
      };
      label.AddToClassList("hotbar__slot-label");
      slot.Add(label);

      var icon = new VisualElement { name = "icon" };
      icon.AddToClassList("hotbar__slot-icon");
      slot.Add(icon);

      var countLabel = new Label { name = "count", pickingMode = PickingMode.Ignore };
      countLabel.AddToClassList("hotbar__slot-count");
      slot.Add(countLabel);

      slot.RegisterCallback<PointerDownEvent>(evt =>
      {
        if (evt.button == (int)MouseButton.LeftMouse)
        {
          SetSelectedIndex(index);
          OnSlotSelected?.Invoke(_selectedIndex);
        }
      });

      return slot;
    }

    public void BindInventory(IReadOnlyList<InventorySlotModelDTO> inventory)
    {
      _inventory = inventory;
      RefreshSlots();
    }

    public void SetSelectedIndex(int index)
    {
      if (SlotCount == 0) return;

      index = Mathf.Clamp(index, 0, SlotCount - 1);
      if (_selectedIndex == index) return;

      _selectedIndex = index;
      ApplySelectionVisuals();
      OnSlotSelected?.Invoke(_selectedIndex);
    }

    public int GetSelectedIndex() => _selectedIndex;

    public void CycleSelection(int direction)
    {
      if (SlotCount == 0) return;

      _selectedIndex = (_selectedIndex + direction) % SlotCount;
      if (_selectedIndex < 0) _selectedIndex += SlotCount;

      ApplySelectionVisuals();
      OnSlotSelected?.Invoke(_selectedIndex);
    }

    private void RefreshSlots()
    {
      for (int i = 0; i < _slots.Count; i++)
      {
        var slot = _slots[i];
        var icon = slot.Q<VisualElement>("icon");
        var countLabel = slot.Q<Label>("count");

        if (_inventory != null && i < _inventory.Count && _inventory[i] != null)
        {
          var itemInstance = _inventory[i].ItemInstance;
          if (itemInstance != null)
          {
            icon.style.backgroundImage = new StyleBackground(itemInstance.ItemTexture);
            icon.RemoveFromClassList("hotbar__slot-empty");
            var count = itemInstance.currCount;
            countLabel.text = count > 1 ? count.ToString() : string.Empty;
          }
          else
          {
            icon.style.backgroundImage = null;
            icon.AddToClassList("hotbar__slot-empty");
            countLabel.text = string.Empty;
          }
        }
        else
        {
          icon.style.backgroundImage = null;
          icon.AddToClassList("hotbar__slot-empty");
          countLabel.text = string.Empty;
        }
      }
    }

    private void ApplySelectionVisuals()
    {
      for (int i = 0; i < _slots.Count; i++)
      {
        if (i == _selectedIndex)
          _slots[i].AddToClassList("hotbar__slot--selected");
        else
          _slots[i].RemoveFromClassList("hotbar__slot--selected");
      }
    }

    private void OnAnyClick(ClickEvent evt)
    {
      // Use this if you need to stop propagation, etc.
    }

    public void ForceRefresh() => RefreshSlots();
  }
}
