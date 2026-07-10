using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class InteractableObjectHintListElement : VisualElement
  {
    private static readonly Color KeyHintBackground = new Color(30f / 255f, 36f / 255f, 44f / 255f, 0.85f);
    private static readonly Color ContentBackground = new Color(18f / 255f, 22f / 255f, 28f / 255f, 0.78f);

    // 이 값은 F키/마우스 스크롤 힌트 영역(_keyHint)의 레이아웃 높이를 고정한다.
    // _keyHint는 overflow: Visible로 설정되어 있어, 내부 아이콘(특히 마우스 스크롤 힌트)의
    // 크기가 나중에 커지더라도 이 고정 높이 덕분에 행(row) 전체의 높이/세로 간격에는 영향을 주지 않는다.
    private const float KeyHintRowHeight = 20f;

    private const string MouseScrollHintIconPath = "Textures/Icons/mouse-scroll";
    private static Texture2D _mouseScrollHintIcon;
    private static Texture2D MouseScrollHintIcon
    {
      get
      {
        if (_mouseScrollHintIcon == null)
          _mouseScrollHintIcon = Resources.Load<Texture2D>(MouseScrollHintIconPath);
        return _mouseScrollHintIcon;
      }
    }

    private readonly VisualElement _keyHint;
    private readonly VisualElement _scrollHint;
    private readonly Label _keyText;
    private readonly VisualElement _iconHolder;
    private readonly Label _contentText;

    public IInteract Interact { get; private set; }

    public InteractableObjectHintListElement()
    {
      AddToClassList("interactable-hint");
      AddToClassList("interactable-content");
      style.flexDirection = FlexDirection.Row;
      style.alignItems = Align.Center;
      style.marginBottom = 5;

      _keyHint = new VisualElement();
      _keyHint.AddToClassList("interact-key-hint");
      _keyHint.style.flexDirection = FlexDirection.Row;
      _keyHint.style.alignItems = Align.Center;
      _keyHint.style.justifyContent = Justify.Center;
      _keyHint.style.unityTextAlign = TextAnchor.MiddleCenter;
      _keyHint.style.color = KeyHintBackground;
      _keyHint.style.opacity = 0f;
      // 고정 높이 + overflow: Visible로 지정하여, 내부 힌트 아이콘(마우스 스크롤 아이콘 등)의
      // 크기가 이후 시인성 개선을 위해 커지더라도 이 컨테이너의 레이아웃 상 높이는 변하지 않는다.
      // 즉, 아래 contentWrapper(현재 선택된 Interactable 옵션)의 세로 간격/높이가
      // 힌트 아이콘의 크기 변경에 영향받지 않는다.
      _keyHint.style.height = KeyHintRowHeight;
      _keyHint.style.overflow = Overflow.Visible;

      _scrollHint = new VisualElement();
      _scrollHint.AddToClassList("interact-scroll-hint");
      _scrollHint.style.width = 40;
      _scrollHint.style.height = 40;
      _scrollHint.style.marginRight = -6;
      _scrollHint.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
      _scrollHint.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
      _scrollHint.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
      _scrollHint.style.backgroundRepeat = new StyleBackgroundRepeat(StyleKeyword.None);
      if (MouseScrollHintIcon != null)
        _scrollHint.style.backgroundImage = new StyleBackground(MouseScrollHintIcon);
      _keyHint.Add(_scrollHint);

      _keyText = new Label();
      _keyText.AddToClassList("interact-key-text");
      _keyText.style.unityTextAlign = TextAnchor.MiddleCenter;
      _keyText.style.whiteSpace = WhiteSpace.Normal;
      _keyText.style.fontSize = 12; // approx 80% of default size
      _keyText.style.unityFontStyleAndWeight = FontStyle.Bold;
      _keyText.style.backgroundColor = Color.white;
      _keyText.style.width = 20;
      _keyText.style.height = 20;
      _keyText.style.borderTopLeftRadius = 5;
      _keyText.style.borderTopRightRadius = 5;
      _keyText.style.borderBottomLeftRadius = 5;
      _keyText.style.borderBottomRightRadius = 5;
      _keyHint.Add(_keyText);
      Add(_keyHint);

      var contentWrapper = new VisualElement();
      contentWrapper.AddToClassList("interactable-content-wrapper");
      contentWrapper.AddToClassList("interactable-content-row");
      contentWrapper.style.backgroundColor = ContentBackground;
      contentWrapper.style.borderTopLeftRadius = 15;
      contentWrapper.style.borderBottomLeftRadius = 15;
      contentWrapper.style.borderLeftWidth = 1;
      contentWrapper.style.borderRightWidth = 1;
      contentWrapper.style.borderTopWidth = 1;
      contentWrapper.style.borderBottomWidth = 1;
      contentWrapper.style.borderLeftColor = new Color(1f, 1f, 1f, 0.12f);
      contentWrapper.style.borderTopColor = new Color(1f, 1f, 1f, 0.10f);
      contentWrapper.style.borderRightColor = new Color(1f, 1f, 1f, 0.06f);
      contentWrapper.style.borderBottomColor = new Color(0f, 0f, 0f, 0.38f);
      contentWrapper.style.paddingLeft = 5;
      contentWrapper.style.paddingRight = 10;
      contentWrapper.style.flexDirection = FlexDirection.Row;
      contentWrapper.style.alignItems = Align.Center;

      _iconHolder = new VisualElement();
      _iconHolder.AddToClassList("interactable-icon-holder");
      _iconHolder.style.width = 28;
      _iconHolder.style.height = 28;
      _iconHolder.style.minWidth = 28;
      _iconHolder.style.minHeight = 28;
      _iconHolder.style.borderTopLeftRadius = 6;
      _iconHolder.style.borderTopRightRadius = 6;
      _iconHolder.style.borderBottomLeftRadius = 6;
      _iconHolder.style.borderBottomRightRadius = 6;
      _iconHolder.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
      _iconHolder.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
      _iconHolder.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
      _iconHolder.style.backgroundRepeat = new StyleBackgroundRepeat(StyleKeyword.None);
      _iconHolder.style.opacity = 0.95f;
      // _iconHolder.style.boxShadow = new StyleBoxShadow(new BoxShadow(new Color(0f, 0f, 0f, 0.25f), 0, 0, 6, 0));
      _iconHolder.style.marginRight = 8;
      contentWrapper.Add(_iconHolder);

      _contentText = new Label();
      _contentText.AddToClassList("interactable-content-text");
      _contentText.style.color = Color.white;
      _contentText.style.fontSize = 18;
      _contentText.style.unityFontStyleAndWeight = FontStyle.Bold;
      _contentText.style.unityTextOutlineWidth = 0.75f;
      _contentText.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.70f);
      contentWrapper.Add(_contentText);

      Add(contentWrapper);
    }

    public void Bind(
        IInteract interact,
        string keyLabel,
        InteractableHintUIMode mode,
        Sprite dialogueIcon,
        bool isSelected)
    {
      Interact = interact;
      _keyText.text = keyLabel ?? string.Empty;

      var iconSprite = interact?.DisplayIcon;
      if (iconSprite == null && (interact?.AllowDisplayIconFallback ?? true))
        iconSprite = mode == InteractableHintUIMode.Dialogue ? dialogueIcon : DefaultsResource.FallbackSprite;

      if (iconSprite != null)
      {
        _iconHolder.style.backgroundImage = new StyleBackground(iconSprite);
        _iconHolder.style.backgroundColor = Color.clear;
      }
      else
      {
        _iconHolder.style.backgroundImage = StyleKeyword.None;
        _iconHolder.style.backgroundColor = interact?.DisplayColor ?? Color.clear;
      }

      _contentText.text = interact?.DisplayText ?? string.Empty;

      EnableInClassList("selected", isSelected);
      EnableInClassList("dialogue-selection", mode == InteractableHintUIMode.Dialogue);
      _contentText.EnableInClassList("dialogue-selection-text", mode == InteractableHintUIMode.Dialogue);
      _keyHint.style.opacity = isSelected ? 1f : 0f;
    }
  }
}
