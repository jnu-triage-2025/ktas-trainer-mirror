# <a id="MultiplayerInfrastructure_Entity_Npc"></a> Class Npc

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[DisallowMultipleComponent]
public class Npc : Interactable, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget, ISpawnedEntityIdentifierReceiver, IOverheadPresentationAnchorProvider
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md) ← 
[Npc](MultiplayerInfrastructure.Entity.Npc.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[ISpawnedEntityIdentifierReceiver](MultiplayerInfrastructure.Registry.ISpawnedEntityIdentifierReceiver.md), 
[IOverheadPresentationAnchorProvider](MultiplayerInfrastructure.UI.IOverheadPresentationAnchorProvider.md)

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

### <a id="MultiplayerInfrastructure_Entity_Npc_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Entity_Npc_Interacts"></a> Interacts

```csharp
public override IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="MultiplayerInfrastructure_Entity_Npc_OverheadPresentationAnchor"></a> OverheadPresentationAnchor

```csharp
public Transform OverheadPresentationAnchor { get; }
```

#### Property Value

 Transform

### <a id="MultiplayerInfrastructure_Entity_Npc_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public override string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Entity_Npc_AddCustomInteractSource_UnityEngine_MonoBehaviour_"></a> AddCustomInteractSource\(MonoBehaviour\)

NPC 에 커스텀 Interactable 소스(<xref href="MultiplayerInfrastructure.InteractableEntity.IInteract" data-throw-if-not-resolved="false"></xref> 를 구현한 MonoBehaviour)를 런타임에 추가한다.
시나리오 그래프 노드(NPCControl)가 특정 시점에 상호작용을 부여할 때 사용한다.

```csharp
public bool AddCustomInteractSource(MonoBehaviour source)
```

#### Parameters

`source` MonoBehaviour

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_Npc_ApplySpawnedEntityIdentifier_System_String_"></a> ApplySpawnedEntityIdentifier\(string\)

EntityPresetSpawn이 요청한 런타임 식별자를 NPC/Entity 레지스트리에 동일하게 적용한다.
Instantiate의 Awake에서 프리팹 식별자로 먼저 등록된 경우에도 안전하게 재등록한다.

```csharp
public void ApplySpawnedEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Entity_Npc_ConfigureScenarioActingNpc_MultiplayerInfrastructure_Scenario_ScenarioActingNpcDefinition_"></a> ConfigureScenarioActingNpc\(ScenarioActingNpcDefinition\)

시나리오 최상위 actingNpcs 정의를 이 NPC 인스턴스에 적용한다.

```csharp
public void ConfigureScenarioActingNpc(ScenarioActingNpcDefinition actingNpc)
```

#### Parameters

`actingNpc` [ScenarioActingNpcDefinition](MultiplayerInfrastructure.Scenario.ScenarioActingNpcDefinition.md)

### <a id="MultiplayerInfrastructure_Entity_Npc_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public override void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="MultiplayerInfrastructure_Entity_Npc_RemoveCustomInteractSource_UnityEngine_MonoBehaviour_"></a> RemoveCustomInteractSource\(MonoBehaviour\)

이전에 추가된 커스텀 Interactable 소스를 NPC 에서 제거한다.

```csharp
public bool RemoveCustomInteractSource(MonoBehaviour source)
```

#### Parameters

`source` MonoBehaviour

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Entity_Npc_SetScenarioDisplay_System_String_System_Nullable_System_Boolean__"></a> SetScenarioDisplay\(string, bool?\)

시나리오 노드가 NPC의 표시명과 머리 위 이름표를 런타임에 갱신한다.

```csharp
public void SetScenarioDisplay(string displayName, bool? showOverheadName)
```

#### Parameters

`displayName` string

`showOverheadName` bool?

