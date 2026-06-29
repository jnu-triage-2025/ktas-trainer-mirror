---
title: "IndevScene 시나리오 기능 검증 가이드"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-26
---

# IndevScene 시나리오 기능 검증 가이드

평면 월드맵인 IndevScene 에서, 임시 오브젝트와 디버그 도구로 시나리오 게이트/신호/평가 기록/처치 표현이
정상 동작하는지 검증하는 절차입니다. 대상 기능은 브랜치 `feat/scenario-validator-gate-timeout` 의 구현입니다.

## 0. 시작(서버/클라)

1. Unity 에서 IndevScene 을 ▶ 로 Play.
2. FishNet HUD 로 **Host**(Server + Client) 시작.
3. `/scenario list` 에 `patient_a_critical`, `patient_b_c_ct` 가 보이는지 확인(그래프 등록 OK).
4. `PlayerController` 가 스폰되고 이동/조준이 되는지 확인.

> 시나리오는 자동 시작되지 않는다. 아래 각 검증에서 `/scenario execute <대상> <id>` 로 시작한다.

## 1. 검증 도구

### 1-1. `/scenario signal` 채팅 커맨드
- `/scenario signal <조건명>` : 신호 올리기(예: `/scenario signal apply_gauze`).
- `/scenario signal <조건명> clear` : 신호 내리기.
- 게이트가 그 신호를 기다리고 있으면 즉시 통과한다.

### 1-2. `DebugSignalEmitter` (검증용 컴포넌트)
임의의 `sig.*` 를 콜라이더 진입 또는 키 입력으로 발생시킨다.
- 빈 GameObject + (선택) `BoxCollider`(IsTrigger=on) + `DebugSignalEmitter`.
- `_signals` 에 발생시킬 조건명 입력(접두사 sig. 제외).
- `_raiseOnTriggerEnter`(플레이어 진입 시), `_raiseKey`/`_clearKey`(키 입력 시), ContextMenu `Raise/Clear Signals Now`.
- → **구역 진입(enter/arrive)** 흉내, 또는 미구현 신호를 임시로 올려 게이트 흐름 통과 검증에 사용.

### 1-3. `PatientController` Debug ContextMenu
- 환자 컴포넌트 우클릭 → `Debug/Show(또는 Hide) Treatment Display` : `_debugTreatmentDisplay` 가 가리키는 처치 표현을
  아이템 없이 토글(데이터 플래그 + 자식 GameObject SetActive 검증).
- `Debug/Apply Item Use` : `_debugItemIdentifier`(예 `gauze`) 사용을 흉내 → 표현 + 신호 동시 검증.

### 1-4. `RubricRecorder`
- 콘솔 로그(`_logDecisions=true`) + 컴포넌트 우클릭 `Export Rubric CSV To Console`.

## 2. 임시 오브젝트 배치 (평면맵)

식별자만 맞으면 위치는 무관하다.

| 검증 대상 | 배치 | 핵심 설정 |
|---|---|---|
| 구역 진입 | 빈 GO + BoxCollider(Trigger) + `ScenarioTriggerZone` **또는** `DebugSignalEmitter` | `_raiseSignalsOnEnter`/`_signals` = `enter_triage_zone` |
| 환자/사정/처치표현 | 환자 프리팹(PatientTypeA) 또는 GO + `PatientController`(+`PatientDisplayState`) | `Identifier=patient_a`, DisplayState.ChildGameObjects 에 임시 오브젝트 연결 |
| 아이템 사용 | `MedicalItem` 프리팹 | `Identifier=gauze`/`neckstabilizer`/`ambubag` 등 |
| 평가 기록 | GO + `RubricRecorder` | `_rubricDefinition=rubric_definition_disaster.json` |

## 3. 기능별 검증 절차

### G-6 게이트 타임아웃
1. 테스트 시나리오 Validator 에 `waitTimeoutSeconds: 5`, `onWaitTimeout: "ForceAdvance"` 임시 지정
   (또는 rubric 매핑 게이트 사용, 기본 120초라 짧게 보려면 단축).
