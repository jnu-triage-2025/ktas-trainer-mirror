# API 레퍼런스: `MultiplayerInfrastructure.Player.PlayerController`

> **네임스페이스:** `MultiplayerInfrastructure.Player`  
> **기반 클래스:** `FishNet.Object.NetworkBehaviour`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController*.cs` (14개 부분 클래스)

---

## 0. 문서 목적

`PlayerController`는 플레이어의 모든 기능을 담당하는 핵심 컴포넌트입니다.  
파일별로 기능이 분리된 `partial class` 구조로 관리됩니다.

---

## 1. 파일 구조

| 파일 | 담당 기능 |
|---|---|
| `PlayerController.cs` | Awake/Start/Update 라이프사이클 |
| `PlayerController.Network.cs` | 네트워크 초기화, Registry 등록/해제 |
| `PlayerController.Movement.cs` | 이동, 점프, 마우스 시점, UI 오버레이 모드 |
| `PlayerController.Camera.cs` | 카메라 타겟, 시점 전환 |
| `PlayerController.Input.cs` | 키 입력 처리, UIOverlayStack 관리 |
| `PlayerController.Gamemode.cs` | 플레이어/스펙테이터 모드 전환 |
| `PlayerController.Visibility.cs` | 게임모드별 렌더러 투명도 처리 |
| `PlayerController.Inventory.cs` | 인벤토리 슬롯 관리 |
| `PlayerController.Hotbar.cs` | 핫바 선택·입력 처리 |
| `PlayerController.Item.cs` | 아이템 핸들링, 공격/사용, 뷰모델 |
| `PlayerController.Interactables.cs` | 주변 인터랙터블 감지 및 상호작용 |
| `PlayerController.Dialogue.cs` | 다이얼로그 UI 연결 |
| `PlayerController.Quest.cs` | 퀘스트 UI 연결 |
| `PlayerController.Chat.cs` | (채팅 입력은 Input에 통합) |
| `PlayerController.EscapeMenu.cs` | ESC 메뉴 연결 |
| `PlayerController.ReposableCarry.cs` | 이동 가능 오브젝트 들기/내려놓기 |

---

## 2. Network (PlayerController.Network.cs)

### 동작

| 이벤트 | 동작 |
|---|---|
| `OnStartClient` (Owner) | Registry에 `PlayerController` 등록; 입력 초기화 |
| `OnStartServer` | `PlayerGamemodeService.RegisterPlayer()` 호출 |
| `OnStopServer` | `PlayerGamemodeService.UnregisterPlayer()` 호출 |
| `OnStopClient` (Owner) | Registry에서 `PlayerController` 해제 |

---

## 3. Movement (PlayerController.Movement.cs)

### 주요 메서드

```csharp
// 커서 잠금/해제 (UI 모드 전환 시 호출됨)
public void LockCursor()
public void UnlockCursor()

// UI 오버레이 모드 진입/해제 (인벤토리, 채팅 등 UI 열릴 때 사용)
public void EnterUIOverlayMode(string reason)
public void ExitUIOverlayMode(string reason)
```

`EnterUIOverlayMode` / `ExitUIOverlayMode`는 스택(UIOverlayStack)으로 관리됩니다. 진입 횟수와 해제 횟수가 일치할 때 실제로 오버레이 모드가 해제됩니다.

### 주요 프로퍼티

```csharp
public bool canMove { get; set; }    // 이동 허용 여부
public bool IsRunning { get; }       // 달리기 중 여부
public bool IsGrounded { get; }      // 지면 접촉 여부
```

---

## 4. Gamemode (PlayerController.Gamemode.cs)

### PlayerGamemode 열거형

```csharp
public enum PlayerGamemode
{
    Player,     // 일반 플레이어
    Spectator,  // 관전자 (다른 플레이어 추종)
}
```

### 서버 측 게임모드 변경

```csharp
// 서버에서만 호출
PlayerGamemodeService.TrySetGamemode(conn, controller, PlayerGamemode.Spectator, out string error);
```

### 클라이언트 조회

```csharp
bool isSpectator = controller.IsSpectator;          // 스펙테이터 여부
PlayerGamemode mode = controller.CurrentGamemode;   // 현재 게임모드
```

---

## 5. Inventory (PlayerController.Inventory.cs)

> **실행 컨텍스트:** 대부분 서버 측 또는 ClientRpc를 통해 간접 호출됩니다.

### `HandlingItem`

```csharp
public ItemData HandlingItem = null;
```

현재 핫바에서 선택된 아이템 데이터입니다.

### 아이템 추가

```csharp
// 인벤토리에 아이템 추가. 성공 여부 반환
public bool TryAddItemToInventory(ItemData item)

