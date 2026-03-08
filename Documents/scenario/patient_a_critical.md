# scenario 환자 A 중증 처치

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 환자 A: 흉부 관통상 및 심정지 대응 |
| 요약 | 환자 A를 처치실로 이동시키고 ABCDE 순서로 처치를 수행한 뒤 ROSC까지 진행한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 A |
| 주요 장소 | 처치실 |
| 리소스 식별자 - 사운드 | 없음 |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_treatment_room |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | D008 |

## 시나리오 본문

[""로 정의된 내용은 각각의 특정 플레이어 혹은 모든 플레이어 및 감독자(관전자) 모두 확인할 수 있도록 텍스트 및 음성으로 출력한다. 음성이 불가피한 경우 텍스트가 모니터에 출력되었으니 확인하라는 알람음을 추가하도록 한다.]
| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D008 | Dialogue | [시스템] "환자 A를 처치실로 이동해야 합니다. 플레이어 A, B, C, D는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오." | P002 |
| P002 | Parallel | 플레이어 A, B, C, D가 환자 침대를 함께 잡고 이동한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | E012 |
| P002-B1 | ParallelBranch | 브랜치 시작 노드는 V001_A이며, 플레이어 A가 침대를 잡는다. CompletionConditionIdentifier는 CC_A_move로 서술한다. | V009_A |
| P002-B2 | ParallelBranch | 브랜치 시작 노드는 V001_B이며, 플레이어 B가 침대를 잡는다. CompletionConditionIdentifier는 CC_B_move로 서술한다. | V009_B |
| P002-B3 | ParallelBranch | 브랜치 시작 노드는 V001_C이며, 플레이어 C가 침대를 잡는다. CompletionConditionIdentifier는 CC_C_move로 서술한다. | V009_C |
| P002-B4 | ParallelBranch | 브랜치 시작 노드는 V001_D이며, 플레이어 D가 침대를 잡는다. CompletionConditionIdentifier는 CC_D_move로 서술한다. | V009_D |
| V009_A | Validator | [플레이어 A] 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher, TargetCount는 1이다. | CC_A_move |
| V009_B | Validator | [플레이어 B] 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher, TargetCount는 1이다. | CC_B_move |
| V009_C | Validator | [플레이어 C] 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher, TargetCount는 1이다. | CC_C_move |
| V009_D | Validator | [플레이어 D] 환자 침대(손잡이 등)을 클릭한다. Condition은 Grab_Stretcher, TargetCount는 1이다. | CC_D_move |
| E012 | InvokeEvent | 플레이어 A, B, C, D가 함께 환자를 처치실로 이동시키고, 베드가 있는 현 위치로 이동시킨다. EventIdentifier로 move_patient_a_to_treatment를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D009 |
| D009 | Dialogue | [시스템] 활력징후 측정, AVPU 및 GCS 측정, 경추 고정 및 흡인을 시작한다. | P003 |
| P003 | Parallel | 플레이어 B는 활력징후 측정, 플레이어 C는 AVPU 및 GCS 사정, 플레이어 D는 경추 고정 및 구강 흡인을 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D013 |
| P003-B1 | ParallelBranch | 브랜치 시작 노드는 D010이며, 플레이어 B가 활력징후를 측정한다(필요한 물품: 활력징후 측정도구/전극/전극 케이블). CompletionConditionIdentifier는 CC_B_vitalcheck_patientA로 서술한다. | D010 |
| P003-B2 | ParallelBranch | 브랜치 시작 노드는 D011이며, 플레이어 C가 AVPU 및 GCS를 사정한다. CompletionConditionIdentifier는 CC_C_gcs_patientA로 서술한다. | D011 |
| P003-B3 | ParallelBranch | 브랜치 시작 노드는 D012이며, 플레이어 D가 경추 고정 및 구강 흡인을 실시한다(필요한 물품: 경추고정기, 흡인기, 석션 라인, 앙커 팁). CompletionConditionIdentifier는 CC_D_suction_patientA 으로 서술한다. | D012 |
| D010 | Dialogue | [플레이어 B 전용] "환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요." | V010 |
| V010 | Validator | [B 1단계 - 시작] 플레이어 B는 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득한다. Condition은 Click_vitalset, Click_electrode, Click_electrode_cable이며, TargetCount는 3이다. | D010_1 |
| D010_1 | Dialogue | "전극을 선택하여 환자의 가슴에 부착하십시오." | V010_1 |
| V010_1 | Validator | [B 2단계] 인벤토리에서 전극을 선택한 뒤 환자(환자의 흉부)를 클릭한다. Condition은 apply_electrode이며, TargetCount는 1이다. | E013 |
| E013 | InvokeEvent | 환자의 흉부에 전극이 부착된다. 부착 위치는 우측 쇄골 아래, 좌측 쇄골 아래, 좌측 겨드랑이의 중앙선과 유두선이 만나는 지점으로 한다. EventIdentifier로 electrode_patientA를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D010_2 |
| D010_2 | Dialogue | "전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요." | V010_2 |
| V010_2 | Validator | [B 3단계] 인벤토리에서 전극 케이블을 선택한 뒤 환자와 모니터를 각각 클릭한다. Condition은 connect_patient_and_monitor_patientA이며, TargetCount는 2이다. | E014 |
| E014 | InvokeEvent | 전극 케이블을 코딩을 통해 줄을 구현하고, 환자와 모니터를 클릭하면 전극과 모니터가 서로 연결된다. EventIdentifier로 connect_electrode_to_monitor를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D010_3 |
| D010_3 | Dialogue | "활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다." | V010_3 |
| V010_3 | Validator | [B 4단계] 인벤토리에서 활력징후 측정도구를 클릭해 선택한 뒤 환자를 클릭하면 활력징후가 출력된다. Condition은 show_vital_patientA이며, TargetCount는 1이다. | E015 |
| E015 | InvokeEvent | [플레이어 B - 활력징후 UI 창 출력] UI로 플레이어 B에게 활력징후를 보여줌과 동시에 활력징후 모니터에 활력징후가 출력된다. EventIdentifier로 activate_vital_monitor_ui_patientA를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D010_4 |
| D010_4 | Dialogue | [B 5단계 - 결과 확인] "혈압 70/40mmHg, 맥박 140회/분 - 약하고 빠름, 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다. 확인 후 모니터 창을 닫으십시오." | V010_4 |
| V010_4 | Validator | [B 5단계 - 종료] 활력징후 UI 창의 닫기(X) 버튼을 클릭한다. Conditiondms Close_vitalUI_patientA, TargetCount는 1이다. | CC_B_vitalcheck_patientA |
| D011 | Dialogue | [플레이어 C 전용] "환자를 클릭해 환자의 의식 상태를 사정하십시오." | V011 |
| V011 | Validator | 플레이어 C는 환자를 클릭해 GCS를 사정한다. Condition은 Check_gcs_patientA이며, TargetCount는 1이다. | D011_1 |
| D011_1 | Dialogue | "환자의 의식 상태(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다." | D011_2 |
| D011_2 | Dialogue | "환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다." | C004 |
| C004 | Choice | "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?" | C004-Wrong, C004-Correct |
| C004-Wrong | ChoiceOption | "A(Alert, 완전히 깨어 있음)", "V(Verbal response, 음성에 반응 있음)", "U(Unconsciousness, 반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_retry_a |
| C004-Correct | ChoiceOption | "P(Pain response, 통증에 반응 있음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_3 |
| D011_retry_a | Dialogue | "오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다." | C004 |
| D011_3 | Dialogue | [관찰1] "추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다." | C005 |
| C005 | Choice | "관찰된 E(Eye Opening) 점수는 몇 점입니까?" | C005-Wrong, C005-Correct |
| C005-Wrong | ChoiceOption | "4점(자발적)", "3점(명령)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_retry_b |
| C005-Correct | ChoiceOption | "2점(통증)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_4 |
| D011_retry_b | Dialogue | "오답입니다. 통증 자극에만 반응했음을 유의하십시오." | C005 |
| D011_4 | Dialogue | [관찰2] "다음은 Verbal Response(V)입니다. "여기가 어디예요?"라고 묻자, 환자는 이해할 수 없는 신음소리만 내고 있습니다." | C006 |
| C006 | Choice | "관찰된 V(Verbal Response) 점수는 몇 점입니까?" | C006-Wrong, C006-Correct |
| C006-Wrong | ChoiceOption | "5점(적절한 답변)", "4점(혼란)", "3점(부적절한 답변)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_retry_c |
| C006-Correct | ChoiceOption | "2점(신음소리)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_5 |
| D011_retry_c | Dialogue | "오답입니다. 현재 환자는 알아들을 수 없는 소리만 내고 있습니다." | C006 |
| D011_5 | Dialogue | [관찰3] "마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 팔을 재빨리 굽혀 자극을 피합니다." | C007 |
| C007 | Choice | "관찰된 M(Motor Response) 점수는 몇 점입니까?" | C007-Wrong, C007-Correct |
| C007-Wrong | ChoiceOption | "6점(명령 수행)", "5점(통증 원인을 치우려고 손을 뻗음)", "3점(이상 굴곡)", "2점(이상 신전)", "1(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_retry_d |
| C007-Correct | ChoiceOption | "4점(통증에 회피)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D011_6 |
| D011_retry_d | Dialogue | "오답입니다. 현재 통증에 회피하고 있습니다." | C007 |
| D011_6 | Dialogue | "GCS 측정 완료. E2 / V2 / M4 = 총 8점 (Stupor) 입니다." | CC_C_gcs_patientA |
| D012 | Dialogue | [플레이어 D 전용] "기도 확보를 위해 환자의 경추를 고정하고 구강 석션을 진행합니다. 경추고정기, 흡인기, 석션 라인, 앙커 팁을 클릭해 획득하세요." | V012 |
| V012 | Validator | [D 1단계 - 시작] 플레이어 D는 경추 고정기, 흡인기, 석션 라인, 앙커 팁을 클릭해 획득한다. Condition은 Click_neckstabilizer, Click_wall_suction, Click_suction_line, Click_yankauer이며, TargetCount는 4이다. | D012_1 |
| D012_1 | Dialogue | "경추 고정기를 환자에게 적용하십시오." | V012_1 |
| V012_1 | Validator | [D 2단계] 인벤토리에서 경추 고정기를 클릭해 선택한 뒤 환자(환자의 목 부위)를 클릭한다. Condition은 Apply_stabilizer_a이며, TargetCount는 1이다. | D012_2 |
| D012_2 | Dialogue | "흡인기를 벽에 설치하십시오." | V012_2 |
| V012_2 | Validator | [D 3단계] 인벤토리에서 흡인기를 클릭해 선택한 뒤 벽에 있는 연결부를 클릭해 연결한다. Condition은 Connect_wall_component_1이며, TargetCount는 1이다. | D012_3 |
| D012_3 | Dialogue | "석션 라인과 앙커 팁을 흡인기에 연결하십시오." | V012_3 |
| V012_3 | Validator | [D 4단계] 인벤토리에서 조립된 석션 라인 및 앙커 팁을 클릭해 선택한 뒤 벽에 연결된 흡인기를 클릭한다. Condition은 Connect_wall_component_and_suction이며, TargetCount는 1이다. | D012_4 |
| D012_4 | Dialogue | "흡인기를 클릭한 뒤 환자를 클릭해 구강 흡인을 진행하십시오." | V012_4 |
| V012_4 | Validator | [D 5단계 - 종료] 벽에 설치된 흡인기를 클릭한 뒤, 환자를 클릭해 구강 흡인을 진행한다. Condition은 Suction_patientA이며, TargetCount는 1이다. | D012_5 |
| D012_5 | Dialogue | "경추 고정 후 석션이 완료되었습니다." | CC_D_suction_patientA |
| D013 | Dialogue | [시스템] "환자의 의식상태는 GCS 8점, 활력징후는 혈압 70/40mmHg, 맥박수 140회/분 (빠르고 약함), 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다." | E016 |
| E016 | InvokeEvent | [모든 플레이어] 모든 플레이어에게 시스템 UI 창으로 환자의 활력징후 정보를 출력한다. 10초 간 출력되게 하고 원하는 경우 최소화하여 닫을 수 있도록 한다. 필요 시 확인할 수 있도록 클릭하면 다시 모든 정보를 출력하도록 한다. EventIdentifier로 vitalinfo_patientA를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D014 |
| D014 | Dialogue | [시스템, by 의사 NPC] "기도 확보를 위해 intubation을 시행하겠습니다. 간호사 B 선생님은 삽관 보조해주세요." | D014_1 |
| D014_1 | Dialogue | [시스템, by 의사 NPC] "그동안 간호사 C 선생님은 멸균장갑을 착용하고 거즈로 출혈부위를 지혈해주세요." | D014_2 |
| D014_2 | Dialogue | [시스템, by 의사 NPC] "간호사 D 선생님은 수액 투여를 위해 양팔에 IV 라인 확보해주세요. 혈관을 보고 18게이지로 잡고, 수액은 생리식염수와 플라즈마 솔루션 달겠습니다." | P004 |
| P004 | Parallel | 플레이어 B는 삽관 보조, 플레이어 C는 지혈, 플레이어 D는 IV 라인을 확보한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D018 |
| P004-B1 | ParallelBranch | 브랜치 시작 노드는 D015이며, 플레이어 B가 의사에게 전달할 기관삽관 관련 물품을 준비한다(필요한 물품: 후두경 블레이드, 후두경 손잡이, 기관내관, 스타일렛, 5cc 주사기, 플라스터). [이후 플레이어 A가 산소 공급을 위해 산소 유량계와 기관내관 간을 연결한다.] CompletionConditionIdentifier는 CC_B_intubation_A_oxy_patientA 로 서술한다. | D015 |
| P004-B2 | ParallelBranch | 브랜치 시작 노드는 D016이며, 플레이어 C가 지혈을 실시한다(필요한 물품: 멸균장갑, 거즈, 플라스터). CompletionConditionIdentifier는 CC_C_stopbleeding_patientA로 서술한다. | D016 |
| P004-B3 | ParallelBranch | 브랜치 시작 노드는 D017이며, 플레이어 D가 IV 라인을 확보한다(필요한 물품: 18G 혹은 20G 캐뉼라, 수액세트 2개, 생리식염수 수액, 플라즈마 솔루션 수액). [이후 플레이어 C가 중심정맥관 삽입 보조 및 Level 1 rapid infuser를 연결한다.] CompletionConditionIdentifier는 CC_D_iv_patientA로 서술한다. | D017 |
| D015 | Dialogue | [플레이어 B 전용] "기관내삽관에 필요한 물품을 준비합니다. 좌측 체크리스트 창을 참고하여 필요한 물품을 클릭해 획득하세요." | E017 |
| E017 | InvokeEvent | [플레이어 B 전용 - 체크리스트 시작] 좌측 상단에 체크리스트 UI 창을 띄워 준비할 물품을 출력한다. 8가지 아이템의 항목과 의사에게 전달해야 할 2가지 아이템과 행위로 구성한다. [표시할 8가지 아이템]은 후두경 블레이드, 후두경 손잡이, 후두경, 기관내관, 스타일렛, 준비된 기관내관, 플라스터, 5cc 주사기이다. 의사에게 전달해야 할 2가지 아이템과 행위는 각각 후두경 전달하기, 준비된 기관내관 전달하기로 정의한다. EventIdentifier로 checklist_intu_a를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | V013 |
| V013 | Validator | [B 1단계 - 시작] 플레이어 B는 후두경 블레이드, 후두경 손잡이, 기관내관, 스타일렛, 플라스터, 5cc 주사기를 클릭해 획득한다. Condition은 Click_laryngo_blade, Click_laryngo_haldle, Click_et_tube, Click_stylet, Click_plaster, Click_syringe_5cc이며, TargetCount는 6이다. [개발 참고: 6개의 Condition이 개별적으로 충족될 때마다 UI 체크리스트에 실시간으로 반영, 후두경 블레이드와 후두경 손잡이를 획득하면 '후두경'으로 아이템이 변화하고, 기관내관과 스타일렛을 획득하면 '준비된 기관내관'으로 아이템이 변화한다.] | D015_1 |
| D015_1 | Dialogue | "완성된 후두경을 의사에게 전달하세요." | V013_1 |
| V013_1 | Validator | [B 2단계] 후두경 블레이드와 손잡이, 두 아이템이 합쳐져 만들어진 후두경을 선택한 뒤 의사 NPC를 클릭해 전달한다. Condition은 Pass_laryngoscope이며, TargetCount는 1이다. | D015_2 |
| D015_2 | Dialogue | "완성된 기관내관을 의사에게 전달하세요." | V013_2 |
| V013_2 | Validator | [B 3단계] ET-tube와 스타일렛, 두 아이템이 합쳐져 만들어진 기관내관을 클릭한 뒤, 의사 NPC를 클릭해 전달한다. Condition은 Pass_et_tube_ready이며, TargetCount는 1이다. | E018 |
| E018 | InvokeEvent | 의사 NPC가 환자에게 삽입하는 모션을 취한다(기관내관이 전부 삽입되지 않고 입에 ballooning 부분만 들어간 채 멈춘다). EventIdentifier로 insert_et_tube를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D015_3 |
| D015_3 | Dialogue | "환자 구강에 삽입된 기관내관을 클릭해 스타일렛을 제거하세요." | V013_3 |
| V013_3 | Validator | [B 4단계] 환자 구강에 중간정도 삽입된 기관내관을 클릭해 스타일렛을 제거한다. Condition은 Remove_intu_stylet이며, TargetCount는 1이다. | E019 |
| E019 | InvokeEvent | 스타일렛이 제거된 기관내관이 환자의 구강에 2/3가량 삽입된다. EventIdentifier로 remove_stylet을 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D015_4 |
| D015_4 | Dialogue | "5cc 주사기를 의사에게 전달하세요." | V013_4 |
| V013_4 | Validator | [B 5단계] 5cc 주사기를 클릭해 선택한 뒤, 의사 NPC를 클릭해 전달한다. Condition은 Pass_syringe이며, TargetCount는 1이다. | D015_5 |
| D015_5 | Dialogue | "플라스터를 클릭해 선택한 뒤, 삽입된 기관내관을 고정하십시오." | V013_5 |
| V013_5 | Validator | [B 6단계 - 종료, 체크리스트 종료] 플라스터를 클릭해 선택한 뒤, 환자에게 삽입된 기관내관을 클릭해 고정한다. Condition은 Apply_plaster_on_intu이며, TargetCount는 1이다. | D015_6 |
| D015_6 | Dialogue | [시스템, by 플레이어 B (6단계 - 종료)] "삽입된 깊이 23cm, 기관내관 고정되었습니다." | D015_7 |
| D015_7 | Dialogue | [시스템, by 의사 NPC] "삽관이 끝났고, 자발호흡이 있으니 간호사 A 선생님이 T-piece 연결하고 산소 10L 주면서 산소포화도 모니터링 해주세요." | D015_8 |
| D015_8 | Dialogue | [플레이어 A 전용] "산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오." | V013_6 |
| V013_6 | Validator | [A 1단계 - 시작] 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하면 [습윤병에 멸균증류수가 채워진 것으로 가정하고 멸균증류수는 사라지고, 습윤병의 아이템 이름만 변경된다. 준비된 아이템은 "준비된 습윤병"으로 출력한다.] Condition은 Click_humidifierbottle, Click_sdw이며, TargetCount는 2이다. | D015_9 |
| D015_9 | Dialogue | "유량계를 습득하여 산소 유량계를 완성합니다." | V013_7 |
| V013_7 | Validator | [A 2단계] 산소 유량계 (뚜껑)를 클릭해 획득하면 산소 유량계와 습윤병이 합쳐진 아이템으로 자동 변화한다. Condition은 Click_oxyflow이며, TargetCount는 1이다. | D015_10 |
| D015_10 | Dialogue | "완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오." | V013_8 |
| V013_8 | Validator | [A 3단계] 완성된 산소 유량계를 클릭해 선택한 뒤, 흡인기 옆 벽면을 클릭해 설치한다. Condition은 Connect_wall_component_2이며, TargetCount는 1이다. | D015_11 |
| D015_11 | Dialogue | "산소줄과 T-piece를 각각 클릭해 획득하고, 산소 유량계와 환자에게 삽입된 기관내관을 각각 클릭해 연결하세요." | V013_9 |
| V013_9 | Validator | [A 4단계] [산소줄(o2 line)과 T-piece를 각각 클릭해 획득하면 산소줄과 T-piece가 연결된 T-piece set으로 변화한다.] 이후 벽에 설치된 산소 유량계와 환자에게 삽입된 기관내관을 각각 클릭해 연결한다. [이 때 T-piece set은 조립된 상태로 환자의 기관내관에 연결되며, 기관내관과 산소 유량계 간의 연결할 산소줄은 생성에 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] Condition은 Click_o2line, Click_tpiece, Connect_tpiece_and_oxyflow(환자의 기관내관과 산소유량계 각각 클릭)이며, TargetCount는 4이다. | D015_12 |
| D015_12 | Dialogue | [플레이어 A 전용] "산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다." | V013_10 |
| V013_10 | Validator | [A 5단계] 벽에 설치된 산소 유량계를 클릭한다. 클릭하는 경우 투여될 산소의 양을 결정할 수 있도록 UI 창을 출력한다. | C008 |
| C008 | Choice | "투여될 산소의 양을 조절합니다." 3L/5L/10L/15L 중 하나를 선택하도록 안내하고 UI 창을 출력한다. | C008-Wrong, C008-Correct |
| C008-Wrong | ChoiceOption | "3L", "5L", "15L" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D015_retry |
| C008-Correct | ChoiceOption | "10L" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D015_13 |
| D015_retry | Dialogue | "오답입니다. 처방은 10L 입니다." | C008 |
| D015_13 | Dialogue | [시스템, by 플레이어 A (6단계 - 종료)] "산소 투여가 완료되었습니다." | CC_B_intubation_A_oxy_patientA |
| D016 | Dialogue | [플레이어 C 전용] "지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오." | V014 |
| V014 | Validator | [C 1단계 - 시작] 플레이어 C는 멸균장갑과 거즈, 플라스터를 클릭해 획득한다. Condition은 Click_glove, Click_gauze, Click_plaster이며, TargetCount는 3이다. | D016_1 |
| D016_1 | Dialogue | "멸균장갑을 [우클릭]해 착용하십시오." | V014_1 |
| V014_1 | Validator | [C 2단계] 멸균장갑 아이템을 우클릭해 착용한다. Condition은 wear_glove이며, TargetCount는 1이다. | D016_2 |
| D016_2 | Dialogue | "거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오." | V014_2 |
| V014_2 | Validator | [C 3단계] 거즈 아이템을 클릭해 선택한 뒤 환자를 클릭해 적용한다. Condition은 Apply_gauze이며, TargetCount는 1이다. | E020 |
| E020 | InvokeEvent | 거즈가 환자 상처부위에 위치한다. 이 때 포장지 없이 흰 거즈가 상처 위에 덮인 모양이 된다. 가능하면 혈흔 이펙트가 거즈를 뚫고 나와도 좋다. EventIdentifier로 apply_gauze_patientA를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D016_3 |
| D016_3 | Dialogue | "압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오." | V014_3 |
| V014_3 | Validator | [C 4단계 - 종료] 플라스터 아이템을 클릭해 선택한 뒤 환자에게 적용되어 있는 거즈를 클릭한다. Condition은 Apply_plaster_on_gauze이며, TargetCount는 1이다. | E021 |
| E021 | InvokeEvent | 환자에게 적용된 거즈의 상단과 하단을 플라스터(테이프)로 고정되어 있는 것으로 변화한다. EventIdentifier로 apply_plaster_on_gauze_patientA를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D016_4 |
| D016_4 | Dialogue | [시스템, by 플레이어 C (5단계 - 종료)] "지혈 중입니다." | CC_C_stopbleeding_patientA |
| D017 | Dialogue | [플레이어 D 전용] "환자의 좌측과 우측 팔에 IV 라인을 확보해야 합니다. 18게이지 2개, 준비된 생리식염수 1L 수액백, 준비된 플라즈마 솔루션 1L 수액백을 클릭해 획득하십시오." | V015 |
| V015 | Validator | [D 1단계 - 시작] 플레이어 D는 18게이지 캐뉼라 2개를 클릭해 획득한다. Condition은 Click_18g이며, TargetCount는 2이다. | D017_1 |
| D017_1 | Dialogue | "18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요." | V015_1 |
| V015_1 | Validator | [D 2단계] 플레이어 D는 인벤토리 내 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭하여 정맥 라인을 확보한다. 캐뉼라는 팔의 오금(팔을 굽혔을 때 굽혀지며 오목해지는 부분)에 적용한다. Condition은 Insert_iv_patientA_left이며, TargetCount는 1이다. | E022 |
| E022 | InvokeEvent | 18G 캐뉼라가 환자 좌측 팔 오금(팔을 굽혔을때 굽혀지며 오목해지는 부분)에 위치한다. 끝에 얇고 뾰족한 부분은 팔 안으로 삽입되어 팔 위에 색깔이 있는 플라스틱 부분부터 노출되어 보인다. EventIdentifier로 insert_18g_left를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D017_2 |
| D017_2 | Dialogue | "준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 18G 캐뉼라를 클릭해 연결하세요." | V015_2 |
| V015_2 | Validator | [D 3단계] 플레이어 D는 인벤토리 내 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 18G 캐뉼라를 클릭해 연결한다. Condition은 Connect_cannula_and_ns1이며, TargetCount는 1이다. | E023 |
| E023 | InvokeEvent | 준비된 생리식염수 1L 수액백이 환자 좌측에 위치한 수액걸대에 걸리고, 좌측 팔에 삽입되어 있는 18G 캐뉼라와 줄로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_ns1_left를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D017_3 |
| D017_3 | Dialogue | "동일하게, 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭해 정맥 라인을 확보하세요." | V015_3 |
| V015_3 | Validator | [D 4단계] 인벤토리 내 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭하여 정맥 라인을 확보한다. 캐뉼라는 팔의 오금(팔을 굽혔을 때 굽혀지며 오목해지는 부분)에 적용한다. Condition은 Insert_iv_patientA_right이며, TargetCount는 1이다. | E024 |
| E024 | InvokeEvent | 18G 캐뉼라가 환자 좌측 팔 오금(팔을 굽혔을때 굽혀지며 오목해지는 부분)에 위치한다. 끝에 얇고 뾰족한 부분은 팔 안으로 삽입되어 팔 위에 색깔이 있는 플라스틱 부분부터 노출되어 보인다. EventIdentifier로 insert_18g_right를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D017_4 |
| D017_4 | Dialogue | "준비된 플라즈마 솔루션 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 18G 캐뉼라를 클릭해 연결하세요." | V015_4 |
| V015_4 | Validator | [D 5단계] 인벤토리 내 준비된 플라즈마 솔루션 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 18G 캐뉼라를 클릭해 연결한다. Condition은 Connect_cannula_and_ps1이며, TargetCount는 1이다. | E025 |
| E025 | InvokeEvent | 준비된 플라즈마 솔루션 1L 수액백이 환자 우측에 위치한 수액걸대에 걸리고, 우측 팔에 삽입되어 있는 18G 캐뉼라와 줄로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_ps1_right를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D017_5 |
| D017_5 | Dialogue | [시스템, by 플레이어 D (6단계 - 종료)] "정맥로가 확보되었습니다." | D017_6 |
| D017_6 | Dialogue | [시스템, by 의사 NPC] "그래도 혈압이 잡히지 않네요. C-line 잡아서 수액을 빠르게 투여하겠습니다. 간호사 C 선생님, C-line set 건네주세요." | D017_7 |
| D017_7 | Dialogue | [플레이어 C 전용] "C-line set을 클릭해 획득하고, 해당 아이템을 의사에게 전달하세요." | V015_5 |
| V015_5 | Validator | [C 1단계] C-line set 아이템을 클릭해 획득해고, 해당 아이템을 의사 NPC를 클릭해 전달한다. Condition은 Pass_cline_set이며, TargetCount는 1이다. | E026 |
| E026 | InvokeEvent | 의사 NPC가 삽입하는 모션을 보여준 뒤, 환자의 몸통에서 목으로 이어지는 경계선으로부터 수액백 또는 혈액백을 연결할 수 있는 관이 3개가 나온다. [해당 관은 구현할 수 있는지 확인이 필요하다.] EventIdentifier로 insert_cline을 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D017_8 |
| D017_8 | Dialogue | [시스템, by 의사 NPC] "[간호사 C 선생님], Level 1 연결시키고 플라즈마 솔루션 달아주세요. 그 이후 [간호사 D 선생님]이 혈액백도 Level 1에 달아주세요." | P005 |
| P005 | Parallel | 플레이어 C는 플라즈마 솔루션 1L 수액백을 획득한 뒤 Level 1 rapid infuser와 상호작용하고, 플레이어 D는 인벤토리 내 혈액백을 선택한 뒤 Level 1 rapid infuser와 상호작용한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | E027 | 
| P005-B1 | ParallelBranch | 브랜치 시작 노드는 D017_9이며, 플레이어 C는 플라즈마 솔루션 1L 수액백을 획득한 뒤, Level 1 rapid infuser와 상호작용한다. CompletionConditionIdentifier는 CC_C_plasma_to_lv1으로 서술한다. | D017_9 |
| P005-B2 | ParallelBranch | 브랜치 시작 노드는 D017_11이며, 플레이어 D는 인벤토리 내 혈액백을 선택한 뒤, Level 1 rapid infuser와 상호작용한다. CompletionConditionIdentifier는 CC_D_blood_to_lv1으로 서술한다. | D017_11 |
| D017_9 | Dialogue | [플레이어 C 전용] "플라즈마 솔루션 1L 수액백을 클릭해 획득하세요." | V015_6 |
| V015_6 | Validator | [C 2단계] 플라즈마 솔루션 1L 수액백을 클릭해 획득한다. Condition은 Click_ps1이며, TargetCount는 1이다. | D017_10 |
| D017_10 | Dialogue | "플라즈마 솔루션 1L 수액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요." | V015_7 |
| V015_7 | Validator | [C 3단계] 플라즈마 솔루션 1L 수액백을 클릭한 뒤, Level 1 rapid infuser를 클릭해 연결한다. Level 1의 외형은 변하지 않지만[가능하다면 좋다] 적용된 것으로 한다. 플라즈마 솔루션 1L 수액백이 상호작용되면 수액백 아이템은 사라지고, Level 1 rapid infuser의 상태를 "플라즈마 솔루션 1L 수액백 연결완료"를 출력한다. Condition은 Connect_ps1_to_lv1이며, TargetCount는 1이다. | CC_C_plasma_to_lv1 |
| D017_11 | Dialogue | [플레이어 D 전용] "인벤토리 내 혈액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요." | V015_8 |
| V015_8 | Validator | [플레이어 D 전용] 인벤토리 내 혈액백을 클릭한 뒤, Level 1 rapid infuser를 클릭해 연결한다. Level 1의 외형은 변하지 않지만[가능하다면 좋다] 적용된 것으로 한다. 혈액백이 상호작용되면 혈액백은 사라지고, Level 1 rapid infuser의 상태를 "혈액백 연결완료"를 출력한다. Condition은 Connect_blood_to_lv1이며, TargetCount는 1이다. | CC_D_blood_to_lv1 |
| E027 | InvokeEvent | 두 개 아이템이 모두 상호작용이 완료되면 Level 1 rapid infuser의 상태를 "준비 완료"로 출력한다. EventIdentifier로 lv1_ready를 호출한다. MoveNextBehacior는 Immediate로 서술한다. | D017_12 |
| D017_12 | Dialogue | "Level 1을 클릭한 뒤, 환자에게 삽입된 C-line을 클릭해 연결하세요." | V015_9 |
| V015_9 | Validator | [C 4단계] Level 1 rapid infuser를 클릭한 뒤, 환자에게 삽입된 C-line을 클릭하면 연결된다. Condition은 Connect_lv1_to_a이며, TargetCount는 1이다. | E028 |
| E028 | InvokeEvent | Level 1 rapid infuser와 환자에게 삽입된 C-line이 [2개의 줄]로 연결된다. [이 때 연결할 줄은 생성하는 데 제한적이었기 때문에, 코딩을 바탕으로 투명 관으로 단순 연결한다.] EventIdentifier로 connect_lv1_to_cline을 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D017_13 |
| D017_13 | Dialogue | [시스템, by 플레이어 C] "Level 1이 연결되었습니다." | CC_D_iv_patientA |
| D018 | Dialogue | [시스템, by 의사 NPC] "그래도 혈압이 잘 안잡히네요.." | E029 |
| E029 | InvokeEvent | 환자 모니터에서 출력되는 창을 모두에게 띄운다. 동시에, 환자의 활력징후가 급격히 하락한다. "환자의 의식수준이 낮아지고, 혈압 -?- mmHg[측정불가하다는 의미], 맥박수 70회/분, 호흡수 -?- 회/분[측정불가하다는 의미], SpO2 -?-%" 를 출력한다. [파형을 출력하는 경우, flatline을 그린다.] EventIdentifier로 patient_crash_ui를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D019 |
| D019 | Dialogue | [시스템, by 의사 NPC] "심전도만 출력되고, 다른 활력징후가 출력되지 않습니다. 간호사 B 선생님, 환자 맥박 확인해주세요." | D020 |
| D020 | Dialogue | [플레이어 B 전용] "환자의 경동맥을 촉지해 맥박을 확인합니다. 목 부위를 클릭하세요." | V016 |
| V016 | Validator | [플레이어 B] 환자의 목 부위를 클릭해 맥박을 촉지한다. 촉지 부위는 C-line이 삽입된 반대편 경동맥을 촉지한다. Condition은 Check_pulse_patientA이며, TargetCount는 1이다. | D021 |
| D021 | Dialogue | [시스템, by 플레이어 B] "맥박 없습니다." | D022 |
| D022 | Dialogue | [시스템, by 의사 NPC] "PEA입니다. CPR 하겠습니다. 제가 팀 리더를 맡겠습니다. 간호사 A 선생님은 앰부백 짜주시고, 간호사 B 선생님은 가슴압박 해주세요. 간호사 C 선생님은 제세동기 연결해주시고, 간호사 D 선생님은 C-line으로 에피네프린 1mg 투여해주세요." | P006 |
| P006 | Parallel | 플레이어 A는 앰부백을 이용한 산소화, 플레이어 B는 가슴 압박, 플레이어 C는 제세동기 연결, 플레이어 D는 에피네프린 투여를 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D027 |
| P006-B1 | ParallelBranch | 브랜치 시작 노드는 D023이며, 플레이어 A는 앰부백을 이용한 산소화를 실시한다(필요한 물품: 앰부백). CompletionConditionIdentifier는 CC_A_ambu로 서술한다. | D023 |
| P006-B2 | ParallelBranch | 브랜치 시작 노드는 D024이며, 플레이어 B는 가슴 압박을 실시한다. CompletionConditionIdentifier는 CC_B_chestcomp로 서술한다. | D024 |
| P006-B3 | ParallelBranch | 브랜치 시작 노드는 D025이며, 플레이어 C는 제세동기를 연결한다(필요한 물품: 제세동 패드 / 필요한 기구: 제세동기). CompletionConditionIdentifier는 CC_C_defib로 서술한다. | D025 |
| P006-B4 | ParallelBranch | 브랜치 시작 노드는 D026이며, 플레이어 D는 에피네프린 투여를 실시한다(필요한 물품: 5cc 주사기, 20cc 주사기, 에피네프린, 20cc 생리식염수). CompletionConditionIdentifier는 CC_D_epi로 서술한다. | D026 |
| D023 | Dialogue | [플레이어 A 전용] "앰부백과 산소 저장낭을 클릭해 획득하세요." | V017 |
| V017 | Validator | [A 1단계 - 시작] 앰부백과 산소 저장낭을 클릭해 획득한다. [앰부백과 산소 저장낭을 획득한 즉시 연결되어 세트를 이룬다.] Condition은 Click_ambubag, Click_reservoir_bag이며, TargetCount는 2이다. | D023_1 |
| D023_1 | Dialogue | "환자에게 연결된 T-piece를 클릭해 연결을 해제하세요." | V017_1 |
| V017_1 | Validator | [A 2단계] 환자에게 연결된 T-piece를 클릭하면 연결이 해제되어 아이템이 인벤토리로 다시 획득된다. 이 때 연결되어 있던 줄은 T-piece와의 연결이 해제되고 사라지지 않는다. Condition은 Remove_tpice이며, TargetCount는 1이다. | D023_2 |
| D023_2 | Dialogue | "앰부백을 클릭해 선택한 뒤, 환자에게 삽입된 기관내관을 클릭해 연결하세요. 이후, 산소줄과 앰부백을 클릭해 연결합니다." | V017_2 |
| V017_2 | Validator | [A 3단계] 인벤토리 내 앰부백을 클릭해 선택한 뒤, 환자에게 삽입된 기관내관을 클릭해 연결한다. 이후, 연결이 해제되어 있던 산소줄을 클릭하고 앰부백을 클릭하면 산소줄이 연결된다. Condition은 Connect_ambubag, Connect_o2_to_ambu이며, TargetCount는 2이다. | V017_3 |
| V017_3 | Validator | [A 4단계] 산소줄이 연결되면 곧바로 산소 투여량을 조절하는 UI를 출력한다. 단, 선택지는 "Full" 하나만 출력한다. 해당 버튼을 클릭하면 산소가 켜진다. Condition은 O2_full이며, TargetCount는 1이다. | E030 |
| E030 | InvokeEvent | 산소가 공급되는 소리 혹은 새는 소리가 시작된다. EventIdentifier로 o2_sound를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D023_3 |
| D023_3 | Dialogue | "앰부백을 클릭해 산소 공급을 시작하세요." | V017_4 |
| V017_4 | Validator | [A 4단계] 앰부백을 클릭하면 산소 공급이 시작된다. Condition은 Start_ambu이며, TargetCount는 1이다. | E031 |
| E031 | InvokeEvent | 플레이어 A의 앰부백을 짜는 애니메이션과 함께, 앰부백이 짜여지는 애니메이션을 시작한다. [해당 애니메이션들은 모든 플레이어들의 임무가 완료될 때 까지 지속한다.] EventIdentifier로 start_ambubagging을 호출한다. MoveNextBehavior는 Immediate로 서술한다. | E032 |
| E032 | InvokeEvent | C009에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C009 |
| C009 | Choice | [플레이어 A 전용] "1. 성인의 정확한 산소 제공량은?" | C009-Wrong, C009-Correct |
| C009-Wrong | ChoiceOption | "약 1500ml (다섯 손가락 모두를 이용해 백을 짠다)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D023_retry_a |
| C009-Correct | ChoiceOption | "약 600ml (다섯 손가락 모두를 이용해 백을 짠다)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E033 |
| D023_retry_a | Dialogue | "오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다." | C009 |
| E033 | InvokeEvent | C010에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C010 |
| C010 | Choice | [플레이어 A 전용] "2. 심폐소생술 중 적절한 속도는?" | C010-Wrong, C010-Correct |
| C010-Wrong | ChoiceOption | "10초에 1번 (분당 약 6회)", "3초에 1번 (분당 약 20회)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D023_retry_b |
| C010-Correct | ChoiceOption | "6초에 1번 (분당 약 10회)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_A_ambu |
| D023_retry_b | Dialogue | "오답입니다. 6초에 1번씩 눌러야 합니다." | C010 |
| D024 | Dialogue | [플레이어 B 전용] "환자의 가슴을 클릭해 가슴압박을 시작하세요." | V018 |
| V018 | Validator | [B] 환자의 가슴을 클릭하여 가슴압박을 시작한다. Condition은 Click_to_start_comp이며, TargetCount는 1이다. | E034 |
| E034 | InvokeEvent | 플레이어 B의 가슴압박 애니메이션과 함께, 가슴압박 리듬에 맞춰 환자의 가슴이 들어가는 애니메이션을 시작한다. [해당 애니메이션들은 모든 플레이어들의 임무가 완료될 때 까지 지속한다.] EventIdentifier로 start_chest_compression을 호출한다. MoveNextBehavior는 Immediate로 서술한다. | E035 |
| E035 | InvokeEvent | C011에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C011 |
| C011 | Choice | [플레이어 B 전용] "1. 성인의 정확한 가슴 압박 깊이는?" | C011-Wrong, C011-Correct |
| C011-Wrong | ChoiceOption | "약 4cm", "약 6cm" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D024_retry_a |
| C011-Correct | ChoiceOption | "약 5cm" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E036 |
| D024_retry_a | Dialogue | "오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다." | C011 |
| E036 | InvokeEvent | C012에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C012 |
| C012 | Choice | [플레이어 B 전용] "2. 성인의 정확한 가슴 압박 위치는?" | C012-Wrong, C012-Correct |
| C012-Wrong | ChoiceOption | "양측 유두선상의 중간지점" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D024_retry_b |
| C012-Correct | ChoiceOption | "흉골 하부 1/2 지점" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E037 |
| D024_retry_b | Dialogue | "오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다." | C012 |
| E037 | InvokeEvent | C013에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C013 |
| C013 | Choice | [플레이어 B 전용] "3. 정확한 가슴 압박 횟수는?" | C013-Wrong, C013-Correct |
| C013-Wrong | ChoiceOption | "분당 약 80~100회", "분당 약 120~140회" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D024_retry_c |
| C013-Correct | ChoiceOption | "분당 약 100~120회" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E038 |
| D024_retry_c | Dialogue | "오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다." | C013 |
| E038 | InvokeEvent | C014에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 Delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C014 |
| C014 | Choice | [플레이어 B 전용] "4. 가슴압박 시 주의사항은?" | C014-Wrong, C014-Correct |
| C014-Wrong | ChoiceOption | "지쳐도 한 사람이 계속 가슴압박을 수행한다.", "뼈가 부러진 것 같으면 멈춘다." (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D024_retry_d |
| C014-Correct | ChoiceOption | "충분한 이완을 제공한다." (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_B_chestcomp |
| D024_retry_d | Dialogue | "오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 심장에 혈액이 들어와 내보낼 수 있게 됩니다." | C014 |
| D025 | Dialogue | [플레이어 C 전용] "제세동 카트를 환자 옆으로 가져오세요." | V019 |
| V019 | Validator | [C 1단계] 제세동기가 올려져 있는 카트를 클릭하여 선택하면 움직일 수 있도록 한다. Condition은 Move_defibcart_to_patient이며, TargetCount는 1이다. | D025_1 |
| D025_1 | Dialogue | "제세동 패드를 획득하고, 환자 흉부의 올바른 위치에 부착하세요." | V019_1 |
| V019_1 | Validator | [C 2단계] 제세동 패드를 클릭해 획득한 뒤, 환자의 흉곽에 부착한다. 부착하기 위해 클릭해야 하는 위치는 우측 쇄골 하부, 좌측 겨드랑이 선의 상체 중간 쯤으로 한다. Condition은 Attach_defibpad 이며, TargetCount는 1이다. | E039 |
| E039 | InvokeEvent | 환자에게 제세동 패드가 적용된다. EventIdentifier로 attach_defibpad를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | E040 |
| E040 | InvokeEvent | 최초 전원이 켜지는 1회의 beep음과 함께, 제세동기 화면에 심전도 그래프만 단독으로 출력된다. 출력되는 수치는 NSR(정상리듬)의 110bpm으로 한다. EventIdentifier로 defib_ui_nsr을 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D025_2 |
| D025_2 | Dialogue | [시스템, by 플레이어 C] "제세동기 준비가 완료되었습니다." | E041 |
| E041 | InvokeEvent | C015에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C015 |
| C015 | Choice | [플레이어 C 전용] "1. 제세동기는 "Sync" 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 그러나 응급 상황 시 버튼을 누르지 않고 제세동을 실시합니다. 다음의 심전도 중 "제세동"을 실시해야 하는 심전도는?" | C015-Wrong, C015-Correct |
| C015-Wrong | ChoiceOption | "Asystole(무수축)", "PEA(무맥성 전기활동)", "VT(맥박이 있는 심실빈맥)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D025_retry_a |
| C015-Correct | ChoiceOption | "VF(심실세동)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E042 |
| D025_retry_a | Dialogue | "오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 "VF(심실세동)" 입니다." | C005_1 |
| E042 | InvokeEvent | C016에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C016 |
| C016 | Choice | [플레이어 C 전용] "2. 현재 많은 병원에서 이상파형(Biphasic) 제세동기를 사용하고 있습니다. 단상파형(Monophasic)보다 더 효과적인 것으로 알려져 있습니다. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은?" | C016-Wrong, C016-Correct |
| C016-Wrong | ChoiceOption | "360J(줄)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D025_retry_b |
| C016-Correct | ChoiceOption |  "150~200J(줄)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E043 |
| D025_retry_b | Dialogue | "오답입니다. 이상파형(Biphasic) 제세동기는 낮은 에너지량으로도 효과적인 제세동이 가능하기 때문에, 150~200J(줄)이 정답입니다." | C016 |
| E043 | InvokeEvent | C005_3에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C017 |
| C017 | Choice | [플레이어 C 전용] "3. 제세동 등 전기충격 시 주의해야 할 사항은?" | C017-Wrong, C017-Correct |
| C017-Wrong | ChoiceOption | "꼬인 수액 줄을 풀어준다.", "의료진이 손을 대어도 괜찮다.", "의사의 지시가 있을 때에만 전기충격을 실시한다." (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D025_retry_c |
| C017-Correct | ChoiceOption | "전기충격 전 모두 환자에게서 떨어지도록 지시하고 확인한다." / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_C_defib |
| D025_retry_c | Dialogue | "오답입니다. 전기충격 전 감전되지 않도록 모두가 떨어지도록 지시하고 확인하는 절차가 필수입니다." | C017 |
| D026 | Dialogue | [플레이어 D 전용] "에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요." | V020 |
| V020 | Validator | [D 1단계] 에피네프린 앰퓰과 5cc 주사기를 각각 클릭해 획득한다. 각 아이템을 획득하면 에피네프린 앰퓰 아이템이 사라지고 5cc 주사기만 남게 되며, [해당 주사기는 "준비된 에피네프린 1mg"으로 아이템명이 변경된다.] Condition은 Prep_epi이며, TargetCount는 1이다. | E044 |
| E044 | InvokeEvent | 에피네프린 앰퓰과 5cc 주사기를 각각 클릭해 획득하면, 에피네프린 앰퓰 아이템은 사라지고, 5cc 주사기의 이름이 "준비된 에피네프린 1mg"으로 변경되어 출력된다. EventIdentifier로 prepped_epi를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D026_1 |
| D026_1 | Dialogue | "20cc 생리식염수와 20cc 주사기를 클릭해 push용 생리식염수를 준비하세요." | V020_1 |
| V020_1 | Validator | [D 2단계] 20cc 생리식염수와 20cc 주사기를 각각 클릭해 획득한다. 각 아이템을 획득하면 20cc 생리식염수 아이템이 사라지고 20cc 주사기만 인벤토리 내 남게 되며, [해당 주사기는 "준비된 생리식염수 20cc"로 아이템명이 변경된다.] EventIdentifier로 prepped_ns_20cc를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D026_2 |
| D026_2 | Dialogue | "준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요." | V020_2 |
| V020_2 | Validator | [D 3단계] 준비된 에피네프린 1mg 아이템을 클릭해 선택한 뒤, 중심정맥관을 클릭하면 [빈 루멘을 통해] 약물이 투여된다. Condition은 Push_epi이며, TargetCount는 1이다. | E045 |
| E045 | InvokeEvent | 주사기가 중심정맥관 [빈 루멘]에 연결되어 약물이 주입된다. EventIdentifier로 push_epi_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D026_3 |
| D026_3 | Dialogue | "동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다." | V020_3 |
| V020_3 | Validator | [D 4단계] 준비된 생리식염수 20cc 아이템을 클릭해 선택한 뒤, 중심정맥관을 클릭하면 [에피네프린이 투여되었던 동일한 루멘에] 약물이 투여된다. Condition은 Push_ns이며, TargetCount는 1이다. | E046 |
| E046 | InvokeEvent | 주사기가 중심정맥관의 [에피네프린이 투여되었던 동일한 루멘]에 연결되어 약물이 주입된다. EventIdentifier로 push_ns_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C018 |
| C018 | Choice | [플레이어 D 전용] "1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가?" | C018-Wrong, C018-Correct |
| C018-Wrong | ChoiceOption | "약 1~2분에 한 번", "약 5~10분에 한 번", "누군가 시킬 때 마다" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D026_retry_a |
| C018-Correct | ChoiceOption | "약 3~5분에 한 번" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E047 |
| D026_retry_a | Dialogue | "오답입니다. 에피네프린은 3~4분에 한 번 투여합니다." | C018 |
| E047 | InvokeEvent | C019에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C019 |
| C019 | Choice | [플레이어 D 전용] "2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는?" | C019-Wrong, C019-Correct |
| C019-Wrong | ChoiceOption | "약물 주입 후 생리식염수 주입", "약물만 주입" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D026_retry_b |
| C019-Correct | ChoiceOption | "약물 주입 후 생리식염수 주입, 이후 팔 들어올리기" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_D_epi |
| D026_retry_b | Dialogue | "오답입니다. 말초로 투여하는 경우, 심장에 빠르게 도달시키기 위해 약물 및 생리식염수 주입 후 팔을 들어올려주어야 합니다." | C019 |
| D027 | Dialogue | [시스템, by 의사 NPC] "2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요." | E048 |
| E048 | InvokeEvent | 불러졌던 Start_ambubagging, Start_chest_compression을 종료한다. EventIdentifier로 Stop_ambu_and_comp를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | E049 |
| E049 | InvokeEvent | [모든 플레이어 및 관전자]에게 무수축(Asystole) 심전도 그래프를 출력한다. [또한, 제세동기 화면과 환자 모니터 화면에도 동일하게 출력]한다. [심박수는 -?- 으로 출력]되도록 한다. EventIdentifier로 Asystole_monitorui를 호출한다. MovementNextBehavior는 WaitUntilDone으로 서술한다. | D028 |
| D028 | Dialogue | [시스템, by 의사 NPC] "Asystole입니다. 가슴압박과 앰부배깅 하시던 간호사 A, B 선생님끼리 교대 후 계속 가슴압박 해주세요. 간호사 C, D 선생님께서도 교대해서 역할을 수행해 주세요. | P007 |
| P007 | Parallel | 플레이어 A는 가슴압박, 앰부백을 이용한 산소화, 플레이어 B는 앰부백을 이용한 산소화, 플레이어 C는 에피네프린 투여, 플레이어 D는 제세동기 준비를 실시한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D033 |
| P007-B1 | ParallelBranch | 브랜치 시작 노드는 D029이며, 플레이어 A는 가슴 압박을 실시한다. CompletionConditionIdentifier는 CC_A_chestcomp로 서술한다. | D029 |
| P007-B2 | ParallelBranch | 브랜치 시작 노드는 D030이며, 플레이어 B는 앰부백을 이용한 산소화를 실시한다. CompletionConditionIdentifier는 CC_B_ambu로 서술한다. | D030 |
| P007-B3 | ParallelBranch | 브랜치 시작 노드는 D031이며, 플레이어 C는 에피네프린 투여를 실시한다(필요한 물품: 5cc 주사기, 20cc 주사기, 에피네프린, 20cc 생리식염수). CompletionConditionIdentifier는 CC_C_epi로 서술한다. | D031 |
| P007-B4 | ParallelBranch | 브랜치 시작 노드는 D032이며, 플레이어 D는 제세동기를 준비한다. CompletionConditionIdentifier는 CC_D_defib로 서술한다. | D032 |
| D029 | Dialogue | [플레이어 A 전용] "환자의 가슴을 클릭해 가슴압박을 시작하세요." | V021 |
| V021 | Validator | [A] 환자의 가슴을 클릭하여 가슴압박을 시작한다. Condition은 Click_chest이며, TargetCount는 1이다. | E050 |
| E050 | InvokeEvent | 플레이어 A의 가슴압박 애니메이션과 함께, 가슴압박 리듬에 맞춰 환자의 가슴이 들어가는 애니메이션을 시작한다. [해당 애니메이션들은 모든 플레이어들의 임무가 완료될 때 까지 지속한다.] EventIdentifier로 start_chest_compression을 호출한다. MoveNextBehavior는 Immediate로 서술한다. | E051 |
| E051 | InvokeEvent | C020에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C020 |
| C020 | Choice | [플레이어 A 전용] "1. 성인의 정확한 가슴 압박 깊이는?" | C020-Wrong, C020-Correct |
| C020-Wrong | ChoiceOption | "약 4cm", "약 6cm" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D029_retry_a |
| C020-Correct | ChoiceOption | "약 5cm" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E052 |
| D029_retry_a | Dialogue | "오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다." | C007_1 |
| E052 | InvokeEvent | C021에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C021 |
| C021 | Choice | [플레이어 A 전용] "2. 성인의 정확한 가슴 압박 위치는?" | C021-Wrong, C021-Correct |
| C021-Wrong | ChoiceOption | "양측 유두선상의 중간지점" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D029_retry_b |
| C021-Correct | ChoiceOption | "흉골 하부 1/2 지점" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E053 |
| D029_retry_b | Dialogue | "오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다." | C021 |
| E053 | InvokeEvent | C022에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C022 |
| C022 | Choice | [플레이어 A 전용] "3. 정확한 가슴 압박 횟수는?" | C022-Wrong, C022-Correct |
| C022-Wrong | ChoiceOption | "분당 약 80~100회", "분당 약 120~140회" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D029_retry_c |
| C022-Correct | ChoiceOption | "분당 약 100~120회" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E054 |
| D029_retry_c | Dialogue | "오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다." | C022 |
| E054 | InvokeEvent | C023에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 Delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C023 |
| C023 | Choice | [플레이어 A 전용] "4. 가슴압박 시 주의사항은?" | C023-Wrong, C023-Correct |
| C023-Wrong | ChoiceOption | "지쳐도 한 사람이 계속 가슴압박을 수행한다.", "뼈가 부러진 것 같으면 멈춘다." (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D029_retry_d |
| C023-Correct | ChoiceOption | "충분한 이완을 제공한다." (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_A_chestcomp |
| D029_retry_d | Dialogue | "오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 심장에 혈액이 들어와 내보낼 수 있게 됩니다." | C023 |
| D030 | Dialogue | [플레이어 B 전용] "앰부백을 클릭해 산소 공급을 시작하세요." | V022 |
| V022 | Validator | [B] 앰부백을 클릭하면 산소 공급이 시작된다. Condition은 Start_ambu이며, TargetCount는 1이다. | E055 |
| E055 | InvokeEvent | 플레이어 B의 앰부백을 짜는 애니메이션과 함께, 앰부백이 짜여지는 애니메이션을 시작한다. [해당 애니메이션들은 모든 플레이어들의 임무가 완료될 때 까지 지속한다.] EventIdentifier로 start_ambubagging을 호출한다. MoveNextBehavior는 Immediate로 서술한다. | E056 |
| E056 | InvokeEvent | C024에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C024 |
| C024 | Choice | [플레이어 B 전용] "1. 성인의 정확한 산소 제공량은?" | C024-Wrong, C024-Correct |
| C024-Wrong | ChoiceOption | "약 1500ml (다섯 손가락 모두를 이용해 백을 짠다)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D030_retry_a |
| C024-Correct | ChoiceOption | "약 600ml (다섯 손가락 모두를 이용해 백을 짠다)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E057 |
| D030_retry_a | Dialogue | "오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다." | C024 |
| E057 | InvokeEvent | C025에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 애니메이션이 재생되는 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C025 |
| C025 | Choice | [플레이어 B 전용] "2. 심폐소생술 중 적절한 속도는?" | C025-Wrong, C025-Correct |
| C025-Wrong | ChoiceOption | "10초에 1번 (분당 약 6회)", "3초에 1번 (분당 약 20회)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D030_retry_b |
| C025-Correct | ChoiceOption | "6초에 1번 (분당 약 10회)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_B_ambu |
| D030_retry_b | Dialogue | "오답입니다. 6초에 1번씩 눌러야 합니다." | C025 |
| D031 | Dialogue | [시스템, by 의사 NPC] "간호사 C 선생님, 바로 에피네프린 투여 준비하면 시간이 맞을 것 같습니다. 바로 투여 준비 해주세요." | D031_1 |
| D031_1 | Dialogue | [플레이어 C 전용] "에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요." | V023 |
| V023 | Validator | [C 1단계] 에피네프린 앰퓰과 5cc 주사기를 각각 클릭해 획득한다. 각 아이템을 획득하면 에피네프린 앰퓰 아이템이 사라지고 5cc 주사기만 남게 되며, [해당 주사기는 "준비된 에피네프린 1mg"으로 아이템명이 변경된다.] Condition은 Prep_epi이며, TargetCount는 1이다. | E058 |
| E058 | InvokeEvent | 에피네프린 앰퓰과 5cc 주사기를 각각 클릭해 획득하면, 에피네프린 앰퓰 아이템은 사라지고, 5cc 주사기의 이름이 "준비된 에피네프린 1mg"으로 변경되어 출력된다. EventIdentifier로 prepped_epi를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D031_2 |
| D031_2 | Dialogue | "20cc 생리식염수와 20cc 주사기를 클릭해 push용 생리식염수를 준비하세요." | V023_1 |
| V023_1 | Validator | [C 2단계] 20cc 생리식염수와 20cc 주사기를 각각 클릭해 획득한다. 각 아이템을 획득하면 20cc 생리식염수 아이템이 사라지고 20cc 주사기만 인벤토리 내 남게 되며, [해당 주사기는 "준비된 생리식염수 20cc"로 아이템명이 변경된다.] EventIdentifier로 prepped_ns_20cc를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D031_3 |
| D031_3 | Dialogue | "준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요." | V023_2 |
| V023_2 | Validator | [C 3단계] 준비된 에피네프린 1mg 아이템을 클릭해 선택한 뒤, 중심정맥관을 클릭하면 [빈 루멘을 통해] 약물이 투여된다. Condition은 Push_epi이며, TargetCount는 1이다. | E059 |
| E059 | InvokeEvent | 주사기가 중심정맥관 [빈 루멘]에 연결되어 약물이 주입된다. EventIdentifier로 push_epi_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D031_4 |
| D031_4 | Dialogue | "동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다." | V023_3 |
| V023_3 | Validator | [C 4단계] 준비된 생리식염수 20cc 아이템을 클릭해 선택한 뒤, 중심정맥관을 클릭하면 [에피네프린이 투여되었던 동일한 루멘에] 약물이 투여된다. Condition은 Push_ns이며, TargetCount는 1이다. | E060 |
| E060 | InvokeEvent | 주사기가 중심정맥관의 [에피네프린이 투여되었던 동일한 루멘]에 연결되어 약물이 주입된다. EventIdentifier로 push_ns_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C026 |
| C026 | Choice | [플레이어 C 전용] "1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가?" | C026-Wrong, C026-Correct |
| C026-Wrong | ChoiceOption | "1~2분에 한 번", "5~10분에 한 번", "누군가 시킬 때 마다" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D031_retry_a |
| C026-Correct | ChoiceOption | "3~5분에 한 번" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E061 |
| D031_retry_a | Dialogue | "오답입니다. 에피네프린은 3~4분에 한 번 투여합니다." | C026 |
| E061 | InvokeEvent | C027에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C027 |
| C027 | Choice | [플레이어 C 전용] "2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는?" | C009_2-Wrong, C009_2-Correct |
| C027-Wrong | ChoiceOption | "약물 주입 후 생리식염수 주입", "약물만 주입" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D031_retry_b |
| C027-Correct | ChoiceOption | "약물 주입 후 생리식염수 주입, 이후 팔 들어올리기" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_C_epi |
| D031_retry_b | Dialogue | "오답입니다. 말초로 투여하는 경우, 심장에 빠르게 도달시키기 위해 약물 및 생리식염수 주입 후 팔을 들어올려주어야 합니다." | C027 |
| D032 | Dialogue | [플레이어 D 전용] "제세동기를 클릭해 역할을 부여받으세요." | V024 |
| V024 | Validator | [플레이어 D] 제세동기를 클릭하면 역할을 부여받은 것으로 가정하고, 다음 단계로 진행한다. Condition은 Click_defib이며, TargetCount는 1이다. | E062 |
| E062 | InvokeEvent | 전원이 켜지는 1회의 beep음을 들려준다. EventIdentifier로 defib_sound_only를 호출한다. MoveNextBehavior는 Immediate로 서술한다. | D032_1 |
| D032_1 | Dialogue | [시스템, by 플레이어 D] "제세동기 준비가 완료되었습니다." | E063 |
| E063 | InvokeEvent | C028에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C028 |
| C028 | Choice | [플레이어 D 전용] "1. 제세동기는 "Sync" 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 그러나 응급 상황 시 버튼을 누르지 않고 제세동을 실시합니다. 다음의 심전도 중 "제세동"을 실시해야 하는 심전도는?" | C028-Wrong, C028-Correct |
| C028-Wrong | ChoiceOption | "Asystole(무수축)", "PEA(무맥성 전기활동)", "VT(맥박이 있는 심실빈맥)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D032_retry_a |
| C028-Correct | ChoiceOption | "VF(심실세동)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E064 |
| D032_retry_a | Dialogue | "오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 "VF(심실세동)" 입니다." | C028 |
| E064 | InvokeEvent | C029에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C029 |
| C029 | Choice | [플레이어 D 전용] "2. 현재 많은 병원에서 이상파형(Biphasic) 제세동기를 사용하고 있습니다. 단상파형(Monophasic)보다 더 효과적인 것으로 알려져 있습니다. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은?" | C029-Wrong, C029-Correct |
| C029-Wrong | ChoiceOption | "360J(줄)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D032_retry_b |
| C029-Correct | ChoiceOption |  "150~200J(줄)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | E065 |
| D032_retry_b | Dialogue | "오답입니다. 이상파형(Biphasic) 제세동기는 낮은 에너지량으로도 효과적인 제세동이 가능하기 때문에, 150~200J(줄)이 정답입니다." | C029 |
| E065 | InvokeEvent | C030에 해당하는 문제가 제시되기 전, [4초가 지난 뒤 제시]하여 시간을 확보한다. EventIdentifier로 delay_4sec를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | C030 |
| C030 | Choice | [플레이어 D 전용] "3. 제세동 등 전기충격 시 주의해야 할 사항은?" | C030-Wrong, C030-Correct |
| C030-Wrong | ChoiceOption | "꼬인 수액 줄을 풀어준다.", "의료진이 손을 대어도 괜찮다.", "의사의 지시가 있을 때에만 전기충격을 실시한다." (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D032_retry_c |
| C030-Correct | ChoiceOption | "전기충격 전 모두 환자에게서 떨어지도록 지시하고 확인한다." / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | CC_D_defib |
| D032_retry_c | Dialogue | "오답입니다. 전기충격 전 감전되지 않도록 모두가 떨어지도록 지시하고 확인하는 절차가 필수입니다." | C030 |
| D033 | Dialogue | [시스템, by 의사 NPC] "2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요." | E066 |
| E066 | InvokeEvent | 불러졌던 Start_ambubagging, Start_chest_compression을 종료한다. EventIdentifier로 Stop_ambu_and_comp를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | E067 |
| E067 | InvokeEvent | [모든 플레이어 및 관전자]에게 NSR(정상 리듬) 심전도 그래프를 출력한다. [또한, 제세동기 화면과 환자 모니터 화면에도 동일하게 출력]한다. [심박수는 110회/분 으로 출력]되도록 한다. [나머지 활력징후는 혈압 85/55mmHg, 호흡수 11회/분, SpO2 88%로 출력한다.] EventIdentifier로 ROSC_monitorui를 호출한다. MovementNextBehavior는 WaitUntilDone으로 서술한다. | D034 |
| D034 | Dialogue | [시스템, by 의사 NPC] "간호사 A 선생님, 맥박 있는지 목을 클릭해서 확인해주세요." | D035 |
| D035 | Dialogue | [간호사 A 전용] "환자의 목을 클릭해서 경동맥을 촉지합니다." | V025 |
| V025 | Validator | [간호사 A] 환자의 목 부위를 클릭해 맥박을 촉지한다. 촉지 부위는 C-line이 삽입된 반대편 경동맥을 촉지한다. Condition은 Check_puls이며, TargetCount는 1이다. | D036 |
| D036 | Dialogue | [시스템, by 간호사 A] "환자 맥박 느껴집니다." | D037 |
| D037 | Dialogue | [시스템, by 의사 NPC] "환자 ROSC 되었습니다. 제가 검사랑 협진 의뢰 할테니 간호사 D 선생님이 의식상태 확인해주세요." | D038 |
| D038 | Dialogue | [시스템, by 의사 NPC] "간호사 B 선생님, 의복 제거해서 추가 손상 있는지 사정해주세요." | D039 |
| D039 | Dialogue | [시스템, by 의사 NPC] "간호사 A 선생님께서는 다시 분류구역으로 이동해서 환자 분류헤주세요." | P008 |
| P008 | Parallel | 플레이어 A는 중증도 분류 구역으로 복귀, 플레이어 B는 가위를 이용한 의복 제거, 플레이어 D는 의식상태 확인을 수행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D043 |
| P008-B1 | ParallelBranch | 브랜치 시작 노드는 D040이며, 플레이어 A는 중증도 분류 구역으로 이동한다. CompletionConditionIdentifier는 CC_A_triagearea로 서술한다. | D040 |
| P008-B2 | ParallelBranch | 브랜치 시작 노드는 D041이며, 플레이어 B는 가위를 이용해 환자의 의복을 제거한다. CompletionConditionIdentifier는 CC_B_cut_patientA로 서술한다. | D041 |
| P008-B3 | ParallelBranch | 브랜치 시작 노드는 D042이며, 플레이어 D는 환자의 의식을 사정한다. CompletionConditionIdentifier는 CC_D_gcs_patientA_rosc로 서술한다. | D042 |
| D040 | Dialogue | [플레이어 A 전용] "중증도 분류 구역으로 이동하세요." | V026 |
| V026 | Validator | [플레이어 A] 중증도 분류 구역으로 이동하고, 게이트를 열고 중증도 분류 구역에 들어가면 도달한 것으로 간주한다. Condition은 Arrive_triagearea이며, TargetCount는 1이다. | E068 |
| E068 | InvokeEvent | 플레이어 A가 중증도 분류 구역으로 이동한다. | CC_A_triagearea |
| D041 | Dialogue | [플레이어 B 전용] "가위를 클릭해 획득하고, 환자를 클릭해 의복을 제거하세요." | V027 |
| V027 | Validator | [플레이어 B] 가위를 클릭해 획득하고, 해당 아이템을 선택한 상태로 환자를 클릭한다. Condition은 remove_clothings_a이며, TargetCount는 1이다. | E069 |
| E069 | InvokeEvent | 가위로 천을 자르는 소리를 [처치실 내에서만 들리도록] 출력한다. EventIdentifier로 cutting_sound를 호출한다. MoveNextBrhavior는 WaitUntilDone으로 서술한다. | D041_1 |
| D041_1 | Dialogue | [시스템, by 플레이어 B] "추가 외상은 확인되지 않습니다." | CC_B_cut_patientA |
| D042 | Dialogue | [플레이어 D 전용] "환자를 클릭해 환자의 의식 상태를 사정하십시오." | V028 |
| V028 | Validator | 플레이어 D는 환자를 클릭해 GCS를 사정한다. Condition은 Check_gcs_a_rosc이며, TargetCount는 1이다. | D042_1 |
| D042_1 | Dialogue | "환자의 의식 상태를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다." | D042_2 |
| D042_2 | Dialogue | "환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다." | C031 |
| C031 | Choice | "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?" | C031-Wrong, C031-Correct |
| C031-Wrong | ChoiceOption | "A(Alert, 완전히 깨어 있음)", "V(Verbal response, 음성에 반응 있음)", "U(Unconsciousness, 반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_retry_a |
| C031-Correct | ChoiceOption | "P(Pain response, 통증에 반응 있음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_3 |
| D042_retry_a | Dialogue | "오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다." | C031 |
| D042_3 | Dialogue | [관찰1] "추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다." | C032 |
| C032 | Choice | "관찰된 E(Eye Opening) 점수는 몇 점입니까?" | C032-Wrong, C032-Correct |
| C032-Wrong | ChoiceOption | "4점(자발적)", "3점(명령)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_retry_b |
| C032-Correct | ChoiceOption | "2점(통증)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_4 |
| D042_retry_b | Dialogue | "오답입니다. 통증 자극에만 반응했음을 유의하십시오." | C032 |
| D042_4 | Dialogue | [관찰2] "다음은 Verbal Response(V)입니다. "여기가 어디예요?"라고 묻자, 환자는 이해할 수 없는 신음소리만 내고 있습니다. 현재 기관내삽관이 시행되어있는 상태입니다." | C033 |
| C033 | Choice | "관찰된 V(Verbal Response) 점수는 몇 점입니까?" | C033-Wrong, C033-Correct |
| C033-Wrong | ChoiceOption | "5점(적절한 답변)", "4점(혼란)", "3점(부적절한 답변)", "2점(신음소리)", "1점(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_retry_c |
| C033-Correct | ChoiceOption | "E(기관삽관)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_5 |
| D042_retry_c | Dialogue | "오답입니다. 현재 환자는 알아들을 수 없는 소리만 내고 있으나, 기관삽관을 하는 경우 1점으로 처리합니다." | C033 |
| D042_5 | Dialogue | [관찰3] "마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 반대쪽 손으로 잡으려 합니다." | C034 |
| C034 | Choice | "관찰된 M(Motor Response) 점수는 몇 점입니까?" | C034-Wrong, C034-Correct |
| C034-Wrong | ChoiceOption | "6점(명령 수행)", "4점(통증에 회피)", "3점(이상 굴곡)", "2점(이상 신전)", "1(반응 없음)" (오답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_retry_d |
| C034-Correct | ChoiceOption | "5점(통증 원인을 치우려고 손을 뻗음)" (정답) / DisplayIconIdentifier 없음 / DisplayColor: #88AAFF | D042_6 |
| D042_retry_d | Dialogue | "오답입니다. 현재 통증에 회피하고 있습니다." | C034 |
| D042_6 | Dialogue | "GCS 측정 완료. E2 / V(E) / M5 = 총 7T점 입니다." | CC_D_gcs_patientA_rosc |
| D043 | Dialogue | [시스템] "시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다." 메세지를 표시한다. | (end) |



## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D034 |
| 종료 연출/설명 | ROSC 이후 신경학적 확인과 전신 노출을 마친 뒤, 검은 화면으로 fade out 되며 "시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다." 메세지를 표시하며 종료된다. 이후 다시 밝아지며 다음 시나리오로 이어진다. |
