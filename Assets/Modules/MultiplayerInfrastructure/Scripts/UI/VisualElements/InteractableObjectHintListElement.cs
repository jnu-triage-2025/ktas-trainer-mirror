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

    private readonly VisualElement _keyHint;
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
      if (iconSprite == null)
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
