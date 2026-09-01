# <a id="MultiplayerInfrastructure_Quest_QuestPresentationService"></a> Class QuestPresentationService

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[DisallowMultipleComponent]
public sealed class QuestPresentationService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[QuestPresentationService](MultiplayerInfrastructure.Quest.QuestPresentationService.md)

## Properties

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_ActiveInstance"></a> ActiveInstance

```csharp
public static QuestPresentationService ActiveInstance { get; }
```

#### Property Value

 [QuestPresentationService](MultiplayerInfrastructure.Quest.QuestPresentationService.md)

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_ClearScenarioMark_MultiplayerInfrastructure_Quest_QuestPresentationTargetType_System_String_System_String_"></a> ClearScenarioMark\(QuestPresentationTargetType, string, string\)

시나리오 그래프의 QuestMark 노드가 끈 마크를 해제한다.

```csharp
public static void ClearScenarioMark(QuestPresentationTargetType targetType, string entityIdentifier, string interactionIdentifier)
```

#### Parameters

`targetType` [QuestPresentationTargetType](MultiplayerInfrastructure.Quest.QuestPresentationTargetType.md)

`entityIdentifier` string

`interactionIdentifier` string

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_ClearScenarioMarks"></a> ClearScenarioMarks\(\)

시나리오가 끝날 때 그래프가 켠 마크를 모두 해제한다.

```csharp
public static void ClearScenarioMarks()
```

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_ExpireQuestPresentation_System_String_"></a> ExpireQuestPresentation\(string\)

```csharp
public void ExpireQuestPresentation(string questIdentifier)
```

#### Parameters

`questIdentifier` string

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_HasActiveInteractionBinding_System_String_System_String_"></a> HasActiveInteractionBinding\(string, string\)

현재 활성 퀘스트 또는 시나리오 마크가 지정한 상호작용을 안내 대상으로 등록했는지 반환한다.
상호작용 자체의 노출을 현재 퀘스트 단계에 맞춰 제한할 때 사용한다.

```csharp
public bool HasActiveInteractionBinding(string entityIdentifier, string interactionIdentifier)
```

#### Parameters

`entityIdentifier` string

`interactionIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_RefreshPresentation"></a> RefreshPresentation\(\)

```csharp
public void RefreshPresentation()
```

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_SetScenarioMark_MultiplayerInfrastructure_Quest_QuestPresentationTargetType_System_String_System_String_System_String_System_Int32_"></a> SetScenarioMark\(QuestPresentationTargetType, string, string, string, int\)

시나리오 그래프의 QuestMark 노드가 켠 마크를 등록한다. 같은 대상에 이미 마크가 있으면 덮어쓴다.
아이콘 식별자를 비우면 대상 종류별 기본 퀘스트 마크 아이콘을 사용한다.

```csharp
public static void SetScenarioMark(QuestPresentationTargetType targetType, string entityIdentifier, string interactionIdentifier, string iconIdentifier, int priority)
```

#### Parameters

`targetType` [QuestPresentationTargetType](MultiplayerInfrastructure.Quest.QuestPresentationTargetType.md)

`entityIdentifier` string

`interactionIdentifier` string

`iconIdentifier` string

`priority` int

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_TryGetPrimaryIconOverride_MultiplayerInfrastructure_InteractableEntity_IInteract_UnityEngine_Sprite__"></a> TryGetPrimaryIconOverride\(IInteract, out Sprite\)

```csharp
public bool TryGetPrimaryIconOverride(IInteract interact, out Sprite icon)
```

#### Parameters

`interact` [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

`icon` Sprite

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_TryGetPrimaryIconOverride_System_String_System_String_UnityEngine_Sprite__"></a> TryGetPrimaryIconOverride\(string, string, out Sprite\)

```csharp
public bool TryGetPrimaryIconOverride(string entityIdentifier, string interactionIdentifier, out Sprite icon)
```

#### Parameters

`entityIdentifier` string

`interactionIdentifier` string

`icon` Sprite

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_OnPresentationChanged"></a> OnPresentationChanged

```csharp
public event Action OnPresentationChanged
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_Quest_QuestPresentationService_PresentationChanged"></a> PresentationChanged

```csharp
public static event Action PresentationChanged
```

#### Event Type

 Action

