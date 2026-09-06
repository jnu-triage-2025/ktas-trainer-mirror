# 2026-09-06 변경 노트: 인터렉션 레지스트리와 가시성 체계 도입

## 요약

월드 인터렉션의 **정의**(무엇이 있는가)와 **가시성**(누구에게 보이는가)을 인터렉션 레지스트리
(`InteractionRegistry`) 한 곳으로 모았습니다. 정의는 코드 리터럴과 시나리오 데이터에서만 오고, 프리팹·씬의
직렬화 필드는 더 이상 정의 출처가 아닙니다. 노출은 조건 절(권장) 또는 트리거 노드로만 바꾸며, 4인 멀티플레이에서
피어마다 같은 결과를 내도록 오버라이드와 엔티티 태그는 서버 권위로 복제됩니다.

명세와 결정 기록: `Agents/Proposals/2026-09-05-interaction-registry-visibility-spec/PROPOSAL.md`

---

## 1) 정의 출처

| 출처 | 형식 | 등록 시점 |
|---|---|---|
| 코드 리터럴 | 엔티티가 `IInteractionDefinitionSource.DeclareInteractions()`로 선언 | 엔티티 초기화 사이클(레지스트리 등록 직후) |
| 시나리오 데이터 | 시나리오 JSON 최상위 `interactions` 구역 | 시나리오 초기화 사이클(시작 시 등록, 종료 시 해제) |
| 전역 카탈로그 | `Resources/Interactions/*.json` (`{"interactions":[...]}`) | 첫 씬 로드 후 한 번 |

같은 주소(`엔티티/인터렉션`)에 코드와 데이터 정의가 모두 있으면 데이터 값이 코드 값을 덮어씁니다(오버레이).
데이터만 있는 주소는 `kind`(`Action`/`Signal`/`StartScenario`/`ItemSubmission`)에 따라 범용 핸들러를 만들고,
`Custom`이면 엔티티의 `IInteractionHandlerFactory`가 `handlerKey`로 핸들러를 만듭니다(예: 환자 의식 확인
`recognition_check`).

초기화 사이클 밖에서 등록하면 에디터 런타임에서 경고를 남기지만 동작은 막지 않습니다.

## 2) 가시성

판정 순서: **오버라이드(플레이어별 → 전역) → 조건 절(`visibility.conditions`) → `visibility.initial`(기본 `false`)**.

- 조건 절은 `ScenarioCondition` 목록입니다. `PlayerHasTag`, `PlayerHasQuest`(퀘스트의 현재 기준),
  `PlayerHasItem`, `PlayerState`/`EntityState`(코드가 `IConditionStateProvider`로 노출하는 키), `SignalRaised`,
  `EntityHasTag`, `Group` 등 13종이며, `Validator` 노드의 `Conditions` 루트 조건도 같은 형식을 씁니다.
- 트리거는 `InteractionVisibility` 노드(`Show`/`Hide`/`Reset`, `playerScope: All|Current|ByTag`) 또는 코드 API
  `InteractionRegistry.SetVisibilityOverride`입니다. `afterInteract: HideForPlayer|HideForAll`은 수행 직후 오버라이드를
  기록합니다.
- 대상은 `{ "entity": { "id" | "tag" }, "interaction" }`입니다. 태그 참조는 엔티티 태그를 가진 모든 엔티티에 적용됩니다.
  엔티티 태그는 `PlayerTagService` 하나가 보관하고(`EntityPresetSpawn.tags`, `EntityTag` 노드, `AssignEntityTag`),
  플레이어가 아닌 식별자는 `ScenarioNetworkRelay`가 복제합니다.

멀티플레이 규약: 정의는 각 피어가 로컬로 만들고, 오버라이드·엔티티 태그는 서버가 기록해 `ObserversRpc`로 미러링하며,
늦게 접속한 피어는 신호 스냅샷 요청 때 함께 복원됩니다. 조건 절은 각 피어가 복제된 입력으로 자기 플레이어 기준
판정합니다.

## 3) 폐기한 것

