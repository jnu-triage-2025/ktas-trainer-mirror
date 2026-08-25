using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// ScrollView와 함께 사용할 수 있는 경량 세로 스크롤바입니다.
  /// 호출자가 현재 오프셋/콘텐츠 크기를 공급하고, 사용자의 이동 요청을 받아 실제 ScrollView에 반영합니다.
  /// </summary>
  public sealed class ReusableVerticalScrollbar : VisualElement
  {
    private const float MinimumThumbHeight = 28f;
    private int _pointerId = -1;
    private float _dragStartY;
    private float _dragStartTop;
    private float _lastViewportHeight;
    private float _lastContentHeight;
    private float _lastOffset;
    private bool _hasMetrics;

    public event Action<float> ScrollNormalizedRequested;
    public VisualElement Thumb { get; }

    public ReusableVerticalScrollbar()
    {
      name = "reusable-vertical-scrollbar";
      pickingMode = PickingMode.Position;
      style.position = Position.Absolute;
      style.right = 7;
      style.top = 10;
      style.bottom = 10;
      style.width = 8;
      style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
      style.borderTopLeftRadius = 4;
      style.borderTopRightRadius = 4;
      style.borderBottomLeftRadius = 4;
      style.borderBottomRightRadius = 4;

      Thumb = new VisualElement { name = "reusable-vertical-scrollbar-thumb", pickingMode = PickingMode.Position };
      Thumb.style.position = Position.Absolute;
      Thumb.style.left = 1;
      Thumb.style.top = 0;
      Thumb.style.width = 6;
      Thumb.style.minHeight = MinimumThumbHeight;
      Thumb.style.backgroundColor = new Color(0.349f, 0.816f, 0.498f, 0.72f);
      Thumb.style.borderTopLeftRadius = 3;
      Thumb.style.borderTopRightRadius = 3;
      Thumb.style.borderBottomLeftRadius = 3;
      Thumb.style.borderBottomRightRadius = 3;

      Add(Thumb);
      RegisterCallback<GeometryChangedEvent>(_ => RefreshFromLastMetrics());
      RegisterCallback<PointerDownEvent>(HandleTrackPointerDown);
      Thumb.RegisterCallback<PointerDownEvent>(HandleThumbPointerDown);
      Thumb.RegisterCallback<PointerMoveEvent>(HandleThumbPointerMove);
      Thumb.RegisterCallback<PointerUpEvent>(HandleThumbPointerUp);
      Thumb.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
    }

    public void SetMetrics(float viewportHeight, float contentHeight, float offset)
    {
      _lastViewportHeight = viewportHeight;
      _lastContentHeight = contentHeight;
      _lastOffset = offset;
      _hasMetrics = true;
      RefreshFromLastMetrics();
    }

    private void RefreshFromLastMetrics()
    {
      if (!_hasMetrics)
        return;

      float trackHeight = contentRect.height;
      float maximumOffset = Mathf.Max(0f, _lastContentHeight - _lastViewportHeight);
      bool hasOverflow = trackHeight > 0f && maximumOffset > Mathf.Epsilon;
      // display:none 이면 다음 레이아웃에서 track 높이가 0이 되어 오버플로우 재감지를 못한다.
      // 레이아웃은 유지하고 시각/입력만 끈다.
      style.visibility = hasOverflow ? Visibility.Visible : Visibility.Hidden;
      pickingMode = hasOverflow ? PickingMode.Position : PickingMode.Ignore;
      if (!hasOverflow)
        return;

      float thumbHeight = Mathf.Clamp(trackHeight * _lastViewportHeight / _lastContentHeight, MinimumThumbHeight, trackHeight);
      float normalized = Mathf.Clamp01(_lastOffset / maximumOffset);
      Thumb.style.height = thumbHeight;
      Thumb.style.top = (trackHeight - thumbHeight) * normalized;
    }

    private void HandleTrackPointerDown(PointerDownEvent evt)
    {
      if (evt.button != 0 || evt.target == Thumb)
        return;

      RequestScrollAt(evt.localPosition.y - Thumb.resolvedStyle.height * 0.5f);
      evt.StopImmediatePropagation();
    }

    private void HandleThumbPointerDown(PointerDownEvent evt)
    {
      if (evt.button != 0)
        return;

      _pointerId = evt.pointerId;
      _dragStartY = evt.position.y;
      _dragStartTop = Thumb.resolvedStyle.top;
      Thumb.CapturePointer(evt.pointerId);
      evt.StopImmediatePropagation();
    }

    private void HandleThumbPointerMove(PointerMoveEvent evt)
    {
      if (evt.pointerId != _pointerId)
        return;

      RequestScrollAt(_dragStartTop + evt.position.y - _dragStartY);
      evt.StopImmediatePropagation();
    }

    private void HandleThumbPointerUp(PointerUpEvent evt)
    {
      if (evt.pointerId != _pointerId)
        return;

      if (Thumb.HasPointerCapture(evt.pointerId))
        Thumb.ReleasePointer(evt.pointerId);
      EndDrag();
      evt.StopImmediatePropagation();
    }

    private void EndDrag() => _pointerId = -1;

    private void RequestScrollAt(float requestedTop)
    {
      float travel = Mathf.Max(0f, contentRect.height - Thumb.resolvedStyle.height);
      ScrollNormalizedRequested?.Invoke(travel > Mathf.Epsilon ? Mathf.Clamp01(requestedTop / travel) : 0f);
    }
  }

  /// <summary>오버플로우가 생길 때만 <see cref="ReusableVerticalScrollbar"/>를 표시하는 ScrollView 래퍼.</summary>
  public sealed class OverflowScrollView : VisualElement
  {
    public ScrollView ScrollView { get; }
    public VisualElement Content => ScrollView.contentContainer;

    public OverflowScrollView()
    {
      style.position = Position.Relative;
      style.flexGrow = 1;
      style.minHeight = 0;
      style.overflow = Overflow.Hidden;

      ScrollView = new ScrollView(ScrollViewMode.Vertical)
      {
        verticalScrollerVisibility = ScrollerVisibility.Hidden,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden,
      };
      ScrollView.style.position = Position.Absolute;
      ScrollView.style.left = 0;
      ScrollView.style.right = 0;
      ScrollView.style.top = 0;
      ScrollView.style.bottom = 0;
      Add(ScrollView);

      var scrollbar = new ReusableVerticalScrollbar();
      scrollbar.ScrollNormalizedRequested += normalized =>
      {
        float maximum = Mathf.Max(0f, ScrollView.verticalScroller.highValue);
        ScrollView.verticalScroller.value = maximum * normalized;
      };
      Add(scrollbar);

      void RefreshScrollbar()
      {
        float viewport = ScrollView.contentViewport.layout.height;
        float content = Mathf.Max(ScrollView.contentContainer.layout.height, ScrollView.contentContainer.contentRect.height);
        scrollbar.SetMetrics(viewport, content, ScrollView.verticalScroller.value);
      }

      RegisterCallback<GeometryChangedEvent>(_ => RefreshScrollbar());
      ScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => RefreshScrollbar());
      ScrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => RefreshScrollbar());
      ScrollView.verticalScroller.valueChanged += _ => RefreshScrollbar();
    }
  }
}
