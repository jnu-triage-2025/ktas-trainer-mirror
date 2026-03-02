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
| `_scenarioRunner` | (Inspector) | `ScenarioCommandRunner` 참조 |
| `_datapackRuntime` | (Inspector) | `DatapackRuntimeService` 참조 |

---

## 2. 공개 API

### `SendSystemMessage`

```csharp
public void SendSystemMessage(NetworkConnection conn, string message)
```

특정 클라이언트에게 시스템 메시지(`[System]` 노란 태그)를 전송합니다.  
`conn`이 `null`이면 서버 로그로 출력됩니다.

```csharp
chatService.SendSystemMessage(conn, "아이템 지급이 완료되었습니다.");
```

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
| `/give` | `/give <id> [count=1] [player]` | 플레이어에게 아이템 지급 |
| `/clean` | `/clean [id] [count]` | 인벤토리 아이템 제거 |
| `/gamemode` | `/gamemode <player\|spectator>` | 게임모드 전환 |
| `/scenario` | `/scenario <id>` | 시나리오 실행 (ScenarioCommandRunner 필요) |

### `/give` 동작 상세

```
/give (item identifier: 필수) (count: 선택, 기본 1) (player identifier: 선택, 기본 호출자)
```

1. `RegistryType.Item`에서 식별자 확인
2. 대상 플레이어 해석 (`@s`, `fish:<clientId>`, `<clientId>` 지원)
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

> **참고:** `/give`, `/clean`은 sender 컨텍스트가 필요합니다. `TryExecuteSystemCommand`에서 대상 플레이어를 명시하지 않으면 실패합니다.

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

- [multiplayer-infrastructure-guide.md](../multiplayer-infrastructure-guide.md) — 채팅 및 커맨드 시스템 개요
- [api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md](MultiplayerInfrastructure.Command.ChatCommandExtensions.md) — 커맨드 확장 이력
- [api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md](MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md) — 데이터팩 기반 자동 커맨드 실행
