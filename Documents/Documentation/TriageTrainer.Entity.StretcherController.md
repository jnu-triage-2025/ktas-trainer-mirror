# <a id="TriageTrainer_Entity_StretcherController"></a> Class StretcherController

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

들것 전용 운반 컨트롤러(서버 권위 네트워크 대응).

<p>
최대 6명의 플레이어가 손잡이를 점유하여 협력 이동한다. 모든 점유 상태는
<xref href="FishNet.Object.Synchronizing.SyncVar%601" data-throw-if-not-resolved="false"></xref> 로 서버 권위 복제되며, 이동은 서버에서 입력을 집계한 뒤
ObserversRpc 로 전 피어에 transform 을 브로드캐스트한다.
</p>

<p>
오프라인(네트워크 비활성) 환경에서는 기존과 동일하게 로컬에서만 동작한다.
</p>

```csharp
[RequireComponent(typeof(NetworkObject))]
public sealed class StretcherController : NetworkBehaviour, IInteractable, IInteract, IInteractorConditional
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[StretcherController](TriageTrainer.Entity.StretcherController.md)

#### Implements

[IInteractable](MultiplayerInfrastructure.InteractableEntity.IInteractable.md), 
[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md), 
[IInteractorConditional](MultiplayerInfrastructure.InteractableEntity.IInteractorConditional.md)

## Properties

### <a id="TriageTrainer_Entity_StretcherController_AllowDisplayIconFallback"></a> AllowDisplayIconFallback

DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.

```csharp
public bool AllowDisplayIconFallback { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_Entity_StretcherController_DisplayColor"></a> DisplayColor

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
기본적으로는 하얀색으로 설정하세요.

```csharp
public Color DisplayColor { get; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Entity_StretcherController_DisplayIcon"></a> DisplayIcon

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.

```csharp
public Sprite DisplayIcon { get; }
```

#### Property Value

 Sprite

### <a id="TriageTrainer_Entity_StretcherController_DisplayText"></a> DisplayText

플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.

```csharp
public string DisplayText { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_Entity_StretcherController_Interacts"></a> Interacts

```csharp
public IInteract[] Interacts { get; }
```

#### Property Value

 [IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\[\]

### <a id="TriageTrainer_Entity_StretcherController_IsWeightBalanced"></a> IsWeightBalanced

```csharp
public bool IsWeightBalanced { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Entity_StretcherController_CanInteract_UnityEngine_Transform_"></a> CanInteract\(Transform\)

```csharp
public bool CanInteract(Transform interactor)
```

#### Parameters

`interactor` Transform

#### Returns

 bool

### <a id="TriageTrainer_Entity_StretcherController_Interact_UnityEngine_Transform_"></a> Interact\(Transform\)

플레이어가 상호작용할 때, 그 처리를 정의합니다.

```csharp
public void Interact(Transform interactor)
```

#### Parameters

`interactor` Transform

### <a id="TriageTrainer_Entity_StretcherController_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="TriageTrainer_Entity_StretcherController_OnStopClient"></a> OnStopClient\(\)

Called on the client before deinitializing this object.

```csharp
public override void OnStopClient()
```

### <a id="TriageTrainer_Entity_StretcherController_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

