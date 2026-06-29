---
title: "평가 루브릭 수행/미수행 기록 시스템"
domain: content-definitions
progress: "2-implementing"
flags: ["refactor-required"]
---

> 구현 현황(2026-06-25): 자동 기록 **코어 첫 증분**이 구현되었다(`TriageTrainer.Scenario.Rubric.RubricRecorder`).
> 게이트 통과→수행, 게이트 타임아웃(G-6)→미수행을 팀 단위로 자동 기록하고 CSV로 내보낸다.
> 관찰자 모드 UI, 플레이어별 분배, 파일 영속화는 후속 작업이다.
> 설정: [rubric-recorder-setup-guide.md](./rubric-recorder-setup-guide.md) ·
> 레퍼런스: [api-references/TriageTrainer.Scenario.Rubric.RubricRecorder.md](../../../api-references/TriageTrainer.Scenario.Rubric.RubricRecorder.md)

## 개요

이 문서는 재난 시뮬레이션 훈련에서 학습자(간호대학생 4인)의 **수행/미수행을 항목별로 기록하고
집계하는 평가 시스템**의 요구사항을 정의한다. 원본 기획(`_origin/시뮬레이션 사례 + 평가 루브릭`)의
핵심 산출물은 두 가지였다: (1) 환자 대응 시나리오, (2) **평가 루브릭**. 현재 시나리오(흐름)는
`patient_a_critical.json` / `patient_b_c_ct.json` 으로 변환·구현 경로에 올랐으나, **루브릭(채점)은
어떤 산출물에도 매핑되지 않았다.** 즉 게임은 학습자를 진행시킬 수는 있어도 "무엇을 수행했고
무엇을 빠뜨렸는지"를 기록·평가하지 못한다. 이 문서는 그 누락을 메우기 위한 기획 요구사항이다.

기대 효과: 감독/평가자(관찰자 5~6인)가 각 학습자의 ABCDE 수행 항목을 실시간 또는 사후에
체크할 수 있고, 세션 종료 시 항목별 수행/미수행 표가 산출된다.

## 상세

원본 루브릭은 ABCDE 순서를 기준으로, 영역별 세부 항목마다 "수행/미수행"을 표기하는 구조다.
예: 활력징후 측정, 경추 고정, 기관내삽관 보조, 지혈, IV 확보, 가슴압박, 에피네프린 투여,
동공 반사 확인, 신체 노출 등. 또한 사정 퀴즈(AVPU/GCS/근력/KTAS 분류)의 정답·오답·재응시
횟수도 평가의 일부다.

요구사항(기획 관점):

1. **항목 정의**: 루브릭의 각 세부 항목을 데이터로 정의한다. 각 항목은 어떤 시나리오 게이트/사정
   퀴즈와 연결되는지(예: "지혈" 항목 ↔ `apply_gauze` 게이트, "E 사정" ↔ `C005` 선택지)를 명시한다.
2. **수행 판정 근거**: 항목 수행 여부는 가능하면 시스템이 자동 판정한다. 자동 판정이 어려운 항목은
   관찰자가 수동 체크할 수 있어야 한다.
   - 자동: 게이트 통과 신호(`sig.*`) 수신, 사정 퀴즈 정답 선택.
   - 미수행: 게이트가 타임아웃으로 강제 진행되거나(향후 도입), 관찰자가 미수행 표기.
3. **관찰자 모드**: 감독 5~6인이 학습자별 수행 현황을 보고, 수동 항목을 체크/정정할 수 있는 화면.
4. **집계/내보내기**: 세션 종료 시 학습자 4인 × 항목별 수행/미수행 표를 생성하고, 사후 디브리핑을
   위해 내보낼 수 있어야 한다(파일 또는 화면).
5. **재응시/오답 기록**: 사정 퀴즈의 오답·재응시 횟수를 누적 기록한다.

원본 루브릭의 "수행/미수행" 컬럼이 곧 이 시스템의 최소 출력 형식이다.

## 기술적 세부 사항

- **자동 판정 연계점**: 시나리오 엔진의 게이트 통과(`ScenarioInteractionSignals` / Validator
  `RegistryContains(RuntimeState, "sig.*")`)와 사정 퀴즈(`ScenarioChoiceNode`/`ScenarioQuizNode`)가
  수행 판정의 1차 데이터다. 항목↔게이트 매핑 표가 필요하다(아래 신호 목록 참조).
- **미수행 판정의 전제**: 게이트 타임아웃 기능(G-6, 2026-06-25 구현)으로 해결되었다.
  게이트가 `waitTimeoutSeconds` 초과 시 `ScenarioController.OnValidatorWaitTimeout` 이벤트가 발생하며,
  `RubricRecorder` 가 이를 구독하여 `ForceAdvance`/`FailBranch` 타임아웃을 "미수행"으로 기록한다.
  단, 타임아웃이 설정되지 않은(무한 대기) 게이트는 여전히 미수행을 자동 판정하지 못한다(설정 필요).
- **수동 항목**: `pass_*`(의사 전달), 신체 사정 등 자동 신호가 없는 항목은 관찰자 수동 체크 UI 필요.
- **관찰자 모드**: 별도 플레이어 권한(관찰/평가)과 UI 가 필요하다. 현재 멀티플레이 인프라의 역할/태그
  (`PlayerTagService`)를 활용하되, 관찰자 전용 화면은 신규 UI 작업이다.
- **데이터 모델 후보**: `RubricItem { id, area(ABCDE), title, autoSignal?, perPlayer }`,
  `RubricResult { sessionId, playerId, itemId, status(Performed/NotPerformed/NA), retries, timestamp }`.
- **신호 커버리지**: 항목별 자동 판정 가능 여부는
  [`interaction-signal-integration-spec.md`](./interaction-signal-integration-spec.md) §2/§5 의
  wired/notWired 분류를 그대로 참조한다.

## 참조

- 원본 기획: `Documents/requirements/content-definitions/scenario/_origin/시뮬레이션 사례 + 평가 루브릭 (4차 수정).txt`
- [인터랙션 완료 신호 연결 명세](./interaction-signal-integration-spec.md)
- [환자 A 시나리오](./patient_a_critical.md) · [환자 B/C 시나리오](./patient_b_c_ct.md)
- 게이트 타임아웃 제안: `Agents/Proposals/done/2026-06-25-scenario-validator-gate-timeout/Feature Proposal - Scenario Validator Gate Timeout.md`
- 기록 코어 설정 가이드: [rubric-recorder-setup-guide.md](./rubric-recorder-setup-guide.md)
