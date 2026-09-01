# <a id="MultiplayerInfrastructure_Logging_GameSessionService"></a> Class GameSessionService

Namespace: [MultiplayerInfrastructure.Logging](MultiplayerInfrastructure.Logging.md)  
Assembly: Assembly\-CSharp.dll  

게임 세션 식별자(session-uuid)를 관리하는 정적 서비스.

서버/클라이언트 모두에서 독립적으로 세션 UUID를 가진다.
- 서버  : 서버 프로세스 시작 시 자동으로 UUID를 생성한다.
- 클라이언트: 서버에서 세션 ID를 부여받거나, 독립적으로 로컬 세션 ID를 생성한다.

로그 파일 이름 형식: session-{uuid} / {localtime}

```csharp
public static class GameSessionService
```

#### Inheritance

object ← 
[GameSessionService](MultiplayerInfrastructure.Logging.GameSessionService.md)

## Properties

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_IsInitialized"></a> IsInitialized

세션이 초기화되었는지 여부.

```csharp
public static bool IsInitialized { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_SessionId"></a> SessionId

현재 세션의 UUID 문자열.
아직 초기화되지 않았다면 자동으로 새 UUID를 생성한다.

```csharp
public static string SessionId { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_SessionStartTime"></a> SessionStartTime

세션 UUID 생성 시각 (로컬 시간).

```csharp
public static DateTime SessionStartTime { get; }
```

#### Property Value

 DateTime

## Methods

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_EnsureInitialized"></a> EnsureInitialized\(\)

세션을 초기화한다. 이미 초기화된 경우 무시한다.
서버/클라이언트 시작 시점에 호출하는 것을 권장한다.

```csharp
public static void EnsureInitialized()
```

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_GetSessionSlug"></a> GetSessionSlug\(\)

로그 파일 이름에 사용하기 위한 세션 슬러그를 반환한다.
형식: session-{yyyyMMdd_HHmmss}-{uuid앞8자}
날짜·시간이 앞에 위치하므로 파일 탐색기에서 이름순 정렬 시 시간 순서대로 나열된다.

```csharp
public static string GetSessionSlug()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_Reset"></a> Reset\(\)

세션을 명시적으로 재설정한다 (재접속, 세션 재시작 등).
이 메서드 호출 후 새 UUID가 생성된다.

```csharp
public static void Reset()
```

### <a id="MultiplayerInfrastructure_Logging_GameSessionService_SetSessionId_System_String_"></a> SetSessionId\(string\)

세션 ID를 외부에서 주입한다 (클라이언트가 서버 세션 ID를 받을 때 사용).
영숫자, 하이픈(-), 밑줄(_)만 허용한다. 파일 경로 탈출 방지.

```csharp
public static void SetSessionId(string sessionId)
```

#### Parameters

`sessionId` string

