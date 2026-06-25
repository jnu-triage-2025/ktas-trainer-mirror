---
title: "Scenario 문서 -> JSON 변환 규칙표"
doc_type: requirement
status: active
updated: 2026-04-14
---

# Scenario 문서 -> JSON 변환 규칙표

이 문서는 시나리오 문서 표현을 현재 엔진 스키마에 맞게 변환할 때 사용하는 기준이다.

## 1) 공통 원칙

- JSON은 반드시 scenario.schema.json 기준으로 작성한다.
- nodeType별 허용 필드만 사용한다.
- 문서의 표현이 엔진과 다르면 엔진 기준으로 치환한다.
- 그래프 상위 `tags`에 시나리오에서 사용할 태그를 사전 선언한다.
- 브랜치/노드에서 선언되지 않은 태그를 사용하면 로더 경고가 발생한다.

참조:
- Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json
- Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/ScenarioGraphLoader.cs

## 2) 값 치환 규칙

### Parallel

- WaitAll -> All
- WaitAny -> Any
- WaitNone -> None

- ByRole -> allocationType = "ByRole" (엔진 정식 지원)
  - 각 브랜치를 자격(requiredPlayerTags/forbiddenPlayerTags)에 맞는 **서로 다른** 플레이어에게 1:1 배정한다.
  - 다인 동시 협력 처치(간호사 B/C/D가 각기 다른 처치를 동시 수행)를 표현하는 기본 모드이다.
  - 역할 의도는 branches[].requiredPlayerTags 로 표현한다(requiredRoleIdentifiers 는 스키마 미지원, 사용 금지).

- Tag 분기 -> branches[].requiredPlayerTags 사용
  - 태그 조건은 배열의 모든 태그를 만족해야 매칭
  - OR 매칭이 필요하면 branches[].requiredPlayerTagsMatchMode = "Any" 사용
  - 생략 시 기본값은 "All"
- 제외 태그 분기 -> branches[].forbiddenPlayerTags 사용
  - 해당 태그를 가진 플레이어는 브랜치 대상에서 제외

### TagModification

- PlayerTagNode 문서 표현은 엔진에서 `TagModification` nodeType으로 저장하는 것을 권장한다.
- 로더는 하위 호환을 위해 `PlayerTag`도 계속 허용한다.

### InvokeEvent

- Immediate -> Immediately
- WaitUntilDone -> WaitUntilDone
- False -> False

### Dialogue

- `.md` 의 `Duration`(자동 진행 시간, System 안내문 등)은 엔진 `autoAdvanceSeconds` 로 매핑한다.
  - `autoAdvanceSeconds > 0`: 표시 후 해당 시간 경과 시 자동 진행(사용자 입력 시 즉시 진행, 타이머 취소).
  - 생략/`null`/`<=0`: 사용자 입력 대기(하위호환).
- 과거(1차 변환)에 `Duration` 을 드롭하고 별도 `Delay` 노드로 보완한 흐름은 `autoAdvanceSeconds` 로 일원화한다.

### Sound

- `SoundResourceIdentifier` 는 `Assets/.../Resources/Sound/<id>`(폴백 `Resources/<id>`) 에 AudioClip 으로 존재해야 한다.
- 클립이 없으면 엔진이 경고 후 스킵한다(진행은 막지 않음). 콘텐츠(클립 배치)는 별도 에셋 작업이다.

### Choice

- ChoiceOptionNode 분리형은 사용하지 않는다.
- ChoiceNode.options 배열로 병합한다.
- ChoiceNode.nextIdentifier는 null 이어야 한다.

### Validator

- 엔진 Validator.condition 은 `PlayerCount*`, `RegistryContains`, `PlayerAssignedTag` 를 지원한다.
- `Click_xxx` / `Apply_xxx` / `Enter_xxx` / `Grab_xxx` 같은 **도메인 인터랙션 완료** 조건은
  enum 으로 직접 표현할 수 없다. 대신 **인터랙션 완료 신호(Signal) + `RegistryContains`(RuntimeState)** 로 게이팅한다(TODO-SPEC-2 채택안).
- "수행해야만 진행"을 강제하려면 Validator 에 `waitForCondition: true` 를 둔다(엔진 정식 지원).
  이때 조건(신호)이 올라올 때까지 진행을 막고 폴링 대기한다. 별도 대기 노드 조합이 더 이상 필요 없다.

#### 게이트 타임아웃·실패 분기 (waitTimeoutSeconds / onWaitTimeout)

`waitForCondition: true` 게이트는 기본적으로 조건이 올라올 때까지 **무한 대기**한다. 신호가
미배선·오설정인 지점에서 세션이 멈추지 않도록(hang 방지), 게이트별로 **선택적 타임아웃**과
타임아웃 시 행동을 지정할 수 있다(2026-06-25 엔진 도입, 하위호환).

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `waitTimeoutSeconds` | number(옵션) | 미지정 | 게이트 타임아웃(초). **미지정/null/0 이하이면 무한 대기(기존 동작)**. 양수이면 그 시간 안에 조건 미충족 시 `onWaitTimeout` 적용. |
| `onWaitTimeout` | string(옵션) | `KeepWaiting` | 타임아웃 시 행동. 아래 4가지. |

