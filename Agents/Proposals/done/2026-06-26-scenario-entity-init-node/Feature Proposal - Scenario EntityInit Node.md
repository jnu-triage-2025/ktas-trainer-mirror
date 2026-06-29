# Feature Proposal: 시나리오 엔티티 생성·상태 설정 노드(EntityInit)

- 작성일: 2026-06-26
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`,
  `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/`
- 연관 구현(TriageTrainer): `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.TreatmentDisplay.cs`
- 관련 제안: `Agents/Proposals/done/2026-06-26-scenario-preflight-requirements/`
- 관련 명세: `Documents/requirements/scenario/scenario-entity-preset-and-tag-node-requirements.md`

### 개요

`EntityInit` 노드는 시나리오 그래프가 **엔티티를 준비(생성 또는 참조)하고 그 엔티티의 초기 상태를
설정**할 수 있게 하는 신규 시나리오 노드입니다.

- 대상 엔티티는 두 가지로 결정한다.
  - **엔티티 프리셋 스폰**: 레지스트리에 등록된 엔티티 프리셋을 식별자로 스폰한다.
  - **기존 엔티티 참조**: 이미 레지스트리에 등록된 엔티티를 식별자(직접) 또는 상태 저장소 키(간접)로 가리킨다.
- 이후 그래프가 동일 엔티티를 계속 제어할 수 있도록 **제어용 식별자(`entityIdentifier`)** 를 설정한다.
  프리셋 스폰 시에는 인스턴스에 부여할 식별자로 쓰이며, 비우면 자동(GUID) 부여된다. 확정된 식별자는
  `resultStateKey` 로 상태 저장소에도 보관할 수 있어 후속 노드가 참조할 수 있다.
- 대상 엔티티에 **초기 상태(`stateOperations`)** 를 일괄 적용한다.
  - `DisplayState`: 엔티티 컴포넌트의 명명된 표시/부착 상태를 표시/비표시로 설정한다.
  - `StateStore`: 시나리오 인메모리 상태 저장소에 키/값을 기록한다(순수 데이터).

1차 목표는 **환자 엔티티에 부착된 처치 부착물(주사기/거즈/경부보호대/비강 캐뉼라 등)의 초기 표시
상태 설정**이다. 시나리오 도입부에서 "이 환자는 이미 경부보호대가 채워져 있고 흉부에 거즈가 붙어
있는 상태"처럼 부착물 초기 표현을 그래프로 선언할 수 있게 한다.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자**로서, 시나리오 시작 시 환자가 이미 일부 처치를 받은 상태(예: 경부
보호대 착용, 거즈 부착)로 등장하도록 초기 표시 상태를 그래프에서 직접 선언하고 싶다. 왜냐하면 현재는
처치 부착물 표현이 런타임 아이템 사용(`ApplyItemUse`)이나 프리팹 하이어라키 초기값으로만 켜지므로,
"시작부터 특정 부착물이 보이는 환자"를 콘텐츠로 표현하려면 환자 프리팹 변형을 따로 만들어야 했기
때문이다.

기존 노드로는 부족하다. `EntityPresetSpawn` 은 엔티티를 스폰하고 식별자만 보관할 뿐 초기 상태를
설정하지 못한다. `EntityTag` 는 태그만 조작한다. `StateUpdate` 는 인메모리 상태 저장소에만 기록할 뿐
엔티티 컴포넌트의 시각 표현을 건드리지 못한다.

### 사용자 경험 목표

- 운영자: 시나리오 그래프에서 `EntityInit` 노드 하나로 "엔티티 준비 + 초기 부착물 표시 상태"를 선언한다.
- 학습자: 시나리오 시작 시 의도된 초기 처치 상태의 환자를 본다.
- 재사용성: MI 측에는 도메인 비종속 인터페이스만 두고, 환자 부착물 매핑은 TriageTrainer 가 구현한다.

### 제안

`MultiplayerInfrastructure.Scenario` 및 `MultiplayerInfrastructure.Entity` 에 다음을 추가한다.

1. `ScenarioNodeType.EntityInit` — 신규 노드 타입.
2. 도메인 모델
   - `ScenarioEntityInitNode` — 대상 결정(프리셋/참조), 제어 식별자, 초기 상태 항목 목록.
   - `ScenarioEntityStateOperation` + `ScenarioEntityStateOperationKind`(`StateStore`/`DisplayState`).
3. DTO/직렬화
   - `ScenarioEntityInitNodeDTO`, `ScenarioEntityStateOperationDTO`.
   - `ScenarioNodeDTOConverter` 에 `"EntityInit"` 매핑.
   - `ScenarioGraphLoader` 의 DTO↔도메인 변환(`ConvertEntityInit`/`ConvertToDTO`) 및 `SaveToJson` 경로.
4. 도메인 비종속 인터페이스
   - `MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget` — `bool ApplyScenarioDisplayState(string, bool)`.
     `IItemUseTarget` 과 동일한 "범용 대상" 설계 철학(신호/도메인 매핑은 구현체 책임).
5. 런타임 실행(`ScenarioController`)
   - `State.ExecutingEntityInit` 추가.
   - `ExecuteEntityInitNode(...)`:
     - 프리셋 스폰(`Registry.TrySpawnEntityPreset`) 또는 기존 엔티티 참조(`Registry.TryGetEntity`).
     - 확정 식별자를 `resultStateKey` 로 상태 저장소에 기록(선택).
     - 대상 GameObject 에서 `IScenarioEntityInitTarget` 를 찾아 `DisplayState` 항목 적용.
       네트워크 프리셋은 비동기 자가 등록되므로, 표시 상태는 스폰된 GameObject 에서 직접 컴포넌트를
       찾아 적용한다(레지스트리 등록 완료에 의존하지 않음).
6. 스키마(`scenario.schema.json`)
   - `nodeType` enum 에 `EntityInit` 추가, `if/then` 분기, `$defs/ScenarioEntityInitNode` 정의 추가.

연관 구현(TriageTrainer, 시스템 외부):
- `PatientController` 가 `IScenarioEntityInitTarget` 을 구현하여 `displayStateName` 을
  `TreatmentDisplay` enum 으로 해석해 `ShowTreatmentDisplay`/`HideTreatmentDisplay` 로 적용한다.

JSON 예시:

```json
{
  "init_patient_a": {
    "identifier": "init_patient_a",
    "nodeType": "EntityInit",
    "presetIdentifier": "patient_with_bed",
    "entityIdentifier": "patient_a",
    "positionSourceEntityIdentifier": "spawn_point_1",
    "resultStateKey": "patient_a.entityIdentifier",
    "stateOperations": [
      { "kind": "DisplayState", "key": "CervicalCollarOnNeck", "displayActive": true },
      { "kind": "DisplayState", "key": "GauzePatchedOnThorax", "displayActive": true }
    ],
    "nextIdentifier": "Q002_2"
  }
}
```

### 자세한 달성 목표

- `EntityInit` 노드로 프리셋 스폰 또는 기존 엔티티 참조 후 식별자를 부여/보관할 수 있다.
- 동일 노드에서 환자 부착물의 초기 표시 상태를 다수 일괄 설정할 수 있다.
- 인식할 수 없는 표시 상태 이름/누락 대상은 시나리오를 중단하지 않고 경고 후 다음 노드로 진행한다.
- 기존 노드 동작과 직렬화 하위호환에 영향이 없다(순수 추가).

### 가용성과 테스트

- 위험: MI 모듈 변경 → 신규 타입/인터페이스 추가와 비침습적 실행 훅으로 하위호환을 보장한다.
- 위험: 네트워크 프리셋의 비동기 등록 → 표시 상태는 스폰 GameObject 에서 직접 컴포넌트를 찾아
  적용하여 등록 완료 시점에 의존하지 않는다.
- 테스트: `disaster_intro` 등에 `EntityInit` 노드를 넣어 시작 시 환자 부착물이 의도대로 표시되는지,
  잘못된 표시 상태 이름이 경고만 남기고 흐름을 막지 않는지 수동 확인.

### 성공 여부 측정

- 성공 지표: 환자 프리팹 변형 없이, 시나리오 그래프만으로 "초기 부착물 표시 상태가 다른 환자"를 표현.
- 수용 기준:
  - 프리셋 스폰 + DisplayState 항목 → 시작 시 해당 부착물이 표시됨.
  - 기존 엔티티 참조(식별자/상태키) → 동일하게 동작.
  - 알 수 없는 표시 상태/누락 대상 → 경고 후 다음 노드 진행.
  - 노드 미사용 시 → 기존과 동일.

### 링크, 참고사항

- `Documents/requirements/scenario/scenario-entity-init-node-requirements.md` (사용자용 요구사항 문서)
- `Documents/requirements/scenario/scenario-entity-preset-and-tag-node-requirements.md`
