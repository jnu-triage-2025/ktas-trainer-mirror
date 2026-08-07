using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using System.Collections.Generic;
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
    private const float KeyHintRowHeight = 16f;

    // "현재 선택된 Interactable 옵션" 배경(contentWrapper)의 폭 규칙.
    // 짧은 DisplayText 에서는 최소 폭으로 모든 항목의 배경 길이를 통일하고,
    // 긴 DisplayText 는 말줄임 없이 읽을 수 있도록 콘텐츠 길이에 맞춰 최대 폭까지 늘어난다.
    // 최대 폭을 넘는 텍스트만 말줄임(ellipsis) 처리된다.
    private const float ContentWrapperMinWidth = 220f;
    private const float ContentWrapperMaxWidth = 560f;

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
    private const float IconSlotSize = 22f;

    private readonly VisualElement _iconContainer;
    private readonly List<VisualElement> _iconHolders = new();
    private readonly Label _contentText;

    public IInteract Interact { get; private set; }

    public InteractableObjectHintListElement()
    {
      AddToClassList("interactable-hint");
      AddToClassList("interactable-content");
      style.flexDirection = FlexDirection.Row;
      style.alignItems = Align.Center;
      style.marginBottom = 3;

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
      _scrollHint.style.width = 32;
      _scrollHint.style.height = 32;
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
      _keyText.style.fontSize = 10;
      _keyText.style.unityFontStyleAndWeight = FontStyle.Bold;
      _keyText.style.backgroundColor = Color.white;
      _keyText.style.width = 16;
      _keyText.style.height = 16;
      _keyText.style.borderTopLeftRadius = 4;
      _keyText.style.borderTopRightRadius = 4;
      _keyText.style.borderBottomLeftRadius = 4;
      _keyText.style.borderBottomRightRadius = 4;
      _keyHint.Add(_keyText);
      Add(_keyHint);

      var contentWrapper = new VisualElement();
      contentWrapper.AddToClassList("interactable-content-wrapper");
      contentWrapper.AddToClassList("interactable-content-row");
      contentWrapper.style.backgroundColor = ContentBackground;
      contentWrapper.style.borderTopLeftRadius = 12;
      contentWrapper.style.borderBottomLeftRadius = 12;
      contentWrapper.style.borderLeftWidth = 1;
      contentWrapper.style.borderRightWidth = 1;
      contentWrapper.style.borderTopWidth = 1;
      contentWrapper.style.borderBottomWidth = 1;
      contentWrapper.style.borderLeftColor = new Color(1f, 1f, 1f, 0.12f);
      contentWrapper.style.borderTopColor = new Color(1f, 1f, 1f, 0.10f);
      contentWrapper.style.borderRightColor = new Color(1f, 1f, 1f, 0.06f);
      contentWrapper.style.borderBottomColor = new Color(0f, 0f, 0f, 0.38f);
      contentWrapper.style.paddingLeft = 4;
      contentWrapper.style.paddingRight = 8;
      contentWrapper.style.paddingTop = 2;
      contentWrapper.style.paddingBottom = 2;
      contentWrapper.style.flexDirection = FlexDirection.Row;
      contentWrapper.style.alignItems = Align.Center;
      // 짧은 항목은 최소 폭으로 배경 길이를 통일하고, 긴 항목은 콘텐츠가 충분히
      // 보이도록 최대 폭까지 배경이 늘어난다(폭은 auto 로 두어 내용에 맞긴다).
      contentWrapper.style.width = StyleKeyword.Auto;
      contentWrapper.style.minWidth = ContentWrapperMinWidth;
      contentWrapper.style.maxWidth = ContentWrapperMaxWidth;
      contentWrapper.style.flexShrink = 0;

      _iconContainer = new VisualElement();
      _iconContainer.AddToClassList("interactable-icon-container");
      // 여러 아이콘은 하나의 정사각형 슬롯 안에서 레이어로 합성한다.
      // 목록의 첫 아이콘(Clear/Fail 오버레이)이 항상 가장 위에 표시된다.
      _iconContainer.style.width = IconSlotSize;
      _iconContainer.style.height = IconSlotSize;
      _iconContainer.style.minWidth = IconSlotSize;
      _iconContainer.style.minHeight = IconSlotSize;
      _iconContainer.style.alignItems = Align.Center;
      _iconContainer.style.marginRight = 6;
      contentWrapper.Add(_iconContainer);

      _contentText = new Label();
      _contentText.AddToClassList("interactable-content-text");
      _contentText.style.color = Color.white;
      _contentText.style.fontSize = 14;
      _contentText.style.unityFontStyleAndWeight = FontStyle.Bold;
      _contentText.style.unityTextOutlineWidth = 0.6f;
      _contentText.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.70f);
      // 배경이 최대 폭에 도달해 텍스트가 넘치는 경우에만 줄바꿈 없이 말줄임(...) 처리한다.
      _contentText.style.flexShrink = 1;
      _contentText.style.flexGrow = 0;
      _contentText.style.whiteSpace = WhiteSpace.NoWrap;
      _contentText.style.overflow = Overflow.Hidden;
      _contentText.style.textOverflow = TextOverflow.Ellipsis;
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

      BindIcons(interact, mode, dialogueIcon);

      _contentText.text = interact?.DisplayText ?? string.Empty;

      EnableInClassList("selected", isSelected);
      EnableInClassList("dialogue-selection", mode == InteractableHintUIMode.Dialogue);
      _contentText.EnableInClassList("dialogue-selection-text", mode == InteractableHintUIMode.Dialogue);
      _keyHint.style.opacity = isSelected ? 1f : 0f;
    }

    private void BindIcons(IInteract interact, InteractableHintUIMode mode, Sprite dialogueIcon)
    {
      var icons = interact as IInteractDisplayIcons;
      var displayedSprites = new HashSet<Sprite>();
      int iconCount = 0;

      if (icons?.DisplayIcons != null)
      {
        foreach (var sprite in icons.DisplayIcons)
        {
          if (sprite == null || !displayedSprites.Add(sprite)) continue;
          ConfigureIconHolder(iconCount++, sprite, Color.clear);
        }
      }

      // 목록 아이콘에 더해, 기존 구현체의 동적/override 단일 아이콘도 표시한다.
      // 같은 Sprite가 목록에 이미 있으면 중복 표시하지 않는다.
      var displayIcon = interact?.DisplayIcon;
      if (displayIcon != null && displayedSprites.Add(displayIcon))
        ConfigureIconHolder(iconCount++, displayIcon, Color.clear);

      if (iconCount == 0)
      {
        var fallbackIcon = (interact?.AllowDisplayIconFallback ?? true)
          ? mode == InteractableHintUIMode.Dialogue ? dialogueIcon : DefaultsResource.FallbackSprite
          : null;

        // fallback을 명시적으로 끈 경우에도 기존처럼 DisplayColor를 가진 빈 슬롯을 유지한다.
        ConfigureIconHolder(iconCount++, fallbackIcon, interact?.DisplayColor ?? Color.clear);
      }

      for (int i = iconCount; i < _iconHolders.Count; i++)
        _iconHolders[i].style.display = DisplayStyle.None;
    }

    private void ConfigureIconHolder(int index, Sprite sprite, Color fallbackColor)
    {
      var holder = GetIconHolder(index);
      holder.style.display = DisplayStyle.Flex;
      if (sprite != null)
        holder.style.backgroundImage = new StyleBackground(sprite);
      else
        holder.style.backgroundImage = StyleKeyword.None;
      holder.style.backgroundColor = sprite != null ? Color.clear : fallbackColor;
    }

    private VisualElement GetIconHolder(int index)
    {
      while (_iconHolders.Count <= index)
      {
        var holder = new VisualElement();
        holder.AddToClassList("interactable-icon-holder");
        holder.style.width = IconSlotSize;
        holder.style.height = IconSlotSize;
        holder.style.minWidth = IconSlotSize;
        holder.style.minHeight = IconSlotSize;
        holder.style.position = Position.Absolute;
        holder.style.left = 0;
        holder.style.top = 0;
        holder.style.borderTopLeftRadius = 5;
        holder.style.borderTopRightRadius = 5;
        holder.style.borderBottomLeftRadius = 5;
        holder.style.borderBottomRightRadius = 5;
        // Contain은 정사각형 슬롯을 넘지 않으면서 스프라이트의 원본 비율을 보존합니다.
        holder.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        holder.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        holder.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        holder.style.backgroundRepeat = new StyleBackgroundRepeat(StyleKeyword.None);
        holder.style.opacity = 0.95f;
        _iconHolders.Add(holder);
        // UI Toolkit은 나중 형제일수록 위에 그린다. 새 레이어는 앞에 삽입하여
        // DisplayIcons의 첫 항목(Clear/Fail)을 마지막 자식, 즉 최상단으로 유지한다.
        _iconContainer.Insert(0, holder);
      }

      return _iconHolders[index];
    }
  }
}
