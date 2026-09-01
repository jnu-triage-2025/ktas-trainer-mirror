# <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals"></a> Class ScenarioInteractionSignals

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 도메인 인터랙션 "완료 신호"를 다루는 얇은 헬퍼.

설계 메모(TODO-SPEC-2):
- 엔진의 Validator 는 RegistryContains 조건 + RegistryType.RuntimeState 로
  "특정 식별자가 등록되어 있는가"를 이미 검사할 수 있다. 따라서 별도의 신규
  ScenarioValidatorCondition/RuleType 없이도 인터랙션 게이팅이 가능하다.
- 이 클래스는 그 위에 의도를 드러내는 얇은 래퍼만 제공한다(신규 저장소 없음).
  게임플레이 코드가 인터랙션 완료 시 Raise(signalId) 를 호출하고,
  시나리오 JSON 은 Validator(RegistryContains, RuntimeState, signalId) 로 검사한다.

변환 규칙(요약): JSON 의 `todo.validate.<cond>` (InvokeEvent 스텁) 은
  Validator 노드(condition=RegistryContains, rule.registryType=RuntimeState,
  rule.registryIdentifier=`sig.<cond>`) 로 치환한다.

```csharp
public static class ScenarioInteractionSignals
```

#### Inheritance

object ← 
[ScenarioInteractionSignals](MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.md)

## Fields

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_Prefix"></a> Prefix

RuntimeState 레지스트리에서 신호 식별자에 적용할 접두사.

```csharp
public const string Prefix = "sig."
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_Clear_System_String_"></a> Clear\(string\)

신호를 내린다. 서버 권한 경로로 라우팅한다(사이클 반복 등에서 재설정 시 사용).

```csharp
public static void Clear(string signalId)
```

#### Parameters

`signalId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_ClearAllInternalSignals"></a> ClearAllInternalSignals\(\)

내부 신호 레지스트리 전체를 제거한다.
시나리오 시작/종료 시점에 호출하여 이전 세션 상태가 섞이지 않도록 한다.

```csharp
public static void ClearAllInternalSignals()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_ClearAllRaisedSignals"></a> ClearAllRaisedSignals\(\)

현재 실행에 속한 일회성 gameplay signal을 모두 제거한다.
RuntimeState는 sticky 저장소이므로 시나리오 재실행 전에 반드시 호출해야 한다.

```csharp
public static void ClearAllRaisedSignals()
```

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_ClearInternal_System_String_System_String_"></a> ClearInternal\(string, string\)

특정 대상/신호의 내부 신호 상태를 제거한다.

```csharp
public static void ClearInternal(string targetId, string signalId)
```

#### Parameters

`targetId` string

`signalId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_IsRaised_System_String_"></a> IsRaised\(string\)

신호가 올라가 있는지 조회한다(Validator 의 RegistryContains 와 동일 기준).

```csharp
public static bool IsRaised(string signalId)
```

#### Parameters

`signalId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_Normalize_System_String_"></a> Normalize\(string\)

신호 식별자를 정규화한다(접두사 보장).

```csharp
public static string Normalize(string signalId)
```

#### Parameters

`signalId` string

#### Returns

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_Raise_System_String_"></a> Raise\(string\)

인터랙션 완료 신호를 올린다.
서버 권한(authoritative) 경로로 라우팅한다: 서버면 직접 RuntimeState 에 기록하고,
클라이언트면 <xref href="MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay" data-throw-if-not-resolved="false"></xref> 를 통해 서버로 보고한다(G-8 P1).
네트워크가 비활성이거나 중계기가 없으면 로컬에 기록(단일 플레이어/오프라인).

```csharp
public static void Raise(string signalId)
```

#### Parameters

`signalId` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_Raise_System_String_System_String_"></a> Raise\(string, string\)

JSON 문자열 파라미터와 함께 인터랙션 완료 신호를 올린다.
네트워크 세션에서는 서버가 JSON 문법을 검증하고 발신 플레이어별 마지막 값을 권위적으로 기록한다.

```csharp
public static void Raise(string signalId, string parameterJson)
```

#### Parameters

`signalId` string

`parameterJson` string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_RegisterInternal_System_String_System_String_System_Action_"></a> RegisterInternal\(string, string, Action\)

서버 내부 신호를 수신 대기 등록한다.
targetId 는 `@m`(서버) 또는 `@s`(self) 같은 서버 내부 목적지 식별자일 수 있다.

```csharp
public static bool RegisterInternal(string targetId, string signalId, Action onResolved)
```

#### Parameters

`targetId` string

`signalId` string

`onResolved` Action

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_ResolveInternal_System_String_System_String_"></a> ResolveInternal\(string, string\)

서버 내부 신호를 resolve 한다.
targetId 는 `@m`(서버) 또는 `@s`(self) 같은 서버 내부 목적지 식별자일 수 있다.

```csharp
public static bool ResolveInternal(string targetId, string signalId)
```

#### Parameters

`targetId` string

`signalId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_OnSignalCleared"></a> OnSignalCleared

신호가 로컬 레지스트리에서 내려갈 때 발생한다(정규화된 식별자 전달).
신호의 누적 상태를 따로 들고 있는 관찰자(시그널 카운터 등)가 내려간 신호를 반영할 수 있게 한다.

```csharp
public static event Action<string> OnSignalCleared
```

#### Event Type

 Action<string\>

### <a id="MultiplayerInfrastructure_Scenario_ScenarioInteractionSignals_OnSignalRegistered"></a> OnSignalRegistered

신호가 로컬 레지스트리에 기록될 때 발생한다(정규화된 식별자 전달).
루브릭 기록 등 신호 관찰자가 게이트 타임아웃 이후의 수행도 추적할 수 있게 한다.

```csharp
public static event Action<string> OnSignalRegistered
```

#### Event Type

 Action<string\>

