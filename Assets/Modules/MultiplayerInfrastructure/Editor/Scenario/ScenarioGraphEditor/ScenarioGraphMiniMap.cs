using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>
  /// GraphView의 실험적 MiniMap 구현에 의존하지 않는 시나리오 그래프 전용 미니맵.
  /// 렌더링, 이동, 크기 조절, viewport 탐색을 하나의 고정 좌표계에서 처리한다.
  /// </summary>
  internal sealed class ScenarioGraphMiniMap : VisualElement
  {
    [Flags]
    private enum Interaction
    {
      None = 0,
      Move = 1,
      Pan = 2,
      Left = 4,
      Right = 8,
      Top = 16,
      Bottom = 32
    }

    private const float HeaderHeight = 20f;
    private const float ResizeHitSize = 7f;
    private const float MinimumWidth = 120f;
    private const float MinimumHeight = 90f;
    private const float MaximumWidth = 800f;
    private const float MaximumHeight = 600f;
    private const float GraphPadding = 80f;
    private const float BorderRadius = 5f;

    private static readonly Color BackgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.96f);
    private static readonly Color HeaderColor = new Color(0.20f, 0.20f, 0.20f, 1f);
    private static readonly Color NodeColor = new Color(0.48f, 0.48f, 0.48f, 0.95f);
    private static readonly Color EdgeColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);
    private static readonly Color TitleColor = new Color(1f, 1f, 1f, 0.92f);
    private static readonly Color HeaderDividerColor = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ViewportFillColor = new Color(1f, 1f, 1f, 0.18f);
    private static readonly Color ViewportBorderColor = new Color(1f, 1f, 1f, 0.62f);
    private static readonly Color BorderColor = new Color(0.62f, 0.62f, 0.62f, 1f);
    private static readonly Color ActiveBorderColor = new Color(0.27f, 0.65f, 1f, 1f);

    private readonly ScenarioGraphView graphView;
    private readonly Label title;
    private Interaction interaction;
    private Rect interactionStartRect;
    private Vector2 interactionStartMouse;
    private Rect graphBounds;
    private Rect currentRect;
    public Action<Rect> RectChanged;

    public ScenarioGraphMiniMap(ScenarioGraphView graphView, Rect initialRect)
    {
      this.graphView = graphView;
      name = "scenario-graph-mini-map";
      style.position = Position.Absolute;
      style.overflow = Overflow.Hidden;
      style.backgroundColor = BackgroundColor;
      style.borderTopLeftRadius = BorderRadius;
      style.borderTopRightRadius = BorderRadius;
      style.borderBottomLeftRadius = BorderRadius;
      style.borderBottomRightRadius = BorderRadius;
      SetBorder(Interaction.None);

      title = new Label("Preview")
      {
        pickingMode = PickingMode.Ignore
      };
      title.style.position = Position.Absolute;
      title.style.left = 5f;
      title.style.top = 0f;
      title.style.height = HeaderHeight - .5f;
      title.style.color = TitleColor;
      title.style.fontSize = 10f;
      title.style.unityTextAlign = TextAnchor.MiddleLeft;
      hierarchy.Add(title);

      generateVisualContent += Draw;
      RegisterCallback<MouseDownEvent>(OnMouseDown);
      RegisterCallback<MouseMoveEvent>(OnMouseMove);
      RegisterCallback<MouseUpEvent>(OnMouseUp);
      RegisterCallback<MouseLeaveEvent>(_ =>
      {
        if (!this.HasMouseCapture())
          SetBorder(Interaction.None);
      });
      RegisterCallback<MouseCaptureOutEvent>(_ =>
      {
        interaction = Interaction.None;
        SetBorder(Interaction.None);
      });

      Reset(initialRect);
      schedule.Execute(MarkDirtyRepaint).Every(50);
    }

    public void Reset(Rect rect)
    {
      interaction = Interaction.None;
      ApplyRect(rect);
      SetBorder(Interaction.None);
      BringToFront();
      MarkDirtyRepaint();
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
      if (evt.button != 0)
        return;

      interaction = HitTestInteraction(evt.localMousePosition);
      interactionStartRect = currentRect;
      interactionStartMouse = evt.mousePosition;
      this.CaptureMouse();

      if (interaction == Interaction.Pan)
        PanGraphTo(evt.localMousePosition);

      evt.StopImmediatePropagation();
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
      if (!this.HasMouseCapture())
      {
        SetBorder(HitTestInteraction(evt.localMousePosition));
        return;
      }

      if (interaction == Interaction.Pan)
      {
        PanGraphTo(evt.localMousePosition);
      }
      else if (interaction == Interaction.Move)
      {
        var rect = interactionStartRect;
        rect.position += evt.mousePosition - interactionStartMouse;
        rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, graphView.layout.width - rect.width));
        rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, graphView.layout.height - rect.height));
        ApplyRect(rect);
      }
      else
      {
        Resize(evt.mousePosition - interactionStartMouse);
      }

      evt.StopImmediatePropagation();
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
      if (evt.button != 0 || !this.HasMouseCapture())
        return;

      this.ReleaseMouse();
      interaction = Interaction.None;
      SetBorder(HitTestInteraction(evt.localMousePosition));
      evt.StopImmediatePropagation();
    }

    private Interaction HitTestInteraction(Vector2 localPosition)
    {
      var left = localPosition.x <= ResizeHitSize;
      var right = localPosition.x >= currentRect.width - ResizeHitSize;
      var top = localPosition.y <= ResizeHitSize;
      var bottom = localPosition.y >= currentRect.height - ResizeHitSize;

      var result = Interaction.None;
      if (left) result |= Interaction.Left;
      if (right) result |= Interaction.Right;
      if (top) result |= Interaction.Top;
      if (bottom) result |= Interaction.Bottom;
      if (result != Interaction.None)
        return result;

      return localPosition.y <= HeaderHeight ? Interaction.Move : Interaction.Pan;
    }

    private void Resize(Vector2 delta)
    {
      var rect = interactionStartRect;
      if ((interaction & Interaction.Left) != 0)
      {
        var fixedRight = rect.xMax;
        rect.width = Mathf.Clamp(rect.width - delta.x, MinimumWidth, Mathf.Min(MaximumWidth, fixedRight));
        rect.x = fixedRight - rect.width;
      }
      if ((interaction & Interaction.Right) != 0)
      {
        var available = Mathf.Max(MinimumWidth, graphView.layout.width - rect.x);
        rect.width = Mathf.Clamp(rect.width + delta.x, MinimumWidth, Mathf.Min(MaximumWidth, available));
      }
      if ((interaction & Interaction.Top) != 0)
      {
        var fixedBottom = rect.yMax;
        rect.height = Mathf.Clamp(rect.height - delta.y, MinimumHeight, Mathf.Min(MaximumHeight, fixedBottom));
        rect.y = fixedBottom - rect.height;
      }
      if ((interaction & Interaction.Bottom) != 0)
      {
        var available = Mathf.Max(MinimumHeight, graphView.layout.height - rect.y);
        rect.height = Mathf.Clamp(rect.height + delta.y, MinimumHeight, Mathf.Min(MaximumHeight, available));
      }
      ApplyRect(rect);
    }

    private void ApplyRect(Rect rect)
    {
      currentRect = rect;
      style.left = rect.x;
      style.top = rect.y;
      style.width = rect.width;
      style.height = rect.height;
      RectChanged?.Invoke(rect);
      MarkDirtyRepaint();
    }

    private void SetBorder(Interaction highlighted)
    {
      style.borderLeftWidth = 1f;
      style.borderRightWidth = 1f;
      style.borderTopWidth = 1f;
      style.borderBottomWidth = 1f;
      style.borderLeftColor = (highlighted & Interaction.Left) != 0 ? ActiveBorderColor : BorderColor;
      style.borderRightColor = (highlighted & Interaction.Right) != 0 ? ActiveBorderColor : BorderColor;
      style.borderTopColor = (highlighted & Interaction.Top) != 0 ? ActiveBorderColor : BorderColor;
      style.borderBottomColor = (highlighted & Interaction.Bottom) != 0 ? ActiveBorderColor : BorderColor;
    }

    private void Draw(MeshGenerationContext context)
    {
      var painter = context.painter2D;
      var body = new Rect(0f, HeaderHeight, resolvedStyle.width, Mathf.Max(0f, resolvedStyle.height - HeaderHeight));

      painter.fillColor = HeaderColor;
      DrawFilledRect(painter, new Rect(0f, 0f, resolvedStyle.width, HeaderHeight));
      painter.strokeColor = HeaderDividerColor;
      painter.lineWidth = 1f;
      painter.BeginPath();
      painter.MoveTo(new Vector2(0f, HeaderHeight - 0.5f));
      painter.LineTo(new Vector2(resolvedStyle.width, HeaderHeight - 0.5f));
      painter.Stroke();

      var nodes = graphView.graphElements.OfType<Node>().ToList();
      if (nodes.Count == 0 || body.width <= 0f || body.height <= 0f)
        return;

      graphBounds = CalculateGraphBounds(nodes);
      foreach (var edge in graphView.graphElements.OfType<Edge>())
      {
        if (edge.output?.node == null || edge.input?.node == null)
          continue;
        painter.strokeColor = EdgeColor;
        painter.lineWidth = 1f;
        painter.BeginPath();
        painter.MoveTo(MapGraphPoint(edge.output.node.GetPosition().center, body));
        painter.LineTo(MapGraphPoint(edge.input.node.GetPosition().center, body));
        painter.Stroke();
      }

      painter.fillColor = NodeColor;
      foreach (var node in nodes)
        DrawFilledRect(painter, MapGraphRect(node.GetPosition(), body));

      var viewportRect = CalculateViewportRect();
      painter.fillColor = ViewportFillColor;
      var mappedViewportRect = MapGraphRect(viewportRect, body);
      DrawFilledRect(painter, mappedViewportRect);
      painter.strokeColor = ViewportBorderColor;
      painter.lineWidth = 1f;
      DrawStrokedRect(painter, mappedViewportRect);
    }

    private static Rect CalculateGraphBounds(System.Collections.Generic.IReadOnlyList<Node> nodes)
    {
      var bounds = nodes[0].GetPosition();
      for (var i = 1; i < nodes.Count; i++)
        bounds = Union(bounds, nodes[i].GetPosition());
      return new Rect(
        bounds.xMin - GraphPadding,
        bounds.yMin - GraphPadding,
        Mathf.Max(1f, bounds.width + GraphPadding * 2f),
        Mathf.Max(1f, bounds.height + GraphPadding * 2f));
    }

    private Rect CalculateViewportRect()
    {
      var topLeft = graphView.contentViewContainer.WorldToLocal(graphView.LocalToWorld(Vector2.zero));
      var bottomRight = graphView.contentViewContainer.WorldToLocal(
        graphView.LocalToWorld(new Vector2(graphView.layout.width, graphView.layout.height)));
      return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    private void PanGraphTo(Vector2 localPoint)
    {
      var body = new Rect(0f, HeaderHeight, resolvedStyle.width, Mathf.Max(0f, resolvedStyle.height - HeaderHeight));
      var nodes = graphView.graphElements.OfType<Node>().ToList();
      if (nodes.Count == 0 || !body.Contains(localPoint))
        return;

      graphBounds = CalculateGraphBounds(nodes);
      var contentPoint = UnmapGraphPoint(localPoint, body);
      var scale = graphView.viewTransform.scale;
      var viewportCenter = new Vector2(graphView.layout.width * 0.5f, graphView.layout.height * 0.5f);
      var position = new Vector3(
        viewportCenter.x - contentPoint.x * scale.x,
        viewportCenter.y - contentPoint.y * scale.y,
        0f);
      graphView.UpdateViewTransform(position, scale);
      MarkDirtyRepaint();
    }

    private Vector2 MapGraphPoint(Vector2 point, Rect body)
    {
      var projection = CalculateProjection(body);
      return projection.offset + (point - graphBounds.min) * projection.scale;
    }

    private Rect MapGraphRect(Rect rect, Rect body)
    {
      var min = MapGraphPoint(rect.min, body);
      var max = MapGraphPoint(rect.max, body);
      return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private Vector2 UnmapGraphPoint(Vector2 point, Rect body)
    {
      var projection = CalculateProjection(body);
      return graphBounds.min + (point - projection.offset) / projection.scale;
    }

    private (float scale, Vector2 offset) CalculateProjection(Rect body)
    {
      var scale = Mathf.Min(body.width / graphBounds.width, body.height / graphBounds.height);
      scale = Mathf.Max(scale, Mathf.Epsilon);
      var renderedSize = graphBounds.size * scale;
      var offset = body.min + (body.size - renderedSize) * 0.5f;
      return (scale, offset);
    }

    private static Rect Union(Rect left, Rect right)
    {
      return Rect.MinMaxRect(
        Mathf.Min(left.xMin, right.xMin),
        Mathf.Min(left.yMin, right.yMin),
        Mathf.Max(left.xMax, right.xMax),
        Mathf.Max(left.yMax, right.yMax));
    }

    private static void DrawFilledRect(Painter2D painter, Rect rect)
    {
      painter.BeginPath();
      painter.MoveTo(rect.min);
      painter.LineTo(new Vector2(rect.xMax, rect.yMin));
      painter.LineTo(rect.max);
      painter.LineTo(new Vector2(rect.xMin, rect.yMax));
      painter.ClosePath();
      painter.Fill();
    }

    private static void DrawStrokedRect(Painter2D painter, Rect rect)
    {
      painter.BeginPath();
      painter.MoveTo(rect.min);
      painter.LineTo(new Vector2(rect.xMax, rect.yMin));
      painter.LineTo(rect.max);
      painter.LineTo(new Vector2(rect.xMin, rect.yMax));
      painter.ClosePath();
      painter.Stroke();
    }

  }
}
