---
title: "TriageAssessControl 시나리오 노드 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

`TriageAssessControl` 노드는 시나리오 그래프에서 특정 환자(엔티티)에 대한 트리아지 평가 인터랙션을 활성화하거나 비활성화하는 즉시 실행 노드다. 트리아지를 수행할 시점을 시나리오 흐름과 정확히 연동하기 위해 사용한다.

## 상세

- 시나리오 그래프 편집기에서 `TriageAssessControl` 타입으로 노드를 추가할 수 있다.
- `targetEntityIdentifier`로 대상 환자를 지정하고, `assessable`(bool)로 활성/비활성을 설정한다.
- 노드가 실행되면 대상 엔티티에서 `IScenarioTriageAssessTarget.SetTriageAssessable`을 호출하고 즉시 다음 노드로 진행한다.
- 대상 엔티티가 레지스트리에 없거나 `IScenarioTriageAssessTarget`을 구현하지 않으면 경고 로그를 남기고 건너뛴다.

## 기술적 세부 사항

- **노드 타입 문자열:** `"TriageAssessControl"`
- **모델 클래스:** `MultiplayerInfrastructure.Scenario.ScenarioTriageAssessControlNode`
- **DTO 클래스:** `ScenarioTriageAssessControlNodeDTO` (`targetEntityIdentifier`: string, `assessable`: bool)
- **직렬화/역직렬화:** `ScenarioNodeDTOConverter`, `ScenarioGraphLoader`에 등록되어 JSON ↔ 모델 변환을 지원한다.
- **JSON 스키마:** `scenario.schema.json`의 `nodeType` 열거형과 `$defs/ScenarioTriageAssessControlNode`에 정의되어 있다. `targetEntityIdentifier`와 `assessable` 모두 필수 필드다.
- **실행:** `ScenarioController.ExecuteTriageAssessControlNode`에서 처리되며, `Parallel` 브랜치 내부에서도 동작한다.
- **인터페이스 경계:** 실제 활성화 로직은 `IScenarioTriageAssessTarget`(인프라 계층 인터페이스)을 통해 호출되므로, 노드와 도메인 구현(`PatientController`) 사이에 직접 의존성이 없다.

### JSON 예시

```json
{
  "identifier": "enable_triage_patient_a",
  "nodeType": "TriageAssessControl",
  "targetEntityIdentifier": "patient_a",
  "assessable": true,
  "nextIdentifier": "next_node"
}
```

## 참조

- [api:ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:IScenarioTriageAssessTarget](../../api-references/MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget.md)
- [req:환자 트리아지 분류](../patient/triage-classification-requirements.md)
