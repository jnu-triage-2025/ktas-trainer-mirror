# <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint"></a> Class PatientMonitorMountPoint

Namespace: [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)  
Assembly: Assembly\-CSharp.dll  

월드맵에 사전 배치된 환자 모니터 오브젝트에 붙이는 설치 슬롯입니다.
이 컴포넌트는 네트워크 오브젝트가 아니며, 서버 전역 상태는 StaticObjectDisplaymentService가 관리합니다.

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class PatientMonitorMountPoint : StaticObjectDisplayment, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget, IInteractorConditional
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md) ← 
[StaticObjectDisplayment](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md) ← 
[PatientMonitorMountPoint](TriageTrainer.Entity.PatientMonitor.PatientMonitorMountPoint.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md)

#### Inherited Members

[StaticObjectDisplayment.EntityIdentifier](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_EntityIdentifier), 
[StaticObjectDisplayment.SetEntityIdentifier\(string\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_SetEntityIdentifier\_System\_String\_), 
[StaticObjectDisplayment.ShareMode](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ShareMode), 
[StaticObjectDisplayment.SharesShownStateAcrossServer](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_SharesShownStateAcrossServer), 
[StaticObjectDisplayment.IsVisible](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_IsVisible), 
[StaticObjectDisplayment.TryGetServerSharedItemExchange\(out string, out int\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_TryGetServerSharedItemExchange\_System\_String\_\_System\_Int32\_\_), 
[StaticObjectDisplayment.Show\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Show), 
[StaticObjectDisplayment.Hide\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Hide), 
[StaticObjectDisplayment.SetVisible\(bool\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_SetVisible\_System\_Boolean\_), 
[StaticObjectDisplayment.ApplyShownFromNetwork\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ApplyShownFromNetwork), 
[StaticObjectDisplayment.ApplyHiddenFromNetwork\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ApplyHiddenFromNetwork), 
[StaticObjectDisplayment.OnShownConfirmed\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnShownConfirmed), 
[StaticObjectDisplayment.OnHiddenConfirmed\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnHiddenConfirmed), 
[StaticObjectDisplayment.Interact\(Transform\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Interact\_UnityEngine\_Transform\_), 
[StaticObjectDisplayment.CanInteract\(Transform\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_CanInteract\_UnityEngine\_Transform\_), 
[Interactable.DisplayText](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayText), 
[Interactable.DisplayIcon](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcon), 
[Interactable.DisplayIcons](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcons), 
[Interactable.AllowDisplayIconFallback](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_AllowDisplayIconFallback), 
[Interactable.DisplayColor](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayColor), 
[Interactable.PresentationEntityIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_PresentationEntityIdentifier), 
[Interactable.InteractionIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_InteractionIdentifier), 
[Interactable.Interacts](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interacts), 
[Interactable.Interact\(Transform\)](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interact\_UnityEngine\_Transform\_)

## Constructors

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint__ctor"></a> PatientMonitorMountPoint\(\)

```csharp
public PatientMonitorMountPoint()
```

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public override Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public override string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_EntityIdPrefix"></a> EntityIdPrefix

파생 클래스가 식별자 자동 생성 시 사용할 접두사입니다.

```csharp
protected override string EntityIdPrefix { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_Interacts"></a> Interacts

```csharp
public override IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_ApplyHiddenFromNetwork"></a> ApplyHiddenFromNetwork\(\)

서버 전역 상태가 해제됐을 때 모든 클라이언트의 로컬 표현을 숨깁니다.

```csharp
public override void ApplyHiddenFromNetwork()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_ApplyInitialVisibility"></a> ApplyInitialVisibility\(\)

시작 시점의 표시 상태를 적용합니다. 기본 구현은 인스펙터의 <code>_initiallyVisible</code> 값을 따릅니다.
규약상 항상 특정 상태로 시작해야 하는 파생 구현은 이 메서드를 오버라이드합니다.

```csharp
protected override void ApplyInitialVisibility()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_ApplyShownFromNetwork"></a> ApplyShownFromNetwork\(\)

"이 오브젝트를 표시(설치/적용)된 상태로 로컬 표현에 반영" 하는 진입점입니다.
ServerShared 모드에서는 서버 권위 프로토콜(<xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 표시 적용 RPC)에 의해
모든 클라이언트에서 호출되고, LocalOnly 모드에서는 상호작용한 클라이언트에서 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.RequestApplyShown(MultiplayerInfrastructure.Player.PlayerController%2cSystem.String%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>
를 통해 호출됩니다.

<p>
기본 구현은 단순히 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.Show" data-throw-if-not-resolved="false"></xref> 를 호출합니다. 파생 클래스가 표시 외에 추가 상태
(예: <code>IsAttached</code> 플래그)를 함께 갱신해야 하면 이 메서드를 오버라이드합니다.
단, "최초 확정 시 1회" 부수효과는 여기가 아니라 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.OnShownConfirmed" data-throw-if-not-resolved="false"></xref> 에서 처리합니다.
</p>

```csharp
public override void ApplyShownFromNetwork()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public override bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public override void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_OnValidate"></a> OnValidate\(\)

```csharp
protected override void OnValidate()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorMountPoint_TryGetServerSharedItemExchange_System_String__System_Int32__"></a> TryGetServerSharedItemExchange\(out string, out int\)

```csharp
public override bool TryGetServerSharedItemExchange(out string itemIdentifier, out int consumeCount)
```

#### Parameters

`itemIdentifier` string

`consumeCount` int

#### Returns

 bool