| 폐기 | 대체 |
|---|---|
| `ScenarioActionInteractable` 컴포넌트, `PatientTypeA`/`DDummyA` 프리팹의 12+1개 액션 컴포넌트 | `interactions` 구역의 `kind: "Action"` 정의(`activateObjects`/`deactivateObjects`) |
| `PatientACriticalQuestStateFlags`, `ACT_*` 노드, `activate_patient_a_*` 이벤트 | `visibility.conditions`의 `PlayerHasQuest`/`PlayerHasTag` |
| 환자 프리팹 `_assessActions`, `_interactConfigs`, 표시 문구 필드 | 코드 리터럴(`PatientController.InteractionRegistry.cs`) + 데이터 오버레이 |
| NPC 프리팹 `_scenarioInteracts`, `_submissionInteracts`, `_customInteractSources`, `NPCBaseModelSO`의 인터렉션 목록 | `interactions` 구역, 전역 카탈로그 |
| `actingNpcs[].interactions` | `interactions` 구역(로더가 자동 이전, 에디터 경고) |
| `Interaction`, `ItemSubmissionConfig`, `NpcInteractControl` 노드 | `InteractionVisibility` 노드, `ItemSubmission` 정의 |
| `NPCControl.interactOperation`/`interactableIdentifier`/`interactEnabled`/`resultStateKey` | 위와 같음 (`NPCControl(Update)`는 표시 이름만 갱신) |
| `ScenarioInteractable` 컴포넌트 | `kind: "StartScenario"` 정의(전역 카탈로그 `demo_scenario_giving_npc/start_disaster_intro`) |
| `InteractableEntityResolver.handlerSources` | 제거(사용처 없음) |
| `IntravenousLineConnectionPoint._interactConfigs`(`InteractConfig`) | 제거. 두 플레이어 인터렉션은 코드에서 항상 잠김(레지스트리 제외 표식) |
| 튜토리얼 미끼의 인스펙터 문구·대사 | `TutorialDecoyInteractable` + 전역 카탈로그(`DummyInteractTrainer/...`) |

`ItemSubmissionInteractable` 컴포넌트는 제출 UI 위임용으로 남아 있으며, `RegistryItemSubmissionInteract`가
엔티티 하위에 만들어 사용합니다. `EntityType.ScenarioInteractable` 열거값은 그 컴포넌트가 아직 쓰므로 유지했습니다.

## 4) 콘텐츠 이전 결과

- `patient_a_critical`: 35개 정의(의사 제출 4, 환자 A 액션·사정·아이템 사용·IV, 모니터 상세 `tag: patient_monitor`,
  `zone_a:wall_suction`, `zone_a:oxyflowmeter`). `ISC_PASS_*`는 `InteractionVisibility Show`.
- `patient_b_c_ct`: 환자별 의식 확인 단계(`recognition_1..4`, `strength_check`, `pupil_check`)를 고유 식별자로 정의하고
  퀘스트 표시 바인딩을 단계별 식별자로 갱신.
- `tutorial`: 모자 NPC 대화 시작·택배·시계 제출 정의, `CONFIG_*`는 `InteractionVisibility`.
- `disaster_intro`: 분류 안내 액션 정의. 동작이 없던 `Interaction` 노드(`I001`, `I002`)는 제거하고 앞뒤를 연결.
- 표시 우선순위(결정 9): `start_ambu_r1`/`start_ambu_r2` 1000, `remove_patient_clothing` 900.

## 5) 다른 브랜치에서 들어오는 작업을 옮기는 방법

1. 프리팹에 인터렉션 컴포넌트나 문구 필드를 추가했다면, 그 값을 시나리오 JSON `interactions` 정의로 옮긴다.
   엔티티가 새 `IInteract`를 갖는다면 `IQuestPresentationTarget`으로 주소를 드러내고 `DeclareInteractions()`에
   포함시킨다(기본 `initialVisible: false`, 항상 보여야 하면 `true`).
2. "조건이 되면 인터렉션을 추가한다"는 코드는 정의(항상 등록) + `visibility.conditions` 또는 `InteractionVisibility`
   노드로 바꾼다. 컴포넌트 `SetEnabled` 호출은 `SetVisibilityOverride`로 바꾼다.
3. `ItemSubmissionConfig`/`NpcInteractControl`/`NPCControl.interactOperation`을 쓰던 그래프는 `ItemSubmission`
   정의와 `InteractionVisibility` 노드로 바꾼다. 스키마 검증(`dotnet run --project Tools/scenario-json-validator -- <file>`)이
   남은 필드를 잡아 준다.
4. 레지스트리 상태는 `Tools > Multiplayer Infrastructure > Interaction Registry` 창에서 확인한다(판정 사유 표시,
   에디터에서만 오버라이드 편집).

## 6) 검증

- `Assembly-CSharp`, `Assembly-CSharp-Editor` 컴파일 0 오류. 모든 `*.scenario.json` 스키마 검증 통과.
- EditMode 테스트: `ScenarioConditionEvaluatorTests`, `InteractionRegistryTests`,
  `ScenarioInteractionDefinitionRoundTripTests` 추가. 환자 A/B/C·튜토리얼 테스트를 데이터 기반으로 재작성.
  Unity 에디터가 열려 있어 배치 실행은 하지 못했으므로 Test Runner에서 EditMode 전체를 한 번 돌려야 한다.
- 실플레이 검증(결정 16): 환자 A 전체, B/C 전체, 튜토리얼을 4인 접속(늦은 접속 포함)으로 사용자가 수동 검토.

## 관련 문서

- [MultiplayerInfrastructure.InteractableEntity.md](../api-references/MultiplayerInfrastructure.InteractableEntity.md)
- [MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md](../api-references/MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md)
- [guide/ScenarioGraph.md](../guide/ScenarioGraph.md)
- [scenario-action-interactable-setup-guide.md](../working-guide/features/scenario/scenario-action-interactable-setup-guide.md)
