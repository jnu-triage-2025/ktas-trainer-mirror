# <a id="MultiplayerInfrastructure_UI_QuestPanelElement"></a> Class QuestPanelElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

임무(퀘스트) 저널 UI.

레이아웃:
 - 배경: 화면 전체를 덮는 반투명 딤(dim)
 - 헤더: 제목 + 추적 수(좌) / 필터 탭(중앙) / × 닫기(우)
 - 좌측 목록: 분류(추적 중 · 진행 중 · 완료)별로 묶인 임무 항목.
   항목마다 마름모 마커 + 제목 + 현재 목표 요약 + 상태 배지를 표시하고,
   선택한 항목은 강조 테두리로 구분한다.
 - 우측 상세: 선택한 임무의 제목 → 현재 목표 → 설명 → 세부 목표 →
   정보 타일(진행도·세부 목표·상태·범위) → 고지 배너 → 추적 토글 버튼.

스타일은 QuestPanelUI.uss 에 정의한다(팔레트는 ItemSubmissionUI / InventoryUI 와 동일).

```csharp
[UxmlElement]
public class QuestPanelElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[QuestPanelElement](MultiplayerInfrastructure.UI.QuestPanelElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement__ctor"></a> QuestPanelElement\(\)

```csharp
public QuestPanelElement()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement_IsOpen"></a> IsOpen

```csharp
public bool IsOpen { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement_SetOpen_System_Boolean_"></a> SetOpen\(bool\)

```csharp
public void SetOpen(bool open)
```

#### Parameters

`open` bool

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement_SetQuests_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Quest_QuestData__"></a> SetQuests\(IReadOnlyList<QuestData\>\)

```csharp
public void SetQuests(IReadOnlyList<QuestData> quests)
```

#### Parameters

`quests` IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement_UpdateTracked_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Quest_QuestData__"></a> UpdateTracked\(IReadOnlyList<QuestData\>\)

```csharp
public void UpdateTracked(IReadOnlyList<QuestData> trackedQuests)
```

#### Parameters

`trackedQuests` IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement_OnCloseRequested"></a> OnCloseRequested

```csharp
public event Action OnCloseRequested
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_QuestPanelElement_OnTrackToggled"></a> OnTrackToggled

```csharp
public event Action<string, bool> OnTrackToggled
```

#### Event Type

 Action<string, bool\>

