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

[""로 정의된 내용은 각각의 특정 플레이어 혹은 모든 플레이어 및 감독자(관전자) 모두 확인할 수 있도록 텍스트 및 음성으로 출력한다. 음성이 불가피한 경우 텍스트가 모니터에 출력되었으니 확인하라는 알람음을 추가하도록 한다.]
| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D001 | Dialogue | 원내 방송이 “병원 인근 지하철역에서 폭발 사고 발생. 재난 상황 발령되었습니다. 응급실 대비 바랍니다.”라고 안내한다. 화면 텍스트로도 동일한 메시지를 표시한다. | C001 |
| C001 | Choice | 역할을 선택한다. 플레이어 A/B/C/D 중 하나를 선택하도록 안내하고 UI 창을 출력한다. | C001-OPT1, C001-OPT2, C001-OPT3, C001-OPT4 |
| C001-OPT1 | ChoiceOption | “플레이어 A: KTAS 분류 담당. 중증도 분류 구역으로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E001 |
| C001-OPT2 | ChoiceOption | “플레이어 B: 환자 처치. 처치 구역으로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E002 |
| C001-OPT3 | ChoiceOption | “플레이어 C: 환자 처치. 처치 구역으로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E003 |
| C001-OPT4 | ChoiceOption | “플레이어 D: 약물/수액 전담. 준비실로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E004 |
| E001 | InvokeEvent | EventIdentifier로 select_role_nurse_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E002 | InvokeEvent | EventIdentifier로 select_role_nurse_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E003 | InvokeEvent | EventIdentifier로 select_role_nurse_c를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E004 | InvokeEvent | EventIdentifier로 select_role_nurse_d를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| D002 | Dialogue | 플레이어들이 재난 상황을 공유하고 초기 대응을 시작한다. | P001 |
| P001 | Parallel | 플레이어 A 중증도 분류 구역에서 대기, 플레이어 B/C 처치 구역에서 물품 준비, 플레이어 D 준비실에서 수액/약물 준비를 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D003 |
| P001-B1 | ParallelBranch | 브랜치 시작 노드는 E005이며, 플레이어 A가 트리아지 구역에 대기한다. CompletionConditionIdentifier는 CC_A_WAIT로 서술한다. | CC_A_WAIT |
| P001-B2 | ParallelBranch | 브랜치 시작 노드는 E006이며, 플레이어 B/C가 처치 물품을 준비한다. CompletionConditionIdentifier는 CC_BC_READY로 서술한다. | CC_BC_READY |
| P001-B3 | ParallelBranch | 브랜치 시작 노드는 E007이며, 플레이어 D가 수액/약물 준비를 완료한다. CompletionConditionIdentifier는 CC_D_READY로 서술한다. | CC_D_READY |
| E005 | InvokeEvent | EventIdentifier로 nurse_a_wait_triage를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_A_WAIT |
| D003_A | Dialogue | 중증도 분류 구역에 도착한 플레이어 A에게 "카트 위 활력징후 측정도구를 획득하십시오."라고 한내한다. 화면 텍스트로도 동일한 메세지를 표시한다. | CC_A_WAIT |
| E006 | InvokeEvent | EventIdentifier로 prep_treatment_supplies를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_BC_READY |
| D003_BC | Dialogue | 처치 구역에 도착한 플레이어 B/C에게 "물품의 위치를 파악하십시오."라고 한내한다. 화면 텍스트로도 동일한 메세지를 표시한다. | CC_BC_READY |
| E007 | InvokeEvent | EventIdentifier로 prep_fluids_and_meds를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_D_READY |
| D003_D_1 | Dialogue | 준비실에 도착한 플레이어 D에게 "수액과 수액세트, 혈액과 혈액세트를 연결하십시오."라고 안내한다. 화면 텍스트로도 동일한 메세지를 표시한다. | D003_D_2 |
| D003_D_2 | Dialogue | [플레이어 D 전용] "수액과 수액세트를 클릭하면 연결되어 준비된 상태가 됩니다. 먼저 생리식염수 1L 수액백과 수액세트를 획득해 연결하세요." | V000_D_1 |
| V000_D_1 | Validator | [D 1단계 - 약물준비 시작] 생리식염수 1L 수액과 수액세트를 클릭해 획득하면 준비된 상태로 아이템이 변환된다. 수액세트가 생리식염수 수액백에 연결된 모습은 인벤토리에서 보여주지 않고 (기존 수액백 외형을 그대로 이용한다) 아이템의 텍스트 출력만을 "준비된 생리식염수 1L 수액백"으로 출력한다. Condition은 Click_ns1, Click_ivset이며, TargetCount는 2이다. | D003_D_3 |
| D003_D_3 | Dialogue | "플라즈마 솔루션 1L 수액백과 수액세트를 획득해 연결하세요." | V000_D_2 |
| V000_D_2 | Validator | [D 2단계] 플라즈마 솔루션 1L 수액백과 수액세트를 클릭해 획득하면 준비된 상태로 아이템이 변환된다.  수액세트가 플라즈마 솔루션 수액백에 연결된 모습은 인벤토리에서 보여주지 않고 (기존 수액백 외형을 그대로 이용한다) 아이템의 텍스트 출력만을 "준비된 플라즈마 솔루션 1L 수액백"으로 출력한다. Condition은 Click_ps1, Click_ivset이며, TargetCount는 2이다. | CC_D_READY |
| D004 | Dialogue | 환자 2명이 이송되어 도착한다. 플레이어 A는 환자 A와 환자 B(더미)를 확인한다. 플레이어 A에게 "두 환자를 각각 클릭하여 외양을 확인하고 우선순위를 결정하십시오."라고 안내한다. 화면 텍스트로도 동일한 메세지를 표시한다." | V001_A |
| P002 | Parallel | 플레이어 B/C/D가 파악 및 준비를 하는 사이, 플레이어 A는 두 환자를 클릭해 외양을 파악한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D008 |
| P002-B1 | ParallelBranch | [플레이어 A 전용] 브랜치 시작 노드는 V001이며, 플레이어 A가 순서에 상관없이 각 환자를 클릭해 외양을 파악한다. CompletionConditionIdentifier는 CC_A_TRIAGE_DONE으로 서술한다. | CC_A_TRIAGE_DONE |
| P002-B2 | ParallelBranch | [플레이어 B, C, D 전용] 브랜치 시작 노드는 D005이며, 플레이어 B/C/D는 기존 임무를 수행한다. CompletionIdentifier는 CC_A_TRIAGE_DONE으로 서술한다. | CC_A_TRIAGE_DONE |
| V001_A | Validator | 플레이어 A는 환자 A를 클릭해 외양을 확인한다. Condition은 Click_PatientA, TargetCount는 1이다. | D005 |
| D005 | Dialogue | [플레이어 A 전용] "환자 A: 흉부 관통상 및 흉부 출혈 관찰됨. 현재 의식 drowsy하며 피부는 창백하고 식은땀이 흐르는 상태임. 환자를 불렀을 때 반응이 느림." | V001_B |
| V001_B | Validator | 플레이어 A는 환자 B를 클릭해 외양을 확인한다. Condition은 Click_PatientDummy1, TargetCount는 1이다. | D006 |
| D006 | Dialogue | [플레이어 A 전용] "환자 B: 양 팔 찰과상. 현재 의식 alert하며 피부는 따뜻하고 반응이 양호함." | C002 |
| D007 | Dialogue | [플레이어 B, C, D 전용] 플레이어 B/C/D에게 "준비한 물품과 약물을 최종 점검 하십시오. 간호사 A의 분류가 완료될 때까지 준비합니다."라고 안내한다. 화면 텍스트로도 동일한 메세지를 표시한다. | CC_A_TRIAGE_DONE |
| C002 | Choice | 어떤 환자를 먼저 응급실로 이송하겠습니까? | C002-OPT1, C002-OPT2 |
| C002-OPT1 | ChoiceOption | 환자 A를 이송한다. | E008 |
| C002-OPT2 | ChoiceOption | 환자 B를 이송한다. | D008 |
| D008 | Dialogue | "환자 B는 상태가 비교적 안정적입니다. 응급도가 더 높은 환자를 선택하세요." | C002 |
| E008 | InvokeEvent | EventIdentifier로 start_triage_two_patients를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D009 |
| D009 | Dialogue | 환자 A를 적색으로 분류하여 응급실 내부로 이동시키고, 골절 환자는 분류 구역 침상에 대기시킨다. | E009 |
| E009 | InvokeEvent | EventIdentifier로 triage_assign_red_and_waiting을 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D010 |
| D010 | Dialogue | 초기 대응과 중증도 분류를 완료하고 다음 처치 시나리오로 넘어간다. | (end) |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D010 |
| 종료 연출/설명 | 중증도 분류 완료 후 다음 환자 처치 시나리오로 전환한다. |
