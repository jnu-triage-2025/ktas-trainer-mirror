using MultiplayerInfrastructure.ItemSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>인벤토리 계열 슬롯에서 공통으로 사용하는 아이템 내구도 막대입니다.</summary>
  public static class ItemDurabilityBar
  {
    public const string TrackName = "DurabilityTrack";
    public const string FillName = "DurabilityFill";

    private static readonly Color HighColor = new Color32(72, 199, 71, 255);
    private static readonly Color MediumColor = new Color32(205, 220, 57, 255);
    private static readonly Color LowColor = new Color32(255, 167, 38, 255);
    private static readonly Color CriticalColor = new Color32(239, 83, 80, 255);

    public static VisualElement Ensure(VisualElement slot, string trackClass, string fillClass)
    {
      var track = slot.Q<VisualElement>(TrackName);
      if (track != null)
        return track;

      track = new VisualElement { name = TrackName, pickingMode = PickingMode.Ignore };
      track.AddToClassList(trackClass);
      var fill = new VisualElement { name = FillName, pickingMode = PickingMode.Ignore };
      fill.AddToClassList(fillClass);
      track.Add(fill);
      slot.Add(track);
      return track;
    }

    public static void Update(VisualElement track, Item item)
    {
      if (track == null)
        return;

      bool visible = item != null
                     && item.HasCurrentDurability
                     && item.CurrentMaxDurability > 0
                     && item.CurrentDurability < item.CurrentMaxDurability;
      track.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      if (!visible)
        return;

      float ratio = Mathf.Clamp01((float)item.CurrentDurability / item.CurrentMaxDurability);
      var fill = track.Q<VisualElement>(FillName);
      if (fill == null)
        return;

      fill.style.width = Length.Percent(ratio * 100f);
      fill.style.backgroundColor = GetColor(ratio);
    }

    public static Color GetColor(float ratio)
    {
      ratio = Mathf.Clamp01(ratio);
      if (ratio >= 0.75f) return HighColor;
      if (ratio >= 0.50f) return MediumColor;
      if (ratio >= 0.25f) return LowColor;
      return CriticalColor;
    }
  }
}
