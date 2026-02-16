using MultiplayerInfrastructure.Item;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class HeldItemActionHud : VisualElement
  {
    private const string AttackClass = "held-item-hud--attack";
    private const string UseClass = "held-item-hud--use";

    private VisualElement _icon;
    private Label _count;

    public HeldItemActionHud()
    {
      AddToClassList("held-item-hud");
      RegisterCallback<AttachToPanelEvent>(_ => CacheElements());
    }

    public void SetItem(ItemData item)
    {
      CacheElements();

      if (_icon == null || _count == null)
        return;

      if (item == null || !item.IsValid())
      {
        style.display = DisplayStyle.None;
        _icon.style.backgroundImage = null;
        _count.text = string.Empty;
        return;
      }

      style.display = DisplayStyle.Flex;
      _icon.style.backgroundImage = new StyleBackground(item.ItemTexture);
      _count.text = item.currCount > 1 ? item.currCount.ToString() : string.Empty;
    }

    public void PlayAttack() => TriggerAnimation(AttackClass, 140);

    public void PlayUseItem() => TriggerAnimation(UseClass, 140);

    private void TriggerAnimation(string className, long durationMs)
    {
      RemoveFromClassList(className);
      AddToClassList(className);
      schedule.Execute(() => RemoveFromClassList(className)).ExecuteLater(durationMs);
    }

    private void CacheElements()
    {
      if (_icon == null)
        _icon = this.Q<VisualElement>("held-item-icon");

      if (_count == null)
        _count = this.Q<Label>("held-item-count");
    }
  }
}
