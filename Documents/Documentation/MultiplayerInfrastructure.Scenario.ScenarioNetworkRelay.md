# <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay"></a> Class ScenarioNetworkRelay

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 도메인의 서버 권한(authoritative) 신호 중계기.

설계 근거(G-8, P1 단계):
- 시나리오 게이팅에 쓰이는 완료 신호는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref> 를 통해
  <code>RegistryType.RuntimeState</code> 레지스트리(정적·비네트워크)에 기록된다.
- 인터랙션은 각 클라이언트 컨텍스트에서 일어나므로, 신호가 클라이언트 로컬에만 남으면
  "한 플레이어의 행동이 다른 플레이어 브랜치의 게이트를 통과시키는" 다인 협력이 성립하지 않는다.
- 이 중계기는 클라이언트가 올린 신호를 ServerRpc 로 서버에 보고하여
  서버의 단일 권위 RuntimeState 에 기록되게 한다. (Validator 판정은 서버에서 수행)

사용:
- 게임플레이 코드는 그대로 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref> 만 호출하면 된다.
  서버 컨텍스트면 직접 기록, 클라이언트 컨텍스트면 이 중계기를 통해 서버로 보고된다.

추가 범위(P3 1차): 서버가 단일 그래프 상태기를 실행하고, 클라이언트는 시작/표시/입력 보고만
수행하도록 Dialogue·Choice의 표현 RPC를 제공한다. 이 경로는 지원 노드 집합으로 검증된
그래프에만 사용하며, 병렬 역할 브랜치와 미구현 표현 노드는 기존 호환 경로로 폴백한다.
단일 플레이어(호스트 단독)에서는 서버=클라 이므로 동작이 기존과 동일하다.

```csharp
public sealed class ScenarioNetworkRelay : NetworkBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
NetworkBehaviour ← 
[ScenarioNetworkRelay](MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_Instance"></a> Instance

씬에 배치된 중계기 인스턴스(없으면 null).

```csharp
public static ScenarioNetworkRelay Instance { get; }
```

#### Property Value

 [ScenarioNetworkRelay](MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_BroadcastManualEntry_System_String_System_Boolean_"></a> BroadcastManualEntry\(string, bool\)

호환 실행 경로(대상 클라이언트마다 독립 상태기가 도는 구성)에서 모든 피어가 각자
같은 지점으로 건너뛰게 한다. 서버 권위 실행 중이면 서버 커서 하나만 옮기면 되므로
이 브로드캐스트를 쓰지 않는다.

```csharp
public static bool BroadcastManualEntry(string entrypointIdentifier, bool clearState)
```

#### Parameters

`entrypointIdentifier` string

`clearState` bool

#### Returns

 bool

브로드캐스트를 실제로 보냈으면 true.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_BroadcastScenarioEnd"></a> BroadcastScenarioEnd\(\)

호환 실행 경로의 모든 피어에서 실행 중인 시나리오를 종료한다.

```csharp
public static bool BroadcastScenarioEnd()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_BroadcastScenarioRestart_System_String_"></a> BroadcastScenarioRestart\(string\)

호환 실행 경로의 모든 피어에서 실행 중인 시나리오를 다시 시작한다.

```csharp
public static bool BroadcastScenarioRestart(string entrypointIdentifier)
```

#### Parameters

`entrypointIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_ClearAuthoritative_System_String_"></a> ClearAuthoritative\(string\)

신호를 권위적으로 내린다(사이클 반복 등에서 재설정).

```csharp
public static void ClearAuthoritative(string normalizedSignalId)
```

#### Parameters

`normalizedSignalId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_ClearScenarioQuestsAuthoritative_System_String_"></a> ClearScenarioQuestsAuthoritative\(string\)

지정한 시나리오가 발행한 퀘스트만 모든 피어에서 제거한다. QuestManager 는 피어마다 따로
들고 있어서 서버에서 지우는 것만으로는 클라이언트 화면의 퀘스트가 남는다.

```csharp
public static void ClearScenarioQuestsAuthoritative(string scenarioIdentifier)
```

#### Parameters

`scenarioIdentifier` string

대상 시나리오 식별자. 비어 있으면 출처를 가릴 수 없으므로 아무것도 지우지 않는다.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_DismissAuthoritativePresentation_System_String_"></a> DismissAuthoritativePresentation\(string\)

서버가 재생 위치를 건너뛰었을 때 표시 피어에 남은 대화 UI 를 내리게 한다.

```csharp
public static void DismissAuthoritativePresentation(string graphIdentifier)
```

#### Parameters

`graphIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_EndAuthoritativePresentation_System_String_"></a> EndAuthoritativePresentation\(string\)

서버가 권위 시나리오의 종료를 표시 참여자에게 전달한다.

```csharp
public static void EndAuthoritativePresentation(string graphIdentifier)
```

#### Parameters

`graphIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_FlushSignalParametersAuthoritative"></a> FlushSignalParametersAuthoritative\(\)

서버 권위 신호 파라미터 저장소를 모든 피어에서 비운다.

