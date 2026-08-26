using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
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
    private VisualElement _inventoryPanel;
    private VisualElement _equipmentPanel;

    private readonly List<VisualElement> _slotElements = new();
    private readonly List<InventorySlotModelDTO> _slotDataBuffer = new();
    private IReadOnlyList<InventorySlotModelDTO> _boundSlots;

    public event Action SlotsMutated;
    public event Action<ItemSystem.Item> ItemDroppedOutside;
    /// <summary>장비 슬롯의 아이템 변화(장착/해제) 발생 시.</summary>
    public event Action EquipmentSlotMutated;

    private InventorySlotModelDTO _heldItem;
    private VisualElement _heldItemGhost;
    private Image _heldItemGhostIcon;
    private Label _heldItemGhostCount;

    private VisualElement _tooltip;
    private Label _tooltipName;
    private Label _tooltipType;
    private Label _tooltipDescription;
    private Label _tooltipDetail;
    private Label _tooltipStack;
    private int _hoveredSlotIndex = -1;

    // ── 장비 슬롯 (Equipment Panel) ──────────────────────────────────
    /// <summary>
    /// PlayerController 에서 바인딩된 장비 슬롯 데이터.
    /// 뷰는 이 공유 참조를 직접 조작하므로, 변경 사항이 PlayerController 에 즉각 반영됩니다.
    /// </summary>
    private IReadOnlyList<EquipmentSlotModelDTO> _boundEquipmentSlots;
    private readonly List<VisualElement> _equipmentSlotElements = new();
    private readonly List<Image> _equipmentShadowImages = new();
    private readonly List<Image> _equipmentIconImages = new();
    private readonly List<Label> _equipmentCountLabels = new();

    public bool IsVisible => style.display != DisplayStyle.None;
    public IReadOnlyList<InventorySlotModelDTO> BoundSlots => _boundSlots ?? _slotDataBuffer;
    /// <summary>현재 바인딩된 장비 슬롯 데이터 (PlayerController 소유).</summary>
    public IReadOnlyList<EquipmentSlotModelDTO> EquipmentSlots => _boundEquipmentSlots;

    public void Initialize(int columns, int rows, VisualTreeAsset slotTemplate, Texture2D defaultIcon)
    {
      _columns = Mathf.Max(1, columns);
      _rows = Mathf.Max(1, rows);
      _slotTemplate = slotTemplate;
      _defaultIcon = defaultIcon;

      _inventoryGrid = this.Q<VisualElement>("InventoryGrid") ?? CreateFallbackGrid();
      // "소유 아이템" 영역(좌측 인벤토리 패널). UI 전체 높이의 기준이 된다.
      _inventoryPanel = this.Q<VisualElement>("InventoryPanel") ?? _inventoryGrid.parent;

      BuildCraftingPanel();
      BuildEquipmentPanel();
      CreateHeldItemGhost();
      CreateTooltip();
      RegisterCallback<PointerMoveEvent>(OnPointerMoveWhileHolding);
      RegisterCallback<PointerMoveEvent>(OnPointerMoveForTooltip);
      RegisterCallback<PointerUpEvent>(OnPointerUpOutsideSlot);
      // 우클릭으로 손에 든 아이템을 장비 슬롯에 바로 장착 (버블링된 이벤트 처리)
      RegisterCallback<PointerDownEvent>(OnRightClickEquipToSlot);

      BuildInventoryGrid();
      SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
      style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      if (!visible && _heldItemGhost != null)
        _heldItemGhost.style.display = DisplayStyle.None;
      if (!visible)
      {
        HideTooltip();
        ResetCraftingSelection();
      }
    }

    public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
    {
      if (_slotElements.Count == 0 || slots == null)
        return;

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
        if (slotModel == null)
          slotModel = new InventorySlotModelDTO();
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
        if (slot == null)
          continue;

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

          if (leftover == null || _heldItem.ItemInstance == null || _heldItem.ItemInstance.CurrentStackCount <= 0)
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
          slot.RegisterCallback<PointerEnterEvent>(evt => HandleSlotPointerEnter(capturedIndex, evt));
          slot.RegisterCallback<PointerLeaveEvent>(_ => HandleSlotPointerLeave(capturedIndex));

          slotRow.Add(slot);
          _slotElements.Add(slot);
          slotIndex++;
        }

        if (row == 0)
          hotbarSlotRow = slotRow;
        else
          _inventoryGrid.Add(slotRow);
      }

      if (hotbarSlotRow != null)
        _inventoryGrid.Add(hotbarSlotRow);

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

      ItemDurabilityBar.Ensure(slot, "slot__durability", "slot__durability-fill");

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

    private void CreateTooltip()
    {
      _tooltip = new VisualElement { name = "ItemTooltip" };
      _tooltip.AddToClassList("item-tooltip");
      _tooltip.style.position = Position.Absolute;
      _tooltip.style.display = DisplayStyle.None;
      _tooltip.pickingMode = PickingMode.Ignore;

      _tooltipName = new Label { name = "ItemTooltipName", pickingMode = PickingMode.Ignore };
      _tooltipName.AddToClassList("item-tooltip__name");
      _tooltip.Add(_tooltipName);

      _tooltipType = new Label { name = "ItemTooltipType", pickingMode = PickingMode.Ignore };
      _tooltipType.AddToClassList("item-tooltip__type");
      _tooltip.Add(_tooltipType);

      _tooltipDescription = new Label { name = "ItemTooltipDescription", pickingMode = PickingMode.Ignore };
      _tooltipDescription.enableRichText = true;
      _tooltipDescription.AddToClassList("item-tooltip__description");
      _tooltip.Add(_tooltipDescription);

      _tooltipDetail = new Label { name = "ItemTooltipDetail", pickingMode = PickingMode.Ignore };
      _tooltipDetail.AddToClassList("item-tooltip__detail");
      _tooltip.Add(_tooltipDetail);

      _tooltipStack = new Label { name = "ItemTooltipStack", pickingMode = PickingMode.Ignore };
      _tooltipStack.AddToClassList("item-tooltip__stack");
      _tooltip.Add(_tooltipStack);

      Add(_tooltip);
    }

    // ── 장비 슬롯 (Equipment Panel) ────────────────────────────────────────

    private void BuildEquipmentPanel()
    {
      _equipmentPanel = this.Q<VisualElement>("EquipmentPanel");
      var equipmentSlotsContainer = this.Q<VisualElement>("EquipmentSlots");
      if (equipmentSlotsContainer == null)
        return;

      // UI 요소만 생성. 실제 데이터는 BindEquipment() 에서 PlayerController 로부터 바인딩.
      CreateEquipmentSlotUI(equipmentSlotsContainer, EquipmentSlotType.Glove, "Glove", slotIndex: 0);
    }

    /// <summary>
    /// PlayerController 의 장비 슬롯 데이터를 뷰에 바인딩합니다.
    /// 공유 참조를 사용하므로 뷰의 변경이 PlayerController 에 즉각 반영됩니다.
    /// </summary>
    public void BindEquipment(IReadOnlyList<EquipmentSlotModelDTO> equipmentSlots)
    {
      if (equipmentSlots == null)
        return;
      _boundEquipmentSlots = equipmentSlots;
      RefreshAllEquipmentSlotVisuals();
    }

    private void CreateEquipmentSlotUI(VisualElement container, EquipmentSlotType slotType, string label, int slotIndex)
    {
      var slot = new VisualElement();
      slot.name = $"EquipmentSlot_{slotType}";
      slot.userData = slotIndex;
      slot.AddToClassList("equipment-slot");
      slot.pickingMode = PickingMode.Position;

      // z+2: 장비 그림자 스프라이트 (UI 위에, 아이템 아래에) — DOM 첫 번째 자식
      // 그림자는 항상 표시. 아이템 장착 시 RefreshEquipmentSlotVisual 에서 숨김.
      var shadowImage = new Image
      {
        name = "EquipmentShadow",
        pickingMode = PickingMode.Ignore,
      };
      shadowImage.AddToClassList("equipment-slot__shadow");
      shadowImage.style.display = DisplayStyle.Flex;
      slot.Add(shadowImage);
      _equipmentShadowImages.Add(shadowImage);

      // z+3: 아이템 스프라이트 (최상위) — DOM 두 번째 자식
      var iconImage = new Image
      {
        name = "EquipmentIcon",
        pickingMode = PickingMode.Ignore,
      };
      iconImage.AddToClassList("equipment-slot__icon");
      iconImage.style.display = DisplayStyle.None;
      slot.Add(iconImage);
      _equipmentIconImages.Add(iconImage);

      // 수량 라벨
      var countLabel = new Label
      {
        name = "EquipmentCount",
        pickingMode = PickingMode.Ignore,
        text = string.Empty,
      };
      countLabel.AddToClassList("equipment-slot__count");
      slot.Add(countLabel);
      _equipmentCountLabels.Add(countLabel);

      // 슬롯 타입 라벨 (하단)
      var typeLabel = new Label
      {
        name = "EquipmentLabel",
        pickingMode = PickingMode.Ignore,
        text = label,
      };
      typeLabel.AddToClassList("equipment-slot__label");
      slot.Add(typeLabel);

      int capturedIndex = slotIndex;
      slot.RegisterCallback<PointerDownEvent>(evt => HandleEquipmentSlotClicked(capturedIndex, evt));
      slot.RegisterCallback<PointerEnterEvent>(evt => HandleEquipmentSlotPointerEnter(capturedIndex, evt));
      slot.RegisterCallback<PointerLeaveEvent>(_ => HandleEquipmentSlotPointerLeave(capturedIndex));

      container.Add(slot);
      _equipmentSlotElements.Add(slot);
    }

    private void HandleEquipmentSlotClicked(int equipSlotIndex, PointerDownEvent evt)
    {
      if (_boundEquipmentSlots == null || equipSlotIndex < 0 || equipSlotIndex >= _boundEquipmentSlots.Count)
        return;

      // 우클릭은 장비 슬롯 자체 클릭 동작을 수행하지 않음 (버블링 허용)
      if (evt.button == 1)
        return;

      var equipSlot = _boundEquipmentSlots[equipSlotIndex];

      if (_heldItem == null)
      {
        // 손이 비어 있고 장비 슬롯에 아이템이 있으면 → 집기 (장비 해제)
        if (!equipSlot.IsEmpty)
        {
          var taken = equipSlot.Unequip();
          if (taken != null)
          {
            _heldItem = new InventorySlotModelDTO(taken);
            RefreshEquipmentSlotVisual(equipSlotIndex);
            UpdateHeldItemGhostVisual(_heldItem);
            NotifyEquipmentSlotMutated();
          }
        }
      }
      else
      {
        // 손에 아이템을 들고 있는 경우
        if (equipSlot.IsEmpty)
        {
          // 빈 장비 슬롯에 장착 시도
          if (equipSlot.CanAccept(_heldItem.ItemInstance))
          {
            var toEquip = _heldItem.ItemInstance;
            _heldItem = null;
            equipSlot.Equip(toEquip);
            if (equipSlot.SlotType == EquipmentSlotType.Glove)
              ScenarioInteractionSignals.Raise("wear_glove");
            RefreshEquipmentSlotVisual(equipSlotIndex);
            UpdateHeldItemGhostVisual(null);
            NotifyEquipmentSlotMutated();
          }
          // 장착 불가 → 아무 동작 없음 (손에 계속 들고 있음)
        }
        else
        {
          // 장비 슬롯에 이미 아이템이 있는 경우 → 스왑 시도
          if (equipSlot.CanAccept(_heldItem.ItemInstance))
          {
            var previous = equipSlot.Equip(_heldItem.ItemInstance);
            _heldItem = previous != null ? new InventorySlotModelDTO(previous) : null;
            if (equipSlot.SlotType == EquipmentSlotType.Glove)
              ScenarioInteractionSignals.Raise("wear_glove");
            RefreshEquipmentSlotVisual(equipSlotIndex);
            UpdateHeldItemGhostVisual(_heldItem);
            NotifyEquipmentSlotMutated();
          }
          // 장착 불가 → 아무 동작 없음
        }
      }

      UpdateHeldItemGhostPosition(evt.position);

      if (_heldItem != null)
        HideTooltip();
      else
        ShowEquipmentTooltipForSlot(equipSlotIndex, evt.position);
    }

    private void HandleEquipmentSlotPointerEnter(int equipSlotIndex, PointerEnterEvent evt)
    {
      _hoveredSlotIndex = -(equipSlotIndex + 100); // 음수 코드로 장비 슬롯 hover 구분

      if (_heldItem != null)
      {
        HideTooltip();
        return;
      }

      ShowEquipmentTooltipForSlot(equipSlotIndex, evt.position);
    }

    private void HandleEquipmentSlotPointerLeave(int equipSlotIndex)
    {
      int equipmentHoverCode = -(equipSlotIndex + 100);
      if (_hoveredSlotIndex == equipmentHoverCode)
        _hoveredSlotIndex = -1;

      HideTooltip();
    }

    private void ShowEquipmentTooltipForSlot(int equipSlotIndex, Vector2 panelPosition)
    {
      if (_tooltip == null || _boundEquipmentSlots == null || equipSlotIndex < 0 || equipSlotIndex >= _boundEquipmentSlots.Count)
        return;

      var equipSlot = _boundEquipmentSlots[equipSlotIndex];
      var item = equipSlot.ItemInstance;
      if (item == null)
      {
        HideTooltip();
        return;
      }

      ShowTooltipForItem(item, panelPosition, showStack: true);
    }

    private void RefreshEquipmentSlotVisual(int equipSlotIndex)
    {
      if (_boundEquipmentSlots == null || equipSlotIndex < 0 || equipSlotIndex >= _boundEquipmentSlots.Count)
        return;

      var equipSlot = _boundEquipmentSlots[equipSlotIndex];
      var shadowImage = _equipmentShadowImages[equipSlotIndex];
      var iconImage = _equipmentIconImages[equipSlotIndex];
      var countLabel = _equipmentCountLabels[equipSlotIndex];

      // z+2: 장비 그림자 스프라이트 — 항상 표시하되, 아이템이 장착되면 숨긴다.
      var shadowTexture = EquipmentShadowSpriteProvider.GetShadowTexture(equipSlot.SlotType);
      if (shadowTexture != null)
      {
        shadowImage.image = shadowTexture;
        shadowImage.style.display = equipSlot.IsEmpty ? DisplayStyle.Flex : DisplayStyle.None;
      }
      else
      {
        shadowImage.style.display = DisplayStyle.None;
      }

      if (equipSlot.IsEmpty)
      {
        iconImage.style.display = DisplayStyle.None;
        countLabel.text = string.Empty;
        return;
      }

      var item = equipSlot.ItemInstance;

      // z+3: 아이템 스프라이트 (장착 시에만 표시)
      var itemSprite = item?.CurrentItemIconTexture;
      if (itemSprite != null)
      {
        iconImage.image = itemSprite.texture;
        iconImage.style.display = DisplayStyle.Flex;
      }
      else
      {
        iconImage.image = _defaultIcon;
        iconImage.style.display = DisplayStyle.Flex;
      }

      // 수량 라벨
      countLabel.text = item != null && item.CurrentStackCount > 1
        ? item.CurrentStackCount.ToString()
        : string.Empty;
    }

    private void RefreshAllEquipmentSlotVisuals()
    {
      if (_boundEquipmentSlots == null)
        return;
      for (int i = 0; i < _boundEquipmentSlots.Count; i++)
        RefreshEquipmentSlotVisual(i);
    }

    private void NotifyEquipmentSlotMutated()
    {
      EquipmentSlotMutated?.Invoke();
    }

    /// <summary>
    /// 지정 EquipmentSlotType 에 해당하는 장비 슬롯의 인덱스를 찾습니다. 없으면 -1.
    /// </summary>
    private int FindEquipmentSlot(EquipmentSlotType slotType)
    {
      if (_boundEquipmentSlots == null)
        return -1;
      for (int i = 0; i < _boundEquipmentSlots.Count; i++)
      {
        if (_boundEquipmentSlots[i].SlotType == slotType)
          return i;
      }
      return -1;
    }

    /// <summary>
    /// 인벤토리 슬롯의 아이템을 장비 슬롯에 바로 장착합니다 (Shift+클릭).
    /// 장비 슬롯이 비어 있어야 하며, 아이템이 장착 가능해야 합니다.
    /// 성공 시 true, 실패 시 false 를 반환합니다.
    /// </summary>
    private bool TryEquipFromInventorySlot(int slotIndex)
    {
      var slotData = GetSlotModel(slotIndex);
      if (slotData == null || slotData.IsEmpty)
        return false;

      var item = slotData.ItemInstance;
      if (item == null)
        return false;

      // 장착 가능 여부 확인 (현재는 Glove 만 지원)
      EquipmentSlotType targetSlotType;
      if (EquipmentAttributeHelper.IsEquippableGlove(item))
        targetSlotType = EquipmentSlotType.Glove;
      else
        return false;

      int equipIdx = FindEquipmentSlot(targetSlotType);
      if (equipIdx < 0)
        return false;

      var equipSlot = _boundEquipmentSlots[equipIdx];

      // 슬롯에 이미 아이템이 있으면 동작하지 않음 (Shift+클릭 규칙)
      if (!equipSlot.IsEmpty)
        return false;

      // 인벤토리 슬롯에서 아이템을 꺼내 장비 슬롯에 장착
      var taken = slotData.TakeAll();
      if (taken == null)
        return false;

      equipSlot.Equip(taken);
      if (targetSlotType == EquipmentSlotType.Glove)
        ScenarioInteractionSignals.Raise("wear_glove");
      RefreshSlotVisual(slotIndex);
      RefreshEquipmentSlotVisual(equipIdx);
      NotifySlotsMutated();
      NotifyEquipmentSlotMutated();
      return true;
    }

    /// <summary>
    /// 손에 든 아이템을 해당 장비 슬롯에 장착합니다 (우클릭).
    /// 장비 슬롯이 비어 있으면 장착, 이미 있으면 서로 교체합니다.
    /// </summary>
    private void TryEquipHeldItemToSlot(EquipmentSlotType targetSlotType)
    {
      if (_heldItem == null || _heldItem.IsEmpty)
        return;

      int equipIdx = FindEquipmentSlot(targetSlotType);
      if (equipIdx < 0)
        return;

      var equipSlot = _boundEquipmentSlots[equipIdx];
      var heldItemInstance = _heldItem.ItemInstance;

      if (!equipSlot.CanAccept(heldItemInstance))
        return;

      if (equipSlot.IsEmpty)
      {
        // 빈 슬롯에 장착
        equipSlot.Equip(heldItemInstance);
        _heldItem = null;
      }
      else
      {
        // 슬롯에 이미 있으면 서로 교체: 슬롯 아이템 → 손, 손 아이템 → 슬롯
        var previous = equipSlot.Equip(heldItemInstance);
        _heldItem = previous != null ? new InventorySlotModelDTO(previous) : null;
      }

      if (targetSlotType == EquipmentSlotType.Glove)
        ScenarioInteractionSignals.Raise("wear_glove");

      RefreshEquipmentSlotVisual(equipIdx);
      UpdateHeldItemGhostVisual(_heldItem);
      NotifyEquipmentSlotMutated();
    }

    /// <summary>
    /// 전역 우클릭 핸들러: 손에 아이템을 들고 있을 때 우클릭하면 장비 슬롯에 바로 장착/교체.
    /// 인벤토리/장비 슬롯의 PointerDown 핸들러에서 버블링된 우클릭 이벤트를 여기서 처리합니다.
    /// </summary>
    private void OnRightClickEquipToSlot(PointerDownEvent evt)
    {
      if (evt.button != 1)
        return;
      if (_heldItem == null || _heldItem.IsEmpty)
        return;

      var item = _heldItem.ItemInstance;
      if (item == null)
        return;

      // 장착 가능 여부 확인 (현재는 Glove 만 지원)
      if (EquipmentAttributeHelper.IsEquippableGlove(item))
        TryEquipHeldItemToSlot(EquipmentSlotType.Glove);
    }

    // ── 인벤토리 슬롯 포인터 이벤트 ────────────────────────────────────────

    private void HandleSlotPointerEnter(int slotIndex, PointerEnterEvent evt)
    {
      _hoveredSlotIndex = slotIndex;

      // 아이템을 들고 있는 동안에는 ghost가 우선이며 툴팁은 방해되므로 표시하지 않는다.
      if (_heldItem != null)
      {
        HideTooltip();
        return;
      }

      ShowTooltipForSlot(slotIndex, evt.position);
    }

    private void HandleSlotPointerLeave(int slotIndex)
    {
      if (_hoveredSlotIndex == slotIndex)
        _hoveredSlotIndex = -1;

      HideTooltip();
    }

    private void ShowTooltipForSlot(int slotIndex, Vector2 panelPosition)
    {
      if (_tooltip == null)
        return;

      var slotData = GetSlotModel(slotIndex);
      var item = slotData?.ItemInstance;
      if (slotData == null || slotData.IsEmpty || item == null)
      {
        HideTooltip();
        return;
      }

      ShowTooltipForItem(item, panelPosition, showStack: true);
    }

    /// <summary>
    /// 임의의 아이템 인스턴스에 대한 툴팁을 표시한다.
    /// 슬롯 hover(<see cref="ShowTooltipForSlot"/>) 뿐 아니라 조합 목록 항목 hover 에서도 재사용된다.
    /// </summary>
    /// <param name="showStack">스택 수량(N/Max) 표시 여부. 조합 목록 항목은 인스턴스가 아니므로 false.</param>
    internal void ShowTooltipForItem(Item item, Vector2 panelPosition, bool showStack)
    {
      if (_tooltip == null || item == null)
        return;

      SetLabel(_tooltipName, string.IsNullOrEmpty(item.CurrentDisplayName) ? item.CurrentIdentifier : item.CurrentDisplayName);
      // 아이템의 CurrentColor를 이름 색으로 사용(희소도/카테고리 필드가 없으므로 색상으로 구분).
      _tooltipName.style.color = item.CurrentColor;

      SetLabel(_tooltipType, item.CurrentIdentifier);
      SetLabel(_tooltipDescription, item.CurrentDescription);
      SetLabel(_tooltipDetail, item.CurrentDetailComment);

      string stackText = showStack && item.IsCurrentlyStackable
        ? $"{item.CurrentStackCount} / {item.CurrentMaxStackCount}"
        : string.Empty;
      SetLabel(_tooltipStack, stackText);

      _tooltip.style.display = DisplayStyle.Flex;
      _tooltip.BringToFront();
      UpdateTooltipPosition(panelPosition);
    }

    private static void SetLabel(Label label, string text)
    {
      if (label == null)
        return;

      bool hasText = !string.IsNullOrWhiteSpace(text);
      label.text = hasText ? text : string.Empty;
      label.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HideTooltip()
    {
      if (_tooltip != null)
        _tooltip.style.display = DisplayStyle.None;
    }

    private void OnPointerMoveForTooltip(PointerMoveEvent evt)
    {
      if (_tooltip == null || _tooltip.style.display.value == DisplayStyle.None)
        return;

      UpdateTooltipPosition(evt.position);
    }

    private void UpdateTooltipPosition(Vector2 panelPosition)
    {
      if (_tooltip == null)
        return;

      Rect rootBounds = worldBound;
      const float offsetX = 16f;
      const float offsetY = 16f;

      float localX = panelPosition.x - rootBounds.x + offsetX;
      float localY = panelPosition.y - rootBounds.y + offsetY;

      // 툴팁이 인벤토리 루트 밖으로 넘치지 않도록 오른쪽/아래 경계에서 보정한다.
      float tooltipWidth = Mathf.Max(1f, _tooltip.resolvedStyle.width);
      float tooltipHeight = Mathf.Max(1f, _tooltip.resolvedStyle.height);

      if (localX + tooltipWidth > rootBounds.width)
        localX = panelPosition.x - rootBounds.x - tooltipWidth - offsetX;
      if (localY + tooltipHeight > rootBounds.height)
        localY = panelPosition.y - rootBounds.y - tooltipHeight - offsetY;

      _tooltip.style.left = Mathf.Max(0f, localX);
      _tooltip.style.top = Mathf.Max(0f, localY);
    }

    private void HandleSlotClicked(int slotIndex, PointerDownEvent evt)
    {
      if (slotIndex < 0 || slotIndex >= _slotElements.Count)
        return;

      // 우클릭: 전역 우클릭 핸들러(장비 장착)로 버블링 허용
      if (evt.button == 1)
        return;

      // Shift + 좌클릭: [Equippable*] 아이템을 장비 슬롯에 바로 장착
      if (evt.shiftKey && _heldItem == null)
      {
        var slotData = GetSlotModel(slotIndex);
        if (slotData != null && !slotData.IsEmpty && TryEquipFromInventorySlot(slotIndex))
        {
          HideTooltip();
          return;
        }
        // 장착 불가 아이템이면 일반 pick-up 으로 폴스루
      }

      if (_heldItem == null)
        TryPickUpFromSlot(slotIndex);
      else
        TryPlaceHeldItemIntoSlot(slotIndex);

      UpdateHeldItemGhostPosition(evt.position);

      // 집는 중에는 툴팁을 숨기고, 아이템을 내려놓아 손이 비었으면 현재 슬롯 기준으로 다시 표시한다.
      if (_heldItem != null)
        HideTooltip();
      else
        ShowTooltipForSlot(slotIndex, evt.position);
    }

    private void TryPickUpFromSlot(int slotIndex)
    {
      var slotModel = GetSlotModel(slotIndex);
      if (slotModel == null || slotModel.IsEmpty)
        return;

      var taken = slotModel.TakeAll();
      if (taken == null)
        return;

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

        if (leftover == null || _heldItem.ItemInstance == null || _heldItem.ItemInstance.CurrentStackCount <= 0)
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
      if (_heldItem == null)
        return;
      UpdateHeldItemGhostPosition(evt.position);
    }

    private void OnPointerUpOutsideSlot(PointerUpEvent evt)
    {
      if (_heldItem == null)
        return;

      var target = evt.target as VisualElement;

      // 조합 패널 또는 장비 패널 위에서 손을 뗀 경우에는 아이템을 바닥에 버리지 않는다.
      // 이들은 인벤토리 UI의 정당한 영역이므로 "슬롯 바깥 = 월드에 드롭" 규칙에서 제외한다.
      if (IsWithinCraftingPanel(target))
        return;
      if (IsWithinEquipmentPanel(target))
        return;

      if (!TryGetSlotIndexFromEvent(target, out _))
      {
        var item = _heldItem.ItemInstance;
        ClearHeldItem();
        NotifySlotsMutated();
        if (item != null)
          ItemDroppedOutside?.Invoke(item);
      }
    }

    private bool TryGetSlotIndexFromEvent(VisualElement target, out int slotIndex)
    {
      slotIndex = -1;
      if (target == null)
        return false;

      VisualElement candidate = target;
      while (candidate != null && !_slotElements.Contains(candidate))
        candidate = candidate.parent;

      if (candidate == null)
        return false;

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
      if (slotIndex < 0 || slotIndex >= _slotElements.Count)
        return;

      var slot = _slotElements[slotIndex];
      var icon = slot.Q<Image>("ItemIcon");
      var label = slot.Q<Label>("ItemCount");
      var durability = ItemDurabilityBar.Ensure(
        slot, "slot__durability", "slot__durability-fill");
      var slotData = GetSlotModel(slotIndex);

      if (slotData == null || slotData.IsEmpty)
      {
        if (icon != null)
          icon.image = _defaultIcon;
        if (label != null)
          label.text = string.Empty;
        ItemDurabilityBar.Update(durability, null);
        return;
      }

      if (icon != null)
      {
        var sprite = slotData.ItemInstance?.CurrentItemIconTexture;
        icon.image = sprite != null ? sprite.texture : _defaultIcon;
      }

      if (label != null)
        label.text = slotData.ItemInstance != null && slotData.ItemInstance.CurrentStackCount > 1
          ? slotData.ItemInstance.CurrentStackCount.ToString()
          : string.Empty;
      ItemDurabilityBar.Update(durability, slotData.ItemInstance);
    }

    private void UpdateHeldItemGhostVisual(InventorySlotModelDTO heldData)
    {
      if (_heldItemGhost == null)
        return;

      if (heldData == null || heldData.IsEmpty)
      {
        _heldItemGhost.style.display = DisplayStyle.None;
        _heldItemGhostIcon.image = null;
        _heldItemGhostCount.text = string.Empty;
        return;
      }

      var sprite = heldData.ItemInstance?.CurrentItemIconTexture;
      _heldItemGhostIcon.image = sprite != null ? sprite.texture : _defaultIcon;
      _heldItemGhostCount.text = heldData.ItemInstance != null && heldData.ItemInstance.CurrentStackCount > 1
        ? heldData.ItemInstance.CurrentStackCount.ToString()
        : string.Empty;

      _heldItemGhost.style.display = DisplayStyle.Flex;
      _heldItemGhost.BringToFront();
    }

    private void UpdateHeldItemGhostPosition(Vector2 panelPosition)
    {
      if (_heldItemGhost == null)
        return;

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
      if (slotIndex < 0)
        return null;

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

    /// <summary>대상 요소가 장비 패널(또는 그 자식) 내부인지 검사한다.</summary>
    private bool IsWithinEquipmentPanel(VisualElement target)
    {
      if (target == null || _equipmentPanel == null)
        return false;
      var cur = target;
      while (cur != null)
      {
        if (cur == _equipmentPanel)
          return true;
        cur = cur.parent;
      }
      return false;
    }
  }
}
