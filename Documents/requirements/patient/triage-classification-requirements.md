---
title: "환자 트리아지 분류 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

환자 트리아지(Triage) 분류 기능은 플레이어가 KTAS(Korean Triage and Acuity Scale) 5단계 체계에 따라 환자의 응급 등급을 직접 분류하는 인게임 행동을 지원한다. 시나리오 설계자는 각 환자에 의도된 정답 등급을 지정할 수 있으며, 플레이어의 평가 결과는 네트워크를 통해 모든 참가자에게 동기화되어 환자 머리 위에 시각 태그로 표시된다.

## 상세

- 환자는 시나리오 노드에 의해 트리아지 평가 인터랙션이 활성화되면 상호작용 목록에 "트리아지 분류" 항목이 추가된다.
- 플레이어가 해당 항목을 선택하면 KTAS 5단계 색상 사각형이 가로로 나열된 전체화면 오버레이 UI가 열린다.
- 등급 사각형을 클릭하면 선택한 등급이 환자에 적용되고 UI가 닫힌다.
- 트리아지가 부여된 환자는 인게임에서 머리 위에 색상 사각형과 등급 명칭이 표기된 라벨이 표시된다.
- 평가 완료 후 재평가 허용 여부는 시나리오 설계자가 환자별로 지정할 수 있다.

## 기술적 세부 사항

### 트리아지 등급 체계 (`TriageLevel`, `TriageLevelInfo`)

- `TriageLevel` 열거형: `Unassessed(0)`, `Level1(1, 파랑, 소생)` ~ `Level5(5, 흰색, 비응급)` 로 구성된다.
- `TriageLevelInfo` 정적 클래스: 등급별 표준 배경 색(`GetColor`), 텍스트 색(`GetTextColor`), 한국어 명칭(`GetDisplayName`), 짧은 라벨(`GetShortLabel`)을 단일 진실 공급원으로 제공한다. UI와 인게임 오버헤드 태그가 동일한 색상 규칙을 공유한다.

### `PatientDescriptor` 확장 필드

- `intendedTriage`: 시나리오 설계자가 지정한 의도된 정답 등급.
- `assessedTriage`: 플레이어가 실제로 부여한 등급(런타임 상태값).

### 환자 트리아지 인터랙션 (`PatientController.Triage`)

- `TriageAssessmentConfig` 직렬화 구조체: `assessable`(활성 플래그), `changeAssessableOnAssessDone`(평가 후 재노출 정책), `displayText`, `displayIcon` 필드를 포함한다.
- `ChangeAssessableOnAssessDone` 열거형: `DisableAssessable`(항상 비활성화), `RemainAssessable`(항상 유지), `DisableOnIntendedOnly`(정답 시에만 비활성화).
- `assessable` 활성화 여부는 `SyncVar<bool>`로 서버 권위 복제된다. 클라이언트 게이트 우회 방지를 위해 서버에서도 재검증한다.
- `AssessedTriage`(`SyncVar<TriageLevel>`)가 변경될 때 환자 오버헤드 라벨이 자동 갱신된다.
- `SetTriageAssessable(bool)` 메서드가 `IScenarioTriageAssessTarget` 인터페이스를 구현하며 시나리오 노드에서 호출된다.

## 참조

- [api:TriageLevel / TriageLevelInfo](../../api-references/TriageTrainer.Patient.TriageLevelInfo.md)
- [api:IScenarioTriageAssessTarget](../../api-references/MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget.md)
- [api:ScenarioTriageAssessControlNode](../scenario/triage-assess-control-node-requirements.md)
- [ui:트리아지 평가 UI](../ui/miui_triage_assessment.md)
- [api:EntityOverheadLabelUIController](../../api-references/MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md)
