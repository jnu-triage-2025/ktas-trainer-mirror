---
title: "기능 카드: B_C_D_to_triage"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 기능 카드: B_C_D_to_triage

| 항목 | 내용 |
|---|---|
| 기능명 | 간호사 B/C/D 트리아지 이동 연출 |
| EventIdentifier | B_C_D_to_triage |
| 실행 주체 | 시스템 |
| 대상 | 간호사 B, C, D 엔티티 |
| 선행 조건 | 간호사 A 분류 완료 후 호출 |
| 관련 태그(tags) | airway_team, support_team, iv_team |
| 입력 | InvokeEvent 호출 |
| 출력 | 3명 이동 또는 도착 연출 |
| 완료 조건 | 3명 트리아지 구역 도달 처리 |
| 실패 처리 | 일부 엔티티 누락 시 가능한 엔티티만 처리 후 진행 |

## MVP 구현 메모

- 1차는 placeholder 로그 핸들러.
- 2차에서 순간이동(안전) 구현.
- 3차에서 보간 이동/애니메이션 확장.
