# API 레퍼런스: `MultiplayerInfrastructure.Scenario.Requirements`

## 시스템 명칭과 경계

`MultiplayerInfrastructure.Scenario.Requirements`의 공식 제품 명칭은
**시나리오 요구사항 계약 시스템(Scenario Requirements Contract System)**이다.

이 namespace는 scenario graph의 외부 의존성을 manifest로 만들고, 선언·씬·에셋·runtime 공급 증거가
그 manifest를 충족하는지 검사한다. TriageTrainer의 prefab, scene 이름, 프로젝트 전용 component를
직접 알지 않는다. 그런 구현은 project factory, capability provider, static provider catalog로 연결한다.

## 모델 계층

| API | 공식 명칭 | 책임 |
|---|---|---|
| `ScenarioRequirementKey` | 요구사항 키 | `(ScenarioRequirementKind, trimmed identifier)`의 Ordinal identity |
| `ScenarioRequirementOccurrence` | 사용 위치 | node, field path, 소비/생산, availability, expected supply 기록 |
| `ScenarioRequirementDescriptor` | 요구사항 | 키, capability 집합, cardinality, scope, authority, binding hint의 계약 |
| `ScenarioRequirementManifest` | 요구사항 manifest | descriptor와 compiler diagnostic의 immutable 정본 |
| `ScenarioRequirementDiagnostic` | 계약 진단 | `SIR` 또는 `SGR` code, severity, source, key, 수정 힌트 |
| `ScenarioRequirementProvider` | 공급자 증거 | 실제/정적 공급자의 물리 identity, capability, scene role, 활성 상태 |
| `ScenarioRequirementProviderSnapshot` | 공급자 snapshot | validation 입력으로 쓰는 안정 정렬 provider 증거 모음 |

`ScenarioRequirementKind`는 대상의 의미를, `ScenarioRequirementCapability`는 해당 대상이 제공해야 하는
실행 기능을 표현한다. identifier만 같은 여러 provider의 capability를 서로 합쳐 `Satisfied`로 만들지
않으며, 하나의 물리 provider가 필요한 capability를 모두 가져야 한다.

## 입력 및 컴파일 API

| API | 책임 |
|---|---|
| `ScenarioRequirementCompiler.CompileInferred` | graph만 읽어 inferred manifest 생성 |
| `ScenarioRequirementCompiler.Compile` | graph, 원문 bytes, canonical sidecar를 병합해 manifest 생성 |
| `ScenarioRequirementsLoader.LoadSidecar` | strict JSON으로 canonical sidecar 로드 |
| `ScenarioRequirementsLoader.LoadCandidates` | strict JSON으로 AI/manual candidate 문서 로드 |
| `ScenarioRequirementsCandidatePreviewService.CreatePreview` | candidate evidence/factory를 비변경 방식으로 검토 |
| `ScenarioRequirementsCandidatePreviewService.CreateApprovedDocument` | 승인된 candidate를 canonical declaration DTO로 변환 |
| `ScenarioRequirementsWriter.WriteUtf8` | canonical sidecar를 결정적으로 UTF-8 직렬화 |

파일 역할은 반드시 구분한다.

- `*.scenario.json`: 실행 graph 원본
- `*.scenario.requirements.json`: 승인된 **canonical sidecar**
- `*.scenario.requirements.candidates.json`: 승인 전 **candidate 문서**

candidate 문서는 `AiImported` 상태로 manifest에 자동 병합되지 않는다. preview/승인 과정을 거쳐
`Declare` 또는 `Override` 선언으로 변환된 경우만 canonical sidecar가 된다.

## 검증 API

