---
title: "시나리오 이벤트 레지스트리"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 시나리오 이벤트 레지스트리

이 문서는 시나리오에서 호출되는 이벤트 식별자를 추적하기 위한 목록입니다. 새로운 InvokeEvent를 추가하는 경우 반드시 여기에 기록합니다.

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| (예) move_patient_a_to_treatment | 환자 A를 처치실로 이동 | 환자 A 시나리오 시작 | Assets/Modules/TriageTrainer/... | deprecated |

상태 값은 `planned`, `implemented`, `deprecated` 중 하나로 기록합니다.

## 태그 연계 규칙

- EventIdentifier를 추가/수정할 때, 해당 이벤트가 사용하는 시나리오의 그래프 상위 `tags` 선언을 함께 확인합니다.
- 병렬 분기 조건 변경이 필요한 이벤트는 문서에서 `RequiredPlayerTags`, `ForbiddenPlayerTags`, `RequiredPlayerTagsMatchMode`를 함께 갱신합니다.
- 플레이어 태그 변경 흐름은 시나리오 문서/JSON에서 `TagModification` 노드 표기를 우선 사용합니다. (하위 호환 `PlayerTag` 허용)



# 이벤트 본문

## 1. 재난 초기 대응 및 중증도 분류 (disaster_intro.md)

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| triage_patientA_dummyA | 환자 A와 더미 A가 중증도 분류 구역으로 이송되어 들어오는 연출. (각각의 캐릭터가 Stretcher에 누워있는 상태로 진입) | 시나리오 시작 후, 간호사 A가 준비를 마친 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_patientA_ui | 환자 A의 외견 및 상태 정보를 보여주는 UI 패널 활성화. [출력될 정보: - 현재 의식 상태: 대화가 불가능하고, 신음소리만을 내고 있음<br>- 흉부 관통상 및 흉부에서 다량의 출혈 관찰됨<br>- 빈맥<br>- 불규칙한 서호흡<br>- 피부는 창백하고 차가움<br>- C/C: 다량의 출혈] | 간호사 A가 중증도 분류를 위해 환자 A 클릭 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_dummyA_ui | 더미 A의 외견 및 상태 정보를 보여주는 UI 패널 활성화. [출력될 정보: - 현재 의식 상태: 원활한 대화 가능함<br>- 활력징후 정상<br>- 사지의 약간의 타박상<br>- C/C: 하지 통증] | 간호사 A가 중증도 분류를 위해 더미 A 클릭 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| [보류 가능: 현재 ValidatorNode로 중증도 분류 구역 진입 시 인식하는 것으로 작성함 -- 불필요하다면 삭제해도 되고, 혹은 PlayerMoveNode로 변경하겠습니다] B_C_D_to_triage | 간호사 B, C, D 캐릭터가 환자 이송을 돕기 위해 트리아지 구역으로 이동하는 연출. | 간호사 A의 분류가 끝나고 이송 요청 대사 출력 후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |

---

