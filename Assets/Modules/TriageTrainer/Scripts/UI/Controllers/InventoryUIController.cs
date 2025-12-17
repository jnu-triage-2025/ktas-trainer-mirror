using System.Collections.Generic;
using TriageTrainer.Player;
using TriageTrainer.Registry;
using TriageTrainer.UI;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryUIController : UIControllerABC, IUIOverlay
{
  #region Properties
  [Header("Layout")]
  [SerializeField] private int columns = 9;
  [SerializeField] private int rows = 4;

  [Header("Slot Template (optional)")]
  [SerializeField] private VisualTreeAsset slotTemplate;

  [Header("Textures")]
  [SerializeField] private Texture2D defaultIcon;

  private UIDocument _uiDocument;
  private VisualElement _documentRoot;
  private VisualElement _inventoryGrid;
  private readonly List<VisualElement> _slotElements = new();

  private List<InventorySlotModelDTO> _slotData;

  public bool IsOpened => _documentRoot != null && _documentRoot.style.display != DisplayStyle.None;
  #endregion

  
  protected virtual void Awake()
  {
    base.Awake();

    _uiDocument = GetComponent<UIDocument>();
    _documentRoot = _uiDocument.rootVisualElement;
    _inventoryGrid = _documentRoot.Q<VisualElement>("InventoryGrid");

    BuildInventoryGrid();
    ToggleRoot(false); // start hidden
  }

  private void BuildInventoryGrid()
  {
    _slotElements.Clear();
    _inventoryGrid?.Clear();

    /**
     * 핫바가 제일 먼저 채워져야 하고, 그 뒤로부터는 위에서부터 아래로 채워져야 하므로
     * 핫바를 제일 마지막에 배치, 핫바 다음 순번의 슬롯은 원래 순서대로 배치하기 위해
     * 핫바를 초회 반복 과정에 할당해놓고 마지막 단계에 추가
     */
    VisualElement hotbarSlotRow = null;

    for (int i = 0; i < rows; i++)
    {
      VisualElement slotRow = CreateSlotRow();
      for (int j = 0; j < columns; j++)
      {
        VisualElement slot = slotTemplate != null
          ? slotTemplate.Instantiate()
          : CreateDefaultSlot();

        slot.name = $"Slot_{i * j}";
        slot.AddToClassList("slot");
        slotRow.Add(slot);
        _slotElements.Add(slot);
      }
      if (i == 0) hotbarSlotRow = slotRow;
      else _inventoryGrid.Add(slotRow);
    }
    Debug.Log(hotbarSlotRow);
    if (hotbarSlotRow != null) _inventoryGrid.Add(hotbarSlotRow);
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

  public void ToggleRoot(bool visible)
  {
    Debug.Log("[Inventory Controller] try to open");
    if (_documentRoot == null) return;
    Debug.Log("[Invenotry Controller] opened");
    _documentRoot.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
    _documentRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
  }

  /// <summary>
  /// Called by the player controller when the data is dirty and the UI is visible.
  /// </summary>
  public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
  {
    if (_slotElements.Count == 0 || slots == null) return;

    int count = Mathf.Min(_slotElements.Count, slots.Count);

    for (int i = 0; i < count; i++)
    {
      var slot = _slotElements[i];
      var icon = slot.Q<Image>("ItemIcon");
      var label = slot.Q<Label>("ItemCount");

      var slotData = slots[i];
      if (slotData == null || slotData.IsEmpty)
      {
        if (icon != null) icon.image = null;
        if (label != null) label.text = string.Empty;
        continue;
      }

      if (icon != null)
      {
        icon.image = ResolveItemTexture(slotData.ItemInstance) ?? defaultIcon;
      }

      if (label != null)
      {
        label.text = slotData.ItemInstance!.currCount > 1
            ? slotData.ItemInstance.currCount.ToString()
            : string.Empty;
      }
    }
  }

  private Texture2D ResolveItemTexture(ItemInstanceModelDTO itemInstance)
  {
    if (itemInstance == null) return null;
    var registry = ItemRegistry.Instance;
    if (registry == null) return null;

    var key = string.IsNullOrWhiteSpace(itemInstance.itemTextureIdentifier)
      ? itemInstance.identifier
      : itemInstance.itemTextureIdentifier;

    var sprite = registry.GetItemIcon(key);
    Debug.Log(sprite);
    return sprite?.texture as Texture2D;
  }

  public void RebuildGrid(int newColumns, int newRows)
  {
    columns = Mathf.Max(1, newColumns);
    rows = Mathf.Max(1, newRows);
    BuildInventoryGrid();
  }

  public void OnOverlayPushed()
  {
    ToggleRoot(true);
    var player = CurrentSessionPlayInfoRegistry.Get<PlayerController>();
    if (player != null) player.EnterUIOverlayMode();
  }

  public void OnOverlayPopped()
  {
    ToggleRoot(false);
    var player = CurrentSessionPlayInfoRegistry.Get<PlayerController>();
    if (player != null) player.ExitUIOverlayMode();
  }
}
