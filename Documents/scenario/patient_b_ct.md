# scenario 환자 B/C 지연 처치

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 환자 B/C: 뇌손상 의심 및 상완 개방성 골절 대응 |
| 요약 | 환자를 처치 구역으로 이동시키고 의식/활력징후 사정, 산소화, 지혈, IV 확보, 동공 반응 확인 후 CT실로 이동한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 B, 환자 C(동일 부상) |
| 주요 장소 | 처치 구역, CT실 |
| 리소스 식별자 - 사운드 | 없음 |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_treatment_area, wp_ct_room |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | D001 |

## 시나리오 본문

[""로 정의된 내용은 각각의 특정 플레이어 혹은 모든 플레이어 및 감독자(관전자) 모두 확인할 수 있도록 텍스트 및 음성으로 출력한다. 음성이 불가피한 경우 텍스트가 모니터에 출력되었으니 확인하라는 알람음을 추가하도록 한다.]
| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| E001 | InvokeEvent | 시나리오 B 환자, 시나리오 C 환자, 더미 B 환자가 [스트레쳐 혹은 베드](스트레쳐에서 베드로 옮기는 과정이 구현 가능하다면 스트레쳐로 들어와서 베드로 옮겨도 좋고, 제한된다면 침대로 들어와서 정해진 위치에 위치시키는 방안으로 대체한다)에 실려 들어온다. EventIdentifier로 triage_B_C_dummy_B를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다.| D001 |
| D001 | Dialogue | [시스템] "환자가 세 명이 이송되었습니다. 간호사 A가 중증도 분류를 시행합니다." | D002 |
| D002 | Dialogue | [플레이어 A 전용] "환자를 차례대로 클릭하여 환자의 상태를 확인하고, 중증도 분류를 실시하세요." | V001 |
| V001 | Validator | [A - 1단계] 시나리오 B 환자를 클릭해 환자의 상태를 확인한다. 시나리오 A 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_patient_B_info이며, TargetCount는 1이다. | E002 |
| E002 | InvokeEvent | 시나리오 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임, - [왼쪽 팔과 다리의 근력이 비교적 약함], - 빈맥, - 빈호흡, - 상완 부위 출혈 지속 중, - 머리에 타박상 및 약간의 출혈 보임, - C/C: 두통"으로 출력한다. EventIdentifier로 show_patient_b_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C001 |
| C001 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C001-Wrong, C001-Correct |
| C001-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 3(응급)", "KTAS 4(준응급)", "KTAS 5(비응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D003 |
| C001-Correct | ChoiceOption | "KTAS 2(긴급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D004 |
| D003 | Dialogue | "오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다." | C001 |
| D004 | Dialogue | "해당 환자를 KTAS 2로 분류했습니다. 다음 환자를 클릭하세요." | V002 |
| V002 | Validator | [A - 2단계] 가운데에 위치한 더미 B 환자를 클릭해 환자의 상태를 확인한다. 더미 B 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_dummy_b_info이며, TargetCount는 1이다. | E003 |
| E003 | InvokeEvent | 더미 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 원활한 대화 가능함, - 활력징후 정상, - 사지의 약간의 타박상, - C/C: 어깨 통증"으로 출력한다. EventIdentifier로 show_dummy_B_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C003 |
| C002 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C002-Wrong, C002-Correct |
| C002-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 2(긴급)", "KTAS 3(응급)", "KTAS 4(준응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D005 |
| C002-Correct | ChoiceOption | "KTAS 5(비응급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D006 |
| D005 | Dialogue | "오답입니다. 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다." | C002 |
| D006 | Dialogue | "해당 환자를 KTAS 5로 분류했습니다. 다음 환자를 클릭하세요." | V003 |
| V003 | Validator | [A - 3단계] 마지막 시나리오 C 환자를 클릭해 환자의 상태를 확인한다. 시나리오 C 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_patient_C_info이며, TargetCount는 1이다. | E004 |
| E004 | InvokeEvent | 시나리오 C 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임, - 한쪽 팔 근력이 비교적 약함, - 빈맥, - 빈호흡, - 무릎 하단 부위 출혈 지속 중, - 머리에 타박상 및 약간의 출혈 보임, - C/C: 어지러움"으로 출력한다. EventIdentifier로 show_patient_c_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C003 |
| C003 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C001-Wrong, C001-Correct |
| C003-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 3(응급)", "KTAS 4(준응급)", "KTAS 5(비응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D007 |
| C003-Correct | ChoiceOption | "KTAS 2(긴급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D008 |
| D007 | Dialogue | "오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다." | C003 |
| D008 | Dialogue | "해당 환자를 KTAS 2로 분류했습니다." | D009 |
| D009 | Dialogue | "이제 입원 구역으로 이송할 긴급 환자 2명을 차례대로 클릭하세요." | V004 |
| V004 | Validator | [A - 4단계] 시나리오 B 환자와 시나리오 C환자를 각각 클릭하여 선정한다. Condition은 Patient_B, Patient_C이며, TargetCount는 2이다. | E005 |
| D010 | Dialogue | [시스템, by 플레이어 A] "KTAS 2(긴급)으로 분류된 환자 2명을 이송하겠습니다. 간호사 B, C, D선생님 이동 도와주세요." | E006 |
| E006 | InvokeEvent | 플레이어 A/C는 시나리오 B 환자를, 플레이어 B/D는 시나리오 C 환자를 입원 구역으로 이동시킨다. EventIdentifier로 move_patient_B_and_C를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | P001 |
| P001 | Parallel | [중요, 처치 동시 시작] 플레이어 A와 C는 시나리오 B 환자의 침대를 잡고 이동하고, 플레이어 B와 D가 시나리오 C 환자를 담당하여 모든 처치를 완료한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D027 |
| P001-B1 | ParallelBranch | 브랜치 시작 노드는 V005_A이며, 플레이어 A와 C가 시나리오 B 환자의 처치를 담당하고, 최종 과정으로 CT실로 이동시킨다. CompletionConditionIdentifier는 CC_A_C_patientB_complete로 서술한다. | V005_A |
| V005_A | Validator | [플레이어 A] 시나리오 B 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientB, TargetCount는 1이다. | V005_C |
| V005_C | Validator | [플레이어 C] 시나리오 B 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientB, TargetCount는 1이다. | E007 |
| E007 | InvokeEvent | 시나리오 B 환자를 입원실 내 정해진 위치로 이동시킨다. EventIdentifier로 move_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D011 |
| D011 | Dialogue | [시스템] "처치 구역에 도착했습니다. 즉시 의식상태 사정 및 활력징후 사정을 시작하세요." | P002 |
| P002 | Parallel | [플레이어 A, C 전용] 플레이어 A는 환자를 클릭해 의식상태를 사정하고, 플레이어 C는 활력징후 사정도구를 사용해 활력징후를 사정한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D014 |
| P002-B1 | ParallelBranch | [플레이어 A] 브랜치 시작 노드는 D012이며, 플레이어 A가 시나리오 B 환자의 의식상태를 사정한다. CompletionConditionIdentifier는 CC_A_gcs_patientB로 서술한다. | D012 |
| P002-B2 | ParallelBranch | [플레이어 C] 브랜치 시작 노드는 D013_vital_1이며, 플레이어 C가 시나리오 B 환자의 활력징후를 사정한다. CompletionConditionIdentifier는 CC_C_vital_patientB로 서술한다. | D013_vital_1 |
| D012 | Dialogue | [플레이어 A 전용] "환자를 클릭하여 환자의 의식상태를 사정하세요." | V006 |
| V006 | Validator | 플레이어 A는 시나리오 B 환자를 클릭해 의식 수준 및 GCS를 사정한다. Condition은 Check_gcs_patientB이며, TargetCount는 1이다. | D012_1 |
| D012_1 | Dialogue | "환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다." | D012_avpu |
| D012_avpu | Dialogue | "환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다." | C004_avpu |
| C004_avpu | Choice | "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?" | C004_avpu-Wrong, C004_avpu-Correct |
| C004_avpu-Wrong | ChoiceOption | "A(Alert, 완전히 깨어 있음)", "P(Pain response, 통증에 반응 있음)", "U(Unconsciousness, 반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_avpu_retry |
| C004_avpu-Correct | ChoiceOption | "V(Verbal response, 음성에 반응 있음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_gcs_1 |
| D012_avpu_retry | Dialogue | "오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다." | C004_avpu |
| D012_gcs_1 | Dialogue | [관찰1] "추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C004_E |
| C004_E | Choice | "관찰된 E(Eye Opening) 점수는 몇 점입니까?" | C004_E-Wrong, C004_E-Correct |
| C004_E-Wrong | ChoiceOption | "4점(자발적)", "2점(통증)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_E_retry |
| C004_E-Correct | ChoiceOption | "3점(명령)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_gcs_2 |
| D012_E_retry | Dialogue | "오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C004_E |
| D012_gcs_2 | Dialogue | [관찰2] "다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. | C004_V |
| C004_V | Choice | "관찰된 V(Verbal Response) 점수는 몇 점입니까?" | C004_V-Wrong, C004_V-Correct |
| C004_V-Wrong | ChoiceOption | "5점(적절한 답변)", "3점(부적절한 답변)", "2점(신음소리)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_V_retry |
| C004_V-Correct | ChoiceOption | "4점(혼란)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_gcs_3 |
| D012_V_retry | Dialogue | "오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다." | C004_V |
| D012_gcs_3 | [관찰3] "마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다." | C004_M |
| C004_M | Choice | "관찰된 M(Motor Response) 점수는 몇 점입니까?" | C004_M-Wrong, C004_M-Correct |
| C004_M-Wrong | ChoiceOption | "5점(통증 원인을 치우려고 손을 뻗음)", "4점(통증에 회피)", "3점(이상 굴곡)", "2점(이상 신전)", "1(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_M_retry |
| C004_M-Correct | ChoiceOption | "6점(명령 수행)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_gcs_4 |
| D012_M_retry | Dialogue | "오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다." | C004_M |
| D012_gcs_4 | Dialogue | "GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다." | D012_motor_1 |
| D012_motor_1 | Dialogue | "GCS의 M(Motor Response) 사정 중 왼쪽 다리가 오른쪽 다리의 정상 근력보다 약하고, 저항에 이기지 못하고 있습니다." | C004_motor |
| C004_motor | Choice | "정상인 우측(5점)에 비해, 좌측의 근력 수준은?" | C004_motor-Wrong, C004_motor-Correct |
| C004_motor-Wrong | ChoiceOption | "5점(정상 근력)", "4점(중력+약간의 저항)", "2점(중력에 저항 불가, 좌우 운동)", "1점(약간의 근육 수축)", "0점(움직임 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_motor_retry |
| C004_motor-Correct | ChoiceOption | "3점(중력에 저항 가능)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D012_result |
| D012_motor_retry | Dialogue | "오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다." | C004_motor |
| D012_result | [플레이어 A 전용] "현재 시나리오 B 환자의 GCS는 13점, 근력(Motor Grade)은 우측 5점, 좌측 3점입니다." | CC_A_gcs_patientB |
| D013_vital_1 | Dialogue | [플레이어 C 전용] "환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요." | V007_1 |
| V007_1 | Validator | [C 1단계 - 시작] 플레이어 C는 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득한다. Condition은 Click_vital_set, Click_electrode, Click_electrode_cable이며, TargetCount는 3이다. | D013_vital_2 |
| D013_vital_2 | Dialogue | "전극을 선택하여 환자의 가슴에 부착하십시오." | V007_2 |
| V007_2 | Validator | [C 2단계] 인벤토리에서 전극을 선택한 뒤 환자(환자의 흉부)를 클릭한다. Condition은 apply_electrode이며, TargetCount는 1이다. | D013_vital_3 |
| D013_vital_3 | Dialogue | "전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요." | V007_3 |
| V007_3 | Validator | [C 3단계] 인벤토리에서 전극 케이블을 선택한 뒤 환자와 모니터를 각각 클릭한다. Condition은 connect_patient_and_monitor_b이며, TargetCount는 1이다. | D013_vital_4 |
| D013_vital_4 | Dialogue | "활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다." | V007_4 |
| V007_4 | Validator | [C 4단계] 인벤토리에서 활력징후 측정도구를 클릭해 선택한 뒤 환자를 클릭하면 활력징후가 출력된다. Condition은 check_vital_b이며, TargetCount는 1이다. | E008 |
| E008 | InvokeEvent | [플레이어 C - 활력징후 UI 창 출력] UI로 플레이어 B에게 활력징후를 보여줌과 동시에 활력징후 모니터에 활력징후가 출력된다. EventIdentifier로 activate_vital_monitor_ui_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D013_vital_5 |
| D013_vital_5 | Dialogue | [C 5단계 - 결과 확인] "혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오." | V007_5 |
| V007_5 | Validator | [C 6단계 - 종료] 활력징후 UI 창의 닫기(X) 버튼을 클릭한다. Conditiondms Close_vitalUI_b, TargetCount는 1이다. | CC_C_vital_patientB |
| D014 | Dialogue | [시스템(플레이어 B/D 제외)] "C 환자의 의식상태는 GCS 13점, 근력(Motor Grade) 우측 5점/좌측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. | D015 |
| D015 | Dialogue | [시스템(플레이어 B/D 제외), by 의사 NPC] "간호사 A 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 C 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요." | P003 |
| P003 | Parallel | [플레이어 A, C 전용] 플레이어 A는 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. 플레이어 C는 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D018 |
| P003-B1 | ParallelBranch | [플레이어 A] 브랜치 시작 노드는 D016이며, 플레이어 A가 시나리오 B 환자에게 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. CompletionConditionIdentifier는 CC_A_pupil_iv_patientB로 서술한다. | D016 |
| P003-B2 | ParallelBranch | [플레이어 C] 브랜치 시작 노드는 D017이며, 플레이어 C가 시나리오 B 환자에게 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. CompletionConditionIdentifier는 CC_C_nasal_pressure_patientB로 서술한다. | D017 |
| D016 | Dialogue | [플레이어 A 전용] "먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요." | V008_1 |
| V008_1 | Validator | [A 1단계 - 시작] 펜라이트를 클릭해 획득한다. Condition은 Click_penlight이며, Targetcount는 1이다. | D016_1 |
| D016_1 | Dialogue | "펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다." | V008_2 |
| V008_2 | Validator | [A 2단계] 환자의 얼굴을 클릭하면 환자의 양쪽 눈이 확대된다. 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. Condition은 Click_patientB_face이며, TargetCount는 1이다. | E009 |
| E009 | InvokeEvent | 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. [좌측 동공은 빛에 따라 동공이 수축하지만, 우측 동공은 거의 수축하지 않는 것으로 한다.] 양쪽을 최소 1회씩 확인해야 목표를 달성한 것으로 한다. EventIdentifier로 Pupil_reflex_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D016_2 |
| D016_2 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 A] "우측 동공이 빛에 반응하지 않습니다. 뇌출혈이 의심됩니다. 추가 검사가 필요해 보입니다." | D016_3 |
| D016_3 | Dialogue | [플레이어 A 전용] "다음으로 IV 라인을 확보합니다. 환자의 우측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오." | V008_3 |
| V008_3 | Validator | [A 1단계 - 시작] 플레이어 A는 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득한다. Condition은 Click_20g, Click_ivset, Click_ns1이며, TargetCount는 3이다. | D016_4 |
| D016_4 | Dialogue | "20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭해 정맥 라인을 확보하세요." | V008_4 |
| V008_4 | Validator | [A 2단계] 플레이어 D는 인벤토리 내 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭하여 정맥 라인을 확보한다. 캐뉼라는 팔의 오금(팔을 굽혔을 때 굽혀지며 오목해지는 부분)에 적용한다. Condition은 Insert_iv_b_right이며, TargetCount는 1이다. | E010 |
| E010 | InvokeEvent | 20G 캐뉼라가 환자 우측 팔 오금(팔을 굽혔을때 굽혀지며 오목해지는 부분)에 위치한다. 끝에 얇고 뾰족한 부분은 팔 안으로 삽입되어 팔 위에 색깔이 있는 플라스틱 부분부터 노출되어 보인다. EventIdentifier로 insert_20g_right_patientB를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D016_5 |
| D016_5 | Dialogue | "준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요." | V008_5 |
| V008_5 | Validator | [A 3단계] 플레이어 D는 인벤토리 내 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 20G 캐뉼라를 클릭해 연결한다. Condition은 Connect_cannula_and_ns1_patientB이며, TargetCount는 1이다. | E011 |
| E011 | InvokeEvent | 준비된 생리식염수 1L 수액백이 수액걸대에 걸리고, 우측 팔에 삽입되어 있는 20G 캐뉼라와 줄로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_ns1_right_patientB를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D016_6 |
| D016_6 | Dialogue | [시스템, by 플레이어 A] "정맥로가 확보되었습니다." | D016_final |
| D016_final | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 A] "환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다." | CC_A_pupil_iv_patientB |
| D017 | Dialogue | [플레이어 C 전용] "비강캐뉼라를 이용한 산소화를 먼저 실시합니다." | D017_1 |
| D017_1 | Dialogue | [플레이어 C 전용] "산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오." | V009_1 |
| V009_1 | Validator | [C 1단계 - 시작] 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하면 [습윤병에 멸균증류수가 채워진 것으로 가정하고 멸균증류수는 사라지고, 습윤병의 아이템 이름만 변경된다. 준비된 아이템은 "준비된 습윤병"으로 출력한다.] Condition은 Click_humidifierbottle, Click_sdw이며, TargetCount는 2이다. | D017_2 |
| D017_2 | Dialogue | "유량계를 습득하여 산소 유량계를 완성하세요." | V009_2 |
| V009_2 | Validator | [C 2단계] 산소 유량계 (뚜껑)를 클릭해 획득하면 산소 유량계와 습윤병이 합쳐진 아이템으로 자동 변화한다. Condition은 Click_oxyflow이며, TargetCount는 1이다. | D017_3 |
| D017_3 | Dialogue | "완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오." | V009_3 |
| V009_3 | Validator | [C 3단계] 완성된 산소 유량계를 클릭해 선택한 뒤, 흡인기 옆 벽면을 클릭해 설치한다. Condition은 Connect_wall_component_2이며, TargetCount는 1이다. | D017_4 |
| D017_4 | Dialogue | "비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요." | V009_4 |
| V009_4 | Validator | [C 4단계] 비강캐뉼라를 클릭해 획득한다. 이후 벽에 설치된 산소 유량계와 환자를 각각 클릭해 연결한다. [비강캐뉼라는 환자의 코에 적용되고 머리 뒤에서 관이 연결되는 것으로 하고, 머리 뒤에서 코딩을 바탕으로 한 투명 관으로 산소 유량계와 단순 연결한다.] Condition은 Click_nasal, Connect_nasal_and_o2이며, TargetCount는 2이다. | D017_5 |
| D017_5 | Dialogue | [플레이어 C 전용] "산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다." | V009_5 |
| V009_5 | Validator | [C 5단계] 벽에 설치된 산소 유량계를 클릭한다. 클릭하는 경우 투여될 산소의 양을 결정할 수 있도록 UI 창을 출력한다. | C005 |
| C005 | Choice | "투여될 산소의 양을 조절합니다." 3L/5L/10L/15L 중 하나를 선택하도록 안내하고 UI 창을 출력한다. | C005-Wrong, C005-Correct |
| C005-Wrong | ChoiceOption | "5L", "10L", "15L" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D017_retry |
| C005-Correct | ChoiceOption |  "3L" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D017_6 |
| D017_retry | Dialogue | "오답입니다. 처방은 3L 입니다." | C005 |
| D017_6 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C (6단계 - 종료)] "산소 투여가 완료되었습니다." | D017_7 |
| D017_7 | Dialogue | [플레이어 C 전용] "지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오." | V009_6 |
| V009_6 | Validator | [C 1단계 - 시작] 플레이어 C는 멸균장갑과 거즈, 플라스터를 클릭해 획득한다. Condition은 Click_glove, Click_gauze, Click_plaster이며, TargetCount는 3이다. | D017_8 |
| D017_8 | Dialogue | "멸균장갑을 [우클릭]해 착용하십시오." | V009_7 |
| V009_7 | Validator | [C 2단계] 멸균장갑 아이템을 우클릭해 착용한다. Condition은 wear_glove이며, TargetCount는 1이다. | D017_9 |
| D017_9 | Dialogue | "거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오." | V009_8 |
| V009_8 | Validator | [C 3단계] 거즈 아이템을 클릭해 선택한 뒤 환자를 클릭해 적용한다. Condition은 Apply_gauze이며, TargetCount는 1이다. | E012 |
| E012 | InvokeEvent | 거즈가 환자 상처부위에 위치한다. 이 때 포장지 없이 흰 거즈가 상처 위에 덮인 모양이 된다. 가능하면 혈흔 이펙트가 거즈에 스며나와도 좋다. EventIdentifier로 apply_gauze_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D017_10 |
| D017_10 | Dialogue | "압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오." | V009_9 |
| V009_9 | Validator | [C 4단계 - 종료] 플라스터 아이템을 클릭해 선택한 뒤 환자에게 적용되어 있는 거즈를 클릭한다. Condition은 Apply_plaster_on_gauze이며, TargetCount는 1이다. | E013 |
| E013 | InvokeEvent | 환자에게 적용된 거즈의 상단과 하단을 플라스터(테이프)로 고정되어 있는 것으로 변화한다. EventIdentifier로 apply_plaster_on_gauze_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D017_11 |
| D017_11 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C (5단계 - 종료)] "지혈 중입니다." | D017_final |
| D017_final | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C] "산소 적용 및 지혈이 완료되었습니다." | CC_C_nasal_pressure_patientB |
| D018 | Dialogue | [시스템, 전체] "시나리오 B 환자에 대한 간호 중재가 완료되었습니다." | CC_A_C_patientB_complete |
| P001-B2 | ParallelBranch | 브랜치 시작 노드는 V005_B이며, 플레이어 B와 D가 시나리오 C 환자의 처치를 담당하고, 최종 과정으로 CT실로 이동시킨다. CompletionConditionIdentifier는 CC_B_D_patientC_complete로 서술한다. | V005_B |
| V005_B | Validator | [플레이어 B] 시나리오 C 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientC, TargetCount는 1이다. | V005_D |
| V005_D | Validator | [플레이어 D] 시나리오 C 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientC, TargetCount는 1이다. | E014 |
| E014 | InvokeEvent | 시나리오 C 환자를 입원실 내 정해진 위치로 이동시킨다. EventIdentifier로 move_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D019 |
| D019 | Dialogue | [시스템] "처치 구역에 도착했습니다. 즉시 의식상태 사정 및 활력징후 사정을 시작하세요." | P004 |
| P004 | Parallel | [플레이어 B, D 전용] 플레이어 B는 환자를 클릭해 의식상태를 사정하고, 플레이어 D는 활력징후 사정도구를 사용해 활력징후를 사정한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D022 |
| P004-B1 | ParallelBranch | [플레이어 B] 브랜치 시작 노드는 D020이며, 플레이어 B가 시나리오 C 환자의 의식상태를 사정한다. CompletionConditionIdentifier는 CC_B_gcs_patientC로 서술한다. | D020 |
| P004-B2 | ParallelBranch | [플레이어 D] 브랜치 시작 노드는 D021_vital_1이며, 플레이어 D가 시나리오 C 환자의 활력징후를 사정한다. CompletionConditionIdentifier는 CC_D_vital_patientC로 서술한다. | D021_vital_1 |
| D020 | Dialogue | [플레이어 B 전용] "환자를 클릭하여 환자의 의식상태를 사정하세요." | V010 |
| V010 | Validator | 플레이어 B는 시나리오 C 환자를 클릭해 의식수준 및 GCS를 사정한다. Condition은 Check_gcs_patientC이며, TargetCount는 1이다. | D020_1 |
| D020_1 | Dialogue | "환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다." | D020_avpu |
| D020_avpu | Dialogue | "환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다." | C006_avpu |
| C006_avpu | Choice | "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?" | C006_avpu-Wrong, C006_avpu-Correct |
| C006_avpu-Wrong | ChoiceOption | "A(Alert, 완전히 깨어 있음)", "P(Pain response, 통증에 반응 있음)", "U(Unconsciousness, 반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_avpu_retry |
| C006_avpu-Correct | ChoiceOption | "V(Verbal response, 음성에 반응 있음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_gcs_1 |
| D020_avpu_retry | Dialogue | "오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다." | C006_avpu |
| D020_gcs_1 | Dialogue | [관찰1] "추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C006_E |
| C006_E | Choice | "관찰된 E(Eye Opening) 점수는 몇 점입니까?" | C006_E-Wrong, C006_E-Correct |
| C006_E-Wrong | ChoiceOption | "4점(자발적)", "2점(통증)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_E_retry |
| C006_E-Correct | ChoiceOption | "3점(명령)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_gcs_2 |
| D020_E_retry | Dialogue | "오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C006_E |
| D020_gcs_2 | Dialogue | [관찰2] "다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. | C006_V |
| C006_V | Choice | "관찰된 V(Verbal Response) 점수는 몇 점입니까?" | C006_V-Wrong, C006_V-Correct |
| C006_V-Wrong | ChoiceOption | "5점(적절한 답변)", "3점(부적절한 답변)", "2점(신음소리)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_V_retry |
| C006_V-Correct | ChoiceOption | "4점(혼란)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_gcs_3 |
| D020_V_retry | Dialogue | "오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다." | C006_V |
| D020_gcs_3 | [관찰3] "마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다." | C006_M |
| C006_M | Choice | "관찰된 M(Motor Response) 점수는 몇 점입니까?" | C006_M-Wrong, C006_M-Correct |
| C006_M-Wrong | ChoiceOption | "5점(통증 원인을 치우려고 손을 뻗음)", "4점(통증에 회피)", "3점(이상 굴곡)", "2점(이상 신전)", "1(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_M_retry |
| C006_M-Correct | ChoiceOption | "6점(명령 수행)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_gcs_4 |
| D020_M_retry | Dialogue | "오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다." | C006_M |
| D020_gcs_4 | Dialogue | "GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다." | D020_motor_1 |
| D020_motor_1 | Dialogue | "GCS의 M(Motor Response) 사정 중 오른쪽 다리가 왼쪽 다리의 정상 근력보다 약하고, 간호사가 가하는 저항에 이기지 못하고 있습니다." | C006_motor |
| C006_motor | Choice | "정상인 좌측(5점)에 비해, 우측의 근력 수준은?" | C006_motor-Wrong, C006_motor-Correct |
| C006_motor-Wrong | ChoiceOption | "5점(정상 근력)", "4점(중력+약간의 저항)", "2점(중력에 저항 불가, 좌우 운동)", "1점(약간의 근육 수축)", "0점(움직임 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_motor_retry |
| C006_motor-Correct | ChoiceOption | "3점(중력에 저항 가능)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D020_result |
| D020_motor_retry | Dialogue | "오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다." | C006_motor |
| D020_result | [플레이어 B 전용] "현재 시나리오 B 환자의 GCS는 13점, 근력(Motor Grade)은 좌측 5점, 우측 3점입니다." | CC_B_gcs_patientC |
| D021_vital_1 | Dialogue | [플레이어 D 전용] "환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요." | V011_1 |
| V011_1 | Validator | [D 1단계 - 시작] 플레이어 D는 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득한다. Condition은 Click_vital_set, Click_electrode, Click_electrode_cable이며, TargetCount는 3이다. | D021_vital_2 |
| D021_vital_2 | Dialogue | "전극을 선택하여 환자의 가슴에 부착하십시오." | V011_2 |
| V011_2 | Validator | [D 2단계] 인벤토리에서 전극을 선택한 뒤 환자(환자의 흉부)를 클릭한다. Condition은 apply_electrode이며, TargetCount는 1이다. | D021_vital_3 |
| D021_vital_3 | Dialogue | "전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요." | V011_3 |
| V011_3 | Validator | [D 3단계] 인벤토리에서 전극 케이블을 선택한 뒤 환자와 모니터를 각각 클릭한다. Condition은 connect_patient_and_monitor_patientC이며, TargetCount는 1이다. | D021_vital_4 |
| D021_vital_4 | Dialogue | "활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다." | V011_4 |
| V011_4 | Validator | [D 4단계] 인벤토리에서 활력징후 측정도구를 클릭해 선택한 뒤 환자를 클릭하면 활력징후가 출력된다. Condition은 check_vital_b이며, TargetCount는 1이다. | E015 |
| E015 | InvokeEvent | [플레이어 D - 활력징후 UI 창 출력] UI로 플레이어 D에게 활력징후를 보여줌과 동시에 활력징후 모니터에 활력징후가 출력된다. EventIdentifier로 activate_vital_monitor_ui_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D021_vital_5 |
| D021_vital_5 | Dialogue | [D 5단계 - 결과 확인] "혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오." | V011_5 |
| V011_5 | Validator | [D 6단계 - 종료] 활력징후 UI 창의 닫기(X) 버튼을 클릭한다. Conditiondms Close_vitalUI_b, TargetCount는 1이다. | CC_D_vital_patientC |
| D022 | Dialogue | [시스템(플레이어 A/C 제외)] "C 환자의 의식상태는 GCS 13점, 근력(Motor Grade) 좌측 5점/우측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. | D023 |
| D023 | Dialogue | [시스템(플레이어 A/C 제외), by 의사 NPC] "간호사 B 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 D 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요." | P005 |
| P005 | Parallel | [플레이어 B, D 전용] 플레이어 B는 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. 플레이어 D는 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D026 |
| P005-B1 | ParallelBranch | [플레이어 B] 브랜치 시작 노드는 D024이며, 플레이어 B가 시나리오 C 환자에게 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. CompletionConditionIdentifier는 CC_B_pupil_iv_patientC로 서술한다. | D024 |
| P005-B2 | ParallelBranch | [플레이어 D] 브랜치 시작 노드는 D025이며, 플레이어 D가 시나리오 C 환자에게 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. CompletionConditionIdentifier는 CC_D_nasal_pressure_patientC로 서술한다. | D025 |
| D024 | Dialogue | [플레이어 B 전용] "먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요." | V012_1 |
| V012_1 | Validator | [B 1단계 - 시작] 펜라이트를 클릭해 획득한다. Condition은 Click_penlight이며, Targetcount는 1이다. | D024_1 |
| D024_1 | Dialogue | "펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다." | V012_2 |
| V012_2 | Validator | [B 2단계] 환자의 얼굴을 클릭하면 환자의 양쪽 눈이 확대된다. 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. Condition은 Click_patientC_face이며, TargetCount는 1이다. | E016 |
| E016 | InvokeEvent | 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. [우측 동공은 빛에 따라 동공이 수축하지만, 좌측 동공은 거의 수축하지 않는 것으로 한다.] 양쪽을 최소 1회씩 확인해야 목표를 달성한 것으로 한다. EventIdentifier로 Pupil_reflex_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D024_2 |
| D024_2 | Dialogue | [시스템(플레이어 A/C 제외), by 플레이어 B] "좌측 동공이 빛에 반응하지 않습니다. 뇌출혈이 의심됩니다. 추가 검사가 필요해 보입니다." | D024_3 |
| D024_3 | Dialogue | [플레이어 B 전용] "다음으로 IV 라인을 확보합니다. 환자의 좌측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오." | V012_3 |
| V012_3 | Validator | [B 1단계 - 시작] 플레이어 A는 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득한다. Condition은 Click_20g, Click_ivset, Click_ns1이며, TargetCount는 3이다. | D024_4 |
| D024_4 | Dialogue | "20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요." | V012_4 |
| V012_4 | Validator | [B 2단계] 플레이어 D는 인벤토리 내 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭하여 정맥 라인을 확보한다. 캐뉼라는 팔의 오금(팔을 굽혔을 때 굽혀지며 오목해지는 부분)에 적용한다. Condition은 Insert_iv_c_left이며, TargetCount는 1이다. | E017 |
| E017 | InvokeEvent | 20G 캐뉼라가 환자 좌측 팔 오금(팔을 굽혔을때 굽혀지며 오목해지는 부분)에 위치한다. 끝에 얇고 뾰족한 부분은 팔 안으로 삽입되어 팔 위에 색깔이 있는 플라스틱 부분부터 노출되어 보인다. EventIdentifier로 insert_20g_left_patientC를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D024_5 |
| D024_5 | Dialogue | "준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요." | V012_5 |
| V012_5 | Validator | [B 3단계] 플레이어 D는 인벤토리 내 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 20G 캐뉼라를 클릭해 연결한다. Condition은 Connect_cannula_and_ns1_patientC이며, TargetCount는 1이다. | E018 |
| E018 | InvokeEvent | 준비된 생리식염수 1L 수액백이 수액걸대에 걸리고, 우측 팔에 삽입되어 있는 20G 캐뉼라와 줄로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_ns1_right_patientC를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D024_6 |
| D024_6 | Dialogue | [시스템, by 플레이어 B] "정맥로가 확보되었습니다." | D024_final |
| D024_final | Dialogue | [시스템(플레이어 A/C 제외), by 플레이어 B] "환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다." | CC_B_pupil_iv_patientC |
| D025 | Dialogue | [플레이어 D 전용] "비강캐뉼라를 이용한 산소화를 먼저 실시합니다." | D025_1 |
| D025_1 | Dialogue | [플레이어 D 전용] "산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오." | V013_1 |
| V013_1 | Validator | [D 1단계 - 시작] 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하면 [습윤병에 멸균증류수가 채워진 것으로 가정하고 멸균증류수는 사라지고, 습윤병의 아이템 이름만 변경된다. 준비된 아이템은 "준비된 습윤병"으로 출력한다.] Condition은 Click_humidifierbottle, Click_sdw이며, TargetCount는 2이다. | D025_2 |
| D025_2 | Dialogue | "유량계를 습득하여 산소 유량계를 완성하세요." | V013_2 |
| V013_2 | Validator | [D 2단계] 산소 유량계 (뚜껑)를 클릭해 획득하면 산소 유량계와 습윤병이 합쳐진 아이템으로 자동 변화한다. Condition은 Click_oxyflow이며, TargetCount는 1이다. | D025_3 |
| D025_3 | Dialogue | "완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오." | V013_3 |
| V013_3 | Validator | [D 3단계] 완성된 산소 유량계를 클릭해 선택한 뒤, 흡인기 옆 벽면을 클릭해 설치한다. Condition은 Connect_wall_component_2이며, TargetCount는 1이다. | D025_4 |
| D025_4 | Dialogue | "비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요." | V013_4 |
| V013_4 | Validator | [D 4단계] 비강캐뉼라를 클릭해 획득한다. 이후 벽에 설치된 산소 유량계와 환자를 각각 클릭해 연결한다. [비강캐뉼라는 환자의 코에 적용되고 머리 뒤에서 관이 연결되는 것으로 하고, 머리 뒤에서 코딩을 바탕으로 한 투명 관으로 산소 유량계와 단순 연결한다.] Condition은 Click_nasal, Connect_nasal_and_o2이며, TargetCount는 2이다. | D025_5 |
| D025_5 | Dialogue | [플레이어 D 전용] "산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다." | V013_5 |
| V013_5 | Validator | [D 5단계] 벽에 설치된 산소 유량계를 클릭한다. 클릭하는 경우 투여될 산소의 양을 결정할 수 있도록 UI 창을 출력한다. | C007 |
| C007 | Choice | "투여될 산소의 양을 조절합니다." 3L/5L/10L/15L 중 하나를 선택하도록 안내하고 UI 창을 출력한다. | C007-Wrong, C007-Correct |
| C007-Wrong | ChoiceOption | "5L", "10L", "15L" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D025_retry |
| C007-Correct | ChoiceOption |  "3L" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D025_6 |
| D025_retry | Dialogue | "오답입니다. 처방은 3L 입니다." | C007 |
| D025_6 | Dialogue | [시스템(플레이어 A/C 제외), by 플레이어 D (6단계 - 종료)] "산소 투여가 완료되었습니다." | D025_7 |
| D025_7 | Dialogue | [플레이어 D 전용] "지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오." | V013_6 |
| V013_6 | Validator | [D 1단계 - 시작] 플레이어 D는 멸균장갑과 거즈, 플라스터를 클릭해 획득한다. Condition은 Click_glove, Click_gauze, Click_plaster이며, TargetCount는 3이다. | D025_8 |
| D025_8 | Dialogue | "멸균장갑을 [우클릭]해 착용하십시오." | V013_7 |
| V013_7 | Validator | [D 2단계] 멸균장갑 아이템을 우클릭해 착용한다. Condition은 wear_glove이며, TargetCount는 1이다. | D025_9 |
| D025_9 | Dialogue | "거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오." | V013_8 |
| V013_8 | Validator | [D 3단계] 거즈 아이템을 클릭해 선택한 뒤 환자를 클릭해 적용한다. Condition은 Apply_gauze이며, TargetCount는 1이다. | E018 |
| E018 | InvokeEvent | 거즈가 환자 상처부위에 위치한다. 이 때 포장지 없이 흰 거즈가 상처 위에 덮인 모양이 된다. 가능하면 혈흔 이펙트가 거즈에 스며나와도 좋다. EventIdentifier로 apply_gauze_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D025_10 |
| D025_10 | Dialogue | "압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오." | V013_9 |
| V013_9 | Validator | [D 4단계 - 종료] 플라스터 아이템을 클릭해 선택한 뒤 환자에게 적용되어 있는 거즈를 클릭한다. Condition은 Apply_plaster_on_gauze이며, TargetCount는 1이다. | E019 |
| E019 | InvokeEvent | 환자에게 적용된 거즈의 상단과 하단을 플라스터(테이프)로 고정되어 있는 것으로 변화한다. EventIdentifier로 apply_plaster_on_gauze_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D025_11 |
| D025_11 | Dialogue | [시스템, by 플레이어 D (5단계 - 종료)] "지혈 중입니다." | D025_final |
| D025_final | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C] "산소 적용 및 지혈이 완료되었습니다." | CC_D_nasal_pressure_patientC |
| D026 | Dialogue | [시스템, 전체] "시나리오 C 환자에 대한 간호 중재가 완료되었습니다." | CC_B_D_patientC_complete |
| D027 | Dialogue | [시스템, by 의사 NPC] "기전과 사정 결과를 보니 뇌손상이 의심됩니다. 활력징후는 비교적 안정되어 있으니 지금 Brain CT 찍겠습니다. 지금 환자를 CT실로 이동시켜주세요." | E_Move_to_CT |
| E_Move_to_CT | InvokeEvent | [모든 플레이어] 처치가 완료된 시나리오 B 환자와 C 환자를 CT실로 이송한다. EventIdentifier로 move_patients_to_CT를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D027 |
| D027 | Dialogue | [시스템] "시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다." 메세지를 표시한다. | (end) |


## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D027 |
| 종료 연출/설명 | 두 환자 모두 CT실 도달 시 종료된다. 검은 화면으로 fade out 되며 "시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다." 메세지를 표시하며 종료된다. |
