# Scenario Ingame Requirements 구현 청사진

이 문서는 Feature Proposal을 코드로 옮길 때의 파일, 타입, 처리 순서, node 추출 규칙과 테스트 경계를
정의한다. 타입명은 구현 중 저장소 convention에 맞게 조정할 수 있지만 책임과 의존 방향은 유지한다.

## 1. 구현 원칙

1. extraction은 graph만 읽는 순수 함수다.
2. Editor validation은 runtime static Registry가 아니라 scene/asset snapshot을 읽는다.
3. runtime validation은 compiled manifest와 실제 Registry/provider snapshot을 읽는다.
4. JSON DTO, domain model과 Unity binding component를 분리한다.
5. MultiplayerInfrastructure runtime은 TriageTrainer 타입을 참조하지 않는다.
6. build validation은 읽기 전용이다.
7. generation은 Preview와 Apply를 분리하고 atomic Undo를 제공한다.
8. graph edge와 runtime world requirement를 분리한다.

## 2. 제안 디렉터리

```text
Assets/Modules/MultiplayerInfrastructure/
  Scripts/Scenario/Requirements/
    Model/
      ScenarioRequirementKey.cs
      ScenarioRequirementDescriptor.cs
      ScenarioRequirementOccurrence.cs
      ScenarioRequirementEnums.cs
      ScenarioRequirementManifest.cs
      ScenarioRequirementDiagnostic.cs
    Compilation/
      ScenarioRequirementCompiler.cs
      ScenarioRequirementCompilationContext.cs
      ScenarioRequirementMergePolicy.cs
      ScenarioRequirementSourceHasher.cs
    Extraction/
      IScenarioRequirementExtractor.cs
      ScenarioRequirementExtractorRegistry.cs
      ScenarioNodeRequirementExtractors.cs
    Serialization/
      ScenarioRequirementsDeclarationDTO.cs
      ScenarioRequirementsCandidateDTO.cs
      ScenarioRequirementsLoader.cs
      ScenarioRequirementsSchemaProvider.cs
      ScenarioRequirementsSchemaValidator.cs
    Validation/
      IScenarioRequirementResolver.cs
      ScenarioRequirementValidationEngine.cs
      ScenarioRequirementValidationContext.cs
      ScenarioRequirementReport.cs
    Runtime/
      ScenarioRequirementsRuntimeValidator.cs
      ScenarioRequirementsRuntimeProvider.cs
      ScenarioRequirementsRuntimePolicy.cs
    Scene/
      ScenarioRequirementsSceneBinding.cs
      ScenarioRequirementObjectBinding.cs
      ScenarioGeneratedWorldObject.cs

  Editor/Scenario/Requirements/
    ScenarioRequirementsWindow.cs
    ScenarioRequirementsWindowState.cs
    ScenarioRequirementsSceneScanner.cs
    ScenarioRequirementsAssetScanner.cs
    ScenarioRequirementsApplyPlanner.cs
    ScenarioRequirementsApplyService.cs
    ScenarioRequirementsBuildValidator.cs
    ScenarioSceneCompositionProfile.cs
    ScenarioRequirementsAiImportService.cs
    Factories/
      IScenarioWorldObjectFactory.cs
      ScenarioWorldObjectFactoryRegistry.cs
      WaypointAnchorFactory.cs
      EmptyEntityAnchorFactory.cs
      DevelopmentInteractionStubFactory.cs

  Resources/Schema/
    scenario.requirements.schema.json
    scenario.requirements.candidates.schema.json
```

프로젝트 확장:

```text
Assets/Modules/TriageTrainer/
  Editor/Scenario/Requirements/
    TriageScenarioWorldObjectFactoryProvider.cs
    TriageNpcFactory.cs
    TriagePatientFactory.cs
    OverworldInitializerMigration.cs
  ScriptableObjects/ScenarioRequirements/
    TriageScenarioFactoryCatalog.asset
    TriageScenarioSceneComposition.asset
```

현 저장소에는 first-party asmdef 경계가 없으므로 1차 구현에서 일부 폴더만 신규 asmdef로 분리하지
않는다. Runtime/Editor folder 경계를 지키고 전체 assembly 분리는 별도 제안으로 수행한다.

## 3. 핵심 인터페이스

### Extractor

