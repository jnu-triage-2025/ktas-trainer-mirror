using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
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

    // DisplayText 길이와 관계없이 모든 메뉴 항목이 같은 폭을 사용한다.
    private const float ContentWrapperWidth = 260f;
    private const float ContentFadeWidth = 44f;
    // 텍스트는 배경 우측 끝을 지나서도 일정 거리까지 읽을 수 있게 한다.
    private const float ContentTextMaxWidth = 340f;

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
      contentWrapper.style.paddingLeft = 4;
      contentWrapper.style.paddingRight = ContentFadeWidth;
      contentWrapper.style.paddingTop = 2;
      contentWrapper.style.paddingBottom = 2;
      contentWrapper.style.flexDirection = FlexDirection.Row;
      contentWrapper.style.alignItems = Align.Center;
      contentWrapper.style.overflow = Overflow.Visible;
      contentWrapper.style.width = ContentWrapperWidth;
      contentWrapper.style.minWidth = ContentWrapperWidth;
      contentWrapper.style.maxWidth = ContentWrapperWidth;
      contentWrapper.style.flexShrink = 0;

      // 본체와 우측 그라데이션을 같은 메시로 렌더링한다. 이전에는 본체는 일반
      // VisualElement 배경, 끝부분은 커스텀 메시였기 때문에 서로 다른 픽셀 정렬과
      // 레이아웃 경계를 사용해 접합선이 보일 수 있었다.
      var contentBackground = new HorizontalContentBackgroundElement(ContentBackground, ContentFadeWidth)
      {
        pickingMode = PickingMode.Ignore
      };
      contentBackground.AddToClassList("interactable-content-background");
      contentBackground.style.position = Position.Absolute;
      contentBackground.style.left = 0;
      contentBackground.style.top = 0;
      contentBackground.style.right = 0;
      contentBackground.style.bottom = 0;
      contentWrapper.Add(contentBackground);

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
      // 배경 폭과 별개로 최대 표시 폭을 확보하여 우측 그라데이션 너머까지
      // 텍스트가 이어지되, 지나치게 긴 문구는 그 이후에만 말줄임 처리한다.
      _contentText.style.width = ContentTextMaxWidth;
      _contentText.style.maxWidth = ContentTextMaxWidth;
      _contentText.style.flexShrink = 0;
      _contentText.style.flexGrow = 0;
      _contentText.style.whiteSpace = WhiteSpace.NoWrap;
      _contentText.style.overflow = Overflow.Hidden;
      _contentText.style.textOverflow = TextOverflow.Ellipsis;
      contentWrapper.Add(_contentText);

      Add(contentWrapper);
    }

    private sealed class HorizontalContentBackgroundElement : VisualElement
    {
      private readonly Color _leftColor;
      private readonly float _fadeWidth;
      private const float LeftCornerRadius = 12f;
      private const int CornerSegments = 6;

      public HorizontalContentBackgroundElement(Color leftColor, float fadeWidth)
      {
        _leftColor = leftColor;
        _fadeWidth = fadeWidth;
        generateVisualContent += DrawGradient;
      }

      private void DrawGradient(MeshGenerationContext context)
      {
        var rect = contentRect;
        if (rect.width <= 0f || rect.height <= 0f)
          return;

        float cornerRadius = Mathf.Min(LeftCornerRadius, rect.height * 0.5f);
        float fadeStart = Mathf.Max(rect.xMin + cornerRadius, rect.xMax - _fadeWidth);
        var transparentColor = new Color(_leftColor.r, _leftColor.g, _leftColor.b, 0f);

        // 사각 본체와 페이드를 하나의 generateVisualContent 호출에서 생성한다.
        DrawQuad(context, rect.xMin + cornerRadius, rect.yMin, fadeStart, rect.yMax, _leftColor, _leftColor);
        DrawQuad(context, fadeStart, rect.yMin, rect.xMax, rect.yMax, _leftColor, transparentColor);

        // 좌측의 기존 둥근 모서리도 같은 메시로 유지한다.
        DrawQuad(context, rect.xMin, rect.yMin + cornerRadius, rect.xMin + cornerRadius, rect.yMax - cornerRadius, _leftColor, _leftColor);
        DrawCorner(context, new Vector2(rect.xMin + cornerRadius, rect.yMin + cornerRadius), cornerRadius, Mathf.PI, Mathf.PI * 1.5f);
        DrawCorner(context, new Vector2(rect.xMin + cornerRadius, rect.yMax - cornerRadius), cornerRadius, Mathf.PI * 0.5f, Mathf.PI);
      }

      private static void DrawQuad(MeshGenerationContext context, float left, float top, float right, float bottom, Color leftColor, Color rightColor)
      {
        if (right <= left || bottom <= top)
          return;

        var mesh = context.Allocate(4, 6);
        mesh.SetNextVertex(CreateVertex(left, top, leftColor));
        mesh.SetNextVertex(CreateVertex(right, top, rightColor));
        mesh.SetNextVertex(CreateVertex(right, bottom, rightColor));
        mesh.SetNextVertex(CreateVertex(left, bottom, leftColor));
        mesh.SetNextIndex(0);
        mesh.SetNextIndex(1);
        mesh.SetNextIndex(2);
        mesh.SetNextIndex(2);
        mesh.SetNextIndex(3);
        mesh.SetNextIndex(0);
      }

      private void DrawCorner(MeshGenerationContext context, Vector2 center, float radius, float startAngle, float endAngle)
      {
        if (radius <= 0f)
          return;

        var mesh = context.Allocate(CornerSegments + 2, CornerSegments * 3);
        mesh.SetNextVertex(CreateVertex(center.x, center.y, _leftColor));
        for (int i = 0; i <= CornerSegments; i++)
        {
          float angle = Mathf.Lerp(startAngle, endAngle, i / (float)CornerSegments);
          mesh.SetNextVertex(CreateVertex(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, _leftColor));
        }

        for (ushort i = 0; i < CornerSegments; i++)
        {
          mesh.SetNextIndex(0);
          mesh.SetNextIndex((ushort)(i + 1));
          mesh.SetNextIndex((ushort)(i + 2));
        }
      }

      private static Vertex CreateVertex(float x, float y, Color color)
      {
        return new Vertex
        {
          position = new Vector3(x, y, Vertex.nearZ),
          tint = color,
          uv = Vector2.zero
        };
      }
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

      // 시나리오 데이터가 문구를 명시한 등록 항목은 그 문구를 우선한다.
      string displayText = interact?.DisplayText ?? string.Empty;
      if (InteractionRegistry.TryGetDataDisplay(interact, out var dataDisplay) && !string.IsNullOrWhiteSpace(dataDisplay.Text))
        displayText = dataDisplay.Text;
      _contentText.text = displayText;

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
      Sprite primaryOverride = null;
      VisualElement primaryOverrideHolder = null;
      bool hasOverride = QuestPresentationService.ActiveInstance != null
                         && QuestPresentationService.ActiveInstance.TryGetPrimaryIconOverride(interact, out primaryOverride);

      // 시나리오 데이터가 아이콘 식별자를 명시한 등록 항목은 데이터 아이콘을 앞에 둔다.
      if (InteractionRegistry.TryGetDataDisplay(interact, out var dataDisplay) && dataDisplay.IconIdentifiers != null)
      {
        for (int i = 0; i < dataDisplay.IconIdentifiers.Count; i++)
        {
          var dataSprite = Registry.Registry.Get<Sprite>(RegistryType.IconSprite, dataDisplay.IconIdentifiers[i])
                           ?? Registry.Registry.GetOrLoadIconSprite(dataDisplay.IconIdentifiers[i]);
          if (dataSprite != null && displayedSprites.Add(dataSprite))
          {
            var holder = ConfigureIconHolder(iconCount++, dataSprite, Color.clear);
            if (dataSprite == primaryOverride)
              primaryOverrideHolder = holder;
          }
        }
      }

      int lastListIconIndex = -1;
      if (hasOverride && interact?.DisplayIcon == null && icons?.DisplayIcons != null)
      {
        for (int i = 0; i < icons.DisplayIcons.Count; i++)
        {
          if (icons.DisplayIcons[i] != null)
            lastListIconIndex = i;
        }
      }

      if (icons?.DisplayIcons != null)
      {
        for (int i = 0; i < icons.DisplayIcons.Count; i++)
        {
          var sprite = hasOverride && i == lastListIconIndex ? primaryOverride : icons.DisplayIcons[i];
          if (sprite == null || !displayedSprites.Add(sprite))
            continue;
          var holder = ConfigureIconHolder(iconCount++, sprite, Color.clear);
          if (sprite == primaryOverride)
            primaryOverrideHolder = holder;
        }
      }

      // 목록 아이콘에 더해, 기존 구현체의 동적/override 단일 아이콘도 표시한다.
      // 같은 Sprite가 목록에 이미 있으면 중복 표시하지 않는다.
      var displayIcon = hasOverride && (interact?.DisplayIcon != null || lastListIconIndex < 0)
        ? primaryOverride
        : interact?.DisplayIcon;
      if (displayIcon != null && displayedSprites.Add(displayIcon))
      {
        var holder = ConfigureIconHolder(iconCount++, displayIcon, Color.clear);
        if (displayIcon == primaryOverride)
          primaryOverrideHolder = holder;
      }

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

      // 목록 요소는 재사용되므로 이전 Bind에서 변경한 형제 순서를 먼저 기본 상태로 되돌린다.
      // 기본 상태에서는 DisplayIcons의 첫 항목(Clear/Fail)이 최상단이다.
      for (int i = _iconHolders.Count - 1; i >= 0; i--)
        _iconHolders[i].BringToFront();

      // UI Toolkit은 나중 형제일수록 위에 그린다. QuestMark가 기존 Clear/Fail 등의
      // 오버레이와 합성되더라도 항상 사용자가 가장 먼저 볼 수 있도록 최상단으로 올린다.
      primaryOverrideHolder?.BringToFront();
    }

    private VisualElement ConfigureIconHolder(int index, Sprite sprite, Color fallbackColor)
    {
      var holder = GetIconHolder(index);
      holder.style.display = DisplayStyle.Flex;
      if (sprite != null)
        holder.style.backgroundImage = new StyleBackground(sprite);
      else
        holder.style.backgroundImage = StyleKeyword.None;
      holder.style.backgroundColor = sprite != null ? Color.clear : fallbackColor;
      return holder;
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
        // Cover는 스프라이트의 원본 비율을 보존하면서 정사각형 슬롯을 완전히 채웁니다.
        // 슬롯보다 작은 아이콘은 넓은 변에 맞춰 확대되고, 남는 부분은 중앙 기준으로 잘립니다.
        holder.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
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
