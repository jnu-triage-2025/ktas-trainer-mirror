using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.UIElements;

using MI = MultiplayerInfrastructure;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// MonoBehaviour controller that owns the view, handles overlay lifecycle, and is accessed via Registry.Registry.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class InventoryUIController : UIControllerABC, IUIOverlay
  {
    [Header("Layout")]
    [SerializeField] private VisualTreeAsset slotTemplate;
    [SerializeField] private Texture2D defaultIcon;
    [SerializeField] private int columns = 9;
    [SerializeField] private int rows = 4;

    [SerializeField] private float _sortingOrder = DefaultsUIDocument.InventoryUISortOrder;


    private UIDocument _document;
    private InventoryUIView _view;
    private HotbarUIController _hotbarUI;

    public bool IsOpened => _view != null && _view.IsVisible;

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public event Action OnItemAtSelectedSlotChanged;

    protected override void Awake()
    {
      base.Awake();

      _document = GetComponent<UIDocument>();
      if (_document == null)
      {
        Debug.LogError("[InventoryUIController] UIDocument missing.");
        return;
      }
      _document.sortingOrder = _sortingOrder;

      EnsureView();
      EnsureHotbar();
    }

    private void EnsureView()
    {
      var root = _document.rootVisualElement;
      _view = root.Q<InventoryUIView>();

      if (_view == null)
      {
        _view = new InventoryUIView();
        root.Add(_view);
      }

      _view.AddToClassList("inventory-root");
      _view.name = string.IsNullOrEmpty(_view.name) ? "InventoryRoot" : _view.name;
      _view.Initialize(columns, rows, slotTemplate, defaultIcon);
      _view.SlotsMutated += HandleSlotsMutated;
      _view.ItemDroppedOutside += HandleItemDroppedOutside;
    }

    private void OnDestroy()
    {
      if (_view != null)
      {
        _view.SlotsMutated -= HandleSlotsMutated;
        _view.ItemDroppedOutside -= HandleItemDroppedOutside;
      }
    }

    public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots) => _view?.UpdateInventory(slots);

    public void ToggleRoot(bool visible) => _view?.SetVisible(visible);

    public void OnOverlayPushed()
    {
      _view?.SetVisible(true);
      Registry.Registry.Get<PlayerController>(RegistryType.Entity, Registry.Registry.TypeKey<PlayerController>())?.EnterUIOverlayMode();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      _view?.SetVisible(false);
      _view?.ReturnHeldItemToInventoryOnClose();
      Registry.Registry.Get<PlayerController>(RegistryType.Entity, Registry.Registry.TypeKey<PlayerController>())?.ExitUIOverlayMode();
      OverlayPopped?.Invoke();
    }

    public void RebuildGrid(int newColumns, int newRows)
    {
      columns = Mathf.Max(1, newColumns);
      rows = Mathf.Max(1, newRows);
      _view?.RebuildGrid(columns, rows);
    }

    private void EnsureHotbar()
    {
      if (_hotbarUI == null)
        _hotbarUI = Registry.Registry.Get<HotbarUIController>(RegistryType.UI, Registry.Registry.TypeKey<HotbarUIController>());
    }

    private void HandleSlotsMutated()
    {
      EnsureHotbar();
      _hotbarUI?.BindInventory(_view?.BoundSlots);
      OnItemAtSelectedSlotChanged?.Invoke();
    }

    private void HandleItemDroppedOutside(Item.ItemData item)
    {
      var player = Registry.Registry.Get<PlayerController>(RegistryType.Entity, Registry.Registry.TypeKey<PlayerController>());
      player?.TryDropItemInFront(item);
    }
  }
}
