using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.ItemSystem;
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

      EnsureHotbar();
    }

    /// <summary>
    /// UIDocument는 자신의 OnEnable에서 rootVisualElement를 (재)생성해 패널에 부착합니다.
    /// 이 컨트롤러가 network-spawned 프리팹의 자식일 경우, 프리팹 비활성/재활성 과정에서
    /// UIDocument가 rootVisualElement를 다시 만들 수 있으므로, Awake에서 한 번만 캐시하면
    /// 패널에 부착되지 않은 detached 트리를 참조하게 되어 화면에 표시되지 않습니다.
    /// 따라서 뷰 바인딩은 매 OnEnable마다 현재 rootVisualElement 기준으로 (재)수행합니다.
    /// </summary>
    private void OnEnable()
    {
      if (_document == null)
        _document = GetComponent<UIDocument>();

      BindViewToCurrentDocumentRoot();
    }

    private void BindViewToCurrentDocumentRoot()
    {
      if (_document == null)
        return;

      var root = _document.rootVisualElement;
      if (root == null)
        return;

      var currentView = root.Q<InventoryUIView>();

      // 이미 현재 live root에 붙어 있는 동일한 뷰라면 재바인딩 불필요.
      if (_view != null && _view == currentView && _view.panel != null)
        return;

      // 이전 뷰가 다른(죽은) 트리에 남아 있다면 구독 해제.
      DetachViewEvents();

      _view = currentView;

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

    private void DetachViewEvents()
    {
      if (_view == null)
        return;

      _view.SlotsMutated -= HandleSlotsMutated;
      _view.ItemDroppedOutside -= HandleItemDroppedOutside;
    }

    private void OnDestroy()
    {
      DetachViewEvents();
    }

    public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots) => _view?.UpdateInventory(slots);

    public void ToggleRoot(bool visible) => _view?.SetVisible(visible);

    public void OnOverlayPushed()
    {
      // 캐시된 뷰가 현재 활성 패널에서 분리된 상태라면(예: network 프리팹 재활성으로
      // UIDocument가 rootVisualElement를 재생성한 경우) 현재 root 기준으로 다시 바인딩한다.
      if (_view == null || _view.panel == null)
        BindViewToCurrentDocumentRoot();

      _view?.SetVisible(true);
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      _view?.SetVisible(false);
      _view?.ReturnHeldItemToInventoryOnClose();
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

    private void HandleItemDroppedOutside(ItemSystem.Item item)
    {
      var player = Registry.Registry.GetFirstEntityComponent<PlayerController>(EntityType.Player, each => each != null && each.IsOwner);
      player?.TryDropItemInFront(item);
    }
  }
}