`onWaitTimeout` 값:
- `KeepWaiting`(기본): 타임아웃을 무시하고 계속 대기 = **기존 동작과 동일**(하위호환).
- `FailBranch`: `failureNextIdentifier` 로 분기. 미지정/미존재면 `KeepWaiting` 으로 폴백.
- `ForceAdvance`: `nextIdentifier` 로 강제 진행하고, 미수행 기록 이벤트(`OnValidatorWaitTimeout`)를 1회 발생.
- `WarnAndKeepWaiting`: 운영자에게 경고(콘솔+인게임챗) 후 계속 대기.

```json
{
  "nodeType": "Validator",
  "identifier": "V013_4",
  "rootConditions": [
    { "condition": "RegistryContains",
      "validationRules": [
        { "type": "Registry", "condition": "Contains",
          "registryType": "RuntimeState", "registryIdentifier": "sig.suction_patient_a" } ] }
  ],
  "onFailure": "Ignore",
  "waitForCondition": true,
  "waitTimeoutSeconds": 90,
  "onWaitTimeout": "ForceAdvance",
  "nextIdentifier": "D009"
}
```

- 미지정 게이트(기존 104개)는 **동작 변화 0** (KeepWaiting = 무한 대기).
- 브랜치(Parallel) 내부 게이트에서는 전역 분기/종료를 일으키지 않으므로, `FailBranch`/`ForceAdvance`
  모두 "타임아웃 시 게이트를 해제하고 브랜치 체인을 다음 노드로 진행"으로 동작한다(미수행 기록).
- 타임아웃 발생은 `ScenarioController.OnValidatorWaitTimeout(node, behavior)` 이벤트로 노출되어
  평가 기록(루브릭 "미수행" 판정, G-3)에서 구독할 수 있다.

#### 도메인 인터랙션 게이트 변환 (todo.validate.* -> Validator/RegistryContains)

1차 변환에서 `todo.validate.<cond>` (InvokeEvent 스텁) 으로 둔 게이트는 다음으로 치환한다.

```json
{
  "nodeType": "Validator",
  "identifier": "<원래 id>",
  "rootConditions": [
    {
      "condition": "RegistryContains",
      "validationRules": [
        { "type": "Registry", "condition": "Contains",
          "registryType": "RuntimeState", "registryIdentifier": "sig.<cond>" }
      ]
    }
  ],
  "onFailure": "Ignore",
  "waitForCondition": true,
  "nextIdentifier": "<원래 NextIdentifier>"
}
```

- 복수 조건(예: 후두경 블레이드+손잡이)은 `validationRules` 에 룰을 여러 개 둔다(모두 Contains 시 통과).
- 신호 식별자 접두사는 `sig.` 로 통일한다(`ScenarioInteractionSignals.Prefix`).
- 게임플레이 측은 인터랙션 완료 시 `ScenarioInteractionSignals.Raise("<cond>")` 를 호출한다
  (= `Registry.Register(RegistryType.RuntimeState, "sig.<cond>", true)`).
  사이클 반복 등에서 재설정이 필요하면 `ScenarioInteractionSignals.Clear("<cond>")`.
- "수행해야만 진행"을 강제하려면 `waitForCondition: true` 를 둔다(신호가 올라올 때까지 진행 차단).
  생략/`false` 이면 1회 평가 후 `onFailure` 정책을 따른다(하위호환).

> 테스트 보조: 게임플레이의 실제 신호 배선 전에는
> `/scenario signal <cond>` (해제는 `/scenario signal <cond> clear`) 채팅 커맨드로 신호를 수동으로 올려
> 게이트 통과를 통합 테스트할 수 있다.

## 3) 구조 치환 예시

### 예시 A: 문서형 ChoiceOptionNode -> 엔진형 Choice.options

문서형(비권장):
- C002(Choice) + C002-Wrong(ChoiceOptionNode) + C002-Correct(ChoiceOptionNode)

엔진형(권장):
- waitMode = All
- allocationType = ByRole  (각 브랜치를 서로 다른 자격 플레이어에게 1:1 배정)
- branches[].requiredPlayerTags = ["airway_team"], ["bleeding_control"], ["iv_team"] ...
- branches[].forbiddenPlayerTags = ["observer"] ...
- branches[].requiredPlayerTagsMatchMode = "All" | "Any"
- branches[].completionConditionIdentifier = 브랜치 체인 종료 수렴 라벨(예: "CC_B_vitalcheck_patientA")

## 4) 금지 규칙

- 엔진 enum에 없는 문자열 사용 금지
- 스키마에 없는 nodeType 사용 금지
- Choice에 nextIdentifier 문자열 지정 금지 (null만 허용)

## 5) 변환 후 검증 체크

- 시작 노드 존재
- 모든 nextIdentifier/nextNodeIdentifier가 실제 노드를 가리킴
- InvokeEvent 식별자가 event-registry와 일치
- 로더 파싱 에러 없음
