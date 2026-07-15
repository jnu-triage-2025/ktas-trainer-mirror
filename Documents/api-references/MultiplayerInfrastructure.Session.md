# API 레퍼런스: MultiplayerInfrastructure.Session

> **네임스페이스:** `MultiplayerInfrastructure.Session`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Session/`

---

## 0. 개요

Session 네임스페이스는 플레이어 신원 관리와 LAN 세션 탐색을 담당하는 세 클래스로 구성됩니다.

| 클래스 | 유형 | 역할 |
|---|---|---|
| `UserDescriptor` | 데이터 모델 | 단일 플레이어의 신원 정보 |
| `UserDescriptorService` | 정적 서비스 | 접속 중인 플레이어 목록 관리 |
| `LanDiscoveryService` | MonoBehaviour 싱글톤 | LAN 세션 브로드캐스트/탐색 |
| `SessionInformationModel` | 데이터 모델 | LAN에서 탐색된 세션 정보 |

---

## 1. UserDescriptor

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Session/UserDescriptor.cs
```

서버에 접속한 단일 플레이어의 신원 설명자입니다.

### 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|---|---|---|
| `Identifier` | `string` | 서버가 발급한 UUID. 재접속 시 새로 발급됩니다. Registry 키, 코드 내 엔티티 쿼리에 사용 |
| `DisplayName` | `string` (get/set) | 플레이어 표시 이름. 중복 가능하므로 명확한 식별에는 `Identifier` 사용 |

### 생성자 및 팩토리

```csharp
// 일반 생성자
new UserDescriptor(string identifier, string displayName)

// 개발용 기본 설명자 (랜덤 UUID 발급, 앞 8자리를 DisplayName으로 사용)
UserDescriptor.CreateDefault()
```

### 사용 패턴

```csharp
// PlayerController에서 자동 생성
var descriptor = UserDescriptor.CreateDefault();

// 표시 이름 변경 후 서비스에 반영
descriptor.DisplayName = "홍길동";
UserDescriptorService.UpdateDisplayName(descriptor.Identifier, "홍길동");
```

---

## 2. UserDescriptorService

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Session/UserDescriptorService.cs
```

접속 중인 모든 플레이어의 `UserDescriptor`를 관리하는 정적 서비스입니다.  
서버와 모든 클라이언트에서 각자 로컬 사전을 유지합니다.

### 자동 등록/해제 시점

- **서버:** `PlayerController.OnStartServer` / `OnStopServer`에서 자동 등록·해제
- **클라이언트:** `PlayerController` 스폰·디스폰 시 `IsOwner` 여부 무관하게 자동 등록·해제
- **SyncVar 변경 시:** `UpdateDisplayName()`이 자동 호출되어 `DisplayName` 최신 유지

### 쿼리 메서드

```csharp
// 엔티티 식별자(UUID)로 조회
bool TryGetByIdentifier(string identifier, out UserDescriptor descriptor)

// FishNet ClientId로 조회
bool TryGetByClientId(int clientId, out UserDescriptor descriptor)

// 표시 이름으로 조회 (채팅 명령어 등 사람 입력용, 대소문자 무관)
bool TryGetByDisplayName(string displayName, out UserDescriptor descriptor)

// 전체 열거
IReadOnlyCollection<UserDescriptor> GetAll()
```

### 등록/해제 메서드

```csharp
// PlayerController 스폰 시 호출
void Register(int clientId, UserDescriptor descriptor)

// PlayerController 디스폰 시 호출
void Unregister(int clientId)

// DisplayName SyncVar 변경 시 호출
void UpdateDisplayName(string identifier, string newDisplayName)
```

---

## 3. SessionInformationModel

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Session/SessionInformationModel.cs
```

LAN에서 탐색된 세션 정보를 담는 데이터 모델입니다.

### 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|---|---|---|
| `Name` | `string` | 세션 이름 |
| `Address` | `string` | 호스트 IP 주소 |
| `Port` | `ushort` | 게임 포트 |
| `LastSeenUtc` | `DateTime` | 마지막으로 브로드캐스트를 수신한 시각 (UTC) |

### 생성자

```csharp
new SessionInformationModel(string address, ushort port, string? sessionName = null, DateTime? lastSeenUtc = null)
```

---

## 4. LanDiscoveryService

```
Assets/Modules/MultiplayerInfrastructure/Scripts/Session/LanDiscoveryService.cs
```

UDP 브로드캐스트로 LAN 내 게임 세션을 탐색하고 알리는 MonoBehaviour 싱글톤입니다.

### Inspector 설정

| 필드 | 기본값 | 설명 |
|---|---|---|
| `discoveryPort` | `47777` | UDP 탐색 포트 |
| `heartbeatIntervalMs` | `1000` | 브로드캐스트 간격(밀리초) |
| `entryTtlMs` | `5000` | 탐색된 세션 TTL(밀리초). 이 시간 내 재수신 없으면 목록에서 제거 |

### 공개 API

```csharp
// 싱글톤 접근
LanDiscoveryService.Instance

// 브로드캐스트 (서버용)
void StartBroadcast(string sessionName, int gamePort)
void StopBroadcast()

// 탐색 (클라이언트용)
void StartDiscovery()
void StopDiscovery()
void ClearDiscovered()

// 탐색 결과 쿼리
List<SessionInformationModel> GetDiscoveredSessions()
bool HasPendingUpdate()   // 마지막 GetDiscoveredSessions() 호출 이후 목록 변경 여부
```

### 사용 예시

```csharp
// 서버: 세션 알리기
LanDiscoveryService.Instance.StartBroadcast("우리 팀 훈련", 7770);

// 클라이언트: 세션 탐색
LanDiscoveryService.Instance.StartDiscovery();

// 매 프레임 또는 주기적으로 목록 갱신 확인
if (LanDiscoveryService.Instance.HasPendingUpdate())
{
    var sessions = LanDiscoveryService.Instance.GetDiscoveredSessions();
    // UI 업데이트
}
```

### 브로드캐스트 패킷 형식

```
FISHNET_DISCOVERY|{sessionName}|{ipAddress}|{gamePort}
```

---

## 5. 관련 문서

- [MultiplayerInfrastructure.Player.PlayerController.md](./MultiplayerInfrastructure.Player.PlayerController.md) — PlayerController에서의 UserDescriptor 사용
- [MultiplayerInfrastructure.Tag.PlayerTagService.md](./MultiplayerInfrastructure.Tag.PlayerTagService.md) — 플레이어 태그 시스템
- [requirements/session/session-lan-requirements.md](../requirements/session/session-lan-requirements.md) — LAN 세션 요구사항