| API | 책임 |
|---|---|
| `ScenarioRequirementValidationEngine.Validate` | Editor/build 정적 snapshot 검증 |
| `ScenarioRequirementValidationEngine.ValidateRuntime` | runtime snapshot 검증. 적용 authority requirement를 실제 증거와 대조 |
| `ScenarioRequirementsSceneScanner.Scan` | composition scene과 binding을 읽어 Editor provider snapshot 생성 |
| `IScenarioRequirementStaticProvider` | project catalog가 Play Mode 없이 공급자를 열거하는 확장점 |
| `ScenarioRequirementStaticProviderCatalog` | inspector에서 작성 가능한 static provider catalog asset |
| `ScenarioRequirementsBuildValidator` | build preprocessor. public 수동 API가 아닌 통합 명령에서 사용 |
| `ScenarioRuntimeRequirementsValidator` | runtime manifest discovery, readiness, strict abort 판정 |
| `ScenarioRuntimeBootstrapGate` | scene ready 이후 AbortSessionBootstrap 검증용 facade |

정적 검증은 fixed network authority와 runtime-only registration을 항상 증명할 수 없으므로 `Indeterminate`를
사용한다. runtime은 실행 context의 server/client/host 상태에 맞는 requirement만 적용한다.

## 씬 생성·연결 API

| API | 공식 명칭 | 책임 |
|---|---|---|
| `ScenarioSceneCompositionProfile` | 구성 프로필 | scenario selector와 scene path/GUID/role/정책 연결 |
| `ScenarioRequirementsSceneBinding` | 씬 binding | requirement key와 scene-local object를 연결 |
| `IScenarioWorldObjectFactory` | 월드 오브젝트 factory | generated/prefab requirement의 preview와 Apply 구현 |
| `ScenarioRequirementsApplyPlanner` | 생성 계획기 | Create/Configure/MarkOrphan/DeleteGenerated/Blocked 계획 생성 |
| `ScenarioRequirementsApplyService` | 적용 서비스 | 계획을 단일 Unity Undo group으로 적용 |
| `ScenarioGeneratedWorldObject` | 생성 marker | 시스템 소유 object의 factory, manifest, generation fingerprint 기록 |

`ScenarioGeneratedWorldObject.ManifestFingerprint`은 graph manifest의 content fingerprint이고,
`GenerationInputFingerprint`은 factory·scene·position 등을 포함한 generation plan identity다. 두 값은
다른 비교 목적을 가지므로 서로 대입하면 안 된다.

## Editor 진입점

수동 검증의 단일 진입점은 다음이다.

```text
Tools > Multiplayer Infrastructure > Validate Scenario Requirements
```

내부 phase, runtime registration, build validator는 개별 메뉴나 public 운영 API가 아니다. 통합 명령이
모두 실행하고 실패를 하나의 결과로 보고한다. object inspection, binding, generation preview는 별도
`Tools > Multiplayer Infrastructure > Scenario Ingame Requirements` 창에서 수행한다.

## 상태와 진단

`ScenarioRequirementValidationStatus`의 핵심 상태는 `Satisfied`, `Missing`, `MissingCapability`,
`Duplicate`, `WrongScene`, `NotReady`, `Indeterminate`, `Suppressed`다. `Indeterminate`는 누락이 아니라
현재 증거만으로 증명할 수 없다는 뜻이다. `mustProve` 또는 profile 정책이 있을 때만 차단 오류가 된다.

진단 namespace는 다음과 같다.

- `SIR1xx`: 입력, JSON, 정규화
- `SIR2xx`: compile, merge, declaration
- `SIR3xx`: scene, binding, generation
- `SIR4xx`: capability, cardinality
- `SIR5xx`: build composition
- `SIR6xx`: runtime manifest/readiness
- `SIR7xx`: candidate review
- `SGR1xx`: graph structural validation

`SGR`은 requirement 자체가 아니라 graph 구조 진단이다. 하지만 malformed graph로는 strict manifest를
신뢰할 수 없으므로 `SGR` Error는 manifest를 invalid로 만든다.

## 관련 문서

- [기능 요구사항](../requirements/scenario/scenario-ingame-requirements-support.md)
- [운영 가이드](../working-guide/features/scenario/scenario-ingame-requirements-support-guide.md)
- [튜토리얼 candidate 예시](../../Assets/Modules/TriageTrainer/Resources/Scenario/tutorial_worldmap_intro.scenario.requirements.candidates.json)
