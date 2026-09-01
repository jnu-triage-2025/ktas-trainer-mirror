# <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure"></a> Class NetworkSessionFailure

Namespace: [TriageTrainer.SceneBootstrapper](TriageTrainer.SceneBootstrapper.md)  
Assembly: Assembly\-CSharp.dll  

네트워크 세션 실패의 상태 코드와 상세 정보를 나타냅니다.
상태 코드 범위: 1xxx=정당한 사유, 2xxx=예기치 않은 오류, 9xxx=미분류.

```csharp
public sealed class NetworkSessionFailure
```

#### Inheritance

object ← 
[NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

## Properties

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_Code"></a> Code

실패 상태 코드입니다.

```csharp
public NetworkSessionFailure.StatusCode Code { get; }
```

#### Property Value

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md).[StatusCode](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.StatusCode.md)

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_Detail"></a> Detail

사람이 읽을 수 있는 상세 설명입니다.

```csharp
public string Detail { get; }
```

#### Property Value

 string

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_IsLegitimate"></a> IsLegitimate

정당한 사유로 인한 실패인지 여부입니다. 상태 코드 1xxx 범위가 정당한 사유에 해당합니다.

```csharp
public bool IsLegitimate { get; }
```

#### Property Value

 bool

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_Reason"></a> Reason

기계 판독 가능한 사유 식별자입니다.

```csharp
public string Reason { get; }
```

#### Property Value

 string

## Methods

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_ConnectionError_System_String_"></a> ConnectionError\(string\)

분류할 수 없는 일반 연결 오류를 생성합니다.

```csharp
public static NetworkSessionFailure ConnectionError(string detail)
```

#### Parameters

`detail` string

#### Returns

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_ConnectionLostFailure_System_String_"></a> ConnectionLostFailure\(string\)

연결 손실 실패를 생성합니다.

```csharp
public static NetworkSessionFailure ConnectionLostFailure(string detail)
```

#### Parameters

`detail` string

#### Returns

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_ConnectionRefusedFailure_System_String_"></a> ConnectionRefusedFailure\(string\)

연결 거부 실패를 생성합니다.

```csharp
public static NetworkSessionFailure ConnectionRefusedFailure(string detail)
```

#### Parameters

`detail` string

#### Returns

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_ConnectionTimeoutFailure_System_String_"></a> ConnectionTimeoutFailure\(string\)

연결 타임아웃 실패를 생성합니다.

```csharp
public static NetworkSessionFailure ConnectionTimeoutFailure(string detail)
```

#### Parameters

`detail` string

#### Returns

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_ServerShutdownFailure_System_String_"></a> ServerShutdownFailure\(string\)

서버 정상 종료에 의한 실패를 생성합니다. 정당한 사유로 분류됩니다.

```csharp
public static NetworkSessionFailure ServerShutdownFailure(string detail)
```

#### Parameters

`detail` string

#### Returns

 [NetworkSessionFailure](TriageTrainer.SceneBootstrapper.NetworkSessionFailure.md)

### <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

