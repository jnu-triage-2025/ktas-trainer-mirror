# Sugiyama Scenario Graph Layout — Example Implementation

대상 구현은 `Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/ScenarioGraphEditor/ScenarioGraphEditor.cs`의 `AutoLayoutNodes`다.

```text
outgoing = CollectScenarioEdges(nodes)
forward = RemoveDfsFeedbackEdges(outgoing)
layers = LongestPathLayering(forward)
normalized = InsertDummyVerticesForLongEdges(layers, forward)
RepeatPortAwareMedianSweeps(normalized)
KeepLowestCrossingOrder(normalized)
TransposeAdjacentNodesWhileCrossingsDecrease(normalized)
y = MedianNeighborCoordinateAssignment(normalized)
SetNodePosition(x = layer * columnSpacing, y)
```

핵심 제약은 모든 순방향 연결의 대상 레이어가 출발 레이어보다 오른쪽이라는 것이다. 동일 부모의 여러 후속 노드는 같은 다음 레이어에서 세로로 나열된다. median 동점은 실제 출력 포트 순서로 해소한다. 각 후보는 `(교차 수, 총 세로 간선 변위)`의 사전식 순서로 비교하므로 교차 수가 같다면 수평 연결이 더 많은 결과를 보존한다. 인접 노드 교환에도 같은 기준을 적용한다. 좌표 할당 단계는 부모를 자식 묶음의 세로 중심으로 당기면서 최소 행 간격을 보장한다.