```csharp
public static void FlushSignalParametersAuthoritative()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_GrantClientSignalCapability_System_Int32_System_String_System_Boolean_System_Boolean_"></a> GrantClientSignalCapability\(int, string, bool, bool\)

서버가 그래프 밖의 검증된 gameplay RPC에 일시적인 client signal capability를 부여한다.
기본적으로 raise만 허용하며 clear는 별도로 명시해야 한다.

```csharp
public static bool GrantClientSignalCapability(int clientId, string signalId, bool allowClear = false, bool isPrefix = false)
```

#### Parameters

`clientId` int

`signalId` string

`allowClear` bool

`isPrefix` bool

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_InvokePresentationEventAuthoritative_System_String_"></a> InvokePresentationEventAuthoritative\(string\)

서버가 실행한 연출 전용 이벤트를 표시 피어에서도 실행시킨다.

역할 브랜치 밖의 일반 InvokeEvent 노드는 그래프를 순회하는 권위 피어에서만 실행된다.
반면 상호작용에서 시작되는 연출은 상호작용한 피어의 로컬 상태로 남으므로, 그 연출을 끝내는
이벤트를 전달하지 않으면 해당 피어에서 연출과 입력 제약이 영구히 남는다. 시작을 로컬에서
수행하고 종료만 서버가 통지하는 연출은 이 중계를 통해 종료를 전 피어에 도달시킨다.

```csharp
public static void InvokePresentationEventAuthoritative(string eventIdentifier)
```

#### Parameters

`eventIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_OnStartClient"></a> OnStartClient\(\)

Called on the client after initializing this object.

```csharp
public override void OnStartClient()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_OnStartServer"></a> OnStartServer\(\)

Called on the server after initializing this object.
SyncTypes modified before or during this method will be sent to clients in the spawn message.

```csharp
public override void OnStartServer()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_OnStopServer"></a> OnStopServer\(\)

Called on the server before deinitializing this object.

```csharp
public override void OnStopServer()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_PresentAuthoritativeNode_System_String_System_String_"></a> PresentAuthoritativeNode\(string, string\)

서버가 현재 노드를 모든 표시 참여자에게 전달한다.

```csharp
public static void PresentAuthoritativeNode(string graphIdentifier, string nodeIdentifier)
```

#### Parameters

`graphIdentifier` string

`nodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_PresentAuthoritativeNodeToClient_System_Int32_System_String_System_String_"></a> PresentAuthoritativeNodeToClient\(int, string, string\)

병렬 역할 브랜치의 표현 노드를 배정된 클라이언트 한 명에게만 전달한다.

```csharp
public static void PresentAuthoritativeNodeToClient(int clientId, string graphIdentifier, string nodeIdentifier)
```

#### Parameters

`clientId` int

`graphIdentifier` string

`nodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_PublishLineTopologyChange_System_String_System_String_System_Boolean_"></a> PublishLineTopologyChange\(string, string, bool\)

```csharp
public static void PublishLineTopologyChange(string first, string second, bool connected)
```

#### Parameters

`first` string

`second` string

`connected` bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_PublishNPCControlUpdate_FishNet_Object_NetworkObject_MultiplayerInfrastructure_Scenario_ScenarioNPCControlNode_"></a> PublishNPCControlUpdate\(NetworkObject, ScenarioNPCControlNode\)

```csharp
public static void PublishNPCControlUpdate(NetworkObject actorObject, ScenarioNPCControlNode node)
```

#### Parameters

`actorObject` NetworkObject

`node` [ScenarioNPCControlNode](MultiplayerInfrastructure.Scenario.ScenarioNPCControlNode.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_PublishParallelAssignments_System_String_System_String_System_Collections_Generic_IReadOnlyDictionary_MultiplayerInfrastructure_Scenario_ScenarioParallelBranch_System_Nullable_System_Int32___"></a> PublishParallelAssignments\(string, string, IReadOnlyDictionary<ScenarioParallelBranch, int?\>\)

서버가 병렬 노드의 확정 배정표를 각 접속자에게 TargetRpc로 전달한다.
빈 배열도 전송해, 해당 parallel node에서 역할을 받지 못한 클라이언트가 이전 배정을
잘못 재사용하지 않게 한다.

```csharp
public static void PublishParallelAssignments(string graphIdentifier, string parallelNodeIdentifier, IReadOnlyDictionary<ScenarioParallelBranch, int?> allocation)
```

#### Parameters

`graphIdentifier` string

`parallelNodeIdentifier` string

`allocation` IReadOnlyDictionary<[ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md), int?\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_PublishScenarioActingNpcConfiguration_System_String_System_String_FishNet_Object_NetworkObject_"></a> PublishScenarioActingNpcConfiguration\(string, string, NetworkObject\)

서버에서 preset spawn 후 적용한 actingNpc 식별자와 인라인 상호작용을 동일한 NetworkObject의
원격 클라이언트 인스턴스에도 적용한다.

```csharp
public static bool PublishScenarioActingNpcConfiguration(string graphIdentifier, string actingNpcIdentifier, NetworkObject actorObject)
```

#### Parameters

`graphIdentifier` string

`actingNpcIdentifier` string

`actorObject` NetworkObject

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_RaiseAuthoritative_System_String_System_String_"></a> RaiseAuthoritative\(string, string\)

신호를 권위적으로 올린다. 서버면 즉시 기록, 클라이언트면 서버로 보고한다.
중계기가 없거나 네트워크가 비활성이면 로컬에 기록(단일 플레이어/오프라인 폴백).

```csharp
public static void RaiseAuthoritative(string normalizedSignalId, string parameterJson = null)
```

#### Parameters

`normalizedSignalId` string

`parameterJson` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_RaiseAuthoritativeForPlayer_System_String_System_String_System_String_System_String_FishNet_Connection_NetworkConnection_"></a> RaiseAuthoritativeForPlayer\(string, string, string, string, NetworkConnection\)

서버 명령·시스템이 명시한 플레이어 귀속으로 신호를 기록한다.

```csharp
public static bool RaiseAuthoritativeForPlayer(string normalizedSignalId, string parameterJson, string playerIdentifier, string playerDisplayName, NetworkConnection sender = null)
```

#### Parameters

`normalizedSignalId` string

`parameterJson` string

`playerIdentifier` string

`playerDisplayName` string

`sender` NetworkConnection

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_RequestAdvance_System_String_System_String_"></a> RequestAdvance\(string, string\)

표시 클라이언트가 대화 계속 입력을 서버에 보고한다.

```csharp
public static void RequestAdvance(string graphIdentifier, string nodeIdentifier)
```

#### Parameters

`graphIdentifier` string

`nodeIdentifier` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_RequestChoiceSelection_System_String_System_String_System_Int32_"></a> RequestChoiceSelection\(string, string, int\)

표시 클라이언트가 Choice 선택을 서버에 보고한다.

```csharp
public static void RequestChoiceSelection(string graphIdentifier, string nodeIdentifier, int optionIndex)
```

#### Parameters

`graphIdentifier` string

`nodeIdentifier` string

`optionIndex` int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_RequestLineTopologyChange_System_String_System_String_System_Boolean_System_Int32_"></a> RequestLineTopologyChange\(string, string, bool, int\)

```csharp
public static bool RequestLineTopologyChange(string first, string second, bool connected, int requestId = 0)
```

#### Parameters

`first` string

`second` string

`connected` bool

`requestId` int

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_RevokeClientSignalCapabilities_System_Int32_"></a> RevokeClientSignalCapabilities\(int\)

```csharp
public static void RevokeClientSignalCapabilities(int clientId)
```

#### Parameters

`clientId` int

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_TryStartAuthoritativeScenario_System_String_System_Int32_System_Collections_Generic_IEnumerable_FishNet_Connection_NetworkConnection__"></a> TryStartAuthoritativeScenario\(string, int, IEnumerable<NetworkConnection\>\)

서버에서 그래프를 한 번만 시작하고, 선택된 클라이언트에는 표시 전용 세션을 준비시킨다.
중계기가 없는 레거시 씬에서는 false를 반환하여 호출자가 기존 호환 경로를 선택할 수 있다.

```csharp
public static bool TryStartAuthoritativeScenario(string scenarioIdentifier, int ownerClientId, IEnumerable<NetworkConnection> targets)
```

#### Parameters

`scenarioIdentifier` string

`ownerClientId` int

`targets` IEnumerable<NetworkConnection\>

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_LineTopologyMirrored"></a> LineTopologyMirrored

```csharp
public static event Action<long, string, string, bool> LineTopologyMirrored
```

#### Event Type

 Action<long, string, string, bool\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_LineTopologyRequestCompleted"></a> LineTopologyRequestCompleted

```csharp
public static event Action<int, string, string, bool, bool> LineTopologyRequestCompleted
```

#### Event Type

 Action<int, string, string, bool, bool\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_LineTopologyRequestReceived"></a> LineTopologyRequestReceived

```csharp
public static event Func<string, string, bool, NetworkConnection, bool> LineTopologyRequestReceived
```

#### Event Type

 Func<string, string, bool, NetworkConnection, bool\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_LineTopologySnapshotBegan"></a> LineTopologySnapshotBegan

```csharp
public static event Action<long> LineTopologySnapshotBegan
```

#### Event Type

 Action<long\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_LineTopologySnapshotEnded"></a> LineTopologySnapshotEnded

```csharp
public static event Action<long> LineTopologySnapshotEnded
```

#### Event Type

 Action<long\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioNetworkRelay_LineTopologySnapshotRequested"></a> LineTopologySnapshotRequested

```csharp
public static event Func<IEnumerable<ScenarioNetworkRelay.LineTopologyPair>> LineTopologySnapshotRequested
```

#### Event Type

 Func<IEnumerable<[ScenarioNetworkRelay](MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.md).[LineTopologyPair](MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.LineTopologyPair.md)\>\>

