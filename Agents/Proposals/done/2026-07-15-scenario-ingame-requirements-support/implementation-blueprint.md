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
    Configuration/
      ScenarioSceneCompositionProfile.cs
      ScenarioSceneCompositionModels.cs
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
  Scripts/Scenario/Requirements/
    TriageScenarioRuntimeCapabilityProvider.cs
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
5. `(kind, normalizedIdentifier)` canonical key별 group
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
| QuestWaypointHighlight | `waypointIdentifier` | SpatialAnchor | HighlightableWaypoint | WhenNodeReached |
| PlayerMove | destination type Waypoint일 때 `destinationIdentifier` | SpatialAnchor | ProvidesPosition | WhenNodeReached |
| NPCMove | `npcIdentifier` | Npc | ResolvableNpcMoveTarget, ProvidesPosition | WhenNodeReached |
| NPCMove | destination type Waypoint일 때 `destinationIdentifier` | SpatialAnchor | ProvidesPosition | WhenNodeReached |
| CameraTarget | `targetObjectIdentifier` | Entity | 없음 | NotConsumed |
| Interaction | `targetIdentifier` | Interactable | Interactable | NotConsumed 현재, inferred 보존 |
| Interaction | `requiredItemIdentifier` | ItemDefinition | 없음 | NotConsumed 현재 |
| Interaction | `completionConditionIdentifier` | EventHandler | InvokableEventHandler | WhenNodeReached |
| CombineItem | 각 `inputItemIdentifiers[]`/`outputItemIdentifier` | ItemDefinition | 없음 | NotConsumed 현재 |
| CombineItem | `!autoCombine`인 `outputItemIdentifier` | EventHandler | InvokableEventHandler | WhenNodeReached |
| InvokeEvent | `eventIdentifier` | EventHandler | InvokableEventHandler | WhenNodeReached |
| Validator | RegistryContains의 모든 valid rule | RegistryEntry 또는 registry별 kind | rule별 | WhenNodeReached |
| Validator | RuntimeState rule | RuntimeSignal | 없음 | WhenNodeReached, Consumes, Gameplay 공급 |
| Validator | PlayerAssignedTag | PlayerTagState | 없음 | WhenNodeReached, External 공급 |
| Parallel | branch `requiredPlayerTags[]`/`forbiddenPlayerTags[]` (필드는 branch DTO에 위치) | PlayerTagState | 없음 | WhenNodeReached, External 공급, 정보성 |
| PlayerTag | operation/scope별 tag 조건 | PlayerTagState | 없음 | 소비는 WhenNodeReached/External, 변경 결과는 Produces occurrence |
| EntityPresetSpawn | `presetIdentifier` | EntityPreset | SpawnablePreset | WhenNodeReached |
| EntityPresetSpawn | `positionSourceEntityIdentifier` | Entity | ProvidesPosition | OptionalFallback |
| EntityTag | direct target identifier | Entity | RegisteredEntity | WhenNodeReached |
| EntityTag | target state key | RuntimeEntityReference | RegisteredEntity | WhenNodeReached, Scenario 공급 |
| EntityInit | preset 경로 `presetIdentifier` | EntityPreset | SpawnablePreset | WhenNodeReached |
| EntityInit | preset 경로 position source | Entity | ProvidesPosition | OptionalFallback |
| EntityInit | direct existing target precedence | Entity | RegisteredEntity | WhenNodeReached |
| EntityInit | DisplayState operation 존재 | 동일 Entity | ScenarioEntityInitTarget | WhenNodeReached |
| TriageAssessControl | `targetEntityIdentifier` | Entity | ScenarioTriageAssessTarget | WhenNodeReached |
| PatientMedicalStatePreset | direct target | Entity | PatientMedicalStateTarget | WhenNodeReached |
| PatientMedicalStatePreset | state key target | RuntimeEntityReference | PatientMedicalStateTarget | WhenNodeReached, Scenario 공급 |
| ItemSubmissionConfig | preset identifier | EntityPreset | SpawnablePreset | WhenNodeReached |
| ItemSubmissionConfig | position source | Entity | ProvidesPosition | OptionalFallback |
| ItemSubmissionConfig | direct target | Interactable | ItemSubmissionTarget | WhenNodeReached |
| ItemSubmissionConfig | target state key | RuntimeEntityReference | ItemSubmissionTarget | WhenNodeReached, Scenario 공급 |
| ItemSubmissionConfig | 각 required item | ItemDefinition | 없음 | WhenNodeReached/Indeterminate |
| ItemSubmissionConfig | `completionSignalIdentifier` | RuntimeSignal | 없음 | WhenNodeReached, Produces occurrence |
| NpcInteractControl | `npcIdentifier` | Npc | RegisteredNpcComponent | WhenNodeReached |
| NpcInteractControl Add | `interactableIdentifier` | Interactable | Interactable | WhenNodeReached |
| NpcInteractControl Remove | `interactableIdentifier` | Interactable | Interactable | OptionalFallback (없으면 무경고 skip) |
| NpcInteractControl Enable/Disable | `interactableIdentifier` | Interactable | ToggleableInteractable | WhenNodeReached |