```csharp
public interface IScenarioRequirementExtractor
{
  ScenarioNodeType NodeType { get; }

  void Extract(
    IScenarioNode node,
    ScenarioRequirementExtractionContext context,
    ScenarioRequirementBuilder output);
}
```

- extractor는 Registry, scene, Resources 또는 AssetDatabase를 조회하지 않는다.
- 빈 identifier도 builder에 보고하여 malformed diagnostic을 만들 수 있게 한다.
- source occurrence의 `fieldPath`를 반드시 제공한다.
- mutually exclusive field precedence는 실제 `ScenarioController`와 동일하게 구현한다.

### Resolver

```csharp
public interface IScenarioRequirementResolver
{
  ScenarioRequirementKind Kind { get; }

  ScenarioRequirementResolution Resolve(
    ScenarioRequirementDescriptor requirement,
    ScenarioRequirementValidationContext context);
}
```

context는 Editor scene snapshot 또는 runtime provider snapshot을 구현한다. resolver가 `#if UNITY_EDITOR`
분기로 AssetDatabase를 직접 호출하지 않게 한다.

### Factory

```csharp
#if UNITY_EDITOR
public interface IScenarioWorldObjectFactory
{
  string Identifier { get; }
  bool Supports(ScenarioRequirementDescriptor requirement);
  ScenarioWorldObjectCreationPlan Plan(
    ScenarioRequirementDescriptor requirement,
    ScenarioWorldObjectFactoryContext context);
  GameObject Apply(
    ScenarioWorldObjectCreationPlan plan,
    ScenarioWorldObjectFactoryContext context);
}
#endif
```

- `Plan`은 scene을 수정하지 않는다.
- `Apply`는 planner가 검증한 target scene에서만 호출한다.
- factory identifier는 ordinal unique다.
- reflection으로 임의 type을 생성하지 않는다.

## 4. Compiler 처리 순서

```text
1. ScenarioGraphLoader로 graph 로드
2. graph structural diagnostics 수행
3. 모든 node extractor 실행
4. occurrence identifier normalize
5. canonical key별 group
6. rule default capability/cardinality/availability 적용
7. declaration schema/DTO 로드
8. source hash와 selector reconcile
9. Declared/Override merge
10. suppression validate/apply
11. conflict diagnostics 생성
12. requirement와 diagnostics stable sort
13. immutable manifest 반환
```

structural diagnostics Error가 있으면 extraction 가능한 범위는 계속 수집하되 compiled manifest를 valid로
표시하지 않는다. graph edge target은 world requirement로 만들지 않는다.

## 5. 노드별 추출 행렬

아래 표는 1차 구현에서 반드시 코드와 테스트로 고정할 기준이다.

