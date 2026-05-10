# Notification 제거 및 Dialogue 이관 (2026-03-26)

## 변경 목적
- Notification 전용 노드 타입을 제거하고 Dialogue로 이관합니다.
- 시스템 안내 문구는 Dialogue(`speakerName=System`)로 통일합니다.

## 스키마 변경
- 대상: `Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json`
- `ScenarioNotificationNode` 정의 자체를 제거했습니다.

## 시나리오 JSON 마이그레이션
- 대상: `Assets/**/Resources/Scenario/*.json`
- Notification 노드를 Dialogue 노드로 이관:
  - `nodeType`: `Notification` -> `Dialogue`
  - `message` -> `dialogueContent`
  - `speakerName`: `System`
  - `portraitSpriteIdentifier`: `null`

적용 파일(대표):
- `Assets/Modules/TriageTrainer/Resources/Scenario/disaster_intro_mvp.json`
- `Assets/Modules/TriageTrainer/Resources/Scenario/disaster_intro.json`
- `Assets/Modules/TriageTrainer/Resources/Scenario/tag_match_mode_regression.json`
- `Assets/Modules/MultiplayerInfrastructure/Resources/Scenario/scenario_graph_all_nodes_example.json`
- `Assets/Modules/MultiplayerInfrastructure/Resources/Scenario/scenario_graph_extended_nodes_example.json`

## 런타임 동작 기준
- 시스템 안내도 Dialogue 실행 경로를 사용합니다.
- 진행은 입력(F/Space/좌클릭) 기반으로만 이뤄집니다.

## 호환성 메모
- 기존 `Notification` 노드를 사용하는 JSON은 `Dialogue`로 마이그레이션해야 합니다.
