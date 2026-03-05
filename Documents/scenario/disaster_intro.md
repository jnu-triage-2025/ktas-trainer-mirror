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
| D001 | Dialogue | [시스템] 원내 방송이 “병원 인근 지하철역에서 폭발 사고 발생. 재난 상황 발령되었습니다. 응급실 대비 바랍니다.”라고 안내한다. 화면 텍스트로도 동일한 메시지를 표시한다. | C001 |
| C001 | Choice | [역할을 선택한다. 플레이어 A/B/C/D 중 하나를 선택하도록 안내하고 UI 창을 출력한다.] | C001-OPT1, C001-OPT2, C001-OPT3, C001-OPT4 |
| C001-OPT1 | ChoiceOption | “플레이어 A: KTAS 분류 담당. 중증도 분류 구역으로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E001 |
| C001-OPT2 | ChoiceOption | “플레이어 B: 환자 처치. 처치 구역으로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E002 |
| C001-OPT3 | ChoiceOption | “플레이어 C: 환자 처치. 처치 구역으로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E003 |
| C001-OPT4 | ChoiceOption | “플레이어 D: 약물/수액 전담. 준비실로 이동하세요.” / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E004 |
| E001 | InvokeEvent | EventIdentifier로 select_role_nurse_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E002 | InvokeEvent | EventIdentifier로 select_role_nurse_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E003 | InvokeEvent | EventIdentifier로 select_role_nurse_c를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| E004 | InvokeEvent | EventIdentifier로 select_role_nurse_d를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| D002 | Dialogue | 플레이어들이 재난 상황을 공유하고 초기 대응을 시작한다. | P001 |
| P001 | Parallel | 플레이어 A는 중증도 분류 구역에서 대기하고 중증도 분류를 실시한다. 플레이어 B/C는 처치 구역에서 물품 준비를 실시한다. 플레이어 D는 준비실에서 수액/약물 준비를 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D006 |
| P001-B1 | ParallelBranch | 브랜치 시작 노드는 D003이며, 플레이어 A가 중증도 분류 구역에 대기하고 중증도 분류를 실시한다. CompletionConditionIdentifier는 CC_A_Triage로 서술한다. | D003 |
| P001-B2 | ParallelBranch | 브랜치 시작 노드는 D004이며, 플레이어 B/C가 처치 물품을 준비한다. CompletionConditionIdentifier는 CC_BC_READY로 서술한다. | D004 |
| P001-B3 | ParallelBranch | 브랜치 시작 노드는 D005이며, 플레이어 D가 수액/약물 준비를 완료한다. CompletionConditionIdentifier는 CC_D_READY로 서술한다. | D005 |
| D003 | Dialogue | [플레이어 A 전용] "중증도 분류 구역으로 이동하세요." | E005 |
| E005 | InvokeEvent | 플레이어 A는 중증도 분류 구역으로 이동한다. EventIdentifier로 nurse_a_to_triage를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D003_1 |
| D003_1 | Dialogue | [A 전용] "카트 위 활력징후 측정도구를 획득하세요." | V001 |
| V001 | Validator | [플레이어 A 전용] 중증도 분류 구역에 있는 카트 위의 활력징후 측정도구를 클릭해 획득한다. Condition은 Click_vitalset이며, TargetCount는 1이다. | D003_2 |
| D003_2 | Dialogue | [시스템] "잠시 후 환자가 이송됩니다. 중증도 분류 후 환자 처치가 시작됩니다. 각자의 역할에 대비하세요. | E006 |
| E006 | InvokeEvent | 시나리오 A환자, 더미 A 환자가 [스트레쳐 혹은 베드](스트레쳐에서 베드로 옮기는 과정이 구현 가능하다면 스트레쳐로 들어와서 베드로 옮겨도 좋고, 제한된다면 침대로 들어와서 정해진 위치에 위치시키는 방안으로 대체한다)에 실려 들어온다. EventIdentifier로 triage_patientA_dummyA를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D003_3 |
| D003_3 | Dialogue | [시스템] "환자 두 명이 이송되었습니다. 간호사 A가 중증도 분류를 시행합니다." | D003_4 |
| D003_4 | Dialogue | [플레이어 A 전용] "환자를 차례대로 클릭하여 환자의 상태를 확인하고, 중증도 분류를 실시하세요." | V002 |
| V002 | Validator | [A - 1단계] 시나리오 A 환자를 클릭해 환자의 상태를 확인한다. 시나리오 A 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_patientA_info이며, TargetCount는 1이다. | E007 |
| E007 | InvokeEvent | 시나리오 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화가 불가능하고, 신음소리만을 내고 있음, - 흉부 관통상 및 흉부에서 다량의 출혈 관찰됨, - 빈맥, - 불규칙한 서호흡, - 피부는 창백하고 차가움, - C/C: 다량의 출혈"으로 출력한다. EventIdentifier로 show_patientA_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C002 |
| C002 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C002-Wrong, C002-Correct |
| C002-Wrong | ChoiceOption | "KTAS 2(긴급)", "KTAS 3(응급)", "KTAS 4(준응급)", "KTAS 5(비응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D003_retry_a |
| C002-Correct | ChoiceOption | "KTAS 1(소생)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D003_5 |
| D003_retry_a | Dialogue | "오답입니다. 현재 흉부의 외상 및 다량의 출혈, 환자의 전반적인 외견을 고려하였을 때, KTAS 1(소생)이 적절합니다." | C002 |
| D003_5 | Dialogue | "해당 환자를 KTAS 1으로 분류했습니다. 다음 환자를 클릭하세요." | V003 |
| V003 | Validator | [A - 2단계] 더미 A 환자를 클릭해 환자의 상태를 확인한다. 더미 A 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_dummyA_info이며, TargetCount는 1이다. | E008 |
| E008 | InvokeEvent | 더미 A 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 원활한 대화 가능함, - 활력징후 정상, - 사지의 약간의 타박상, - C/C: 하지 통증"으로 출력한다. EventIdentifier로 show_dummyA_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C003 |
| C003 | Choice | "해당 환자의 중증도 분류를 시행하세요." | C003-Wrong, C003-Correct |
| C003-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 2(긴급)", "KTAS 3(응급)", "KTAS 4(준응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D003_retry_b |
| C003-Correct | ChoiceOption | "KTAS 5(비응급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D003_6 |
| D003_retry_b | Dialogue | "오답입니다. 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다." | C003 |
| D003_6 | Dialogue | "해당 환자를 KTAS 5로 분류했습니다." | D003_7 |
| D003_7 | Dialogue | "이제 입원 구역으로 이송할 긴급 환자를 클릭하세요." | V004 |
| V004 | Validator | [A - 4단계] 시나리오 A 환자를 클릭하여 선정한다. Condition은 Move_patientA이며, TargetCount는 1이다. | CC_A_Triage |
| D004 | Dialogue | [플레이어 B/C] "KTAS 1(소생) 환자에 대비하기 위해 처치실 내 물품의 위치를 파악하세요." | E009 |
| E009 | InvokeEvent | 플레이어 B와 C는 처치실로 이동한다. EventIdentifier로 B_C_to_treatment를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D004_1 |
| D004_1 | Dialogue | [플레이어 B/C] "처치실 물품의 위치를 확인하세요. 각 카트 및 테이블 앞으로 이동해 물품을 마우스로 가리키면 아이템을 확인할 수 있습니다." | V005 |
| V005 | Validator | [플레이어 B/C 전용] 처치실에 위치한 각 카트 앞을 waypoint로 지정하고, 각 카트 앞을 지나며 아이템을 확인할 수 있도록 한다. 각 카트 앞에 위치할 때, 아이템의 정보를 확인할 수 있도록 마우스 커서를 가져다 대면 아이템의 정보가 출력될 수 있도록 한다. Condition은 Check_treatment_equipments이며, TargetCount는 9이다. | CC_BC_READY |
| D005 | Dialogue | [플레이어 D 전용] "준비실로 이동하세요." | E010 |
| E010 | InvokeEvent | 플레이어 D는 물품 준비실로 이동한다. EventIdentifier로 D_to_preproom을 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D005_1 |
| D005_1 | Dialogue | "생리식염수 1L 수액백과 수액세트를 각각 클릭해 획득하면 서로 연결되며 준비됩니다." | V006 |
| V006 | Validator | 생리식염수 1L 수액백과 수액세트를 각각 클릭해 획득하면 인벤토리 창 내에서 '준비된 생리식염수 1L 수액백'으로 이름이 변환된다. Condition은 Click_ns1, Click_iv_set이며, TargetCount는 2이다. | D005_2 |
| D005_2 | Dialogue | "동일한 방법으로 플라즈마 솔루션 1L 수액백과 수액세트를 클릭해 획득하세요." | V007 |
| V007 | Validator | 플라즈마 솔루션 1L 수액백과 수액세트를 각각 클릭해 획득하면 인벤토리 창 내에서 '준비된 플라즈마 솔루션 1L 수액백'으로 이름이 변환된다. Condition은 Click_ps1, Click_iv_set이며, TargetCount는 2이다. | D005_3 |
| D005_3 | Dialogue | "혈액백을 클릭해 획득하세요." | V008 |
| V008 | Validator | 혈액백을 클릭해 획득한다. Condition은 Click_blood이며, TargetCount는 1이다. | CC_D_READY |
| D006 | Dialogue | [시스템, by 플레이어 A] "KTAS 1(소생)으로 분류된 환자를 이송하겠습니다. 간호사 B, C, D선생님, 해당 환자 처치실로 이동하도록 도와주세요." | E011 |
| E011 | InvokeEvent | 플레이어 B/C/D가 중증도 분류 구역으로 이동한다. EventIdentifier로 B_C_D_to_triage를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D007 |
| D007 | Dialogue | "초기 대응과 중증도 분류를 완료했습니다. 다음 처치 시나리오를 진행합니다." | (end) |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D007 |
| 종료 연출/설명 | "초기 대응과 중증도 분류를 완료했습니다. 다음 처치 시나리오를 진행합니다."라는 안내 메세지를 출력하고 다음 시나리오로 진행한다. |
