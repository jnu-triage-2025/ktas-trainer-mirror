# API 레퍼런스: `TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel`

> **네임스페이스:** `TriageTrainer.MultiplayerInfrastructureSupports`  
> **파일 위치:**  
> - `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/RegisteringMultiplayerInfrastructureSupport.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/RegisteringMultiplayerInfrastructureSupport.PlayerModel.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/ScriptableObjects/PlayerModelRegistryRequirementsSO.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/ScriptableObjects/PlayerModelRegistryRequirement.cs`

## 0. 개요

TriageTrainer 범위에서 플레이어 모델 프리팹 목록을 ScriptableObject로 관리하고, 런타임 시작 시 `RegistryType.PlayerModel`에 일괄 등록하는 지원 API입니다.

## 1. 라이프사이클 구조

`RegisteringMultiplayerInfrastructureSupport`는 partial 구조를 사용합니다.

```csharp
private void Awake()
{
  Awake_Item();
  Awake_PlayerModel();
}
```

- `Awake_PlayerModel()`에서 PlayerModel 등록/검증을 수행합니다.
- 아이템 등록 흐름(`Awake_Item`)과 같은 패턴으로 분리되어 유지보수가 쉽습니다.

## 2. ScriptableObject 모델

### `PlayerModelRegistryRequirement`

```csharp
public struct PlayerModelRegistryRequirement
{
  public string identifier;
  public GameObject prefab;
}
```

### `PlayerModelRegistryRequirementsSO`

```csharp
public class PlayerModelRegistryRequirementsSO : ScriptableObject
{
  public PlayerModelRegistryRequirement[] playerModelRegistryRequirements;
}
```

인스펙터에서 `identifier`와 `prefab` 매핑 목록을 정의합니다.

## 3. 등록/검증 API

### 주요 메서드

| 메서드 | 역할 |
|---|---|
| `Awake_PlayerModel()` | PlayerModel 등록 + 리소스 검증 수행 |
| `RegisterAllPlayerModels()` | SO 목록을 순회하며 `RegistryType.PlayerModel`에 등록 |
| `ValidatePlayerModelResources()` | 프리팹의 `IPlayerCharacterModelObject` 구현 여부 검증 |

### 동작 규칙

- SO 참조가 없거나 항목 배열이 비어 있으면 경고를 출력하고 종료합니다.
- 항목의 `identifier`가 비어 있거나 `prefab`이 null이면 해당 항목을 건너뜁니다.
- 유효한 항목은 `Registry.Register(RegistryType.PlayerModel, identifier, prefab)`으로 등록됩니다.

## 4. 운영 가이드

1. `Create > TriageTrainer > Multiplayer Infrastructure > PlayerModel Registry Requirements SO`로 에셋 생성
2. `RegisteringMultiplayerInfrastructureSupport`의 Player Model Registry 필드에 SO 할당
3. 각 엔트리에 모델 ID와 프리팹을 설정
4. 프리팹 루트(또는 동일 GameObject)에 `IPlayerCharacterModelObject` 구현 컴포넌트 부착

## 5. TriageTrainer 캐릭터 모델 구현체

TriageTrainer는 플레이어 모델 프리팹 루트에 다음 어댑터 컴포넌트를 배치해 `IPlayerCharacterModelObject` 계약을 충족한다.

- `PlayerCharacterModelAiden`
- `PlayerCharacterModelBrian`
- `PlayerCharacterModelDominic`
- `PlayerCharacterModelEmma`
- `PlayerCharacterModelEthan`
- `PlayerCharacterModelJeb`
- `PlayerCharacterModelLiam`
- `PlayerCharacterModelLisa`
- `PlayerCharacterModelMaya`
- `PlayerCharacterModelOlivia`
- `PlayerCharacterModelSerah`
- `PlayerCharacterModelSofia`

공통 계약:

- `CharacterControllerCenter => (0, 1, 0)`
- `Animator` 프로퍼티로 모델 Animator를 노출

검증 시 주의사항:

- 등록 SO에는 identifier/prefab null 항목이 없어야 한다.
- prefab 루트에 `IPlayerCharacterModelObject` 구현이 없으면 런타임 경고가 발생한다.

## 관련 문서

- [api-references/MultiplayerInfrastructure.Player.PlayerModel.md](MultiplayerInfrastructure.Player.PlayerModel.md)
- [requirements/player/player-model-requirements.md](../requirements/player/player-model-requirements.md)