## 2. 환자 A 중증 처치 (patient_a_critical.md)

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| move_patientA_to_treatmentroom | 환자 A가 누운 환자 베드([처치실에 위치한 베드를 미리 적용되어 있는 상태로 시작])를 간호사 4명이 처치실로 이동시키는 연출. | 4명의 간호사가 베드 손잡이를 모두 잡은(Q006_1) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| activate_vital_monitor_ui_patientA | 활력징후를 측정하는 간호사 B 플레이어 화면에 환자 A의 활력징후 UI 출력 및 병실 내 모니터 오브젝트에도 활력징후 출력 시작. **[출력될 활력징후: BP[혈압] 70/40mmHg, HR[맥박] 140회/분 (심전도: 동성빈맥 (빠른 정상파형(NSR)), RR[호흡수] 8회/분, BT[체온] 35.9도, SpO2[산소포화도] 82% / 해당 수치들은 혈압의 경우 +-7, 맥박은 +-20, 호흡수는 +-2, 체온은 +-0.2 범위 내에서 무작위로 fluctuation이 발생하면 됩니다. 구현에 시간이 걸리는 경우 구현하지 않아도 괜찮습니다.]** | 간호사 B가 환자 A에게 활력징후 측정도구 적용(V011_1) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_suction_checklist_ui | 화면 측면에 흡인(suction) 준비물 4종 체크리스트 UI 표시. [표시될 항목: **경추고정기 / 흡인기 / 석션 라인 / 앙커 팁** - ValidatorNode의 TargetCount를 4로 해두었기 때문에, 각 물품을 한 개씩 획득하면 실시간으로 UI에 반영 가능하면 좋겠습니다] | 간호사 D가 구강 흡인 퀘스트(Q009) 수락 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| hide_suction_checklist_ui | 화면에 떠 있던 흡인 준비물 체크리스트 UI 숨김/종료. | 간호사 D가 앙커팁 조립(A003) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| vitalinfo_1_patientA | 시스템 메시지 창에 환자 A의 첫 활력징후 수치 강제 출력. [환자 모니터에 출력되는 그래프 및 수치가 보여지면 되겠습니다] | P003 브랜치 종료 및 합류 이후 첫 환자 상태 브리핑(D010) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_checklist_intu | 화면 측면에 기관내삽관 준비물 6종 체크리스트 UI 표시. [표시될 항목: 후두경 블레이드 / 후두경 손잡이 / 기관내관 / 스타일렛 / 플라스터 / 5cc 주사기]  | 간호사 B가 기관내삽관 퀘스트(Q010) 부여 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| hide_checklist_intu | 기관내삽관 준비물 체크리스트 UI 숨김 or 종료. | 간호사 B가 ET Tube 조립(A005) 완료 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| insert_et_tube | 환자의 구강 내로 기관내관 모델링이 삽입되려는 연출. [완성된 기관내관 오브젝트의 끝부분(동그란 팁 부분)이 환자의 입으로 들어간 것 처럼 좌표를 적용해 주시면 되겠습니다. J자 모양인데, 끝이 굽어있는 쪽이 하늘을 바라보는 방향이면 됩니다.] | 간호사 B가 의사에게 ET Tube 전달(V014_2) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| remove_stylet | 삽입된 기관내관에서 스타일렛 오브젝트가 제거되는 연출. [완성된 기관내관 아이템에서 기관내관 오브젝트로 변경시키는 것으로 스타일렛 오브젝트가 제거된 것으로 간주하고, 2/3만큼 입 안으로 들어간 것으로 좌표를 설정해주시면 되겠습니다.] | 간호사 B가 기관내관에서 스타일렛 제거 클릭(V014_3) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| connect_tpiece_ready | 환자에게 삽입된 기관내관에 T-piece를 연결한 관이 연결되는 연출. [긴 대롱에 연결되는 것이 아닌 짧은 어댑터에 기관내관이 연결되어야 합니다. 긴 대롱의 방향은 환자의 좌측 방향(환자를 마주보았을 때 오른쪽)에 위치할 수 있도록 가로로 연결합니다.] | 간호사 A가 산소 투여 준비를 마치고, 산소줄, T-piece를 각각 클릭하고 산소 유량계와 삽입된 기관내관을 클릭(V015_3) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| apply_gauze_patientA | 환자 A의 출혈 부위(흉부)에 거즈 오브젝트가 덮이는 연출(가능한 경우 혈흔 효과 포함 가능). | 간호사 C가 환자에게 거즈 적용(V016_2) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| apply_gauze_with_plaster_patientA | 환자 흉부에 덮인 거즈를 테이프(플라스터)가 부착된 오브젝트로 변경. | 간호사 C가 거즈에 플라스터 적용(V016_3) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_iv_checklist | 화면 측면에 IV 준비물 체크리스트 UI 표시. [표시될 항목: 18G 2개 / 생리식염수 1L 수액백 / 플라즈마 솔루션 1L 수액백 - ValidatorNode의 TargetCount를 4로 해두었기 때문에, 각 물품을 한 개씩 획득하면 실시간으로 UI에 반영 가능하면 좋겠습니다. 다만, 준비된 생리식염수 1L 수액백과 준비된 플라즈마 솔루션 1L 수액백은 인트로에서 이미 준비하여 인벤토리에 위치해 있으므로 획득된 것으로 간주합니다.]] | 간호사 D에게 IV 퀘스트(Q013) 부여 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| hide_iv_checklist | IV 준비물 체크리스트 UI 숨김 or 종료. | 간호사 D가 수액 및 카테터 모두 클릭 완료(V017) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| insert_18g_left | 환자의 좌측 팔에 18G 카테터가 삽입되는 연출. 18G 캐뉼라 오브젝트의 팁 부분([얇은 바늘같은 튜브가 팔의 오금(오목하게 들어가는 부분)에 들어가 있고, 나머지 플라스틱 부분은 바깥에 보일 수 있도록 좌표를 설정해 주시면 됩니다])이 삽입된다. | 간호사 D가 좌측 팔에 18G 적용(V017_1) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| connect_ns1_left | 삽입된 18G 카테터에 준비된 생리식염수 1L 수액백이 연결되는 연출. [생리식염수 1L 수액백은 수액걸대에 걸리도록 하고, 수액백과 캐뉼라 사이 연결은 단순 흰색 줄 혹은 투명 줄로 연결될 수 있도록 코딩으로 구현해야 합니다..] | 간호사 D가 좌측 팔에 삽입된 캐뉼라에 생리식염수 적용(V017_2) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| insert_18g_right | 환자의 우측 팔에 18G 카테터가 자동 삽입되는 연출. [인벤토리 내 18G 캐뉼라 아이템은 소비되어 사라진 것으로 연출] | 좌측 팔 수액 연결 완료 직후, 자동 연결 안내(N011_3) 이후 (자동) | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| connect_ps1_right | 우측 팔 18G 카테터에 플라즈마 솔루션 수액 라인이 자동 연결되는 연출. [인벤토리 내 준비된 플라즈마 솔루션 1L 수액백 아이템은 소비되어 사라진 것으로 연출] | 우측 팔 18G 삽입(E021) 직후 (자동) | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| insert_central_line_set | 의사 NPC가 환자에게 C-Line을 삽입하는 애니메이션/연출. [혹은 그냥 바로 목에 중심정맥관 오브젝트(세트 말고, 3갈래로 나뉘어진 튜브 오브젝트)가 바로 목에 위치할 수 있도록 합니다. 분지점이 목 바깥으로 보여야 하므로, 분지점 아래의 단일 관만이 환자의 목 우측(환자를 바라보았을 때, 왼쪽편)에 삽입된 것으로 좌표 잡아주시면 됩니다] | 간호사 C가 의사에게 C-line set 전달(V018) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| lv1_ready | Level 1 rapid infuser 장비에 플라즈마 솔루션 및 혈액백이 장착된 것으로 상태 변경. [오브젝트가 실제 삽입된 것처럼 구현하기 어려울 것으로 기대하므로, 마우스를 가져다 대면 오브젝트의 상태를 볼 수 있도록 해주시면 될 것 같습니다. 수액 연결때와 마찬가지로, 단순 관과 환자 목에 위치한 3갈래의 중심정맥관 중 좌측 및 우측에 연결된 것처럼 해주시되, 실제 기계와 연결된 것처럼 하실 필요는 없습니다. 침대 밑에서 튜브가 사라져도 괜찮을 듯 합니다.] | 간호사 D가 Level 1 기기에 수액/혈액 연결 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| patient_crash_ui | 환자 상태 악화 연출. ([심전도 모니터 알람음 발생] + **모니터 출력 화면 변화 및 모든 사람들에게 해당 화면을 출력: BP[혈압] -?- mmHg, HR[맥박] 80회/분 (심전도: 정상파형, NSR), RR[호흡수] -?- 회/분, BT[체온] 35.6도, SpO2[산소포화도] 0%(최초 수치에서 빠르게 끊기듯 감소하여 0에 도달하도록 하면 될 것 같습니다. (e.g. 80 -> 64 -> 49 -> 37 -> 19 -> 7 -> 0)**  | Level 1 연결 완료 직후 의사 대사(D022) 후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| Apply_ambu_patientA | 환자의 기관내관 상단에 앰부백 모델링(ambu_set)이 연결되는 연출. | 간호사 A가 기관내관에 T-piece 제거 후 앰부백 및 산소줄 연결(V023_2) 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| start_ambubagging | 간호사 아바타가 앰부백을 짜는 애니메이션 시작, 종료 시 까지 무한 반복 재생. | 간호사 A/B가 앰부배깅 시작 클릭 시 (1, 2차 사이클 모두 사용) | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| start_chest_compression | 간호사 아바타가 환자 위에서 가슴 압박을 실시하는 애니메이션 시작, 종료 시 까지 무한 반복 재생. | 간호사 B/A가 가슴 압박 시작 클릭 시 (1, 2차 사이클 모두 사용) | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| attach_defibpad | 환자의 맨가슴(우측 쇄골 아래, 좌측 유두 옆)에 제세동 패드가 부착되는 연출. [실제 에셋 구현이 어려워서 이 부분은 어떻게 해야 할지 고민입니다. 좋은 의견 있으시다면 알려주시면 감사하겠습니다] | 간호사 C가 패드를 환자 가슴에 적용 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| defib_ui_irregular | 제세동기 모니터 화면에 fluctuation이 심한 비정상 심전도 파형 출력. (기저선도 위아래로 움직이며 흔들리고, 정말 규칙적이지 않으면 됩니다. 혹은 구현이 어렵다면 대안으로 NSR(정상) 파형을 띄워도 됩니다.)| 간호사 C가 패드 부착을 완료한 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| stop_ambu_and_comp | 플레이어의 가슴 압박 및 앰부배깅 애니메이션 중지 및 환자에게서 떨어져서 서서 기다리는(대기) 연출. | 의사가 2분 경과 리듬 확인 지시 시점 (1, 2차 사이클 모두 사용) | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| asystole_monitor_ui | 환자 감시 모니터와 제세동기 화면에 Asystole(무수축) 파형(일직선) 출력. | 1차 사이클 종료 후 리듬 확인 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| ROSC_monitor_ui | 모니터에 QRS 파형이 정상적으로 나타나고 정상 활력징후 수치가 출력됨. **[출력될 활력징후: BP[혈압] 85/55mmHg, HR[맥박] 110회/분 (심전도: 정상파형, NSR), RR[호흡수] 11회/분, BT[체온] 36.2도, SpO2[산소포화도] 88% / 해당 수치들은 혈압의 경우 +-7, 맥박은 +-10, 호흡수는 +-2, 체온은 +-0.2 범위 내에서 무작위로 fluctuation이 발생하면 됩니다. 구현에 시간이 걸리는 경우 구현하지 않아도 괜찮습니다.]** | 2차 사이클 종료 후 리듬 확인 시, E035 노드 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| [보류 가능: ValidatorNode로 중증도 분류 구역 도달 시 다음으로 넘어가는 것으로 작성함] playerA_move_to_triage | 간호사 A 아바타가 트리아지 구역으로 이동. | ROSC 확인 후 간호사 A 트리아지 복귀 지시(V032) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |

---

## 3. 환자 B, C 지연 처치 (patient_b_c_ct.md)

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| triage_patientB_patientC_dummyB | 시나리오 B 환자, 더미 B, 시나리오 C 환자가 응급실 트리아지 구역으로 이송되어 들어오는 연출. | 시나리오 시작 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_patientB_ui | 환자 B의 외견 및 상태 정보를 보여주는 UI 패널 활성화. [출력될 정보: - 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임<br>- 왼쪽 팔과 다리의 근력이 비교적 약함<br>- 빈맥<br>- 빈호흡<br>- 상완 부위 출혈 지속 중<br>- 머리에 타박상 및 약간의 출혈 보임<br>- C/C: 두통] | 간호사 A가 환자 B 클릭(V035) 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_dummyB_ui | 더미 B의 외견 및 상태 정보를 보여주는 UI 패널 활성화. [출력될 정보: - 현재 의식 상태: 원활한 대화 가능함<br>- 활력징후 정상<br>- 사지에 약간의 타박상<br>- C/C: 어깨 통증] | 간호사 A가 더미 B 클릭(V036) 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| show_patient_c_ui | 환자 C의 외견 및 상태 정보를 보여주는 UI 패널 활성화. [출력될 정보: - 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임<br>- 한쪽 팔 근력이 비교적 약함<br>- 빈맥<br>- 빈호흡<br>- 무릎 하단 부위 출혈 지속 중<br>- 머리에 타박상 및 약간의 출혈 보임<br>- C/C: 어지러움] | 간호사 A가 환자 C 클릭(V037) 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| [보류 가능: ValidatorNode로 간호사 B/C/D 셋이 중증도 분류 구역 도달 시 다음으로 넘어가는 것으로 작성함] B_C_D_to_triage | 간호사 3명(B,C,D)이 트리아지 구역으로 이동해오는 연출 (환자 이송 준비).  | 간호사 A가 분류를 마친(Q032_1) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| move_patientB | 환자 B가 누운 스트레쳐가 입원실 구역으로 이동. [세 개 구역 중 아무 곳이나 상관없으나, 가능하면 동선의 단축을 위해 간호사 구역에 가장 가까운 침대로 배정] | 간호사 A, C가 환자 B 스트레쳐를 잡은(V040_C) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| activate_vital_monitor_ui_patientB | 플레이어 화면에 환자 B 활력징후 UI 출력 및 모니터 수치 표시. **[출력될 활력징후: BP[혈압] 140/86mmHg, HR[맥박] 120회/분 (심전도: 동성빈맥 (빠른 정상파형(NSR)), RR[호흡수] 24회/분, BT[체온] 37.3도, SpO2[산소포화도] 93% / 해당 수치들은 위와 동일한 범위 내에서 무작위로 fluctuation이 발생하면 됩니다. 구현에 시간이 걸리는 경우 구현하지 않아도 괜찮습니다.]** | 간호사 C가 환자 B 활력징후 측정(V045) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| pupil_reflex_patientB | **[코딩 구현 필요]** [환자 B 얼굴에 펜라이트를 비췄을 때 우측 동공이 고정되어 있는 연출 (UI 또는 애니메이션) - 좌측 동공은 빛이 비춰지면 동공이 수축하지만, 우측 동공은 거의 수축하지 않는 것(혹은 고정)으로 합니다.] | 간호사 A가 환자 B 얼굴에 펜라이트 적용(V048) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| insert_20g_right_patientB | 환자 B 우측 팔에 20G 카테터 삽입 연출. [앞선 시나리오 A 환자와 동일한 조건으로 20G 캐뉼라가 삽입되어야 합니다(얇은 바늘같은 팁만 들어가고, 나머지 부분은 팔 밖으로 노출).] | 간호사 A가 환자 B 우측 팔에 20G 적용(V050) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| connect_ns1_right_patientB | 환자 B 우측 팔 20G 카테터에 생리식염수 라인 연결 연출. [생리식염수 1L 수액백은 수액걸대에 걸리도록 하고, 수액백과 캐뉼라 사이 연결은 단순 흰색 줄 혹은 투명 줄로 연결될 수 있도록 코딩으로 구현해야 합니다..] | 간호사 A가 환자 B 우측 팔에 NS 연결(V051) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| apply_gauze_patientB | 환자 B 상완 출혈 부위에 거즈 오브젝트가 덮이는 연출. | 간호사 C가 환자 B에게 거즈 적용(V058) 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| apply_gauze_with_plaster_patientB | 환자 B 상완 출혈부위에 덮인 거즈를 테이프(플라스터)가 부착된 오브젝트로 변경. | 간호사 C가 환자 B 거즈에 플라스터 적용(V059) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| move_patientC | 환자 C가 누운 스트레쳐가 처치 구역으로 이동. [세 개 구역 중 아무 곳이나 상관없으나, 시나리오 B 환자와 겹치지 않는 구역 및 동선의 단축을 위해 간호사 구역에 가장 가까운 침대로 배정] | 간호사 B, D가 환자 C 스트레쳐를 잡은(V040_D) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| activate_vital_monitor_ui_patientC | 플레이어 화면에 환자 C 활력징후 UI 출력 및 모니터 수치 표시. **[출력될 활력징후: BP[혈압] 140/86mmHg, HR[맥박] 120회/분 (심전도: 동성빈맥 (빠른 정상파형(NSR)), RR[호흡수] 24회/분, BT[체온] 37.3도, SpO2[산소포화도] 93% / 해당 수치들은 위와 동일한 범위 내에서 무작위로 fluctuation이 발생하면 됩니다. 구현에 시간이 걸리는 경우 구현하지 않아도 괜찮습니다.]** | 간호사 D가 환자 C 활력징후 측정(V064) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| pupil_reflex_patientC | **[코딩 구현 필요]** [환자 B 얼굴에 펜라이트를 비췄을 때 우측 동공이 고정되어 있는 연출 (UI 또는 애니메이션) - 우측 동공은 빛이 비춰지면 동공이 수축하지만, 좌측 동공은 거의 수축하지 않는 것(혹은 고정)으로 합니다.] | 간호사 B가 환자 C 얼굴에 펜라이트 적용(V067) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| insert_20g_left_patientC | 환자 C 좌측 팔에 20G 카테터 삽입 연출. [앞선 시나리오 A 환자와 동일한 조건으로 20G 캐뉼라가 삽입되어야 합니다(얇은 바늘같은 팁만 들어가고, 나머지 부분은 팔 밖으로 노출).] | 간호사 B가 환자 C 좌측 팔에 20G 적용(V069) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| connect_ns1_left_patientC | 환자 C 좌측 팔 20G 카테터에 생리식염수 라인 연결 연출. [생리식염수 1L 수액백은 수액걸대에 걸리도록 하고, 수액백과 캐뉼라 사이 연결은 단순 흰색 줄 혹은 투명 줄로 연결될 수 있도록 코딩으로 구현해야 합니다..] | 간호사 B가 환자 C 좌측 팔에 NS 연결(V070) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| apply_gauze_patientC | 환자 C 무릎 하단 출혈 부위에 거즈 오브젝트가 덮이는 연출. | 간호사 D가 환자 C에게 거즈 적용(V077) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| apply_gauze_with_plaster_patientC | 환자 C 무릎 거즈 위에 플라스터 테이핑 처리 연출. | 간호사 D가 환자 C 거즈에 플라스터 적용(V078) 시 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
| move_patients_to_CT | 처치를 마친 환자 B와 C가 침대째로 CT실로 이동하는 최종 연출 (두 환자 모두 도착화면 화면 페이드 아웃되며 다음 노드로 진행). | 의사 NPC의 CT실 이송 지시 대사(D058) 직후 | Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs | implemented |