외부 requirement가 없는 node:

- `Delay`
- `StateUpdate`: 이름과 달리 외부 entity를 조회하지 않고 내부 state key를 만든다.
- `TimeControl`: timer ID는 graph/runtime 내부 handle이다.
- `ServerInternalSignal`: signal key는 runtime 내부 생산/소비 관계다.
- `ChatPrint`: identifier 기반 외부 요구사항은 없다.
- `ExecuteCommand`: 자유 command line 파싱은 1차 범위에서 제외한다.

암시적 service requirement는 identifier 없는 well-known key로 추출한다.

| 사용 node | Service key | Availability |
|---|---|---|
| Dialogue/Choice/Quiz/DisinteractableDialogue | `mi.service.dialogue-ui` | OptionalFallback 또는 node 정책 |
| PlayTTS/inline TTS | `mi.service.tts` | PlayTTS는 WhenNodeReached, inline은 OptionalFallback |
| PlayTTS | `mi.service.tts-audio-source` | WhenNodeReached |
| QuestControl | `mi.service.quest-manager` | WhenNodeReached |
| PlayerTag/Parallel tag 조건 | `mi.service.player-tag`, `mi.service.user-descriptor` | WhenNodeReached |
| ChatPrint broadcast/ExecuteCommand | `mi.service.chat` | OptionalFallback 또는 WhenNodeReached |

well-known service key는 schema와 resolver registry에 선언한다.

별도 structural/temporal validation 대상:

- 모든 `nextIdentifier`
- Choice/Quiz/Validator branch target
- Parallel branch start/completion/join
- TimeControl create/use 순서
- ServerInternalSignal register/resolve 순서
- state key producer/consumer 순서

## 6. 실제 lookup과 capability 기준

### SpatialAnchor

`ProvidesPosition`은 현재 runtime과 같이 다음 순서로 위치를 공급할 수 있어야 한다.

1. `RegistryType.Waypoint`의 `Vector3`
2. `RegistryType.InteractableEntity`의 `Vector3`

Highlight requirement는 이보다 엄격하며 실제 `WaypointAnchor.TryGet`과 highlight 기능을 요구한다.

공급자 cardinality는 registry entry 수가 아니라 **물리 공급자 identity** 기준으로 센다. Editor에서는
scene object의 stable scan identity, runtime에서는 provider object reference 또는 registration owner
metadata를 사용한다. 한 `WaypointAnchor`가 Waypoint registry, InteractableEntity 위치와 anchor lookup의
여러 증거를 제공해도 공급자 하나다. 하나의 공급자가 requirement의 모든 필수 capability를 만족해야
하며 서로 다른 공급자의 capability를 합쳐 거짓 `Satisfied`를 만들지 않는다.

**현재 Registry 한계와 판정 신뢰도.** 현재 `Registry.Register(type, id, value)`는 owner token이나
registration source를 저장하지 않고 dictionary indexer로 last-write-wins 덮어쓰기만 한다. 또한
`WaypointAnchor`는 자기 정적 dictionary(`WaypointAnchor.TryGet`)에 등록하면서 동시에 `Waypoint`와
`InteractableEntity` 두 registry에 position을 넣는다. 그 결과 **runtime에는 한 물리 공급자의 여러 증거를
합칠 owner metadata가 존재하지 않는다.** 따라서 다음을 원칙으로 한다.

- cardinality의 정확한 duplicate 판정은 **Editor/build scene snapshot**을 정본으로 한다. scene scanner는
  scene object의 stable identity를 알므로 같은 오브젝트의 다중 registry 증거를 하나로 묶을 수 있다.
- runtime에서는 owner metadata 없이 증거 identity를 합칠 수 없으면 **`Duplicate`로 단정하지 않고
  `Indeterminate`로 보고**한다. 즉 waypoint류의 runtime cardinality는 상당수가 `Indeterminate`가 될 수
  있으며 이는 정상 동작이다. runtime duplicate 확정은 후속 owner-aware Registry API(§13)가 도입된 뒤에만
  강화한다.
