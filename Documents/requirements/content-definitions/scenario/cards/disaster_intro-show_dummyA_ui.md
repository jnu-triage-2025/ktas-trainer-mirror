---
title: "기능 카드: show_dummyA_ui"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 기능 카드: show_dummyA_ui

| 항목 | 내용 |
|---|---|
| 기능명 | 더미 A 상태 UI 표시 |
| EventIdentifier | show_dummyA_ui |
| 실행 주체 | 시스템 |
| 대상 | 플레이어 화면 UI 패널 |
| 선행 조건 | 간호사 A가 더미 A 선택 |
| 관련 태그(tags) | triage_lead |
| 입력 | InvokeEvent 호출 |
| 출력 | 더미 A 외견/상태 패널 표시 |
| 완료 조건 | UI가 1회 정상 표시 |
| 실패 처리 | UI 참조 누락 시 로그 + 다음 노드 진행 |

## MVP 구현 메모

- show_patientA_ui와 동일 패턴으로 구현.
- 데이터 소스만 더미 A로 분리.
