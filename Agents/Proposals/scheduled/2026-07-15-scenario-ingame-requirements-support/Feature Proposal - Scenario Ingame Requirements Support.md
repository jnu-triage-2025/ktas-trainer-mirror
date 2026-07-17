# Feature Proposal: Scenario Ingame Requirements Support

- 작성일: 2026-07-15
- 상태: **제안(scheduled)**
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`,
  `Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/`
- 프로젝트 확장 대상: `Assets/Modules/TriageTrainer/Editor/Scenario/`
- 관련 기존 기능: `ScenarioPreflight`, `ScenarioRequirementsCollector`,
  `ScenarioDevStubSpawner`, `OverworldGameObjectInitializer`
- 구현 상세: [`implementation-blueprint.md`](./implementation-blueprint.md)
- 데이터 계약 초안: [`requirements-data-contract.md`](./requirements-data-contract.md)

> `MultiplayerInfrastructure`는 다른 프로젝트에서 재사용되는 핵심 모듈이다. 이 문서는 구현 승인을
> 위한 설계 제안이며 실제 모듈 코드를 변경하지 않는다. 구현은 단계별 수용 기준을 통과한 뒤 진행한다.

### 개요

`Scenario Ingame Requirements Support`는 `.scenario.json`이 실행되기 위해 필요로 하는 Unity 씬
오브젝트, Registry 항목, 프리셋, 이벤트 핸들러, 리소스 및 런타임 공급 조건을 **명시적인 요구사항
계약**으로 변환하고, 그 계약을 실제 씬 구성에 연결하며, Editor·빌드·런타임에서 같은 의미로
검증하는 기능이다.

이 기능은 입력을 엄격하게 제한하는 authoring 도구가 아니다. 시나리오 자동 추출, 사람이 작성한
보충 선언, AI 에이전트가 생성한 후보 선언처럼 서로 다른 품질의 입력을 받아들인다. 대신 입력을
정규화하고 병합한 뒤에는 출처, 대상 종류, 필요한 기능, 수량, 공급 시점과 씬 범위를 갖춘 엄격한
manifest를 만들며, 모호하거나 충족되지 않은 항목을 명시적인 진단으로 출력한다.

핵심 파이프라인은 다음과 같다.

```text
.scenario.json ── inferred provider ─┐
                                     │
AI/수동 declaration ─ declaration ──┼─> normalize/merge/compile
                                     │            │
scene bindings ───── binding ────────┘            v
                                           compiled manifest
                                                    │
                         ┌──────────────────────────┼──────────────────────┐
                         v                          v                      v
                   Editor validator          Build validator       Runtime preflight
                         │                          │                      │
                         v                          v                      v
                  bind/create/apply          build gate/report      start gate/report
