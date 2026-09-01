# <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter"></a> Class WallAttachedOxyflowmeter

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

벽면에 장착하는 산소 유량계(Oxyflowmeter) 표현입니다.

<p>
처음에는 <b>보이지 않는 상태</b>로 시작합니다. 플레이어가 인벤토리의 산소 유량계
(<xref href="TriageTrainer.ItemDefinitions.Oxyflowmeter" data-throw-if-not-resolved="false"></xref>)를 <b>손에 든 채</b> 이 오브젝트 근처
(부착된 트리거 Collider 범위 안)에 있으면 "설치(장착)" 상호작용이 힌트로 노출됩니다. 상호작용하면
이 오브젝트가 표시(Show)되고 인벤토리의 산소 유량계 1개가 소비됩니다.
</p>

<p>
상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이 상태는 이후 데이터로 사용할 수
있도록 <xref href="TriageTrainer.Entity.WallAttachedOxyflowmeter.IsAttached" data-throw-if-not-resolved="false"></xref> 불리언 플래그로 공개합니다.
</p>

<p>
설치 상태의 전파 방식은 베이스의 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.ShareMode" data-throw-if-not-resolved="false"></xref> 로 설정합니다
(기본값 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.ServerShared" data-throw-if-not-resolved="false"></xref>). ServerShared 이면 상호작용이
<xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 서버 권위 프로토콜로 위임되어, 확정 시 서버가 모든 클라이언트에
표시(설치)를 브로드캐스트하고(신규 접속자 포함) 요청자 클라이언트에서 산소 유량계가 소비됩니다
(진실 원천: <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentService" data-throw-if-not-resolved="false"></xref>). LocalOnly 이면 상호작용한 클라이언트에서만
소비/표시되고 전파되지 않습니다.
</p>

```csharp
[DisallowMultipleComponent]
public class WallAttachedOxyflowmeter : StaticObjectDisplayment, IInteractable, IInteract, IInteractDisplayIcons, IQuestPresentationTarget, IInteractorConditional, INearestOnlyInteract, IAttachCompletionSignalConfigurable
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[Interactable](MultiplayerInfrastructure.InteractableEntity.Interactable.md) ← 
[StaticObjectDisplayment](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md) ← 
[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractDisplayIcons](MultiplayerInfrastructure.InteractableEntity.IInteractDisplayIcons.md), 
[IQuestPresentationTarget](MultiplayerInfrastructure.InteractableEntity.IQuestPresentationTarget.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md), 
[INearestOnlyInteract](MultiplayerInfrastructure.InteractableEntity.INearestOnlyInteract.md), 
[IAttachCompletionSignalConfigurable](TriageTrainer.Entity.IAttachCompletionSignalConfigurable.md)

#### Inherited Members

[StaticObjectDisplayment.EntityIdPrefix](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_EntityIdPrefix), 
[StaticObjectDisplayment.EntityIdentifier](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_EntityIdentifier), 
[StaticObjectDisplayment.SetEntityIdentifier\(string\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_SetEntityIdentifier\_System\_String\_), 
[StaticObjectDisplayment.ShareMode](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ShareMode), 
[StaticObjectDisplayment.SharesShownStateAcrossServer](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_SharesShownStateAcrossServer), 
[StaticObjectDisplayment.IsVisible](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_IsVisible), 
[StaticObjectDisplayment.TryGetServerSharedItemExchange\(out string, out int\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_TryGetServerSharedItemExchange\_System\_String\_\_System\_Int32\_\_), 
[StaticObjectDisplayment.Awake\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Awake), 
[StaticObjectDisplayment.ApplyInitialVisibility\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ApplyInitialVisibility), 
[StaticObjectDisplayment.OnEnable\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnEnable), 
[StaticObjectDisplayment.OnDisable\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnDisable), 
[StaticObjectDisplayment.OnDestroy\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnDestroy), 
[StaticObjectDisplayment.Show\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Show), 
[StaticObjectDisplayment.Hide\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Hide), 
[StaticObjectDisplayment.SetVisible\(bool\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_SetVisible\_System\_Boolean\_), 
[StaticObjectDisplayment.ApplyShownFromNetwork\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ApplyShownFromNetwork), 
[StaticObjectDisplayment.ApplyHiddenFromNetwork\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ApplyHiddenFromNetwork), 
[StaticObjectDisplayment.OnShownConfirmed\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnShownConfirmed), 
[StaticObjectDisplayment.OnHiddenConfirmed\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnHiddenConfirmed), 
[StaticObjectDisplayment.RequestApplyShown\(PlayerController, string, int\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_RequestApplyShown\_MultiplayerInfrastructure\_Player\_PlayerController\_System\_String\_System\_Int32\_), 
[StaticObjectDisplayment.ResolvePlayer\(Transform\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_ResolvePlayer\_UnityEngine\_Transform\_), 
[StaticObjectDisplayment.Interact\(Transform\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_Interact\_UnityEngine\_Transform\_), 
[StaticObjectDisplayment.CanInteract\(Transform\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_CanInteract\_UnityEngine\_Transform\_), 
[StaticObjectDisplayment.OnValidate\(\)](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md\#MultiplayerInfrastructure\_ItemSystem\_StaticObjectDisplayment\_OnValidate), 
[Interactable.DisplayText](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayText), 
[Interactable.DisplayIcon](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcon), 
[Interactable.DisplayIcons](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayIcons), 
[Interactable.AllowDisplayIconFallback](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_AllowDisplayIconFallback), 
[Interactable.DisplayColor](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_DisplayColor), 
[Interactable.PresentationEntityIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_PresentationEntityIdentifier), 
[Interactable.InteractionIdentifier](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_InteractionIdentifier), 
[Interactable.Interacts](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interacts), 
[Interactable.Interact\(Transform\)](MultiplayerInfrastructure.InteractableEntity.Interactable.md\#MultiplayerInfrastructure\_InteractableEntity\_Interactable\_Interact\_UnityEngine\_Transform\_)

## Fields

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_DetachInteractionIdentifier"></a> DetachInteractionIdentifier

회수 상태의 상호작용 식별자. 설치/조작과 분리해 두면 퀘스트 표시 바인딩이
"산소 유량계 회수"에는 붙지 않는다(회수는 퀘스트가 지시하는 행동이 아니다).

```csharp
public const string DetachInteractionIdentifier = "oxyflowmeter_detach"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_QuestPresentationInteractionIdentifier"></a> QuestPresentationInteractionIdentifier

```csharp
public const string QuestPresentationInteractionIdentifier = "oxyflowmeter"
```

#### Field Value

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_UnmarkedInteractionIdentifier"></a> UnmarkedInteractionIdentifier

```csharp
public const string UnmarkedInteractionIdentifier = "oxyflowmeter_unmarked"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_DisplayIcons"></a> DisplayIcons

```csharp
public override IReadOnlyList<Sprite> DisplayIcons { get; }
```

#### Property Value

 IReadOnlyList<Sprite\>

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public override string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_EntityIdPrefix"></a> EntityIdPrefix

파생 클래스가 식별자 자동 생성 시 사용할 접두사입니다.

```csharp
protected override string EntityIdPrefix { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_InteractionIdentifier"></a> InteractionIdentifier

```csharp
public override string InteractionIdentifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_IsAttached"></a> IsAttached

```csharp
public bool IsAttached { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_IsAttachedInteractCompleted"></a> IsAttachedInteractCompleted

이 유량계의 "조작" 상호작용이 이미 수행되었는지 여부.
조작 신호를 설정하지 않은 유량계는 설치 즉시 회수 대상이므로 완료로 본다.

```csharp
public bool IsAttachedInteractCompleted { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_IsDetachInteraction"></a> IsDetachInteraction

설치 상태에서 조작 신호까지 올라가, 다음 상호작용이 회수로 동작하는지 여부.

```csharp
public bool IsDetachInteraction { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_NearestOnlyCollider"></a> NearestOnlyCollider

```csharp
public Collider NearestOnlyCollider { get; }
```

#### Property Value

 Collider

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_NearestOnlyDistanceOrigin"></a> NearestOnlyDistanceOrigin

```csharp
public Transform NearestOnlyDistanceOrigin { get; }
```

#### Property Value

 Transform

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_NearestOnlyGroup"></a> NearestOnlyGroup

```csharp
public string NearestOnlyGroup { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_NearestOnlyTieBreaker"></a> NearestOnlyTieBreaker

```csharp
public int NearestOnlyTieBreaker { get; }
```

#### Property Value

 int

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_OxyLineConnectionPoint"></a> OxyLineConnectionPoint

산소 라인 자동 연결에 사용할 유량계 측 포트. 프리팹에 설정되지 않으면 null이다.

```csharp
public OxyLineConnectionPoint OxyLineConnectionPoint { get; }
```

#### Property Value

 [OxyLineConnectionPoint](TriageTrainer.Entity.OxyLine.OxyLineConnectionPoint.md)

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_PresentationEntityIdentifier"></a> PresentationEntityIdentifier

```csharp
public override string PresentationEntityIdentifier { get; }
```

#### Property Value

 string

## Methods

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_ApplyHiddenFromNetwork"></a> ApplyHiddenFromNetwork\(\)

서버 전역 상태가 해제됐을 때 모든 클라이언트의 로컬 표현을 숨깁니다.

```csharp
public override void ApplyHiddenFromNetwork()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_ApplyInitialVisibility"></a> ApplyInitialVisibility\(\)

이 오브젝트는 규약상 항상 "미설치(숨김)" 상태로 시작하므로 베이스의 <code>_initiallyVisible</code> 설정을 무시한다.

```csharp
protected override void ApplyInitialVisibility()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_ApplyShownFromNetwork"></a> ApplyShownFromNetwork\(\)

이 오브젝트가 표시(설치/적용)될 때 로컬 표현에 반영한다(모든 클라이언트에서 실행).
표시와 함께 <xref href="TriageTrainer.Entity.WallAttachedOxyflowmeter.IsAttached" data-throw-if-not-resolved="false"></xref> 를 확정한다. 완료 신호는 "최초 확정 시 1회"만 필요하므로
여기가 아니라 <xref href="TriageTrainer.Entity.WallAttachedOxyflowmeter.OnShownConfirmed" data-throw-if-not-resolved="false"></xref> 에서 처리한다(옵저버 중복 실행 방지).

```csharp
public override void ApplyShownFromNetwork()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

아직 설치되지 않았고, 플레이어가 산소 유량계를 손에 든 채 근처(콜라이더 범위)에 있을 때만 상호작용 가능합니다.

```csharp
public override bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_Detach"></a> Detach\(\)

설치 상태를 해제하고 다시 숨긴다(관리자 리셋/시나리오 되돌림 등에서 사용하는 로컬 표현 API).

```csharp
public void Detach()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_Hide"></a> Hide\(\)

이 오브젝트를 비표시(숨김) 상태로 만든다.

```csharp
public override void Hide()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public override void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_OnHiddenConfirmed"></a> OnHiddenConfirmed\(\)

표시(설치)가 권위 경로에서 해제되었을 때 한 번 호출되는 훅입니다.

```csharp
public override void OnHiddenConfirmed()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_OnShownConfirmed"></a> OnShownConfirmed\(\)

표시(설치)가 권위 경로에서 최초로 확정될 때 1회 호출된다(ServerShared: 서버, LocalOnly: 로컬).
설치 완료 시나리오 신호를 여기서 올린다.

```csharp
public override void OnShownConfirmed()
```

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_ResolveAttachedInteractSignal"></a> ResolveAttachedInteractSignal\(\)

이 유량계 전용 조작 신호. 설정값(<code>_attachedInteractSignal</code>)은 프리팹 공유라
그대로 쓰면 구역 하나를 조작한 순간 다른 구역의 유량계까지 회수 상태가 된다.
엔티티 식별자를 붙여 유량계별로 상태를 분리한다.

```csharp
public string ResolveAttachedInteractSignal()
```

#### Returns

 string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_SetAttachCompletionSignalForEditor_System_String_"></a> SetAttachCompletionSignalForEditor\(string\)

배치 데이터가 설치 완료 신호를 다시 주입할 때 호출한다. 이 값을 프리팹 오버라이드로만
남기면 레이아웃을 다시 생성할 때 지워지므로, 재생성 경로에서 항상 이 메서드로 복원한다.

```csharp
public void SetAttachCompletionSignalForEditor(string signal)
```

#### Parameters

`signal` string

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_TryGetServerSharedItemExchange_System_String__System_Int32__"></a> TryGetServerSharedItemExchange\(out string, out int\)

```csharp
public override bool TryGetServerSharedItemExchange(out string itemIdentifier, out int consumeCount)
```

#### Parameters

`itemIdentifier` string

`consumeCount` int

#### Returns

 bool

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_AttachmentStateChanged"></a> AttachmentStateChanged

```csharp
public static event Action<WallAttachedOxyflowmeter, bool> AttachmentStateChanged
```

#### Event Type

 Action<[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md), bool\>

### <a id="TriageTrainer_Entity_WallAttachedOxyflowmeter_InstallationConfirmed"></a> InstallationConfirmed

플레이어 상호작용으로 새 설치가 확정된 경우에만 발생한다.

```csharp
public static event Action<WallAttachedOxyflowmeter> InstallationConfirmed
```

#### Event Type

 Action<[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)\>