// leftover: 넣지 못한 나머지 수량
public bool TryAddItemToInventory(ItemData item, out ItemData leftover)
```

### 아이템 제거

```csharp
// 특정 아이템 n개 제거. 실제 제거된 수량 반환
public int RemoveItemFromInventory(string itemIdentifier, int count)

// 특정 아이템 전부 제거. 제거된 수량 반환
public int RemoveAllOfItemFromInventory(string itemIdentifier)

// 인벤토리 전체 비우기. 제거된 슬롯 수 반환
public int ClearInventory()
```

### 아이템 조회 / 드롭

```csharp
// 인벤토리 내 특정 아이템 총 개수 반환
public int CountItemInInventory(string itemIdentifier)

// 플레이어 앞에 아이템 드롭
public bool TryDropItemInFront(ItemData itemData)
```

---

## 6. Interactables (PlayerController.Interactables.cs)

직접 호출보다는 `NearbyInteractablesDetector`를 통해 자동으로 관리됩니다.  
`E` (기본 상호작용 키)를 누르면 감지된 첫 번째 인터랙터블이 `Interact()`됩니다.

---

## 7. ReposableCarry (PlayerController.ReposableCarry.cs)

월드에서 들고 이동할 수 있는 오브젝트와 상호작용합니다.

```csharp
// 가까운 이동 가능 오브젝트 집기
public bool TryPickUpReposable(IReposable reposable)

// 들고 있는 오브젝트 내려놓기
public bool TryDropCarriedReposable()
```

---

## 8. PlayerGamemodeService

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerGamemodeService.cs
```

서버 측에서 플레이어의 게임모드 상태를 관리하는 정적 클래스입니다.

```csharp
// 플레이어 등록 (OnStartServer에서 호출)
PlayerGamemodeService.RegisterPlayer(NetworkConnection conn, PlayerController controller)

// 플레이어 해제 (OnStopServer에서 호출)
PlayerGamemodeService.UnregisterPlayer(NetworkConnection conn)

// 게임모드 변경
// error: 실패 시 오류 메시지
bool PlayerGamemodeService.TrySetGamemode(
    NetworkConnection conn,
    PlayerController controller,
    PlayerGamemode gamemode,
    out string error)
```

---

## 9. UIOverlayStack (PlayerController.Input.cs)

UI 모드 중복 진입을 안전하게 처리하는 스택입니다.

```csharp
// 인벤토리나 채팅 등이 열릴 때 자동으로 호출됨
controller.EnterUIOverlayMode("inventory");
controller.EnterUIOverlayMode("chat");    // 중첩 진입

controller.ExitUIOverlayMode("chat");     // 스택에서 제거
controller.ExitUIOverlayMode("inventory"); // 최종 해제 시 커서 잠금 복원
```

---

## 관련 문서

- [multiplayer-infrastructure-overview.md](architecture/multiplayer-infrastructure-overview.md) — 시스템 전체 가이드
- [api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md](MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md) — 인벤토리 커맨드
- [api-references/MultiplayerInfrastructure.Item.Item.md](MultiplayerInfrastructure.Item.Item.md) — 아이템 시스템
