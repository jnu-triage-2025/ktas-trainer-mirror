# MultiplayerInfrastructure.Quest.QuestManager

## 0. 문서 목적

`QuestManager`에 최근 추가된 웨이포인트 관련 동작을 정리합니다.
- Quest 등록/갱신 시 `WaypointIdentifier`를 활용한 하이라이트 흐름
- `FeatureFlags.HighlightAssignedWaypoint` 비트마스크 설정으로 동작을 켜고 끄는 방법
- HUD와 전체 퀘스트 패널에서 `WaypointIdentifier`를 노출하기 위한 데이터 흐름

이 문서는 기획/디자인/QA 담당자가 변경된 행동을 빠르게 파악하고 테스트 조건을 마련할 수 있도록 간단한 흐름과 예시를 제공합니다.

---

## 1. 핵심 변경 요약

1. `QuestManager.FeatureFlags.HighlightAssignedWaypoint` (기본 `On`)을 통해 플레이어에게 배정된 퀘스트에 대해 웨이포인트 하이라이트를 자동으로 트리거한다.
2. `AddOrUpdateQuest`에서 새로운 퀘스트를 등록할 때 `WaypointIdentifier`가 채워져 있고 플래그가 켜져 있으면 `WaypointAnchor.Highlight()`를 호출한다.
3. 퀘스트 관련 UI(미리보기 HUD와 퀘스트 패널)에서 `WaypointIdentifier`를 보여줘 플레이어가 목표 지점을 빠르게 확인할 수 있다.

---

## 2. 주요 필드/메서드

### `FeatureFlags`
- 비트마스크 enum이며 기본값은 `HighlightAssignedWaypoint`.
- 이 플래그가 꺼져 있으면 새 퀘스트 등록 시 웨이포인트 강조 또는 자동 하이라이트를 건너뛴다.
- 인스펙터에서 체크박스를 해제하면 `AddOrUpdateQuest`의 `TryHighlightWaypointForQuest`가 early return한다.

### `AddOrUpdateQuest(QuestData quest, bool notify = true)`
- 퀘스트 데이터를 깊은 복사하여 내부 딕셔너리를 갱신한다.
- 새로 등록된 퀘스트(`isNewQuest`) 여부를 판별하여 `TryHighlightWaypointForQuest`를 호출한다.
- `notify`가 true면 기존과 동일하게 트래킹/리스트 변경 이벤트를 발행한다.

### `TryHighlightWaypointForQuest(QuestData quest)`
- 현재 플래그가 `HighlightAssignedWaypoint`인지 확인한 뒤 `WaypointIdentifier`가 없으면 중단.
- `WaypointAnchor.TryGet`으로 앵커를 조회하여 발견되면 `Highlight()`를 실행.
- `WaypointAnchor`가 존재하지 않거나 플래그가 끊겨 있으면 아무런 로그나 처리 없이 종료.

---

## 3. UI 연동 흐름

- `QuestPreviewHudElement.CreateCard`는 `QuestData.WaypointIdentifier`를 `Waypoint: {identifier}` 형식의 라벨로 표시하여 즉시 위치 정보를 제공한다.
- `QuestPanelElement.QuestEntryElement.Bind`도 같은 텍스트를 갱신하므로 전체 퀘스트 목록에서 좌표/웨이포인트 식별자가 보인다.
- UI 측은 `QuestData`에 `WaypointIdentifier`를 노출만 하므로 추가 상호작용 없이 정보를 확인할 수 있는 것이 목표다.

---

## 4. 의도된 동작 예시

```csharp
var quest = new QuestData
{
    Id = "fetch-001",
    Title = "찾기",
    Description = "NPC에게 전달할 물건",
    QuestContent = "시장 북문 앞 수레",
    WaypointIdentifier = "market-cart",
    IsTracked = true
};
questManager.AddOrUpdateQuest(quest);
// FeatureFlags.HighlightAssignedWaypoint가 켜져 있으면 "market-cart" 앵커의 Highlight()가 한 번 실행됨.
```

`WaypointIdentifier`가 비어 있거나 플래그가 꺼진 상태에서는 `AddOrUpdateQuest`가 `WaypointAnchor.Highlight()`를 호출하지 않는다.

---

## 5. 테스트 스텝 제안

1. `QuestManager` 인스펙터에서 `FeatureFlags.HighlightAssignedWaypoint` 체크박스를 비활성화하고 새 퀘스트를 추가하여 하이라이트가 발생하지 않는지 확인.
2. 같은 퀘스트를 다시 등록했을 때 `WaypointAnchor`에 `Highlight()`가 의도대로 한 번만 호출되는지 로그/디버거로 검증.
3. `QuestPreviewHudElement`와 `QuestPanelElement`에서 각각 `WaypointIdentifier` 텍스트가 표시되는지 확인.

---

## 6. 관련 파일

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Quest/QuestManager.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/QuestPreviewHudElement.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/QuestPanelElement.cs`