- 이 한계 때문에 Duplicate Error의 1차 계약은 Editor/build 판정에 둔다. runtime은 가능한 범위의 duplicate
  경고만 제공한다.

### NPCMove target

다음 중 하나로 GameObject를 얻어야 한다.

1. `RegistryType.Npc`
2. generic Entity registry

`ResolvableNpcMoveTarget`은 Npc registry 또는 generic Entity fallback 중 하나로 GameObject를 해석할 수
있다는 뜻이다. `NpcInteractControl`은 실제 `Npc` component가 필요하므로
`RegisteredNpcComponent` capability를 사용한다.

### ItemSubmission target

generic Entity 존재만으로 통과시키지 않는다. 실제 runtime처럼 `RegistryType.InteractableEntity`에서
`ItemSubmissionInteractable`을 얻을 수 있어야 한다.

### NpcInteractControl

- NPC는 Npc registry(`RegistryType.Npc`의 `GameObject` → `Npc` component)를 요구한다.
- interactable은 `RegistryType.InteractableEntity`의 `MonoBehaviour` 또는 Entity descriptor 자식의
  `IInteract`를 허용한다. `interactableComponent`는 실제 runtime과 동일하게 이 순서로 해석한다.
- operation은 `Add`, `Remove`, `Enable`, `Disable` 네 가지다.
- `Add`는 interactable 존재를 요구한다(없으면 실행 경고). `Remove`는 존재하면 제거하고 없으면 무경고로
  건너뛰므로 존재를 강제 요구로 승격하지 않는다. `Enable`/`Disable`은 `IInteractToggleable`을 추가로
  요구한다.

### Triage/Patient

- `TriageAssessControl`: `IScenarioTriageAssessTarget`
- `EntityInit` DisplayState: `IScenarioEntityInitTarget`
- `PatientMedicalStatePreset`: `PatientController`

MI core가 TriageTrainer의 concrete patient type을 직접 참조하지 않도록 runtime 확장 인터페이스
`IScenarioRuntimeCapabilityProvider`를 둔다. provider는 `(object, capability)` 지원 여부를 부작용 없이
판정하며 `RuntimeInitializeOnLoadMethod`에서 ordinal unique provider identifier로 등록한다.
TriageTrainer의 `TriageScenarioRuntimeCapabilityProvider`가 `PatientController` 등 concrete component를
검사한다. static provider registry는 `SubsystemRegistration`에서 초기화하고 중복 identifier를 Error로
처리한다. 기존 `ScenarioController` concrete 의존 제거는 별도 리팩터링으로 남긴다.

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
  [SerializeField] private int _keySchemaVersion;
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

binding은 scenario별이 아니라 composition의 `(kind, identifier)` 공급 계약이다. 여러 scenario가 같은
key를 공유하면 하나의 binding으로 충족된다. 서로 다른 scenario가 같은 composition/key에 양립할 수
없는 factory, scope 또는 cardinality를 요구하면 composition compile conflict다. generated marker는
composition, requirement key와 source scenario 목록을 기록한다.

## 9. Scene composition profile

```csharp
[CreateAssetMenu(...)]
public sealed class ScenarioSceneCompositionProfile : ScriptableObject
{
  [SerializeField] private string _identifier;
  [SerializeField] private List<ScenarioPattern> _scenarioSelectors;
  [SerializeField] private List<ScenarioSceneRoleEntry> _scenes;
  [SerializeField] private ScenarioRequirementsValidationProfile _validationProfile;
}
```

이 ScriptableObject와 직렬화 model은 `Scripts/Scenario/Requirements/Configuration`에 두어 Player에서 읽을
수 있게 하고 custom inspector만 Editor 폴더에 둔다. role entry는 scene asset GUID/path, role,
minimum/maximum load count, optional 여부와 authority를 갖는다.
scenario exact selector를 wildcard보다 우선한다. 정확히 하나의 profile만 선택되어야 하며 0개면 legacy
`AnyLoadedScene` profile로 Authoring 진단만 수행하고 결과를 `Indeterminate`로 제한한다. Development와
Production build에서는 0개 또는 2개 이상 모두 Error다. build와 runtime은 scene 이름이 아니라
path/GUID로 비교한다.

## 10. Generation planner

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

## 11. Build validation

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

정적 code registration은 `IScenarioRequirementStaticProvider` 또는 project catalog가 identifier,
capability와 authority를 열거한다. runtime 등록만 있고 static provider가 없으면 `Indeterminate`다.
Production에서도 기본 Warning이며 declaration `mustProve=true`일 때만 Error다.

## 12. Runtime manifest discovery와 integration

