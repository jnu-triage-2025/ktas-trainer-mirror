# Notification 타입 완전 제거 및 Dialogue 이관 (2026-03-26)

## 변경 개요
- Notification 노드 타입을 완전히 제거합니다.
- 모든 시스템 안내 문구를 Dialogue(`speakerName=System`)로 이관합니다.
- 런타임, 에디터, 스키마, 시나리오 JSON 전체 계층에서 Notification 참조를 제거합니다.

## 제거 범위

### 1. 런타임 코드
- `ScenarioNodeType.cs`: `Notification` enum 항목 제거
- `ScenarioController.cs`: Notification 실행 (`ExecuteNotificationNode`), 상태 (`ExecutingNotification`) 제거
- `ScenarioNotificationNode.cs`: 파일 자체 삭제

### 2. 직렬화 & 변환
- `ScenarioNodeDTOConverter.cs`: `"Notification"` 타입 매핑 제거
- `ScenarioGraphLoader.cs`: DTO ↔ 모델 Notification 변환 로직 제거
- `ScenarioNotificationNodeDTO.cs`: 파일 자체 삭제
- `ScenarioNotificationDisplayMode.cs`: 파일 자체 삭제

### 3. 에디터 도구
- `ScenarioNodeFactory.cs`: Notification 노드 생성 메서드 제거
- `ScenarioNodeSearchWindow.cs`: Notification 메뉴 항목 제거
- `ScenarioNodeView.cs`: Notification 표시 로직 제거
- `ScenarioInspectorView.cs`: Notification 인스펙터 케이스 제거

### 4. 스키마
- `scenario.schema.json`:
  - `ScenarioNotificationNode` 정의 제거
  - `nodeType` union에서 `"Notification"` 제거

### 5. 시나리오 데이터 마이그레이션
- 대상: `Assets/**/Resources/Scenario/*.json`
- Notification 노드를 Dialogue 노드로 일괄 이관:
  - `nodeType`: `Notification` → `Dialogue`
  - `message` → `dialogueContent`
  - `speakerName` 추가: `System`
  - `portraitSpriteIdentifier` 설정: `null`

### 6. 문서 동기화
- `Documents/scenario-graph.md`: Notification 섹션 제거, Dialogue(System) 안내 추가
- `Documents/scenario/_template.md`: Notification 행 제거, Dialogue 기준 갱신
- `Documents/scenario/*.md` (시나리오 상세):
  - `disaster_intro.md`, `patient_a_critical.md`, `patient_b_c_ct.md`
  - 모든 NotificationNode 표 → DialogueNode 표로 변경
  - Message/DisplayMode 필드 → SpeakerName/DialogueContent/PortraitSpriteIdentifier로 변경

## 스키마 변경
- 대상: `Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json`
- `ScenarioNotificationNode` 정의 자체를 제거했습니다.

## 검증 결과
- ✅ 런타임 코드: Notification 참조 없음
- ✅ 에디터 코드: Notification 참조 없음
- ✅ 스키마: Notification 타입 정의 없음
- ✅ 시나리오 JSON: 모든 Notification 노드 Dialogue로 이관 완료
- ✅ 문서: Notification 기반 설명 모두 Dialogue(System) 기준으로 갱신
