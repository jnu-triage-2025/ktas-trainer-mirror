# <a id="MultiplayerInfrastructure_Logging_GameLogService"></a> Class GameLogService

Namespace: [MultiplayerInfrastructure.Logging](MultiplayerInfrastructure.Logging.md)  
Assembly: Assembly\-CSharp.dll  

인게임 로그를 수집하고 애플리케이션 데이터 폴더에 저장하는 정적 서비스.

## 동작 원칙
- 서버/클라이언트 양측에서 각자 독립적으로 동작한다.
- 각 세션(session-uuid)별로 별도 로그 파일/폴더를 생성한다.
- 메모리에 최근 엔트리를 캐시(순환 버퍼 방식)하며, 주기적으로 파일에 플러시한다.
- 로컬 시간 기준으로 타임스탬프를 기록한다.

## 저장 경로
  {Application.persistentDataPath}/GameLogs/{sessionSlug}.log

## 커맨드
  /log folder  — 로그 폴더를 파일 탐색기로 연다 (에디터/빌드, 비배치 모드 전용)
  /log export  — 현재 세션 로그를 사용자 지정 경로로 복사/내보내기
  /log list    — 저장된 세션 로그 목록을 채팅에 출력
  /log flush   — 버퍼를 강제 플러시

```csharp
public static class GameLogService
```

#### Inheritance

object ← 
[GameLogService](MultiplayerInfrastructure.Logging.GameLogService.md)

## Fields

### <a id="MultiplayerInfrastructure_Logging_GameLogService_DatapackSubfolder"></a> DatapackSubfolder

```csharp
public const string DatapackSubfolder = "DataPacks"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_LogSubfolder"></a> LogSubfolder

로그 파일을 저장하는 하위 디렉터리 이름.

```csharp
public const string LogSubfolder = "GameLogs"
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_Logging_GameLogService_ContextLabel"></a> ContextLabel

이 인스턴스의 컨텍스트 레이블 (예: "server", "client", "client:abc123").
<xref href="MultiplayerInfrastructure.Logging.GameLogService.Initialize(System.String)" data-throw-if-not-resolved="false"></xref> 호출 시 지정하거나 <xref href="MultiplayerInfrastructure.Logging.GameLogService.SetContext(System.String)" data-throw-if-not-resolved="false"></xref> 로 변경한다.

