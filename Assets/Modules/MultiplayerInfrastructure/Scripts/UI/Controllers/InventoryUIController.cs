using System;
using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 뷰를 소유하고 오버레이 수명 주기를 처리하며 Registry.Registry 를 통해 접근되는
  /// MonoBehaviour 컨트롤러이다.
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

    /// <summary>
    /// 인벤토리가 열려 있는지 여부. 문서 루트의 픽킹 상태를 결정하는 기준이며,
    /// UIDocument가 root를 다시 만들어 뷰를 재바인딩할 때에도 이 값으로 상태를 복원한다.
    /// </summary>
    private bool _isOpened;

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

      // rootVisualElement 생성이 늦어져 바인딩이 미뤄지는 경우에도
      // 닫혀 있는 인벤토리 문서가 아래 문서의 클릭과 휠을 가로채지 않도록 한다.
      if (!_isOpened)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
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

      // ScrollView 콘텐츠 컨테이너에 가로·세로 중앙 정렬을 적용한다.
      // UXML의 ScrollView가 뷰를 감싸고 있으며, 해상도 부족 시 가로 스크롤을 제공한다.
      var scrollView = root.Q<ScrollView>("InventoryScrollView");
      if (scrollView != null)
        ApplyScrollContentCentering(scrollView.contentContainer);

      _view.SlotsMutated += HandleSlotsMutated;
      _view.ItemDroppedOutside += HandleItemDroppedOutside;
      _view.CraftLeftoverReturned += HandleCraftLeftoverReturned;
      _view.EquipmentSlotMutated += HandleEquipmentSlotMutated;

      // 조합 패널 콜백 주입: 보유 수량 조회 + 조합 실행.
      _view.SetCraftingCallbacks(ResolveHeldCount, HandleCraftRequest);

      // 뷰를 다시 바인딩한 직후에도 문서의 픽킹 상태를 현재 열림 여부와 맞춘다.
      ApplyDocumentInteractable(_isOpened);
    }

    /// <summary>
    /// 인벤토리 문서 전체의 표시와 포인터 히트테스트 상태를 함께 전환한다.
    ///
    /// InventoryUI 문서는 화면 전체를 채우는 래퍼(<c>InventoryUIWrapper</c>)와 ScrollView를 항상 가지고 있고,
    /// sortingOrder 가 6 이라서 그보다 아래에 있는 문서(퀘스트 패널 4, 크로스헤어 1, 핫바 등)보다 위에 놓인다.
    /// 인벤토리 뷰만 display:none 으로 숨기면 이 래퍼와 ScrollView 가 그대로 남아
    /// 화면 어디를 클릭하거나 휠을 굴려도 아래 문서 대신 인벤토리 문서가 입력을 가져간다.
    /// 따라서 닫혀 있는 동안에는 문서 루트까지 함께 비활성화해야 한다.
    /// </summary>
    private void ApplyDocumentInteractable(bool interactable)
    {
      if (_document == null)
        _document = GetComponent<UIDocument>();

      SetDocumentVisible(_document, interactable);
    }

    /// <summary>
    /// 인벤토리 창을 ScrollView 뷰포트의 가로·세로 중앙에 배치한다.
    ///
    /// 콘텐츠 컨테이너는 ScrollView가 내부적으로 만드는 요소라 UXML에서 지정할 수 없고,
    /// UI Toolkit 기본 테마가 이 요소(<c>unity-content-container</c>)에 자체 스타일을 적용한다.
    /// 정렬 값이 테마 규칙에 밀리지 않도록, USS 클래스(<c>inventory-scroll-content</c>)와 함께
    /// 항상 우선하는 인라인 스타일로도 지정한다. 두 정의는 같은 값을 유지해야 한다.
    ///
    /// 세로 중앙 정렬(justify-content)은 컨테이너가 뷰포트 높이를 실제로 차지할 때만 의미가 있으므로,
    /// min-height 백분율에만 기대지 않고 flex-grow로 남는 세로 공간을 흡수한다.
    /// </summary>
    private static void ApplyScrollContentCentering(VisualElement content)
    {
      if (content == null)
        return;

      content.AddToClassList("inventory-scroll-content");

      content.style.flexGrow = 1f;
      content.style.flexShrink = 0f;
      content.style.minHeight = Length.Percent(100f);
      content.style.justifyContent = Justify.Center;
      content.style.alignItems = Align.Center;
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
      if (recipe == null)
        return null;
      var player = ResolveOwningPlayer();
      if (player == null)
        return null;
      var crafted = player.TryCraftRecipe(recipe.OutputIdentifier);
      if (crafted != null && crafted.DeferredOnGet)
        _pendingAcquisition = crafted;
      return crafted;
    }

    /// <summary>커서 스택 한도를 초과한 조합 결과 잔량을 인벤토리로 돌려보낸다.</summary>
    private void HandleCraftLeftoverReturned(ItemSystem.Item leftover)
    {
      if (leftover == null || leftover.CurrentStackCount <= 0)
        return;
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

    protected override void OnDestroy()
    {
      DetachViewEvents();
      ItemSystem.EquipmentShadowSpriteProvider.ClearCache();
      base.OnDestroy();
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
      if (_view == null)
        return;
      var player = ResolveOwningPlayer();
      _view.UpdateCraftableRecipes(player != null ? player.GetCraftableRecipes() : null);
    }

    public void ToggleRoot(bool visible)
    {
      _isOpened = visible;
      _view?.SetVisible(visible);
      ApplyDocumentInteractable(visible);
    }

    /// <summary>
    /// 커서가 올라가 있는 인벤토리 슬롯과 지정한 핫바 슬롯의 아이템을 서로 맞바꾼다.
    /// 인벤토리가 열려 있는 동안 핫바 숫자 키 입력을 처리하기 위해 PlayerController 가 호출한다.
    /// </summary>
    /// <returns>실제로 교환이 일어났으면 true.</returns>
    public bool TrySwapHoveredSlotWithHotbarSlot(int hotbarSlotIndex)
      => _view != null && _view.TrySwapHoveredSlotWithHotbarSlot(hotbarSlotIndex);

    public void OnOverlayPushed()
    {
      // 캐시된 뷰가 현재 활성 패널에서 분리된 상태라면(예: network 프리팹 재활성으로
      // UIDocument가 rootVisualElement를 재생성한 경우) 현재 root 기준으로 다시 바인딩한다.
      if (_view == null || _view.panel == null)
        BindViewToCurrentDocumentRoot();

      _isOpened = true;
      ApplyDocumentInteractable(true);
      _view?.SetVisible(true);
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      _isOpened = false;
      _view?.SetVisible(false);
      _view?.ReturnHeldItemToInventoryOnClose();
      ApplyDocumentInteractable(false);
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

      // 조합 결과물을 포함해 UI가 슬롯을 직접 변경하는 경로는 PlayerController의 인벤토리
      // 변경 공통 처리를 거치지 않는다. 소지 아이템을 조건으로 하는 주변 상호작용이 현재
      // 감지 범위 안에서도 즉시 나타나거나 사라지도록 힌트를 다시 계산한다.
      ResolveOwningPlayer()?.RefreshInteractableHintsNow();

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
