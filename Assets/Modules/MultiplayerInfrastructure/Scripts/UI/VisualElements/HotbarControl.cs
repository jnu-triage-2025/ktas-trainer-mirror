using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.ItemSystem;
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

    // 현재 손에 들고 있는(선택된 슬롯) 아이템 이름 추적용.
    // 슬롯 선택 변경 또는 슬롯 내 아이템 상태 변경을 감지하기 위한 스냅샷 값.
    private Item _heldItemSnapshot;
    private string _heldItemNameSnapshot;
    private bool _hasHeldItemSnapshot;

    [SerializeField] private int _hotbarSlotCount = 9;

    [UxmlAttribute("slot-count")]
    public int HotbarSlotCount
    {
      get => _hotbarSlotCount;
      set
      {
        var clamped = Mathf.Clamp(value, MinSlotSize, MaxSlotSize);
        if (_hotbarSlotCount == clamped)
          return;

        _hotbarSlotCount = clamped;
        if (panel != null) // 실제로 화면에 나타났을 때만 한 번 재구성한다
        {
          Initialize(_hotbarSlotCount);
        }
      }
    }

    public int SlotCount { get; private set; } = 9;
    public Action<int> OnSlotSelected;

    /// <summary>
    /// 현재 선택된(손에 들고 있는) 슬롯의 아이템이 변경되었을 때 호출됩니다.
    /// 변경으로 간주되는 경우:
    /// - 선택 슬롯이 바뀐 경우
    /// - 선택 슬롯의 아이템 인스턴스 또는 표시 이름이 바뀐 경우
    /// 슬롯이 비어있으면 null 을 전달합니다.
    /// </summary>
    public Action<string> OnHeldItemNameChanged;

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
      // 선택: 외부 이벤트를 구독한다면 여기서 해제한다
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
        RefreshSlots(); // 인벤토리가 변경되었을 수 있다
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

      ItemDurabilityBar.Ensure(slot, "hotbar__durability", "hotbar__durability-fill");

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
      if (SlotCount == 0)
        return;

      index = Mathf.Clamp(index, 0, SlotCount - 1);
      if (_selectedIndex == index)
        return;

      _selectedIndex = index;
      ApplySelectionVisuals();
      UpdateHeldItemName();
      OnSlotSelected?.Invoke(_selectedIndex);
    }

    public int GetSelectedIndex() => _selectedIndex;

    public void CycleSelection(int direction)
    {
      if (SlotCount == 0)
        return;

      _selectedIndex = (_selectedIndex + direction) % SlotCount;
      if (_selectedIndex < 0)
        _selectedIndex += SlotCount;

      ApplySelectionVisuals();
      UpdateHeldItemName();
      OnSlotSelected?.Invoke(_selectedIndex);
    }

    private void RefreshSlots()
    {
      for (int i = 0; i < _slots.Count; i++)
      {
        var slot = _slots[i];
        var icon = slot.Q<VisualElement>("icon");
        var countLabel = slot.Q<Label>("count");
        var durability = ItemDurabilityBar.Ensure(
          slot, "hotbar__durability", "hotbar__durability-fill");

        if (_inventory != null && i < _inventory.Count && _inventory[i] != null)
        {
          var itemInstance = _inventory[i].ItemInstance;
          if (itemInstance != null)
          {
            icon.style.backgroundImage = new StyleBackground(itemInstance.CurrentItemIconTexture);
            icon.RemoveFromClassList("hotbar__slot-empty");
            var count = itemInstance.CurrentStackCount;
            countLabel.text = count > 1 ? count.ToString() : string.Empty;
            ItemDurabilityBar.Update(durability, itemInstance);
          }
          else
          {
            icon.style.backgroundImage = null;
            icon.AddToClassList("hotbar__slot-empty");
            countLabel.text = string.Empty;
            ItemDurabilityBar.Update(durability, null);
          }
        }
        else
        {
          icon.style.backgroundImage = null;
          icon.AddToClassList("hotbar__slot-empty");
          countLabel.text = string.Empty;
          ItemDurabilityBar.Update(durability, null);
        }
      }

      // 인벤토리 갱신으로 선택 슬롯의 아이템 상태가 바뀌었을 수 있으므로 이름 재확인.
      UpdateHeldItemName();
    }

    /// <summary>
    /// 현재 선택된 슬롯의 아이템을 확인하고, 이전 스냅샷과 비교하여
    /// 변경되었으면 <see cref="OnHeldItemNameChanged"/> 를 호출합니다.
    /// </summary>
    private void UpdateHeldItemName()
    {
      Item current = null;
      if (_inventory != null &&
          _selectedIndex >= 0 &&
          _selectedIndex < _inventory.Count &&
          _inventory[_selectedIndex] != null)
      {
        current = _inventory[_selectedIndex].ItemInstance;
      }

      var currentName = current != null ? current.CurrentDisplayName : null;

      // 아이템 인스턴스 참조 또는 표시 이름이 이전과 동일하면 변경으로 보지 않는다.
      bool changed = !_hasHeldItemSnapshot
        || !ReferenceEquals(_heldItemSnapshot, current)
        || _heldItemNameSnapshot != currentName;

      if (!changed)
        return;

      _heldItemSnapshot = current;
      _heldItemNameSnapshot = currentName;
      _hasHeldItemSnapshot = true;

      // 슬롯이 비어있으면(아이템 없음) 아무것도 표시하지 않도록 null 전달.
      OnHeldItemNameChanged?.Invoke(current != null ? currentName : null);
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
      // 전파를 중단해야 하는 경우 등에 사용한다
    }

    public void ForceRefresh() => RefreshSlots();
  }
}
