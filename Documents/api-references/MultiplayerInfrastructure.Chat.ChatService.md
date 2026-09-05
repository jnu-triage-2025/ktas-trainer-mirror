# API 레퍼런스: `MultiplayerInfrastructure.Chat.ChatService`

> **네임스페이스:** `MultiplayerInfrastructure.Chat`  
> **기반 클래스:** `FishNet.Object.NetworkBehaviour`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs`

---

## 0. 문서 목적

`ChatService`는 게임 내 채팅 메시지 전송·수신과 `/커맨드` 처리를 담당합니다.  
커맨드 실행, 시스템 메시지 발송, 레이트 리밋 등 모든 채팅 관련 서버 로직이 이 클래스에 집중됩니다.

---

## 1. 직렬화 필드

| 필드 | 기본값 | 설명 |
|---|---|---|
| `_messageCooldownSeconds` | `DefaultsChatControl.MessageCooldownSeconds` | 채팅 쿨다운(초) |
| `_uiController` | (Inspector) | `ChatUIController` 참조 |
| `_commandService` | (Inspector) | `ChatCommandService` 참조 |
| `_datapackRuntime` | (Inspector) | `DatapackRuntimeService` 참조 |

---

## 2. 공개 API

### `SendSystemMessage`

```csharp
public void SendSystemMessage(NetworkConnection conn, string message)
```

플레이어가 실행한 명령의 결과를 실행자 이름 접두어(`(이름) 메시지`)와 함께 모든 접속자의 채팅창에 전파합니다.  
`conn`이 `null`이거나 시스템 권한 실행(`TryExecuteSystemCommand`) 중이면 채팅에 전파하지 않고 서버 로그로만 출력합니다.  
오류, 사용법, 조회 결과처럼 실행자에게만 의미 있는 출력에 사용합니다.

```csharp
chatService.SendSystemMessage(conn, "Usage: /give <item> [count] [target]");
```

---

### `SendSystemNotification`

```csharp
public void SendSystemNotification(NetworkConnection actor, string message)
```

플레이어에게 알리는 성격의 시스템 메시지를 실행 컨텍스트와 무관하게 서버 전역으로 전파합니다.  
플레이어가 실행한 경우 `(이름) 메시지` 형식으로, 시스템 권한 실행이나 서버 콘솔처럼 `actor`가 없는 경우 `[System]` 노란 태그로 모든 접속자에게 표시됩니다.  
명령이 대상 플레이어나 세션 상태를 바꿨음을 알리는 "~에게 ~했습니다" 류의 결과 보고(`/give`, `/tp`, `/clean`, `/speed`, `/gamemode`, `/tag`, `/server kick|ban|unban|stop`, `/entitypreset` 스폰)에 사용합니다.

```csharp
chatService.SendSystemNotification(sender, $"Gave {count}x '{itemIdentifier}' to {targetName}.");
```

메시지에서 대상 플레이어를 지칭할 때는 `PlayerTargetResolver.DescribeTarget`으로 표기합니다. 표시 이름을 기본으로 하되, 명령이 FishNet 연결 번호로 대상을 지정했으면 `이름(clientId)`, 사용자 식별자로 지정했으면 `이름(uuid)` 형식이 됩니다.

---

### `TryExecuteSystemCommand`

```csharp
public bool TryExecuteSystemCommand(string commandLine, out string result)
```

서버 코드에서 직접 커맨드를 실행합니다. 플레이어 입력 없이 프리픽스(`/`) 없이 커맨드 라인을 전달합니다.

- `sender`가 `null`이므로 플레이어 컨텍스트가 필요한 일부 커맨드는 실패할 수 있습니다.
- 성공 시 `true`, 알 수 없는 커맨드나 오류 시 `false`를 반환합니다.

```csharp
chatService.TryExecuteSystemCommand("give bandage 5 fish:2", out string result);
chatService.TryExecuteSystemCommand("scenario hospital_emergency", out _);
```

---

### `TryDispatchProblemSheet`

```csharp
public bool TryDispatchProblemSheet(
    string problemSetIdentifier,
    IEnumerable<NetworkConnection> targets,
    int startIndex,
    bool singleProblemMode,
    out string error)
