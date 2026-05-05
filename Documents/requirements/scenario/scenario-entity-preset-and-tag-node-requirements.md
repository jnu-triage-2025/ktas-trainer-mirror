---
title: "Scenario EntityPreset/EntityTag 노드 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

시나리오 실행 중 엔티티를 동적으로 생성하고, 직후 해당 엔티티에 태그를 부여할 수 있어야 한다. 이 기능은 훈련 상황에서 "상황 객체 생성 → 상태 라벨링" 흐름을 그래프 노드로 재현하기 위한 요구사항이다.

## 상세

- ScenarioGraph는 `EntityPresetSpawn` 노드를 통해 Entity Preset을 스폰할 수 있어야 한다.
- 스폰 위치는 고정 좌표 또는 기존 엔티티 위치 참조 방식 중 하나를 사용할 수 있어야 한다.
- 스폰 결과(생성된 entity identifier)는 후속 노드에서 사용할 수 있도록 상태 저장소에 보관 가능해야 한다.
- ScenarioGraph는 `EntityTag` 노드를 통해 엔티티 태그를 `Add/Remove/Change` 할 수 있어야 한다.
- `EntityTag`는 직접 entity identifier 지정 또는 상태 저장소 키 기반 해석을 모두 지원해야 한다.
- 대상 엔티티가 없거나 설정이 비어 있는 경우, 시나리오 전체를 중단하지 않고 경고 후 다음 노드로 진행해야 한다.

## 기술적 세부 사항

- 신규 노드 타입
  - `ScenarioNodeType.EntityPresetSpawn`
  - `ScenarioNodeType.EntityTag`
- 도메인 노드 모델
  - `ScenarioEntityPresetSpawnNode`
  - `ScenarioEntityTagNode`
- DTO/직렬화
  - `ScenarioEntityPresetSpawnNodeDTO`
  - `ScenarioEntityTagNodeDTO`
  - `ScenarioNodeDTOConverter`에 `EntityPresetSpawn`, `EntityTag` 매핑 추가
  - `ScenarioGraphLoader`의 DTO↔도메인 변환 및 `SaveToJson` 경로 반영
- 런타임 실행(`ScenarioController`)
  - `ExecuteEntityPresetSpawnNode(...)`
    - `Registry.TrySpawnEntityPreset(...)` 호출
    - 성공 시 state store에 스폰 결과 식별자 저장
  - `ExecuteEntityTagNode(...)`
    - `PlayerTagService` 식별자 기반 API(`AddTagToIdentifier`, `RemoveTagFromIdentifier`, `ChangeTagForIdentifier`) 사용
  - 실행 상태(enum) 확장
    - `ExecutingEntityPresetSpawn`
    - `ExecutingEntityTag`
- 스키마
  - `scenario.schema.json`의 `ScenarioNode.oneOf`에 신규 노드 추가
  - `$defs`에 `ScenarioEntityPresetSpawnNode`, `ScenarioEntityTagNode` 정의 추가

## 참조

- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
- [api:MultiplayerInfrastructure.Tag.PlayerTagService](../../api-references/MultiplayerInfrastructure.Tag.PlayerTagService.md)
- [api:scenario-graph-spec](../content-definitions/scenario/scenario-graph-spec.md)
