---
title: "평가 루브릭 기록 코어(RubricRecorder) 설정 가이드"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-25
---

# 평가 루브릭 기록 코어(RubricRecorder) 설정 가이드

이 가이드는 운영자가 `RubricRecorder`(평가 루브릭 수행/미수행 자동 기록 코어, G-3 첫 증분)를
씬에 설치·설정하는 방법을 설명합니다. 이 단계는 **자동 기록 코어**만 다룹니다(관찰자 화면·파일 저장은 후속).

## 1. 무엇을 하는 컴포넌트인가

- 시나리오가 진행되는 동안, 각 루브릭 항목(예: 지혈, IV 확보, 가슴압박)이
  **수행되었는지/미수행인지**를 자동으로 판정해 기록합니다.
- 판정 기준:
  - **수행**: 해당 처치의 Validator 게이트를 통과(다음 단계로 진행).
  - **미수행**: 게이트가 제한 시간(`waitTimeoutSeconds`)을 넘겨 `ForceAdvance`/`FailBranch`로 처리됨.
- 세션이 끝나면 항목별 결과를 CSV로 콘솔에 출력합니다(디브리핑용).

> 전제: "미수행"이 자동 기록되려면, 해당 게이트에 타임아웃이 설정되어 있어야 합니다.
> 설정 방법은 [validator-gate-timeout-setup-guide.md](./validator-gate-timeout-setup-guide.md) 참고.

## 2. 설치 순서 (Unity 에디터)

1. 시나리오를 구동하는 씬에 빈 GameObject를 하나 만들고 이름을 `RubricRecorder`로 지정합니다.
2. 그 GameObject에 `RubricRecorder` 컴포넌트를 추가합니다(Add Component → "Rubric Recorder").
3. 인스펙터의 **루브릭 정의 데이터팩(`_rubricDefinition`)** 칸에
   `Assets/Modules/TriageTrainer/Resources/Scenario/rubric_definition_disaster.json`을 드래그해 넣습니다.
4. **디버그(`_logDecisions`)** 는 켜 두면 수행/미수행 판정이 콘솔에 출력됩니다(검수 시 권장).

`ScenarioController`는 별도로 연결할 필요가 없습니다(런타임에 `ScenarioController.Instance`를 자동 구독).

## 3. 루브릭 항목 추가·수정

`rubric_definition_disaster.json`의 `items` 배열에 항목을 추가합니다.

```json
{
  "id": "rubric.c.iv_access_left",
  "area": "Circulation",
  "title": "정맥로 확보(좌측, 환자 A)",
  "gateNodeIdentifier": "V017_1",
  "autoSignal": "insert_iv_patient_a_left",
  "perPlayer": false
}
```

- `gateNodeIdentifier`: 시나리오 JSON에서 그 처치를 막는 **Validator 노드의 식별자**(예: `V017_1`).
  이 값이 시나리오 노드 ID와 정확히 일치해야 자동 판정됩니다.
- `area`: `Triage`/`Airway`/`Breathing`/`Circulation`/`Disability`/`Exposure`/`General`.
- 게이트와 연결할 수 없는 항목(의사 전달 등)은 `gateNodeIdentifier`를 비워 두고, 추후 관찰자 수동 체크 대상으로 둡니다.

## 4. 결과 확인 방법

- **세션 종료 시**: `_logDecisions`가 켜져 있으면 CSV가 콘솔에 출력됩니다.
- **수동 출력**: 플레이 중 컴포넌트 우클릭 → `Export Rubric CSV To Console`.
- CSV 컬럼: `sessionId, playerId, itemId, status, retries, updatedAtUtc, note`
  - `playerId`가 `__team__`이면 팀 단위 기록(본 증분 기본).

## 5. 한계 / 후속

- 본 증분은 **팀 단위 기록**입니다. 플레이어별 분배(누가 수행했는지)는 후속(관찰자 모드/권한 연동)에서 확장됩니다.
- 자동 신호가 없는 항목(`pass_*`, 신체 사정 등)은 자동 판정되지 않으며, `RubricRecorder.MarkManual(...)` API로
  외부(관찰자 UI)에서 표기해야 합니다.
- 파일 저장/내보내기 UI, 관찰자 전용 화면은 별도 작업입니다.

## 관련 문서

- [평가 루브릭 수행/미수행 기록 시스템(요구사항)](./evaluation-rubric-recording-system.md)
- [Validator 게이트 타임아웃 설정 가이드](./validator-gate-timeout-setup-guide.md)
- [api-references/TriageTrainer.Scenario.Rubric.RubricRecorder.md](../../../api-references/TriageTrainer.Scenario.Rubric.RubricRecorder.md)
