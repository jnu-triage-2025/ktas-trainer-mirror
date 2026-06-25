# Feature Proposal: Scenario Validator 게이트 타임아웃·실패 분기

- 작성일: 2026-06-25
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/`
- 관련 제안: `Agents/Proposals/2026-06-24-scenario-parallel-execution/`
- 관련 명세: `Documents/requirements/content-definitions/scenario/interaction-signal-integration-spec.md`

### 개요

`Validator 게이트 타임아웃·실패 분기` 기능은 `waitForCondition=true` 로 동작하는 시나리오
Validator 게이트가 **무한 대기에 빠지지 않고**, 일정 시간/조건 초과 시 정해진 실패 경로로
빠져나가거나 운영자에게 경고하도록 하는 안전장치 구현입니다. 이 기능은 게이트에 선택적
타임아웃과 타임아웃 시 행동(대기 지속 / 실패 분기 / 강제 진행 / 운영자 경고)을 부여하여,
"수행해야 진행"이라는 교육 의도(평가 루브릭)를 **데모와 운영을 멈추지 않으면서** 점진적으로
활성화할 수 있게 합니다.

- 요약: `ScenarioValidatorNode.WaitForCondition` 게이트에 `WaitTimeoutSeconds`(옵션)와
  `OnWaitTimeout`(정책) 을 추가한다.
- 의도/목표: 신호 미배선·오설정 상태에서도 시나리오가 **영구 정지(hang)** 하지 않도록 하고,
  설계 의도(수행 강제)를 운영 위험 없이 단계적으로 도입한다.
- 주요 맥락: 현재 변환된 104개 Validator 는 전부 `onFailure=Ignore` 이며 `waitForCondition=true`
  다. 신호가 안 올라오면 게이트는 통과(Advance)하지만, 만약 운영 강화를 위해 `onFailure` 를
  Panic/Branching 으로 바꾸면 미배선 게이트에서 **코루틴이 영구 대기**한다(아래 근거).
- 기술적 제약: `MultiplayerInfrastructure` 는 타 프로젝트 재사용을 전제로 하므로 신중히 변경한다.
  하위호환(필드 미지정 시 기존 동작 유지)이 필수다.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자/교육 설계자**로서, "필수 처치를 수행해야만 다음 단계로 진행"
하도록 게이트를 강제(blocking)하고 싶다. 왜냐하면 그것이 원본 평가 루브릭의 핵심 교육 의도이기
때문이다.

그러나 현재 엔진은 `waitForCondition=true` 게이트에서 신호가 끝내 올라오지 않으면 **타임아웃도
실패 분기도 없이 무한 대기**한다(근거: `ScenarioController.cs` 의 `ExecuteValidatorNode` /
`ExecuteValidatorGate` 가 `yield return new WaitUntil(() => EvaluateValidator(node));` 만 수행하며,
`OnFailure`/`FailureNextIdentifier` 처리는 `waitForCondition=false` 분기에서만 실행됨).

따라서 신호 배선이 끝나기 전에 게이트를 blocking 으로 강화하면, 미배선/오설정 지점에서 세션 전체가
멈춰 데모와 수업이 불가능해진다. 이 진퇴양난이 G-2(게이트 정책)를 막는 직접 원인이다.

### 사용자 경험 목표

- 운영자: 게이트별로 "이 시간 안에 수행되지 않으면 (a)계속 대기 (b)실패 분기로 보냄
  (c)강제 진행하고 미수행으로 기록 (d)운영자에게 경고" 를 선택할 수 있다.
- 학습자: 정상 수행 시에는 기존과 동일하게 즉시 진행한다. 변화 없음.
- 감독/평가자: 타임아웃 발생이 로그/평가 기록으로 남아 "미수행" 판정 근거가 된다(G-3 연계).

### 제안

`ScenarioValidatorNode` 에 하위호환 옵션 2개 추가:

```
// 신규(옵션). null/<=0 이면 기존 동작(무한 대기) 유지 → 하위호환.
public float? WaitTimeoutSeconds { get; set; }

