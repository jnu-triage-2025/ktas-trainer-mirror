using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class HotbarUIController : UIControllerABC
  {
    [Header("Config")]
    [SerializeField] private int hotbarSlotCount = 9;

    [Tooltip("아이템 이름이 표시된 후 자동으로 사라지기까지의 시간(초)")]
    [SerializeField] private float _itemNameDisplayDuration = 1.5f;

    [Header("References")]
    [SerializeField] private UIDocument _uiDocument;

    private HotbarControl _hotbar;
    private IReadOnlyList<InventorySlotModelDTO> _boundInventory;
    private Label _itemNameLabel;
    private Coroutine _itemNameHideRoutine;
    [Header("State")]
    [SerializeField] private int _selectedSlot = 0;

    public event Action OnSelectedSlotChanged;

    public int SelectedSlot
    {
      get { return _selectedSlot; }
    }

    /// <summary>현재 핫바가 실제로 표시 중인 슬롯 수. 초기화 전에는 설정값을 반환한다.</summary>
    public int SlotCount => _hotbar != null ? _hotbar.SlotCount : hotbarSlotCount;

    public bool SetupHotbarUI(bool logFailure = true)
    {
      if (_uiDocument.IsUnityNull())
        _uiDocument = GetComponent<UIDocument>();

      if (_uiDocument.IsUnityNull())
      {
        if (logFailure)
          Debug.LogError("[HotbarUIController] UIDocument is null");
        return false;
      }

      var root = _uiDocument.rootVisualElement;
      if (root == null)
      {
        // UIDocument 가 비활성이거나 패널이 아직 만들어지지 않은 동안(예: 애디티브
        // 씬 로드 순서)에는 rootVisualElement 가 null 이다. 여기서 예외를 던지면
        // FishNet 의 OnStartClient 콜백 체인이 중단된다.
        if (logFailure)
          Debug.LogWarning("[HotbarUIController] rootVisualElement is not ready yet. Skipping hotbar setup.");
        return false;
      }

      _hotbar = root.Q<HotbarControl>("hotbar-root");
      _itemNameLabel = root.Q<Label>("hotbar-item-name");

      if (_hotbar.IsUnityNull())
      {
        if (logFailure)
          Debug.LogError("[HotbarUIController] Hotbar is null");
        return false;
      }

      _hotbar.Initialize(Math.Clamp(hotbarSlotCount, HotbarControl.MinSlotSize, HotbarControl.MaxSlotSize));
      if (_boundInventory != null)
        _hotbar.BindInventory(_boundInventory);
      // _hotbar.BindInventory(Inventory);
      _hotbar.SetSelectedIndex(0);
      // SetupHotbarUI 가 여러 번 호출되어도(플레이어 리스폰 등) 핸들러가 중복 누적되지 않도록
      // 구독 전에 항상 해제한다(idempotent).
      _hotbar.OnSlotSelected -= OnHotbarSlotSelected;
      _hotbar.OnHeldItemNameChanged -= OnHeldItemNameChanged;
      _hotbar.OnSlotSelected += OnHotbarSlotSelected;
      _hotbar.OnHeldItemNameChanged += OnHeldItemNameChanged;
      return true;
    }

    /// <summary>
    /// 손에 들고 있는 아이템 이름이 변경되면 핫바 위 중앙 라벨을 잠깐 표시한다.
    /// 표시 후 <see cref="_itemNameDisplayDuration"/> 초가 지나면 자동으로 사라진다.
    /// 이름이 비어있으면(슬롯이 비어있으면) 즉시 숨긴다.
    /// </summary>
    private void OnHeldItemNameChanged(string itemName)
    {
      if (_itemNameLabel == null)
        return;

      if (_itemNameHideRoutine != null)
      {
        StopCoroutine(_itemNameHideRoutine);
        _itemNameHideRoutine = null;
      }

      if (string.IsNullOrEmpty(itemName))
      {
        HideItemNameLabel();
        return;
      }

      _itemNameLabel.text = itemName;
      _itemNameLabel.RemoveFromClassList("hotbar__item-name--hidden");

      if (isActiveAndEnabled)
        _itemNameHideRoutine = StartCoroutine(HideItemNameAfterDelay());
    }

    private IEnumerator HideItemNameAfterDelay()
    {
      yield return new WaitForSeconds(_itemNameDisplayDuration);
      HideItemNameLabel();
      _itemNameHideRoutine = null;
    }

    private void HideItemNameLabel()
    {
      if (_itemNameLabel == null)
        return;
      _itemNameLabel.AddToClassList("hotbar__item-name--hidden");
      _itemNameLabel.text = string.Empty;
    }
    protected override void Awake()
    {
      base.Awake();
    }
    private void OnHotbarSlotSelected(int slot)
    {
      _selectedSlot = slot;
      OnSelectedSlotChanged?.Invoke();
    }

    private void SyncHotbar()
    {
      if (_hotbar.IsUnityNull())
        return;
      // _hotbar.BindInventory(Inventory);
      _hotbar.ForceRefresh();
    }

    private bool EnsureHotbarReady()
    {
      if (_hotbar != null)
        return true;
      SetupHotbarUI();
      return _hotbar != null;
    }

    public void SetSelectedIndex(int index)
    {
      if (EnsureHotbarReady())
        _hotbar.SetSelectedIndex(index);
    }

    public void CycleSelection(int direction)
    {
      if (EnsureHotbarReady())
        _hotbar.CycleSelection(direction);
    }

    public void BindInventory(IReadOnlyList<InventorySlotModelDTO> inventory)
    {
      _boundInventory = inventory;
      if (EnsureHotbarReady())
        _hotbar.BindInventory(inventory);
    }

    /// <summary>UIDocument가 준비되기 전에도 이후 바인딩할 인벤토리를 보관한다.</summary>
    public void CacheInventory(IReadOnlyList<InventorySlotModelDTO> inventory)
    {
      _boundInventory = inventory;
    }
  }
}
