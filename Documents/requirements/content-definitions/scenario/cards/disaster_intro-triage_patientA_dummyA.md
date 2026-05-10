---
title: "기능 카드: triage_patientA_dummyA"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 기능 카드: triage_patientA_dummyA

| 항목 | 내용 |
|---|---|
| 기능명 | 환자 A + 더미 A 트리아지 구역 등장 |
| EventIdentifier | triage_patientA_dummyA |
| 실행 주체 | 시스템 |
| 대상 | 환자 A, 더미 A, 스트레처 |
| 선행 조건 | 간호사 A 준비 완료 이후 호출 |
| 관련 태그(tags) | triage_lead, airway_team, support_team, iv_team |
| 입력 | InvokeEvent 호출 |
| 출력 | 두 환자 등장 연출, 상호작용 가능 상태 |
| 완료 조건 | 환자 오브젝트 활성 + 지정 위치 도달 |
| 실패 처리 | 타겟 누락 시 로그 + 즉시 종료(시나리오 정지 금지) |

## MVP 구현 메모

- 1차는 placeholder 로그 핸들러 유지.
- 2차에서 환자/베드 활성화 및 위치 이동을 추가.
- MultiplayerInfrastructure 수정 없이 TriageTrainer 컴포넌트만 사용.
