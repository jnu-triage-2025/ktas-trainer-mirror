# scenario 재난 초기 대응 및 중증도 분류

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 재난 발생 초기 대응과 중증도 분류 |
| 요약 | 병원 내 재난 상황 인지, 역할 선택, 물품 준비, 초기 중증도 분류를 수행한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 원내 방송 시스템, 환자 A, 골절 환자(더미) |
| 주요 장소 | 응급실 트리아지 구역, 처치 준비 구역 |
| 리소스 식별자 - 사운드 | 없음 |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_triage, wp_preproom |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | D001 |

## 시나리오 본문

| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D001 | Dialogue | 원내 방송이 “병원 인근 재난 상황 발생. 응급실 대비 바랍니다.”라고 안내한다. 화면 텍스트로도 동일한 메시지를 표시한다. | C001 |
| C001 | Choice | 역할을 선택한다. 플레이어 A/B/C/D 중 하나를 선택하도록 안내한다. | (optional) |
| C001-OPT1 | ChoiceOption | “플레이어 A: KTAS 분류 담당.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E001 |
| C001-OPT2 | ChoiceOption | “플레이어 B: 환자 처치.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E002 |
| C001-OPT3 | ChoiceOption | “플레이어 C: 환자 처치.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E003 |
| C001-OPT4 | ChoiceOption | “플레이어 D: 약물/수액 전담.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E004 |
| E001 | InvokeEvent | EventIdentifier로 select_role_nurse_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E002 | InvokeEvent | EventIdentifier로 select_role_nurse_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E003 | InvokeEvent | EventIdentifier로 select_role_nurse_c를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E004 | InvokeEvent | EventIdentifier로 select_role_nurse_d를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| D002 | Dialogue | 플레이어들이 재난 상황을 공유하고 초기 대응을 시작한다. | P001 |
| P001 | Parallel | 플레이어 A 대기, 플레이어 B/C 물품 준비, 플레이어 D 수액/약물 준비를 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D003 |
| P001-B1 | ParallelBranch | 브랜치 시작 노드는 E005이며, 플레이어 A가 트리아지 구역에 대기한다. CompletionConditionIdentifier는 CC_A_WAIT로 서술한다. | CC_A_WAIT |
| P001-B2 | ParallelBranch | 브랜치 시작 노드는 E006이며, 플레이어 B/C가 처치 물품을 준비한다. CompletionConditionIdentifier는 CC_BC_READY로 서술한다. | CC_BC_READY |
| P001-B3 | ParallelBranch | 브랜치 시작 노드는 E007이며, 플레이어 D가 수액/약물 준비를 완료한다. CompletionConditionIdentifier는 CC_D_READY로 서술한다. | CC_D_READY |
| E005 | InvokeEvent | EventIdentifier로 nurse_a_wait_triage를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_A_WAIT |
| E006 | InvokeEvent | EventIdentifier로 prep_treatment_supplies를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_BC_READY |
| E007 | InvokeEvent | EventIdentifier로 prep_fluids_and_meds를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_D_READY |
| D003 | Dialogue | 환자 2명이 이송되어 도착한다. 환자 A와 골절 환자(더미)를 확인한다. | E008 |
| E008 | InvokeEvent | EventIdentifier로 start_triage_two_patients를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D004 |
| D004 | Dialogue | 환자 A를 적색으로 분류하여 응급실 내부로 이동시키고, 골절 환자는 분류 구역 침상에 대기시킨다. | E009 |
| E009 | InvokeEvent | EventIdentifier로 triage_assign_red_and_waiting을 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D005 |
| D005 | Dialogue | 초기 대응과 중증도 분류를 완료하고 다음 처치 시나리오로 넘어간다. | (end) |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D005 |
| 종료 연출/설명 | 중증도 분류 완료 후 다음 환자 처치 시나리오로 전환한다. |