version 1에는 generated compiled catalog가 없다.

- `ScenarioGraph`는 canonical serialization으로 계산한 `ContentFingerprint`를 가진다.
- Resources loader는 sibling requirements TextAsset을 찾아 manifest registry에 등록한다.
- direct TextAsset consumer는 optional requirements TextAsset을 함께 직렬화한다.
- lookup key는 `(graph.Identifier, graph.ContentFingerprint)`다.
- 같은 key에 여러 sidecar가 등록되면 ambiguity Error다.
- sidecar가 없으면 inferred-only compile fallback을 사용한다.
- runtime cache는 `SubsystemRegistration`에서 비운다.
- build validator는 direct scenario TextAsset의 sidecar 포함 경로를 검사한다.

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

## 13. Registry 관련 범위

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

## 14. 기존 기능 마이그레이션

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

- 기존 serialized setting과 기본 warn-and-continue 동작을 유지한다. 현재 정책은 `[Serializable] struct
  ScenarioPreflightPolicy`이며 modes enum은 `ScenarioPreflightMissingBehavior`
  (`ContinueWithWarning`, `AbortStart`)다. struct/enum 이름을 그대로 두고 serialized layout을 깨지 않는다.
- 새 `ReportOnly`와 기존 `ContinueWithWarning`을 매핑한다.
- `AbortStart`와 `AbortScenarioStart`를 매핑한다.

### ScenarioDevStubSpawner

- 개발 전용 factory adapter로 유지한다.
- 이름 기반 추측은 legacy fallback으로만 유지한다. 현재 구현은 identifier를 소문자화한 뒤
  `trigger`/`zone`/`room` 부분 문자열을 매칭하며(`ScenarioDevStub.StubMode.TriggerZone`), `room`도
  포함한다. 이 이름 추측을 production authoring 규칙으로 승격하지 않는다.
- canonical declaration에서 명시한 kind/factory가 있으면 이를 우선한다.

### OverworldGameObjectInitializer

- 기존 세 항목을 TriageTrainer sidecar/factory catalog 예제로 만든다.
- 기존 generated marker를 새 marker로 바꾸는 migration preview를 제공한다.
- migration 전 기존 root를 자동 삭제하지 않는다.
- 오탈자가 고착된 기존 identifier는 자동 rename하지 않는다.

## 15. 테스트 파일 구조

```text
Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/Requirements/Tests/
  ScenarioRequirementCompilerTests.cs
  ScenarioRequirementsSerializationTests.cs
  ScenarioRequirementValidationEngineTests.cs
  ScenarioRequirementsSceneScannerTests.cs
  ScenarioRequirementsApplyServiceTests.cs
  ScenarioRequirementsBuildValidatorTests.cs

Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Requirements/Tests/
  ScenarioRequirementsRuntimePlayModeSmokeTests.cs
```

현재 first-party runtime asmdef가 없으므로 EditMode test source는 `Assembly-CSharp-Editor`에 포함되는
`Editor/` 아래에 둔다. 이 구성은 Test Runner가 NUnit fixture를 발견하게 하며, runtime assembly를
부분적으로 asmdef화하는 변경과 섞지 않는다. runtime assembly의 PlayMode smoke fixture는
`UNITY_INCLUDE_TESTS` 조건부 synchronous NUnit test로 두어 별도 asmdef 없이 player test에 포함한다.

## 16. 필수 단위 테스트

### Extraction

- `ScenarioNodeType`의 현재 30개 모든 값에서 expected occurrence 또는 명시적 no-requirement 결과.
  enum은 30개이며, DTO converter의 `"TagModification"`은 별도 enum 값이 아니라
  `ScenarioPlayerTagNodeDTO`로 매핑되는 JSON `nodeType` alias다. converter switch arm(31개)을
  enum 값 수로 오인하지 않는다. alias도 extraction 대상 입력 문자열로 함께 테스트한다.
- mutually exclusive preset/direct/state-key precedence
- PlayerMove/NPCMove destination type 조건
- Validator의 모든 RegistryType 및 RuntimeState 분류
- implicit UI/TTS/Quest/player service 추출
- producer/consumer occurrence 분리
- descriptor-wide와 occurrence-specific override
- NpcInteractControl operation별 capability (Add/Remove/Enable/Disable 4종, Remove의 무경고 skip 포함)
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
- 하나의 물리 공급자가 여러 registry 증거를 제공할 때 cardinality 1로 판정
- 서로 다른 공급자의 capability를 합치지 않음
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
- project runtime capability provider 등록/reset/중복
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

## 17. 구현 완료 정의

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
