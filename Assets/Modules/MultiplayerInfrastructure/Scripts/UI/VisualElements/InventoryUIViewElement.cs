using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// Pure view responsible for inventory slot visuals and interactions.
  /// Intended to be instantiated from UXML; controller owns lifecycle and data binding.
  /// </summary>
  [UxmlElement]
  public partial class InventoryUIView : VisualElement
  {
    private int _columns = 9;
    private int _rows = 4;
    private VisualTreeAsset _slotTemplate;
    private Texture2D _defaultIcon;

    private VisualElement _inventoryGrid;

    private readonly List<VisualElement> _slotElements = new();
    private readonly List<InventorySlotModelDTO> _slotDataBuffer = new();
    private IReadOnlyList<InventorySlotModelDTO> _boundSlots;

    public event Action SlotsMutated;

    private InventorySlotModelDTO _heldItem;
    private VisualElement _heldItemGhost;
    private Image _heldItemGhostIcon;
    private Label _heldItemGhostCount;

    public bool IsVisible => style.display != DisplayStyle.None;
    public IReadOnlyList<InventorySlotModelDTO> BoundSlots => _boundSlots ?? _slotDataBuffer;

    public void Initialize(int columns, int rows, VisualTreeAsset slotTemplate, Texture2D defaultIcon)
    {
      _columns = Mathf.Max(1, columns);
      _rows = Mathf.Max(1, rows);
      _slotTemplate = slotTemplate;
      _defaultIcon = defaultIcon;

      _inventoryGrid = this.Q<VisualElement>("InventoryGrid") ?? CreateFallbackGrid();

      CreateHeldItemGhost();
      RegisterCallback<PointerMoveEvent>(OnPointerMoveWhileHolding);
      RegisterCallback<PointerUpEvent>(OnPointerUpOutsideSlot);

      BuildInventoryGrid();
      SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
      style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      if (!visible && _heldItemGhost != null)
        _heldItemGhost.style.display = DisplayStyle.None;
    }

    public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
    {
      if (_slotElements.Count == 0 || slots == null) return;

      _boundSlots = slots;

      // Ensure backing buffers cover both the visual grid and incoming data size to avoid out-of-range issues when the counts diverge.
      int incomingCount = slots.Count;
      int targetBufferSize = Math.Max(_slotElements.Count, incomingCount);
      EnsureSlotDataCapacity(targetBufferSize);

      for (int i = 0; i < _slotElements.Count; i++)
      {
        if (_slotDataBuffer.Count <= i)
          EnsureSlotDataCapacity(i + 1);

        var slotModel = i < incomingCount && slots[i] != null ? slots[i] : _slotDataBuffer[i];
        if (slotModel == null) slotModel = new InventorySlotModelDTO();
        _slotDataBuffer[i] = slotModel;
        RefreshSlotVisual(i);
      }
    }

    public void RebuildGrid(int newColumns, int newRows)
    {
      _columns = Mathf.Max(1, newColumns);
      _rows = Mathf.Max(1, newRows);
      BuildInventoryGrid();
    }

    public void ReturnHeldItemToInventoryOnClose()
    {
      if (_heldItem == null || _heldItem.IsEmpty)
      {
        ClearHeldItem();
        return;
      }

      for (int i = 0; i < _slotElements.Count; i++)
      {
        var slot = GetSlotModel(i);
        if (slot == null) continue;

        if (slot.IsEmpty)
        {
          slot.SetItem(_heldItem.ItemInstance);
          RefreshSlotVisual(i);
          ClearHeldItem();
          NotifySlotsMutated();
          return;
        }

        if (slot.ItemInstance != null && _heldItem.ItemInstance != null && slot.ItemInstance.CanStackWith(_heldItem.ItemInstance))
        {
          var leftover = slot.Push(_heldItem);
          RefreshSlotVisual(i);

          if (leftover == null || _heldItem.ItemInstance == null || _heldItem.ItemInstance.currCount <= 0)
          {
            ClearHeldItem();
            NotifySlotsMutated();
            return;
          }

          _heldItem.ItemInstance = leftover;
          NotifySlotsMutated();
        }
      }

      Debug.Log("[InventoryUIView] No space to return held item. TODO: spawn Item world instance.");
      ClearHeldItem();
    }

    private VisualElement CreateFallbackGrid()
    {
      var grid = new VisualElement { name = "InventoryGrid" };
      grid.AddToClassList("inventory-grid");
      Add(grid);
      return grid;
    }

    private void BuildInventoryGrid()
    {
      _slotElements.Clear();
      _inventoryGrid?.Clear();

      VisualElement hotbarSlotRow = null;
      int slotIndex = 0;

      for (int row = 0; row < _rows; row++)
      {
        var slotRow = new VisualElement();
        slotRow.AddToClassList("slot-row");

        for (int col = 0; col < _columns; col++)
        {
          var slot = _slotTemplate != null ? _slotTemplate.CloneTree() : CreateDefaultSlot();
          slot.name = $"Slot_{slotIndex}";
          slot.userData = slotIndex;
          slot.AddToClassList("slot");
          slot.pickingMode = PickingMode.Position;

          int capturedIndex = slotIndex;
          slot.RegisterCallback<PointerDownEvent>(evt => HandleSlotClicked(capturedIndex, evt));

          slotRow.Add(slot);
          _slotElements.Add(slot);
          slotIndex++;
        }

        if (row == 0) hotbarSlotRow = slotRow;
        else _inventoryGrid.Add(slotRow);
      }

      if (hotbarSlotRow != null) _inventoryGrid.Add(hotbarSlotRow);

      EnsureSlotDataCapacity(_slotElements.Count);
      RefreshAllSlots();
    }

    private VisualElement CreateDefaultSlot()
    {
      var slot = new VisualElement();

      var icon = new Image
      {
        name = "ItemIcon",
        pickingMode = PickingMode.Ignore
      };
      icon.AddToClassList("slot__icon");
      slot.Add(icon);

      var countLabel = new Label
      {
        name = "ItemCount",
        pickingMode = PickingMode.Ignore,
        text = string.Empty
      };
      countLabel.AddToClassList("slot__count");
      slot.Add(countLabel);

      return slot;
    }

    private void CreateHeldItemGhost()
    {
      _heldItemGhost = new VisualElement { name = "HeldItemGhost" };
      _heldItemGhost.AddToClassList("held-item-ghost");
      _heldItemGhost.style.position = Position.Absolute;
      _heldItemGhost.style.display = DisplayStyle.None;
      _heldItemGhost.pickingMode = PickingMode.Ignore;

      _heldItemGhostIcon = new Image { name = "HeldItemGhostIcon", pickingMode = PickingMode.Ignore };
      _heldItemGhostIcon.AddToClassList("held-item-ghost__icon");
      _heldItemGhost.Add(_heldItemGhostIcon);

      _heldItemGhostCount = new Label { name = "HeldItemGhostCount", pickingMode = PickingMode.Ignore };
      _heldItemGhostCount.AddToClassList("held-item-ghost__count");
      _heldItemGhost.Add(_heldItemGhostCount);

      Add(_heldItemGhost);
    }

    private void HandleSlotClicked(int slotIndex, PointerDownEvent evt)
    {
      Debug.Log($"[InventoryUIView] Slot {slotIndex} clicked.");
      if (slotIndex < 0 || slotIndex >= _slotElements.Count) return;

      if (_heldItem == null)
        TryPickUpFromSlot(slotIndex);
      else
        TryPlaceHeldItemIntoSlot(slotIndex);

      UpdateHeldItemGhostPosition(evt.position);
    }

    private void TryPickUpFromSlot(int slotIndex)
    {
      var slotModel = GetSlotModel(slotIndex);
      if (slotModel == null || slotModel.IsEmpty) return;

      var taken = slotModel.TakeAll();
      if (taken == null) return;

      _heldItem = new InventorySlotModelDTO(taken);

      RefreshSlotVisual(slotIndex);
      UpdateHeldItemGhostVisual(_heldItem);
      NotifySlotsMutated();
    }

    private void TryPlaceHeldItemIntoSlot(int slotIndex)
    {
      if (_heldItem == null || _heldItem.IsEmpty)
      {
        ClearHeldItem();
        return;
      }

      var target = GetSlotModel(slotIndex);
      if (target == null)
        target = _slotDataBuffer[slotIndex] = new InventorySlotModelDTO();

      if (target.IsEmpty)
      {
        target.SetItem(_heldItem.ItemInstance);
        RefreshSlotVisual(slotIndex);
        ClearHeldItem();
        NotifySlotsMutated();
        return;
      }

      if (target.ItemInstance != null && _heldItem.ItemInstance != null && target.ItemInstance.CanStackWith(_heldItem.ItemInstance))
      {
        var leftover = target.Push(_heldItem);
        RefreshSlotVisual(slotIndex);

        if (leftover == null || _heldItem.ItemInstance == null || _heldItem.ItemInstance.currCount <= 0)
        {
          ClearHeldItem();
        }
        else
        {
          _heldItem.ItemInstance = leftover;
          UpdateHeldItemGhostVisual(_heldItem);
        }

        NotifySlotsMutated();
        return;
      }

      var previous = target.SwapWith(_heldItem.ItemInstance);
      RefreshSlotVisual(slotIndex);

      _heldItem = previous != null ? new InventorySlotModelDTO(previous) : null;
      UpdateHeldItemGhostVisual(_heldItem);
      NotifySlotsMutated();
    }

    private void OnPointerMoveWhileHolding(PointerMoveEvent evt)
    {
      if (_heldItem == null) return;
      UpdateHeldItemGhostPosition(evt.position);
    }

    private void OnPointerUpOutsideSlot(PointerUpEvent evt)
    {
      if (_heldItem == null) return;

      if (!TryGetSlotIndexFromEvent(evt.target as VisualElement, out _))
        Debug.Log("[InventoryUIView] Pointer released outside inventory grid (drop logic TBD).");
    }

    private bool TryGetSlotIndexFromEvent(VisualElement target, out int slotIndex)
    {
      slotIndex = -1;
      if (target == null) return false;

      VisualElement candidate = target;
      while (candidate != null && !_slotElements.Contains(candidate))
        candidate = candidate.parent;

      if (candidate == null) return false;

      if (candidate.userData is int idx)
        slotIndex = idx;
      else
        slotIndex = _slotElements.IndexOf(candidate);

      return slotIndex >= 0;
    }

    private void RefreshAllSlots()
    {
      for (int i = 0; i < _slotElements.Count; i++)
        RefreshSlotVisual(i);
    }

    private void RefreshSlotVisual(int slotIndex)
    {
      if (slotIndex < 0 || slotIndex >= _slotElements.Count) return;

      var slot = _slotElements[slotIndex];
      var icon = slot.Q<Image>("ItemIcon");
      var label = slot.Q<Label>("ItemCount");
      var slotData = GetSlotModel(slotIndex);

      if (slotData == null || slotData.IsEmpty)
      {
        if (icon != null) icon.image = _defaultIcon;
        if (label != null) label.text = string.Empty;
        return;
      }

      if (icon != null)
      {
        var sprite = slotData.ItemInstance?.ItemTexture;
        icon.image = sprite != null ? sprite.texture : _defaultIcon;
      }

      if (label != null)
        label.text = slotData.ItemInstance != null && slotData.ItemInstance.currCount > 1
          ? slotData.ItemInstance.currCount.ToString()
          : string.Empty;
    }

    private void UpdateHeldItemGhostVisual(InventorySlotModelDTO heldData)
    {
      if (_heldItemGhost == null) return;

      if (heldData == null || heldData.IsEmpty)
      {
        _heldItemGhost.style.display = DisplayStyle.None;
        _heldItemGhostIcon.image = null;
        _heldItemGhostCount.text = string.Empty;
        return;
      }

      var sprite = heldData.ItemInstance?.ItemTexture;
      _heldItemGhostIcon.image = sprite != null ? sprite.texture : _defaultIcon;
      _heldItemGhostCount.text = heldData.ItemInstance != null && heldData.ItemInstance.currCount > 1
        ? heldData.ItemInstance.currCount.ToString()
        : string.Empty;

      _heldItemGhost.style.display = DisplayStyle.Flex;
      _heldItemGhost.BringToFront();
    }

    private void UpdateHeldItemGhostPosition(Vector2 panelPosition)
    {
      if (_heldItemGhost == null) return;

      Rect rootBounds = worldBound;
      float ghostWidth = Mathf.Max(1f, _heldItemGhost.resolvedStyle.width);
      float ghostHeight = Mathf.Max(1f, _heldItemGhost.resolvedStyle.height);

      _heldItemGhost.style.left = panelPosition.x - rootBounds.x - ghostWidth * 0.5f;
      _heldItemGhost.style.top = panelPosition.y - rootBounds.y - ghostHeight * 0.5f;
    }

    private void EnsureSlotDataCapacity(int desiredCapacity)
    {
      while (_slotDataBuffer.Count < desiredCapacity)
        _slotDataBuffer.Add(new InventorySlotModelDTO());
    }

    private InventorySlotModelDTO GetSlotModel(int slotIndex)
    {
      if (slotIndex < 0) return null;

      EnsureSlotDataCapacity(slotIndex + 1);

      if (_boundSlots != null && slotIndex < _boundSlots.Count && _boundSlots[slotIndex] != null)
        return _boundSlots[slotIndex];

      if (_slotDataBuffer[slotIndex] == null)
        _slotDataBuffer[slotIndex] = new InventorySlotModelDTO();

      return _slotDataBuffer[slotIndex];
    }

    private void ClearHeldItem()
    {
      _heldItem = null;
      UpdateHeldItemGhostVisual(null);
    }

    private void NotifySlotsMutated()
    {
      SlotsMutated?.Invoke();
    }
  }
}
