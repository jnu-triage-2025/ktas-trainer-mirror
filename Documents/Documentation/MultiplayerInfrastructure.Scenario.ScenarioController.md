# <a id="MultiplayerInfrastructure_Scenario_ScenarioController"></a> Class ScenarioController

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 흐름을 제어합니다.
UI 제어는 ScenarioPanelUIController에 위임합니다.

```csharp
public class ScenarioController : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ScenarioController](MultiplayerInfrastructure.Scenario.ScenarioController.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_ConcurrencyConflictPolicy"></a> ConcurrencyConflictPolicy

동시 대화창 점유 충돌 처리 정책. 인게임 커맨드로 런타임 변경 가능.

```csharp
public ScenarioConcurrencyConflictPolicy ConcurrencyConflictPolicy { get; set; }
```

#### Property Value

 [ScenarioConcurrencyConflictPolicy](MultiplayerInfrastructure.Scenario.ScenarioConcurrencyConflictPolicy.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_CurrentGraph"></a> CurrentGraph

```csharp
public ScenarioGraph CurrentGraph { get; }
```

#### Property Value

 [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_CurrentNode"></a> CurrentNode

```csharp
public IScenarioNode CurrentNode { get; }
```

#### Property Value

 [IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_CurrentState"></a> CurrentState

```csharp
public ScenarioController.State CurrentState { get; }
```

#### Property Value

 [ScenarioController](MultiplayerInfrastructure.Scenario.ScenarioController.md).[State](MultiplayerInfrastructure.Scenario.ScenarioController.State.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_HasActiveScenario"></a> HasActiveScenario

활성화된(재생 중인) 시나리오 그래프가 존재하는지 여부.

```csharp
public bool HasActiveScenario { get; }
```

#### Property Value

 bool

#### Remarks

Local/ServerAuthoritative/ClientPresentation 모든 실행 모드에서 그래프가 시작되어
종료되지 않은 동안 true 이다(설정: StartScenarioInternal/BeginPresentationScenario,
해제: EndScenario/EndPresentationScenario). <xref href="MultiplayerInfrastructure.Scenario.ScenarioController.IsActive" data-throw-if-not-resolved="false"></xref> 는 현재 노드 실행 상태
(_state) 기반이라 클라이언트 표시 모드나 즉시 진행 노드 체인 사이에서는 false 일 수
있으므로, "시나리오가 재생 중인가" 판정에는 이 프로퍼티를 사용해야 한다.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_Instance"></a> Instance

```csharp
public static ScenarioController Instance { get; }
```

#### Property Value

 [ScenarioController](MultiplayerInfrastructure.Scenario.ScenarioController.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_IsActive"></a> IsActive

```csharp
public bool IsActive { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_IsAuthoritativeExecutor"></a> IsAuthoritativeExecutor

이 피어가 그래프를 서버 권위로 순회하고 있는지. true 면 나머지 피어는 표시 전용이라
서버 커서만 옮기면 되고, false 면 각 피어가 자기 상태기를 직접 옮겨야 한다
(호환 실행 경로에서는 대상 클라이언트마다 독립 상태기가 돈다).

```csharp
public bool IsAuthoritativeExecutor { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_ValidatorBlockLogTargets"></a> ValidatorBlockLogTargets

```csharp
public ScenarioValidatorBlockLogTarget ValidatorBlockLogTargets { get; set; }
```

#### Property Value

 [ScenarioValidatorBlockLogTarget](MultiplayerInfrastructure.Scenario.ScenarioValidatorBlockLogTarget.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_Advance"></a> Advance\(\)

다음 노드로 진행

```csharp
public void Advance()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_BeginPresentationScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_Nullable_System_Int32__"></a> BeginPresentationScenario\(ScenarioGraph, int?\)

서버가 시작을 통지한 클라이언트의 표시 전용 상태를 준비한다. 이 경로는 그래프를
실행하거나 RuntimeState를 지우지 않는다.

```csharp
public void BeginPresentationScenario(ScenarioGraph graph, int? ownerClientId)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`ownerClientId` int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_CanAcceptPatientBCMonitorClose_System_Int32_System_String_"></a> CanAcceptPatientBCMonitorClose\(int, string\)

B/C 환자 모니터 닫기 완료 신호의 승인 여부를 판정한다.

<p>모니터 UI 활성화 여부(arm)는 모니터 쪽 서버 권한 상태가 관리하므로, 여기서는
실행 모드와 무관하게 '지금 이 그래프가 활성 상태이고, 발신자가 활성 역할 브랜치에
참여 중이며, 신호가 아직 올라가지 않았는가'만 검사한다. 호환 실행 경로(Local 모드,
그래프에 미지원 노드가 있어 릴레이가 권위 실행을 거부한 경우)에서도 호스트가 그래프를
실행하므로 모드로 거부하면 닫기 완료가 불가능해진다.</p>

```csharp
public bool CanAcceptPatientBCMonitorClose(int senderClientId, string normalizedSignal)
```

#### Parameters

`senderClientId` int

`normalizedSignal` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_CollectManualEntrypointIdentifiers_MultiplayerInfrastructure_Scenario_ScenarioGraph_"></a> CollectManualEntrypointIdentifiers\(ScenarioGraph\)

그래프에서 ManualEntrypoint 별칭만 뽑아낸다.

```csharp
public static IReadOnlyList<string> CollectManualEntrypointIdentifiers(ScenarioGraph graph)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_DismissPresentationUI_System_String_"></a> DismissPresentationUI\(string\)

서버가 재생 위치를 옮겼을 때 표시 피어에 남은 대화 UI 를 내린다.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.DismissAuthoritativePresentation(System.String)" data-throw-if-not-resolved="false"></xref> 가 호출한다.

```csharp
public void DismissPresentationUI(string graphIdentifier)
```

#### Parameters

`graphIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_EndPresentationScenario_System_String_"></a> EndPresentationScenario\(string\)

서버가 종료를 통지한 클라이언트 표시 상태만 정리한다.

```csharp
public void EndPresentationScenario(string graphIdentifier)
```

#### Parameters

`graphIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_EndScenario"></a> EndScenario\(\)

시나리오 종료

```csharp
public void EndScenario()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_GetManualEntrypointIdentifiers"></a> GetManualEntrypointIdentifiers\(\)

현재 그래프가 선언한 ManualEntrypoint 별칭을 순서 없이 모아 반환한다.
명령 자동완성과 오류 안내에 쓴다.

```csharp
public IReadOnlyList<string> GetManualEntrypointIdentifiers()
```

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_GetNodeVisitNotes_System_String_System_Int32_"></a> GetNodeVisitNotes\(string, int\)

지정한 노드 방문에서 발생한 흐름 변경/경고 메모를 반환한다.

```csharp
public IReadOnlyList<string> GetNodeVisitNotes(string graphIdentifier, int sequence)
```

#### Parameters

`graphIdentifier` string

`sequence` int

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_GetNodeVisitOrders_System_String_System_String_"></a> GetNodeVisitOrders\(string, string\)

```csharp
public IReadOnlyList<int> GetNodeVisitOrders(string graphIdentifier, string nodeIdentifier)
```

#### Parameters

`graphIdentifier` string

`nodeIdentifier` string

#### Returns

 IReadOnlyList<int\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_GetNodeVisitOrders_System_String_"></a> GetNodeVisitOrders\(string\)

```csharp
public IReadOnlyList<int> GetNodeVisitOrders(string nodeIdentifier)
```

#### Parameters

`nodeIdentifier` string

#### Returns

 IReadOnlyList<int\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_PresentAuthoritativeNode_System_String_System_String_System_Boolean_"></a> PresentAuthoritativeNode\(string, string, bool\)

서버가 보낸 노드를 클라이언트 UI에 표시한다.

```csharp
public void PresentAuthoritativeNode(string graphIdentifier, string nodeIdentifier, bool roleScoped = false)
```

#### Parameters

`graphIdentifier` string

`nodeIdentifier` string

`roleScoped` bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_RegisterReferences_MultiplayerInfrastructure_UI_DialoguePanelUIController_MultiplayerInfrastructure_Camera_MainCameraController_MultiplayerInfrastructure_UI_InteractableObjectHintUIController_"></a> RegisterReferences\(DialoguePanelUIController, MainCameraController, InteractableObjectHintUIController\)

```csharp
public void RegisterReferences(DialoguePanelUIController uiController, MainCameraController camController, InteractableObjectHintUIController hintUIController)
```

#### Parameters

`uiController` [DialoguePanelUIController](MultiplayerInfrastructure.UI.DialoguePanelUIController.md)

`camController` [MainCameraController](MultiplayerInfrastructure.Camera.MainCameraController.md)

`hintUIController` [InteractableObjectHintUIController](MultiplayerInfrastructure.UI.InteractableObjectHintUIController.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_RestartScenario_System_String_"></a> RestartScenario\(string\)

현재 시나리오를 정리한 뒤 같은 그래프의 지정 진입점부터 다시 시작한다.

```csharp
public bool RestartScenario(string startNodeIdentifier = null)
```

#### Parameters

`startNodeIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_RunPresentationEvent_System_String_System_String_"></a> RunPresentationEvent\(string, string\)

서버가 실행한 연출 전용 이벤트를 표시 피어에서도 실행한다.
<xref href="MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.InvokePresentationEventAuthoritative(System.String)" data-throw-if-not-resolved="false"></xref> 가 호출한다.
그래프 순회는 서버가 담당하므로 이 경로는 노드를 진행시키지 않는다.

```csharp
public void RunPresentationEvent(string graphIdentifier, string eventIdentifier)
```

#### Parameters

`graphIdentifier` string

`eventIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_SelectOption_System_Int32_"></a> SelectOption\(int\)

선택지 선택

```csharp
public void SelectOption(int index)
```

#### Parameters

`index` int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_StartAuthoritativeScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_String_System_Nullable_System_Int32__"></a> StartAuthoritativeScenario\(ScenarioGraph, string, int?\)

서버 권위 시나리오를 시작한다. 그래프 순회는 이 서버 인스턴스에서만 수행한다.

```csharp
public void StartAuthoritativeScenario(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`startNodeIdentifier` string

`ownerClientId` int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_StartScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_String_"></a> StartScenario\(ScenarioGraph, string\)

시나리오 시작

```csharp
public void StartScenario(ScenarioGraph graph, string startNodeIdentifier = null)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`startNodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_StartScenario_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_String_System_Nullable_System_Int32__"></a> StartScenario\(ScenarioGraph, string, int?\)

```csharp
public void StartScenario(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`startNodeIdentifier` string

`ownerClientId` int?

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_SubmitLocalAdvance"></a> SubmitLocalAdvance\(\)

로컬 대화 UI의 다음 진행 요청을 처리한다. 서버와 클라이언트를 겸하는 호스트에서는
권위 상태기가 직접 노출되므로, 이 진입점에서만 owner 정책을 검사한다. 내부 자동 진행은
<xref href="MultiplayerInfrastructure.Scenario.ScenarioController.Advance" data-throw-if-not-resolved="false"></xref>를 계속 사용하여 원격 소유 시나리오도 서버에서 정상 진행한다.

```csharp
public void SubmitLocalAdvance()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_SubmitLocalOptionSelection_System_Int32_"></a> SubmitLocalOptionSelection\(int\)

로컬 대화 UI의 선택 요청을 owner 정책에 따라 처리한다.

```csharp
public void SubmitLocalOptionSelection(int index)
```

#### Parameters

`index` int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_TryEnterManualEntrypoint_System_String_System_Boolean_System_String__"></a> TryEnterManualEntrypoint\(string, bool, out string\)

재생 위치를 ManualEntrypoint 노드로 옮긴다. 진행 중이던 노드/브랜치 코루틴은 모두 중단된다.

```csharp
public bool TryEnterManualEntrypoint(string entrypointIdentifier, bool clearState, out string error)
```

#### Parameters

`entrypointIdentifier` string

ManualEntrypoint 노드의 별칭 또는 식별자.

`clearState` bool

true 면 지금까지 쌓인 시나리오 상태(상태값·신호·카운터·타이머·발행된 퀘스트)를 먼저 비운다.

`error` string

실패했을 때 사용자에게 보여줄 사유.

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_TryFindGroundY_UnityEngine_Vector3_UnityEngine_Collider___System_Single__"></a> TryFindGroundY\(Vector3, Collider\[\], out float\)

지정 위치 아래의 가장 높은 비트리거 충돌면을 찾는다. 일반적인 경우에는 재사용 버퍼를
사용해 할당하지 않으며, 버퍼가 가득 찬 경우에만 정확한 결과를 위해 전체 히트를 다시 읽는다.

```csharp
public static bool TryFindGroundY(Vector3 position, Collider[] ignoredColliders, out float groundY)
```

#### Parameters

`position` Vector3

`ignoredColliders` Collider\[\]

`groundY` float

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_TryFindManualEntrypoint_MultiplayerInfrastructure_Scenario_ScenarioGraph_System_String_MultiplayerInfrastructure_Scenario_ScenarioManualEntrypointNode__"></a> TryFindManualEntrypoint\(ScenarioGraph, string, out ScenarioManualEntrypointNode\)

별칭(<code>entrypointIdentifier</code>) 또는 노드 식별자로 ManualEntrypoint 노드를 찾는다.
별칭이 먼저이고, 없으면 노드 식별자로 한 번 더 찾는다.

```csharp
public static bool TryFindManualEntrypoint(ScenarioGraph graph, string entrypointIdentifier, out ScenarioManualEntrypointNode entrypoint)
```

#### Parameters

`graph` [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

`entrypointIdentifier` string

`entrypoint` [ScenarioManualEntrypointNode](MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_TryGetNodeVisitTiming_System_String_System_Int32_MultiplayerInfrastructure_Scenario_ScenarioNodeVisitTiming__"></a> TryGetNodeVisitTiming\(string, int, out ScenarioNodeVisitTiming\)

지정한 방문 순서의 노드 진입/이탈 시각을 반환한다.

```csharp
public bool TryGetNodeVisitTiming(string graphIdentifier, int sequence, out ScenarioNodeVisitTiming timing)
```

#### Parameters

`graphIdentifier` string

`sequence` int

`timing` [ScenarioNodeVisitTiming](MultiplayerInfrastructure.Scenario.ScenarioNodeVisitTiming.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_InstanceAvailable"></a> InstanceAvailable

```csharp
public static event Action<ScenarioController> InstanceAvailable
```

#### Event Type

 Action<[ScenarioController](MultiplayerInfrastructure.Scenario.ScenarioController.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_OnNodeChanged"></a> OnNodeChanged

```csharp
public event Action<IScenarioNode> OnNodeChanged
```

#### Event Type

 Action<[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_OnOptionSelected"></a> OnOptionSelected

```csharp
public event Action<ScenarioChoiceOption> OnOptionSelected
```

#### Event Type

 Action<[ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md)\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_OnScenarioEnded"></a> OnScenarioEnded

```csharp
public event Action OnScenarioEnded
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_OnScenarioStarted"></a> OnScenarioStarted

```csharp
public event Action OnScenarioStarted
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_Scenario_ScenarioController_OnValidatorWaitTimeout"></a> OnValidatorWaitTimeout

WaitForCondition 게이트가 WaitTimeoutSeconds 안에 조건을 충족하지 못해 타임아웃 정책이
적용될 때 1회 발생한다. 평가 기록(루브릭의 "미수행" 판정 등, G-3)에서 구독할 수 있다.
인자: (타임아웃된 Validator 노드, 적용된 타임아웃 정책).

```csharp
public event Action<ScenarioValidatorNode, ScenarioValidatorWaitTimeoutBehavior> OnValidatorWaitTimeout
```

#### Event Type

 Action<[ScenarioValidatorNode](MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.md), [ScenarioValidatorWaitTimeoutBehavior](MultiplayerInfrastructure.Scenario.ScenarioValidatorWaitTimeoutBehavior.md)\>