```

기능의 최종 책임은 다음과 같다.

> 시나리오에 적힌 문자열 identifier를 실제 실행 가능한 Unity 환경 계약으로 변환하고, 그 계약의
> 추출 근거, 씬 연결, 생성 방식과 검증 결과를 한 곳에서 추적한다.

### 해결하려는 문제 상황

현재 시나리오 실행은 `.scenario.json`과 Unity 씬 사이의 암묵적인 약속에 의존한다. 예를 들어
`NPCMove.npcIdentifier = "npc-1"`이면 실행 시점에 `npc-1`이 Registry에 등록된 NPC 또는 Entity로
존재해야 한다. `QuestWaypointHighlight.waypointIdentifier`는 단순 위치 값이 아니라 실제
`WaypointAnchor`를 요구한다. 이 약속은 JSON만 읽어서는 완전히 드러나지 않으며 씬에 잘못 배치된
경우 대부분 실행 중 경고, 건너뛰기 또는 대기로 나타난다.

현재 기반 기능에도 다음 한계가 있다.

- `ScenarioRequirementsCollector`는 일부 노드만 수집한다. `PlayerMove`, `NPCMove`, `Sound`, TTS,
  `TriageAssessControl`, 환자 상태 적용 등 실제 외부 의존성이 누락된다.
- 같은 `(kind, identifier)`를 여러 노드가 사용하면 최초 source만 남아 모든 사용 위치를 추적할 수 없다.
- 현재 kind는 필요한 실제 기능을 표현하지 못한다. Registry에 identifier가 있어도 필요한 component나
  interface가 없으면 실행은 실패할 수 있다.
- Editor 검사, preflight checker와 `ScenarioController`의 실제 lookup 경로가 일부 다르다.
- preflight는 현재 Registry 상태만 보므로 중복 identifier, 다른 씬 배치, 비활성 component,
  잘못된 타입과 생성 시점 문제를 충분히 진단하지 못한다.
- `OverworldGameObjectInitializer`는 고정된 세 항목만 처리하고 생성 루트 하위를 통째로 재구성한다.
  범용 시나리오 요구사항 편집기로 확장하기 어렵다.
- `ScenarioDevStubSpawner`는 identifier 이름으로 trigger 여부를 추측하며 개발 시연에는 유용하지만
  production authoring 계약으로 사용할 수 없다.
- AI가 요구사항을 식별해도 이를 받을 표준 데이터 계약, 검토 과정과 strict validation 경계가 없다.
- 현재 scenario JSON schema validator의 "조건부 오류 우회"(`IsConditionalNodeTypeNoiseOnly`)는 이름과
  달리 `EvaluationResults`를 사용하지 않고, 모든 `nodeType`이 known이면 필수 필드 누락과 타입 오류까지
  포함한 schema 결과 전체를 무시한다. 게다가 schema root `nodeType` enum에는 `PatientMedicalStatePreset`,
  `ItemSubmissionConfig`, `NpcInteractControl`이 빠져 있어, 이 노드를 쓰는 기존 시나리오가 사실상 이
  우회에 의존해 통과하고 있다. 따라서 잘못된 입력으로부터 strict manifest를 만드는 기반으로 바로 사용할
  수 없으며, 우회 제거는 schema enum 보강과 함께 순서대로 진행해야 한다(Phase 0 참고).

사용자 관점의 문제는 다음과 같다.

- 시나리오 콘텐츠 제작자는 실행 전에 씬에 무엇이 필요한지 전체 목록으로 볼 수 없다.
- 씬 제작자는 `npc-1`에 어떤 component와 registry 등록 방식이 필요한지 JSON 문자열만으로 알기 어렵다.
- QA는 오류가 시나리오 데이터, 씬 배치, Registry lifecycle 또는 런타임 생성 순서 중 어디에 있는지
  빠르게 분리하기 어렵다.
- AI 에이전트는 후보를 만들 수 있지만 불확실한 추정을 안전하게 전달할 형식이 없다.
- 빌드는 시나리오와 씬의 불일치를 차단하지 않으므로 결함이 런타임까지 전달된다.

### 사용자 경험 목표

#### 시나리오 콘텐츠 제작자

- `.scenario.json`을 선택하면 시나리오가 요구하는 항목이 하나의 목록으로 나타난다.
- 각 항목에서 kind, identifier, 필요한 capability, 공급 시점, 모든 source node와 field path를 확인한다.
- 누락된 항목과 정적으로 판정할 수 없는 항목을 구분한다.
- 시나리오를 수정한 뒤 요구사항이 stale 상태인지 즉시 확인하고 다시 추출한다.

#### 씬 제작자

- 요구사항을 이미 존재하는 씬 오브젝트에 연결할 수 있다.
- 안전하게 생성 가능한 항목은 생성 계획을 미리 보고 명시적으로 적용할 수 있다.
- 자동 생성 불가능한 NPC나 프로젝트 전용 대상은 factory 또는 prefab을 선택한다.
- 잘못된 타입, 중복 identifier, 다른 씬 소속과 비활성 상태를 구체적인 수정 지침과 함께 확인한다.
- 한 번의 Apply를 한 번의 Undo로 되돌릴 수 있다.

#### QA와 빌드 관리자

- Editor, CI build, runtime preflight가 같은 requirement rule과 diagnostic code를 사용한다.
- production profile에서는 unresolved Error가 있는 시나리오 또는 scene composition의 빌드를 차단한다.
- runtime-produced signal과 같이 증명할 수 없는 항목은 Missing과 구분된 `Indeterminate`로 확인한다.
- 동일 입력에서 진단 순서와 결과가 결정적으로 재현된다.

#### AI 에이전트 사용자

- 고정된 JSON Schema와 prompt 계약으로 requirement declaration 후보를 생성한다.
- AI 결과는 import preview와 schema validation을 거치며 즉시 씬을 수정하지 않는다.
- AI가 확정할 수 없는 prefab, 좌표, scene role은 `reviewRequired` 또는 unresolved 값으로 남긴다.
- 사용자가 승인한 정보만 canonical declaration에 반영된다.

### 제안

#### 1. 네 개의 독립 모델을 둔다

시스템은 Requirement, Declaration, Binding, Diagnostic을 분리한다.

| 모델 | 책임 | Unity object 참조 |
|---|---|---|
| Requirement | 시나리오가 무엇을 요구하는지 표현 | 금지 |
| Declaration | 추론만으로 알 수 없는 scope/factory/생성 정보를 보충 | 금지 |
| Binding | requirement를 실제 scene object에 연결 | scene-local 참조 허용 |
| Diagnostic | 특정 context에서 계약이 충족되는지 설명 | 진단 대상 참조 선택 허용 |

이 분리는 `.scenario.json`만으로 알 수 없는 prefab, 좌표와 실제 씬 오브젝트 참조를 시나리오 실행
데이터에 섞지 않기 위해 필요하다.

#### 2. Requirement identity와 occurrence를 분리한다

Requirement는 다음 tuple을 canonical identity로 사용한다.

```text
(kind, normalizedIdentifier)
```

`scope`와 `authority`는 identity가 아니라 적용 제약이다. 따라서 inferred 항목의 기본 제약을
declaration이 `Overworld/Server`처럼 구체화할 수 있다. 동일 key에 양립할 수 없는 제약이 선언되면
별도 requirement를 암묵적으로 만들지 않고 compile conflict로 보고한다.

- 문자열은 앞뒤 공백을 제거한다.
- 빈 문자열은 조용히 버리지 않고 `SIR100 InvalidIdentifier` 오류 occurrence를 만든다.
- identifier 비교는 현재 Registry와 맞추어 `StringComparer.Ordinal`을 사용한다.
- 대소문자만 다른 identifier는 자동 병합하지 않고 충돌 가능성 경고를 낸다.
- 같은 requirement를 사용하는 모든 source occurrence를 보존한다.

각 occurrence는 최소한 다음 값을 갖는다.

```text
scenarioIdentifier
nodeIdentifier
nodeType
fieldPath
usage
sourceOrigin
direction
availability
expectedSupply
```

예를 들어 같은 `treatment-room` 위치가 두 이동 노드와 한 highlight 노드에서 사용되면 하나의
`SpatialAnchor` requirement에 세 occurrence가 붙고 `ProvidesPosition`과
`HighlightableWaypoint` capability가 합쳐진다.

#### 3. Kind와 Capability를 분리한다

`Kind`는 identifier가 가리키는 논리적 대상을 나타내고, `Capability`는 그 대상이 실제로 제공해야
하는 기능을 나타낸다.

초기 kind 집합:

- `Entity`
- `Npc`
- `Interactable`
- `SpatialAnchor`
- `SpawnPoint`
- `EntityPreset`
- `ItemDefinition`
- `EventHandler`
- `RegistryEntry`
- `RuntimeSignal`
- `AudioResource`
- `SpriteResource`
- `TtsTranscript`
- `TtsVoice`
- `QuestDefinition`
- `PlayerTagState`
- `RuntimeEntityReference`
- `Service`

초기 capability 집합:

- `RegisteredEntity`
- `ResolvableNpcMoveTarget`
- `RegisteredNpcComponent`
- `ProvidesPosition`
- `HighlightableWaypoint`
- `Interactable`
- `ToggleableInteractable`
- `ItemSubmissionTarget`
- `ScenarioEntityInitTarget`
- `ScenarioTriageAssessTarget`
- `PatientMedicalStateTarget`
- `SpawnablePreset`
- `InvokableEventHandler`
- `LoadableResource`
- `ResolvableQuestDefinition`

Kind나 capability enum을 sidecar의 프로젝트별 새 문자열로 임의 확장하지 않는다. 프로젝트별 구현은
새 kind가 아니라 factory/provider가 표준 capability를 공급하는 방식으로 확장한다. 새 의미가 정말
필요하면 MultiplayerInfrastructure의 versioned schema와 enum에 명시적으로 추가한다.

#### 4. Availability를 명시한다

요구사항은 단순히 존재/부재만으로 판단하지 않고 언제 공급되어야 하는지 표현한다. Availability는
각 occurrence에 기록한다. Descriptor의 `EffectiveAvailability`는 모든 소비 occurrence 중 가장 이른
필요 시점이며, 생산 occurrence는 `Direction = Produces`로 별도 보존한다.

| Availability | 의미 |
|---|---|
| `BeforeScenarioStart` | 시나리오 시작 전에 준비되어야 함 |
| `WhenNodeReached` | 해당 node 도달 전까지 준비되어야 함 |
| `OptionalFallback` | 없으면 명시된 runtime fallback을 사용 |
| `NotConsumed` | 데이터 필드는 있으나 현재 runtime이 실제 소비하지 않음 |

`BeforeScenarioStart`는 node별 추출 행렬에서 직접 발생하지 않는다. 이는 declaration의
`availability` override, scope role이 `Bootstrap`/`SystemOverlay`처럼 시작 전 준비되어야 하는 항목,
또는 `Availability = BeforeScenarioStart`가 기본인 well-known service requirement에서 나온다. 즉 개별
consumer node가 아니라 declaration/scope/service default가 이 값의 출처다.

생산 여부와 필요 시점은 분리한다. `direction`은 `Consumes` 또는 `Produces`, availability는 소비 또는
생산 node의 시점, `expectedSupply`는 `Scene`, `Scenario`, `Gameplay`, `External` 중 예상 공급원을
나타낸다. `expectedSupply = Scenario`는 단순 면제가 아니다. 후속 temporal validation에서 producer node와
consumer node 사이의 도달 가능성을 검사한다. 1차 구현은 이를 `Indeterminate`로 보고한다.

#### 5. Cardinality와 Scope를 명시한다

모든 requirement는 최소/최대 수량을 갖는다. 기본값은 kind별 rule이 제공한다.

```text
Npc/SpatialAnchor/Entity identifier: exactly 1
EventHandler: at least 1
Service: exactly 1 per applicable authority
RuntimeSignal: cardinality validation 없음
```

Scope는 scene name 문자열이 아니라 표준 role과 선택적 scene composition을 사용한다.

초기 role:

- `AnyLoadedScene`
- `Bootstrap`
- `SystemOverlay`
- `Overworld`
- `Content`
- `DontDestroyOnLoad`

실제 scene asset path/GUID와 role의 연결은 프로젝트별 `ScenarioSceneCompositionProfile`에서 관리한다.
MultiplayerInfrastructure는 `OverworldScene` 같은 TriageTrainer의 구체 scene 이름을 알지 않는다.
Authoring에서는 profile 부재 시 제한적인 `AnyLoadedScene` 진단을 허용하지만 Development/Production
build에서는 scenario에 정확히 하나의 composition profile이 연결되어야 한다. profile 부재나 중복은
build Error다.

Authority는 `Any`, `Server`, `Client`, `HostOnly`를 지원한다.

- dedicated server: `Any`, `Server`
- remote client: `Any`, `Client`
- host server pass: `Any`, `Server`
- host client pass: `Any`, `Client`
- host 통합 공급자 전용: `HostOnly`

Host에서는 server/client pass를 분리하며 동일한 `Any` 진단은 report 단계에서 축약한다.

#### 6. Requirement rule을 실행 의미의 단일 정의로 사용한다

각 rule은 다음 책임을 가진다.

- 특정 node에서 requirement occurrence 추출
- 기본 kind, capability, cardinality와 availability 결정
- Editor snapshot에서 후보 공급자 탐색
- runtime context에서 실제 존재와 capability 검사
- 생성 가능한 경우 사용할 factory category 제안
- diagnostic code와 수정 힌트 생성

Editor와 runtime이 서로 다른 lookup 의미를 갖지 않도록 rule의 resolution 로직을 공유한다. UnityEditor
API가 필요한 scene scan과 생성은 Editor adapter에 격리한다.

노드별 상세 추출 행렬과 파일별 구현은
[`implementation-blueprint.md`](./implementation-blueprint.md)에 정의한다.

#### 7. Sidecar 파일을 canonical authoring 계약으로 사용한다

파일 배치는 다음과 같다.

```text
disaster_intro.scenario.json
disaster_intro.scenario.requirements.json
disaster_intro.scenario.editor.json
```

- `.scenario.json`: 실행 그래프
- `.scenario.requirements.json`: 사람이 승인한 보충 declaration
- `.scenario.editor.json`: 그래프 레이아웃

자동 추출 결과는 source JSON에서 결정적으로 재생성할 수 있으므로 tracked 파일로 저장하지 않는다.
version 1은 generated ScriptableObject catalog를 두지 않고 graph와 sidecar를 compile한 immutable
manifest를 process-local cache에 둔다.

manifest lookup key는 `(scenarioIdentifier, graphFingerprint)`다. `graphFingerprint`는 domain graph의
canonical JSON UTF-8 bytes에 대한 SHA-256으로 계산한다.

- Resources scenario는 같은 Resources subtree의 sidecar를 함께 발견한다.
- scenario TextAsset을 직접 받는 component에는 선택적 requirements TextAsset reference를 추가한다.
- 동일 identifier라도 fingerprint가 다르면 다른 manifest로 취급한다.
- sidecar 없는 graph와 메모리 생성 graph는 inferred-only manifest를 runtime에서 compile한다.
- 같은 lookup key의 sidecar가 둘 이상이면 ambiguity Error다.
- Player build에는 Resources sidecar 또는 serialized TextAsset reference로 계약이 포함된다.

sidecar를 선택한 이유:

- scenario root schema의 `additionalProperties: false`와 기존 DTO를 불필요하게 확장하지 않는다.
- AI/씬 배치 정보와 실행 그래프의 변경 주기를 분리한다.
- 자동 추출을 재실행해도 수동 authoring 정보를 잃지 않는다.
- 다른 프로젝트가 같은 scenario graph에 서로 다른 scene binding을 제공할 수 있다.

sidecar의 상세 JSON 계약은
[`requirements-data-contract.md`](./requirements-data-contract.md)에 정의한다.

#### 8. 출처와 병합 우선순위를 고정한다

출처는 다음 값을 사용한다.

| 출처 | 의미 | build-blocking 여부 |
|---|---|---|
| `Inferred` | scenario node에서 rule이 자동 추출 | 가능 |
| `AiImported` | AI 파일에서 가져왔지만 미승인 | 불가, 최대 Warning |
| `Declared` | 사람이 승인한 sidecar 값 | 가능 |
| `Override` | declaration의 `operation: Override`로 inferred 제약 구체화 | 가능 |
| `Suppressed` | 사유를 갖고 검증에서 제외 | 만료/형식 검증 대상 |

병합 우선순위는 `Override > Declared > Inferred`로 고정한다. `Declared`는 inferred에 없는 공급 계약을
추가하고 `Override`는 같은 `(kind, identifier)`의 scope, authority, cardinality, availability 또는
binding hint를 구체화한다. 둘은 sidecar의 `operation` 필드로 구분한다. `AiImported`는 승인 전에는
canonical compile에 참여하지 않는다.

다음 값은 자동 합친다.

- occurrence/source 목록: 합집합
- capability: 합집합
- 최소 cardinality: 모든 제약의 `max(minimum)`
- 최대 cardinality: bounded maximum의 `min(maximum)`, 모두 unbounded면 unbounded

결과가 `minimum > maximum`이면 `SIR204 ImpossibleCardinality` Error다.

다음 충돌은 자동 결정하지 않고 compile Error로 보고한다.

- 동일 requirement에 서로 다른 고정 scene role
- 서로 다른 factory identifier
- 양립할 수 없는 authority
- `Ignored`와 필수 inferred occurrence의 충돌
- 동일 selector를 가진 중복 override

#### 9. AI는 declaration 후보 producer로 제한한다

AI import pipeline은 다음 순서를 지킨다.

```text
AI JSON
 -> syntax/schema validation
 -> allowlist validation
 -> normalized candidate model
 -> inferred/declared diff preview
 -> human approval
 -> canonical sidecar update
