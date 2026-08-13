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

    /// <summary>
    /// 조합으로 생성되어 커서로 지급된 뒤 아직 인벤토리 진입이 확인되지 않은 결과 아이템.
    /// 인벤토리 배치(슬롯 변화)가 감지되면 이 아이템의 지연 획득 훅(OnGet)을 발행한다.
    /// </summary>
    private ItemSystem.Item _pendingAcquisition;

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

      // ScrollView 콘텐츠 컨테이너에 수직 중앙 정렬 클래스를 적용한다.
      // UXML의 ScrollView가 뷰를 감싸고 있으며, 해상도 부족 시 가로 스크롤을 제공한다.
      var scrollView = root.Q<ScrollView>("InventoryScrollView");
      if (scrollView != null)
        scrollView.contentContainer.AddToClassList("inventory-scroll-content");

      _view.SlotsMutated += HandleSlotsMutated;
      _view.ItemDroppedOutside += HandleItemDroppedOutside;
      _view.CraftLeftoverReturned += HandleCraftLeftoverReturned;
      _view.EquipmentSlotMutated += HandleEquipmentSlotMutated;

      // 조합 패널 콜백 주입: 보유 수량 조회 + 조합 실행.
      _view.SetCraftingCallbacks(ResolveHeldCount, HandleCraftRequest);
    }

    private void DetachViewEvents()
    {
      if (_view == null)
        return;

      _view.SlotsMutated -= HandleSlotsMutated;
      _view.ItemDroppedOutside -= HandleItemDroppedOutside;
      _view.CraftLeftoverReturned -= HandleCraftLeftoverReturned;
      _view.EquipmentSlotMutated -= HandleEquipmentSlotMutated;
    }

    private PlayerController ResolveOwningPlayer()
      => Registry.Registry.GetFirstEntityComponent<PlayerController>(
           EntityType.Player, each => each != null && each.IsOwner);

    private int ResolveHeldCount(string identifier)
    {
      var player = ResolveOwningPlayer();
      return player != null ? player.CountItemInInventory(identifier) : 0;
    }

    /// <summary>
    /// 조합 패널에서 선택된 레시피의 조합을 요청받아 실행한다.
    /// 재료를 소비하고 생성된 결과 아이템을 반환한다(뷰가 커서로 pickup 처리).
    /// 결과 아이템에는 지연 획득 플래그(<see cref="ItemSystem.Item.DeferredOnGet"/>)가 설정되어 있으며,
    /// 인벤토리 진입이 확인되면 <see cref="HandleSlotsMutated"/> 에서 획득 훅(OnGet)을 발행한다.
    /// </summary>
    private ItemSystem.Item HandleCraftRequest(InventoryUIView.CraftableRecipeDisplay recipe)
    {
      if (recipe == null) return null;
      var player = ResolveOwningPlayer();
      if (player == null) return null;
      var crafted = player.TryCraftRecipe(recipe.OutputIdentifier);
      if (crafted != null && crafted.DeferredOnGet)
        _pendingAcquisition = crafted;
      return crafted;
    }

    /// <summary>커서 스택 한도를 초과한 조합 결과 잔량을 인벤토리로 돌려보낸다.</summary>
    private void HandleCraftLeftoverReturned(ItemSystem.Item leftover)
    {
      if (leftover == null || leftover.CurrentStackCount <= 0) return;
      var player = ResolveOwningPlayer();
      player?.TryAddItemToInventory(leftover);
    }

    /// <summary>
    /// 장비 슬롯의 아이템 변화(장착/해제) 발생 시 호출됩니다.
    /// 핫바 동기화 및 조합 가능 목록 갱신을 트리거합니다.
    /// </summary>
    private void HandleEquipmentSlotMutated()
    {
      // 장비 변경 시 핫바/조합 목록도 갱신 (장비 아이템이 인벤토리에서 빠지므로)
      EnsureHotbar();
      _hotbarUI?.BindInventory(_view?.BoundSlots);
      OnItemAtSelectedSlotChanged?.Invoke();
      RefreshCraftableRecipes();
    }

    private void OnDestroy()
    {
      DetachViewEvents();
      ItemSystem.EquipmentShadowSpriteProvider.ClearCache();
    }

    public void UpdateInventory(IReadOnlyList<InventorySlotModelDTO> slots)
    {
      _view?.UpdateInventory(slots);
      RefreshCraftableRecipes();
    }

    /// <summary>
    /// PlayerController 의 장비 슬롯 데이터를 뷰에 바인딩합니다.
    /// </summary>
    public void UpdateEquipment(IReadOnlyList<EquipmentSlotModelDTO> equipmentSlots)
    {
      _view?.BindEquipment(equipmentSlots);
    }

    /// <summary>조합 패널의 "조합 가능" 목록과 "필요 아이템" 표시를 현재 보유량 기준으로 갱신한다.</summary>
    private void RefreshCraftableRecipes()
    {
      if (_view == null) return;
      var player = ResolveOwningPlayer();
      _view.UpdateCraftableRecipes(player != null ? player.GetCraftableRecipes() : null);
    }

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

      // 슬롯 변화(집기/놓기/조합)에 따라 조합 가능 목록/필요 아이템 표시를 즉시 갱신.
      RefreshCraftableRecipes();

      // 조합 결과물(커서 지급)이 인벤토리에 진입했으면 지연된 획득 훅(OnGet)을 발행.
      TryResolvePendingAcquisition();
    }

    /// <summary>
    /// 조합으로 커서에 지급된 결과 아이템이 인벤토리 슬롯에 배치되었는지 확인하고,
    /// 진입이 확인되면 지연된 획득 훅(<see cref="ItemSystem.Item.OnGet"/>)을 발행한다.
    ///
    /// 조합 결과물은 커서로 지급되어 슬롯 배치 시 <see cref="PlayerController.TryAddItemToInventory"/>
    /// 를 우회하므로(뷰가 slot.SetItem/Push 로 직접 배치) 획득 훅이 생략된다. 이 메서드가 슬롯 변화를
    /// 감지해 실제 인벤토리 진입 시점에 OnGet 을 발행함으로써, MedicalItem 의 획득 신호
    /// (sig.&lt;id&gt;, sig.click_&lt;id&gt>) 등 획득 훅 로직이 조합 결과물에도 일관되게 동작한다.
    /// </summary>
    private void TryResolvePendingAcquisition()
    {
      var pending = _pendingAcquisition;
      if (pending == null || !pending.DeferredOnGet)
        return;

      var player = ResolveOwningPlayer();
      if (player == null)
        return;

      if (player.CountItemInInventory(pending.CurrentIdentifier) <= 0)
        return;

      _pendingAcquisition = null;
      pending.DeferredOnGet = false;
      pending.OnGet(player);
    }

    private void HandleItemDroppedOutside(ItemSystem.Item item)
    {
      var player = Registry.Registry.GetFirstEntityComponent<PlayerController>(EntityType.Player, each => each != null && each.IsOwner);
      player?.TryDropItemInFront(item);
    }
  }
}
