# <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable"></a> Class ItemSubmissionInteractable

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

"요구 아이템을 들고 있으면 상호작용하여 제출할 수 있는" 독립 Interactable 컴포넌트.

사용 시나리오(예): 의사 NPC 또는 접수대에 부착 → 플레이어가 상호작용 → 제출 UI 가 열리고
요구 아이템을 넣고 제출 → 인벤토리에서 소모 → 서버 세션 전역 신호(sig.*)를 올린다.

설정 우선순위: 인스펙터의 프리셋 기본값(<xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable._definition" data-throw-if-not-resolved="false"></xref>)을 기본으로 하되,
시나리오 그래프 노드가 런타임에 요구 아이템/완료 신호/활성 상태를 덮어쓸 수 있다
(<xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.ApplyDefinitionOverride(MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition)" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.SetEnabled(System.Boolean)" data-throw-if-not-resolved="false"></xref>).

이 컴포넌트는 <xref href="MultiplayerInfrastructure.Registry.RegistryType.InteractableEntity" data-throw-if-not-resolved="false"></xref> 와 <xref href="MultiplayerInfrastructure.Registry.RegistryType.Entity" data-throw-if-not-resolved="false"></xref> 에
식별자로 등록되어, 그래프 노드가 식별자로 이 인스턴스를 찾아 사전 설정할 수 있게 한다.
프리팹으로 만들어 <xref href="MultiplayerInfrastructure.Registry.EntityPresetDefinition" data-throw-if-not-resolved="false"></xref> 으로 등록하면, EntityPresetSpawn 노드나
전용 ItemSubmissionConfig 노드로 스폰/사전설정할 수 있다.

```csharp
[DisallowMultipleComponent]
public class ItemSubmissionInteractable : Interactable, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget, IInteractorConditional, IInteractToggleable
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md) ← 
[ItemSubmissionInteractable](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md), 
[IInteractToggleable](MultiplayerInfrastructure.InteractableEntity.IInteractToggleable.md)

#### Inherited Members

[Interactable.DisplayText](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayText), 
[Interactable.DisplayIcon](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcon), 
[Interactable.DisplayIcons](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcons), 
[Interactable.AllowDisplayIconFallback](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_AllowDisplayIconFallback), 
[Interactable.DisplayColor](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayColor), 
[Interactable.PresentationEntityIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_PresentationEntityIdentifier), 
[Interactable.InteractionIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_InteractionIdentifier), 
[Interactable.Interacts](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interacts), 
[Interactable.Interact\(Transform\)](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interact\_UnityEngine\_Transform\_)

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_Definition"></a> Definition

```csharp
public ItemSubmissionDefinition Definition { get; }
```

#### Property Value

 [ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public override Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public override Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public override string DisplayText { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_InteractionIdentifier"></a> InteractionIdentifier

```csharp
public override string InteractionIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_IsCompleted"></a> IsCompleted

```csharp
public bool IsCompleted { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public override string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_ApplyDefinitionOverride_MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_"></a> ApplyDefinitionOverride\(ItemSubmissionDefinition\)

요구 아이템/완료 신호/표시를 런타임에 덮어쓴다(그래프 노드 오버라이드).

```csharp
public void ApplyDefinitionOverride(ItemSubmissionDefinition definition)
```

#### Parameters

`definition` [ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_Configure_System_String_MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_UnityEngine_Sprite_System_Nullable_UnityEngine_Color__System_Boolean_"></a> Configure\(string, ItemSubmissionDefinition, Sprite, Color?, bool\)

코드(예: <xref href="MultiplayerInfrastructure.Entity.Npc" data-throw-if-not-resolved="false"></xref>)에서 이 컴포넌트를 프로그래밍 방식으로 구성한다.
인스펙터 없이 AddComponent 로 생성한 뒤 이 메서드로 식별자/정의/표시/활성 상태를 설정한다.
식별자 변경 시 레지스트리에 재등록한다.

```csharp
public void Configure(string identifier, ItemSubmissionDefinition definition, Sprite displayIcon = null, Color? displayColor = null, bool enabled = true)
```

#### Parameters

`identifier` string

`definition` [ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

`displayIcon` Sprite

`displayColor` Color?

`enabled` bool

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public override void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_ResetCompletion"></a> ResetCompletion\(\)

완료 상태를 재설정한다(consumeOnce 이더라도 다시 제출 가능하게 함).

```csharp
public void ResetCompletion()
```

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_SetCompletionSignal_System_String_"></a> SetCompletionSignal\(string\)

제출 완료 시 올릴 서버 세션 전역 신호를 설정한다.

```csharp
public void SetCompletionSignal(string signalIdentifier)
```

#### Parameters

`signalIdentifier` string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_SetEnabled_System_Boolean_"></a> SetEnabled\(bool\)

상호작용 활성/비활성 전환(그래프 노드에서 사용).

```csharp
public void SetEnabled(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionInteractable_SetRequiredItems_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_InteractableEntity_ItemRequirement__"></a> SetRequiredItems\(IReadOnlyList<ItemRequirement\>\)

요구 아이템 목록만 덮어쓴다.

```csharp
public void SetRequiredItems(IReadOnlyList<ItemRequirement> requiredItems)
```

#### Parameters

`requiredItems` IReadOnlyList<[ItemRequirement](MultiplayerInfrastructure.InteractableEntity.ItemRequirement.md)\>

