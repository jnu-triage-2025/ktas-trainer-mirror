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
| 시작 노드 Identifier | E070 |

## 시나리오 본문

[""로 정의된 내용은 각각의 특정 플레이어 혹은 모든 플레이어 및 감독자(관전자) 모두 확인할 수 있도록 텍스트 및 음성으로 출력한다. 음성이 불가피한 경우 텍스트가 모니터에 출력되었으니 확인하라는 알람음을 추가하도록 한다.]
| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| E070 | InvokeEvent | 시나리오 B 환자, 시나리오 C 환자, 더미 B 환자가 [스트레쳐 혹은 베드](스트레쳐에서 베드로 옮기는 과정이 구현 가능하다면 스트레쳐로 들어와서 베드로 옮겨도 좋고, 제한된다면 침대로 들어와서 정해진 위치에 위치시키는 방안으로 대체한다)에 실려 들어온다. EventIdentifier로 triage_patientB_patientC_dummyB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다.| D044 |
| D044 | Dialogue | [시스템] "환자 세 명이 이송되었습니다. 간호사 A가 중증도 분류를 시행합니다." | D045 |
| D045 | Dialogue | [플레이어 A 전용] "환자를 차례대로 클릭하여 환자의 상태를 확인하고, 중증도 분류를 실시하세요." | V029 |
| V029 | Validator | [A - 1단계] 시나리오 B 환자를 클릭해 환자의 상태를 확인한다. 시나리오 B 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_patientB_info이며, TargetCount는 1이다. | E071 |
| E071 | InvokeEvent | 시나리오 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임, - [왼쪽 팔과 다리의 근력이 비교적 약함], - 빈맥, - 빈호흡, - 상완 부위 출혈 지속 중, - 머리에 타박상 및 약간의 출혈 보임, - C/C: 두통"으로 출력한다. EventIdentifier로 show_patientB_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C035 |
| C035 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C035-Wrong, C035-Correct |
| C035-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 3(응급)", "KTAS 4(준응급)", "KTAS 5(비응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D045_retry_a |
| C035-Correct | ChoiceOption | "KTAS 2(긴급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D045_1 |
| D045_retry_a | Dialogue | "오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다." | C035 |
| D045_1 | Dialogue | "해당 환자를 KTAS 2로 분류했습니다. 다음 환자를 클릭하세요." | V029_1 |
| V029_1 | Validator | [A - 2단계] 가운데에 위치한 더미 B 환자를 클릭해 환자의 상태를 확인한다. 더미 B 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_dummyB_info이며, TargetCount는 1이다. | E072 |
| E072 | InvokeEvent | 더미 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 원활한 대화 가능함, - 활력징후 정상, - 사지의 약간의 타박상, - C/C: 어깨 통증"으로 출력한다. EventIdentifier로 show_dummyB_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C036 |
| C036 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C036-Wrong, C036-Correct |
| C036-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 2(긴급)", "KTAS 3(응급)", "KTAS 4(준응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D045_retry_b |
| C036-Correct | ChoiceOption | "KTAS 5(비응급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D045_2 |
| D045_retry_b | Dialogue | "오답입니다. 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다." | C036 |
| D045_2 | Dialogue | "해당 환자를 KTAS 5로 분류했습니다. 다음 환자를 클릭하세요." | V029_2 |
| V029_2 | Validator | [A - 3단계] 마지막 시나리오 C 환자를 클릭해 환자의 상태를 확인한다. 시나리오 C 환자를 클릭하면 환자의 정보가 UI로 출력되도록 한다. Condition은 Show_patientC_info이며, TargetCount는 1이다. | E073 |
| E073 | InvokeEvent | 시나리오 C 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임, - 한쪽 팔 근력이 비교적 약함, - 빈맥, - 빈호흡, - 무릎 하단 부위 출혈 지속 중, - 머리에 타박상 및 약간의 출혈 보임, - C/C: 어지러움"으로 출력한다. EventIdentifier로 show_patient_c_ui를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C037 |
| C037 | Choice | [플레이어 A 전용] "해당 환자의 중증도 분류를 시행하세요." | C037-Wrong, C037-Correct |
| C037-Wrong | ChoiceOption | "KTAS 1(소생)", "KTAS 3(응급)", "KTAS 4(준응급)", "KTAS 5(비응급)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D045_retry_c |
| C037-Correct | ChoiceOption | "KTAS 2(긴급)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D045_3 |
| D045_retry_c | Dialogue | "오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다." | C037 |
| D045_3 | Dialogue | "해당 환자를 KTAS 2로 분류했습니다." | D045_4 |
| D045_4 | Dialogue | "이제 입원 구역으로 이송할 긴급 환자 2명을 차례대로 클릭하세요." | V029_3 |
| V029_3 | Validator | [A - 4단계] 시나리오 B 환자와 시나리오 C환자를 각각 클릭하여 선정한다. Condition은 Move_patientB, Move_patientC이며, TargetCount는 2이다. | D045_5 |
| D045_5 | Dialogue | [시스템, by 플레이어 A] "KTAS 2(긴급)으로 분류된 환자 2명을 이송하겠습니다. 간호사 B, C, D선생님 이동 도와주세요." | E074 |
| E074 | InvokeEvent | 플레이어 A/C는 시나리오 B 환자를, 플레이어 B/D는 시나리오 C 환자를 입원 구역으로 이동시킨다. EventIdentifier로 move_patient_B_and_C를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | P009 |
| P009 | Parallel | [중요, 처치 동시 시작] 플레이어 A와 C는 시나리오 B 환자의 침대를 잡고 이동하고, 플레이어 B와 D가 시나리오 C 환자를 담당하여 모든 처치를 완료한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D062 |
| P009-B1 | ParallelBranch | 브랜치 시작 노드는 V030_A이며, 플레이어 A와 C가 시나리오 B 환자의 처치를 담당하고, 최종 과정으로 CT실로 이동시킨다. CompletionConditionIdentifier는 CC_A_C_patientB_complete로 서술한다. | V030_A |
| V030_A | Validator | [플레이어 A] 시나리오 B 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientB, TargetCount는 1이다. | V030_C |
| V030_C | Validator | [플레이어 C] 시나리오 B 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientB, TargetCount는 1이다. | E075 |
| E075 | InvokeEvent | 시나리오 B 환자를 입원실 내 정해진 위치로 이동시킨다. EventIdentifier로 move_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D046 |
| D046 | Dialogue | [시스템] "처치 구역에 도착했습니다. 간호사 A는 의식상태를, 간호사 C는 활력징후를 사정하세요." | P010 |
| P010 | Parallel | [플레이어 A, C 전용] 플레이어 A는 환자를 클릭해 의식상태를 사정하고, 플레이어 C는 활력징후 사정도구를 사용해 활력징후를 사정한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D049 |
| P010-B1 | ParallelBranch | [플레이어 A] 브랜치 시작 노드는 D047이며, 플레이어 A가 시나리오 B 환자의 의식상태를 사정한다. CompletionConditionIdentifier는 CC_A_gcs_patientB로 서술한다. | D047 |
| P010-B2 | ParallelBranch | [플레이어 C] 브랜치 시작 노드는 D048이며, 플레이어 C가 시나리오 B 환자의 활력징후를 사정한다. CompletionConditionIdentifier는 CC_C_vital_patientB로 서술한다. | D048 |
| D047 | Dialogue | [플레이어 A 전용] "환자를 클릭하여 환자의 의식상태를 사정하세요." | V031 |
| V031 | Validator | 플레이어 A는 시나리오 B 환자를 클릭해 의식 수준 및 GCS를 사정한다. Condition은 Check_gcs_patientB이며, TargetCount는 1이다. | D047_1 |
| D047_1 | Dialogue | "환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해주세요. 정답 시 계속 진행, 오답 시 재응시 합니다." | D047_2 |
| D047_2 | Dialogue | "환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다." | C038 |
| C038 | Choice | "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?" | C038-Wrong, C038-Correct |
| C038-Wrong | ChoiceOption | "A(Alert, 완전히 깨어 있음)", "P(Pain response, 통증에 반응 있음)", "U(Unconsciousness, 반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_retry_a |
| C038-Correct | ChoiceOption | "V(Verbal response, 음성에 반응 있음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_3 |
| D047_retry_a | Dialogue | "오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다." | C038 |
| D047_3 | Dialogue | [관찰1] "추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C039 |
| C039 | Choice | "관찰된 E(Eye Opening) 점수는 몇 점입니까?" | C039-Wrong, C039-Correct |
| C039-Wrong | ChoiceOption | "4점(자발적)", "2점(통증)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_retry_b |
| C039-Correct | ChoiceOption | "3점(명령)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_4 |
| D047_retry_b | Dialogue | "오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C039 |
| D047_4 | Dialogue | [관찰2] "다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. | C040 |
| C040 | Choice | "관찰된 V(Verbal Response) 점수는 몇 점입니까?" | C040-Wrong, C040-Correct |
| C040-Wrong | ChoiceOption | "5점(적절한 답변)", "3점(부적절한 답변)", "2점(신음소리)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_retry_c |
| C040-Correct | ChoiceOption | "4점(혼란)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_5 |
| D047_retry_c | Dialogue | "오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다." | C040 |
| D047_5 | Dialogue | [관찰3] "마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다." | C041 |
| C041 | Choice | "관찰된 M(Motor Response) 점수는 몇 점입니까?" | C041-Wrong, C041-Correct |
| C041-Wrong | ChoiceOption | "5점(통증 원인을 치우려고 손을 뻗음)", "4점(통증에 회피)", "3점(이상 굴곡)", "2점(이상 신전)", "1(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_retry_d |
| C041-Correct | ChoiceOption | "6점(명령 수행)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_6 |
| D047_retry_d | Dialogue | "오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다." | C004_M |
| D047_6 | Dialogue | "GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다." | D047_7 |
| D047_7 | Dialogue | "GCS의 M(Motor Response) 사정 중 왼쪽 다리가 오른쪽 다리의 정상 근력보다 약하고, 저항에 이기지 못하고 있습니다." | C042 |
| C042 | Choice | "정상인 우측(5점)에 비해, 좌측의 근력 수준은?" | C042-Wrong, C042-Correct |
| C042-Wrong | ChoiceOption | "5점(정상 근력)", "4점(중력+약간의 저항)", "2점(중력에 저항 불가, 좌우 운동)", "1점(약간의 근육 수축)", "0점(움직임 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_retry_e |
| C042-Correct | ChoiceOption | "3점(중력에 저항 가능)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D047_8 |
| D047_retry_e | Dialogue | "오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다." | C004_motor |
| D047_8 | Dialogue | [플레이어 A 전용] "현재 시나리오 B 환자의 GCS는 13점, 근력(Motor Grade)은 우측 5점, 좌측 3점입니다." | CC_A_gcs_patientB |
| D048 | Dialogue | [플레이어 C 전용] "환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요." | V035 |
| V032 | Validator | [C 1단계 - 시작] 플레이어 C는 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득한다. Condition은 Click_vital_set, Click_electrode, Click_electrode_cable이며, TargetCount는 3이다. | D048_1 |
| D048_1 | Dialogue | "전극을 선택하여 환자의 가슴에 부착하십시오." | V032_1 |
| V032_1 | Validator | [C 2단계] 인벤토리에서 전극을 선택한 뒤 환자(환자의 흉부)를 클릭한다. Condition은 apply_electrode이며, TargetCount는 1이다. | D048_2 |
| D048_2 | Dialogue | "전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요." | V032_2 |
| V032_2 | Validator | [C 3단계] 인벤토리에서 전극 케이블을 선택한 뒤 환자와 모니터를 각각 클릭한다. Condition은 connect_patient_and_monitor_b이며, TargetCount는 1이다. | D048_3 |
| D048_3 | Dialogue | "활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다." | V032_3 |
| V032_3 | Validator | [C 4단계] 인벤토리에서 활력징후 측정도구를 클릭해 선택한 뒤 환자를 클릭하면 활력징후가 출력된다. Condition은 check_vital_b이며, TargetCount는 1이다. | E076 |
| E076 | InvokeEvent | [플레이어 C - 활력징후 UI 창 출력] UI로 플레이어 B에게 활력징후를 보여줌과 동시에 활력징후 모니터에 활력징후가 출력된다. EventIdentifier로 activate_vital_monitor_ui_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D048_4 |
| D048_4 | Dialogue | [C 5단계 - 결과 확인] "혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오." | V032_4 |
| V032_4 | Validator | [C 6단계 - 종료] 활력징후 UI 창의 닫기(X) 버튼을 클릭한다. Conditiondms Close_vitalUI_b, TargetCount는 1이다. | CC_C_vital_patientB |
| D049 | Dialogue | [시스템(플레이어 B/D 제외)] "C 환자의 의식상태는 GCS 13점, 근력(Motor Grade) 우측 5점/좌측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. | D050 |
| D050 | Dialogue | [시스템(플레이어 B/D 제외), by 의사 NPC] "간호사 A 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 C 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요." | P011 |
| P011 | Parallel | [플레이어 A, C 전용] 플레이어 A는 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. 플레이어 C는 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D053 |
| P011-B1 | ParallelBranch | [플레이어 A] 브랜치 시작 노드는 D051이며, 플레이어 A가 시나리오 B 환자에게 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. CompletionConditionIdentifier는 CC_A_pupil_iv_patientB로 서술한다. | D051 |
| P011-B2 | ParallelBranch | [플레이어 C] 브랜치 시작 노드는 D052이며, 플레이어 C가 시나리오 B 환자에게 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. CompletionConditionIdentifier는 CC_C_nasal_pressure_patientB로 서술한다. | D052 |
| D051 | Dialogue | [플레이어 A 전용] "먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요." | V033 |
| V033 | Validator | [A 1단계 - 시작] 펜라이트를 클릭해 획득한다. Condition은 Click_penlight이며, Targetcount는 1이다. | D051_1 |
| D051_1 | Dialogue | "펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다." | V033_1 |
| V033_1 | Validator | [A 2단계] 환자의 얼굴을 클릭하면 환자의 양쪽 눈이 확대된다. 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. Condition은 Click_patientB_face이며, TargetCount는 1이다. | E077 |
| E077 | InvokeEvent | 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. [좌측 동공은 빛에 따라 동공이 수축하지만, 우측 동공은 거의 수축하지 않는 것으로 한다.] 양쪽을 최소 1회씩 확인해야 목표를 달성한 것으로 한다. EventIdentifier로 Pupil_reflex_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D051_2 |
| D051_2 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 A] "좌측 동공에 비해 우측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다." | D051_3 |
| D051_3 | Dialogue | [플레이어 A 전용] "다음으로 IV 라인을 확보합니다. 환자의 우측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오." | V033_2 |
| V033_2 | Validator | [A 1단계 - 시작] 플레이어 A는 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득한다. Condition은 Click_20g, Click_iv_set, Click_ns1이며, TargetCount는 3이다. | D051_4 |
| D051_4 | Dialogue | "20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭해 정맥 라인을 확보하세요." | V033_3 |
| V033_3 | Validator | [A 2단계] 플레이어 D는 인벤토리 내 20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭하여 정맥 라인을 확보한다. 캐뉼라는 팔의 오금(팔을 굽혔을 때 굽혀지며 오목해지는 부분)에 적용한다. Condition은 Insert_iv_b_right이며, TargetCount는 1이다. | E078 |
| E078 | InvokeEvent | 20G 캐뉼라가 환자 우측 팔 오금(팔을 굽혔을때 굽혀지며 오목해지는 부분)에 위치한다. 끝에 얇고 뾰족한 부분은 팔 안으로 삽입되어 팔 위에 색깔이 있는 플라스틱 부분부터 노출되어 보인다. EventIdentifier로 insert_20g_right_patientB를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D051_5 |
| D051_5 | Dialogue | "준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요." | V033_4 |
| V033_4 | Validator | [A 3단계] 플레이어 D는 인벤토리 내 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 20G 캐뉼라를 클릭해 연결한다. Condition은 Connect_cannula_and_ns1_patientB이며, TargetCount는 1이다. | E079 |
| E079 | InvokeEvent | 준비된 생리식염수 1L 수액백이 수액걸대에 걸리고, 우측 팔에 삽입되어 있는 20G 캐뉼라와 줄로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_ns1_right_patientB를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D051_6 |
| D051_6 | Dialogue | [시스템, by 플레이어 A] "정맥로가 확보되었습니다." | D051_7 |
| D051_7 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 A] "환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다." | CC_A_pupil_iv_patientB |
| D052 | Dialogue | [플레이어 C 전용] "비강캐뉼라를 이용한 산소화를 먼저 실시합니다." | D052_1 |
| D052_1 | Dialogue | [플레이어 C 전용] "산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오." | V034 |
| V034 | Validator | [C 1단계 - 시작] 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하면 [습윤병에 멸균증류수가 채워진 것으로 가정하고 멸균증류수는 사라지고, 습윤병의 아이템 이름만 변경된다. 준비된 아이템은 "준비된 습윤병"으로 출력한다.] Condition은 Click_humidifierbottle, Click_sdw이며, TargetCount는 2이다. | D052_2 |
| D052_2 | Dialogue | "유량계를 습득하여 산소 유량계를 완성하세요." | V034_1 |
| V034_1 | Validator | [C 2단계] 산소 유량계 (뚜껑)를 클릭해 획득하면 산소 유량계와 습윤병이 합쳐진 아이템으로 자동 변화한다. Condition은 Click_oxyflow이며, TargetCount는 1이다. | D052_3 |
| D052_3 | Dialogue | "완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오." | V034_2 |
| V034_2 | Validator | [C 3단계] 완성된 산소 유량계를 클릭해 선택한 뒤, 흡인기 옆 벽면을 클릭해 설치한다. Condition은 Connect_wall_component_2이며, TargetCount는 1이다. | D052_4 |
| D052_4 | Dialogue | "비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요." | V034_3 |
| V034_3 | Validator | [C 4단계] 비강캐뉼라를 클릭해 획득한다. 이후 벽에 설치된 산소 유량계와 환자를 각각 클릭해 연결한다. [비강캐뉼라는 환자의 코에 적용되고 머리 뒤에서 관이 연결되는 것으로 하고, 머리 뒤에서 코딩을 바탕으로 한 투명 관으로 산소 유량계와 단순 연결한다.] Condition은 Click_nasal, Connect_nasal_and_o2이며, TargetCount는 2이다. | D052_5 |
| D052_5 | Dialogue | [플레이어 C 전용] "산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다." | V034_4 |
| V034_4 | Validator | [C 5단계] 벽에 설치된 산소 유량계를 클릭한다. 클릭하는 경우 투여될 산소의 양을 결정할 수 있도록 UI 창을 출력한다. | C043 |
| C043 | Choice | "투여될 산소의 양을 조절합니다." 3L/5L/10L/15L 중 하나를 선택하도록 안내하고 UI 창을 출력한다. | C043-Wrong, C043-Correct |
| C043-Wrong | ChoiceOption | "5L", "10L", "15L" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D052_retry_a |
| C043-Correct | ChoiceOption |  "3L" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D052_6 |
| D052_retry_a | Dialogue | "오답입니다. 처방은 3L 입니다." | C043 |
| D052_6 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C (6단계 - 종료)] "산소 투여가 완료되었습니다." | D052_7 |
| D052_7 | Dialogue | [플레이어 C 전용] "지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오." | V034_5 |
| V034_5 | Validator | [C 1단계 - 시작] 플레이어 C는 멸균장갑과 거즈, 플라스터를 클릭해 획득한다. Condition은 Click_glove, Click_gauze, Click_plaster이며, TargetCount는 3이다. | D052_8 |
| D052_8 | Dialogue | "멸균장갑을 [우클릭]해 착용하십시오." | V034_6 |
| V034_6 | Validator | [C 2단계] 멸균장갑 아이템을 우클릭해 착용한다. Condition은 wear_glove이며, TargetCount는 1이다. | D052_9 |
| D052_9 | Dialogue | "거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오." | V034_7 |
| V034_7 | Validator | [C 3단계] 거즈 아이템을 클릭해 선택한 뒤 환자를 클릭해 적용한다. Condition은 Apply_gauze이며, TargetCount는 1이다. | E080 |
| E080 | InvokeEvent | 거즈가 환자 상처부위에 위치한다. 이 때 포장지 없이 흰 거즈가 상처 위에 덮인 모양이 된다. 가능하면 혈흔 이펙트가 거즈에 스며나와도 좋다. EventIdentifier로 apply_gauze_patientB를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D052_10 |
| D052_10 | Dialogue | "압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오." | V034_8 |
| V034_8 | Validator | [C 4단계 - 종료] 플라스터 아이템을 클릭해 선택한 뒤 환자에게 적용되어 있는 거즈를 클릭한다. Condition은 Apply_plaster_on_gauze이며, TargetCount는 1이다. | E081 |
| E081 | InvokeEvent | 환자에게 적용된 거즈의 상단과 하단을 플라스터(테이프)로 고정되어 있는 것으로 변화한다. EventIdentifier로 apply_plaster_on_gauze_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D052_11 |
| D052_11 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C (5단계 - 종료)] "지혈 중입니다." | D052_12 |
| D052_12 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C] "산소 적용 및 지혈이 완료되었습니다." | CC_C_nasal_pressure_patientB |
| D053 | Dialogue | [시스템, 전체] "시나리오 B 환자에 대한 간호 중재가 완료되었습니다." | CC_A_C_patientB_complete |
| P009-B2 | ParallelBranch | 브랜치 시작 노드는 V030_B이며, 플레이어 B와 D가 시나리오 C 환자의 처치를 담당하고, 최종 과정으로 CT실로 이동시킨다. CompletionConditionIdentifier는 CC_B_D_patientC_complete로 서술한다. | V030_B |
| V030_B | Validator | [플레이어 B] 시나리오 C 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientC, TargetCount는 1이다. | V030_D |
| V030_D | Validator | [플레이어 D] 시나리오 C 환자의 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher_patientC, TargetCount는 1이다. | E082 |
| E082 | InvokeEvent | 시나리오 C 환자를 입원실 내 정해진 위치로 이동시킨다. EventIdentifier로 move_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D054 |
| D054 | Dialogue | [시스템] "처치 구역에 도착했습니다. 즉시 의식상태 사정 및 활력징후 사정을 시작하세요." | P012 |
| P012 | Parallel | [플레이어 B, D 전용] 플레이어 B는 환자를 클릭해 의식상태를 사정하고, 플레이어 D는 활력징후 사정도구를 사용해 활력징후를 사정한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D057 |
| P012-B1 | ParallelBranch | [플레이어 B] 브랜치 시작 노드는 D055이며, 플레이어 B가 시나리오 C 환자의 의식상태를 사정한다. CompletionConditionIdentifier는 CC_B_gcs_patientC로 서술한다. | D055 |
| P012-B2 | ParallelBranch | [플레이어 D] 브랜치 시작 노드는 D056이며, 플레이어 D가 시나리오 C 환자의 활력징후를 사정한다. CompletionConditionIdentifier는 CC_D_vital_patientC로 서술한다. | D056 |
| D055 | Dialogue | [플레이어 B 전용] "환자를 클릭하여 환자의 의식상태를 사정하세요." | V035 |
| V035 | Validator | 플레이어 B는 시나리오 C 환자를 클릭해 의식수준 및 GCS를 사정한다. Condition은 Check_gcs_patientC이며, TargetCount는 1이다. | D055_1 |
| D055_1 | Dialogue | "환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다." | D055_2 |
| D055_2 | Dialogue | "환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다." | C044 |
| C044 | Choice | "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?" | C044-Wrong, C044-Correct |
| C044-Wrong | ChoiceOption | "A(Alert, 완전히 깨어 있음)", "P(Pain response, 통증에 반응 있음)", "U(Unconsciousness, 반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_retry_a |
| C044-Correct | ChoiceOption | "V(Verbal response, 음성에 반응 있음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_3 |
| D055_retry_a | Dialogue | "오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다." | C044 |
| D055_3 | Dialogue | [관찰1] "추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C045 |
| C045 | Choice | "관찰된 E(Eye Opening) 점수는 몇 점입니까?" | C045-Wrong, C045-Correct |
| C045-Wrong | ChoiceOption | "4점(자발적)", "2점(통증)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_retry_b |
| C045-Correct | ChoiceOption | "3점(명령)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_4 |
| D055_retry_b | Dialogue | "오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다." | C045 |
| D055_4 | Dialogue | [관찰2] "다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. | C046 |
| C046 | Choice | "관찰된 V(Verbal Response) 점수는 몇 점입니까?" | C006_V-Wrong, C006_V-Correct |
| C046-Wrong | ChoiceOption | "5점(적절한 답변)", "3점(부적절한 답변)", "2점(신음소리)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_retry_c |
| C046-Correct | ChoiceOption | "4점(혼란)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_5 |
| D055_retry_c | Dialogue | "오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다." | C046 |
| D055_5 | Dialogue | [관찰3] "마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다." | C047 |
| C047 | Choice | "관찰된 M(Motor Response) 점수는 몇 점입니까?" | C047-Wrong, C047-Correct |
| C047-Wrong | ChoiceOption | "5점(통증 원인을 치우려고 손을 뻗음)", "4점(통증에 회피)", "3점(이상 굴곡)", "2점(이상 신전)", "1(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_retry_d |
| C047-Correct | ChoiceOption | "6점(명령 수행)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_6 |
| D055_retry_d | Dialogue | "오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다." | C047 |
| D055_6 | Dialogue | "GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다." | D055_7 |
| D055_7 | Dialogue | "GCS의 M(Motor Response) 사정 중 오른쪽 다리가 왼쪽 다리의 정상 근력보다 약하고, 간호사가 가하는 저항에 이기지 못하고 있습니다." | C048 |
| C048 | Choice | "정상인 좌측(5점)에 비해, 우측의 근력 수준은?" | C048-Wrong, C048-Correct |
| C048-Wrong | ChoiceOption | "5점(정상 근력)", "4점(중력+약간의 저항)", "2점(중력에 저항 불가, 좌우 운동)", "1점(약간의 근육 수축)", "0점(움직임 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_retry_e |
| C048-Correct | ChoiceOption | "3점(중력에 저항 가능)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D055_8 |
| D055_retry_e | Dialogue | "오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다." | C048 |
| D055_8 | Dialogue | [플레이어 B 전용] "현재 시나리오 B 환자의 GCS는 13점, 근력(Motor Grade)은 좌측 5점, 우측 3점입니다." | CC_B_gcs_patientC |
| D056 | Dialogue | [플레이어 D 전용] "환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요." | V036 |
| V036 | Validator | [D 1단계 - 시작] 플레이어 D는 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득한다. Condition은 Click_vital_set, Click_electrode, Click_electrode_cable이며, TargetCount는 3이다. | D056_1 |
| D056_1 | Dialogue | "전극을 선택하여 환자의 가슴에 부착하십시오." | V036_1 |
| V036_1 | Validator | [D 2단계] 인벤토리에서 전극을 선택한 뒤 환자(환자의 흉부)를 클릭한다. Condition은 apply_electrode이며, TargetCount는 1이다. | D056_2 |
| D056_2 | Dialogue | "전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요." | V036_2 |
| V036_2 | Validator | [D 3단계] 인벤토리에서 전극 케이블을 선택한 뒤 환자와 모니터를 각각 클릭한다. Condition은 connect_patient_and_monitor_patientC이며, TargetCount는 1이다. | D056_3 |
| D056_3 | Dialogue | "활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다." | V036_3 |
| V036_3 | Validator | [D 4단계] 인벤토리에서 활력징후 측정도구를 클릭해 선택한 뒤 환자를 클릭하면 활력징후가 출력된다. Condition은 check_vital_b이며, TargetCount는 1이다. | E083 |
| E083 | InvokeEvent | [플레이어 D - 활력징후 UI 창 출력] UI로 플레이어 D에게 활력징후를 보여줌과 동시에 활력징후 모니터에 활력징후가 출력된다. EventIdentifier로 activate_vital_monitor_ui_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D056_4 |
| D056_4 | Dialogue | [D 5단계 - 결과 확인] "혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오." | V036_4 |
| V036_4 | Validator | [D 6단계 - 종료] 활력징후 UI 창의 닫기(X) 버튼을 클릭한다. Conditiondms Close_vitalUI_b, TargetCount는 1이다. | CC_D_vital_patientC |
| D057 | Dialogue | [시스템(플레이어 A/C 제외)] "C 환자의 의식상태는 GCS 13점, 근력(Motor Grade) 좌측 5점/우측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. | D058 |
| D058 | Dialogue | [시스템(플레이어 A/C 제외), by 의사 NPC] "간호사 B 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 D 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요." | P013 |
| P013 | Parallel | [플레이어 B, D 전용] 플레이어 B는 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. 플레이어 D는 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D061 |
| P013-B1 | ParallelBranch | [플레이어 B] 브랜치 시작 노드는 D059이며, 플레이어 B가 시나리오 C 환자에게 펜라이트로 동공반사(직접대광반사)를 확인하고 IV 라인을 확보한다. CompletionConditionIdentifier는 CC_B_pupil_iv_patientC로 서술한다. | D059 |
| P013-B2 | ParallelBranch | [플레이어 D] 브랜치 시작 노드는 D060이며, 플레이어 D가 시나리오 C 환자에게 비강캐뉼라를 이용한 산소화와 지혈을 실시한다. CompletionConditionIdentifier는 CC_D_nasal_pressure_patientC로 서술한다. | D060 |
| D059 | Dialogue | [플레이어 B 전용] "먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요." | V037 |
| V037 | Validator | [B 1단계 - 시작] 펜라이트를 클릭해 획득한다. Condition은 Click_penlight이며, Targetcount는 1이다. | D059_1 |
| D059_1 | Dialogue | "펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다." | V037_1 |
| V037_1 | Validator | [B 2단계] 환자의 얼굴을 클릭하면 환자의 양쪽 눈이 확대된다. 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. Condition은 Click_patientC_face이며, TargetCount는 1이다. | E084 |
| E084 | InvokeEvent | 펜라이트가 불이 켜져있다고 가정하고 마우스 커서가 빛을 비추는 것으로 설정하고, 마우스가 환자의 눈 위를 지나가면 빛이 비추는 범위만큼 동공이 반응하는 것으로 한다. [우측 동공은 빛에 따라 동공이 수축하지만, 좌측 동공은 거의 수축하지 않는 것으로 한다.] 양쪽을 최소 1회씩 확인해야 목표를 달성한 것으로 한다. EventIdentifier로 Pupil_reflex_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D059_2 |
| D059_2 | Dialogue | [시스템(플레이어 A/C 제외), by 플레이어 B] "우측 동공에 비해 좌측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다." | D059_3 |
| D059_3 | Dialogue | [플레이어 B 전용] "다음으로 IV 라인을 확보합니다. 환자의 좌측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오." | V037_2 |
| V037_2 | Validator | [B 1단계 - 시작] 플레이어 A는 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득한다. Condition은 Click_20g, Click_iv_set, Click_ns1이며, TargetCount는 3이다. | D059_4 |
| D059_4 | Dialogue | "20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요." | V037_3 |
| V037_3 | Validator | [B 2단계] 플레이어 D는 인벤토리 내 20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭하여 정맥 라인을 확보한다. 캐뉼라는 팔의 오금(팔을 굽혔을 때 굽혀지며 오목해지는 부분)에 적용한다. Condition은 Insert_iv_c_left이며, TargetCount는 1이다. | E085 |
| E085 | InvokeEvent | 20G 캐뉼라가 환자 좌측 팔 오금(팔을 굽혔을때 굽혀지며 오목해지는 부분)에 위치한다. 끝에 얇고 뾰족한 부분은 팔 안으로 삽입되어 팔 위에 색깔이 있는 플라스틱 부분부터 노출되어 보인다. EventIdentifier로 insert_20g_left_patientC를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D059_5 |
| D059_5 | Dialogue | "준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요." | V037_4 |
| V037_4 | Validator | [B 3단계] 플레이어 D는 인벤토리 내 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 20G 캐뉼라를 클릭해 연결한다. Condition은 Connect_cannula_and_ns1_patientC이며, TargetCount는 1이다. | E086 |
| E086 | InvokeEvent | 준비된 생리식염수 1L 수액백이 수액걸대에 걸리고, 우측 팔에 삽입되어 있는 20G 캐뉼라와 줄로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_ns1_right_patientC를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D059_6 |
| D059_6 | Dialogue | [시스템, by 플레이어 B] "정맥로가 확보되었습니다." | D059_7 |
| D059_7 | Dialogue | [시스템(플레이어 A/C 제외), by 플레이어 B] "환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다." | CC_B_pupil_iv_patientC |
| D060 | Dialogue | [플레이어 D 전용] "비강캐뉼라를 이용한 산소화를 먼저 실시합니다." | D060_1 |
| D060_1 | Dialogue | [플레이어 D 전용] "산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오." | V038 |
| V038 | Validator | [D 1단계 - 시작] 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하면 [습윤병에 멸균증류수가 채워진 것으로 가정하고 멸균증류수는 사라지고, 습윤병의 아이템 이름만 변경된다. 준비된 아이템은 "준비된 습윤병"으로 출력한다.] Condition은 Click_humidifierbottle, Click_sdw이며, TargetCount는 2이다. | D060_2 |
| D060_2 | Dialogue | "유량계를 습득하여 산소 유량계를 완성하세요." | V038_1 |
| V038_1 | Validator | [D 2단계] 산소 유량계 (뚜껑)를 클릭해 획득하면 산소 유량계와 습윤병이 합쳐진 아이템으로 자동 변화한다. Condition은 Click_oxyflow이며, TargetCount는 1이다. | D060_3 |
| D060_3 | Dialogue | "완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오." | V038_2 |
| V038_2 | Validator | [D 3단계] 완성된 산소 유량계를 클릭해 선택한 뒤, 흡인기 옆 벽면을 클릭해 설치한다. Condition은 Connect_wall_component_2이며, TargetCount는 1이다. | D060_4 |
| D060_4 | Dialogue | "비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요." | V038_3 |
| V038_3 | Validator | [D 4단계] 비강캐뉼라를 클릭해 획득한다. 이후 벽에 설치된 산소 유량계와 환자를 각각 클릭해 연결한다. [비강캐뉼라는 환자의 코에 적용되고 머리 뒤에서 관이 연결되는 것으로 하고, 머리 뒤에서 코딩을 바탕으로 한 투명 관으로 산소 유량계와 단순 연결한다.] Condition은 Click_nasal, Connect_nasal_and_o2이며, TargetCount는 2이다. | D060_5 |
| D060_5 | Dialogue | [플레이어 D 전용] "산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다." | V038_4 |
| V038_4 | Validator | [D 5단계] 벽에 설치된 산소 유량계를 클릭한다. 클릭하는 경우 투여될 산소의 양을 결정할 수 있도록 UI 창을 출력한다. | C049 |
| C049 | Choice | "투여될 산소의 양을 조절합니다." 3L/5L/10L/15L 중 하나를 선택하도록 안내하고 UI 창을 출력한다. | C049-Wrong, C049-Correct |
| C049-Wrong | ChoiceOption | "5L", "10L", "15L" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D060_retry |
| C049-Correct | ChoiceOption |  "3L" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D060_6 |
| D060_retry | Dialogue | "오답입니다. 처방은 3L 입니다." | C049 |
| D060_6 | Dialogue | [시스템(플레이어 A/C 제외), by 플레이어 D (6단계 - 종료)] "산소 투여가 완료되었습니다." | D060_7 |
| D060_7 | Dialogue | [플레이어 D 전용] "지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오." | V038_5 |
| V038_5 | Validator | [D 1단계 - 시작] 플레이어 D는 멸균장갑과 거즈, 플라스터를 클릭해 획득한다. Condition은 Click_glove, Click_gauze, Click_plaster이며, TargetCount는 3이다. | D060_8 |
| D060_8 | Dialogue | "멸균장갑을 [우클릭]해 착용하십시오." | V038_6 |
| V038_6 | Validator | [D 2단계] 멸균장갑 아이템을 우클릭해 착용한다. Condition은 wear_glove이며, TargetCount는 1이다. | D060_9 |
| D060_9 | Dialogue | "거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오." | V038_7 |
| V038_7 | Validator | [D 3단계] 거즈 아이템을 클릭해 선택한 뒤 환자를 클릭해 적용한다. Condition은 Apply_gauze이며, TargetCount는 1이다. | E087 |
| E087 | InvokeEvent | 거즈가 환자 상처부위에 위치한다. 이 때 포장지 없이 흰 거즈가 상처 위에 덮인 모양이 된다. 가능하면 혈흔 이펙트가 거즈에 스며나와도 좋다. EventIdentifier로 apply_gauze_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D060_10 |
| D060_10 | Dialogue | "압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오." | V038_8 |
| V038_8 | Validator | [D 4단계 - 종료] 플라스터 아이템을 클릭해 선택한 뒤 환자에게 적용되어 있는 거즈를 클릭한다. Condition은 Apply_plaster_on_gauze이며, TargetCount는 1이다. | E088 |
| E088 | InvokeEvent | 환자에게 적용된 거즈의 상단과 하단을 플라스터(테이프)로 고정되어 있는 것으로 변화한다. EventIdentifier로 apply_plaster_on_gauze_patientC를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D060_11 |
| D060_11 | Dialogue | [시스템, by 플레이어 D (5단계 - 종료)] "지혈 중입니다." | D060_12 |
| D060_12 | Dialogue | [시스템(플레이어 B/D 제외), by 플레이어 C] "산소 적용 및 지혈이 완료되었습니다." | CC_D_nasal_pressure_patientC |
| D061 | Dialogue | [시스템, 전체] "시나리오 C 환자에 대한 간호 중재가 완료되었습니다." | CC_B_D_patientC_complete |
| D062 | Dialogue | [시스템, by 의사 NPC] "기전과 사정 결과를 보니 뇌손상이 의심됩니다. 활력징후는 비교적 안정되어 있으니 지금 Brain CT 찍겠습니다. 지금 환자를 CT실로 이동시켜주세요." | E_Move_to_CT |
| E_Move_to_CT | InvokeEvent | [모든 플레이어] 처치가 완료된 시나리오 B 환자와 C 환자를 CT실로 이송한다. EventIdentifier로 move_patients_to_CT를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D063 |
| D063 | Dialogue | [시스템] "시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다." 메세지를 표시한다. | (end) |


## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D063 |
| 종료 연출/설명 | 두 환자 모두 CT실 도달 시 종료된다. 검은 화면으로 fade out 되며 "시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다." 메세지를 표시하며 종료된다. |
