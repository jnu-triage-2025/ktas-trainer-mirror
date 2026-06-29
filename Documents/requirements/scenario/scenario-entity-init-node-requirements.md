---
title: "Scenario EntityInit 노드 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

시나리오를 만드는 사람이, 시나리오 그래프 안에서 **엔티티(예: 환자)를 준비하고 그 엔티티의 처음
상태를 정해 둘 수 있어야 한다.** 이 기능의 1차 목표는 **환자에게 이미 붙어 있는 처치 부착물
(목 보호대, 거즈, 비강 캐뉼라 등)의 초기 표시 여부를 시나리오에서 직접 정하는 것**이다.

예를 들어 "이 환자는 시작부터 목 보호대를 착용하고 있고 가슴에 거즈가 붙어 있는 상태"처럼,
처음 화면에 등장할 때의 모습을 시나리오 작성만으로 표현할 수 있게 한다.

## 상세

- 시나리오 그래프는 `EntityInit` 노드를 통해 대상 엔티티를 다음 두 가지 방식 중 하나로 정할 수 있다.
  - **새로 만들기(프리셋 스폰)**: 미리 등록된 "엔티티 프리셋"을 식별자로 지정해 새 엔티티를 생성한다.
  - **이미 있는 엔티티 가리키기**: 이미 만들어져 등록된 엔티티를 식별자(또는 앞 노드가 보관해 둔
    상태 키)로 지정한다.
- 이 엔티티에 **고유 식별자(identifier)** 를 붙여, 이후 시나리오 그래프가 같은 엔티티를 계속
  제어할 수 있게 한다. 식별자를 비워 두면 자동으로 부여된다. 정해진 식별자는 상태 저장소에도
  보관해 두어 다음 노드가 참조할 수 있다.
- 이 엔티티의 **초기 상태**를 한 노드에서 여러 개 한 번에 설정할 수 있다.
  - **표시 상태(DisplayState)**: 환자 부착물처럼 "보이게/안 보이게"를 정한다(처음 표시 여부 설정).
  - **데이터 상태(StateStore)**: 시나리오 내부에서만 쓰는 값(키/값)을 기록한다.
- 대상 엔티티를 찾지 못하거나 인식할 수 없는 표시 상태 이름이 들어오면, 시나리오 전체를 멈추지 않고
  경고만 남긴 뒤 다음 노드로 진행한다(기존 노드들과 동일한 "경고 후 계속" 정책).

## 기술적 세부 사항

- 신규 노드 타입: `ScenarioNodeType.EntityInit`
- 도메인 노드 모델
  - `ScenarioEntityInitNode`
  - `ScenarioEntityStateOperation` / `ScenarioEntityStateOperationKind`(`StateStore`, `DisplayState`)
- DTO/직렬화
  - `ScenarioEntityInitNodeDTO`, `ScenarioEntityStateOperationDTO`
  - `ScenarioNodeDTOConverter` 에 `EntityInit` 매핑 추가
  - `ScenarioGraphLoader` 의 DTO↔도메인 변환(`ConvertEntityInit`/`ConvertToDTO`) 및 `SaveToJson` 경로 반영
- 도메인 비종속 인터페이스(재사용성)
  - `MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget`
    (`bool ApplyScenarioDisplayState(string displayStateName, bool active)`)
  - TriageTrainer 의 `PatientController` 가 이를 구현하여 표시 상태 이름을 `TreatmentDisplay` 로 해석
- 런타임 실행(`ScenarioController`)
  - `ExecuteEntityInitNode(...)`
    - 프리셋 스폰(`Registry.TrySpawnEntityPreset`) 또는 기존 엔티티 참조(`Registry.TryGetEntity`)
    - 확정 식별자를 `resultStateKey` 로 상태 저장소에 보관(선택)
    - 대상 GameObject 에서 `IScenarioEntityInitTarget` 을 찾아 표시 상태 적용
      (네트워크 프리셋은 비동기 등록이므로 스폰된 GameObject 에서 직접 컴포넌트를 찾아 적용)
  - 실행 상태(enum) 확장: `ExecutingEntityInit`
- 스키마
  - `scenario.schema.json` 의 `nodeType` enum 에 `EntityInit` 추가 및 `if/then` 분기
  - `$defs` 에 `ScenarioEntityInitNode` 정의 추가

## 참조

- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
- [scenario-entity-preset-and-tag-node-requirements.md](./scenario-entity-preset-and-tag-node-requirements.md)
- [api:scenario-graph-spec](../content-definitions/scenario/scenario-graph-spec.md)