| Node | 조건/필드 | Kind | Capability | Availability |
|---|---|---|---|---|
| Dialogue | `portraitSpriteIdentifier` non-empty | SpriteResource | LoadableResource | OptionalFallback |
| Dialogue | `playTTS && ttsVoiceIdentifier` | TtsVoice | LoadableResource | OptionalFallback |
| DisinteractableDialogue | `portraitSpriteIdentifier` | SpriteResource | LoadableResource | OptionalFallback |
| Choice | `portraitSpriteIdentifier` | SpriteResource | LoadableResource | OptionalFallback |
| Choice | 각 `options[].displayIconIdentifier` | SpriteResource | LoadableResource | OptionalFallback |
| Choice | `playTTS && ttsVoiceIdentifier` | TtsVoice | LoadableResource | OptionalFallback |
| Quiz | `playTTS && ttsVoiceIdentifier` | TtsVoice | LoadableResource | OptionalFallback |
| Sound | `soundResourceIdentifier` | AudioResource | LoadableResource | WhenNodeReached |
| PlayTTS | `transcriptIdentifier` | TtsTranscript | LoadableResource | WhenNodeReached |
| PlayTTS | `ttsVoiceIdentifier` non-empty | TtsVoice | LoadableResource | OptionalFallback |
| QuestControl | definition identifier precedence 적용 | QuestDefinition | ResolvableQuestDefinition | 조건부 |
| QuestWaypointHighlight | `waypointIdentifier` | Waypoint | HighlightableWaypoint | WhenNodeReached |
| PlayerMove | destination type Waypoint일 때 `destinationIdentifier` | MoveDestination | ProvidesPosition | WhenNodeReached |
| NPCMove | `npcIdentifier` | Npc | RegisteredNpc, ProvidesPosition | WhenNodeReached |
| NPCMove | destination type Waypoint일 때 `destinationIdentifier` | MoveDestination | ProvidesPosition | WhenNodeReached |
| CameraTarget | `targetObjectIdentifier` | Entity | 없음 | NotConsumed |
| Interaction | `targetIdentifier` | Interactable | Interactable | NotConsumed 현재, inferred 보존 |
| Interaction | `requiredItemIdentifier` | ItemDefinition | 없음 | NotConsumed 현재 |
| Interaction | `completionConditionIdentifier` | EventHandler | InvokableEventHandler | WhenNodeReached |
| CombineItem | 각 input/output identifier | ItemDefinition | 없음 | NotConsumed 현재 |
| CombineItem | `!autoCombine` output identifier | EventHandler | InvokableEventHandler | WhenNodeReached |
| InvokeEvent | `eventIdentifier` | EventHandler | InvokableEventHandler | WhenNodeReached |
| Validator | RegistryContains의 모든 valid rule | RegistryEntry 또는 registry별 kind | rule별 | WhenNodeReached |
| Validator | RuntimeState rule | RuntimeSignal | 없음 | ProducedByGameplay |
| Validator | PlayerAssignedTag | PlayerTagState | 없음 | ExternalRuntime |
| Parallel | required/forbidden tags | PlayerTagState | 없음 | ExternalRuntime, 정보성 |
| EntityPresetSpawn | `presetIdentifier` | EntityPreset | SpawnablePreset | WhenNodeReached |
| EntityPresetSpawn | `positionSourceEntityIdentifier` | Entity | ProvidesPosition | OptionalFallback |
| EntityTag | direct target identifier | Entity | RegisteredEntity | WhenNodeReached |
| EntityTag | target state key | RuntimeEntityReference | RegisteredEntity | ProducedByScenario |
| EntityInit | preset 경로 `presetIdentifier` | EntityPreset | SpawnablePreset | WhenNodeReached |
| EntityInit | preset 경로 position source | Entity | ProvidesPosition | OptionalFallback |
| EntityInit | direct existing target precedence | Entity | RegisteredEntity | WhenNodeReached |
| EntityInit | DisplayState operation 존재 | 동일 Entity | ScenarioEntityInitTarget | WhenNodeReached |
| TriageAssessControl | `targetEntityIdentifier` | Entity | ScenarioTriageAssessTarget | WhenNodeReached |
| PatientMedicalStatePreset | direct target | Entity | PatientMedicalStateTarget | WhenNodeReached |
| PatientMedicalStatePreset | state key target | RuntimeEntityReference | PatientMedicalStateTarget | ProducedByScenario |
| ItemSubmissionConfig | preset identifier | EntityPreset | SpawnablePreset | WhenNodeReached |
| ItemSubmissionConfig | position source | Entity | ProvidesPosition | OptionalFallback |
| ItemSubmissionConfig | direct target | Interactable | ItemSubmissionTarget | WhenNodeReached |
| ItemSubmissionConfig | target state key | RuntimeEntityReference | ItemSubmissionTarget | ProducedByScenario |
| ItemSubmissionConfig | 각 required item | ItemDefinition | 없음 | WhenNodeReached/Indeterminate |
| NpcInteractControl | `npcIdentifier` | Npc | RegisteredNpc | WhenNodeReached |
| NpcInteractControl Add | `interactableIdentifier` | Interactable | Interactable | WhenNodeReached |
| NpcInteractControl Enable/Disable | interactable identifier | Interactable | ToggleableInteractable | WhenNodeReached |

외부 requirement가 없는 node:

- `Delay`
- `StateUpdate`: 이름과 달리 외부 entity를 조회하지 않고 내부 state key를 만든다.
- `TimeControl`: timer ID는 graph/runtime 내부 handle이다.
- `ServerInternalSignal`: signal key는 runtime 내부 생산/소비 관계다.
- `ChatPrint`: identifier 기반 외부 요구사항은 없다.
- `ExecuteCommand`: 자유 command line 파싱은 1차 범위에서 제외한다.

별도 structural/temporal validation 대상:

- 모든 `nextIdentifier`
- Choice/Quiz/Validator branch target
- Parallel branch start/completion/join
- TimeControl create/use 순서
- ServerInternalSignal register/resolve 순서
- state key producer/consumer 순서