```csharp
public static string ContextLabel { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_CurrentLogFilePath"></a> CurrentLogFilePath

현재 세션의 로그 파일 전체 경로. 초기화 전이면 null.

```csharp
public static string CurrentLogFilePath { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_DatapackRootPath"></a> DatapackRootPath

```csharp
public static string DatapackRootPath { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_IsInitialized"></a> IsInitialized

로그가 초기화되어 있는지.

```csharp
public static bool IsInitialized { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Logging_GameLogService_LogRootPath"></a> LogRootPath

로그 루트 폴더 경로.

```csharp
public static string LogRootPath { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Logging_GameLogService_ClearAllSessionLogsAsync"></a> ClearAllSessionLogsAsync\(\)

로컬에 저장된 모든 세션 로그를 제거한다. 현재 기록 중인 로그는 먼저 닫은 뒤 제거하고,
제거 후에는 동일한 컨텍스트로 새 로그를 다시 시작한다.
macOS에서는 휴지통 이동을 우선 시도하며, 지원하지 않는 플랫폼 또는 실패 시 영구 삭제한다.

```csharp
public static Task<GameLogService.SessionLogClearResult> ClearAllSessionLogsAsync()
```

#### Returns

 Task<[GameLogService](MultiplayerInfrastructure.Logging.GameLogService.md).[SessionLogClearResult](MultiplayerInfrastructure.Logging.GameLogService.SessionLogClearResult.md)\>

### <a id="MultiplayerInfrastructure_Logging_GameLogService_Flush"></a> Flush\(\)

버퍼를 강제 플러시한다.

```csharp
public static void Flush()
```

### <a id="MultiplayerInfrastructure_Logging_GameLogService_GetEntries"></a> GetEntries\(\)

현재 세션의 캐시된 엔트리를 반환한다 (최대 MaxCachedEntries 개, 오래된 것부터 순환 제거됨).

```csharp
public static IReadOnlyList<GameLogEntry> GetEntries()
```

#### Returns

 IReadOnlyList<[GameLogEntry](MultiplayerInfrastructure.Logging.GameLogEntry.md)\>

### <a id="MultiplayerInfrastructure_Logging_GameLogService_GetLogFiles"></a> GetLogFiles\(\)

로그 루트 폴더에 저장된 세션 로그 파일 목록을 반환한다.

```csharp
public static string[] GetLogFiles()
```

#### Returns

 string\[\]

### <a id="MultiplayerInfrastructure_Logging_GameLogService_Initialize_System_String_"></a> Initialize\(string\)

로그 서비스를 초기화한다.
이미 초기화된 경우 무시한다.

```csharp
public static void Initialize(string contextLabel = "unknown")
```

#### Parameters

`contextLabel` string

이 피어의 컨텍스트 문자열 (예: "server", "client")

### <a id="MultiplayerInfrastructure_Logging_GameLogService_OpenDatapackFolder"></a> OpenDatapackFolder\(\)

데이터팩이 저장되는 DataPacks 폴더를 연다.

```csharp
public static void OpenDatapackFolder()
```

### <a id="MultiplayerInfrastructure_Logging_GameLogService_OpenLogFolder"></a> OpenLogFolder\(\)

로그 루트 폴더를 파일 탐색기(OS 네이티브)로 연다.
에디터와 빌드에서만 동작한다. 배치(헤드리스) 모드에서는 경로만 출력한다.

```csharp
public static void OpenLogFolder()
```

### <a id="MultiplayerInfrastructure_Logging_GameLogService_SetContext_System_String_"></a> SetContext\(string\)

컨텍스트 레이블을 변경한다.

```csharp
public static void SetContext(string label)
```

#### Parameters

`label` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_Shutdown"></a> Shutdown\(\)

로그 서비스를 종료하고 파일 스트림을 닫는다.
애플리케이션 종료 전 또는 세션 종료 시 호출한다.

```csharp
public static void Shutdown()
```

### <a id="MultiplayerInfrastructure_Logging_GameLogService_TryExport_System_String_System_String__"></a> TryExport\(string, out string\)

현재 세션의 로그를 지정 경로의 텍스트 파일로 내보낸다.
destinationPath는 persistentDataPath 하위이거나 절대 경로여야 하며,
경로 탈출(../)을 허용하지 않는다.

```csharp
public static bool TryExport(string destinationPath, out string error)
```

#### Parameters

`destinationPath` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Logging_GameLogService_Write_MultiplayerInfrastructure_Logging_GameLogCategory_System_String_System_String_"></a> Write\(GameLogCategory, string, string\)

로그 엔트리를 기록한다.

```csharp
public static void Write(GameLogCategory category, string message, string tag = null)
```

#### Parameters

`category` [GameLogCategory](MultiplayerInfrastructure.Logging.GameLogCategory.md)

로그 카테고리

`message` string

로그 메시지

`tag` string

추가 태그 (선택)

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WriteChat_System_String_System_String_"></a> WriteChat\(string, string\)

채팅 메시지를 기록한다.

```csharp
public static void WriteChat(string message, string senderTag = null)
```

#### Parameters

`message` string

`senderTag` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WriteCommand_System_String_System_String_"></a> WriteCommand\(string, string\)

커맨드 실행을 기록한다.

```csharp
public static void WriteCommand(string message, string senderTag = null)
```

#### Parameters

`message` string

`senderTag` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WriteInteraction_System_String_System_String_"></a> WriteInteraction\(string, string\)

인터랙션을 기록한다.

```csharp
public static void WriteInteraction(string message, string interactionTag = null)
```

#### Parameters

`message` string

`interactionTag` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WritePlayerJoin_System_String_System_String_"></a> WritePlayerJoin\(string, string\)

플레이어 접속/퇴장을 기록한다.

```csharp
public static void WritePlayerJoin(string message, string playerTag = null)
```

#### Parameters

`message` string

`playerTag` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WriteScenario_System_String_System_String_"></a> WriteScenario\(string, string\)

시나리오 그래프 실행을 기록한다.

```csharp
public static void WriteScenario(string message, string scenarioTag = null)
```

#### Parameters

`message` string

`scenarioTag` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WriteSignal_System_String_System_String_"></a> WriteSignal\(string, string\)

시나리오 신호를 기록한다.

```csharp
public static void WriteSignal(string message, string signalTag = null)
```

#### Parameters

`message` string

`signalTag` string

### <a id="MultiplayerInfrastructure_Logging_GameLogService_WriteSystem_System_String_System_String_"></a> WriteSystem\(string, string\)

시스템 이벤트를 기록한다.

```csharp
public static void WriteSystem(string message, string tag = null)
```

#### Parameters

`message` string

`tag` string

