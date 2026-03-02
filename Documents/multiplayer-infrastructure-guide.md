# MultiplayerInfrastructure 사용자 가이드

`MultiplayerInfrastructure` 모듈은 이 시뮬레이터의 모든 핵심 백엔드 시스템을 제공합니다.  
이 가이드는 각 시스템의 역할과 연결 구조를 설명하며, 콘텐츠 개발자가 시스템을 올바르게 활용할 수 있도록 돕습니다.

> **원칙:** 이 모듈은 직접 수정하지 않습니다.  
> 콘텐츠 구현은 `Assets/Modules/TriageTrainer/` 아래에서만 수행하세요.  
> 기반 시스템 변경이 필요하면 `Agents/Proposals/`에 제안서를 작성하세요.

---

## 목차

1. [시스템 전체 구조](#1-시스템-전체-구조)
2. [Registry (중앙 레지스트리)](#2-registry-중앙-레지스트리)
3. [PlayerController (플레이어 시스템)](#3-playercontroller-플레이어-시스템)
4. [시나리오 시스템](#4-시나리오-시스템)
5. [인터랙터블 시스템](#5-인터랙터블-시스템)
6. [아이템 시스템](#6-아이템-시스템)
7. [퀘스트 시스템](#7-퀘스트-시스템)
8. [채팅 및 커맨드 시스템](#8-채팅-및-커맨드-시스템)
9. [웨이포인트 앵커](#9-웨이포인트-앵커)
10. [데이터팩 시스템](#10-데이터팩-시스템)
11. [시스템 간 연결 흐름 예시](#11-시스템-간-연결-흐름-예시)

---

## 1. 시스템 전체 구조

```
┌────────────────────────────────────────────────────┐
│              MultiplayerInfrastructure              │
│                                                    │
│  Registry ←──── 모든 시스템이 공유하는 중앙 저장소     │
│      ↑                                             │
│  Item / NPC / Waypoint / UI / Entity / ...         │
│                                                    │
│  PlayerController ← Input → Inventory / Hotbar     │
│        ↓                                           │
│  InteractableEntity (감지 → 상호작용)                │
│        ↓                                           │
│  ScenarioController ← ScenarioGraph (JSON)         │
│        ↓                     ↓                     │
│  QuestManager       ScenarioEventIdentifierRegistry│
│        ↓                                           │
│  WaypointAnchor (하이라이트)                         │
│                                                    │
│  ChatService ← ChatCommandService → 커맨드 실행     │
│  DatapackRuntimeService (JSON 기반 자동화)           │
└────────────────────────────────────────────────────┘
```

---

## 2. Registry (중앙 레지스트리)

모든 시스템 오브젝트는 `Registry`를 통해 등록·조회됩니다.  
싱글톤이나 직접 참조 대신 Registry를 사용하면 느슨한 결합을 유지할 수 있습니다.

### 주요 RegistryType

| 타입 | 저장되는 것 |
|---|---|
| `RegistryType.Item` | `Item` 컴포넌트 (프리팹 템플릿) |
| `RegistryType.ScenarioGraph` | `ScenarioGraph` 또는 `TextAsset` (JSON) |
| `RegistryType.Npc` | NPC `GameObject` |
| `RegistryType.Waypoint` | `Vector3` (웨이포인트 위치) |
| `RegistryType.Entity` | `PlayerController`, `QuestManager` 등 |
| `RegistryType.InteractableEntity` | 위치·인터랙터블 엔티티 |
| `RegistryType.UI` | `UIControllerABC` 하위 컨트롤러들 |
| `RegistryType.IconSprite` | `Sprite` (아이콘) |

### 등록 방법 선택 기준

레지스트리 등록에는 두 가지 방법이 있으며, **등록 대상의 성격에 따라 방법을 선택합니다.**

| 대상 성격 | 권장 방법 |
|---|---|
| 프리팹 템플릿, 에셋, 씬 독립적 데이터 | **RegistryPreloaderController (기본)** |
| 런타임에 동적으로 스폰·소멸되는 오브젝트 | 자기 등록 (`Awake`/`OnStartClient` 등) |

### RegistryPreloaderController를 통한 등록 (기본)

씬 하이어라키에 `RegistryPreloaderController` 게임 오브젝트를 배치하고, 각 RegistryType에 대응하는 ScriptableObject를 생성해 항목을 등록합니다. 씬에 종속되지 않으며 인스펙터만으로 관리할 수 있어 **에셋/프리팹 템플릿의 기본 등록 방법입니다.**

```
1. RegistryPreloaderController 게임 오브젝트를 씬에 배치
2. Assets/Create/MultiplayerInfrastructure/ 에서 원하는 RegistryPreload*SO 생성
3. SO에 등록할 항목 채우기
4. 컨트롤러 인스펙터에서 SO 연결
```

지원하는 RegistryType 및 SO 목록:

| ScriptableObject | 등록 대상 |
|---|---|
| `RegistryPreloadItemSO` | `RegistryType.Item` + `IconSprite` |
| `RegistryPreloadScenarioGraphSO` | `RegistryType.ScenarioGraph` |
| `RegistryPreloadIconSpriteSO` | `RegistryType.IconSprite` |
| `RegistryPreloadNpcSO` | `RegistryType.Npc` |
| `RegistryPreloadWaypointSO` | `RegistryType.Waypoint` |
| `RegistryPreloadEntitySO` | `RegistryType.Entity` |
| `RegistryPreloadInteractableEntitySO` | `RegistryType.InteractableEntity` |
| `RegistryPreloadUIControllerSO` | `RegistryType.UI` |

### 자기 등록 (런타임 예외)

네트워크 스폰 플레이어나 씬에 배치된 월드 오브젝트처럼 런타임에 동적으로 생성·소멸되는 오브젝트는 자신의 Lifecycle에서 직접 등록합니다. 이때 `OnDestroy()`에서 반드시 `Unregister`를 쌍으로 호출해야 합니다.

```csharp
// 등록 (Awake / OnStartClient)
Registry.Register(RegistryType.Entity, Registry.TypeKey<QuestManager>(), this);

// 해제 (OnDestroy)
Registry.Unregister(RegistryType.Entity, Registry.TypeKey<QuestManager>());
```

### 조회

```csharp
// 일반 조회
var questManager = Registry.Get<QuestManager>(RegistryType.Entity, Registry.TypeKey<QuestManager>());

// 안전한 조회 (실패 시 false)
if (Registry.TryGet<QuestManager>(RegistryType.Entity, Registry.TypeKey<QuestManager>(), out var mgr))
{ ... }

// 존재 여부 확인
bool exists = Registry.Contains(RegistryType.Item, "scalpel");
```

### TypeKey 패턴

타입별로 고유 키가 필요할 때는 `Registry.TypeKey<T>()`를 사용합니다.

```csharp
string key = Registry.TypeKey<ChatUIController>();   // "MultiplayerInfrastructure.UI.ChatUIController"
```

### ScenarioGraph 지연 로딩

`RegistryType.ScenarioGraph`에 `TextAsset`이 등록되면, `Get<ScenarioGraph>()`를 호출할 때 자동으로 JSON을 파싱하고 파싱 결과로 캐싱됩니다.

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Registry.md](api-references/MultiplayerInfrastructure.Registry.md)

---

## 3. PlayerController (플레이어 시스템)

`PlayerController`는 FishNet `NetworkBehaviour`를 상속하며, 파일 분리(partial class) 구조로 관리됩니다.

### 구성 파일

| 파일 | 담당 기능 |
|---|---|
| `PlayerController.cs` | Awake/Start/Update 라이프사이클 |
| `PlayerController.Movement.cs` | 이동, 점프, 스펙테이터 이동, 마우스 시점 |
| `PlayerController.Camera.cs` | 카메라 타겟 설정, 시점 전환 |
| `PlayerController.Input.cs` | 모든 키 입력 처리 |
| `PlayerController.Gamemode.cs` | 플레이어/스펙테이터 모드 전환 |
| `PlayerController.Visibility.cs` | 모드별 렌더러 투명도 처리 |
| `PlayerController.Inventory.cs` | 인벤토리 슬롯 관리 |
| `PlayerController.Hotbar.cs` | 핫바 선택·입력 처리 |
| `PlayerController.Item.cs` | 아이템 핸들링, 공격/사용, 뷰모델 |
| `PlayerController.Interactables.cs` | 주변 인터랙터블 감지 및 상호작용 |
| `PlayerController.Dialogue.cs` | 다이얼로그 UI 연결 |
| `PlayerController.Quest.cs` | 퀘스트 UI 연결 |
| `PlayerController.Chat.cs` | (빈 파일; 채팅 입력은 Input에 통합) |
| `PlayerController.EscapeMenu.cs` | ESC 메뉴 연결 |
| `PlayerController.Network.cs` | 네트워크 초기화, Registry 등록 |
| `PlayerController.ReposableCarry.cs` | 이동 가능 오브젝트 들기/내려놓기 |

### 게임모드

```csharp
// 서버에서만 호출
PlayerGamemodeService.TrySetGamemode(conn, controller, PlayerGamemode.Spectator, out string error);

// 플레이어가 스펙테이터인지 확인
bool isSpectator = controller.IsSpectator;
```

### 인벤토리 조작 (서버/커맨드 용도)

```csharp
player.TryAddItemToInventory(itemData, out ItemData leftover);
player.RemoveItemFromInventory("bandage", 3);
player.RemoveAllOfItemFromInventory("bandage");
player.ClearInventory();
int count = player.CountItemInInventory("bandage");
player.TryDropItemInFront(itemData);
```

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Player.PlayerController.md](api-references/MultiplayerInfrastructure.Player.PlayerController.md)

---

## 4. 시나리오 시스템

시나리오는 JSON으로 작성된 노드 그래프 형태로 정의됩니다.

### 흐름 요약

```
ScenarioRegistry (씬 컴포넌트)
    → TextAsset (JSON 파일) → Registry.ScenarioGraph 등록
    → ScenarioCommandRunner (서버 → 클라이언트로 전송)
    → ScenarioController.StartScenario(graph)
    → 노드 순차 실행 (ExecuteNode → Advance)
```

### ScenarioController

씬에 하나만 배치됩니다. 시나리오 시작/종료/진행을 담당합니다.

```csharp
// 식별자로 시작 (ScenarioRegistry를 통해)
scenarioRegistry.TryGetScenarioGraph("patient_a_critical", out var graph, out _);
ScenarioController.Instance.StartScenario(graph);

// 수동 진행 (다이얼로그에서 다음 버튼)
ScenarioController.Instance.Advance();

// 선택지 선택
ScenarioController.Instance.SelectOption(0);

// 상태 확인
bool running = ScenarioController.Instance.IsActive;
```

### 노드 종류

| 노드 | 기능 |
|---|---|
| `Dialogue` | 화자 이름+내용 표시, 수동 진행 대기 |
| `Choice` | 복수 선택지 표시 |
| `Quiz` | 정답/오답 분기 |
| `InvokeEvent` | `ScenarioEventIdentifierRegistry`에 등록된 핸들러 실행 |
| `QuestControl` | 퀘스트 추가/갱신/제거 |
| `QuestWaypointHighlight` | 웨이포인트 강조 표시 |
| `Delay` | 일정 시간 대기 후 진행 |
| `Notification` | 알림 메시지 표시 |
| `PlayerMove` | (구현 예정) 플레이어 이동 |
| `NPCMove` | (구현 예정) NPC 이동 |
| `CameraTarget` | (구현 예정) 카메라 타겟 전환 |
| `Parallel` | 복수 브랜치를 동시 또는 순차 실행 |
| `Validator` | 플레이어 수 등 조건 검사 |
| `StateUpdate` | 내부 상태 변수 갱신 |
| `RoleAssignment` | 역할 자동/수동 배정 |
| `CombineItem` | (구현 예정) 아이템 합성 |
| `Sound` | (구현 예정) 사운드 재생 |

### ScenarioEventIdentifierRegistry (이벤트 등록)

구체 구현을 `InvokeEvent` 노드와 연결하는 다리 역할입니다.  
`TriageTrainer` 모듈에서 커스텀 이벤트 핸들러를 등록합니다.

```csharp
// 등록 (MonoBehaviour.Start에서 호출 권장)
ScenarioEventIdentifierRegistry.Register("move_patient_a_to_treatment", () =>
{
    return MyCoroutineMethod();
});

// 해제 (OnDestroy)
ScenarioEventIdentifierRegistry.Unregister("move_patient_a_to_treatment");
```

> **API 레퍼런스:**  
> - [api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md](api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)  
> - [api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md](api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)  
> - [scenario-authoring.md](scenario-authoring.md)  
> - [scenario-graph.md](scenario-graph.md)

---

## 5. 인터랙터블 시스템

플레이어가 세계 오브젝트와 상호작용하는 구조를 정의합니다.

### 핵심 인터페이스

| 인터페이스 | 역할 |
|---|---|
| `IInteractable` | "상호작용 가능한 오브젝트" (복수 `IInteract` 목록 제공) |
| `IInteract` | 개별 상호작용 액션 1개 (표시명 + `Interact()`) |

### 상호작용 컴포넌트

- **`Interactable`**: 가장 기본적인 `IInteractable` 구현체. `handlerSources` 슬롯에 `IInteract` 구현 컴포넌트를 연결합니다.
- **`InteractableEntityResolver`**: `PlayerController`에 붙어서 인터랙션 라우팅을 처리합니다.
- **`NearbyInteractablesDetector`**: 카메라에 붙어 주기적으로 반경 내 `IInteractable`을 감지합니다.
- **`InteractableObjectHintUIController`**: 가까운 인터랙터블을 HUD에 표시합니다.

### 새 인터랙터블 만들기 (TriageTrainer 측)

```csharp
// 1. IInteract 구현 클래스 작성
public class OpenDoorInteract : MonoBehaviour, IInteract
{
    public string InteractName => "문 열기";
    public void Interact(Transform interactor) { /* 문 여는 로직 */ }
}

// 2. 오브젝트에 Interactable 컴포넌트 추가
//    Inspector에서 handlerSources에 OpenDoorInteract 추가

// 3. Interactable Layer 또는 Collider 설정
//    NearbyInteractablesDetector의 interactionLayerMask에 해당 레이어 포함
```

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.InteractableEntity.md](api-references/MultiplayerInfrastructure.InteractableEntity.md)  
> **구현 가이드:** [interaction-implementation-guide.md](interaction-implementation-guide.md)

---

## 6. 아이템 시스템

월드에 배치된 오브젝트로서 상호작용(줍기), 인벤토리 관리, 사용/공격 이벤트를 담당합니다.

### Item 컴포넌트

```
Item.cs            - 핵심 참조 (ItemBaseModelSO, ItemData, identifier)
Item.Lifecycle.cs  - Awake 초기화 (BaseModel → ItemData → Registry 등록)
Item.Interactable.cs - IInteractable + IInteract 구현 (줍기)
Item.Visual.cs     - 렌더러 등 시각 처리
Item.Runtime.cs    - ApplyRuntimeItemData() 런타임 데이터 교체
Item.RuntimeData.cs - 기타 런타임 데이터 처리
```

### ItemData

아이템의 데이터 컨테이너입니다. 인벤토리 슬롯 간 복사·전달에 사용됩니다.

```csharp
var data = new ItemData("scalpel", "메스", 1, 1, true, 100);
var clone = data.Clone();
bool canStack = data.CanStackWith(other);
data.Add(5).ApplyRestriction();   // 연산 체이닝 지원
```

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Item.Item.md](api-references/MultiplayerInfrastructure.Item.Item.md)  
> **인벤토리 API:** [api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md](api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md)  
> **아이템 정의:** [item.md](item.md)

---

## 7. 퀘스트 시스템

`QuestManager`는 씬에 하나 배치되는 MonoBehaviour이며, Registry를 통해 접근합니다.

```csharp
var mgr = Registry.Get<QuestManager>(RegistryType.Entity, Registry.TypeKey<QuestManager>());

// 퀘스트 추가 / 갱신
mgr.AddOrUpdateQuest(new QuestData { Id = "q1", Title = "검사", IsTracked = true, WaypointIdentifier = "exam-room" });

// 퀘스트 제거
mgr.RemoveQuest("q1");

// 트래킹 ON/OFF
mgr.SetTracked("q1", true);

// 이벤트 구독
mgr.OnQuestListChanged += quests => UpdateUI(quests);
mgr.OnTrackedQuestsChanged += tracked => UpdateHUD(tracked);
```

`WaypointIdentifier`가 설정된 신규 퀘스트가 추가될 때 `FeatureFlags.HighlightAssignedWaypoint`가 활성화되어 있으면 해당 웨이포인트가 자동으로 강조됩니다.

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Quest.QuestManager.md](api-references/MultiplayerInfrastructure.Quest.QuestManager.md)

---

## 8. 채팅 및 커맨드 시스템

### ChatService

게임 내 채팅 전송·수신과 `/커맨드` 실행을 담당합니다.

```csharp
// 시스템 메시지 전송
chatService.SendSystemMessage(conn, "환자 상태가 변경되었습니다.");

// 시스템 커맨드 실행 (서버 코드)
chatService.TryExecuteSystemCommand("/give bandage 5", out string result);
```

### 내장 커맨드

| 커맨드 | 설명 |
|---|---|
| `/help [command]` | 커맨드 목록 또는 상세 설명 |
| `/give <id> [count] [player]` | 아이템 지급 |
| `/clean [id] [count]` | 인벤토리 아이템 제거 |
| `/gamemode <player\|spectator>` | 게임모드 전환 |
| `/scenario <id>` | 시나리오 실행 |

### 커스텀 커맨드 추가 (TriageTrainer 측)

`IChatCommandModel`을 구현하고 `ChatCommandService`에 등록합니다.

```csharp
public class MyCommand : IChatCommandModel
{
    public string CommandEntry => "mycommand";
    public string Description => "설명";
    public bool RequiresAdmin => false;
    public void Execute(NetworkConnection sender, string[] args) { ... }
}
// ChatCommandService.Initialize() 내부에서 RegisterCommand(new MyCommand(...))
```

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Chat.ChatService.md](api-references/MultiplayerInfrastructure.Chat.ChatService.md)  
> **커맨드 확장:** [api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md](api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)

---

## 9. 웨이포인트 앵커

씬에 배치한 `WaypointAnchor` 컴포넌트가 자동으로 Registry에 등록되며, 시나리오나 퀘스트 시스템이 위치 정보 및 시각 강조를 요청합니다.

```csharp
// 식별자로 앵커 조회
if (WaypointAnchor.TryGet("exam-room", out var anchor))
    anchor.Highlight();   // 반짝이는 시각 강조 실행
```

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md](api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md)

---

## 10. 데이터팩 시스템

JSON 파일로 주기 명령 실행 및 시나리오 이벤트 핸들러 주입을 자동화합니다.

```json
{
  "packId": "my-pack",
  "periodicCommands": [
    { "command": "/give bandage 1", "intervalSeconds": 30, "runImmediately": false }
  ],
  "eventHandlers": [
    { "eventIdentifier": "door_open", "command": "/give key 1" }
  ]
}
```

```csharp
datapackRuntime.RegisterDatapackFromJson(jsonText);
datapackRuntime.UnregisterDatapack("my-pack");
```

> **API 레퍼런스:** [api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md](api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md)

---

## 11. 시스템 간 연결 흐름 예시

### 예시: 시나리오로 퀘스트를 부여하고 웨이포인트를 안내하기

```json
{ "type": "QuestControl",
  "identifier": "N010",
  "operation": "Add",
  "quest": { "id": "q1", "title": "처치실 이동", "waypointIdentifier": "treatment-room", "isTracked": true },
  "next": "N020" }

{ "type": "QuestWaypointHighlight",
  "identifier": "N020",
  "waypointIdentifier": "treatment-room",
  "next": "N030" }
```

→ `QuestManager`에 `q1` 추가됨  
→ `WaypointAnchor("treatment-room").Highlight()` 자동 호출됨  
→ 퀘스트 HUD에 `treatment-room` 표시됨

### 예시: InvokeEvent로 커스텀 로직 연결하기

```csharp
// TriageTrainer 측 MonoBehaviour
void Start()
{
    ScenarioEventIdentifierRegistry.Register("perform_bvm_oxygenation_a", () =>
    {
        return StartBVMRoutine();   // IEnumerator 반환
    });
}

void OnDestroy()
{
    ScenarioEventIdentifierRegistry.Unregister("perform_bvm_oxygenation_a");
}
```

```json
{ "type": "InvokeEvent",
  "identifier": "E010",
  "eventIdentifier": "perform_bvm_oxygenation_a",
  "moveNextBehavior": "WaitUntilDone",
  "next": "E020" }
```

---

## 관련 문서

| 문서 | 내용 |
|---|---|
| [scenario-graph.md](scenario-graph.md) | 시나리오 노드 JSON 스펙 |
| [scenario-authoring.md](scenario-authoring.md) | 시나리오 작성 가이드 |
| [interaction-implementation-guide.md](interaction-implementation-guide.md) | 인터랙터블 구현 가이드 |
| [item.md](item.md) | 아이템 정의 가이드 |
| [api-references/](api-references/) | API 레퍼런스 모음 |