## 6. 실제 lookup과 capability 기준

### MoveDestination

현재 runtime과 같이 다음 순서로 위치를 공급할 수 있어야 한다.

1. `RegistryType.Waypoint`의 `Vector3`
2. `RegistryType.InteractableEntity`의 `Vector3`

Highlight requirement는 이보다 엄격하며 실제 `WaypointAnchor.TryGet`과 highlight 기능을 요구한다.

### NPCMove target

다음 중 하나로 GameObject를 얻어야 한다.

1. `RegistryType.Npc`
2. generic Entity registry

단순 GameObject 존재를 넘어 이동 구현이 실제로 요구하는 component를 조사해 capability를 세분화할 수
있다. 1차 구현은 runtime lookup parity를 우선한다.

### ItemSubmission target

generic Entity 존재만으로 통과시키지 않는다. 실제 runtime처럼 `RegistryType.InteractableEntity`에서
`ItemSubmissionInteractable`을 얻을 수 있어야 한다.

### NpcInteractControl

- NPC는 Npc registry와 `Npc` component를 요구한다.
- interactable은 InteractableEntity 또는 Entity 자식의 `IInteract`를 허용한다.
- Enable/Disable은 `IInteractToggleable`을 추가로 요구한다.

### Triage/Patient

- `TriageAssessControl`: `IScenarioTriageAssessTarget`
- `EntityInit` DisplayState: `IScenarioEntityInitTarget`
- `PatientMedicalStatePreset`: `PatientController`

MI core가 TriageTrainer의 concrete patient type을 직접 참조하지 않도록 capability adapter/provider를
도입한다. 기존 runtime의 concrete 의존 제거가 선행되지 않으면 해당 extractor와 resolver만 project
extension으로 분리한다.

## 7. Scene snapshot

Editor scanner는 target scene별 root에서 시작해 공급자 후보를 만든다.

```csharp
public sealed class ScenarioSceneRequirementSnapshot
{
  public IReadOnlyList<ScenarioSceneObjectProvider> Providers { get; }
  public IReadOnlyList<ScenarioSceneScanDiagnostic> Diagnostics { get; }
}
```

공급자 metadata:

- scene asset GUID/path와 role
- hierarchy path는 표시용으로만 사용
- GameObject/Component reference
- identifier와 registry type
- 제공 capability
- active/enabled 상태
- prefab instance 여부
- generated marker와 ownership
- network authority metadata

검색 규칙:

- target `Scene.GetRootGameObjects()`에서만 순회한다.
- inactive object를 포함하되 상태를 별도로 기록한다.
- persistent asset, preview scene과 internal scene은 제외한다.
- Prefab Stage는 Main Stage validation과 분리한다.
- 동일 identifier 공급자를 모두 보존하고 마지막 하나로 덮어쓰지 않는다.

## 8. Scene binding component

```csharp
public sealed class ScenarioRequirementsSceneBinding : MonoBehaviour
{
  [SerializeField] private string _compositionIdentifier;
  [SerializeField] private List<ScenarioRequirementObjectBinding> _bindings;
}

[Serializable]
public sealed class ScenarioRequirementObjectBinding
{
  [SerializeField] private string _requirementKey;
  [SerializeField] private Object _target;
  [SerializeField] private string _notes;
}
```

검증 규칙:

- binding target은 같은 scene에 있어야 한다.
- key가 current manifest에 없으면 orphan Warning이다.
- 같은 key의 binding이 cardinality maximum을 넘으면 Duplicate Error다.
- target이 null/missing script이면 Malformed Error다.
- binding이 있어도 resolver capability가 실패하면 MissingCapability다.
- binding component가 없어도 convention/registry component scan으로 자동 후보를 찾을 수 있다.

binding은 runtime registration을 대신하지 않는다. 필요하면 runtime provider가 binding을 참고할 수 있지만
기존 component lifecycle과 중복 등록하지 않도록 한다.

## 9. Generation planner

planner operation:

- `Create`
- `Configure`
- `Bind`
- `MoveToScene`
- `MarkOrphan`
- `DeleteGenerated`
- `NoChange`
- `Blocked`

Preview는 operation별 대상, 이유, 변경 필드와 위험을 표시한다. `Blocked`가 있으면 기본적으로 Apply를
막고 사용자가 해결하도록 한다.

Apply 안전 규칙:

1. Main Stage인지 검사한다.
2. target scene이 loaded/writable인지 검사한다.
3. Undo group을 만들고 이름을 지정한다.
4. `Undo.RegisterCreatedObjectUndo`, `Undo.AddComponent`, `Undo.RecordObject`,
   `Undo.SetTransformParent`, `Undo.DestroyObjectImmediate`를 사용한다.
5. 생성 직후 `SceneManager.MoveGameObjectToScene`으로 target scene을 명시한다.
6. private field reflection 대신 public configuration API 또는 `SerializedObject`를 사용한다.
7. prefab instance override는 `PrefabUtility.RecordPrefabInstancePropertyModifications`로 기록한다.
8. nested prefab unpack은 자동 수행하지 않는다.
9. 변경한 scene만 dirty 처리한다.
10. 성공 시 Undo group을 collapse한다.
11. 실패 시 적용된 operation과 실패 지점을 report하고 Undo 가능한 상태를 보존한다.
12. 자동 저장하지 않는다.

## 10. Build validation

### Preprocess

- Build Settings와 composition profile 로드
- scenario/sidecar pair 검색
- schema, hash, compile diagnostics 수집
- 필요한 scene asset 포함 여부 검사
- asset/resource/factory catalog 검사

### Process scene 또는 독립 scene scan

- scene provider snapshot 생성
- composition별 snapshot 합성
- cardinality, scope, capability 검사
- duplicate identifier와 missing script 검사

### 결과

- stable sort: scenario, requirement key, diagnostic code, source node 순서
- console summary와 상세 report
- 선택적으로 JSON report 저장
- Error/Fatal이 있으면 마지막에 `BuildFailedException`
- validation 과정의 tracked file hash 전후가 같아야 함

## 11. Runtime integration

runtime provider는 다음 증거를 제공한다.

- Registry type/id별 현재 registration
- 실제 object/component와 capability adapter
- loaded scene와 role
- authority/host/server/client 상태
- readiness timestamp 또는 phase

runtime validation 순서:

```text
compiled manifest load
 -> static declaration validity 확인
 -> loaded composition readiness 확인
 -> applicable authority 필터
 -> provider resolution/cardinality
 -> capability validation
 -> report
 -> policy 적용
```

`NotReady` 재시도는 무한 대기하지 않는다. profile에서 timeout과 poll interval을 지정하며 timeout 후
`Missing`이 아니라 `NotReady` Error로 보고한다.

`ScenarioController.StartScenario` 통합 시 side-effect 없는 validation을 다음 작업보다 먼저 수행한다.

- 기존 coroutine 중단
- signal/timer reset
- `_currentGraph` 교체
- quest include preload

단, runtime-produced requirement 판정을 위해 quest include가 필요한 경우 preload를 validation preparation
단계로 분리하고 실패 시 기존 실행 상태를 건드리지 않는다.

## 12. Registry 관련 범위

1차 기능은 Registry API 전체 변경을 필수 조건으로 삼지 않는다. Editor/build에서는 scene snapshot으로
duplicate를 검출한다. runtime에서는 현재 Registry lookup과 scene binding/provider metadata를 함께
사용해 가능한 범위에서 ambiguity를 보고한다.

후속 권고 API:

```text
Register(key, value, ownerToken, source)
Unregister(key, ownerToken)
GetRegistrations(key)
Reset(scope)
```

owner-aware unregister와 scope reset은 domain reload 비활성화 및 duplicate lifecycle 문제를 근본적으로
해결하지만 별도 Registry proposal로 구현한다.

requirements 구현 중 추가하는 static cache는 반드시
`RuntimeInitializeLoadType.SubsystemRegistration` reset을 제공한다.

## 13. 기존 기능 마이그레이션

### ScenarioRequirementsCollector

- 새 compiler에서 `Inferred` requirements를 만든다.
- 기존 `Collect(graph)`는 projection 결과를 반환한다.
- projection 과정에서도 기존 enum으로 표현할 수 없는 새 kind는 누락하지 말고 compatibility policy에
  따라 generic 또는 정보성 결과로 기록한다.

### ScenarioRequirementsChecker

- 새 runtime validation engine adapter로 교체한다.
- 기존 `Satisfied/Missing/Indeterminate`로 상태를 축약한다.
- canonical report는 상세 상태를 유지한다.

### ScenarioPreflight