```

AI가 할 수 있는 일:

- requirement 후보와 근거 node/field 제시
- kind/capability/factory 후보 제안
- scene role, prefab, 위치가 불확실함을 명시
- 사람이 확인해야 할 설명 작성

AI가 할 수 없는 일:

- 임의 C# 타입명을 reflection으로 생성
- 존재하지 않는 asset GUID 또는 prefab을 존재한다고 확정
- 근거 없는 좌표를 승인 값으로 기록
- Error를 자동 suppress
- import와 동시에 scene 수정
- 사용자 승인 없이 production build 계약으로 승격

AI 입력의 `confidence`는 검증 severity를 낮추는 데 사용하지 않는다. 단지 review 정렬과 설명에만
사용한다.

#### 10. Scene binding은 scene-local component로 저장한다

JSON DTO에는 `GameObject`, `Component`, `Scene`, Unity instance ID 또는 `GlobalObjectId`를 저장하지 않는다.
실제 scene object 참조는 각 scene에 배치된 `ScenarioRequirementsSceneBinding`이 보유한다.

```text
Scenario requirement key
 -> scene-local binding entry
 -> target GameObject/Component
 -> runtime registration/provider
```

additive scene 사이의 serialized reference를 만들지 않는다. 각 scene binding은 자기 scene 안의 target만
직접 참조할 수 있다. 다른 scene의 공급자는 identifier와 composition snapshot으로 합성한다.

binding은 실제 기능 component를 대체하지 않는다. 예를 들어 `Waypoint` binding marker만 있다고
충족되는 것이 아니라 `WaypointAnchor`, identifier, 활성 상태와 필요한 highlight capability를 검사한다.

#### 11. Editor 도구는 목록, 계획, 적용을 분리한다

메뉴:

```text
Tools/Multiplayer Infrastructure/Scenario Ingame Requirements
```

주요 단계:

1. Scenario와 scene composition 선택
2. Extract/Compile
3. 요구사항 목록과 진단 확인
4. 기존 오브젝트 binding 또는 factory 선택
5. 변경 계획 Preview
6. 명시적 Apply
7. 재검증
8. 사용자가 scene 저장

목록은 status, kind, identifier, capability, availability, binding mode, scene role과 source를 표시한다.
항목 상세에서는 모든 occurrence와 실제 후보 오브젝트를 보여주며 source node를 Scenario Graph Editor에서
열 수 있어야 한다.

지원 binding mode:

- `ExistingSceneObject`
- `GeneratedSceneObject`
- `PrefabInstance`
- `RegistryProvided`
- `RuntimeProduced`
- `External`
- `Suppressed`

#### 12. 생성은 factory allowlist와 증분 소유권을 사용한다

공용 factory 예:

- `WaypointAnchorFactory`
- `EmptyEntityAnchorFactory`
- `SpawnPointProviderFactory`
- 개발 전용 `InteractionStubFactory`
- 개발 전용 `TriggerZoneStubFactory`

프로젝트 factory 예:

- TriageTrainer NPC prefab factory
- 환자 prefab factory
- 특정 치료구역 interactable factory

MultiplayerInfrastructure는 프로젝트 prefab을 직접 참조하지 않는다. 프로젝트 module이 표준
`IScenarioWorldObjectFactory`를 등록한다.

생성물에는 `ScenarioGeneratedWorldObject` marker를 붙이고 다음 값을 기록한다.

```text
manifest/scenario identifier
requirement key
factory identifier
generator version
target scene role
```

재적용은 생성 루트 전체를 삭제하지 않는다.

- 같은 requirement key의 생성물은 업데이트 후보로 제시한다.
- declaration에서 제거된 생성물은 orphan으로 표시한다.
- 사용자 수정이나 수동 자식이 있으면 자동 삭제하지 않는다.
- 삭제와 교체는 Preview에 나타내고 승인을 요구한다.
- manual object는 generator 소유로 간주하지 않는다.

Apply는 하나의 Undo group으로 묶고 target scene을 명시한다. active scene에 암묵적으로 생성하지 않으며,
Prefab Stage에서는 기본적으로 Apply를 금지한다. build validation은 생성이나 수정 작업을 절대 수행하지
않는다.

#### 13. Validation context를 분리한다

같은 rule을 사용하되 context별로 얻을 수 있는 증거가 다르다.

| Context | 데이터 원천 | 주요 목적 |
|---|---|---|
| Import | JSON/DTO | 문법과 declaration 계약 검사 |
| Compile | graph + declarations | 병합, stale, conflict 검사 |
| Editor Scene | scene/asset snapshot | 배치, 타입, 수량, scope 검사 |
| Build | build scene composition | production 계약 gate |
| Runtime | loaded scene + Registry | 실제 등록과 readiness 검사 |

표준 상태:

- `Satisfied`
- `Missing`
- `Duplicate`
- `WrongType`
- `MissingCapability`
- `WrongScene`
- `Inactive`
- `NotRegistered`
- `NotReady`
- `Indeterminate`
- `Suppressed`
- `Malformed`
- `Stale`
- `NotConsumed`

Severity는 `Info`, `Warning`, `Error`, `Fatal`을 사용한다. 모든 진단은 안정적인 code, requirement key,
source occurrence와 수정 힌트를 갖는다.

| Status | Authoring | Development | Production |
|---|---|---|---|
| Missing/Duplicate/WrongType/MissingCapability/Malformed | Error | Error | 차단 |
| WrongScene/Inactive/NotRegistered | Warning 또는 Error | Error | 차단 |
| NotReady | 정보성 | runtime timeout 전 재시도 | runtime timeout 후 차단 |
| Indeterminate | 정보성 | Warning | Warning, `mustProve=true`만 차단 |
| NotConsumed | 정보성 | 차단 안 함 | 차단 안 함 |
| Suppressed | 사유/만료 표시 | 유효하면 차단 안 함 | 유효하면 차단 안 함 |

진단 code namespace는 `SIR`을 사용한다.

```text
SIR1xx input/normalization
SIR2xx compile/merge
SIR3xx scene binding
SIR4xx capability/cardinality
SIR5xx build composition
SIR6xx runtime readiness
SIR7xx AI review
```

#### 14. Build strict validation은 읽기 전용이다

빌드 검증은 `IPreprocessBuildWithReport`와 필요 시 `IProcessSceneWithReport`를 사용한다.

- 모든 scenario와 sidecar의 schema/compile 결과를 검사한다.
- build scene composition에 필요한 scene이 포함되었는지 검사한다.
- 각 scene의 공급자 snapshot을 합성하여 cardinality와 capability를 판정한다.
- 첫 오류에서 멈추지 않고 가능한 전체 오류를 수집한다.
- 결과를 stable code와 deterministic order로 출력한다.
- CI batch mode에서는 dialog를 사용하지 않는다.
- 에셋 생성, 다운로드, scene 저장, `AssetDatabase.Refresh`를 수행하지 않는다.
- Error가 있으면 마지막에 `BuildFailedException`으로 실패시킨다.

검증 profile:

- `Authoring`: Warning 중심, Apply 가능
- `Development`: Error 보고, 빌드 차단 선택 가능
- `Production`: unresolved Error/Fatal 빌드 차단

runtime code registration은 `IScenarioRequirementStaticProvider` 또는 프로젝트 catalog가 공급 identifier,
capability와 authority를 부작용 없이 열거하여 build-time 증거를 제공한다. runtime 등록만 존재하면
`Indeterminate`이며 기본 Production profile에서도 Warning이다. declaration의 `mustProve=true`는 이를
Error로 승격한다.

#### 15. Runtime preflight는 compiled manifest를 사용한다

런타임에서 매번 새로운 규칙으로 graph를 해석하지 않는다. 가능하면 Editor/build에서 생성된 compiled
manifest를 로드하고 현재 Registry와 loaded scene readiness만 확인한다. sidecar가 없는 기존 scenario는
runtime inferred compilation fallback을 허용한다.

runtime 실행 모드:

- `Off`
- `ReportOnly`
- `AbortScenarioStart`
- `AbortSessionBootstrap`

additive scene load가 완료되고 `Awake`/`OnEnable` 등록이 끝난 이후 validation해야 한다. 구체 bootstrap
모듈이 MultiplayerInfrastructure의 public validation API를 호출하며, requirements core가
`UnitySceneSupports`나 TriageTrainer를 역참조하지 않는다.

새 시나리오 시작 요청에 대한 side-effect 없는 validation은 현재 실행 중인 시나리오를 정리하기 전에
수행한다. 검증 실패 때문에 기존 실행이 먼저 중단되는 동작을 피한다.

#### 16. 기존 Preflight를 facade로 마이그레이션한다

최종 관계:

```text
ScenarioRequirementsCollector -> inferred provider compatibility facade
ScenarioRequirementsChecker   -> runtime validator compatibility facade
ScenarioPreflight              -> compiled manifest runtime entry point
ScenarioDevStubSpawner         -> development factory adapter
OverworldGameObjectInitializer -> project declaration/factory migration 대상
```

초기에는 기존 public API와 serialized `ScenarioPreflightPolicy`(현재 `[Serializable] struct`, modes enum은
`ScenarioPreflightMissingBehavior`)를 유지한다. 기존 `ScenarioRequirementsCollector`가 `(kind, identifier)`
중복 시 첫 source만 남기고 나머지를 버리는 현재 동작과 달리, 새 compiler는 모든 occurrence를 보존한다.
새 결과를 기존 `ScenarioRequirement` 형태로 projection하여 하위호환하되(단일 `SourceNodeIdentifier`로
축약), 새 Editor와 build 경로는 canonical model을 직접 사용한다.

### 자세한 달성 목표

#### 기능 목표

1. 모든 현재 Scenario node type의 외부 identifier 소비를 추출 행렬로 관리한다.
2. 같은 requirement를 사용하는 모든 source node와 field path가 보존된다.
3. 존재 여부뿐 아니라 실제 runtime이 요구하는 component/interface capability를 판정한다.
4. `.scenario.requirements.json`을 schema로 검증하고 inferred 결과와 결정적으로 병합한다.
5. AI 후보는 review/approval 전 production 계약에 참여하지 않는다.
6. Editor에서 전체 요구사항 목록, scene 후보, binding과 진단을 한 번에 확인한다.
7. 안전한 항목은 Preview/Apply/Undo가 가능한 factory로 생성한다.
8. additive scene composition 전체를 하나의 환경으로 검증한다.
9. Editor, build, runtime이 같은 canonical requirement와 diagnostic code를 사용한다.
10. 기존 scenario와 preflight는 sidecar가 없어도 계속 동작한다.

#### 비목표

- AI가 임상적으로 올바른 씬 구성을 자동 판단하는 기능이 아니다.
- 모든 requirement를 자동 prefab으로 생성하는 기능이 아니다.
- 사용자 씬 전체를 자동 복구하거나 수동 배치를 덮어쓰지 않는다.
- cross-scene serialized reference를 새로 지원하지 않는다.
- FishNet scene management를 대체하지 않는다.
- runtime signal이 미래에 반드시 발생할 것을 정적으로 증명하지 않는다.
- graph edge 무결성 검증 자체를 requirement occurrence로 변환하거나 capability/cardinality 판정에
  혼합하지 않는다. 이는 별도 structural validation이다. 다만 malformed graph에서 strict manifest를
  신뢰할 수 없으므로 structural Error는 compiled manifest의 유효성을 무효화하고 build/runtime strict
  gate에서 함께 보고한다. structural diagnostics는 requirement diagnostics(`SIR`)와 구분되는
  `SGR` namespace를 사용한다.
- Registry 전체를 이 기능 안에서 즉시 리팩터링하지 않는다.
- build validation 중 asset 생성, 다운로드 또는 자동 저장을 하지 않는다.
- `ScenarioDevStubSpawner`의 이름 기반 trigger 추측을 production 생성 규칙으로 사용하지 않는다.
- camera target처럼 runtime이 현재 소비하지 않는 필드를 존재 요구사항으로 거짓 강화하지 않는다.

### 구현 단계

#### Phase 0. 선행 정합성 수정

이 단계는 **엄격한 순서**가 있다. 순서를 지키지 않으면 정상 콘텐츠가 회귀로 거부된다.

1. **먼저** scenario schema root `nodeType` enum과 per-node `if/then` 브랜치에 누락 node type을 추가한다.
   현재 schema root enum(`scenario.schema.json`)은 28종만 나열하며 다음 3종이 빠져 있다.
   - `PatientMedicalStatePreset`
   - `ItemSubmissionConfig`
   - `NpcInteractControl`

   이 3종은 DTO converter(`ScenarioNodeDTOConverter`)와 validator의 `knownTypes`(31종)에는 있으나
   schema root enum에 없다. root가 `additionalProperties: false`이므로, 현재 이 노드를 쓰는 시나리오는
   **오직 아래 2번의 우회 덕분에** 통과하고 있다. 우회를 먼저 제거하면 정상 콘텐츠가 즉시 거부된다.
   `ChatPrint`/`ExecuteCommand`처럼 schema enum에는 있으나 per-node `if/then` 브랜치가 없어 필드 검증이
   비어 있는 node도 이 단계에서 브랜치를 채운다.
2. schema가 3종을 정식 검증하게 된 **후에** `ScenarioJsonSchemaValidator`의 우회를 제거하거나 실제 오류
   위치와 keyword에 한정한다. 현재 `IsConditionalNodeTypeNoiseOnly`는 이름과 달리 `EvaluationResults`를
   사용하지 않고, 모든 `nodeType`이 known이면 필수 필드 누락·타입 오류·잘못된 속성을 포함한 **모든
   schema 오류를 통째로 무시**한다. conditional(if/then/anyOf/oneOf) noise에 한정하는 것이 목표라면
   실제 실패 keyword/instance path 기준으로 좁혀야 한다.
3. 현재 node별 runtime lookup 행렬을 테스트 가능한 문서와 코드 등록 구조로 확정한다.

완료 조건: schema root enum·per-node 브랜치·DTO converter·validator known types가 일치하고, 3종 노드를
쓰는 기존 시나리오가 우회 없이 valid로 통과하며, 잘못된 scenario JSON은 requirement compiler 진입 전에
결정적으로 거부된다. Phase 0 전후로 기존 정상 시나리오 fixture의 검증 결과가 회귀하지 않는다.

#### Phase 1. Canonical model과 inferred compiler

- immutable domain model, occurrence, capability, availability, cardinality와 diagnostic을 추가한다.
- 모든 현재 node type의 rule 또는 extractor를 구현한다.
- source 보존과 deterministic merge를 구현한다.
- 기존 collector/checker를 facade로 연결한다.

완료 조건: 현재 scenario fixture에서 누락 없이 requirements를 추출하고 snapshot 테스트가 통과한다.

#### Phase 2. Sidecar와 AI import preview

- version 1 JSON Schema, DTO, loader와 compiler를 추가한다.
- source hash/stale 검사를 추가한다.
- AI candidate import, diff, 승인 workflow를 추가한다.
- 이 단계에서는 scene을 수정하지 않는다.

완료 조건: invalid/unknown/conflicting declaration이 stable diagnostic으로 거부되고 승인된 값만 sidecar에
저장된다.

#### Phase 3. Editor scene scanner와 binding

- target scene composition snapshot을 만든다.
- duplicate, wrong type, missing capability, inactive와 wrong scene을 검출한다.
- scene-local binding component와 EditorWindow를 추가한다.
- runtime static Registry를 Editor 검증의 근거로 사용하지 않는다.

완료 조건: 열린 additive scene과 닫힌 scene asset 검증 결과가 동일한 계약을 따른다.

#### Phase 4. Factory와 initializer

- 공용 생성 가능한 kind의 allowlist와 factory registry를 추가한다.
- Preview/Apply, target scene 이동, atomic Undo와 orphan 처리를 구현한다.
- 기존 Overworld initializer의 항목을 TriageTrainer declaration/factory 예제로 이전한다.
- 기존 initializer는 즉시 삭제하지 않고 migration command를 제공한다.

완료 조건: 재적용이 idempotent하고 수동 오브젝트를 삭제하지 않으며 Undo 한 번으로 복원된다.

#### Phase 5. Build strict validation

- composition profile과 read-only build validator를 추가한다.
- Production profile에서 unresolved Error를 차단한다.
- CI용 machine-readable report를 선택적으로 출력한다.

완료 조건: clean checkout batch build에서 진단 결과가 재현되고 검증이 파일을 변경하지 않는다.

#### Phase 6. Runtime strict migration

- compiled manifest 로딩과 runtime provider snapshot을 추가한다.
- additive load completion hook 이후 readiness를 검사한다.
- 기존 preflight policy를 새 runtime mode로 매핑한다.
- `ReportOnly` 관찰 기간 후 opt-in abort 정책을 활성화한다.

완료 조건: Editor/build/runtime의 동일 fixture가 같은 requirement key와 core diagnostic을 출력한다.

각 Phase는 별도 issue와 merge request로 승인한다. Phase 0-1은 extraction core, Phase 2는 data/AI,
Phase 3-4는 scene authoring, Phase 5는 build gate, Phase 6은 runtime lifecycle 변경이다.

### 문서화

구현 시 다음 문서를 추가 또는 갱신한다.

- `Documents/requirements/scenario/`: 사용자 관점 기능 요구사항
- `Documents/working-guide/features/scenario/`: Editor 사용, AI import, binding/apply, CI 설정 가이드
- `Documents/api-references/`: canonical model, rule/provider API, validator, factory API
- `Documents/guide/ScenarioGraph.md`: scenario sidecar와 runtime preflight 관계
- JSON Schema 파일 내 description과 version migration note
- diagnostic code 목록과 해결 방법

문서 작성 후 `Documents/requirements/README.md`와 하위 색인을 갱신하고
`Tools/validate-documentation-links.sh`를 실행한다.

### 가용성과 테스트

#### 주요 위험

| 위험 | 영향 | 완화 |
|---|---|---|
| rule과 실제 runtime lookup 불일치 | false pass/false fail | runtime lookup 행렬 테스트와 공용 resolver 사용 |
| additive scene 미로딩 상태 검증 | false Missing | composition과 readiness phase 분리 |
| Registry last-write-wins, owner metadata 부재 | 중복이 정상처럼 보이고 runtime에서 증거 identity 병합 불가 | duplicate 판정 정본을 Editor/build scene snapshot에 두고, runtime은 병합 불가 시 Indeterminate로 강등. owner-aware Registry는 별도 proposal(§13) |
| domain reload 비활성화 | stale static entry | runtime static reset 및 반복 PlayMode 테스트 |
| AI의 잘못된 추정 | 잘못된 scene mutation | preview, allowlist, 승인 전 non-blocking |
| 자동 생성이 수동 작업 삭제 | authoring 손실 | 증분 marker, orphan preview, atomic Undo |
| build validator mutation | CI 비결정성 | read-only contract와 파일 변경 검사 |
| MI와 TriageTrainer 결합 | 재사용성 저하 | project factory/provider를 역참조하지 않음 |
| 초기 strict 적용 | 기존 콘텐츠 빌드 차단 | 관찰→Editor→CI opt-in→Production 단계 도입 |

#### 필수 테스트 범주

- 순수 EditMode: 모든 node extractor, normalization, occurrence merge, conflict, deterministic ordering
- JSON: unknown kind/property/version, duplicate property, null/empty, Unicode, stale source hash
- Scene EditMode: additive composition, duplicate ID, inactive, wrong component, wrong scene, prefab instance
- Editor generation: Preview/Apply, Undo/Redo, idempotency, partial failure, orphan과 manual edit
- Prefab Stage: Main Stage 외 Apply 차단, prefab asset 오인 방지
- PlayMode: domain/scene reload 비활성 반복, scene unload/reload, stale Registry 제거
- 네트워크: host, dedicated server, remote client, server-only/client-only requirement
- Runtime strict: ReportOnly/Abort, current scenario 보존, readiness timeout, Indeterminate 처리
- Build: clean checkout, batch mode, missing scene, invalid sidecar, deterministic report, zero file mutation

상세 테스트 행렬은 [`implementation-blueprint.md`](./implementation-blueprint.md)에 정의한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

#### 성공 지표

- 시나리오 실행 전 모든 정적 외부 의존성을 하나의 목록으로 확인할 수 있다.
- runtime에서 처음 발견되던 scene wiring 오류가 Editor 또는 build 단계에서 발견된다.
- 같은 identifier의 다중 source와 capability 요구가 손실 없이 보고된다.
- AI가 생성한 declaration이 scene 변경 전에 schema와 사용자 검토를 통과한다.
- production build는 unresolved 필수 requirement를 차단한다.
- sidecar가 없는 기존 scenario의 동작은 변경되지 않는다.

#### 수용 기준

1. `NPCMove(npc-1, waypoint-a)`에서 NPC와 `SpatialAnchor`가 별도 requirement로 추출된다.
2. 같은 waypoint를 이동과 highlight가 함께 사용하면 `ProvidesPosition`과
   `HighlightableWaypoint` capability가 합쳐지고 모든 source가 표시된다.
3. identifier는 있으나 필요한 component가 없는 경우 `Satisfied`가 아니라
   `MissingCapability`가 출력된다.
4. 동일 identifier 공급자가 두 개이고 cardinality가 exactly one이면 Editor/build scene snapshot 판정에서
   `Duplicate` Error가 출력된다. runtime에서는 owner metadata가 없어 물리 공급자 identity를 합칠 수 없는
   경우 `Duplicate`로 단정하지 않고 `Indeterminate`로 보고한다. runtime duplicate 확정 강화는 owner-aware
   Registry API 이후로 미룬다.
5. runtime signal은 존재하지 않는 scene object로 오판되지 않고 `Indeterminate` 또는 producer 계약으로
   표시된다.
6. AI import는 승인 전에 sidecar와 scene을 변경하지 않는다.
7. Editor Apply는 변경 계획을 먼저 표시하고 한 번의 Undo로 전체 적용을 되돌린다.
8. Apply를 반복해도 같은 generated object가 중복 생성되지 않는다.
9. Build validator는 모든 Error를 수집한 뒤 실패하며 validation 전후 tracked file 내용이 동일하다.
10. Runtime strict 실패는 새 scenario를 시작하지 않되 기존 실행 중 scenario를 먼저 중단하지 않는다.
11. Editor, build와 runtime report가 같은 requirement key와 diagnostic code를 사용한다.
12. 이 기능으로 새로 추가되는 MultiplayerInfrastructure requirements 코드는 TriageTrainer 타입,
    prefab 또는 scene 이름을 참조하지 않는다. 기존 `ScenarioController`의 concrete 의존 제거는 별도
    리팩터링으로 다룬다.

### 링크, 참고사항

- 기존 요구사항: [`scenario-preflight-requirements.md`](../../../../Documents/requirements/scenario/scenario-preflight-requirements.md)
- 기존 구현 제안: [Scenario Preflight Requirements](../../done/2026-06-26-scenario-preflight-requirements/Feature Proposal - Scenario Preflight Requirements.md)
- 기존 운영 가이드: [`scenario-preflight-and-dev-stub-setup-guide.md`](../../../../Documents/working-guide/features/scenario/scenario-preflight-and-dev-stub-setup-guide.md)
- Scenario graph 가이드: [`ScenarioGraph.md`](../../../../Documents/guide/ScenarioGraph.md)
- 기존 collector: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Preflight/ScenarioRequirementsCollector.cs`
- 기존 checker: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Preflight/ScenarioRequirementsChecker.cs`
- 기존 initializer: `Assets/Modules/TriageTrainer/Editor/Utils/OverworldGameObjectInitializer/OverworldGameObjectInitializer.cs`
- 기존 개발 stub: `Assets/Modules/TriageTrainer/Scripts/Debug/ScenarioDevStubSpawner.cs`
