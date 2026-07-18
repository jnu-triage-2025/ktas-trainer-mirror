---
title: "아이템 획득형 Quest 흐름 및 UI 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

아이템을 월드에서 직접 클릭해 획득해야 하는 시나리오 구간에서, 대화 UI가 상호작용을 막지 않도록 입력 전환 규칙을 정의하고 Quest 진행을 HUD/패널에 일관되게 표시하기 위한 요구사항이다.

이 요구사항의 목적은 "안내 문구는 보이는데 실제로는 클릭이 안 되어 진행이 막히는" 체감 오류를 제거하고, 사용자가 현재 퀘스트 상태를 직관적으로 확인할 수 있게 만드는 것이다.

## 상세

- 아이템 획득 안내가 발생한 경우(`생리식염수 1L 수액백과 수액세트를 각각 클릭해 획득하세요` 등), Dialogue는 상호작용 가능한 시점에 닫히거나 자동 종료되어야 한다.
- 아이템 획득 목표는 Quest로 등록되어야 하며, QuestPreview HUD와 Quest 전용 UI(패널)에서 동시에 확인 가능해야 한다.
- Quest 전용 UI는 별도 구현체가 없는 상태를 기준으로, 최소 기능(목록/진행도/완료 상태)을 제공하는 패널을 구현 가능하도록 정의한다.
- 개별 Quest 완료 조건은 Scenario `ValidatorNode` 검증 단계와 유사한 구조(조건 평가기 + 타깃 수량/조합)로 구성해야 한다.
- Quest 등록은 identifier 중심으로 처리한다. 즉, 제목/설명/완료조건은 사전 콘텐츠 정의에 저장하고 런타임에서는 identifier를 참조해 인스턴스를 생성한다.
- 진행형 Quest(예: 아이템 여러 개 획득)는 진행도 타입을 사용해 `2/N` 형태로 표시할 수 있어야 한다.

## 기술적 세부 사항

- 권장 데이터 구조(초안):
  - `QuestDefinition`:
    - `identifier`
    - `title`
    - `description`
    - `completionCriteria`(단일/복합 조건)
    - `trackInPreview`(HUD 표시 여부)
  - `QuestProgressValue`:
    - `current`
    - `target`
    - `displayFormat`(기본 `current/target`)
- 권장 조건 평가 인터페이스(초안):
  - `IQuestCompletionCriteria.Evaluate(context) -> QuestCriteriaResult`
  - `QuestCriteriaResult`는 완료 여부와 진행 수치를 함께 반환
- 권장 실행 흐름(초안):
  1. Scenario `QuestControl(Add)`가 `questIdentifier`를 전달
  2. QuestManager가 `QuestDefinitionRegistry`에서 정의 조회
  3. Quest 인스턴스 생성 후 HUD/패널 ViewModel에 브로드캐스트
  4. 아이템 획득 이벤트 수신 시 조건 재평가
  5. 진행도/완료 상태 동기 갱신
- Dialogue-상호작용 전환 규칙(초안):
  - "월드 상호작용 유도" 성격의 Dialogue는 닫힘 이후 Crosshair/InteractionHint가 복원되어야 함
  - 기존 차단형 Dialogue와 충돌하지 않도록 명시 플래그 기반 확장 필요

## 구현 스케줄

- Phase 1 (설계, 0.5~1일)
  - QuestDefinition/Progress/Criteria 스키마 확정
  - Dialogue 전환 정책 확정
- Phase 2 (시스템, 1.5~2일)
  - identifier 기반 Quest 등록/조회 경로 구현
  - 조건 평가기 및 진행도 집계 구현
- Phase 3 (UI, 1~1.5일)
  - QuestPreview HUD 진행도 반영
  - Quest 패널(전용 UI) 최소 사양 구현
- Phase 4 (콘텐츠 반영/검증, 0.5~1일)
  - `disaster_intro` 아이템 획득 구간 연결
  - 멀티플레이/회귀 검증

총 예상: 3.5~5.5일

## 참조

- [scenario:disaster_intro](../../../Assets/Modules/TriageTrainer/Resources/Scenario/disaster_intro.scenario.json)
- [requirement:quest-manager](./quest-manager-requirements.md)
- [requirement:ui-controllers](../ui/ui-controllers-requirements.md)
- [proposal:quest-acquisition-flow](../../../Agents/Proposals/done/2026-06-29-quest-acquisition-flow/Feature Proposal - Quest Acquisition Flow & UI.md)
