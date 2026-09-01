# <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition"></a> Class NPCSubmissionInteractDefinition

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

NPC 에디터(인스펙터/NPCBaseModelSO)에서 "아이템 제출" 상호작용을 사전 설정하기 위한 직렬화 데이터.

이 정의 자체는 순수 데이터이며, <xref href="MultiplayerInfrastructure.Entity.Npc" data-throw-if-not-resolved="false"></xref> 가 런타임에 이 정의로부터
<xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable" data-throw-if-not-resolved="false"></xref> 컴포넌트를 자동 생성/구성하여 상호작용 소스로 추가한다.
(submission 상호작용은 제출 UI 를 여는 컴포넌트가 필요하므로 순수 데이터만으로는 동작하지 않는다.)

시나리오 그래프 노드(ItemSubmissionConfig)는 이렇게 생성된 Interactable 을
<xref href="MultiplayerInfrastructure.Entity.NPCSubmissionInteractDefinition.InteractableIdentifier" data-throw-if-not-resolved="false"></xref> 로 참조하여 런타임에 요구 아이템/완료 신호를 덮어쓸 수 있다.

```csharp
[Serializable]
public class NPCSubmissionInteractDefinition
```

#### Inheritance

object ← 
[NPCSubmissionInteractDefinition](MultiplayerInfrastructure.Entity.NPCSubmissionInteractDefinition.md)

## Properties

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_DisplayColor"></a> DisplayColor

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_DisplayIcon"></a> DisplayIcon

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_DisplayText"></a> DisplayText

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_Enabled"></a> Enabled

```csharp
public bool Enabled { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_InteractableIdentifier"></a> InteractableIdentifier

```csharp
public string InteractableIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_IsValid"></a> IsValid

요구 아이템이 하나 이상 정의되어 있으면 유효하다.

```csharp
public bool IsValid { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_Clone"></a> Clone\(\)

```csharp
public NPCSubmissionInteractDefinition Clone()
```

#### Returns

 [NPCSubmissionInteractDefinition](MultiplayerInfrastructure.Entity.NPCSubmissionInteractDefinition.md)

### <a id="MultiplayerInfrastructure_Entity_NPCSubmissionInteractDefinition_ToSubmissionDefinition"></a> ToSubmissionDefinition\(\)

이 정의를 런타임 <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition" data-throw-if-not-resolved="false"></xref> 로 변환한다.

```csharp
public ItemSubmissionDefinition ToSubmissionDefinition()
```

#### Returns

 [ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

