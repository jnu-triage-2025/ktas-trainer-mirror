# <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView"></a> Class ItemSubmissionUIView

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

아이템 제출 패널의 순수 뷰.

레이아웃:
 - 배경: 화면 전체를 덮는 반투명 딤(dim)으로 뒤 배경을 어둡게 처리(모달 느낌 강조)
 - 헤더: 제목(좌) + × 닫기 버튼(우) + 하단 구분선
 - 설명 문구: 무엇을 해야 하는지 짧게 안내
 - "필요 아이템" 섹션 라벨
 - 요구 아이템 그리드: 슬롯마다 아이콘(placeholder) + 아이템 이름 + 보유/요구 수량 세로 배치
   · 미충족 → 아이콘 흐림/회색, 파란 수량 텍스트, navy 테두리
   · 충족   → 아이콘 선명, 녹색 수량 텍스트, 녹색 테두리·배경, 살짝 확대(scale) 강조
 - 제출(녹색) / 취소(빨간) 버튼 행: hover/active 시 색상·scale 트랜지션

```csharp
[UxmlElement]
public class ItemSubmissionUIView : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[ItemSubmissionUIView](MultiplayerInfrastructure.UI.ItemSubmissionUIView.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView__ctor"></a> ItemSubmissionUIView\(\)

```csharp
public ItemSubmissionUIView()
```

## Properties

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView_IsVisible"></a> IsVisible

```csharp
public bool IsVisible { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView_Configure_System_String_System_String_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_InteractableEntity_ItemRequirement__"></a> Configure\(string, string, IReadOnlyList<ItemRequirement\>\)

패널을 특정 요구 사항 세트로 구성한다.

```csharp
public void Configure(string title, string submitButtonText, IReadOnlyList<ItemRequirement> requirements)
```

#### Parameters

`title` string

`submitButtonText` string

`requirements` IReadOnlyList<[ItemRequirement](MultiplayerInfrastructure.InteractableEntity.ItemRequirement.md)\>

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView_SetVisible_System_Boolean_"></a> SetVisible\(bool\)

```csharp
public void SetVisible(bool visible)
```

#### Parameters

`visible` bool

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView_UpdateHeldCounts_System_Func_System_String_System_Int32__"></a> UpdateHeldCounts\(Func<string, int\>\)

플레이어 보유량을 반영하여 각 요구 칸의 표시를 갱신하고,
모든 요구가 충족되면 제출 버튼을 활성화한다.

```csharp
public void UpdateHeldCounts(Func<string, int> heldCountResolver)
```

#### Parameters

`heldCountResolver` Func<string, int\>

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView_CloseClicked"></a> CloseClicked

```csharp
public event Action CloseClicked
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_ItemSubmissionUIView_SubmitClicked"></a> SubmitClicked

```csharp
public event Action SubmitClicked
```

#### Event Type

 Action

