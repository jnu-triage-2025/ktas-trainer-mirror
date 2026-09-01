# <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment"></a> Class StaticObjectDisplayment

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

맵에 사전 배치되어 있으며 다른 제어 흐름(상호작용/시나리오 등)에 의해 표시(Show)/비표시(Hide)될 수 있는
정적 오브젝트의 공통 골격(추상 베이스)입니다.

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 과 마찬가지로 Rigidbody 물리로 스폰하지 않으며, 네트워크 스폰 오브젝트가
아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"입니다. Interactable 하며(상호작용 항목이 있고),
상호작용/외부 흐름에 따라 렌더러 또는 게임오브젝트를 켜고 끌 수 있습니다.
(편의를 위해 Registry 에는 <xref href="MultiplayerInfrastructure.Registry.EntityType.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 로 등록됩니다.)
</p>

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 는 "획득 상호작용 → 설정에 따라 표시 On/Off" 라는 하나의 구체적인 동작을
가지지만, <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 는 <b>구조만</b> 공유합니다. 실제 상호작용 동작
(무엇을 소비/적용하고 언제 표시할지)은 파생 구현마다 다르므로, <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.Interact(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 와
<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.CanInteract(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 를 파생 클래스가 정의합니다. 베이스는 등록/콜라이더 보장/표시 토글 유틸리티만
제공합니다.
</p>

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public abstract class StaticObjectDisplayment : Interactable, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget, IInteractorConditional
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md) ← 
[StaticObjectDisplayment](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md)

#### Derived

[PatientMonitorMountPoint](TriageTrainer.Entity.PatientMonitor.PatientMonitorMountPoint.md), 
[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md), 
[WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md)

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

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_EntityIdPrefix"></a> EntityIdPrefix

파생 클래스가 식별자 자동 생성 시 사용할 접두사입니다.

```csharp
protected virtual string EntityIdPrefix { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_EntityIdentifier"></a> EntityIdentifier

이 정적 오브젝트의 전역 식별자(서버/모든 클라이언트 동일).

```csharp
public string EntityIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_IsVisible"></a> IsVisible

현재 로컬 표현이 표시(보임) 상태인지 여부입니다.

```csharp
public bool IsVisible { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_ShareMode"></a> ShareMode

표시(설치/적용) 상태의 전파 방식입니다.

```csharp
public StaticObjectDisplaymentShareMode ShareMode { get; }
```

#### Property Value

 [StaticObjectDisplaymentShareMode](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.md)

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_SharesShownStateAcrossServer"></a> SharesShownStateAcrossServer

표시(설치/적용) 상태가 서버 권위로 모든 클라이언트에 전파되는지 여부입니다.

```csharp
public bool SharesShownStateAcrossServer { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_ApplyHiddenFromNetwork"></a> ApplyHiddenFromNetwork\(\)

서버 전역 상태가 해제됐을 때 모든 클라이언트의 로컬 표현을 숨깁니다.

```csharp
public virtual void ApplyHiddenFromNetwork()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_ApplyInitialVisibility"></a> ApplyInitialVisibility\(\)

시작 시점의 표시 상태를 적용합니다. 기본 구현은 인스펙터의 <code>_initiallyVisible</code> 값을 따릅니다.
규약상 항상 특정 상태로 시작해야 하는 파생 구현은 이 메서드를 오버라이드합니다.

```csharp
protected virtual void ApplyInitialVisibility()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_ApplyShownFromNetwork"></a> ApplyShownFromNetwork\(\)

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
public virtual void ApplyShownFromNetwork()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_Awake"></a> Awake\(\)

```csharp
protected virtual void Awake()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public abstract bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_Hide"></a> Hide\(\)

이 오브젝트를 비표시(숨김) 상태로 만든다.

```csharp
public virtual void Hide()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public override abstract void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_OnDestroy"></a> OnDestroy\(\)

```csharp
protected virtual void OnDestroy()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_OnDisable"></a> OnDisable\(\)

```csharp
protected virtual void OnDisable()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_OnEnable"></a> OnEnable\(\)

```csharp
protected virtual void OnEnable()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_OnHiddenConfirmed"></a> OnHiddenConfirmed\(\)

표시(설치)가 권위 경로에서 해제되었을 때 한 번 호출되는 훅입니다.

```csharp
public virtual void OnHiddenConfirmed()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_OnShownConfirmed"></a> OnShownConfirmed\(\)

표시(설치/적용)가 <b>권위 경로에서 최초로 확정</b>되었을 때 한 번만 호출되는 훅입니다.

<ul><li><xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.ServerShared" data-throw-if-not-resolved="false"></xref>: 서버에서 상태를 확정한 직후
(전체 브로드캐스트 전에) 서버 컨텍스트에서 1회 호출됩니다.</li><li><xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.LocalOnly" data-throw-if-not-resolved="false"></xref>: 로컬에서 표시를 적용할 때 1회 호출됩니다.</li></ul>

시나리오 신호 발생처럼 "최초 확정 시 정확히 한 번" 수행해야 하는 부수효과를 이 훅에서 처리하세요.
(모든 옵저버에서 실행되는 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.ApplyShownFromNetwork" data-throw-if-not-resolved="false"></xref> 에 두면 중복 실행됩니다.)
기본 구현은 아무것도 하지 않습니다.

```csharp
public virtual void OnShownConfirmed()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_OnValidate"></a> OnValidate\(\)

```csharp
protected virtual void OnValidate()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_RequestApplyShown_MultiplayerInfrastructure_Player_PlayerController_System_String_System_Int32_"></a> RequestApplyShown\(PlayerController, string, int\)

표시(설치/적용)를 요청합니다. <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.ShareMode" data-throw-if-not-resolved="false"></xref> 에 따라 전파 경로가 달라집니다.

<ul><li><xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.ServerShared" data-throw-if-not-resolved="false"></xref>: <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 서버 권위
프로토콜로 위임합니다(승인 → 요청자 인벤토리 소비 → 전체 브로드캐스트, 신규 접속자 동기화 포함).</li><li><xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.LocalOnly" data-throw-if-not-resolved="false"></xref>: 이 클라이언트에서만 요구 아이템을 소비하고
즉시 표시를 적용합니다(네트워크 전파 없음).</li></ul>

요구 아이템 소비가 필요 없으면 <code class="paramref">requiredItemIdentifier</code> 를 비우거나
<code class="paramref">consumeCount</code> 를 0 으로 둡니다.

```csharp
protected bool RequestApplyShown(PlayerController requester, string requiredItemIdentifier = null, int consumeCount = 0)
```

#### Parameters

`requester` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`requiredItemIdentifier` string

`consumeCount` int

#### Returns

 bool

요청/적용을 시작했으면 true. (실패한 로컬 소비 등으로) 적용하지 못했으면 false.

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_ResolvePlayer_UnityEngine_Transform_"></a> ResolvePlayer\(Transform\)

interactor Transform 에서 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 를 해석한다(없으면 null).

```csharp
protected static PlayerController ResolvePlayer(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_SetEntityIdentifier_System_String_"></a> SetEntityIdentifier\(string\)

```csharp
public void SetEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_SetVisible_System_Boolean_"></a> SetVisible\(bool\)

표시 상태를 <code class="paramref">visible</code> 로 설정한다.

```csharp
public void SetVisible(bool visible)
```

#### Parameters

`visible` bool

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_Show"></a> Show\(\)

이 오브젝트를 표시(보임) 상태로 만든다.

```csharp
public virtual void Show()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplayment_TryGetServerSharedItemExchange_System_String__System_Int32__"></a> TryGetServerSharedItemExchange\(out string, out int\)

```csharp
public virtual bool TryGetServerSharedItemExchange(out string itemIdentifier, out int consumeCount)
```

#### Parameters

`itemIdentifier` string

`consumeCount` int

#### Returns

 bool

