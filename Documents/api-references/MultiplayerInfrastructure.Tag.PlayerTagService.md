# API 레퍼런스: MultiplayerInfrastructure.Tag.PlayerTagService

> 네임스페이스: MultiplayerInfrastructure.Tag  
> 파일 위치: Assets/Modules/MultiplayerInfrastructure/Scripts/Tag/PlayerTagService.cs

## 0. 개요

PlayerTagService는 플레이어 태그를 Registry(RegistryType.PlayerTag)에 저장/조회하는 정적 서비스입니다.

- 저장 키: UserDescriptor.Identifier(UUID)
- 저장 값: List<string>
- 서버 권한: 태그 변경(Add/Remove/Change)은 서버 컨텍스트에서만 허용
- 동기화: 변경 시 소유 PlayerController의 SyncPlayerTagsToObservers() 호출

## 1. 핵심 동작

| 항목 | 설명 |
|---|---|
| 서버 컨텍스트 가드 | IsServerMutationAllowed()로 서버 외 변경 차단 |
| 저장소 생성 | GetOrCreateTagList(uuid)로 없으면 자동 생성/등록 |
| 오너 동기화 | SyncOwnerPlayerTags(uuid)로 관전자 태그 동기화 |
| 정리 | ClearTags(uuid)로 레지스트리에서 제거 |

## 2. 공개 API

### AddTag

```csharp
public static void AddTag(string uuid, string tag)
```

- 역할: 태그 추가(중복 방지)
- 조건: 서버에서만 실제 반영

### RemoveTag

```csharp
public static bool RemoveTag(string uuid, string tag)
```

- 역할: 태그 제거
- 반환: 제거 성공 여부

### ChangeTag

```csharp
public static bool ChangeTag(string uuid, string fromTag, string toTag)
```

- 역할: 기존 태그 교체
- 반환: fromTag 존재 시 true

### ClearTags

```csharp
public static void ClearTags(string uuid)
```

- 역할: 플레이어 태그 저장소 제거
- 용도: 연결 종료/디스폰 정리

### ReplaceTags

```csharp
public static void ReplaceTags(string uuid, IReadOnlyList<string> tags)
```

- 역할: 네트워크 스냅샷으로 태그 목록 치환
- 특징: 빈 값/중복 값 필터링

### GetTags

```csharp
public static IReadOnlyList<string> GetTags(string uuid)
```

- 역할: 태그 읽기
- 반환: 없으면 빈 배열

### HasTag

```csharp
public static bool HasTag(string uuid, string tag)
```

- 역할: 태그 보유 여부 확인

## 3. 사용 가이드

- 태그 변경은 서버 로직에서만 수행합니다.
- 태그 분기(Scenario Parallel branch)는 대소문자 일관성을 유지합니다.
- 플레이어 종료 시 ClearTags를 호출해 누수 데이터를 방지합니다.

## 3-1. 엔티티 태그 (2026-09-06)

같은 서비스가 플레이어가 아닌 엔티티(환자 모니터, 흡인기 등)의 태그도 식별자 키로 보관한다. 별도 저장소를 두지
않는다(결정 5). `EntityPresetSpawn.tags`, `EntityTag` 노드, `InteractionRegistry.AssignEntityTag`가 서버에서
기록하면, 소유 플레이어가 없는 식별자는 `ScenarioNetworkRelay.PublishEntityTags`로 전 피어에 미러링되고 늦은
접속 스냅샷에도 포함된다. `interactions[].entity.tag`와 조건 절 `EntityHasTag`가 이 값을 읽으며, 변경 시
`TagsChanged` 이벤트가 레지스트리의 가시성 재판정을 깨운다.

## 4. 관련 문서

- api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md
- api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md
- requirements/content-definitions/scenario/scenario-graph-spec.md
- api-references/MultiplayerInfrastructure.InteractableEntity.md (인터렉션 레지스트리)
