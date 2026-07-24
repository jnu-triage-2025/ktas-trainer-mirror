# Scenario Inline Acting NPC API

## 데이터 계약

`ScenarioGraph.ActingNpcs`는 `IReadOnlyList<ScenarioActingNpcDefinition>`이다. 각 actingNpc는 등록된
`EntityPresetDefinition`을 참조하며 `ScenarioController`의 실행 수명주기에 종속된다. 생성 시점은
actingNpc의 `spawnOnStart` 또는 `ScenarioEntityPresetSpawnNode.ActingNpcIdentifier`가 지정한다.

주요 타입:

- `ScenarioActingNpcDefinition`
- `ScenarioActingNpcInteractionDefinition`
- `ScenarioActingNpcItemRequirement`
- `ScenarioActingNpcType`
- `ScenarioActingNpcInteractionType`

## 생성 경로

시작 소환과 중간 소환은 모두 `TrySpawnScenarioActingNpc` 경로를 사용한다.

1. `ScenarioController.StartScenarioInternal`의 `PrepareScenarioActingNpcs`가 `spawnOnStart` actingNpc를 생성하거나,
   `ScenarioEntityPresetSpawnNode`가 `actingNpcIdentifier`로 actingNpc를 생성한다.
2. `Registry.TrySpawnEntityPreset`
3. `Npc.ApplySpawnedEntityIdentifier`
4. `Npc.ConfigureScenarioActingNpc`
5. 네트워크 actingNpc이면 `ScenarioNetworkRelay`로 원격 클라이언트 구성 전달

`actingNpcIdentifier`를 쓸 때에는 actingNpc 정의가 preset·transform·상호작용을 소유한다. 노드의
`presetIdentifier` 등 일반 preset spawn 전용 필드는 무시된다.

시작 소환 actingNpc는 사전 검증 전에 등록되므로 `NPCControl`이 요구하는 NPC provider를
같은 시나리오의 actingNpc 선언이 공급할 수 있다. 중간 소환 actingNpc는 해당 spawn 노드 이후에 사용해야 한다.

## 식별자 계약

`Npc`는 `ISpawnedEntityIdentifierReceiver`를 구현한다. 프리팹 `Awake`에서 임시 식별자로
등록된 경우 기존 항목을 해제한 뒤 actingNpc identifier로 `RegistryType.Npc`와 Entity descriptor를
동시에 다시 등록한다.

## 상호작용 구현

- `StartScenario`: 경량 `IInteract` adapter를 만들고 대상 그래프를 Registry에서 찾아 시작한다.
- `ItemSubmission`: NPC 하위에 `ItemSubmissionInteractable`을 생성하고
  `ItemSubmissionDefinition`을 구성한다.
- `Signal`: 상호작용 시 `completionSignalIdentifier`를 런타임 신호로 발생시킨다.

알 수 없는 interaction enum과 중복 identifier는 JSON 로드 단계에서 거부된다.

## 정리 정책

`ScenarioController`는 자신이 만든 GameObject와 `despawnOnScenarioEnd`를 추적한다.
정상 종료에서는 `despawnOnScenarioEnd`가 true인 경우에 정리하고, 시작 실패·중단·새 시나리오 교체 시에는
값과 무관하게 부분 생성물을 정리한다. 서버에서 spawn된
`NetworkObject`는 `ServerManager.Despawn`, 오프라인 오브젝트는 `Destroy`를 사용한다.

## 제한

- Unity 프리팹 자체는 JSON에 직렬화하지 않는다.
- 지원 actingNpc type은 현재 `Npc`뿐이다.
- 지원 interaction type은 `StartScenario`, `ItemSubmission`, `Signal`이다.
- 같은 actingNpc를 중복 생성하는 것은 허용하지 않는다. 다시 생성하려면 별도 despawn/re-spawn 기능이 필요하다.
- actingNpc별 scale, spawn anchor, 대화 한 줄 자체를 실행하는 interaction은 아직 지원하지 않는다.
- 네트워크 actingNpc는 서버 구성 후 `ScenarioNetworkRelay`가 graph/actingNpc identifier와
  `NetworkObject` 참조를 전달한다. 신규 접속자에게도 현재 spawn 상태의 구성 목록을 Target RPC로 재전송한다.
