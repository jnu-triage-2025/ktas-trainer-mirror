# <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController"></a> Class EntityOverheadLabelUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

엔티티 위에 라벨(뱃지)을 띄우는 일반화된 오버헤드 라벨 컨트롤러.

<p>
플레이어 이름을 머리 위에 표기하듯, 임의의 월드 엔티티(Transform) 위에 "색상 사각형 + 텍스트" 라벨을
스크린 스페이스(UI Toolkit)로 표시한다. 도메인 코드는 <xref href="MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.SetLabel(UnityEngine.Transform%2cMultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent)" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.RemoveLabel(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 로
대상별 라벨을 등록/해제하기만 하면 되고, 월드→스크린 투영과 위치 갱신은 이 컨트롤러가 매 프레임 수행한다.
</p>

<p>
배치: 씬에 <xref href="UnityEngine.UIElements.UIDocument" data-throw-if-not-resolved="false"></xref> 와 함께 배치한다. 카메라는 지정되지 않으면 Camera.main 을 사용한다.
</p>

```csharp
[RequireComponent(typeof(UIDocument))]
public sealed class EntityOverheadLabelUIController : UIControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md)

## Properties

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_ActiveInstance"></a> ActiveInstance

활성 인스턴스(단일 로컬 클라이언트에 하나). 도메인 코드가 손쉽게 접근하기 위한 헬퍼.

```csharp
public static EntityOverheadLabelUIController ActiveInstance { get; }
```

#### Property Value

 [EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_RemoveLabel_UnityEngine_Transform_"></a> RemoveLabel\(Transform\)

대상 엔티티의 라벨을 제거한다.

```csharp
public void RemoveLabel(Transform target)
```

#### Parameters

`target` Transform

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_RemoveLabel_UnityEngine_Transform_System_String_"></a> RemoveLabel\(Transform, string\)

```csharp
public void RemoveLabel(Transform target, string channelId)
```

#### Parameters

`target` Transform

`channelId` string

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_RemoveLabels_UnityEngine_Transform_"></a> RemoveLabels\(Transform\)

```csharp
public void RemoveLabels(Transform target)
```

#### Parameters

`target` Transform

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_SetLabel_UnityEngine_Transform_MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_"></a> SetLabel\(Transform, LabelContent\)

대상 엔티티 위에 표시할 라벨을 설정한다(없으면 생성, 있으면 내용 갱신).

```csharp
public void SetLabel(Transform target, EntityOverheadLabelUIController.LabelContent content)
```

#### Parameters

`target` Transform

라벨을 띄울 월드 앵커(예: 엔티티 Transform).

`content` [EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md).[LabelContent](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent.md)

표시 내용(색상 사각형 + 텍스트).

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_SetLabel_UnityEngine_Transform_System_String_System_Int32_MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_"></a> SetLabel\(Transform, string, int, LabelContent\)

```csharp
public void SetLabel(Transform target, string channelId, int channelOrder, EntityOverheadLabelUIController.LabelContent content)
```

#### Parameters

`target` Transform

`channelId` string

`channelOrder` int

`content` [EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md).[LabelContent](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent.md)

### <a id="MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_SetLabel_UnityEngine_Transform_System_String_System_Int32_MultiplayerInfrastructure_UI_EntityOverheadLabelUIController_LabelContent_System_Single_"></a> SetLabel\(Transform, string, int, LabelContent, float\)

```csharp
public void SetLabel(Transform target, string channelId, int channelOrder, EntityOverheadLabelUIController.LabelContent content, float maxVisibleDistance)
```

#### Parameters

`target` Transform

`channelId` string

`channelOrder` int

`content` [EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md).[LabelContent](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent.md)

`maxVisibleDistance` float

카메라와 이 거리보다 멀어지면 라벨을 숨긴다. 0 이하이면 거리와 무관하게 항상 표시한다.

