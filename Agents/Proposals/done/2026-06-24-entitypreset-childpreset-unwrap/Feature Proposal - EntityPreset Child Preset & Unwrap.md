### 개요

EntityPreset(엔티티 프리셋) 기능은 Entity(대개 Prefab)에 사전 설정과 식별자를 부여해 저장해 두고, 시스템이 필요할 때
월드에 생성(스폰)하기 위한 구현입니다. 본 제안은 기존 EntityPreset 의 하위 오브젝트 처리 방식을 **전면 재작성**합니다.

기존 구현은 "컨테이너 프리팹 1개에 환자/침대를 자식 NetworkObject 로 nested 시키고, 스폰 시 `childPath`(Transform 경로
문자열)로 자식을 찾아 분리(detach)"하는 방식이었습니다. 이 방식은 다음 문제를 가집니다.

- **원본 Prefab 을 핵심 로직에 사용**: 자식을 `childPath` 문자열로 가리키므로, 사전 설정된 하위 프리팹이 아니라 원본
  프리팹의 Transform 구조에 의존합니다. 하위가 또 다른 사전 설정(프리셋)을 가져야 하는 요구를 충족할 수 없습니다.
- **nested NetworkObject**: 컨테이너 프리팹이 자식 NetworkObject 를 nested 로 포함하므로, FishNet 의 프리팹 직렬화
  (Reserialize)가 누락되면 `ObjectId [65535] ... is expected to be initialized but was not.` 오류가 빈번히 발생합니다.
- **암묵적 "순수 컨테이너 소비"**: 루트가 의미 없는 컨테이너인지 자동 판별·파괴하는 매직 로직이 있어 동작이 불투명했습니다.

새 구현은 하위 오브젝트를 **"다른 EntityPreset 의 식별자"** 로 참조하고, **Unwrap on spawn** 플래그로 하위를 루트의
자식이 아닌 **동일 계층(형제 루트)** 에 둘 수 있게 합니다. 환자 + 환자침대를 결합 사전 설정해 한 번에 스폰하되, 런타임에는
두 독립 루트로 배치하는 사례를 1급으로 지원합니다.

- 요약: 하위 참조를 `childPath` → `childPresetIdentifier` 로 변경, `unwrapOnSpawn` 플래그 추가, 재귀 스폰 도입.
- 의도: 핵심 로직이 항상 EntityPreset 단위로 동작하게 하고, nested NetworkObject 구조를 제거.
- 주요 맥락: 환자/침대는 런타임에 Transform 위계가 없고 각자 독립 NetworkObject 다.
- 기술 제약: FishNet 은 nested NetworkObject 를 단일 단위로 취급하므로, 독립 개체는 각각 루트로 스폰되어야 한다.

### 해결하려는 문제 상황

나는 시나리오 콘텐츠를 구성하는 운영자로서, **사전 설정이 끝난 환자와 환자침대를 결합 상태로 한 번에 스폰**하고 싶다.
왜냐하면 환자/침대를 매번 따로 배치·연결하는 것은 번거롭고, 기존 컨테이너 프리팹 방식은 FishNet 직렬화 오류가 잦아
신뢰할 수 없기 때문이다. 또한 하위 오브젝트도 (원본 프리팹이 아니라) 사전 설정된 프리셋이어야 일관된 결과를 얻는다.

### 사용자 경험 목표

- 운영자는 SO 에서 `patient_a` 프리셋이 `bed_a` 프리셋을 `unwrapOnSpawn` 으로 함께 스폰하도록 한 줄로 구성한다.
- 시나리오에서 `EntityPresetSpawn` 노드로 `patient_a` 만 스폰하면, 환자와 침대가 각각 독립 루트로 생성·복제·등록된다.
- 더 이상 컨테이너 프리팹을 만들거나 nested NetworkObject 를 위해 Reserialize 를 신경 쓰지 않아도 된다(각 프리셋은
  기존처럼 독립 프리팹이므로 일반 프리팹 직렬화 규칙만 따른다).

### 제안

핵심 모델/로직 변경(`Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/`):

- `EntityPresetChildDetachment`(childPath 기반) **삭제** → `EntityPresetChildReference` **신설**:
  `childPresetIdentifier`(하위로 함께 스폰할 다른 EntityPreset 식별자), `spawnedEntityIdentifier`(스폰 인스턴스 식별자),
  `unwrapOnSpawn`(루트 자식 vs 형제 루트).
