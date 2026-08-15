---
title: "시나리오 요구사항 명세 시스템 사용 가이드"
domain: "content-definitions.scenario"
progress: "3-implemented"
flags: []
---

# 시나리오 요구사항 명세 시스템 사용 가이드

## 이 시스템의 공식 명칭

이 기능의 공식 명칭은 **시나리오 요구사항 명세 시스템**이며, 영어 코드/문서에서는
**Scenario Requirements Specification System**이라고 한다. 약어를 새 식별자나 파일명에 쓰지 않고,
기존 `ScenarioRequirement*` 이름을 사용한다.

이 시스템은 시나리오 그래프가 외부 세계에 요구하는 NPC, 상호작용, 위치, 리소스, 퀘스트 정의 등을
**요구사항 명세(Requirement Specification)**로 만들고, 씬·에셋·런타임 공급자가 그 명세를 충족하는지 같은 규칙으로
확인한다. 시나리오 본문을 대신하는 도구가 아니라, 시나리오가 실행될 환경을 명확히 하는 도구다.

## 먼저 알아둘 이름

| 공식 용어 | 코드/파일 이름 | 뜻 |
|---|---|---|
| 시나리오 그래프 | `*.scenario.json`, `ScenarioGraph` | 요구사항을 추출하는 원본 실행 데이터 |
| 추론 요구사항 | inferred manifest | 그래프만 읽어 자동으로 얻은 요구사항 |
| 요구사항 manifest | `ScenarioRequirementManifest` | 추론 결과와 승인된 선언을 합친 정본 검증 결과 |
| 요구사항 | `ScenarioRequirementDescriptor` | 하나의 `(kind, identifier)` 명세 항목 |
| 요구사항 키 | `ScenarioRequirementKey` | kind와 공백 제거 identifier의 Ordinal 조합 |
| 사용 위치 | `ScenarioRequirementOccurrence` | 어느 node의 어느 field가 요구사항을 썼는지 기록 |
| canonical sidecar | `*.scenario.requirements.json` | 사람이 승인한 scope, binding, factory 등 보충 선언 |
| candidate 문서 | `*.scenario.requirements.candidates.json` | AI/수동 제안 입력. 승인 전에는 요구사항 명세·씬을 바꾸지 않음 |
| 씬 binding | `ScenarioRequirementsSceneBinding` | requirement를 실제 scene object에 연결하는 컴포넌트 |
| 구성 프로필 | `ScenarioSceneCompositionProfile` | scenario와 씬 path/GUID, role, 검증 정책의 연결 |
| 공급자 snapshot | `ScenarioRequirementProviderSnapshot` | 현재 씬·에셋·runtime 등록에서 수집한 공급 증거 |
| 정적 공급자 | `IScenarioRequirementStaticProvider` | Play Mode 없이 asset/catalog에서 공급자를 열거하는 인터페이스 |
| 생성 계획 | `ScenarioWorldObjectCreationPlan(Set)` | Apply 전에 보여 주는 생성·설정·orphan·삭제 작업 목록 |
| 생성 marker | `ScenarioGeneratedWorldObject` | 시스템이 만든 오브젝트의 소유자·factory·fingerprint 기록 |

`kind`는 대상의 논리적 종류(`Npc`, `Interactable`, `SpatialAnchor` 등)이고, `capability`는 대상이
실제로 제공해야 하는 기능(`ProvidesPosition`, `ItemSubmissionTarget` 등)이다. identifier만 같아도
capability가 부족하면 요구사항 명세는 충족되지 않는다.

## 전체 흐름

```text
scenario graph ──> 추론 요구사항 ──┐
sidecar 선언 ──────────────────────┼─> 요구사항 manifest ─> Editor / Build / Runtime 검증
candidate 제안 ─> preview + 승인 ──┘                         │
scene object / asset / registry ───────────> provider snapshot ┘
```

권장 순서는 다음과 같다.

1. 시나리오 그래프를 먼저 작성하고 identifier를 확정한다.
2. 아래의 통합 검증을 실행하여 자동 추출된 요구사항과 누락을 확인한다.
3. 그래프만으로 알 수 없는 씬 role, factory, 위치는 candidate 또는 sidecar로 보충한다.
4. candidate는 preview에서 evidence와 factory를 확인한 뒤 승인한다.
5. 기존 오브젝트는 binding으로 연결하고, 생성 가능한 항목만 generation preview 후 Apply한다.
6. 빌드 전에는 구성 프로필과 build validator 결과를 확인한다.

