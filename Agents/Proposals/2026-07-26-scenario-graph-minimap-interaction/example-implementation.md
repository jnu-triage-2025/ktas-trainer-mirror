# Scenario Graph Minimap Interaction — Example Implementation

```text
ScenarioGraphMiniMap:
    base = VisualElement
    overflow = hidden
    currentRect = Rect(5, 5, 175, 135)
    scale = min(body.width / graph.width, body.height / graph.height)
    offset = center(graph * scale, body)
    Painter2D.draw(nodes, edges)
    Painter2D.fill(viewport, translucentWhite)

mouse down:
    border/corner → resize
    header → move
    body → pan graph viewport

mouse drag:
    delta = panelMouse - startPanelMouse
    rect = resize(startRect, delta)
    apply(left, top, width, height)

View → Reset Minimap:
    miniMap.currentRect = defaultRect
    miniMap.MarkDirtyRepaint()
    miniMap.BringToFront()
```

본문 좌표를 전체 노드 bounds의 그래프 좌표로 역변환하고 `GraphView.UpdateViewTransform`으로 viewport를 이동한다.