// 타임아웃 시 정책. 기본값은 기존 동작과 동일한 "계속 대기".
public ScenarioValidatorWaitTimeoutBehavior OnWaitTimeout { get; set; }
   = ScenarioValidatorWaitTimeoutBehavior.KeepWaiting;
```

```
public enum ScenarioValidatorWaitTimeoutBehavior
{
  KeepWaiting,        // 기존 동작(무한 대기). 기본값.
  FailBranch,         // FailureNextIdentifier 로 분기(없으면 KeepWaiting 으로 폴백).
  ForceAdvance,       // NextIdentifier 로 강제 진행(미수행 기록과 함께).
  WarnAndKeepWaiting  // 운영자 경고(콘솔/인게임챗) 후 계속 대기.
}
```

`ScenarioController` 의 게이트 대기 지점을 `WaitUntil` 단독에서 "조건 충족 OR 타임아웃" 경합으로
교체한다(의사 코드):

```
float deadline = (node.WaitTimeoutSeconds is > 0f) ? Time.time + node.WaitTimeoutSeconds.Value : float.PositiveInfinity;
yield return new WaitUntil(() => EvaluateValidator(node) || Time.time >= deadline);
if (!EvaluateValidator(node)) { /* 타임아웃 → OnWaitTimeout 정책 적용 */ }
```

서버 권한 실행(`server-authoritative-execution-spec.md`)과 정합하도록, 타임아웃 판정은 게이트를
구동하는 권위 컨텍스트(서버)에서 수행한다.

### 자세한 달성 목표

1. 필드 미지정 JSON(기존 104개 게이트)은 **동작 변화 0** (KeepWaiting = 현재와 동일).
2. 운영자가 게이트별 `waitTimeoutSeconds` + `onWaitTimeout` 만 지정하면 hang 없이 강화 가능.
3. 타임아웃 발생이 평가 기록 훅(G-3)으로 전달 가능하도록 이벤트/콜백 노출.
4. CPR 반복 구간의 `Clear(...)` 리셋과 충돌하지 않음.

### 문서화

- `interaction-signal-integration-spec.md` §0(현재 상태)과 §5 체크리스트에 타임아웃 정책 사용법 추가.
- 변환 규칙 문서(`json-conversion-rules.md`)의 Validator 섹션에 신규 필드 표기.
- 본 제안 채택 시 `Documents/requirements` 색인/링크 검증(`Tools/validate-documentation-links.sh`) 수행.

### 가용성과 테스트

- 위험: `MultiplayerInfrastructure` 변경. 하위호환을 깨면 타 시나리오(샘플/리그레션 JSON)에 영향.
  → 기본값을 KeepWaiting 으로 고정하여 위험 최소화.
- 테스트:
  - 기존 리그레션 JSON(`validating_full.json`, `tag_match_mode_regression.json`)이 동작 변화 없이 통과.
  - 신규: 타임아웃→FailBranch/ForceAdvance/Warn 각 정책의 단위 동작 검증.
  - `/scenario signal <cond>` 커맨드로 타임아웃 전후 통과/분기 수동 검증.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 미배선 게이트를 blocking 으로 설정해도 세션이 hang 되지 않고, 지정 시간 후 정책대로
  진행/분기/경고된다.
- 수용 기준:
  1. `waitTimeoutSeconds` 미지정 게이트의 런타임 동작이 변경 전과 동일(회귀 0).
  2. `onWaitTimeout=ForceAdvance` 설정 게이트가 신호 없이도 시간 경과 후 `NextIdentifier` 로 진행하며
     "미수행" 신호/이벤트를 1회 발생.
  3. `onWaitTimeout=FailBranch` 가 `FailureNextIdentifier` 로 정확히 분기.

### 링크, 참고사항

- 무한 대기 근거: `ScenarioController.cs` `ExecuteValidatorNode`(waitForCondition 분기), `ExecuteValidatorGate`.
- 신호 배선 현황: `interaction-signal-integration-spec.md` §2, 각 `*.unsupported.flags.json` 의 `gateSignalCoverage`.
- 선행 제안: `Agents/Proposals/2026-06-24-scenario-parallel-execution/`.