- 기존 serialized setting과 기본 warn-and-continue 동작을 유지한다.
- 새 `ReportOnly`와 기존 ContinueWithWarning을 매핑한다.
- `AbortStart`와 `AbortScenarioStart`를 매핑한다.

### ScenarioDevStubSpawner

- 개발 전용 factory adapter로 유지한다.
- trigger/zone/room 이름 추측은 legacy fallback으로만 유지한다.
- canonical declaration에서 명시한 kind/factory가 있으면 이를 우선한다.

### OverworldGameObjectInitializer

- 기존 세 항목을 TriageTrainer sidecar/factory catalog 예제로 만든다.
- 기존 generated marker를 새 marker로 바꾸는 migration preview를 제공한다.
- migration 전 기존 root를 자동 삭제하지 않는다.
- 오탈자가 고착된 기존 identifier는 자동 rename하지 않는다.

## 14. 테스트 파일 구조

```text
Assets/Modules/MultiplayerInfrastructure/Tests/EditMode/Scenario/Requirements/
  ScenarioRequirementExtractorTests.cs
  ScenarioRequirementCompilerTests.cs
  ScenarioRequirementsSerializationTests.cs
  ScenarioRequirementsSceneScannerTests.cs
  ScenarioRequirementsApplyServiceTests.cs
  ScenarioRequirementsBuildValidatorTests.cs

Assets/Modules/MultiplayerInfrastructure/Tests/PlayMode/Scenario/Requirements/
  ScenarioRequirementsRuntimeValidatorTests.cs
  ScenarioRequirementsRegistryLifecycleTests.cs
  ScenarioRequirementsAdditiveSceneTests.cs
```

현재 first-party test asmdef가 없으므로 테스트 도입 시 Test assembly 설정을 함께 해야 한다. runtime
assembly를 부분적으로 asmdef화하는 변경과 한 commit에 섞지 않는다.

## 15. 필수 단위 테스트

### Extraction

- 30개 모든 node type에서 expected occurrence snapshot
- mutually exclusive preset/direct/state-key precedence
- PlayerMove/NPCMove destination type 조건
- Validator의 모든 RegistryType 및 RuntimeState 분류
- NpcInteractControl operation별 capability
- optional/fallback과 NotConsumed 분류
- 모든 source node/field path 보존
- 빈 identifier malformed 보고

### Merge/serialization

- trim 후 중복 병합
- case-only collision 경고
- capability 합집합
- conflict는 자동 우선순위로 숨기지 않음
- AI unapproved candidate non-blocking
- stale hash, orphan declaration
- suppression reason/owner/expiry
- unknown property/version/enum 거부
- duplicate JSON property 거부
- deterministic byte serialization

### Scene

- single/additive composition
- inactive root/disabled component
- duplicate identifier
- wrong type/missing interface
- wrong scene role
- prefab instance/nested prefab/missing script
- Prefab Stage 제외
- scene-local binding cross-scene target 거부

### Generation

- Preview가 scene을 변경하지 않음
- Apply/Undo/Redo
- 두 번 Apply해도 중복 없음
- orphan을 자동 삭제하지 않음
- active scene과 target scene이 달라도 올바르게 배치
- partial failure report
- manual child/override 정책

### Runtime

- domain reload on/off
- scene reload off 반복 Play 5회
- additive load 지연과 timeout
- host/server/client authority
- ReportOnly/AbortScenarioStart
- 검증 실패 시 기존 scenario 실행 보존
- scene unload/reload 후 stale provider 없음

### Build

- clean checkout batch mode
- missing composition scene
- invalid/stale sidecar
- sidecar 없는 legacy scenario
- duplicate scenario identifier/provider
- 첫 오류 후에도 전체 진단 수집
- dialog 없음
- validation 전후 파일 내용 불변
- deterministic report

## 16. 구현 완료 정의

각 phase는 다음 산출물이 모두 있어야 완료다.

- 코드와 automated test
- JSON Schema와 valid/invalid fixtures
- 사용자 요구사항 문서
- Editor setup/operation guide
- public API reference
- migration note
- documentation link validation 통과
- clean Unity compile
- EditMode/PlayMode test 결과
- build validator의 read-only 검증 결과

단순히 EditorWindow가 요구사항 목록을 표시하는 것만으로 완료로 보지 않는다. canonical model,
runtime parity, strict diagnostics와 migration 경계가 함께 구현되어야 한다.
