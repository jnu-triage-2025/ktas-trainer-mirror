---
title: "TriageTrainer.ScenarioActionInteractable"
doc_type: api-reference
status: active
updated: 2026-07-18
---

# `ScenarioActionInteractable`

경로: `Assets/Modules/TriageTrainer/Scripts/Scenario/ScenarioActionInteractable.cs`

`IInteractable`, `IInteract`, `IInteractorConditional`, `IInteractToggleable`을 구현하는 TriageTrainer 전용
월드 상호작용 컴포넌트다. 유효한 `PlayerController`가 상호작용하면 선택적으로 GameObject 표시 상태를
바꾸고 `ScenarioInteractionSignals.Raise(completionSignal)`을 호출한다.

| 필드 | 역할 |
|---|---|
| `Display Text` | 상호작용 힌트 문구 |
| `Completion Signal` | 완료 시 올릴 `sig.*` 조건명 |
| `Consume Once` | 완료 후 재상호작용 차단 여부 |
| `Activate/Deactivate On Interact` | 완료와 함께 표시 상태를 바꿀 GameObject 목록 |

상호작용의 표시 상태가 모든 클라이언트에 동기화돼야 할 때에는 단순 GameObject 토글 대신 대상별 서버 권위
구현을 사용한다. 예를 들어 벽 유량계는 `WallAttachedOxyflowmeter`, 환자 처치 표현은
`PatientController.ApplyScenarioDisplayState`/`SetTreatmentDisplayNetworked`를 사용한다.