```

문제 세트를 대상 클라이언트에 연다.

- `startIndex`: 시작 문제 인덱스(0-based)
- `singleProblemMode`: `true`면 지정 문제만 표시, `false`면 전체 세트 진행 모드

### `ReportProblemGrade`

```csharp
public void ReportProblemGrade(string problemSetIdentifier, int problemIndex, int gradeCode)
```

클라이언트의 문제 판정 결과를 서버에 보고한다.

- `gradeCode = 0`: 정답
- `gradeCode = 1`: 오답
- 정답(0)일 때만 `onCorrect.scoreboard` 보상이 서버에서 적용된다.

### `GetLastProblemSheetGradeCode`

```csharp
public int GetLastProblemSheetGradeCode(NetworkConnection sender)
```

해당 연결의 마지막 문제 판정 코드를 반환한다. 커맨드 파이프라이닝에서 `problemsheet` 결과값(`0|1`) 생성에 사용된다.

---

### `TryDispatchTitle`

```csharp
public bool TryDispatchTitle(
    IEnumerable<NetworkConnection> targets,
    string title,
    string subtitle,
    out string error)
```

대상 클라이언트에 타이틀/서브타이틀을 표시합니다.

---

### `TryDispatchSubtitle`

```csharp
public bool TryDispatchSubtitle(IEnumerable<NetworkConnection> targets, string subtitle, out string error)
```

현재 표시 중인 타이틀이 있으면 서브타이틀을 갱신합니다.

---

### `TryDispatchActionbar`

```csharp
public bool TryDispatchActionbar(IEnumerable<NetworkConnection> targets, string actionbar, out string error)
```

액션바 텍스트를 표시합니다.

---

### `TryDispatchTitleClear`

```csharp
public bool TryDispatchTitleClear(IEnumerable<NetworkConnection> targets, out string error)
```

타이틀/서브타이틀/액션바를 제거합니다.

---

### `TryDispatchTitleReset`

```csharp
public bool TryDispatchTitleReset(IEnumerable<NetworkConnection> targets, out string error)
```

타이틀 타이밍을 기본값으로 복구하고 서브타이틀을 초기화합니다.

---

### `TryDispatchTitleTimes`

```csharp
public bool TryDispatchTitleTimes(
    IEnumerable<NetworkConnection> targets,
    int fadeInTicks,
    int stayTicks,
    int fadeOutTicks,
    out string error)
```

타이틀 페이드 타이밍(틱)을 대상 클라이언트에 설정합니다.

---

### `BroadcastSystemMessage`

```csharp
public void BroadcastSystemMessage(string message)
```

모든 클라이언트에게 시스템 메시지를 브로드캐스트합니다 (`[System]` 노란 태그).

```csharp
chatService.BroadcastSystemMessage("시나리오가 종료되었습니다.");
```

---

### `GetDisplayName`

```csharp
public string GetDisplayName(NetworkConnection conn) => conn?.ClientId.ToString() ?? "Server";
```

채팅 표시명을 반환합니다. 커스텀 닉네임 시스템이 없으면 클라이언트 ID를 반환합니다.

---

## 3. 클라이언트-서버 흐름

```
[클라이언트] 텍스트 입력 → ChatUIController.OnSubmitted
    ↓
HandleLocalSubmission(raw)
    ├── /로 시작 → ExecuteCommandServerRpc(commandLine)
    └── 일반 텍스트 → SendChatServerRpc(rawMessage)

[서버] SendChatServerRpc
    ├── 쿨다운 체크 → 실패 시 발신자에게 경고 메시지
    └── 성공 시 ReceiveChatObserversRpc(formattedLine) → 모든 클라이언트에 전달

