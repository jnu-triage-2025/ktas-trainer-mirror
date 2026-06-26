---
title: "Validator 게이트 타임아웃·실패 분기 설정 가이드"
doc_type: requirement
domain: content-definitions
progress: "3-implemented"
status: active
updated: 2026-06-25
---

# Validator 게이트 타임아웃·실패 분기 설정 가이드

이 가이드는 비개발 운영자가 시나리오 JSON에서 **Validator 게이트의 타임아웃**을 설정해
"신호가 안 올라오면 세션이 멈추는(hang)" 문제를 막는 방법을 설명합니다.

## 1. 무엇이 바뀌었나

- 기존: `waitForCondition: true` 게이트는 조건(인터랙션 완료 신호)이 올라올 때까지 **영원히 대기**했습니다.
  신호가 배선되지 않은 지점에 도달하면 게임이 그 자리에서 멈췄습니다.
- 변경: 게이트마다 **제한 시간(`waitTimeoutSeconds`)**과 **시간 초과 시 행동(`onWaitTimeout`)**을
  지정할 수 있습니다. 지정하지 않으면 **기존과 똑같이 무한 대기**하므로, 기존 시나리오에는 아무 영향이 없습니다.

## 2. JSON에 추가하는 두 필드

Validator 노드(JSON) 안에 아래 두 줄을 추가합니다.

```json
{
  "nodeType": "Validator",
  "identifier": "V013_4",
  "rootConditions": [ ... ],
  "onFailure": "Ignore",
  "waitForCondition": true,

  "waitTimeoutSeconds": 90,
  "onWaitTimeout": "ForceAdvance",

  "nextIdentifier": "D009"
}
```

- `waitTimeoutSeconds`: 기다릴 최대 시간(초). 예: `90`이면 90초.
  - 적지 않거나 `0` 이하이면 → **무한 대기(기존 동작)**.
- `onWaitTimeout`: 시간이 초과됐을 때 무엇을 할지. 아래 4가지 중 하나.

| 값 | 시간 초과 시 행동 | 언제 쓰나 |
|---|---|---|
| `KeepWaiting` | 그냥 계속 기다림 (기본값) | 변화 없이 두고 싶을 때 |
| `ForceAdvance` | 다음 노드(`nextIdentifier`)로 그냥 넘어감 + "미수행" 기록 | 데모/수업을 멈추지 않게 하고 싶을 때(권장 기본) |
| `FailBranch` | 실패용 노드(`failureNextIdentifier`)로 보냄 | 미수행 시 별도 안내/재시도 흐름이 있을 때 |
| `WarnAndKeepWaiting` | 운영자에게 경고 메시지를 띄우고 계속 기다림 | 사람이 개입해 처리할 때 |

> `FailBranch`를 쓰려면 같은 노드에 `failureNextIdentifier`(실패 시 갈 노드 ID)도 지정해야 합니다.
> 지정하지 않으면 자동으로 `KeepWaiting`처럼 동작합니다.

## 3. 권장 설정 예

- **데모/수업용(멈추면 안 됨)**: 핵심 처치 게이트에
  `waitTimeoutSeconds`(예: 60~120)와 `onWaitTimeout: "ForceAdvance"`를 지정.
  → 학습자가 시간 내 수행하지 못해도 자동으로 다음으로 넘어가며, "미수행"이 기록됩니다.
- **평가 모드(미수행을 명확히 분기)**: `onWaitTimeout: "FailBranch"` + `failureNextIdentifier`로
  미수행 안내 노드를 연결.
- **그대로 두기**: 아무것도 지정하지 않으면 기존처럼 무한 대기.

## 4. 테스트 방법 (Unity 에디터/플레이 중)

1. 시나리오를 실행하고 타임아웃을 짧게(예: `5`) 설정한 게이트까지 진행합니다.
2. 신호를 올리지 않고 5초를 기다립니다.
3. `onWaitTimeout` 설정대로 동작하는지 확인합니다.
   - `ForceAdvance` → 5초 후 다음 노드로 진행.
   - `FailBranch` → 5초 후 실패 노드로 이동.
   - `WarnAndKeepWaiting` → 채팅창에 경고가 뜨고 계속 대기.
4. 반대로, 5초 이내에 `/scenario signal <조건명>` 채팅 커맨드로 신호를 올리면
   타임아웃 없이 정상 진행되는지 확인합니다.

## 5. 주의사항

- 이 기능은 공용 모듈(`MultiplayerInfrastructure`) 변경이므로, **미지정 시 기존 동작을 그대로 유지**하도록
  설계되었습니다. 기존 시나리오(`patient_a_critical.json` 등 104개 게이트)는 수정하지 않는 한 동작이 바뀌지 않습니다.
- 병렬(Parallel) 브랜치 안의 게이트에서는 `ForceAdvance`와 `FailBranch`가 모두 "게이트를 풀고 그 브랜치의
  다음 노드로 진행"으로 동작합니다(브랜치 내부에는 전역 분기/종료가 없기 때문).
- 타임아웃이 발생하면 `ScenarioController.OnValidatorWaitTimeout` 이벤트가 1회 발생하여,
  평가 기록 시스템이 "어느 게이트가 미수행되었는지"를 받아갈 수 있습니다(평가 기록 연동은 별도 작업).

## 관련 문서

- [json-conversion-rules.md](./json-conversion-rules.md) — Validator 섹션(필드 표기)
- [interaction-signal-integration-spec.md](./interaction-signal-integration-spec.md) — §0.1 게이트 정책(G-6)
- [api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md](../../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md) — §7-1 게이트 타임아웃 동작/이벤트
- 제안서: `Agents/Proposals/scheduled/2026-06-25-scenario-validator-gate-timeout/`