2. `/scenario execute ... patient_a_critical` 로 시작 → 해당 게이트에서 신호 없이 대기.
3. 5초 후 다음 노드로 진행 + 콘솔 `[ScenarioController] Validator gate '...' timed out ... ForceAdvance`.
4. 타임아웃 전 `/scenario signal <조건명>` → 즉시 통과(타임아웃 미발생) = 회귀 확인.
5. `waitTimeoutSeconds` 미지정 게이트는 무한 대기(기존 동작) 확인.

### G-3 평가 기록
1. `RubricRecorder` 배치 후 시나리오 시작 → 콘솔 `세션 시작`.
2. 매핑 게이트(예 `V016_2`) 통과 → `수행: rubric.c.bleeding_control_gauze ...`.
3. 다른 매핑 게이트에서 타임아웃 → `미수행: ... 타임아웃(ForceAdvance)`.
4. `Export Rubric CSV To Console` → status/Performed/NotPerformed 확인.

### S-2 구역 진입
1. `ScenarioTriggerZone._raiseSignalsOnEnter=enter_triage_zone`(또는 `DebugSignalEmitter`) 박스 배치.
2. 플레이어가 진입 → 콘솔 `Raising enter signal 'sig.enter_triage_zone'` + 대응 게이트 통과.

### S-2 아이템 사용 → 처치 표현 + 신호
1. 환자(`patient_a`) + `PatientDisplayState.ChildGameObjects.GauzePatchedOnThorax` 에 임시 큐브(비활성) 연결.
2. 거즈(`gauze`) 아이템을 들고 환자 조준 후 사용 →
   - 큐브 활성화(시각), `DisplayState.GauzePatchedOnThorax=true`(인스펙터 확인),
   - `sig.apply_gauze` 발생 → `V016_2` 통과, RubricRecorder "수행".
3. 아이템 없이 단독 검증: 환자 ContextMenu `Debug/Apply Item Use`(`_debugItemIdentifier=gauze`).
4. 조준 빗나감 시 무동작(안전) 확인.

### S-2 사정(check_*)
1. 환자 클릭 → 상호작용 힌트에 "의식상태 사정/맥박 확인/GCS 재사정/활력징후 사정" 노출(코드 기본값).
2. 사정 수행 → `sig.check_avpu_gcs_patient_a` 등 → 대응 게이트(`V012`) 통과.
3. 비표준 `check_gcs_a_rosc` 는 환자 Assess Actions 인스펙터에 `AssessSignal=check_gcs_a_rosc` 입력 후 확인.

### S-3 병렬 Reallocation (소수 인원)
1. **Host 1인**으로 시작.
2. `patient_a_critical` 병렬 구간 진입 → 모든 브랜치가 라운드로빈 재배정되어 실행, 세션 미중단.
3. 각 브랜치 게이트는 `/scenario signal` 또는 `DebugSignalEmitter` 로 통과시켜 합류(CC_*) 확인.

## 4. 엔드투엔드
- `/scenario signal` 를 순차로 올려(또는 실제 조작) `patient_a_critical`(D037), `patient_b_c_ct`(N092) 완주.
- 순수 게임플레이만으로 어디까지 가는지(첫 미배선 게이트) 기록.

## 5. 주의
- `DebugSignalEmitter`, `PatientController` Debug ContextMenu 는 **검증 전용**이다. 프로덕션 씬/프리팹에는 두지 않는다.
- 처치 **시각 오브젝트**는 `PatientDisplayState.ChildGameObjects` 데이터 연결이 있어야 보인다(콘텐츠).
- 신호명 정합이 핵심이다(예: `apply_gauze`, `check_avpu_gcs_patient_a`). 게이트 조건명과 1:1 일치해야 통과한다.

## 관련 문서
- [아이템 처치/사용 신호·처치 시각 표현 설정 가이드](./item-apply-signal-setup-guide.md)
- [인터랙션 완료 신호 연결 명세](./interaction-signal-integration-spec.md)
- [Validator 게이트 타임아웃 설정 가이드](./validator-gate-timeout-setup-guide.md)
- [평가 루브릭 기록 코어(RubricRecorder) 설정 가이드](./rubric-recorder-setup-guide.md)
