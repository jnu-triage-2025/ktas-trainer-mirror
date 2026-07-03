using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 엔티티 위에 띄우는 일반화된 오버헤드 라벨(뱃지) 비주얼 엘리먼트.
  ///
  /// <para>
  /// 플레이어 이름표를 머리 위에 띄우듯, 임의의 월드 엔티티 위에 "색상 사각형 + 텍스트" 형태의 라벨을 표시한다.
  /// 색상 사각형과 텍스트 색상은 독립적으로 지정할 수 있어, 트리아지 등급(색상 + 명칭) 같은 도메인 표기에
  /// 재사용된다. 위치(화면 좌표) 갱신은 <see cref="EntityOverheadLabelUIController"/> 가 담당한다.
  /// </para>
  /// </summary>
  public sealed class EntityOverheadLabelElement : VisualElement
  {
    private readonly VisualElement _swatch;
    private readonly Label _text;

    public EntityOverheadLabelElement()
    {
      // 화면 좌표에 절대 배치되므로 position:absolute. 자신의 크기 중심으로 정렬하기 위해 translate 를 사용한다.
      style.position = Position.Absolute;
      style.flexDirection = FlexDirection.Row;
      style.alignItems = Align.Center;
      style.paddingLeft = 3;
      style.paddingRight = 5;
      style.paddingTop = 1;
      style.paddingBottom = 1;
      style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
      style.borderTopLeftRadius = 4;
      style.borderTopRightRadius = 4;
      style.borderBottomLeftRadius = 4;
      style.borderBottomRightRadius = 4;
      pickingMode = PickingMode.Ignore;

      _swatch = new VisualElement();
      _swatch.style.width = 11;
      _swatch.style.height = 11;
      _swatch.style.minWidth = 11;
      _swatch.style.minHeight = 11;
      _swatch.style.marginRight = 4;
      _swatch.style.borderTopLeftRadius = 2;
      _swatch.style.borderTopRightRadius = 2;
      _swatch.style.borderBottomLeftRadius = 2;
      _swatch.style.borderBottomRightRadius = 2;
      _swatch.style.borderLeftWidth = 1;
      _swatch.style.borderRightWidth = 1;
      _swatch.style.borderTopWidth = 1;
      _swatch.style.borderBottomWidth = 1;
      var border = new Color(1f, 1f, 1f, 0.6f);
      _swatch.style.borderLeftColor = border;
      _swatch.style.borderRightColor = border;
      _swatch.style.borderTopColor = border;
      _swatch.style.borderBottomColor = border;
      _swatch.pickingMode = PickingMode.Ignore;
      Add(_swatch);

      _text = new Label();
      _text.style.fontSize = 11;
      _text.style.unityFontStyleAndWeight = FontStyle.Bold;
      // 텍스트 색은 SetContent 에서 트리아지 등급 색으로 지정된다.
      _text.style.color = Color.white;
      _text.style.unityTextOutlineWidth = 0.5f;
      _text.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.7f);
      _text.pickingMode = PickingMode.Ignore;
      Add(_text);
    }

    /// <summary>
    /// 라벨 내용을 갱신한다.
    /// </summary>
    /// <param name="swatchColor">색상 사각형의 색.</param>
    /// <param name="text">표기할 텍스트(명칭).</param>
    /// <param name="textColor">텍스트 색.</param>
    public void SetContent(Color swatchColor, string text, Color textColor)
    {
      _swatch.style.backgroundColor = swatchColor;
      _text.text = text ?? string.Empty;
      _text.style.color = textColor;
    }

    /// <summary>
    /// 화면 좌표에 라벨 중심을 배치한다(자기 크기 중심 정렬).
    /// </summary>
    public void SetScreenPosition(Vector2 panelPosition)
    {
      style.left = panelPosition.x;
      style.top = panelPosition.y;
      // 라벨의 중심이 지정 좌표에 오도록 자기 크기의 절반만큼 이동.
      style.translate = new Translate(Length.Percent(-50f), Length.Percent(-100f));
    }
  }
}
