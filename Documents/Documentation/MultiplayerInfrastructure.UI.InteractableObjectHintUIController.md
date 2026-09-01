# <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController"></a> Class InteractableObjectHintUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

InteractableObjectHintUIController는 InteractableObjectHintUI를 사용하는 데 필요한
컨트롤을 제공합니다. PlayerController등에서 이 컨트롤을 제어하는 것이 의도됩니다.

다이얼로그 모드를 지원하여, 대화 중에는 선택지만 표시하고
대화 종료 후 원래 상호작용 객체 목록을 복원합니다.

```csharp
[RequireComponent(typeof(UIDocument))]
public class InteractableObjectHintUIController : UIControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[InteractableObjectHintUIController](MultiplayerInfrastructure.UI.InteractableObjectHintUIController.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Fields

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_OnDialogueSelectionsChanged"></a> OnDialogueSelectionsChanged

다이얼로그 선택지가 변경되었을 때 발생

```csharp
public UnityEvent OnDialogueSelectionsChanged
```

#### Field Value

 UnityEvent

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_OnModeChanged"></a> OnModeChanged

```csharp
public UnityEvent<InteractableHintUIMode> OnModeChanged
```

#### Field Value

 UnityEvent<[InteractableHintUIMode](MultiplayerInfrastructure.UI.InteractableHintUIMode.md)\>

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_OnNewInteractableAdded"></a> OnNewInteractableAdded

```csharp
public UnityEvent OnNewInteractableAdded
```

#### Field Value

 UnityEvent

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_OnNewInteractableRemoved"></a> OnNewInteractableRemoved

```csharp
public UnityEvent OnNewInteractableRemoved
```

#### Field Value

 UnityEvent

## Properties

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_CurrentMode"></a> CurrentMode

현재 UI 모드

```csharp
public InteractableHintUIMode CurrentMode { get; }
```

#### Property Value

 [InteractableHintUIMode](MultiplayerInfrastructure.UI.InteractableHintUIMode.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_HasAnySelections"></a> HasAnySelections

현재 선택 가능한 항목이 있는지 여부

```csharp
public bool HasAnySelections { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_IsDialogueMode"></a> IsDialogueMode

다이얼로그 모드인지 여부

```csharp
public bool IsDialogueMode { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Add_MultiplayerInfrastructure_InteractableEntity_IInteract_"></a> Add\(IInteract\)

상호작용 객체 추가
다이얼로그 모드에서는 캐시에 추가됩니다.

```csharp
public void Add(IInteract interact)
```

#### Parameters

`interact` [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Clear"></a> Clear\(\)

Clear InteractableObjects List
다이얼로그 모드에서는 동작하지 않습니다.

```csharp
public void Clear()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_ClearDialogueSelections"></a> ClearDialogueSelections\(\)

다이얼로그 선택지 클리어 (다이얼로그 모드에서만 동작)
선택지 없이 UI를 비웁니다.

```csharp
public void ClearDialogueSelections()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_EnterDialogueMode"></a> EnterDialogueMode\(\)

다이얼로그 모드 진입
현재 상호작용 객체 목록을 백업하고 UI를 비웁니다.

```csharp
public void EnterDialogueMode()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_ExecuteSelected_UnityEngine_Transform_"></a> ExecuteSelected\(Transform\)

현재 선택된 상호작용 객체를 실행합니다.

```csharp
public bool ExecuteSelected(Transform interactor)
```

#### Parameters

`interactor` Transform

상호작용을 수행하는 Transform

#### Returns

 bool

상호작용이 실행되었는지 여부

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_ExecuteSelectedDialogueSelection_UnityEngine_Transform_"></a> ExecuteSelectedDialogueSelection\(Transform\)

현재 선택된 다이얼로그 선택지를 실행합니다.

```csharp
public bool ExecuteSelectedDialogueSelection(Transform interactor)
```

#### Parameters

`interactor` Transform

상호작용을 수행하는 Transform (플레이어)

#### Returns

 bool

선택지가 실행되었는지 여부

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_ExitDialogueMode"></a> ExitDialogueMode\(\)

다이얼로그 모드 종료
백업된 상호작용 객체 목록을 복원합니다.

```csharp
public void ExitDialogueMode()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Get_System_Int32_"></a> Get\(int\)

인덱스로 상호작용 객체 가져오기

```csharp
public IInteract Get(int idx)
```

#### Parameters

`idx` int

#### Returns

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_GetCount"></a> GetCount\(\)

현재 목록의 항목 수

```csharp
public int GetCount()
```

#### Returns

 int

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_GetSelected"></a> GetSelected\(\)

현재 선택된 상호작용 객체 가져오기

```csharp
public IInteract GetSelected()
```

#### Returns

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_GetSelectedIndex"></a> GetSelectedIndex\(\)

현재 선택된 인덱스

```csharp
public int GetSelectedIndex()
```

#### Returns

 int

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_HasDialogueSelection"></a> HasDialogueSelection\(\)

현재 선택된 다이얼로그 선택지가 있는지 확인

```csharp
public bool HasDialogueSelection()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_HasSelections"></a> HasSelections\(\)

선택 가능한 항목이 있는지 여부

```csharp
public bool HasSelections()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_MoveSelected_System_Int32_"></a> MoveSelected\(int\)

선택 인덱스 이동 (delta만큼)

```csharp
public void MoveSelected(int delta)
```

#### Parameters

`delta` int

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Pop"></a> Pop\(\)

현재 선택된 요소를 interactables 목록에서 제거하고 반환합니다.
다이얼로그 모드에서는 null을 반환합니다.

```csharp
public IInteract Pop()
```

#### Returns

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Pop_System_Int32_"></a> Pop\(int\)

idx 위치의 요소를 interactables 목록에서 제거하고 반환합니다.
다이얼로그 모드에서는 null을 반환합니다.

```csharp
public IInteract Pop(int idx)
```

#### Parameters

`idx` int

#### Returns

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Remove_MultiplayerInfrastructure_InteractableEntity_IInteract_"></a> Remove\(IInteract\)

상호작용 객체 제거
다이얼로그 모드에서는 캐시에서 제거됩니다.

```csharp
public void Remove(IInteract interact)
```

#### Parameters

`interact` [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Remove"></a> Remove\(\)

현재 선택된 요소를 interactables 목록에서 제거합니다.
다이얼로그 모드에서는 동작하지 않습니다.

```csharp
public void Remove()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_Remove_System_Int32_"></a> Remove\(int\)

idx 위치의 요소 제거를 시도합니다.
다이얼로그 모드에서는 동작하지 않습니다.

```csharp
public void Remove(int idx)
```

#### Parameters

`idx` int

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_SelectNext"></a> SelectNext\(\)

다음 항목 선택

```csharp
public void SelectNext()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_SelectPrevious"></a> SelectPrevious\(\)

이전 항목 선택

```csharp
public void SelectPrevious()
```

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_SetDialogueSelections_System_Collections_Generic_List_MultiplayerInfrastructure_InteractableEntity_IInteract__"></a> SetDialogueSelections\(List<IInteract\>\)

다이얼로그 선택지 설정 (다이얼로그 모드에서만 동작)

```csharp
public void SetDialogueSelections(List<IInteract> selections)
```

#### Parameters

`selections` List<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

표시할 선택지 목록

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_SetDialogueSelections_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_InteractableEntity_IInteract__"></a> SetDialogueSelections\(IReadOnlyList<IInteract\>\)

다이얼로그 선택지 설정 (IReadOnlyList 버전)

```csharp
public void SetDialogueSelections(IReadOnlyList<IInteract> selections)
```

#### Parameters

`selections` IReadOnlyList<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_SetSelected_System_Int32_"></a> SetSelected\(int\)

선택 인덱스 직접 설정

```csharp
public void SetSelected(int idx)
```

#### Parameters

`idx` int

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_UpdateInteractables_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_InteractableEntity_IInteract__"></a> UpdateInteractables\(IReadOnlyList<IInteract\>\)

상호작용 객체 목록 일괄 업데이트
다이얼로그 모드에서는 캐시가 업데이트됩니다.

```csharp
public void UpdateInteractables(IReadOnlyList<IInteract> newInteracts)
```

#### Parameters

`newInteracts` IReadOnlyList<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

### <a id="MultiplayerInfrastructure_UI_InteractableObjectHintUIController_InteractionClicked"></a> InteractionClicked

화면의 상호작용 메뉴 행을 마우스로 클릭했을 때 발생합니다.
인덱스는 클릭 시점의 현재 목록을 기준으로 합니다.

```csharp
public event Action<int> InteractionClicked
```

#### Event Type

 Action<int\>

