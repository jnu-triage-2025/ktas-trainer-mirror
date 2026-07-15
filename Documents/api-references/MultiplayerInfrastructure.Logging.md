# API 레퍼런스: MultiplayerInfrastructure.Logging

> **네임스페이스:** `MultiplayerInfrastructure.Logging`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Logging/`

---

## 0. 개요

Logging 네임스페이스는 인게임 이벤트를 파일로 기록하고 세션을 식별하는 두 개의 정적 서비스로 구성됩니다.

| 클래스 | 역할 |
|---|---|
| `GameSessionService` | 세션 UUID/슬러그 관리 |
| `GameLogService` | 이벤트 로그 수집 및 파일 저장 |
| `GameLogEntry` | 단일 로그 엔트리 데이터 모델 |
| `LogService` | Unity Debug.Log를 로그 서비스로 라우팅 |

---

## 1. GameSessionService

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Logging/GameSessionService.cs
```

현재 게임 세션의 UUID와 시작 시각을 관리합니다.

### 프로퍼티

```csharp
// 세션 UUID (예: "a1b2c3d4-...")
static string SessionId { get; }

// 세션 시작 시각 (로컬 시간)
static DateTime SessionStartTime { get; }

// 로그 파일명 등에 사용하는 슬러그 (예: "20260715-093000-a1b2c3")
static string GetSessionSlug()
```

### 초기화

```csharp
// 세션이 초기화되지 않았으면 새 UUID로 초기화
static void EnsureInitialized()
```

---

## 2. GameLogService

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Logging/GameLogService.cs
```

인게임 이벤트를 파일로 기록합니다. 서버/클라이언트 양측에서 각자 독립적으로 동작합니다.

### 저장 경로

```
{Application.persistentDataPath}/GameLogs/{sessionSlug}.log
```

### 초기화/종료

```csharp
// 로그 서비스를 초기화합니다 (이미 초기화되어 있으면 무시)
static void Initialize(string contextLabel = "unknown")

// 로그 서비스를 종료하고 파일 스트림을 닫습니다
static void Shutdown()

// 초기화 여부
static bool IsInitialized { get; }

// 현재 로그 파일 경로 (초기화 전이면 null)
static string CurrentLogFilePath { get; }

// 로그 루트 폴더 경로
static string LogRootPath { get; }

// 컨텍스트 레이블 (예: "server", "client:abc123")
static string ContextLabel { get; }
static void SetContext(string label)
```

### 쓰기 API

```csharp
// 일반 로그 엔트리 기록
static void Write(GameLogCategory category, string message, string tag = null)

// 카테고리별 편의 메서드
static void WriteSystem(string message, string tag = null)
static void WritePlayerJoin(string message, string playerTag = null)
static void WriteChat(string message, string senderTag = null)
static void WriteCommand(string message, string senderTag = null)
static void WriteScenario(string message, string scenarioTag = null)
static void WriteSignal(string message, string signalTag = null)
static void WriteInteraction(string message, string interactionTag = null)
```

### 조회 API

```csharp
// 현재 세션의 캐시된 엔트리 (최대 4096개)
static IReadOnlyList<GameLogEntry> GetEntries()

// 저장된 세션 로그 파일 목록
static string[] GetLogFiles()

// 로그 파일을 지정 경로로 내보내기
static bool TryExport(string destinationPath, out string error)

// 로그 폴더를 파일 탐색기로 열기 (에디터/빌드, 비배치 모드 전용)
static void OpenLogFolder()

// 버퍼를 파일에 강제 플러시
static void Flush()
```

### 커맨드 연동

| 커맨드 | 설명 |
|---|---|
| `/log folder` | 로그 폴더를 파일 탐색기로 열기 |
| `/log export <path>` | 현재 세션 로그를 지정 경로로 내보내기 |
| `/log list` | 저장된 세션 로그 목록 채팅 출력 |
| `/log flush` | 버퍼 강제 플러시 |

---

## 3. GameLogEntry

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Logging/GameLogEntry.cs
```

단일 로그 엔트리를 나타내는 데이터 모델입니다.

| 프로퍼티 | 타입 | 설명 |
|---|---|---|
| `Timestamp` | `DateTime` | 기록 시각 (로컬 시간) |
| `Context` | `string` | 컨텍스트 레이블 (예: "server", "client") |
| `Category` | `GameLogCategory` | 로그 카테고리 |
| `Message` | `string` | 로그 메시지 |
| `Tag` | `string` | 추가 태그 (nullable) |

### GameLogCategory 열거형

| 값 | 설명 |
|---|---|
| `System` | 시스템 이벤트 |
| `PlayerJoin` | 플레이어 접속/퇴장 |
| `Chat` | 채팅 메시지 |
| `Command` | 커맨드 실행 |
| `ScenarioGraph` | 시나리오 그래프 실행 |
| `ScenarioSignal` | 시나리오 신호 |
| `Interaction` | 인터랙션 이벤트 |

---

## 4. 사용 예시

```csharp
// 서버 시작 시 초기화
GameLogService.Initialize("server");

// 이벤트 기록
GameLogService.WriteScenario($"시나리오 시작: {scenarioName}", "patient_a");
GameLogService.WriteInteraction($"IV 라인 삽입: {playerName}", "iv_line");

// 세션 종료 시
GameLogService.Shutdown();
```

---

## 5. 관련 문서

- [MultiplayerInfrastructure.Permission.md](./MultiplayerInfrastructure.Permission.md) — 권한 시스템 (log 커맨드 권한)
- [Commands.md](../guide/Commands.md) — /log 커맨드 사용법
