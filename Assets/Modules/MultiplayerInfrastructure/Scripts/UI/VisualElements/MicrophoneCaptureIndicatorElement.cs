using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 마이크 입력을 실제로 받아오는 동안 화면 우측 하단에 띄우는 시각 요소.
  ///
  /// 아이콘 뒤의 원형 배경은 입력 음량을 나타내는 게이지로도 쓴다. 아래에서 위로 차오르는
  /// 채움 막대를 원 안에 두고, 원이 overflow 를 잘라 내어 테두리 모양대로 보이게 한다.
  ///
  /// 표시 여부와 게이지 값은 <see cref="MicrophoneCaptureIndicatorUIController"/> 가 주입한다.
  /// 비차단 HUD 이므로 pickingMode 는 Ignore 로 두어 하위 UI 클릭을 가로채지 않는다.
  /// (<see cref="UIOverlayStack"/>/<see cref="IUIOverlay"/> 는 모달 전용이므로 사용하지 않는다.)
  ///
  /// 시각 스타일은 <see cref="TimeDisplayElement"/> 와 같이 인라인 스타일에서 관리한다
  /// (uxml/uss/StyleSheet 불필요).
  /// </summary>
  [UxmlElement]
  public partial class MicrophoneCaptureIndicatorElement : VisualElement
  {
    public const string RootName = "microphone-capture-indicator-root";

    private const string IconResourcePath = "Textures/Icons/mic";
    private const float EdgeMargin = 16f;
    private const float CardSize = 44f;
    private const float IconSize = 26f;

    private static readonly Color CardColor = new Color(0f, 0f, 0f, 0.62f);
    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color IconColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color GaugeColor = new Color(0.45f, 0.70f, 0.92f, 0.55f);

    private static Sprite _icon;
    private static Sprite Icon
    {
      get
      {
        if (_icon == null)
          _icon = Resources.Load<Sprite>(IconResourcePath);
        return _icon;
      }
    }

    private VisualElement _card;
    private VisualElement _gauge;

    public MicrophoneCaptureIndicatorElement()
    {
      name = RootName;
      AddToClassList("microphone-capture-indicator-root");
      pickingMode = PickingMode.Ignore;
      ApplyInlineStyles();
      Build();
      SetVisibleState(false);
    }

    private void ApplyInlineStyles()
    {
      // 화면 우측 하단 정렬.
      style.position = Position.Absolute;
      style.right = EdgeMargin;
      style.bottom = EdgeMargin;
      style.flexDirection = FlexDirection.Row;
      style.alignItems = Align.Center;
      style.justifyContent = Justify.FlexEnd;
    }

    private void Build()
    {
      _card = new VisualElement { pickingMode = PickingMode.Ignore };
      _card.style.width = CardSize;
      _card.style.height = CardSize;
      _card.style.backgroundColor = CardColor;
      _card.style.borderTopWidth = 1;
      _card.style.borderBottomWidth = 1;
      _card.style.borderLeftWidth = 1;
      _card.style.borderRightWidth = 1;
      _card.style.borderTopColor = BorderColor;
      _card.style.borderBottomColor = BorderColor;
      _card.style.borderLeftColor = BorderColor;
      _card.style.borderRightColor = BorderColor;
      _card.style.borderTopLeftRadius = CardSize * 0.5f;
      _card.style.borderTopRightRadius = CardSize * 0.5f;
      _card.style.borderBottomLeftRadius = CardSize * 0.5f;
      _card.style.borderBottomRightRadius = CardSize * 0.5f;
      _card.style.flexDirection = FlexDirection.Row;
      _card.style.alignItems = Align.Center;
      _card.style.justifyContent = Justify.Center;
      // 채움 막대가 원형 테두리 밖으로 삐져나가지 않도록 잘라 낸다.
      _card.style.overflow = Overflow.Hidden;

      // 흐름에서 빠진 절대 위치 요소이므로 아이콘의 가운데 정렬에 영향을 주지 않는다.
      // 아이콘보다 먼저 붙여서 아이콘이 항상 게이지 위에 그려지게 한다.
      _gauge = new VisualElement { pickingMode = PickingMode.Ignore };
      _gauge.style.position = Position.Absolute;
      _gauge.style.left = 0;
      _gauge.style.right = 0;
      _gauge.style.bottom = 0;
      _gauge.style.height = Length.Percent(0f);
      _gauge.style.backgroundColor = GaugeColor;
      _card.Add(_gauge);

      var icon = new VisualElement { pickingMode = PickingMode.Ignore };
      icon.style.width = IconSize;
      icon.style.height = IconSize;
      icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
      icon.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
      icon.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
      icon.style.backgroundRepeat = new StyleBackgroundRepeat(StyleKeyword.None);
      icon.style.unityBackgroundImageTintColor = IconColor;

      // 아이콘 리소스를 못 찾아도 표시 자체는 남겨 두어, 마이크 수집 중이라는 사실은 계속 알린다.
      var sprite = Icon;
      if (sprite != null)
        icon.style.backgroundImage = new StyleBackground(sprite);
      else
        Debug.LogWarning($"[MicrophoneCaptureIndicatorElement] Failed to load icon sprite from '{IconResourcePath}'.");

      _card.Add(icon);
      Add(_card);
    }

    /// <summary>표시 여부를 토글한다.</summary>
    public void SetVisibleState(bool visible)
    {
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// 음량 게이지의 높이를 0에서 1 사이의 값으로 지정한다.
    /// 프레임마다 불리므로, 값을 다듬는 일은 컨트롤러가 맡고 여기서는 그대로 반영한다.
    /// </summary>
    public void SetGaugeLevel(float normalizedLevel)
    {
      if (_gauge == null)
        return;

      _gauge.style.height = Length.Percent(Mathf.Clamp01(normalizedLevel) * 100f);
    }
  }
}