[서버] ExecuteCommandServerRpc
    └── TryExecuteCommandInternal → ChatCommandService.TryExecute
```

---

## 4. 레이트 리밋

`_messageCooldownSeconds` 값으로 클라이언트 당 최소 전송 간격을 제한합니다.  
쿨다운 중에는 남은 시간을 알리는 경고 메시지가 발신자에게 전달됩니다.

---

## 5. 커맨드 목록

`ChatCommandService`가 `Awake`에서 초기화되면서 아래 커맨드를 등록합니다.

| 커맨드 | 구문 | 설명 |
|---|---|---|
| `/help` | `/help [command]` | 커맨드 목록 또는 상세 설명 |
| `/give` | `/give <id> [count=1] [target]` | 대상에게 아이템 지급 |
| `/clean` | `/clean [id] [count]` | 인벤토리 아이템 제거 |
| `/gamemode` | `/gamemode <player\|spectator>` | 게임모드 전환 |
| `/scenario` | `/scenario execute <target> <scenario_id>` | 대상에게 시나리오 실행 |
| `/problemsheet` | `/problemsheet list \| /problemsheet <target> <problem-identifier> [problem-index]` | 문제 시트 실행/조회 |
| `/title` | `/title <target> ...` | 타이틀/액션바 표시 |

### `/give` 동작 상세

```
/give (item identifier: 필수) (count: 선택, 기본 1) (target identifier: 선택, 기본 호출자)
```

1. `RegistryType.Item`에서 식별자 확인
2. 대상 해석 (`@s`, `fish:<clientId>`, `<clientId>` 지원, 선택자 파싱 지원)
3. 아이템 템플릿에서 데이터 복제 후 count 설정
4. `TryAddItemToInventory()` — 인벤토리 여유 공간에 추가
5. 남은 수량(`leftover`)이 있으면 플레이어 앞에 월드 드롭

### `/clean` 동작 상세

```
/clean [item identifier] [count]
```

| 인수 | 동작 |
|---|---|
| 없음 | 인벤토리 전체 비우기 |
| id만 지정 | 해당 아이템 전부 제거 |
| id + count | `min(보유 수량, count)` 제거 |
| count만 지정 | 오류 반환 |

> **참고:** `/give`, `/clean`은 sender 컨텍스트가 필요합니다. `TryExecuteSystemCommand`에서 대상 타겟을 명시하지 않으면 실패합니다.

---

## 6. 커스텀 커맨드 추가

```csharp
// 1. IChatCommandModel 구현
public class MyCommand : IChatCommandModel
{
    private readonly ChatService _chatService;

    public MyCommand(ChatService chatService)
    {
        _chatService = chatService;
    }

    public string CommandEntry => "mycommand";
    public string Description => "내 커맨드 설명";
    public bool RequiresAdmin => false;

    public void Execute(NetworkConnection sender, string[] args)
    {
        _chatService.SendSystemMessage(sender, "실행 완료!");
    }
}

// 2. ChatCommandService.Initialize() 내부에서 등록
// CommandService.cs 파일에 추가
RegisterCommand(new MyCommand(chatService));
```

> **참고:** 커스텀 커맨드는 `CommandService.cs`의 `Initialize()` 메서드에 직접 추가합니다.  
> `TriageTrainer` 측에서 외부 주입이 필요하다면 `CommandService`에 별도 등록 메서드를 추가하세요.

---

## 관련 문서

- [multiplayer-infrastructure-overview.md](architecture/multiplayer-infrastructure-overview.md) — 채팅 및 커맨드 시스템 개요
- [api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md](MultiplayerInfrastructure.Command.ChatCommandExtensions.md) — 커맨드 확장 이력
- [api-references/MultiplayerInfrastructure.Command.TargetSelectorResolver.md](MultiplayerInfrastructure.Command.TargetSelectorResolver.md) — 대상 선택자 파서
- [api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md](MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md) — 데이터팩 기반 자동 커맨드 실행