- `EntityPresetDefinition.ChildDetachments` → `ChildReferences` 로 교체.
- `Registry.EntityPreset.cs` 의 스폰 로직 재작성:
  - 루트 프리팹 Instantiate → 식별자 주입(`ISpawnedEntityIdentifierReceiver`).
  - `ChildReferences` 를 **재귀 스폰**(각 하위는 등록된 다른 EntityPreset). `unwrapOnSpawn=true` 면 형제 루트로,
    `false` 면 루트의 자식으로 배치.
  - 네트워크 프리셋이면 NetworkObject 를 FishNet `ServerManager.Spawn` 으로 복제.
  - 순환 참조 방지(스폰 스택), 컨테이너 자동 파괴 매직 제거(루트는 항상 실제 엔티티이거나 fallbackEntityType 으로 등록).

시나리오 계층:

- `ScenarioEntityChildDetachment`(+DTO) 삭제, `ScenarioEntityPresetSpawnNode` 에서 `ChildDetachments` 제거(하위 구성은
  프리셋 정의가 소유). 노드는 `presetIdentifier`/위치/결과 식별자만 다룸. `scenario.schema.json` 에서 `childDetachments` 제거.

TriageTrainer 지원:

- `EntityPresetRegistryRequirement.childDetachments` → `childReferences`. SO 에디터 검증을 참조 무결성/순환/누락 검사로 교체.
- 부트스트랩/디버거의 등록 호출을 `childReferences` 로 갱신. SO 자산을 `patient_a`(침대 unwrap 참조) + `bed_a` 2개 프리셋으로 재구성.

### 자세한 달성 목표

- `patient_a` 스폰 시: 환자(루트, networked)와 침대(`bed_a`, unwrap → 형제 루트, networked)가 각각 독립 스폰·복제되고,
  `PatientController`/`MovingPatientBedController` 가 각자 `Patient`/`MovingPatientBed` EntityType 으로 자가 등록한다.
- 결합(환자가 침대 위) 논리 연결은 기존과 동일하게 `attach_patient_bed_pairs` 이벤트(`bed.TryReposeTarget(patient)`)로 재설정.
- nested NetworkObject/컨테이너 프리팹/Reserialize 의존 제거.

### 문서화

- 갱신: `Documents/requirements/content-definitions/scenario/patient-bed-combined-preset-guide.md`
  (childPath/컨테이너 → childReferences/unwrap 모델로 재작성).
- 갱신: `Documents/requirements/content-definitions/scenario/entity-preset-debug-guide.md`
  (nested NetworkObject Reserialize 항목을 "각 프리셋은 독립 프리팹" 으로 조정).
- 본 제안서: `Agents/Proposals/2026-06-24-entitypreset-childpreset-unwrap/`.

### 가용성과 테스트

- 위험: MultiplayerInfrastructure(공용 모듈) 의 공개 API(`EntityPresetDefinition`, `RegisterEntityPreset`,
  `TrySpawnEntityPreset`) 시그니처가 변경됩니다. 다른 프로젝트가 `childDetachments`/`childPath` 에 의존했다면 영향이 있습니다.
  본 리포 내 모든 호출부는 갱신 완료.
- 테스트: IndevScene `EntityPresetDebugger` 로 등록/스폰/형제 루트 분리/엔티티 등록을 수동 확인.
  호스트/서버 컨텍스트에서 `patient_a` 스폰 → `patient_a`(Patient) / `bed_a`(MovingPatientBed) 등록 확인.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 수용 기준:
  1. `patient_a` 스폰 시 컨테이너 없이 환자/침대가 각각 독립 루트로 생성되고 둘 다 네트워크 복제된다.
  2. 레지스트리에 `patient_a`(Patient), `bed_a`(MovingPatientBed) 가 올라온다.
  3. nested NetworkObject 직렬화 오류(ObjectId 65535)가 더 이상 발생하지 않는다.
  4. 하위 참조는 항상 EntityPreset 식별자로 이루어지며, 원본 프리팹 Transform 경로에 의존하지 않는다.

### 링크, 참고사항

- 관련 선행 제안: `Agents/Proposals/2026-04-29-moving-patient-bed-entitytype-proposal.md`
- 핵심 변경 파일: `Registry.EntityPreset.cs`, `EntityPresetChildReference.cs`, `EntityPresetDefinition.cs`,
  `ScenarioEntityPresetSpawnNode.cs`, `ScenarioGraphLoader.cs`, `ScenarioController.cs`, `scenario.schema.json`,
  `EntityPresetRegistryRequirement.cs`, `EntityPresetRegistryRequirementsSO.cs`.
