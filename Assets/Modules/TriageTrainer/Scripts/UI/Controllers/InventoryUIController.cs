using System;
using System.Collections.Generic;
using System.Linq;
using TriageTrainer.Player;
using TriageTrainer.Registry;
using TriageTrainer.UI;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryUIController : UIControllerABC, IUIOverlay
{
  #region Inspector Properties
  [Header("Layout")]
  [SerializeField] private int columns = 9;
  [SerializeField] private int rows = 4;

  [Header("Slot Template (optional)")]
  [SerializeField] private VisualTreeAsset slotTemplate;

  [Header("Textures")]
  [SerializeField] private Texture2D defaultIcon;
  #endregion

  #region Private Fields
  private UIDocument _uiDocument;
  private VisualElement _documentRoot;
  private VisualElement _inventoryGrid;

  private readonly List<VisualElement> _slotElements = new();
  private readonly List<InventorySlotModelDTO> _slotDataBuffer = new();

  private InventorySlotModelDTO _heldItem;
  private int _heldItemSourceIndex = -1;

  private VisualElement _heldItemGhost;
  private Image _heldItemGhostIcon;
  private Label _heldItemGhostCount;
  #endregion

  #region Public API
  public bool IsOpened => _documentRoot != null && _documentRoot.style.display != DisplayStyle.None;
  #endregion

  #region Unity Lifecycle
  protected virtual void Awake()
  {
    base.Awake();

    _uiDocument = GetComponent<UIDocument>();
    _documentRoot = _uiDocument.rootVisualElement;
    _inventoryGrid = _documentRoot.Q<VisualElement>("InventoryGrid");

    CreateHeldItemGhost();
    _documentRoot.RegisterCallback<PointerMoveEvent>(OnPointerMoveWhileHolding);
    _documentRoot.RegisterCallback<PointerUpEvent>(OnPointerUpOutsideSlot);

    BuildInventoryGrid();
    ToggleRoot(false);
  }

  private void OnDestroy()
  {
    if (_documentRoot == null) return;
    _documentRoot.UnregisterCallback<PointerMoveEvent>(OnPointerMoveWhileHolding);
    _documentRoot.UnregisterCallback<PointerUpEvent>(OnPointerUpOutsideSlot);
  }
  #endregion

  #region UI Construction
  private void BuildInventoryGrid()
  {
    _slotElements.Clear();
    _inventoryGrid?.Clear();

    VisualElement hotbarSlotRow = null;
    int slotIndex = 0;

    for (int row = 0; row < rows; row++)
    {
      var slotRow = CreateSlotRow();

      for (int col = 0; col < columns; col++)
      {
        var slot = slotTemplate != null ? slotTemplate.Instantiate() : CreateDefaultSlot();
        slot.name = $"Slot_{slotIndex}";
        slot.userData = slotIndex;
        slot.AddToClassList("slot");
        slot.RegisterCallback<PointerDownEvent>(_ => Debug.Log($"Down {slotIndex}"));

        AttachSlotManipulator(slot, slotIndex);

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

  private VisualElement CreateSlotRow()
  {
    var slotRow = new VisualElement();
    slotRow.AddToClassList("slot-row");
    return slotRow;
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

  private void AttachSlotManipulator(VisualElement slot, int slotIndex)
  {
    slot.pickingMode = PickingMode.Position;   // MUST be pickable

    var clickable = new Clickable(() => HandleSlotClicked(slotIndex, null));
    clickable.clickedWithEventInfo += evt => HandleSlotClicked(slotIndex, evt);

    slot.AddManipulator(clickable);
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

    _documentRoot.Add(_heldItemGhost);
  }
  #endregion

  #region Overlay Controls
  public void ToggleRoot(bool visible)
  {
    if (_documentRoot == null) return;

    _documentRoot.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
    _documentRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
  }

  public void OnOverlayPushed()
  {
    ToggleRoot(true);
    var player = CurrentSessionPlayInfoRegistry.Get<PlayerController>();
    player?.EnterUIOverlayMode();
  }

  public void OnOverlayPopped()
  {
    ToggleRoot(false);
    var player = CurrentSessionPlayInfoRegistry.Get<PlayerController>();
    player?.ExitUIOverlayMode();
    ClearHeldItem();
  }
  #endregion

  #region Data Binding
  public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
  {
    if (_slotElements.Count == 0 || slots == null) return;

    EnsureSlotDataCapacity(_slotElements.Count);

    for (int i = 0; i < _slotElements.Count; i++)
    {
      _slotDataBuffer[i] = i < slots.Count ? slots[i] : null;
      RefreshSlotVisual(i);
    }

    if (_heldItemSourceIndex >= 0 && _heldItemSourceIndex < _slotDataBuffer.Count)
    {
      // Keep the “source” index consistent if upstream data reordered.
      if (!ReferenceEquals(_slotDataBuffer[_heldItemSourceIndex], _heldItem))
        _heldItemSourceIndex = -1;
    }
  }

  private void EnsureSlotDataCapacity(int desiredCapacity)
  {
    while (_slotDataBuffer.Count < desiredCapacity)
      _slotDataBuffer.Add(null);

    if (_slotDataBuffer.Count > desiredCapacity)
      _slotDataBuffer.RemoveRange(desiredCapacity, _slotDataBuffer.Count - desiredCapacity);
  }
  #endregion

  #region Slot Interaction (Clickable)
  private void HandleSlotClicked(int slotIndex, EventBase evt)
  {
    if (slotIndex < 0 || slotIndex >= _slotDataBuffer.Count) return;

    var clickedSlotData = _slotDataBuffer[slotIndex];

    if (_heldItem == null)
    {
      if (clickedSlotData == null || clickedSlotData.IsEmpty) return;
      StartHoldingItem(slotIndex);
    }
    else
    {
      if (clickedSlotData == null || clickedSlotData.IsEmpty)
        PlaceHeldItemIntoSlot(slotIndex);
      else
        SwapHeldItemWithSlot(slotIndex);
    }

    if (evt is IPointerEvent pointerEvt)
      UpdateHeldItemGhostPosition(pointerEvt.position);
  }

  private void StartHoldingItem(int slotIndex)
  {
    _heldItem = _slotDataBuffer[slotIndex];
    _slotDataBuffer[slotIndex] = null;
    _heldItemSourceIndex = slotIndex;

    RefreshSlotVisual(slotIndex);
    UpdateHeldItemGhostVisual(_heldItem);
  }

  private void PlaceHeldItemIntoSlot(int slotIndex)
  {
    _slotDataBuffer[slotIndex] = _heldItem;
    RefreshSlotVisual(slotIndex);
    ClearHeldItem();
  }

  private void SwapHeldItemWithSlot(int slotIndex)
  {
    (_slotDataBuffer[slotIndex], _heldItem) = (_heldItem, _slotDataBuffer[slotIndex]);

    RefreshSlotVisual(slotIndex);
    _heldItemSourceIndex = slotIndex; // New “origin” so repeated swaps feel natural.
    UpdateHeldItemGhostVisual(_heldItem);
  }

  private void ClearHeldItem()
  {
    _heldItem = null;
    _heldItemSourceIndex = -1;
    UpdateHeldItemGhostVisual(null);
  }
  #endregion

  #region Pointer Helpers
  private void OnPointerMoveWhileHolding(PointerMoveEvent evt)
  {
    if (_heldItem == null) return;
    UpdateHeldItemGhostPosition(evt.position);
  }

  private void OnPointerUpOutsideSlot(PointerUpEvent evt)
  {
    if (_heldItem == null) return;

    if (!TryGetSlotIndexFromEvent(evt.target as VisualElement, out _))
    {
      // Placeholder for future “drop to world” behaviour.
      Debug.Log("[InventoryUIController] Pointer released outside inventory grid (drop logic TBD).");
    }
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
  #endregion

  #region Visual Refresh
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
    var slotData = slotIndex < _slotDataBuffer.Count ? _slotDataBuffer[slotIndex] : null;

    if (slotData == null || slotData.IsEmpty)
    {
      if (icon != null) icon.image = null;
      if (label != null) label.text = string.Empty;
      return;
    }

    if (icon != null)
      icon.image = slotData.ItemInstance.ItemTexture.texture as Texture;

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

    _heldItemGhostIcon.image = heldData.ItemInstance.ItemTexture.texture as Texture;
    _heldItemGhostCount.text = heldData.ItemInstance != null && heldData.ItemInstance.currCount > 1
        ? heldData.ItemInstance.currCount.ToString()
        : string.Empty;

    _heldItemGhost.style.display = DisplayStyle.Flex;
    _heldItemGhost.BringToFront();
  }

  private void UpdateHeldItemGhostPosition(Vector2 panelPosition)
  {
    if (_heldItemGhost == null || _documentRoot == null) return;

    Rect rootBounds = _documentRoot.worldBound;
    float ghostWidth = Mathf.Max(1f, _heldItemGhost.resolvedStyle.width);
    float ghostHeight = Mathf.Max(1f, _heldItemGhost.resolvedStyle.height);

    _heldItemGhost.style.left = panelPosition.x - rootBounds.x - ghostWidth * 0.5f;
    _heldItemGhost.style.top = panelPosition.y - rootBounds.y - ghostHeight * 0.5f;
  }
  #endregion

  #region Helpers
  public void RebuildGrid(int newColumns, int newRows)
  {
    columns = Mathf.Max(1, newColumns);
    rows = Mathf.Max(1, newRows);
    BuildInventoryGrid();
  }
  #endregion
}
