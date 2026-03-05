using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 화면 중앙에 표시되는 크로스헤어 VisualElement입니다.
  /// 단순 점(dot) 형태로 렌더링됩니다.
  /// </summary>
  [UxmlElement]
  public partial class CrosshairElement : VisualElement
  {
    private readonly VisualElement _dot;

    public CrosshairElement()
    {
      pickingMode = PickingMode.Ignore;
      AddToClassList("crosshair");

      _dot = new VisualElement
      {
        name = "crosshair-dot",
        pickingMode = PickingMode.Ignore,
      };
      _dot.AddToClassList("crosshair__dot");

      Add(_dot);
    }
  }
}