## 통합 검증 사용법

Unity에서 다음 메뉴를 사용한다.

**Tools > Multiplayer Infrastructure > Validate Scenario Requirements**

이것이 이 시스템의 유일한 수동 검증 진입점이다. 내부적으로 다음을 모두 실행한다.

- 그래프 추출, sidecar/candidate, scene scanner, generation/Undo smoke 검증
- runtime requirement registration 소유자 검증
- 모든 발견 scenario의 build/composition 검증

오류가 하나라도 있으면 통합 검증이 실패한다. 경고는 즉시 실행을 막지는 않지만 Production profile에서는
정책에 따라 build 오류가 될 수 있다. build가 실제로 실행될 때는 같은 build validator가 자동으로
`IPreprocessBuildWithReport` 경로에서 실행되며, output 폴더에
`scenario-requirements-build-report.json`을 남긴다.

## Authoring 절차

### 1. 후보 데이터 만들기

AI 또는 사람이 확실하지 않은 world 요구사항을 발견했을 때는 canonical sidecar 대신 candidate 문서를
만든다. 파일명은 다음과 같다.

```text
<scenario>.scenario.requirements.candidates.json
```

candidate에는 kind, identifier, 근거 node/field, confidence, `reviewRequired: true`를 넣는다. 좌표,
prefab, factory를 모르면 임의 값으로 채우지 않는다. 예를 들어 tutorial 데이터의
`delivery-storage-spot`은 `SpatialAnchor` 후보와 `mi.waypoint-anchor` 후보 factory까지만 제안하고,
position은 reviewer가 월드맵에서 확정한다.

candidate preview는 다음을 거부한다.

- graph에 없는 node/field를 evidence로 쓴 경우 (`SIR701`)
- 등록되지 않은 factory를 쓴 경우 (`SIR702`)
- 중복 candidate 또는 빈 identifier
- `RegistryProvided`처럼 후보 문서만으로 provider를 특정할 수 없는 binding

승인한 candidate만 `Declare` 또는 `Override` declaration으로 변환해 canonical sidecar에 넣는다.
candidate 자체는 runtime과 build 요구사항 명세에 참여하지 않는다.

### 2. Canonical sidecar 작성

파일명은 다음과 같고 scenario JSON과 같은 폴더에 둔다.

```text
<scenario>.scenario.requirements.json
```

중요 규칙은 다음과 같다.

- `scenarioIdentifier`는 graph identifier와 정확히 같아야 한다.
- `source.scenarioSha256`은 scenario 원문 bytes의 SHA-256이다.
- 자동 추출된 키를 보충할 때는 `Override`, 새 외부 요구사항 명세를 추가할 때는 `Declare`를 쓴다.
- `GeneratedSceneObject`/`PrefabInstance`는 `factoryIdentifier`가 필요하다.
- `ExistingSceneObject`는 JSON object reference가 아니라 다음 단계의 씬 binding으로 연결한다.
- unknown property, unknown enum, 중복 JSON property, 공백 identifier는 모두 오류다.

### 3. 씬 구성과 binding

`ScenarioSceneCompositionProfile`에서 이 scenario가 어떤 씬들을 어떤 role로 함께 로드하는지 설정한다.
role은 `Overworld`, `Bootstrap`, `Content`처럼 의미로 선택하며, scene 이름을 role로 쓰지 않는다.

이미 배치한 NPC·상호작용·waypoint는 **Scenario Requirements** 창에서 target을 선택해 binding한다.
binding은 같은 scene의 `ScenarioRequirementsSceneBinding`에 저장된다. 다른 scene object 또는 prefab asset을
연결하면 오류다.

프로젝트 catalog에서만 증명할 수 있는 항목은 `IScenarioRequirementStaticProvider`를 구현하거나
`ScenarioRequirementStaticProviderCatalog` asset에 넣는다. 이 경로는 build 중 runtime registry를
실행하지 않고도 공급 증거를 제공한다.

### 4. 생성 계획과 Apply

**Tools > Multiplayer Infrastructure > Scenario Ingame Requirements** 창에서 scenario, sidecar,
composition profile을 지정하고 **Compile and Validate**를 누른다. 생성 binding이 있는 requirement에
position과 target scene을 설정한 뒤 **Build Generation Preview**를 누른다.

계획 operation의 의미는 다음과 같다.

