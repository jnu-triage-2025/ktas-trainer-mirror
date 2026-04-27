# API 레퍼런스: `MultiplayerInfrastructure.Player.PlayerModel`

> **네임스페이스:** `MultiplayerInfrastructure.Player`  
> **파일 위치:**  
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Model.cs`  
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerModelControls/IPlayerModelObject.cs`  
> - `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerModelControls/PlayerModelAttachPoint.cs`

---

## 0. 문서 목적

플레이어 3D 모델을 식별자 기반으로 조회/교체하고, 서버 권한으로 변경 사항을 네트워크 전체에 반영하는 API를 정리합니다.

---

## 1. 핵심 구성 요소

| 구성 요소 | 역할 |
|---|---|
| `IPlayerModelObject` | 플레이어 모델로 등록 가능한 프리팹의 마커 인터페이스 |
| `PlayerModelAttachPoint` | 기존 자식 모델 제거 후 신규 모델 인스턴스를 하위에 부착 |
| `PlayerController.Model` | 모델 ID 관리, 서버 권한 변경, SyncVar 동기화 |

---

## 2. `IPlayerModelObject`

```csharp
public interface IPlayerModelObject {}
```

- 모델 프리팹이 플레이어 모델 시스템에서 유효하다는 것을 나타내는 마커입니다.
- `PlayerModelAttachPoint`와 `PlayerController.Model`은 등록/적용 시 이 인터페이스 구현 여부를 검증합니다.

---

## 3. `PlayerModelAttachPoint`

### 주요 메서드

```csharp
public GameObject ReplaceAttachedModel(GameObject playerModelObject)
public GameObject ReplaceAttachedModel(IPlayerModelObject playerModelObject)
```

### 동작

- 입력 오브젝트의 `IPlayerModelObject` 구현 여부를 확인합니다.
- 현재 AttachPoint 하위 자식 오브젝트를 모두 제거합니다.
- 전달된 모델 프리팹을 AttachPoint 하위에 인스턴스화합니다.
- 생성된 모델의 로컬 트랜스폼을 기본값(`position=0`, `rotation=identity`, `scale=1`)으로 맞춥니다.

---

## 4. `PlayerController.Model` (partial)

### 상태 필드

```csharp
[SerializeField] private PlayerModelAttachPoint _playerModelAttachPoint;
[SerializeField] private string _defaultPlayerModelIdentifier;
[SerializeField] private string _currentPlayerModelIdentifier;
private readonly SyncVar<string> _playerModelIdentifier;

public string CurrentPlayerModelIdentifier { get; }
```

### 라이프사이클 훅

| 메서드 | 시점 | 역할 |
|---|---|---|
| `Awake_PlayerModel()` | `Awake` | AttachPoint 자동 탐색 |
| `OnStartServer_PlayerModel()` | 서버 스폰 시 | 기본 모델 ID 1회 적용 |
| `OnStartClient_AnyPeer_PlayerModel()` | 모든 클라이언트 스폰 시 | SyncVar 구독 및 초기 모델 반영 |
| `OnStopClient_AnyPeer_PlayerModel()` | 모든 클라이언트 디스폰 시 | SyncVar 구독 해제 |

### 공개/네트워크 API

```csharp
public void RequestSetPlayerModel(string modelIdentifier)
[ServerRpc] private void CmdSetPlayerModel(string modelIdentifier)
internal bool ApplyPlayerModelByIdentifierServer(string modelIdentifier)
```

- `RequestSetPlayerModel`은 Owner에서 호출하는 모델 변경 요청 진입점입니다.
- 실제 적용은 서버 권한 메서드(`ApplyPlayerModelByIdentifierServer`)에서 수행됩니다.
- 서버가 `_playerModelIdentifier`를 갱신하면 SyncVar 전파로 모든 피어가 동일 모델을 적용합니다.

---

## 5. Registry 연동

모델 조회는 `RegistryType.PlayerModel`에서 수행됩니다.

```csharp
var raw = Registry.Registry.Get<object>(RegistryType.PlayerModel, modelIdentifier);
```

조회 규칙:

- `GameObject`로 등록된 경우: 해당 오브젝트가 `IPlayerModelObject`를 가져야 유효합니다.
- `Component`로 등록된 경우: 그 컴포넌트가 `IPlayerModelObject`여야 유효합니다.

---

## 6. 기본값 자동 적용 흐름

1. 서버에서 `OnStartServer_PlayerModel()` 실행
2. `_defaultPlayerModelIdentifier`가 비어 있지 않으면 서버 적용 시도
3. 서버가 `_playerModelIdentifier` SyncVar 갱신
4. 각 클라이언트의 OnChange 핸들러에서 로컬 AttachPoint 교체

---

## 관련 문서

- [api-references/MultiplayerInfrastructure.Registry.md](MultiplayerInfrastructure.Registry.md)
- [api-references/TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel.md](TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel.md)
- [requirements/player/player-model-requirements.md](../requirements/player/player-model-requirements.md)
