using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Item;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class HeldItemActionHudUIController : UIControllerABC
  {
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.HeldItemHudSortOrder;
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet _styleSheet;

    private HeldItemActionHud _hud;

    protected override void Awake()
    {
      base.Awake();

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      if (_uiDocument != null)
        _uiDocument.sortingOrder = _sortingOrder;

      BindHud();
    }

    public void SetItem(ItemData item)
    {
      EnsureHud();
      _hud?.SetItem(item);
    }

    public void PlayAttack() => _hud?.PlayAttack();

    public void PlayUseItem() => _hud?.PlayUseItem();

    private void BindHud()
    {
      if (_uiDocument == null)
        return;

      var root = _uiDocument.rootVisualElement;
      EnsureStyleSheet(root);

      _hud = root.Q<HeldItemActionHud>("held-item-hud") ?? root.Q<HeldItemActionHud>();
      if (_hud == null)
      {
        _hud = new HeldItemActionHud();
        EnsureStyleSheet(_hud);
        root.Add(_hud);
      }
    }

    private void EnsureHud()
    {
      if (_hud == null)
        BindHud();
    }

    private void EnsureStyleSheet(VisualElement element)
    {
      if (element == null || _styleSheet == null)
        return;

      if (!element.styleSheets.Contains(_styleSheet))
        element.styleSheets.Add(_styleSheet);
    }
  }
}