| Operation | 뜻 | Apply 동작 |
|---|---|---|
| `Create` | 새 generated object 필요 | factory가 새 object를 만든다 |
| `Configure` | 기존 generated object 갱신 | factory 설정을 다시 적용한다 |
| `MarkOrphan` | manifest에서 더는 쓰지 않음 | marker를 orphan으로 표시한다 |
| `DeleteGenerated` | orphan을 영구 삭제하도록 명시 승인 | Undo 가능한 삭제를 수행한다 |
| `NoChange` | 이미 orphan이지만 삭제 미승인 | 아무것도 바꾸지 않는다 |
| `Blocked` | 안전하게 판단할 수 없음 | Apply 전체를 막는다 |

삭제는 한 단계 절차다. 먼저 `MarkOrphan`을 Apply한 뒤, 다음 preview에서 해당 orphan의
**Approve permanent deletion**을 체크하고 preview를 다시 만든다. 그러면 `DeleteGenerated`가 보이며,
Apply가 삭제한다. 이 절차는 수동 배치나 살아 있는 generated object를 자동 삭제하지 않게 한다.

Apply는 Main Stage에서만 실행하고 한 번의 Undo group으로 되돌릴 수 있다. marker의
`ManifestFingerprint`은 graph manifest fingerprint, `GenerationInputFingerprint`은 생성 계획 입력값을
각각 기록한다. 두 값은 같은 값이 아니다.

## 검증 결과 읽기

| Status | 의미와 조치 |
|---|---|
| `Satisfied` | 하나의 물리 공급자가 필요한 capability와 cardinality를 충족함 |
| `Missing` | 필요한 공급자가 없음. scene/binding/catalog을 추가 |
| `MissingCapability` | identifier는 있으나 한 공급자가 요구 기능을 모두 제공하지 못함 |
| `Duplicate` | exact-one 요구사항 명세에 여러 물리 공급자가 있음 |
| `WrongScene` | 공급자가 있으나 구성 프로필의 role이 맞지 않음 |
| `Inactive` / `NotReady` | component가 비활성 또는 bootstrap 준비 전임 |
| `Indeterminate` | runtime 신호·legacy registry 등으로 정적으로 증명할 수 없음 |
| `Suppressed` | owner와 만료일이 있는 suppression으로 의도적으로 제외됨 |
| `NotConsumed` | 현재 runtime이 해당 필드를 소비하지 않음 |

`Indeterminate`는 `Missing`이 아니다. `mustProve: true` 또는 profile의 `failOnIndeterminate`가 있을 때만
차단 오류로 승격한다.

## Runtime 모드

`ScenarioController`의 runtime validation mode는 다음 중 하나다.

- `Off`: canonical runtime 검증을 실행하지 않는다. 기존 preflight 경로만 사용한다.
- `ReportOnly`: 결과만 기록한다.
- `AbortScenarioStart`: 오류 또는 준비 timeout이면 새 scenario를 시작하지 않는다.
- `AbortSessionBootstrap`: bootstrap 단계에서 같은 strict 정책을 적용한다.

strict 실패는 현재 실행 중인 scenario를 먼저 중단하지 않는다. 새 start 요청만 거부한다. sidecar가
없으면 inferred manifest로 fallback하지만, sidecar/schema/merge 오류가 있는 manifest는 strict 모드에서
시작을 막는다.

## 문제 해결 순서

1. `SIR1xx`는 JSON/schema/identifier 입력을 고친다.
2. `SIR2xx`는 declaration merge, stale hash, suppression을 확인한다.
3. `SIR3xx`는 scene binding, composition, generation을 확인한다.
4. `SIR4xx`는 capability/cardinality와 provider component를 확인한다.
5. `SIR5xx`는 build profile/scene inclusion을 확인한다.
6. `SIR6xx`는 runtime manifest/readiness를 확인한다.
7. `SIR7xx`는 AI candidate evidence와 승인 전 데이터를 확인한다.
8. `SGR1xx`는 graph 구조 오류다. requirement capability 오류와는 별개지만 strict manifest를 무효화하므로
   dangling `nextIdentifier`, 중복 node identifier 등을 먼저 고친다.

## 관련 자료

- [기능 요구사항](../../../requirements/scenario/scenario-ingame-requirements-support.md)
- [API 레퍼런스](../../../api-references/MultiplayerInfrastructure.Scenario.Requirements.md)
- [튜토리얼 월드맵 candidate 예시](../../../../Assets/Modules/TriageTrainer/Resources/Scenario/tutorial_worldmap_intro.scenario.requirements.candidates.json)
