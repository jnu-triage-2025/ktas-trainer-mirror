# <a id="TriageTrainer_DebugTools_ScenarioDevStub"></a> Class ScenarioDevStub

Namespace: [TriageTrainer.DebugTools](TriageTrainer.DebugTools.md)  
Assembly: Assembly\-CSharp.dll  

IndevScene 등 개발 씬에서, 시나리오가 요구하는 인터랙션 대상/트리거존을 간이로 대체하는
디버그 스텁. ScenarioDevStubSpawner 가 자동 생성한다.

<p>두 가지 모드</p>
<ul><li><b>Interactable</b>: 원기둥(Cylinder). 플레이어가 상호작용하면 신호를 올린다.</li><li><b>TriggerZone</b>: 통과형 넓은 큐브(isTrigger). 플레이어가 들어오면 신호를 올린다.</li></ul>

어느 모드든 지정한 식별자로 레지스트리에 엔티티를 등록하여, 사전 검증(Preflight)의
InteractionTarget 점검을 충족시키고, 인터랙션/통과 시 게이트 신호(sig.*)를 올린다.

디버그 전용이며 빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다.

```csharp
[DisallowMultipleComponent]
public sealed class ScenarioDevStub : MonoBehaviour, IInteractable, IInteract
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ScenarioDevStub](TriageTrainer.DebugTools.ScenarioDevStub.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)

## Properties

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

## Methods

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_Configure_TriageTrainer_DebugTools_ScenarioDevStub_StubMode_System_String_System_String___"></a> Configure\(StubMode, string, string\[\]\)

스포너가 생성 직후 설정하는 초기화 진입점.

```csharp
public void Configure(ScenarioDevStub.StubMode mode, string identifier, string[] signals)
```

#### Parameters

`mode` [ScenarioDevStub](TriageTrainer.DebugTools.ScenarioDevStub.md).[StubMode](TriageTrainer.DebugTools.ScenarioDevStub.StubMode.md)

`identifier` string

`signals` string\[\]

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_DebugTools_ScenarioDevStub_RaiseSignals"></a> RaiseSignals\(\)

```csharp
[ContextMenu("Raise Signals Now")]
public void RaiseSignals()
```

