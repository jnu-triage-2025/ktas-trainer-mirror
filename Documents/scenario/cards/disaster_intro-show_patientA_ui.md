# 기능 카드: show_patientA_ui

| 항목 | 내용 |
|---|---|
| 기능명 | 환자 A 상태 UI 표시 |
| EventIdentifier | show_patientA_ui |
| 실행 주체 | 시스템 |
| 대상 | 플레이어 화면 UI 패널 |
| 선행 조건 | 간호사 A가 환자 A 선택 |
| 관련 태그(tags) | triage_lead |
| 입력 | InvokeEvent 호출 |
| 출력 | 환자 A 외견/상태 패널 표시 |
| 완료 조건 | UI가 1회 정상 표시 |
| 실패 처리 | UI 참조 누락 시 로그 + 다음 노드 진행 |

## MVP 구현 메모

- 1차는 placeholder 로그 핸들러.
- 2차에서 고정 문자열 패널 표시.
- 3차에서 환자 상태 데이터 바인딩.
