---
title: "scenario 환자 A 중증 처치"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-08-19
flags: ["refactor-required"]
---

# scenario 환자 A 중증 처치

> 이 문서는 기존 `patient_a_critical.scenario.json` 또는 과거 변환 산출물을 복사/부분 재사용하지 않고, 본문에 명시된 명세(노드 연결, Validator 신호, 이벤트 핸들러, 퀘스트 정의, 기술 노트, 코멘트)만으로 실행 가능한 시나리오를 재구성하는 것을 목표로 한다.

## 줄글 시나리오

재난 현장에서 흉부 관통상을 입은 환자 A가 이송되어 온다. 환자는 심각한 출혈로 의식이 흐려진 상태다. 간호사 네 명과 의사 NPC 한 명이 대응에 투입된다. 시나리오 시작 시 다음과 같이 처리한다.
- 기술 노트
  1. 환자 A를 스폰한다.
    - 노드 식별자: `SPAWN_A`, 프리셋 `patient_a`, 스폰된 엔티티 식별자 `patient_a`
  2. 의사 NPC도 스폰한다.
    - 노드 식별자: `SPAWN_DOCTOR`, NPC 식별자 `npc-doctor-patient-a-critical`, `npc_doctor_preset` 사용
    - 의사 NPC는 이후 물품 제출 상호작용 네 종류(후두경, 기관내관, 5cc 주사기, C-line set)를 자신의 위치에서 제공한다.
  3. 환자 A의 의료 상태를 사전설정한다. (`PRESET_A`)
    - 남성 35세, GCS 8점(Stupor), 동공 반응 정상, 호흡 8회/분 불규칙, 맥박 140회/분 약함, 혈압 70/40mmHg, 피부 창백하고 차가움, 체온 35.9도, SpO2 82%, 심정지 아님
  4. 안내 재생
    - Dialogue
      - Speaker: "시스템"
      - Content: "환자 A를 처치실로 이동해야 합니다. 플레이어 A, B, C, D는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오."
      - TTS: true

환자 이동시키기
- 전체 인물을 상대로 퀘스트 발행
  - 제목: "들것 이동" (`Quest_Grab_Stretcher`)
  - 목표
    - 표기: "환자 침대의 손잡이 네 개를 모두 잡기"
    - 처리: 네 인물이 각자 침대 손잡이를 잡으면 완료로 처리한다
      - 기술 노트: 역할별 병렬(P002)로 네 개의 잡기 신호 `sig.grab_stretcher_patient_a_handle_0`~`sig.grab_stretcher_patient_a_handle_3`를 각각 대기한다.
- 퀘스트 완료 시 처치실 이동 연출 이벤트(`move_patient_a_to_treatmentroom`)를 재생하고 초기 평가 단계로 넘어간다.

### 초기 평가: 활력징후, 의식 상태, 경추 고정·흡인

안내 대사 "활력징후 측정, AVPU 및 GCS 측정, 경추 고정 및 흡인을 시작합니다."와 함께 세 가지 평가가 동시에 진행된다(P003).

- 간호사 B에게 퀘스트 발행
  - 제목: "활력징후 측정" (`Quest_Check_Vital_PatientA`)
  - 목표
    - 표기: "활력징후 측정도구를 획득해 환자의 활력징후를 측정하기"
    - 처리: 측정 완료 신호(`sig.show_vital_patient_a`)가 올라오면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "환자의 활력징후를 측정합니다. 활력징후 측정도구를 클릭해 획득하세요."
      - TTS: true
    2. 측정도구 획득을 기다린다(`sig.click_vital_set`).
    3. Dialogue
      - Speaker: "시스템"
      - Content: "활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. 활력징후가 모니터에도 출력됩니다."
      - TTS: true
    4. 측정이 끝나면 모니터 UI를 활성화한다(`activate_vital_monitor_ui_patient_a`).
    5. Dialogue
      - Speaker: "간호사 B"
      - Content: "환자 활력징후 출력됩니다."
      - TTS: true
    6. Dialogue
      - Speaker: "시스템"
      - Content: "혈압 70/40mmHg, 맥박 140회/분 - 약하고 빠름, 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다."
      - TTS: true
- 간호사 C에게도 퀘스트 발행
  - 제목: "의식 상태 사정" (`Quest_Check_GCS_PatientA`)
  - 목표
    - 표기: "환자의 의식 상태를 사정하고 AVPU와 GCS를 평가하기"
    - 처리: 사정 신호(`sig.check_avpu_gcs_patient_a`) 이후 아래 문항 흐름을 전부 통과하면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "환자를 클릭해 환자의 의식 상태를 사정하십시오."
      - TTS: true
    2. Dialogue
      - Speaker: "시스템"
      - Content: "환자의 의식 상태(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다."
      - TTS: true
    3. Dialogue
      - Speaker: "시스템"
      - Content: "[관찰] 환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다."
      - TTS: true
    4. ChoiceDialogue
      - Speaker: "시스템"
      - Content: "의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까?"
      - Choices
        - "A(Alert, 완전히 깨어 있음)": 오답 노드로 진행
        - "V(Verbal response, 음성에 반응 있음)": 오답 노드로 진행
        - "P(Pain response, 통증에 반응 있음)": 정답 노드로 진행
        - "U(Unconsciousness, 반응 없음)": 오답 노드로 진행
      - 오답 노드: "오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다."를 보여준 뒤 같은 질문으로 돌아간다.
    5. Dialogue
      - Speaker: "시스템"
      - Content: "[관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다."
      - TTS: true
    6. ChoiceDialogue
      - Speaker: "시스템"
      - Content: "관찰된 E(Eye Opening) 점수는 몇 점입니까?"
      - Choices: 4점/3점/1점은 오답, 2점(통증에 반응)이 정답
      - 오답 노드: "오답입니다. 통증 자극에만 반응했음을 유의하세요." 후 재시도
    7. Dialogue
      - Speaker: "시스템"
      - Content: "[관찰] 다음은 Verbal Response(V)입니다. \"여기가 어디예요?\"라고 묻자, 환자는 이해할 수 없는 신음소리만 내고 있습니다."
      - TTS: true
    8. ChoiceDialogue
      - Speaker: "시스템"
      - Content: "관찰된 V(Verbal Response) 점수는 몇 점입니까?"
      - Choices: 5점/4점/3점/1점은 오답, 2점(신음소리)이 정답
      - 오답 노드: "오답입니다. 현재 환자는 알아들을 수 없는 소리만 내고 있습니다." 후 재응시
    9. Dialogue
      - Speaker: "시스템"
      - Content: "[관찰] 마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 팔을 재빨리 굽혀 자극을 피합니다."
      - TTS: true
    10. ChoiceDialogue
      - Speaker: "시스템"
      - Content: "관찰된 M(Motor Response) 점수는 몇 점입니까?"
      - Choices: 6점/5점/3점/2점/1점은 오답, 4점(통증에 회피)이 정답
      - 오답 노드: "오답입니다. 현재 통증에 회피하고 있습니다." 후 재시도
    11. Dialogue
      - Speaker: "시스템"
      - Content: "GCS 측정 완료. E2 / V2 / M4 = 총 8점 (Stupor) 입니다."
      - TTS: true
    12. Dialogue
      - Speaker: "간호사 C"
      - Content: "AVPU 중 P이며, 추가 사정한 GCS 결과 8점 확인했습니다."
      - TTS: true
- 간호사 D에게는 퀘스트 발행
  - 제목: "경추 고정 및 구강 흡인" (`Quest_Stabilizer_And_Suction_PatientA`)
  - 목표
    - 표기: "경추를 고정하고 구강 흡인을 완료하기"
    - 처리: 아래 단계를 순서대로 마치면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "기도 확보를 위해 환자의 경추를 고정하고 구강 석션을 진행합니다. 경추고정기, 흡인기, 석션 라인, 앙카우어 팁을 클릭해 획득하세요."
      - TTS: true
    2. 흡인 체크리스트 UI를 띄우고(`show_suction_checklist_ui`) 경추고정기 적용과 세 물품(흡인기, 석션 라인, 앙카우어 팁) 획득을 기다린다(`sig.apply_stabilizer_patient_a`, `sig.click_wall_suction`, `sig.click_suction_line`, `sig.click_yankauer`). 이후 UI를 내린다(`hide_suction_checklist_ui`).
    3. Dialogue
      - Speaker: "시스템"
      - Content: "경추 고정기를 환자에게 적용하십시오."
      - TTS: true
      - 처리: 적용 신호(`sig.apply_stabilizer_patient_a`) 대기
    4. Dialogue
      - Speaker: "시스템"
      - Content: "흡인기를 벽에 설치하십시오."
      - TTS: true
      - 처리: 설치 신호(`sig.connect_wall_component_1`) 대기
    5. Dialogue
      - Speaker: "시스템"
      - Content: "준비된 앙카우어 팁을 흡인기에 연결하십시오."
      - TTS: true
      - 처리: 연결 신호(`sig.connect_wall_component_and_yankauer`) 대기
    6. Dialogue
      - Speaker: "시스템"
      - Content: "흡인기를 클릭한 뒤 환자를 클릭해 구강 흡인을 진행하십시오."
      - TTS: true
      - 처리: 흡인 신호(`sig.suction_patient_a`) 대기
    7. Dialogue
      - Speaker: "간호사 D"
      - Content: "경추 고정 및 구강 흡인 완료했습니다."
      - TTS: true

세 브랜치가 모두 마무리되면 평가 결과를 종합해 보고한다.
- Dialogue
  - Speaker: "시스템"
  - Content: "환자의 의식상태는 GCS 8점, 활력징후는 혈압 70/40mmHg, 맥박수 140회/분 (빠르고 약함), 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다."
  - TTS: true
- 그 뒤 활력 정보 연출 이벤트(`vitalinfo_1_patient_a`)를 재생한다.

### 의사 지시와 역할별 처치

의사 NPC가 역할별로 지시를 내린다.
1. Dialogue
  - Speaker: "의사 NPC"
  - Content: "기도 확보를 위해 intubation을 시행하겠습니다. 간호사 B 선생님은 보조해주세요."
  - TTS: true
2. Dialogue
  - Speaker: "의사 NPC"
  - Content: "그동안 간호사 C 선생님은 멸균장갑을 착용하고 거즈로 출혈부위를 지혈해주세요."
  - TTS: true
3. Dialogue
  - Speaker: "의사 NPC"
  - Content: "간호사 D 선생님은 수액 투여를 위해 양팔에 IV 라인을 확보해주세요. 혈관을 보고 18게이지로 잡고, 수액은 생리식염수와 플라즈마 솔루션을 연결하겠습니다."
  - TTS: true

이 지시를 받아 세 브랜치가 동시에 진행된다(P004).

- 간호사 B(미배정 시 간호사 A)에게 퀘스트 발행 — 기관내삽관과 산소 공급
  - 제목: "기관내삽관" (`Quest_Intubation_PatientA`)
  - 목표
    - 표기: "삽관 물품을 준비하고 의사의 삽관을 보조하기"
    - 처리: 아래 단계를 순서대로 마치면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "기관내삽관에 필요한 물품을 준비합니다. 좌측 체크리스트 창을 참고하여 필요한 물품을 클릭해 획득하세요."
      - TTS: true
    2. 삽관 체크리스트 UI를 띄운 뒤(`show_checklist_intu`) 여섯 물품 획득을 기다린다. 후두경 블레이드, 후두경 핸들, 기관내관, 스타일렛, 플라스터, 5cc 주사기(`sig.click_laryngoscope_blade`, `sig.click_laryngoscope_handle`, `sig.click_endotracheal_tube`, `sig.click_stylet`, `sig.click_plaster`, `sig.click_syringe_5cc`). 이후 UI를 내린다(`hide_checklist_intu`).
      - 기술 노트: 후두경과 기관내관(스타일렛 삽입 완료 상태)은 조합 산출물(`laryngoscope`, `endotracheal_tube_ready`)이다. 조합은 노드가 아니라 crafting 시스템으로 처리한다.
    3. Dialogue
      - Speaker: "시스템"
      - Content: "완성된 후두경을 의사에게 전달하세요."
      - TTS: true
      - 처리: 의사 NPC의 제출 상호작용(`patient-a-doctor-submit-laryngoscope`)에 후두경 1개를 제출하면 완료(`sig.pass_laryngoscope`)
    4. Dialogue
      - Speaker: "시스템"
      - Content: "완성된 기관내관을 의사에게 전달하세요."
      - TTS: true
      - 처리: 제출 상호작용(`patient-a-doctor-submit-et-tube`)에 기관내관 1개를 제출하면 완료(`sig.pass_et_tube_ready`)
    5. 삽관 연출(`insert_et_tube`) 후 Dialogue
      - Speaker: "시스템"
      - Content: "환자 구강에 삽입된 기관내관을 클릭해 스타일렛을 제거하세요."
      - TTS: true
      - 처리: 스타일렛 제거(`sig.remove_intu_stylet`)에 이어 제거 연출(`remove_stylet`)
    6. Dialogue
      - Speaker: "시스템"
      - Content: "5cc 주사기를 의사에게 전달하세요."
      - TTS: true
      - 처리: 제출 상호작용(`patient-a-doctor-submit-5cc-syringe`)에 5cc 주사기 1개를 제출하면 완료(`sig.pass_syringe`)
    7. Dialogue
      - Speaker: "시스템"
      - Content: "플라스터를 클릭해 선택한 뒤, 삽입된 기관내관을 고정하십시오."
      - TTS: true
      - 처리: 고정 신호(`sig.apply_plaster_on_intu`), 이후 테이프 소리(`tape_sound`) 재생
    8. Dialogue
      - Speaker: "간호사 B"
      - Content: "삽입된 깊이 23cm, 기관내관 고정되었습니다."
      - TTS: true
    9. Dialogue
      - Speaker: "의사 NPC"
      - Content: "삽관이 끝났고, 자발호흡이 있으니 간호사 A 선생님이 T-piece를 연결하고 산소 10L를 공급하며 산소포화도를 모니터링해주세요."
      - TTS: true
    - 이어서 산소 공급 서브 흐름, 퀘스트 "산소 공급" (`Quest_Oxygen_PatientA`) 발행
      1. Dialogue
        - Speaker: "시스템"
        - Content: "산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오."
        - TTS: true
        - 처리: 획득 신호(`sig.click_humidifier_bottle`, `sig.click_sterile_distilled_water`)
      2. Dialogue
        - Speaker: "시스템"
        - Content: "유량계를 습득하여 산소 유량계를 완성합니다."
        - TTS: true
        - 처리: 획득 신호(`sig.click_flowmeter`)
        - 기술 노트: 습윤병+멸균증류수, 그리고 유량계 조합(`oxyflowmeter`)도 crafting 시스템으로 처리한다.
      3. Dialogue
        - Speaker: "시스템"
        - Content: "완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오."
        - TTS: true
        - 처리: 설치 신호(`sig.connect_wall_component_2`)
      4. Dialogue
        - Speaker: "시스템"
        - Content: "산소줄과 T-Piece를 획득하세요."
        - TTS: true
        - 처리: 획득 신호(`sig.click_o2_line`)
      5. Dialogue
        - Speaker: "시스템"
        - Content: "환자에게 삽입된 기관내관을 클릭해 T-piece를 장착하세요."
        - TTS: true
        - 처리: 장착 신호(`sig.interact_tpiece`), 완료 시 T-piece 시각 오브젝트 활성화
      6. Dialogue
        - Speaker: "시스템"
        - Content: "장착된 T-piece와 벽면 유량계를 각각 클릭해 라인을 연결하세요."
        - TTS: true
        - 처리: 연결 신호(`sig.connect_tpiece_and_oxyflow`), 뒤이어 연결 연출(`connect_tpiece_ready`)
      7. Dialogue
        - Speaker: "시스템"
        - Content: "산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다."
        - TTS: true
        - 처리: 설치된 유량계 클릭 신호(`sig.interact_oxyflow_wall`) 대기
      8. ChoiceDialogue
        - Speaker: "시스템"
        - Content: "투여될 산소의 양을 조절합니다."
        - Choices: 3L/5L/15L은 오답, 10L이 정답
        - 오답 노드: "오답입니다. 처방은 10L 입니다." 후 재시도
      9. Dialogue
        - Speaker: "간호사 A"
        - Content: "산소 투여 시작했습니다."
        - TTS: true
- 간호사 C에게 지혈 퀘스트를 발행
  - 제목: "지혈" (`Quest_BleedingControl_PatientA`)
  - 목표
    - 표기: "출혈 부위를 지혈하기"
    - 처리: 아래 단계를 차례로 수행하면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오."
      - TTS: true
      - 처리: 획득 신호(`sig.click_sterile_gloves`, `sig.click_gauze`, `sig.click_plaster`)
    2. Dialogue
      - Speaker: "시스템"
      - Content: "멸균장갑을 착용하십시오. E키를 눌러 인벤토리 창을 열고, 좌측 상단의 착용 칸으로 멸균장갑 아이템을 옮기십시오."
      - TTS: true
      - 처리: 착용 신호(`sig.wear_glove`)
    3. Dialogue
      - Speaker: "시스템"
      - Content: "거즈를 선택한 뒤, 환자에게 적용하십시오."
      - TTS: true
      - 처리: 적용 신호(`sig.apply_gauze`) 후 거즈 적용 연출(`apply_gauze_patient_a`)
    4. Dialogue
      - Speaker: "시스템"
      - Content: "압박을 가해 지혈하고 있습니다. 플라스터를 선택한 뒤, 거즈를 고정하십시오."
      - TTS: true
      - 처리: 고정 신호(`sig.apply_plaster_on_gauze`), 이후 고정 연출(`apply_gauze_with_plaster_patient_a`)과 테이프 소리(`tape_sound`)
    5. Dialogue
      - Speaker: "간호사 C"
      - Content: "지혈 중입니다. 거즈 고정했습니다."
      - TTS: true
- 정맥로 확보와 수액 준비 — 간호사 D(미배정 시 간호사 C)에게 퀘스트 발행
  - 제목: "IV 라인" (`Quest_IV_Line_PatientA`)
  - 목표
    - 표기: "양팔에 정맥로를 확보하고 수액을 연결하기"
    - 처리: 아래 단계를 순서대로 진행하면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "환자의 양쪽 팔에 IV 라인을 순서대로 확보합니다. 먼저 18G 캐뉼라 1개와 준비된 생리식염수 1L, 플라즈마 솔루션 1L 수액백을 획득하십시오."
      - TTS: true
    2. IV 체크리스트 UI를 띄우고(`show_iv_checklist`) 획득 신호를 기다린다(`sig.click_cannula_18g`, `sig.click_normal_saline_1000ml`, `sig.click_plasma_solution_1000ml`). 그다음 UI를 내린다(`hide_iv_checklist`).
      - 기술 노트: 18G 캐뉼라는 준비 단계에서 1개만 확인한다. 첫 번째는 좌측 삽입 시 소비하고 두 번째는 좌측 완료 후 다시 획득해 우측에 사용한다.
    3. Dialogue
      - Speaker: "시스템"
      - Content: "18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요."
      - TTS: true
      - 처리: 좌측 삽입 신호(`sig.insert_iv_patient_a_left`), 이후 삽입 연출(`insert_18g_left`)
    4. Dialogue
      - Speaker: "시스템"
      - Content: "준비된 생리식염수 1L 수액백을 먼저 수액 걸대에 건 뒤, 수액줄을 좌측 팔의 18G 캐뉼라에 연결하세요."
      - TTS: true
      - 처리: 연결 신호(`sig.connect_cannula_and_ns1`)에 이어 연결 연출(`connect_ns1_left`)
    5. Dialogue
      - Speaker: "시스템"
      - Content: "좌측 팔의 정맥로가 확보되었습니다. 두 번째 18G 캐뉼라를 획득해 반대쪽 팔에 삽입하고, 플라즈마 솔루션 수액백을 먼저 건 뒤 연결하십시오."
      - TTS: true
      - 처리: 우측 삽입 신호(`sig.insert_iv_patient_a_right`), 그 뒤 삽입·연결 연출(`insert_18g_right`, `connect_ps1_right`)
    6. Dialogue
      - Speaker: "간호사 D"
      - Content: "양측 정맥로가 모두 확보되었습니다."
      - TTS: true
    - 같은 브랜치 안에서 C-line 보조와 Level 1 연결 서브 흐름이 이어진다.
      1. Dialogue
        - Speaker: "의사 NPC"
        - Content: "그래도 혈압이 잡히지 않네요. C-line 잡아서 수액을 빠르게 투여하겠습니다. 간호사 C 선생님, C-line set 건네주세요."
        - TTS: true
      2. Dialogue
        - Speaker: "시스템"
        - Content: "C-line set을 클릭해 획득하고, 해당 아이템을 의사에게 전달하세요."
        - TTS: true
        - 퀘스트 "C-line 보조" (`Quest_Cline_Assist`) 발행
        - 처리: 제출 상호작용(`patient-a-doctor-submit-central-line-set`)에 C-line set 1개를 제출하면 완료(`sig.pass_central_line_set`), 뒤이어 삽입 연출(`insert_central_line_set`)
      3. Dialogue
        - Speaker: "의사 NPC"
        - Content: "간호사 C 선생님, Level 1 rapid infuser에 플라즈마 솔루션과 혈액백 연결시켜주세요."
        - TTS: true
      4. Dialogue
        - Speaker: "시스템"
        - Content: "플라즈마 솔루션 1L 수액백과 혈액백을 클릭해 획득하세요."
        - TTS: true
        - 퀘스트 "Level 1 수액" (`Quest_Lv1_Fluids`) 발행
        - 처리: 획득 신호(`sig.click_plasma_solution_1000ml`, `sig.click_blood_transfusion_set`)
      5. Dialogue
        - Speaker: "시스템"
        - Content: "플라즈마 솔루션 1L 수액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요."
        - TTS: true
        - 처리: 연결 신호(`sig.connect_ps1_to_lv1`)
      6. Dialogue
        - Speaker: "시스템"
        - Content: "혈액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요."
        - TTS: true
        - 처리: 연결 신호(`sig.connect_blood_to_lv1`) 수신, 준비 연출(`lv1_ready`) 재생
      7. Dialogue
        - Speaker: "간호사 C"
        - Content: "Level 1에 플라즈마 솔루션과 혈액백 연결 완료되었습니다."
        - TTS: true

세 브랜치 완료 후 합류한다.

### 심정지 발생과 맥박 확인

1. Dialogue
  - Speaker: "의사 NPC"
  - Content: "그래도 혈압이 잘 안잡히네요..."
  - TTS: true
2. 환자 악화 연출(`patient_crash_ui`)을 재생한다.
3. Dialogue
  - Speaker: "의사 NPC"
  - Content: "심전도만 출력되고, 다른 활력징후가 출력되지 않습니다. 간호사 B 선생님, 환자 맥박 확인해주세요."
  - TTS: true
4. 간호사 B에게 퀘스트 발행
  - 제목: "맥박 확인" (`Quest_Check_Pulse`)
  - 목표
    - 표기: "환자의 경동맥을 촉지해 맥박 확인하기"
    - 처리: 촉지 신호(`sig.check_pulse_patient_a_r1`) 대기
    - Dialogue
      - Speaker: "시스템"
      - Content: "환자의 경동맥을 촉지해 맥박을 확인합니다. 목 부위를 클릭하세요."
      - TTS: true
5. Dialogue
  - Speaker: "간호사 B"
  - Content: "맥박 없습니다."
  - TTS: true
6. Dialogue
  - Speaker: "의사 NPC"
  - Content: "PEA입니다. CPR 하겠습니다. 제가 팀 리더를 맡겠습니다. 간호사 A 선생님은 앰부백 짜주시고, 간호사 B 선생님은 가슴압박 해주세요. 간호사 C 선생님은 제세동기 연결해주시고, 간호사 D 선생님은 C-line으로 에피네프린 1mg 투여해주세요."
  - TTS: true

### CPR 1주기

의사 지시에 따라 네 개의 브랜치가 병렬로 진행된다(P005).

- 간호사 A에게 퀘스트 발행 — 앰부백 산소화
  - 제목: "앰부백 산소화" (`Quest_Ambu_A`)
  - 목표
    - 표기: "앰부백으로 산소를 공급하기"
    - 처리: 아래 단계를 순서대로 완료하면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "앰부백과 산소 저장낭을 클릭해 획득하세요."
      - TTS: true
      - 처리: 획득 신호(`sig.click_ambubag`, `sig.click_reservoir_bag`)
    2. Dialogue
      - Speaker: "시스템"
      - Content: "환자에게 연결된 T-piece를 클릭해 연결을 해제하세요."
      - TTS: true
      - 처리: 해제 신호(`sig.remove_tpiece`)
    3. Dialogue
      - Speaker: "시스템"
      - Content: "앰부백을 클릭해 선택한 뒤, 환자에게 삽입된 기관내관을 클릭해 연결하세요. 이후, 산소줄과 앰부백을 클릭해 연결합니다."
      - TTS: true
      - 처리: 연결 신호(`sig.connect_ambubag`, `sig.connect_o2_to_ambu`), 이후 앰부백 적용 연출(`apply_ambu_patient_a`)
    4. ChoiceDialogue
      - Speaker: "시스템"
      - Content: "투여될 산소의 양을 조절합니다."
      - Choices(1개): "Full"
      - 이어서 산소 소리(`oxygen_sound`) 재생
    5. Dialogue
      - Speaker: "시스템"
      - Content: "앰부백을 클릭해 산소 공급을 시작하세요."
      - TTS: true
      - 처리: 시작 신호(`sig.start_ambu_r1`) 후 앰부배깅 연출(`start_ambubagging`)
    6. 이론 문항 1 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "1. 성인의 정확한 산소 제공량은?"
      - Choices: "약 1500ml (다섯 손가락 모두를 이용해 백을 짠다)"는 오답, "약 600ml (엄지, 검지, 중지를 이용해 백을 짠다)"이 정답
      - 오답 노드: "오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다." 후 재시도
    7. 이론 문항 2 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "2. 심폐소생술 시 앰부 배깅(ambu-bagging)의 적절한 속도는?"
      - Choices: "10초에 1번 (분당 약 6회)"와 "3초에 1번 (분당 약 20회)"은 오답, "6초에 1번 (분당 약 10회)"이 정답
      - 오답 노드: "오답입니다. 6초에 1번씩 눌러야 합니다." 후 재시도
- 간호사 B에게는 가슴압박 퀘스트를 발행
  - 제목: "가슴압박" (`Quest_ChestComp_B`)
  - 목표
    - 표기: "가슴압박을 수행하기"
    - 처리: 아래 단계를 순서대로 마치면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "환자의 가슴을 클릭해 가슴압박을 시작하세요."
      - TTS: true
      - 처리: 시작 신호(`sig.click_to_start_comp`)에 이어 압박 시작 연출(`start_chest_compression`)
    2. 이론 문항 1 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "1. 성인의 정확한 가슴 압박 깊이는?"
      - Choices: "약 4cm"와 "약 6cm"는 오답, "약 5cm"가 정답
      - 오답 노드: "오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다." 후 재시도
    3. 이론 문항 2 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "2. 성인의 정확한 가슴 압박 위치는?"
      - Choices: "양측 유두선상의 중간지점"은 오답, "흉골 하부 1/2 지점"이 정답
      - 오답 노드: "오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다." 후 재시도
    4. 이론 문항 3 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "3. 정확한 가슴 압박 횟수는?"
      - Choices: "분당 약 80~100회"와 "분당 약 120~140회"는 오답, "분당 약 100~120회"가 정답
      - 오답 노드: "오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다." 후 재시도
    5. 이론 문항 4 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "4. 가슴압박 시 주의사항은?"
      - Choices: "지쳐도 한 사람이 계속 가슴압박을 수행한다."와 "뼈가 부러진 것 같으면 멈춘다."는 오답, "충분한 이완을 제공한다."가 정답
      - 오답 노드: "오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 혈액 순환이 가능합니다." 후 재시도
- 간호사 C에게 퀘스트 발행 — 제세동기 준비
  - 제목: "제세동기 준비" (`Quest_Defib_C`)
  - 목표
    - 표기: "제세동기를 연결하고 패드를 부착하기"
    - 처리: 아래 단계를 순서대로 수행하면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "제세동 카트를 환자 옆으로 가져오세요."
      - TTS: true
      - 처리: 카트 이동 완료 신호(`sig.patient_bed_position_reached_defib_cart_a_defibcart_to_patient`) 대기
    2. Dialogue
      - Speaker: "시스템"
      - Content: "제세동 패드를 획득하고, 환자 흉부를 클릭해 부착하세요."
      - TTS: true
      - 처리: 패드 획득과 부착 신호(`sig.click_defibpad`, `sig.interact_patient_chest`), 이어서 부착 연출(`attach_defibpad`)과 불규칙 파형 UI(`defib_ui_irregular`)
    3. Dialogue
      - Speaker: "간호사 C"
      - Content: "제세동기 준비가 완료되었습니다."
      - TTS: true
    4. 이론 문항 1 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "1. 제세동기는 Sync 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 다음 중 제세동을 실시해야 하는 심전도는?"
      - Choices: "Asystole(무수축)", "PEA(무맥성 전기활동)", "VT(맥박이 있는 심실빈맥)"은 오답, "VF(심실세동)"가 정답
      - 오답 노드: "오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 VF(심실세동) 입니다." 후 재시도
    5. 이론 문항 2 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "2. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은?"
      - Choices: "360J(줄)"은 오답, "150~200J(줄)"이 정답
      - 오답 노드: "오답입니다. 150~200J(줄)이 정답입니다." 후 재시도
    6. 이론 문항 3 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "3. 제세동 등 전기충격 시 주의해야 할 사항은?"
      - Choices: "꼬인 수액 줄을 풀어준다.", "의료진이 손을 대어도 괜찮다.", "의사의 지시가 있을 때에만 실시한다."는 오답, "전기충격 전 모두 환자에게서 떨어지도록 지시한다."가 정답
      - 오답 노드: "오답입니다. 감전되지 않도록 모두가 떨어지도록 지시해야 합니다." 후 재시도
- 간호사 D에게는 에피네프린 투여 퀘스트를 발행
  - 제목: "에피네프린 투여" (`Quest_Epi_D`)
  - 목표
    - 표기: "에피네프린 1mg과 생리식염수 20cc를 투여하기"
    - 처리: 아래 단계를 순서대로 끝내면 완료 처리
    1. Dialogue
      - Speaker: "시스템"
      - Content: "에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요."
      - TTS: true
      - 처리: 획득 신호(`sig.click_epinephrine_ampule`, `sig.click_syringe_5cc`)
      - 기술 노트: 약물 주사기(`epinephrine_5cc_syringe`)는 crafting 시스템으로 조합한다.
    2. Dialogue
      - Speaker: "시스템"
      - Content: "Push용 생리식염수를 준비합니다. 20cc 생리식염수와 20cc 주사기를 클릭해 획득하세요."
      - TTS: true
      - 처리: 획득 신호(`sig.click_normal_saline_20ml`, `sig.click_syringe_20cc`)
      - 기술 노트: Push용 주사기(`normal_saline_20cc_syringe`)도 crafting 시스템으로 조합한다.
    3. Dialogue
      - Speaker: "시스템"
      - Content: "인벤토리에서 조합하여 준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요."
      - TTS: true
      - 처리: 투여 신호(`sig.push_epi_r1`)
    4. Dialogue
      - Speaker: "간호사 D"
      - Content: "에피네프린 1mg 투여했습니다."
      - TTS: true
    5. Dialogue
      - Speaker: "시스템"
      - Content: "동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다."
      - TTS: true
      - 처리: 투여 신호(`sig.push_ns_r1`)
    6. Dialogue
      - Speaker: "간호사 D"
      - Content: "생리식염수 20cc 투여했습니다."
      - TTS: true
    7. 이론 문항 1 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가?"
      - Choices: "약 1~2분에 한 번", "약 5~10분에 한 번", "누군가 시킬 때 마다"는 오답, "약 3~5분에 한 번"이 정답
      - 오답 노드: "오답입니다. 에피네프린은 3~5분에 한 번 투여합니다." 후 재시도
    8. 이론 문항 2 — ChoiceDialogue
      - Speaker: "시스템"
      - Content: "2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는?"
      - Choices: "약물만 주입"과 "약물 주입 후 생리식염수 주입"은 오답, "약물 주입 후 생리식염수 주입, 이후 팔 들어올리기"가 정답
      - 오답 노드: "오답입니다. 심장에 빠르게 도달시키기 위해 생리식염수 주입 후 팔을 들어올려야 합니다." 후 재시도

네 브랜치가 모두 끝난 뒤 합류한다.

### 리듬 확인과 CPR 2주기 (역할 교대)

1. Dialogue
  - Speaker: "의사 NPC"
  - Content: "2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요."
  - TTS: true
2. 압박·배깅 정지 연출(`stop_ambu_and_comp`) 후 무수축 모니터 연출(`asystole_monitor_ui`)을 재생한다.
3. Dialogue
  - Speaker: "의사 NPC"
  - Content: "Asystole입니다. 가슴압박과 앰부배깅 하시던 간호사 A, B 선생님끼리 교대 후 계속 가슴압박 해주세요. 간호사 C, D 선생님께서도 교대해서 역할을 수행해 주세요."
  - TTS: true

이 교대 지시로 네 브랜치가 나란히 진행된다(P006).

- 간호사 A에게 퀘스트 발행 — 가슴압박 교대 (`Quest_ChestComp_A`)
  - 간호사 B의 1주기 절차와 같다. 가슴 클릭(`sig.interact_chest`)으로 압박을 시작하며 깊이·위치·횟수·이완 네 문항을 순서대로 통과한다.
- 간호사 B에게 퀘스트 발행 — 앰부백 교대 (`Quest_Ambu_B`)
  1. Dialogue
    - Speaker: "시스템"
    - Content: "앰부백을 클릭해 산소 공급을 시작하세요."
    - TTS: true
    - 처리: 시작 신호(`sig.start_ambu_r2`), 이후 앰부배깅 연출(`start_ambubagging`)
  2. 산소 제공량(정답 약 600ml), 배깅 속도(정답 6초에 1번) 두 문항을 1주기 절차대로 진행한다.
- 간호사 C에게 퀘스트 발행 — 에피네프린 투여 교대 (`Quest_Epi_C`)
  1. 1주기와 같이 에피네프린 앰퓰과 5cc 주사기를 획득해 조합하고 20cc 생리식염수와 20cc 주사기를 획득한다.
  2. Dialogue
    - Speaker: "의사 NPC"
    - Content: "에피네프린 첫 투여 시점부터 4분 지났습니다. 간호사 C 선생님, 바로 에피네프린과 생리식염수 20cc 투여해주세요."
    - TTS: true
  3. Dialogue
    - Speaker: "시스템"
    - Content: "준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요."
    - TTS: true
    - 처리: 투여 신호(`sig.push_epi_r2`)
  4. Dialogue
    - Speaker: "간호사 C"
    - Content: "에피네프린 1mg 투여했습니다."
    - TTS: true
  5. 이어서 생리식염수 20cc를 같은 방법으로 투여한다(`sig.push_ns_r2`).
  6. Dialogue
    - Speaker: "간호사 C"
    - Content: "생리식염수 20cc 투여했습니다."
    - TTS: true
  7. 투여 간격(정답 3~5분에 한 번), 말초 투여 절차(정답 약물 주입 후 생리식염수 주입, 이후 팔 들어올리기) 두 문항을 1주기와 동일한 절차로 진행한다.
- 간호사 D에게 퀘스트 발행 — 제세동기 대기 (`Quest_Defib_D`)
  1. Dialogue
    - Speaker: "시스템"
    - Content: "제세동기를 클릭해 역할을 부여받으세요."
    - TTS: true
    - 처리: 클릭 신호(`sig.interact_defib`), 이후 제세동기 전원 소리(`defib_on_sound`) 재생
  2. Dialogue
    - Speaker: "간호사 D"
    - Content: "제세동기 준비가 완료되었습니다."
    - TTS: true
  3. 심전도(정답 VF), 에너지 양(정답 150~200J), 감전 주의(정답 전기충격 전 모두 떨어지도록 지시) 세 문항을 1주기와 같은 절차로 진행한다.

네 브랜치가 종료되면 합류한다.

### ROSC 확인과 후속 조치

1. Dialogue
  - Speaker: "의사 NPC"
  - Content: "2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요."
  - TTS: true
2. 압박·배깅 정지 연출(`stop_ambu_and_comp`) 후 ROSC 모니터 연출(`rosc_monitor_ui`)을 실행한다.
3. Dialogue
  - Speaker: "의사 NPC"
  - Content: "QRS 보입니다. 간호사 A 선생님, 환자 맥박 있는지 확인해주세요."
  - TTS: true
4. 간호사 A에게 퀘스트 발행
  - 제목: "ROSC 맥박 확인" (`Quest_Check_Pulse_ROSC`)
  - 목표
    - 표기: "환자의 경동맥을 촉지해 맥박 확인하기"
    - 처리: 촉지 신호(`sig.check_pulse_patient_a_r2`) 대기
    - Dialogue
      - Speaker: "시스템"
      - Content: "환자의 목을 클릭해서 경동맥을 촉지합니다."
      - TTS: true
5. Dialogue
  - Speaker: "간호사 A"
  - Content: "환자 맥박 느껴집니다."
  - TTS: true
6. Dialogue
  - Speaker: "의사 NPC"
  - Content: "환자 ROSC 되었습니다. 제가 검사랑 협진 의뢰 할테니 간호사 D 선생님이 의식상태 확인해주세요. 간호사 B 선생님, 의복 제거해서 추가 손상 있는지 사정해주세요. 간호사 A 선생님께서는 다시 분류구역으로 이동해서 환자 분류해주세요."
  - TTS: true

이 지시가 내려지면 세 브랜치가 병렬로 진행된다(P007).

- 간호사 A에게 퀘스트 발행 — 분류구역 복귀 (`Quest_Return_Triage`)
  1. Dialogue
    - Speaker: "시스템"
    - Content: "중증도 분류 구역으로 이동하세요."
    - TTS: true
    - 처리: 구역 도착 신호(`sig.arrive_triagearea`) 수신 뒤 이동 연출(`player_a_move_to_triage`)
- 간호사 B에게 퀘스트 발행 — 의복 제거 (`Quest_Cut_Clothing`)
  1. Dialogue
    - Speaker: "시스템"
    - Content: "가위를 클릭해 획득하고, 환자를 클릭해 의복을 제거하세요."
    - TTS: true
    - 처리: 가위 획득과 의복 제거 신호(`sig.click_scissors`, `sig.remove_patient_clothing`), 이후 가위질 소리(`cutting_sound`) 재생
  2. Dialogue
    - Speaker: "시스템"
    - Content: "추가 외상은 확인되지 않습니다."
    - TTS: true
- 간호사 D에게 퀘스트 부여 — 의식 상태 재사정 (`Quest_Check_GCS_ROSC`)
  1. Dialogue
    - Speaker: "시스템"
    - Content: "환자를 클릭해 환자의 의식 상태를 사정하십시오."
    - TTS: true
    - 처리: 사정 신호(`sig.check_gcs_a_rosc`) 대기
  2. 초기 평가와 같은 방법으로 AVPU/GCS를 다시 평가한다.
    - AVPU 관찰: 부를 때 응답이 없고 옆구리를 꼬집자 불편해하며 피함 → 정답 P
    - E 관찰: 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감음 → 정답 2점(통증에 반응)
    - V 관찰: 현재 기관내삽관이 시행 중인 상태 → 정답 E(기관삽관)
      - 오답 노드: "오답입니다. 기관삽관을 하는 경우 E로 처리(표기)합니다." 후 재시도
    - M 관찰: 손톱 뿌리쪽 피부에 압력을 가하자 움찔거리며 움직이려 함 → 정답 4점(통증에 회피)
  3. Dialogue
    - Speaker: "시스템"
    - Content: "GCS 측정 완료. E2 / V(E) / M4 = 총 6E점 입니다."
    - TTS: true

세 브랜치 전부가 끝나는 대로 한 지점에 합류한다.

### 시나리오 종료

- Dialogue
  - Speaker: "시스템"
  - Content: "시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다."
  - TTS: true
- 이 대사(D037)가 마지막 노드이며 다음 연결은 없다(`null`). 환자 A 시나리오는 이 지점에서 독립 종료된다. 다음 시나리오로 자동 전환은 없다.

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 환자 A: 흉부 관통상 및 심정지 대응 |
| 요약 | 환자 A를 처치실로 이동시키고 ABCDE 순서로 처치를 수행한 뒤 ROSC까지 진행한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 A |
| 주요 장소 | 처치실 |
| 리소스 식별자 - 사운드 | tape_sound, oxygen_sound, defib_on_sound, cutting_sound |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_treatment_room |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | SPAWN_A |

- [x] a-3: 콘텐츠 노출 화자명은 `시스템`(한글), 기술/식별자 표기는 `System`(영문)으로 정규화함.
- [x] a-2: MoveNextBehavior/WaitUntil 열거값을 엔진 정본(`Immediately`/`WaitUntilDone`)으로 정규화함(구 md `Immediate` 폐기).
- [x] a-1/a-2: EventIdentifier·Validator 시그널·아이템 식별자를 JSON/C# 정본 snake_case로 통일함.
- [x] a-1 잔여: 병렬 브랜치의 `CompletionConditionIdentifier`는 인간 작업자 코멘트에 따라 `CC_*_patient_a` 계열로 통일함. 이 값은 독립 노드가 아니라 병렬 브랜치 종료 표식이며, 브랜치의 마지막 노드가 이 식별자로 전이할 때 `Parallel` 실행기가 완료로 소비한다(2026-07-18).

## TTS 음성 프리셋 정책 (2026-08-16)

시나리오의 대사 노드에 TTS 음성을 부여한다. 화자(`SpeakerName`)별 음성 프리셋 배정은 아래 표와 같으며,
JSON에서는 각 노드에 `"playTTS": true`와 `"ttsVoiceProfile": { "preset": "..." }`로 기록한다.

| SpeakerName | 프리셋 | 비고 |
|---|---|---|
| 시스템 | F3 | `patient_b_c_ct`의 시스템 화자와 동일 프리셋 |
| 의사 NPC | M1 | `patient_b_c_ct`의 의사 화자와 동일 프리셋 |
| 간호사 A | M3 | |
| 간호사 B | F4 | |
| 간호사 C | M4 | |
| 간호사 D | F5 | |

노드 명명 규칙(알파벳 접두 + 숫자 3자리)에 따른 적용 범위:

1. **`D***` Dialogue 노드**: 모든 인간 참여자에게 텍스트가 보이고 TTS가 출력된다. 화자별 프리셋을 적용한다.
2. **`N***` Dialogue 노드**: 특정 플레이어에게만 텍스트가 보이고 TTS가 출력된다. 화자별 프리셋을 동일하게 적용한다.
3. **`C***` Choice 노드**: TTS를 적용하지 않는다(`playTTS` 미설정 유지).

- [x] TTS-1: 위 정책을 `patient_a_critical.scenario.json`에 반영함(2026-08-16). D 접두 35개 + N 접두 101개 = 총 136개 Dialogue 노드에 `playTTS`/`ttsVoiceProfile` 기록, C 접두 Choice 노드 32개는 미적용 유지.
- [x] TTS-2: 사용 프리셋 6종(F3, F4, F5, M1, M3, M4)의 사전 bake 완료(2026-08-16, 커밋 96b92a39). 산출물 `Assets/StreamingAssets/TTS/BakedInline/patient_a_critical/{프리셋}/`에 136개 노드분 생성 확인(F3 105, M1 15, M4 6, F5 5, F4 3, M3 2).
- [x] TTS-4: TTS 속도 배율을 전 시나리오 1.05 → **1.15**로 통일(2026-08-16, 커밋 96b92a39). 반영 위치: 전 시나리오 재bake(disaster_intro, patient_a_critical, patient_b_c_ct, tutorial, multi_voice_example), `MI_ TextToSpeechService`/`MI_ DialogueController` 프리팹 인스펙터, 프리셋 정의 C#(`ScenarioTTSVoiceProfile.cs`, 커밋 반영 여부 확인 필요). 향후 재bake 시 bake 창 "속도 배율"에 1.15를 입력해야 한다(창 기본값은 1.05).
- [ ] TTS-3 (인간 검토 필요): 실플레이에서 화자별 음성 출력 및 N 접두 노드의 대상 플레이어 한정 재생 검증.

## JSON 변환 전 연결성 감사 (2026-07-18)

이 절은 기존 `patient_a_critical.scenario.json`을 참조하지 않고 이 문서만으로 그래프를 재구성한 결과다.
변환기는 아래의 **확정 보완 규칙**을 적용해야 하며, **차단 항목**이 해결되지 않은 상태에서는 플레이 가능
Scenario로 판정하면 안 된다.

### 확정 보완 연결 (실행 안전 그래프)

아래 연결은 현재 문서의 노드 정의를 실행 관점에서 재정렬한 요약이다. 목적은 "중간 영구 정지(무한 대기)"를 유발하는 누락 신호/누락 합류를 사전에 식별하는 데 있다.

```text
SPAWN_A
  -> SPAWN_DOCTOR
  -> PRESET_A
  -> D005
  -> Q006(들것 이동) -> P002(ByRole 4분기) -> Q006_1
  -> E005(처치실 이동)
  -> D006
  -> P003(활력/GCS/흡인) -> D010
  -> E009 -> D011 -> D012 -> D013
  -> P004(삽관·산소 / 지혈 / IV·C-line) -> D022
  -> E025 -> D023 -> N016 -> Q018 -> V022 -> D024 -> D025
  -> P005(CPR 1주기 병렬) -> D028 -> E031 -> E032 -> D029
  -> P006(CPR 2주기 병렬) -> D033 -> E035 -> E036 -> D034 -> N025
  -> Q027 -> V031 -> D035 -> D036
  -> P007(ROSC 후 병렬) -> D037
  -> (end)
```

- `CC_*` 식별자는 독립 노드가 아니라 병렬 브랜치 완료 표식이다. 각 브랜치 마지막 노드는 해당 `CompletionConditionIdentifier`로 전이되어야 하며, 누락 시 `Parallel` 합류가 해제되지 않는다.
- `Validator(waitForCondition=true, failureNextIdentifier=null)`는 의도적으로 무한 대기다. 따라서 producer 미배선 신호가 남아 있으면 실제 플레이에서 해당 지점이 하드 스톱된다.

### 기술 노트/코멘트 반영 지침 (변환 시 강제)

이 문서의 "기술 노트"와 "코멘트"는 설명 텍스트가 아니라 **실행 명세**로 취급한다.

1. **(a) 자동 계측 가능** 표시는 "새 신호를 만들라"는 의미가 아니다.
   - 기존 자동 producer(`MedicalItem.OnGet`, `Item Use`, `ScenarioTriggerZone`,
     `IntravenousLineConnectionPoint`, `PatientController Assess`)에 식별자만 정합시킨다.
   - 변환 시 임의의 디버그 이벤트/임시 emitter를 정식 producer로 대체하지 않는다.
2. **(b) 선행 구현 필요(미배선)** 표시는 시나리오 실행 차단 신호다.
   - 해당 Validator는 제거/완화하지 않고 유지한다.
   - 대신 미구현 상태를 "인간 검토 메모"로 남겨 승인 전 해결 대상으로 추적한다.
3. **코멘트 기반 분기 의도**(예: OR 게이트, 교대 학습 루프, 오답 재진입)는 축약 금지다.
   - Choice 오답 루프를 제거하거나 단축하면 교육 의도가 손상된다.
4. **QuestControl Add/Remove 페어**는 동일 식별자 유지가 원칙이다.
   - 안내형 quest는 Validator 게이팅 + Remove로 종료되므로, 자동 완료 task를 임의 추가하지 않는다.
5. **ByRole 병렬 실행**은 현재 엔진 제약을 따른다.
   - 한 브랜치에 다중 역할을 기대하는 표기가 있더라도, 실제 배정 정책은 ROLE-2 메모 확정 전
     단정하지 않는다.

### 인간 검토 메모 (미확정/차단 항목)

아래 메모는 "없는 내용을 임의 보강하지 않기" 위한 고정 메모다. 각 메모는 해당 위치의 변환/구현 작업 티켓에 그대로 첨부한다.

| 메모 ID | 위치 | 인간 검토 메모 |
|---|---|---|
| HR-SPAWN-A-1 | `SPAWN_A` | `patient_a` prefab의 FishNet spawnable 등록/재직렬화 완료 전에는 시작 노드를 Production 승인하지 않는다. |
| HR-ROLE-2 | `P004` (`N008`, `N011`) | `ByRole` 다중 인원 브랜치 정책을 확정한다. (a) 단일 수행자로 단순화, (b) 병렬 브랜치 분해 중 하나를 선택하고 근거를 남긴다. |
| HR-S-1 | `V011_1`, `V014_1~V014_4`, `V018`, `V023_1`, `V024`, `V025~V025_1`, `V027`, `V030`, `V033` | 미배선 10개 producer의 실제 상호작용 소스와 callback 위치를 확정한다. 디버그 emitter는 불인정한다. |
| HR-IV-1 | `V017` 계열 | 18G 2개 획득 판정을 `좌/우 획득 신호 분리` 또는 `인벤토리 수량 조건` 중 하나로 확정한다. 확정 전에는 V017 계열을 임의 완화하지 않는다. |
| HR-END-1 | `D037` 이후 | 종료 UX(페이드아웃, 종료 메시지, 다음 scenario identifier)를 확정한다. 확정 전에는 `(end)` 직결 유지가 임시안이다. |


### 인간 검토 작업 지시서 (작업 편의용)

아래는 인간 작업자가 바로 실행할 수 있도록 각 HR 항목을 **요구 작업 단위**로 재작성한 목록이다.

| 작업 ID | 선행 조건 | 요구 작업(필수) | 완료 판정(증빙) |
|---|---|---|---|
| TASK-HR-SPAWN-A-1 | Unity 프로젝트 열림 | `patient_a` 원본 prefab을 FishNet Spawnable Prefabs에 등록하고 재직렬화한다. | Play Mode에서 `SPAWN_A` 시작 시 `ObjectId 65535` 오류가 재현되지 않고, Production profile에서 `EntityPreset(patient_a)` capability 검증이 통과한다. |
| TASK-HR-ROLE-2 | ROLE-1 태그 공급 유지 | `P004`의 다중 역할 브랜치 정책을 하나로 확정한다: (A) 단일 수행자 규칙으로 문구/태그 정리, (B) 브랜치를 다인 병렬 구조로 분해. | `P004` 진입 시 역할 미배정/교착이 없고, 선택한 정책이 문서(`ROLE-2`)와 scenario JSON에 동일하게 반영된다. |
| TASK-HR-S-1 | 각 상호작용 프리팹 접근 가능 | 미배선 10개 신호의 producer를 실제 gameplay callback에 연결한다. (`show_vital_patient_a`, `pass_laryngoscope`, `pass_et_tube_ready`, `remove_intu_stylet`, `pass_syringe`, `pass_central_line_set`, `remove_tpiece`, `click_to_start_comp`, `move_defibcart_to_patient`, `remove_patient_clothing`) | 각 Validator가 목표 상호작용 1회 수행으로 통과하고, 디버그/수동 emitter 없이도 end-to-end 진행이 가능하다. |
| TASK-HR-IV-1 | `insert_iv_patient_a_left/right` 배선 완료 | 18G 2개 획득 판정 방식을 확정하고 V017 계열 규칙을 통일한다: (A) 좌/우 획득 신호 분리, (B) 인벤토리 수량 조건(`Count=2`) 사용. | V017~V017_3 구간에서 획득/삽입 의미가 충돌하지 않고, 오탐/미탐 없이 좌우 IV 완료까지 진행된다. |
| TASK-HR-END-1 | `D037` 도달 가능 | 종료 UX를 확정한다: fade-out 이벤트, 종료 메시지 표시, 다음 scenario identifier 연결. | `D037` 이후 연출이 서술과 동일하며 마지막 노드 1개만 `null` 종료를 사용한다. |

### 작업 후 문서 수정 규칙 (추가/삭제 허용 범위)

인간 검토 작업 완료 후 문서를 갱신할 때 아래 규칙을 따른다.

1. **반드시 수정할 항목**
   - 해당 HR 행의 상태를 "미확정"에서 "해결"로 바꾸고, 해결 날짜와 근거(어떤 컴포넌트/콜백에서 해결했는지)를 한 줄로 기록한다.
   - 관련 `(b) 선행 구현 필요` 주석을 `[x] 배선 완료(YYYY-MM-DD)` 형식으로 전환한다.
   - `변환 승인 조건`에서 해소된 차단 항목을 반영한다.

2. **추가해도 되는 항목**
   - 실제 구현 경로(프리팹/스크립트/이벤트 식별자) 참조 한 줄.
   - 검증 절차(재현 단계 2~4줄)와 검증 결과 요약.
   - 선택지 확정 근거(왜 A/B 중 하나를 선택했는지) 한 단락.

3. **삭제/완화하면 안 되는 항목**
   - `Validator(waitForCondition=true, failureNextIdentifier=null)` 자체를 임의 삭제/즉시 통과로 변경.
   - 교육 의도용 Choice 오답 루프/코멘트 분기 축약.
   - Add/Remove Quest 페어 불일치(식별자 변경 포함).
   - "미해결" 상태인데 HR 메모만 삭제하는 행위.

4. **삭제해도 되는 항목(조건부)**
   - 동일 의미 중복 메모/구현 전 임시 주석은, 해결 근거가 본문 다른 위치에 남아 있을 때만 삭제 가능.
   - `인간 검토 메모`의 특정 행은 해당 항목이 해결되고, 해결 근거가 `플레이 차단 항목` 또는 노드 주석에 이관된 경우에만 삭제 가능.

5. **JSON 동기화 규칙**
   - 문서에서 확정한 식별자/분기/종료 연결은 `Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json`에 동일하게 반영한다.
   - 문서와 JSON이 불일치하면 문서를 우선 기준으로 보고 불일치 항목을 즉시 메모로 남긴다.

### 구조 감사 결과

| 검사 | 결과 | 변환 규칙 |
|---|---|---|
| 시작점 및 도달성 | `SPAWN_A`에서 문서의 337개 노드가 모두 도달 가능 | 시작 노드는 `SPAWN_A`로 고정한다. |
| 일반 전이 | 정의되지 않은 일반 `NextIdentifier`/선택지 대상 없음 | `(end)`만 JSON의 `null`로 변환한다. |
| 병렬 합류 | 6개 `Parallel`과 21개 완료 표식의 시작·종료 연결이 대응됨 | `CC_*`는 `nodes`에 만들지 않고 `CompletionConditionIdentifier` 및 브랜치 마지막 `nextIdentifier`에 같은 문자열로 기록한다. |
| 이벤트 | 문서의 고유 `EventIdentifier` 30개가 모두 `TriageScenarioEventBootstrap`에 등록됨 | `InvokeEvent`로 보존하되 Requirements 검증에서 handler 등록을 필수로 한다. |
| 열거값 | 문서의 `WaitAll`은 현재 엔진/schema에 존재하지 않음 | 모든 병렬 노드의 `WaitMode`를 정본 `All`로 보정했다. |
| 퀘스트 | ~~23개 퀘스트가 식별자만 있고 제목·본문·완료조건 정의가 없었다.~~ **해결:** `patient_a_critical.quests.quest.json`에 23개 definition의 title/description/questContent를 작성했고 모든 `QuestControl` 참조가 정의와 일치한다. | 이 그래프는 Validator가 플레이 완료를 게이팅하고 `QuestControl(Remove)`가 오버레이를 내리므로, definition의 빈 `tasks`는 자동 완료 조건이 아닌 안내 전용 퀘스트라는 의도된 구성이다. |
| 종료 연결 | `D037` 뒤 fade-out·종료 메시지·다음 Scenario 연결이 서술에만 있음 | **해결:** 환자 A는 `D037`을 마지막 노드로 종료하며 `nextIdentifier=null`이 의도된 구성이다. 다음 시나리오 자동 연결은 없다. |

### 확정 보완 규칙

1. `RequiredRoleIdentifiers` 열은 현재 `ScenarioParallelBranch` JSON 필드가 아니므로 출력하지 않는다.
   런타임 할당은 `requiredPlayerTags`, `forbiddenPlayerTags`, `requiredPlayerTagsMatchMode`만 사용한다.
2. `FailureNextIdentifier=null`이고 `WaitForCondition=true`인 Validator는 무한 대기 게이트로 유지한다.
   단, 아래 S-1 신호 생산자 명세에 포함되지 않은 신호를 기다리는 Validator는 생성하지 않는다.
3. `Quest_*`를 Add/Remove하는 두 노드는 동일한 대소문자 식별자를 사용해야 한다. 변환 시 임의로
   `questDefinitionIdentifier`로 치환하지 않고, Q-1에서 확정한 quest definition을 참조한다.
4. `CC_*_patientA`였던 합류 표식은 모두 `CC_*_patient_a`로 정규화한다.

### 플레이 차단 항목과 인간 판단 위치

| ID | 위치 | 부족한 연결 | 처리 |
|---|---|---|---|
| SPAWN-A-1 | `SPAWN_A` | ~~Unity import에서 `patient_a`의 `PatientTypeA` prefab이 FishNet `DefaultPrefabObjects`에 등록되지 않아 `PrefabId`가 미할당된 것으로 확인됐다. 현재 상태로 network spawn하면 런타임 `ObjectId 65535` 오류가 발생한다.~~ **해결(2026-07-28):** `PatientTypeA.prefab`이 `_isSpawnable: 1`, `PrefabId: 7`로 reserialize되었고 `patient_a` EntityPreset이 해당 prefab을 참조한다. | FishNet spawnable prefab 등록 상태를 확인했다. Production profile의 `SpawnablePreset` capability 검증만 최종 실행하면 된다. |
| ROLE-1 | `P002`, `P003`, `P004`, `P005`, `P006`, `P007` 진입 전 | ~~`ByRole`의 역할 태그 공급 명세가 없었다.~~ **해결:** `disaster_intro`의 `C_role_select`가 현재 플레이어에게 `nurse_a`~`nurse_d` 식별 태그와 해당 역할 facet 태그를 함께 부여한다. | 환자 A/B/C는 intro 역할 선택 뒤에 시작하는 시나리오다. CPR 교대(P005/P006)는 facet 재사용을 피하기 위해 `nurse_a`~`nurse_d` 식별 태그로 고정 배정한다. |
| ROLE-2 | `P004`의 `N008`, `N011` 브랜치 | 현재 `ByRole`은 한 브랜치에 한 플레이어를 배정하므로 두 역할을 동시에 요구하는 `matchMode=All`은 실제 매칭이 불가능하다. | **해결(2026-07-28):** 두 브랜치 모두 `requiredPlayerTagsMatchMode=Any`로 변경했다. `N008`은 `airway_team` 또는 `triage_lead` 중 하나의 태그를 가진 플레이어 1명이 삽관·산소 흐름 전체를 담당하고, `N011`은 `iv_team` 또는 `access_support` 중 하나의 태그를 가진 플레이어 1명이 IV·C-line 보조 흐름 전체를 담당한다. 이 정책은 2인 동시 협업을 표현하지 않으며, 각 브랜치의 두 역할은 대체 담당 자격이다. 서버 권위 실행은 기존 `ByRole` 다중 브랜치 실행을 위해 유지한다. |
| S-1 | `V011_1`, `V014_1~V014_4`, `V018`, `V023_1`, `V024`, `V025~V025_1`, `V027`, `V030`, `V033` | 실제 코드·문서 대조 결과, 정식 producer가 없는 신호는 10개다: `show_vital_patient_a`, `pass_laryngoscope`, `pass_et_tube_ready`, `remove_intu_stylet`, `pass_syringe`, `pass_central_line_set`, `remove_tpiece`, `click_to_start_comp`, `move_defibcart_to_patient`, `remove_patient_clothing`. 하나라도 생산되지 않으면 해당 Validator에서 영구 정지한다. | 각 노드의 기존 `(b) 선행 구현 필요` 주석을 producer 작업 목록으로 사용한다. `pass_*`의 NPC identifier·제출 상호작용 identifier·배치 위치는 아래 **의사 NPC 제출 producer 콘텐츠 확정**에서 확정했다. 나머지 6개 producer는 해당 주석의 전용 게임플레이 상호작용으로 구현한다. Debug emitter를 정식 producer로 간주하지 않는다. |
| Q-1 | 모든 `Q006`~`Q030_1` | ~~23개 `Quest_*`가 표시용 식별자만 있어 빈 오버레이를 만들었다.~~ **해결:** `Resources/Quest/patient_a_critical.quests.quest.json`에 23개 definition의 title/description/questContent를 작성했고, Add/Remove가 같은 definition identifier를 참조한다. | Scenario Validator가 완료를 판단하고 QuestControl이 명시적으로 Remove한다. 따라서 이 안내형 quest에 별도 자동 완료 task를 추가하지 않는다. |
| IV-1 | `V017` / `V017_1` / `V017_3` | **해결(2026-07-28):** 18G는 두 개를 사전에 동시에 획득하지 않는다. 첫 번째 18G를 획득해 좌측 삽입 시 1개를 소비하고, `N011_3` 안내 후 두 번째 18G를 다시 획득해 우측 삽입 시 1개를 소비한다. 삽입은 `insert_iv_patient_a_left` / `insert_iv_patient_a_right`로 좌·우를 구분한다. | `click_cannula_18g`는 준비 단계에서 첫 번째 18G 보유를 확인하는 단일 신호로 유지한다. 두 번째 18G는 두 번째 삽입 직전에 별도 획득하며, 실제 소비는 삽입 완료 코드가 담당한다. |
| END-1 | `D037` 및 종료 조건 | `D037`이 마지막 노드이며 `nextIdentifier=null`이다. | **해결(2026-07-28):** 환자 A는 이 지점에서 독립 종료한다. `patient_b_c_ct`는 다음 시나리오가 아니며 관리자가 별도 실행한다. |

아래 표의 상태는 **변환 전 단일 차단 게이트**를 기준으로 갱신한다.
### **변환 전 단일 차단 게이트 (작업판)**

| Gate ID | 분류 | 현재 상태 | 결정/구현 필요 | 담당 | 증빙 |
|---|---|---|---|---|---|
| SPAWN-A-1 | Runtime | 해결(2026-07-28) | FishNet Spawnable 등록 + 재직렬화는 완료, Production profile capability 최종 검증만 잔여 | 구현자 | `PatientTypeA.prefab` spawn 정상, ObjectId 65535 미재현 |
| ROLE-2 | Design+Runtime | 해결(2026-07-28) | P004 정책 A안 확정 및 반영(`requiredPlayerTagsMatchMode=Any`) | 주도자 | P004 교착 없음, 문서/JSON 동일 |
| S-1 | Runtime | 부분해결(2026-07-31) | 잔여 과업은 실플레이 검증. B-02/B-03/B-05/B-06은 `ItemSubmissionConfig` 노드(`ISC_PASS_*`) 추가로 그래프 배선 완료 | 구현자 | `N008_1 -> ISC_PASS_LARYNGOSCOPE -> V014_1`, `N008_2 -> ISC_PASS_ET_TUBE -> V014_2`, `N008_4 -> ISC_PASS_SYRINGE -> V014_4`, `Q014 -> ISC_PASS_CENTRAL_LINE_SET -> V018` |
| IV-1 | Design | 해결(2026-07-28) | V017 18G 순차 획득·소비 및 좌/우 삽입 분리 확정 반영 | 주도자 | V017~V017_3 규칙 문서/JSON 일치 |
| END-1 | Design+Content | 해결(2026-07-28) | 종료 정책 확정(`D037 -> null` 독립 종료) | 주도자 | D037 이후 종료 조건 문서/JSON 일치 |

### 의사 NPC 제출 producer 콘텐츠 확정

환자 A의 10개 producer 콘텐츠 대상 중 `pass_*` 네 건은 `ItemSubmissionConfig`와
`ItemSubmissionInteractable`로 구현한다. 의사 NPC 식별자는 모두 **`npc-doctor-patient-a-critical`**로 고정한다.
제출 상호작용은 별도 바닥 오브젝트를 만들지 않고 **`npc-doctor-patient-a-critical`에 부착**한다. 즉,
`positionSourceEntityIdentifier`는 `npc-doctor-patient-a-critical`이고, 제출 위치는 시나리오 진행 시점의 의사 NPC
현재 위치다.

| Validator / 완료 신호 | 제출 내용 | 의사 NPC identifier | 제출 상호작용 identifier | 제출 위치 |
|---|---|---|---|---|
| `V014_1` / `sig.pass_laryngoscope` | 조립 완료한 후두경 | `npc-doctor-patient-a-critical` | `patient-a-doctor-submit-laryngoscope` | `npc-doctor-patient-a-critical`에 부착(의사 NPC 현재 위치) |
| `V014_2` / `sig.pass_et_tube_ready` | 준비 완료한 기관내관 | `npc-doctor-patient-a-critical` | `patient-a-doctor-submit-et-tube` | `npc-doctor-patient-a-critical`에 부착(의사 NPC 현재 위치) |
| `V014_4` / `sig.pass_syringe` | 5 cc 주사기 | `npc-doctor-patient-a-critical` | `patient-a-doctor-submit-5cc-syringe` | `npc-doctor-patient-a-critical`에 부착(의사 NPC 현재 위치) |
| `V018` / `sig.pass_central_line_set` | C-line set | `npc-doctor-patient-a-critical` | `patient-a-doctor-submit-central-line-set` | `npc-doctor-patient-a-critical`에 부착(의사 NPC 현재 위치) |

구현 시 각 상호작용은 해당 단계에서만 활성화하고, 완료 신호를 표의 `sig.pass_*` 값으로 발신한다.
완료 또는 다음 제출 단계 전에는 이전 상호작용을 비활성화하여, 같은 물품을 잘못 제출하는 일을 막는다.

### **미배선 신호 명세표 (S-1)**

| TaskID | Signal | 소비 Validator | Producer 위치(오브젝트/프리팹) | 콜백/트리거 | 상태 | 검증 |
|---|---|---|---|---|---|---|
| B-01 | sig.show_vital_patient_a | V011_1 | patient_a / PatientController.AssessActions(assess_vital) | PatientController.PerformAssess() (assess_vital) 완료 시 ScenarioInteractionSignals.Raise("show_vital_patient_a") | 배선완료(2026-07-28, PatientTypeA.assess_vital._assessSignal 정합 + PerformAssess Raise 경로 확인) | 미검증 |
| B-02 | sig.pass_laryngoscope | V014_1 | `ISC_PASS_LARYNGOSCOPE` (`ItemSubmissionConfig`) -> `patient-a-doctor-submit-laryngoscope` | ItemSubmission 완료 | 배선완료(2026-07-31, `N008_1` 다음에 `ISC_PASS_LARYNGOSCOPE` 추가) | 미검증 |
| B-03 | sig.pass_et_tube_ready | V014_2 | `ISC_PASS_ET_TUBE` (`ItemSubmissionConfig`) -> `patient-a-doctor-submit-et-tube` | ItemSubmission 완료 | 배선완료(2026-07-31, `N008_2` 다음에 `ISC_PASS_ET_TUBE` 추가) | 미검증 |
| B-04 | sig.remove_intu_stylet | V014_3 | OverworldScene / endotracheal_tube_ready_A / EtTubeStyletInteractPoint(ScenarioActionInteractable) | ScenarioActionInteractable.Interact() 완료 시 ScenarioInteractionSignals.Raise("remove_intu_stylet") | 배선완료(2026-07-28, PatientTypeA.EtTubeStyletInteractPoint._completionSignal 정합 확인) | 미검증 |
| B-05 | sig.pass_syringe | V014_4 | `ISC_PASS_SYRINGE` (`ItemSubmissionConfig`) -> `patient-a-doctor-submit-5cc-syringe` | ItemSubmission 완료 | 배선완료(2026-07-31, `N008_4` 다음에 `ISC_PASS_SYRINGE` 추가) | 미검증 |
| B-06 | sig.pass_central_line_set | V018 | `ISC_PASS_CENTRAL_LINE_SET` (`ItemSubmissionConfig`) -> `patient-a-doctor-submit-central-line-set` | ItemSubmission 완료 | 배선완료(2026-07-31, `Q014` 다음에 `ISC_PASS_CENTRAL_LINE_SET` 추가) | 미검증 |
| B-07 | sig.remove_tpiece | V023_1 | OverworldScene / patient_a T-piece connected visual / TPieceRemoveInteractPoint(ScenarioActionInteractable) | ScenarioActionInteractable.Interact() 완료 시 ScenarioInteractionSignals.Raise("remove_tpiece") | 배선완료(2026-07-28, PatientTypeA.TPieceRemoveInteractPoint._completionSignal 정합 확인) | 미검증 |
| B-08 | sig.click_to_start_comp | V024 | OverworldScene / patient_a chest interaction point / ChestCompStartInteractPoint(ScenarioActionInteractable) | ScenarioActionInteractable.Interact() 완료 시 ScenarioInteractionSignals.Raise("click_to_start_comp") | 배선완료(2026-07-28, PatientTypeA.ChestCompStartInteractPoint._completionSignal 정합 확인) | 미검증 |
| B-09 | sig.patient_bed_position_reached_defib_cart_a_defibcart_to_patient | V025 | Defib cart(MovingPatientBedController: `defib_cart_a`) + defibcart_to_patient(MovingPatientBedPositioningPoint) | PublishPositioningPointReached() -> Raise("patient_bed_position_reached_defib_cart_a_defibcart_to_patient") | 배선완료(2026-07-28, MovingPatientBedController scoped signal 발신 + OverworldScene defib_cart_a/defibcart_to_patient 정합 확인) | 미검증 |
| B-10 | sig.remove_patient_clothing | V033 | OverworldScene / patient_a 흉부 클릭 포인트(PatientClothingCutPoint) / ScenarioActionInteractable | ScenarioActionInteractable.Interact() 완료 시 ScenarioInteractionSignals.Raise("remove_patient_clothing") 발신 | 배선완료(2026-07-28, PatientTypeA.PatientClothingCutPoint._completionSignal 정합 확인) | 미검증 |

### **결정 카드 (주도자 확정 필요)**

#### DECISION-ROLE-2 (P004 다중 역할)
- 선택지 A: 한 브랜치=한 수행자 규칙으로 문구/태그 단순화
- 선택지 B: N008/N011을 다인 병렬 하위 브랜치로 분해
- 현재 상태: 확정(2026-07-28)
- 확정안: A (단일 수행자 규칙, `requiredPlayerTagsMatchMode=Any`)
- 반영 대상: P004 branches, N008/N011 주변 안내 문구, 관련 CompletionCondition 반영 완료

#### DECISION-IV-1 (V017 18G 2개 판정)
- 선택지 A: 좌/우 획득 신호 분리
- 선택지 B: 인벤토리 수량 조건(Count=2)
- 현재 상태: 확정(2026-07-28)
- 확정안: A (삽입 신호 좌/우 분리: `insert_iv_patient_a_left/right`)
- 반영 대상: V017 rules/설명 주석 반영 완료(실플레이 검증은 별도)

#### DECISION-END-1 (종료 UX/다음 시나리오)
- 선택지 A: D037 -> END_FADE_OUT -> END_MESSAGE -> START_NEXT_SCENARIO -> null
- 선택지 B: D037 -> END_MESSAGE -> null (임시)
- 현재 상태: 확정(2026-07-28)
- 확정안: B 변형 (환자 A 독립 종료: `D037 -> null`)
- 반영 대상: 종료 조건 절, 변환 승인 조건, JSON 종료 연결 반영 완료

<!-- WORK-OVERLAY:START -->
### 변환 안전 작업 오버레이 (삭제 가능)

이 절은 인간 작업자의 검토/결정 편의를 위한 **작업 오버레이**다. 시나리오 노드 명세 본문이 아니며,
`WORK-OVERLAY:START/END` 블록은 변환기 입력에서 제외(또는 무시)해도 된다.

#### 오버레이 사용 규칙

1. 각 행의 `Node`를 본문의 `### [Node]`에서 검색해 해당 위치를 바로 열람한다.
2. `작업 유형`이 `결정`인 항목은 주도자 확정 후 본문 명세 문장으로 승격한다.
3. `작업 유형`이 `배선`인 항목은 Unity 상호작용/콜백 연결 후 `(b)` 주석을 `[x] 배선 완료(YYYY-MM-DD)`로 갱신한다.
4. 모든 항목이 해결되면 이 오버레이 절은 통째로 삭제 가능하다(삭제 전 본문 반영 필수).

#### A. 차단/결정 항목 (우선 처리)

| ID | Node | 작업 유형 | 필요한 작업 | 검토/결정 포인트 | 완료 기준 |
|---|---|---|---|---|---|
| SPAWN-A-1 | `SPAWN_A` | 배선/환경 | `patient_a` prefab을 FishNet Spawnable에 등록 및 재직렬화 | Production profile에서 SpawnablePreset capability 확인 | 해결(2026-07-28), 최종 capability 검증만 잔여 |
| ROLE-2 | `P004` (`N008`,`N011`) | 결정 | 다중 역할 정책 확정: (A) 단일 수행자 단순화, (B) 병렬 분해 | 엔진 `ByRole` 1브랜치 1인 정책과 합치 여부 | 해결(2026-07-28), A안 반영 완료 |
| IV-1 | `V017` 계열 | 결정 | 18G 2개 판정 방식 확정: (A) 좌/우 획득 신호 분리, (B) 인벤토리 Count=2 | 획득 의미와 삽입 의미 분리 여부 | 해결(2026-07-28), A안 반영 완료 |
| END-1 | `D037` 이후 | 결정 | 종료 체인(`fade-out`, 메시지, 다음 scenario) 확정 및 노드 명시 | 마지막 `null` 노드 1개 원칙 | 해결(2026-07-28), 독립 종료 정책 반영 |

#### B. 미배선 producer (S-1) + 보조 상호작용

| Node | Signal | 작업 유형 | 필요한 작업 | 완료 기준 |
|---|---|---|---|---|
| `V011_1` | `sig.show_vital_patient_a` | 배선 | 활력 측정 완료 콜백에서 signal raise 연결 | 1회 측정으로 `V011_1` 통과 |
| `V014_1` | `sig.pass_laryngoscope` | 배선 | `npc-doctor-patient-a-critical` 제출 상호작용 완료 시 raise | 1회 제출로 `V014_1` 통과 |
| `V014_2` | `sig.pass_et_tube_ready` | 배선 | `npc-doctor-patient-a-critical` 제출 상호작용 완료 시 raise | 1회 제출로 `V014_2` 통과 |
| `V014_3` | `sig.remove_intu_stylet` | 배선 | 기관내관 스타일렛 제거 상호작용 완료 콜백 연결 | 1회 제거로 `V014_3` 통과 |
| `V014_4` | `sig.pass_syringe` | 배선 | `npc-doctor-patient-a-critical` 제출 상호작용 완료 시 raise | 1회 제출로 `V014_4` 통과 |
| `V018` | `sig.pass_central_line_set` | 배선 | `npc-doctor-patient-a-critical` 제출 상호작용 완료 시 raise | 1회 제출로 `V018` 통과 |
| `V023_1` | `sig.remove_tpiece` | 배선 | T-piece 분리 상호작용 완료 콜백 연결 | 1회 분리로 `V023_1` 통과 |
| `V024` | `sig.click_to_start_comp` | 배선 | 가슴압박 시작 상호작용 콜백 연결 | 1회 시작으로 `V024` 통과 |
| `V025` | `sig.patient_bed_position_reached_defib_cart_a_defibcart_to_patient` | 배선 | 제세동 카트 이동 완료 콜백 연결(`defib_cart_a` + `defibcart_to_patient`) | 1회 이동으로 `V025` 통과 |
| `V033` | `sig.remove_patient_clothing` | 배선 | 의복 제거 상호작용 완료 콜백 연결 | 1회 제거로 `V033` 통과 |
| `V015_3` | `sig.click_o2_line` | 정합(보조) | 산소줄 획득 신호를 선행 단계로 고정 | `V015_3` 통과 |
| `V015_3_1` | `sig.interact_tpiece` | 배선(보조) | `endotracheal_tube_A` 클릭 지점에 `ScenarioActionInteractable` 설정(장착 단계) | `V015_3_1` 통과 |
| `V015_3_2` | `sig.connect_tpiece_and_oxyflow` | 정합(보조) | T-piece 측/벽 유량계 측 연결점 Identifier 정합(연결 완료 신호) | `V015_3_2` 통과 |
| `V015_4` | `sig.interact_oxyflow_wall` | 배선(보조) | 벽 유량계 설치 상태 상호작용 신호 설정(설치 시점이 아닌, 설치된 유량계 클릭 시 발행) | `V015_4` 통과 |
| `V025_1` | `sig.interact_patient_chest` | 배선(보조) | 환자 흉부 collider에 `ScenarioActionInteractable` 설정 | `V025_1` 통과 |
| `V027` | `sig.interact_chest` | 배선(보조) | 가슴압박 위치 collider에 `ScenarioActionInteractable` 설정 | `V027` 통과 |
| `V030` | `sig.interact_defib` | 배선(보조) | 제세동기 collider에 `ScenarioActionInteractable` 설정 | `V030` 통과 |

#### C. 자동 계측/정합 확인 항목 (A-타입)

| Node | Signal | 작업 유형 | 필요한 작업 | 완료 기준 |
|---|---|---|---|---|
| `V010_A` | `sig.grab_stretcher_patient_a_handle_0` | 정합 | grab 지점 Identifier 정합 | 1회 잡기로 통과 |
| `V010_B` | `sig.grab_stretcher_patient_a_handle_1` | 정합 | grab 지점 Identifier 정합 | 1회 잡기로 통과 |
| `V010_C` | `sig.grab_stretcher_patient_a_handle_2` | 정합 | grab 지점 Identifier 정합 | 1회 잡기로 통과 |
| `V010_D` | `sig.grab_stretcher_patient_a_handle_3` | 정합 | grab 지점 Identifier 정합 | 1회 잡기로 통과 |
| `V011` | `sig.click_vital_set` | 정합 | `MedicalItem.OnGet` 자동 발행 식별자 정합 | 1회 획득으로 통과 |
| `V012` | `sig.check_avpu_gcs_patient_a` | 정합 | Assess callback 식별자 정합 | 1회 사정으로 통과 |
| `V013` | `sig.click_wall_suction`, `sig.click_suction_line`, `sig.click_yankauer` | 정합 | 아이템 획득 자동 발행 식별자 정합 | 3개 획득 후 통과 |
| `V013_1` | `sig.apply_stabilizer_patient_a` | 정합 | Item Apply signal 식별자 정합 | 적용 후 통과 |
| `V013_2` | `sig.connect_wall_component_1` | 정합 | 연결지점 signal 식별자 정합 | 연결 후 통과 |
| `V013_3` | `sig.connect_wall_component_and_yankauer` | 정합 | 연결지점 signal 식별자 정합 | 연결 후 통과 |
| `V013_4` | `sig.suction_patient_a` | 정합 | Item Use signal 식별자 정합 | 사용 후 통과 |
| `V014` | `sig.click_laryngoscope_blade`, `sig.click_laryngoscope_handle`, `sig.click_endotracheal_tube`, `sig.click_stylet`, `sig.click_plaster`, `sig.click_syringe_5cc` | 정합 | 획득 자동 발행 식별자 정합 | 6개 획득 후 통과 |
| `V014_5` | `sig.apply_plaster_on_intu` | 정합 | Item Apply signal 식별자 정합 | 적용 후 통과 |
| `V015_2` | `sig.connect_wall_component_2` | 정합 | 연결지점 signal 식별자 정합 | 연결 후 통과 |
| `V015_3` | `sig.click_o2_line` | 정합 | 획득 signal 식별자 정합 | 선행 획득 통과 |
| `V015_3_1` | `sig.interact_tpiece` | 정합/배선 | 기관내관 클릭 상호작용 지점 completion signal 정합 | 장착 단계 통과 |
| `V015_3_2` | `sig.connect_tpiece_and_oxyflow` | 구현 대기 | T-piece 표시와 설치된 oxyflowmeter의 어느 쪽을 감지해도 `T피스에 산소 연결`을 노출하고, 실행 시 두 산소 포트를 연결한다. 현재는 권장 기획이며, 양측 `OxyLineConnectionPoint` 프리팹 배치가 선행되어야 한다. | 실제 연결 완료 통과 |
| `V016` | `sig.click_sterile_gloves`, `sig.click_gauze`, `sig.click_plaster` | 정합 | 획득 자동 발행 식별자 정합 | 3개 획득 후 통과 |
| `V016_1` | `sig.wear_glove` | 정합 | Item Apply signal 식별자 정합 | 착용 후 통과 |
| `V016_2` | `sig.apply_gauze` | 정합 | Item Apply signal 식별자 정합 | 적용 후 통과 |
| `V016_3` | `sig.apply_plaster_on_gauze` | 정합 | Item Apply signal 식별자 정합 | 적용 후 통과 |
| `V017` | `sig.click_cannula_18g`, `sig.click_normal_saline_1000ml`, `sig.click_plasma_solution_1000ml` | 정합/결정연계 | 자동 발행 정합 + IV-1 확정안 반영 | 확정안 기준 통과 |
| `V017_2` | `sig.connect_cannula_and_ns1` | 정합 | 연결지점 signal 식별자 정합 | 연결 후 통과 |
| `V019` | `sig.click_plasma_solution_1000ml`, `sig.click_blood_transfusion_set` | 정합 | 획득 자동 발행 정합 | 2개 획득 후 통과 |
| `V019_1` | `sig.connect_ps1_to_lv1` | 정합 | 연결지점 signal 식별자 정합 | 연결 후 통과 |
| `V020` | `sig.connect_blood_to_lv1` | 정합 | 연결지점 signal 식별자 정합 | 연결 후 통과 |
| `V022` | `sig.check_pulse_patient_a_r1` | 정합 | Assess callback 식별자 정합 | 1회 사정 통과 |
| `V023` | `sig.click_ambubag`, `sig.click_reservoir_bag` | 정합 | 획득 자동 발행 정합 | 2개 획득 후 통과 |
| `V023_2` | `sig.connect_ambubag`, `sig.connect_o2_to_ambu` | 정합 | 연결지점 signal 식별자 정합 | 2연결 통과 |
| `V023_4` | `sig.start_ambu_r1` | 정합 | Item Use signal 식별자 정합 | 사용 후 통과 |
| `V025_1` | `sig.click_defibpad` | 정합 | 획득 자동 발행 정합 | 획득 후 통과 |
| `V026` | `sig.click_epinephrine_ampule`, `sig.click_syringe_5cc` | 정합 | 획득 자동 발행 정합 | 2개 획득 통과 |
| `V026_1` | `sig.click_normal_saline_20ml`, `sig.click_syringe_20cc` | 정합 | 획득 자동 발행 정합 | 2개 획득 통과 |
| `V026_2` | `sig.push_epi_r1` | 정합 | Item Use signal 식별자 정합(OR 게이트 정책 유지) | 투여 후 통과 |
| `V026_3` | `sig.push_ns_r1` | 정합 | Item Use signal 식별자 정합 | 투여 후 통과 |
| `V028` | `sig.start_ambu_r2` | 정합 | Item Use signal 식별자 정합 | 사용 후 통과 |
| `V029` | `sig.click_epinephrine_ampule`, `sig.click_syringe_5cc` | 정합 | 획득 자동 발행 정합 | 2개 획득 통과 |
| `V029_1` | `sig.click_normal_saline_20ml`, `sig.click_syringe_20cc` | 정합 | 획득 자동 발행 정합 | 2개 획득 통과 |
| `V029_2` | `sig.push_epi_r2` | 정합 | Item Use signal 식별자 정합(OR 게이트 정책 유지) | 투여 후 통과 |
| `V029_3` | `sig.push_ns_r2` | 정합 | Item Use signal 식별자 정합 | 투여 후 통과 |
| `V031` | `sig.check_pulse_patient_a_r2` | 정합 | Assess callback 식별자 정합 | 1회 사정 통과 |
| `V032` | `sig.arrive_triagearea` | 정합 | `ScenarioTriggerZone` 진입 signal 정합 | 구역 진입 통과 |
| `V033` | `sig.click_scissors` | 정합 | 획득 자동 발행 정합 | 획득 후 통과 |
| `V034` | `sig.check_gcs_a_rosc` | 정합 | Assess callback 식별자 정합 | 1회 사정 통과 |

#### D. 변환기 안전 장치

- 이 오버레이 절은 실행 노드 정의가 아니므로, 변환기에서 무시하거나 변환 전 삭제해도 된다.
- 본문 노드 명세(`### [Identifier]`)과 충돌하는 식별자를 이 절에서 새로 정의하지 않는다.
- 이 절을 삭제하더라도, 확정된 결정/배선 결과는 각 노드 본문과 차단 표에 반드시 이관한다.

<!-- WORK-OVERLAY:END -->

-----

### 변환 승인 조건

- ROLE-2의 대체 담당 할당 정책은 확정되었다. `matchMode=Any`로 각 P004 브랜치를 한 명의 적격 플레이어가 수행하며, 서버 권위 실행은 기존 `ByRole` 다중 브랜치 실행을 위해 유지한다.
- SPAWN-A-1의 FishNet spawnable prefab 등록은 완료되었다(`PatientTypeA.prefab`, `PrefabId: 7`). Production profile의 `SpawnablePreset` capability 최종 검증이 남아 있다.
- S-1의 10개 신호에 정식 producer와 동일 식별자가 연결되어야 한다.
- Q-1의 23개 quest definition 작성과 Add/Remove 참조 일치는 완료되었다.
- IV-1의 18G 순차 획득·소비 정책과 END-1의 독립 종료(`D037 -> null`)가 반영되어야 한다.
- 변환 후 Requirements Supports에서 NPC/entity/item/event/quest/runtime-signal 요구사항을 컴파일하고,
  Production profile에서 unresolved `Error`가 0개여야 플레이 가능으로 승인한다.

## 조합(crafting) 참조 (d-2)

조합은 게임플레이 **노드가 아니라 시스템**으로 처리한다(`ItemCombineRecipeRegistry`). 구 md의 `CombineItem` 노드(A003~A011)는 이 문서의 노드 흐름에서 **제거**하고, 산출물이 준비(prepared)되어 있다고 전제한다. 상세 레시피는 [`crafting-recipes.md`](./crafting-recipes.md) 참조.

이 시나리오가 요구하는 조합 산출물:

| 산출물(정본) | 입력 | 구 md 노드(제거됨) | 등록 상태 | 명명 주의 |
|---|---|---|---|---|
| `yankauer_suction_ready` | `suction_line` + `yankauer` | A003 | 등록됨(2026-07-09) | 구 `yankauer_ready` → 정본 `yankauer_suction_ready` |
| `laryngoscope` | `laryngoscope_handle` + `laryngoscope_blade` | A004 | 등록됨 | 구 md `laryngo_handle`/`laryngo_blade` → 정본 `laryngoscope_handle`/`laryngoscope_blade` |
| `endotracheal_tube_ready` | `endotracheal_tube` + `stylet` | A005 | 등록됨 | 구 md `et_tube_ready`/`et_tube` → 정본 `endotracheal_tube_ready`/`endotracheal_tube` |
| `humidifier_sterile_distilled_water_bottle` | `humidifier_bottle` + `sterile_distilled_water` | A006 | 등록됨 | 구 `sdw` → 정본 `sterile_distilled_water` |
| `oxyflowmeter` | `humidifier_sterile_distilled_water_bottle` + `flowmeter` | A007 | 등록됨 | 환자 B·C 공용 단일 산출물 |
| `epinephrine_5cc_syringe` | `epinephrine_ampule` + `syringe_5cc` | A008 · A010 | 등록됨(2026-07-09) | 구 md `epi_ready`/`epi`/`epinephrine_syringe` → 정본 `epinephrine_5cc_syringe`/`epinephrine_ampule` |
| `normal_saline_20cc_syringe` | `normal_saline_20ml` + `syringe_20cc` | A009 · A011 | 등록됨(2026-07-09) | 구 `ns_20cc(_ready)` 산출물 제거 → 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1) |
| `normal_saline_intravenous_ready` | `normal_saline_1000ml` + `intravenous_set` | (인트로 사전조합) | 등록됨(2026-07-09) | 구 `ns1_ready` → 정본 `normal_saline_intravenous_ready`(수액 준비물) |
| `plasma_solution_intravenous_ready` | `plasma_solution_1000ml` + `intravenous_set` | (인트로 사전조합) | 등록됨(2026-07-09) | 구 `ps1_ready` → 정본 `plasma_solution_intravenous_ready`(수액 준비물) |

- [x] 명명충돌 확정요청: `et_tube_ready`↔`endotracheal_tube_ready`, `epi_ready`↔`epinephrine_5cc_syringe`, `ns_20cc(_ready)`→`normal_saline_20cc_syringe`, `ns1_ready`→`normal_saline_intravenous_ready`, `ps1_ready`→`plasma_solution_intravenous_ready`, `yankauer_ready`→`yankauer_suction_ready`. C# 정본에 맞춰 시나리오 산출물명 치환 확정(crafting-recipes.md §확정 요청 [x] 참조, 2026-07-09).
- [x] 등록완료 레시피(`yankauer_suction_ready`, `epinephrine_5cc_syringe`, `normal_saline_20cc_syringe`, `normal_saline_intravenous_ready`, `plasma_solution_intravenous_ready`, `laryngoscope`, `endotracheal_tube_ready`)를 `RegisterAllCombineRecipes()`에 추가 완료 (crafting-recipes.md 참조, 2026-07-09).
- [x] 산소화 레시피(`humidifier_sterile_distilled_water_bottle`, `oxyflowmeter`)와 재료(`humidifier_bottle`, `sterile_distilled_water`, `flowmeter`)는 `RegisterAllItems()`/`RegisterAllCombineRecipes()`에 등록됨.

## 환자 A 사전설정 (d-3)

시나리오 시작 시 환자 A 엔티티를 스폰하고 의료 상태를 사전설정한다. 시작 노드는 `SPAWN_A`이다.

### [actingNpcs[0]] 의사 NPC 선언

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | npc-doctor-patient-a-critical |
| **ActingNpcType** | 문자열 | Npc |
| **PresetIdentifier** | 문자열 | npc_doctor_preset |
| **DisplayName** | 문자열 | 의사 NPC |
| **ShowOverheadName** | bool | true |
| **PositionX / PositionY / PositionZ** | 실수 | 0.0 / 0.0 / 0.0 |
| **RotationX / RotationY / RotationZ** | 실수 | 0.0 / 180.0 / 0.0 |
| **SpawnOnStart** | bool | false |
| **DespawnOnScenarioEnd** | bool | true |
| **Interactions** | 배열 | [`patient-a-doctor-submit-laryngoscope`, `patient-a-doctor-submit-et-tube`, `patient-a-doctor-submit-5cc-syringe`, `patient-a-doctor-submit-central-line-set`] |

- [x] actingNpcs-1: 의사 NPC는 시나리오 소유 `actingNpcs`로 선언하고, 그래프 노드에서 명시적으로 소환한다.

---

### [SPAWN_A] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | SPAWN_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **PresetIdentifier** | 문자열 | patient_a |
| **SpawnedEntityIdentifier** | 문자열 | patient_a |
| **PositionSourceEntityIdentifier** | 문자열/null | null |
| **PositionX / PositionY / PositionZ** | 실수 | 0.0 / 0.0 / 0.0 |
| **NextIdentifier** | 문자열 | SPAWN_DOCTOR |

---

### [SPAWN_DOCTOR] EntityPresetSpawnNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | SPAWN_DOCTOR |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.EntityPresetSpawn |
| **ActingNpcIdentifier** | 문자열 | npc-doctor-patient-a-critical |
| **PresetIdentifier** | 문자열/null | null |
| **SpawnedEntityIdentifier** | 문자열/null | null |
| **PositionSourceEntityIdentifier** | 문자열/null | null |
| **PositionX / PositionY / PositionZ** | 실수 | 0.0 / 0.0 / 0.0 |
| **ResultStateKey** | 문자열/null | null |
| **NextIdentifier** | 문자열 | PRESET_A |

- [x] actingNpcs-2: `SPAWN_A -> SPAWN_DOCTOR -> PRESET_A` 순서로 연결해, 의사 NPC가 이후 단계에서 참조되기 전에 먼저 생성되도록 보장한다.

---

### [PRESET_A] PatientMedicalStatePresetNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | PRESET_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.PatientMedicalStatePreset |
| **TargetEntityIdentifier** | 문자열 | patient_a |
| **Sex** | 문자열 | Male |
| **Age** | 정수 | 35 |
| **ConsciousnessGcs** | 정수 | 8 |
| **ConsciousnessLocLabel** | 문자열 | Stupor |
| **ConsciousnessPupillaryResponse** | 문자열 | Normal |
| **RespirationAwRR** | 정수 | 8 |
| **RespirationType** | 문자열 | Irregular |
| **PulseRate** | 정수 | 140 |
| **PulseForceType** | 문자열 | Weak |
| **BloodPressureSystolic** | 정수 | 70 |
| **BloodPressureDiastolic** | 정수 | 40 |
| **SkinColorHue** | 문자열 | Pale |
| **SkinTemperatureType** | 문자열 | Cold |
| **BodyTemperatureCelsius** | 실수 | 35.9 |
| **Spo2** | 정수 | 82 |
| **IsCardiacArrest** | bool | false |
| **NextIdentifier** | 문자열 | D005 |

- [x] d-3: 환자 A 상태 사전설정 값 명시됨 (원본 _origin 및 JSON 일치).
- [x] 체온(BT) 35.9°, SpO2 82% 프리셋 필드 추가(PatientMedicalStatePreset 스키마 확장 반영).

---

## 시나리오 본문

### [D005] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 A를 처치실로 이동해야 합니다. 플레이어 A, B, C, D는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q006 |


---

### [Q006] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Grab_Stretcher |
| **NextIdentifier** | 문자열 | P002 |


---

### [P002] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P002_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | Q006_1 |

#### [P002_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| V010_A | CC_A_grab | NurseA | triage_lead | - | All |
| V010_B | CC_B_grab | NurseB | airway_team | - | All |
| V010_C | CC_C_grab | NurseC | bleeding_control | - | All |
| V010_D | CC_D_grab | NurseD | iv_team | - | All |


---

### [V010_A] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_A_grab |

#### [V010_A_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_patient_a_handle_0 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_a [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].
-> **2027-07-28. AI의 권고에 따라 sig.grab_stretcher_patient_a_handle_0 으로 입력 완료.

---

### [V010_B] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_B_grab |

#### [V010_B_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_patient_a_handle_1 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_b [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].
-> **2027-07-28. AI의 권고에 따라 sig.grab_stretcher_patient_a_handle_1 으로 입력 완료.

---

### [V010_C] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_C_grab |

#### [V010_C_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_patient_a_handle_2 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_c [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].
-> **2027-07-28. AI의 권고에 따라 sig.grab_stretcher_patient_a_handle_2 으로 입력 완료.

---

### [V010_D] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V010_D |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | CC_D_grab |

#### [V010_D_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.grab_stretcher_patient_a_handle_3 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.grab_stretcher_d [들것 잡기(grab 지점 Identifier 정합), spec §5.1~5.3].
-> **2027-07-28. AI의 권고에 따라 sig.grab_stretcher_patient_a_handle_3 으로 입력 완료.

---

### [Q006_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q006_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Grab_Stretcher |
| **NextIdentifier** | 문자열 | E005 |


---

### [E005] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patient_a_to_treatmentroom |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D006 |


---

### [D006] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 활력징후 측정, AVPU 및 GCS 측정, 경추 고정 및 흡인을 시작합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P003 |


---

### [P003] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P003_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D010 |

#### [P003_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N005 | CC_B_vitalcheck_patient_a | NurseB | airway_team | - | All |
| N006 | CC_C_gcs_patient_a | NurseC | neuro_assessment | - | All |
| N007 | CC_D_suction_patient_a | NurseD | suction_team | - | All |


---


<!-- ================= [병렬 브랜치 1] 플레이어 B (활력징후 측정) 흐름 ================= -->

### [N005] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 활력징후를 측정합니다. 활력징후 측정도구를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q007 |


---

### [Q007] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Vital_PatientA |
| **NextIdentifier** | 문자열 | V011 |


---

### [V011] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N005_1 |

#### [V011_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_vital_set |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_vital_set [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2027-07-28 완료


---

### [N005_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N005_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. 활력징후가 모니터에도 출력됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V011_1 |


---

### [V011_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V011_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E006 |

#### [V011_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.show_vital_patient_a |


- [x] (b) **배선 완료(2026-07-28): sig.show_vital_patient_a. `PatientTypeA`의 `assess_vital` 액션에 `_assessSignal=show_vital_patient_a` 정합했고, `PatientController.PerformAssess()` 완료 시 `ScenarioInteractionSignals.Raise("show_vital_patient_a")` 경로를 사용한다. (검증 상태: 미검증)


---

### [E006] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | activate_vital_monitor_ui_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D007 |


---

### [D007] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 환자 활력징후 출력됩니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N005_4 |


---

### [N005_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N005_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 혈압 70/40mmHg, 맥박 140회/분 - 약하고 빠름, 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 8.0 |
| **NextIdentifier** | 문자열 | Q007_1 |


---

### [Q007_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q007_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Vital_PatientA |
| **NextIdentifier** | 문자열 | CC_B_vitalcheck_patient_a |


---


<!-- ================= [병렬 브랜치 2] 플레이어 C (AVPU 및 GCS 사정) 흐름 ================= -->

### [N006] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 클릭해 환자의 의식 상태를 사정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | Q008 |


---

### [Q008] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_PatientA |
| **NextIdentifier** | 문자열 | V012 |


---

### [V012] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N006_1 |

#### [V012_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_avpu_gcs_patient_a |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_avpu_gcs_patient_a [사정(PatientController Assess 자동), spec §5.1~5.3]. **2027-07-28 완료


---

### [N006_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식 상태(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N006_2 |


---

### [N006_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C004 |


---

### [C004] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C004_Options 표 참조]** |

#### [C004_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) |  | #88AAFF | N006_retry_a |
| V(Verbal response, 음성에 반응 있음) |  | #88AAFF | N006_retry_a |
| P(Pain response, 통증에 반응 있음) |  | #88AAFF | N006_3 |
| U(Unconsciousness, 반응 없음) |  | #88AAFF | N006_retry_a |


---

### [N006_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C004 |


---

### [N006_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C005 |


---

### [C005] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C005_Options 표 참조]** |

#### [C005_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적 반응) |  | #88AAFF | N006_retry_b |
| 3점(구두 명령에 반응) |  | #88AAFF | N006_retry_b |
| 2점(통증에 반응) |  | #88AAFF | N006_4 |
| 1점(반응 없음) |  | #88AAFF | N006_retry_b |


---

### [N006_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 통증 자극에만 반응했음을 유의하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C005 |


---

### [N006_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. "여기가 어디예요?"라고 묻자, 환자는 이해할 수 없는 신음소리만 내고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C006 |


---

### [C006] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C006_Options 표 참조]** |

#### [C006_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) |  | #88AAFF | N006_retry_c |
| 4점(혼란) |  | #88AAFF | N006_retry_c |
| 3점(부적절한 답변) |  | #88AAFF | N006_retry_c |
| 2점(신음소리) |  | #88AAFF | N006_5 |
| 1점(반응 없음) |  | #88AAFF | N006_retry_c |


---

### [N006_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 환자는 알아들을 수 없는 소리만 내고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C006 |


---

### [N006_5] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 팔을 재빨리 굽혀 자극을 피합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C007 |


---

### [C007] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C007_Options 표 참조]** |

#### [C007_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 6점(명령 수행) |  | #88AAFF | N006_retry_d |
| 5점(통증 원인을 치우려고 손을 뻗음) |  | #88AAFF | N006_retry_d |
| 4점(통증에 회피) |  | #88AAFF | N006_6 |
| 3점(이상 굴곡) |  | #88AAFF | N006_retry_d |
| 2점(이상 신전) |  | #88AAFF | N006_retry_d |
| 1점(반응 없음) |  | #88AAFF | N006_retry_d |


---

### [N006_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 현재 통증에 회피하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C007 |


---

### [N006_6] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N006_6 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS 측정 완료. E2 / V2 / M4 = 총 8점 (Stupor) 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | D008 |


---

### [D008] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | AVPU 중 P이며, 추가 사정한 GCS 결과 8점 확인했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q008_1 |


---

### [Q008_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q008_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_PatientA |
| **NextIdentifier** | 문자열 | CC_C_gcs_patient_a |


---


<!-- ================= [병렬 브랜치 3] 플레이어 D (경추 고정 및 구강 흡인) 흐름 ================= -->

### [N007] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 기도 확보를 위해 환자의 경추를 고정하고 구강 석션을 진행합니다. 경추고정기, 흡인기, 석션 라인, 앙커 팁을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q009 |


---

### [Q009] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Stabilizer_And_Suction_PatientA |
| **NextIdentifier** | 문자열 | E007 |


---

### [E007] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_suction_checklist_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | V013 |


---

### [V013] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E008 |

#### [V013_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_stabilizer_patient_a |
| Registry | Contains | RuntimeState | sig.click_wall_suction |
| Registry | Contains | RuntimeState | sig.click_suction_line |
| Registry | Contains | RuntimeState | sig.click_yankauer |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A003(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A003 → E008 로 재지정함. 산출물 `yankauer_suction_ready` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 `yankauer_ready` → 정본 `yankauer_suction_ready` 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_wall_suction, sig.click_suction_line, sig.click_yankauer [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2027-07-28 완료

- [x] 목 고정대는 클릭 대상이 아니라 환자에게 사용하는 아이템이다. `cervical_collar` 사용이 기존 환자 표시 상태를 켜고 `sig.apply_stabilizer_patient_a`를 올린다.


---

### [E008] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | hide_suction_checklist_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N007_1 |


---

### [N007_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 경추 고정기를 환자에게 적용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_1 |


---

### [V013_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N007_2 |

#### [V013_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_stabilizer_patient_a |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_stabilizer_patient_a [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].
**검토 필요. 현재 neckstabilizer 혹은 cervical_collar 혹은 stabilizer 아이템이 확인되지 않습니다 

---

### [N007_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 흡인기를 벽에 설치하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_2 |


---

### [V013_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N007_3 |

#### [V013_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_wall_component_1 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_wall_component_1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].
-> **2026-07-28 완료. Wall Attached Wall Suction의 Attach Completion Signal에 connect_wall_component_1 추가함

---

### [N007_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 앙커 팁을 흡인기에 연결하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_3 |


---

### [V013_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N007_4 |

#### [V013_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_wall_component_and_yankauer |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_wall_component_and_yankauer [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].
-> **2026-07-28 완료. 그러나 LineConnectionPoint 연결에 대한 검증 필요

---

### [N007_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N007_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 흡인기를 클릭한 뒤 환자를 클릭해 구강 흡인을 진행하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V013_4 |


---

### [V013_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V013_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D009 |

#### [V013_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.suction_patient_a |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.suction_patient_a [아이템 사용(Item Use Signal), spec §5.1~5.3]. **2026-07-29 확인 완료


---

### [D009] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 경추 고정 및 구강 흡인 완료했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q009_1 |


---

### [Q009_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q009_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Stabilizer_And_Suction_PatientA |
| **NextIdentifier** | 문자열 | CC_D_suction_patient_a |


---


<!-- ================= [병렬 브랜치 종료 및 합류] ================= -->

### [D010] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식상태는 GCS 8점, 활력징후는 혈압 70/40mmHg, 맥박수 140회/분 (빠르고 약함), 호흡수 8회/분, 체온 35.9도, SpO2 82% 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E009 |


---

### [E009] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | vitalinfo_1_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D011 |


---

### [D011] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 기도 확보를 위해 intubation을 시행하겠습니다. 간호사 B 선생님은 보조해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D012 |


---

### [D012] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 그동안 간호사 C 선생님은 멸균장갑을 착용하고 거즈로 출혈부위를 지혈해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D013 |


---

### [D013] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 D 선생님은 수액 투여를 위해 양팔에 IV 라인을 확보해주세요. 혈관을 보고 18게이지로 잡고, 수액은 생리식염수와 플라즈마 솔루션을 연결하겠습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P004 |


---

### [P004] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P004_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D022 |

#### [P004_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N008 | CC_B_intubation_A_oxy_patient_a | NurseB 또는 NurseA | airway_team, triage_lead | - | Any |
| N010 | CC_C_stopbleeding_patient_a | NurseC | bleeding_control | - | All |
| N011 | CC_D_iv_patient_a | NurseD 또는 NurseC | iv_team, access_support | - | Any |


---


<!-- ================= [P004 병렬 브랜치 1] 플레이어 B 또는 A (기관내삽관 및 산소 공급) ================= -->

### [N008] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 기관내삽관에 필요한 물품을 준비합니다. 좌측 체크리스트 창을 참고하여 필요한 물품을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q010 |


---

### [Q010] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Intubation_PatientA |
| **NextIdentifier** | 문자열 | E010 |


---

### [E010] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_checklist_intu |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | V014 |


---

### [V014] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E011 |

#### [V014_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_laryngoscope_blade |
| Registry | Contains | RuntimeState | sig.click_laryngoscope_handle |
| Registry | Contains | RuntimeState | sig.click_endotracheal_tube |
| Registry | Contains | RuntimeState | sig.click_stylet |
| Registry | Contains | RuntimeState | sig.click_plaster |
| Registry | Contains | RuntimeState | sig.click_syringe_5cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A004(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A004 → E011 로 재지정함. 산출물 `laryngoscope` 는 레지스트리 등록 완료, 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 입력 식별자 `laryngo_handle`/`laryngo_blade` → 정본 `laryngoscope_handle`/`laryngoscope_blade` 확정(crafting-recipes.md §확정 요청 [x]).


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_laryngo_blade→click_laryngoscope_blade, click_laryngo_handle→click_laryngoscope_handle, click_et_tube→click_endotracheal_tube (interaction-signal-integration-spec §2 참조).

- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_laryngoscope_blade, sig.click_laryngoscope_handle, sig.click_endotracheal_tube, sig.click_stylet, sig.click_plaster, sig.click_syringe_5cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 확인 완료.


---

### [E011] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | hide_checklist_intu |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N008_1 |


---

### [N008_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 후두경을 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | ISC_PASS_LARYNGOSCOPE |


---

### [ISC_PASS_LARYNGOSCOPE] ItemSubmissionConfigNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | ISC_PASS_LARYNGOSCOPE |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.ItemSubmissionConfig |
| **TargetIdentifier** | 문자열 | patient-a-doctor-submit-laryngoscope |
| **RequiredItems** | 배열 | [{ itemIdentifier: laryngoscope, count: 1 }] |
| **CompletionSignalIdentifier** | 문자열 | sig.pass_laryngoscope |
| **NextIdentifier** | 문자열 | V014_1 |


---

### [V014_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N008_2 |

#### [V014_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_laryngoscope |


- [x] (b) 배선 완료(2026-07-31): `N008_1 -> ISC_PASS_LARYNGOSCOPE -> V014_1`로 전이 체인을 확정했다. `ISC_PASS_LARYNGOSCOPE`는 `patient-a-doctor-submit-laryngoscope` 대상에 `requiredItems(laryngoscope x1)`를 설정하고 제출 완료 시 `sig.pass_laryngoscope`를 발신한다. (검증 상태: 미검증)

---

### [N008_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 기관내관을 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | ISC_PASS_ET_TUBE |


---

### [ISC_PASS_ET_TUBE] ItemSubmissionConfigNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | ISC_PASS_ET_TUBE |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.ItemSubmissionConfig |
| **TargetIdentifier** | 문자열 | patient-a-doctor-submit-et-tube |
| **RequiredItems** | 배열 | [{ itemIdentifier: endotracheal_tube_ready, count: 1 }] |
| **CompletionSignalIdentifier** | 문자열 | sig.pass_et_tube_ready |
| **NextIdentifier** | 문자열 | V014_2 |


---

### [V014_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E012 |

#### [V014_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_et_tube_ready |


- [x] (b) 배선 완료(2026-07-31): `N008_2 -> ISC_PASS_ET_TUBE -> V014_2`로 전이 체인을 확정했다. `ISC_PASS_ET_TUBE`는 `patient-a-doctor-submit-et-tube` 대상에 `requiredItems(endotracheal_tube_ready x1)`를 설정하고 제출 완료 시 `sig.pass_et_tube_ready`를 발신한다. (검증 상태: 미검증)

---

### [E012] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_et_tube |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N008_3 |


---

### [N008_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 구강에 삽입된 기관내관을 클릭해 스타일렛을 제거하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_3 |


---

### [V014_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E013 |

#### [V014_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.remove_intu_stylet |


- [x] (b) 배선 완료(2026-07-28): sig.remove_intu_stylet. `PatientTypeA`의 `EtTubeStyletInteractPoint(ScenarioActionInteractable)`에 `_completionSignal=remove_intu_stylet` 정합했고, `ScenarioActionInteractable.Interact()` 완료 시 Raise 경로를 사용한다. (검증 상태: 미검증)


---

### [E013] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | remove_stylet |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N008_4 |


---

### [N008_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 5cc 주사기를 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | ISC_PASS_SYRINGE |


---

### [ISC_PASS_SYRINGE] ItemSubmissionConfigNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | ISC_PASS_SYRINGE |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.ItemSubmissionConfig |
| **TargetIdentifier** | 문자열 | patient-a-doctor-submit-5cc-syringe |
| **RequiredItems** | 배열 | [{ itemIdentifier: syringe_5cc, count: 1 }] |
| **CompletionSignalIdentifier** | 문자열 | sig.pass_syringe |
| **NextIdentifier** | 문자열 | V014_4 |


---

### [V014_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N008_5 |

#### [V014_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_syringe |


- [x] (b) 배선 완료(2026-07-31): `N008_4 -> ISC_PASS_SYRINGE -> V014_4`로 전이 체인을 확정했다. `ISC_PASS_SYRINGE`는 `patient-a-doctor-submit-5cc-syringe` 대상에 `requiredItems(syringe_5cc x1)`를 설정하고 제출 완료 시 `sig.pass_syringe`를 발신한다. (검증 상태: 미검증)

---

### [N008_5] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N008_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 플라스터를 클릭해 선택한 뒤, 삽입된 기관내관을 고정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V014_5 |


---

### [V014_5] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V014_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | S001 |

#### [V014_5_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_plaster_on_intu |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_plaster_on_intu [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3]. **2026-07-29 완료, 그러나 검증 필요함.


---

### [S001] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | Q010_1 |


---

### [Q010_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q010_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Intubation_PatientA |
| **NextIdentifier** | 문자열 | D014 |


---

### [D014] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 삽입된 깊이 23cm, 기관내관 고정되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D015 |


---

### [D015] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 삽관이 끝났고, 자발호흡이 있으니 간호사 A 선생님이 T-piece를 연결하고 산소 10L를 공급하며 산소포화도를 모니터링해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009 |


---

### [N009] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q011 |


---

### [Q011] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Oxygen_PatientA |
| **NextIdentifier** | 문자열 | V015 |


---

### [V015] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_1 |

#### [V015_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_humidifier_bottle |
| Registry | Contains | RuntimeState | sig.click_sterile_distilled_water |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A006(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A006 → N009_1 로 재지정함. 산출물 `humidifier_sterile_distilled_water_bottle` 레시피는 등록되어 있다(crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 입력 식별자 `sdw` → 정본 `sterile_distilled_water`, `humidifierbottle` → `humidifier_bottle`로 확정(crafting-recipes.md 참조).


- [x] `sig.click_humidifier_bottle`, `sig.click_sterile_distilled_water`는 `MedicalItem.OnGet()`이 아이템 획득 시 자동 발행한다.


---

### [N009_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 유량계를 습득하여 산소 유량계를 완성합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V015_1 |


---

### [V015_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_2 |

#### [V015_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_flowmeter |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A007(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A007 → N009_2 로 재지정함. 산출물 `oxyflowmeter` 레시피는 등록되어 있다(crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 `oxyflowmeter` 단일 식별자로 확정(환자 B·C 공용, `oxyflowmeter_b`/`oxyflowmeter_c` 미분리)(crafting-recipes.md 참조).


- [x] `sig.click_flowmeter`는 `MedicalItem.OnGet()`이 아이템 획득 시 자동 발행한다.


---

### [N009_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V015_2 |


---

### [V015_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_3 |

#### [V015_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_wall_component_2 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_wall_component_2 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3]. **2026-07-29 완료.


---

### [N009_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소줄과 T-Piece를 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V015_3 |


---

### [V015_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_3_1 |

#### [V015_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_o2_line |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_o2_line [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 완료.


---

### [N009_3_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_3_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자에게 삽입된 기관내관을 클릭해 T-piece를 장착하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | V015_3_1 |


---

### [V015_3_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_3_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N009_3_2 |

#### [V015_3_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_tpiece |


- [x] `endotracheal_tube_A` 클릭 지점에 `ScenarioActionInteractable`을 배선하고 completion signal을 `interact_tpiece`로 설정한다. **2026-07-29 완료

- [x] `interact_tpiece` 완료 시 `TPieceSet_A` 시각 오브젝트가 활성화되어야 한다(장착 완료 표현). **2026-07-29 완료


---

### [N009_3_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_3_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 장착된 T-piece와 벽면 유량계를 각각 클릭해 라인을 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | V015_3_2 |


---

### [V015_3_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_3_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E014 |

#### [V015_3_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_tpiece_and_oxyflow |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_tpiece_and_oxyflow [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3]. **2026-07-29 완료

- [ ] `connect_tpiece_and_oxyflow`는 T-piece 측 연결점과 벽 유량계 측 연결점의 실제 연결 완료로 발신되어야 한다.
-> 검증 필요

---

### [E014] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_tpiece_ready |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N009_4 |


---

### [N009_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V015_4 |


---

### [V015_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V015_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | C008 |

#### [V015_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_oxyflow_wall |


- [x] 벽 유량계 `WallAttachedOxyflowmeter`의 Attach Completion Signal을 `interact_oxyflow_wall`로 설정한다. **2026-07-29 완료.
  - 2026-08-15 정정: 설치 완료 시점에 신호가 올라 V015_3_2(라인 연결) 이전에 래치되면 V015_4가 실제 클릭 없이 자동 통과(스킵)하는 문제가 있어, Attach Completion Signal 대신 **설치 상태 상호작용 신호(Attached Interact Signal)** `interact_oxyflow_wall`를 사용한다. 설치된 유량계를 클릭하면 신호만 올리고(첫 1회), 이후 클릭은 기존처럼 회수로 동작한다.


---

### [C008] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C008_Options 표 참조]** |

#### [C008_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 3L |  | #88AAFF | N009_retry |
| 5L |  | #88AAFF | N009_retry |
| 10L |  | #88AAFF | D016 |
| 15L |  | #88AAFF | N009_retry |


---

### [N009_retry] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N009_retry |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 처방은 10L 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | C008 |


---

### [D016] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 산소 투여 시작했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q011_1 |


---

### [Q011_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q011_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Oxygen_PatientA |
| **NextIdentifier** | 문자열 | CC_B_intubation_A_oxy_patient_a |


---


<!-- ================= [P004 병렬 브랜치 2] 플레이어 C (지혈) ================= -->

### [N010] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q012 |


---

### [Q012] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_PatientA |
| **NextIdentifier** | 문자열 | V016 |


---

### [V016] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N010_1 |

#### [V016_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_sterile_gloves |
| Registry | Contains | RuntimeState | sig.click_gauze |
| Registry | Contains | RuntimeState | sig.click_plaster |


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_glove→click_gloves (interaction-signal-integration-spec §2 참조). 이후 아이템 리네임(gloves→sterile_gloves, 2026-08-13)에 따라 click_sterile_gloves 로 재정합.

- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_sterile_gloves, sig.click_gauze, sig.click_plaster [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 완료


---

### [N010_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 멸균장갑을 착용하십시오. E키를 눌러 인벤토리 창을 열고, 좌측 상단의 착용 칸으로 멸균장갑 아이템을 옮기십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V016_1 |


---

### [V016_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N010_2 |

#### [V016_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.wear_glove |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.wear_glove [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3]. **2026-07-29 확인 완료


---

### [N010_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 거즈를 선택한 뒤, 환자에게 적용하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V016_2 |


---

### [V016_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E015 |

#### [V016_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_gauze |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_gauze [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].
-> 검증 필요

---

### [E015] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N010_3 |


---

### [N010_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N010_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 압박을 가해 지혈하고 있습니다. 플라스터를 선택한 뒤, 거즈를 고정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V016_3 |


---

### [V016_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V016_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E016 |

#### [V016_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.apply_plaster_on_gauze |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.apply_plaster_on_gauze [부착형 적용/착용(Item Apply Signal), spec §5.1~5.3].
-> 검증 필요

---

### [E016] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_with_plaster_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | S002 |


---

### [S002] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D017 |


---

### [D017] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 지혈 중입니다. 거즈 고정했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q012_1 |


---

### [Q012_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q012_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_PatientA |
| **NextIdentifier** | 문자열 | CC_C_stopbleeding_patient_a |


---


<!-- ================= [P004 병렬 브랜치 3] 플레이어 D 또는 C (IV 라인 및 C-line 보조) ================= -->

### [N011] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 양쪽 팔에 IV 라인을 순서대로 확보합니다. 먼저 18G 캐뉼라 1개와 준비된 생리식염수 1L, 플라즈마 솔루션 1L 수액백을 획득하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | Q013 |


---

### [Q013] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_IV_Line_PatientA |
| **NextIdentifier** | 문자열 | E017 |


---

### [E017] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_iv_checklist |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | V017 |


---

### [V017] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E018 |

#### [V017_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_cannula_18g |
| Registry | Contains | RuntimeState | sig.click_normal_saline_1000ml |
| Registry | Contains | RuntimeState | sig.click_plasma_solution_1000ml |


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_ns1→click_normal_saline_1000ml, click_ps1→click_plasma_solution_1000ml (interaction-signal-integration-spec §2 참조).

- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_cannula_18g, sig.click_normal_saline_1000ml, sig.click_plasma_solution_1000ml [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 확인 완료

- [x] 개수 정책 확정(2026-07-28): 준비 단계에서는 18G 1개와 수액 2종을 확인한다. 첫 18G는 좌측 삽입 시 소비하고, 두 번째 18G는 `N011_3` 이후 다시 획득해 우측 삽입 시 소비한다. 따라서 `click_18g` 단일 신호를 2회 획득 판정으로 확장하지 않는다.
-> **2026-07-29 click_cannula_18g로 변경함

---

### [E018] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | hide_iv_checklist |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N011_1 |


---

### [N011_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 18게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V017_1 |


---

### [V017_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E019 |

#### [V017_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.insert_iv_patient_a_left |


- [x] (b) 배선 완료(2026-07-20): `sig.insert_iv_patient_a_left` 는 `PatientController.IntravenousLineCannula.PerformIntravenousLineCannulaInsertion` 이 좌측(첫 삽입) 확정 시 발신한다. 우측은 `sig.insert_iv_patient_a_right`(V017_3 게이트) 로 이어진다. 게이지(18G/20G)별 처치 표현(`Syringe{18G|20G}InsertedInto{Left|Right}Arm`)도 함께 켜진다.
- 환자 외부 장비 상태 기술자 `PatientSupportExternalRefs`는 `IntravenousFluids`를 `List<MonoBehaviour>`로 보관하며 index 0/1을 각각 좌측/우측 IV 수액 슬롯으로 사용한다. `IVFluidLeftArm`과 `IVFluidRightArm`은 이 목록을 통해 노출되며, 이는 캐뉼라 삽입 신호의 좌/우 상태와 별개의 연결 상태다.


---

### [E019] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_18g_left |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N011_2 |


---

### [N011_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 생리식염수 1L 수액백을 먼저 수액 걸대에 건 뒤, 수액줄을 좌측 팔의 18G 캐뉼라에 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V017_2 |


---

### [V017_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E020 |

#### [V017_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_cannula_and_ns1 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_cannula_and_ns1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3]. **2026-07-29 완료.
PatientA 프리팹 아래 18g_left의 자식 오브젝트 내에 18g_left_port 오브젝트 추가 및 Intravenous Line Connection Point 컴포넌트 추가 및 Shpere Collider로 연결될 위치에 고정 완료. 환자 베드에 자식 오브젝트로 추가된 ns1의 identifier를 connect_cannula_and_ns1으로, 환자 좌측 팔에 삽입된 18g_left_port의 identifier를 patient_a_cannula_left_port로 입력


---

### [E020] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ns1_left |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | N011_3 |


---

### [N011_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N011_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 좌측 팔의 정맥로가 확보되었습니다. 두 번째 18G 캐뉼라를 획득해 반대쪽 팔에 삽입하고, 플라즈마 솔루션 수액백을 먼저 건 뒤 연결하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V017_3 |


---

### [V017_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V017_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E021 |

#### [V017_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.insert_iv_patient_a_right |


- [x] (b) 배선 완료(2026-07-20): `sig.insert_iv_patient_a_right` 는 `PatientController.IntravenousLineCannula.PerformIntravenousLineCannulaInsertion` 이 우측(두 번째 삽입) 확정 시 발신한다. 좌측은 `sig.insert_iv_patient_a_left`(V017_1 게이트)에서 선행 확인된다. 게이지(18G/20G)별 처치 표현(`Syringe{18G|20G}InsertedInto{Left|Right}Arm`)도 함께 켜진다.


---

### [E021] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_18g_right |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | E022 |


---

### [E022] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ps1_right |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D018 |


---

### [D018] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 양측 정맥로가 모두 확보되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q013_1 |


---

### [Q013_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q013_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_IV_Line_PatientA |
| **NextIdentifier** | 문자열 | D019 |


---

### [D019] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 그래도 혈압이 잡히지 않네요. C-line 잡아서 수액을 빠르게 투여하겠습니다. 간호사 C 선생님, C-line set 건네주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N012 |


---

### [N012] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | C-line set을 클릭해 획득하고, 해당 아이템을 의사에게 전달하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q014 |


---

### [Q014] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cline_Assist |
| **NextIdentifier** | 문자열 | ISC_PASS_CENTRAL_LINE_SET |


---

### [ISC_PASS_CENTRAL_LINE_SET] ItemSubmissionConfigNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | ISC_PASS_CENTRAL_LINE_SET |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.ItemSubmissionConfig |
| **TargetIdentifier** | 문자열 | patient-a-doctor-submit-central-line-set |
| **RequiredItems** | 배열 | [{ itemIdentifier: central_line_set, count: 1 }] |
| **CompletionSignalIdentifier** | 문자열 | sig.pass_central_line_set |
| **NextIdentifier** | 문자열 | V018 |


---

### [V018] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E023 |

#### [V018_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.pass_central_line_set |


- [x] (b) 배선 완료(2026-07-31): `Q014 -> ISC_PASS_CENTRAL_LINE_SET -> V018`로 전이 체인을 확정했다. `ISC_PASS_CENTRAL_LINE_SET`는 `patient-a-doctor-submit-central-line-set` 대상에 `requiredItems(central_line_set x1)`를 설정하고 제출 완료 시 `sig.pass_central_line_set`를 발신한다. (검증 상태: 미검증)

---

### [E023] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_central_line_set |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | Q014_1 |


---

### [Q014_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q014_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cline_Assist |
| **NextIdentifier** | 문자열 | D020 |


---

### [D020] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 C 선생님, Level 1 rapid infuser에 플라즈마 솔루션과 혈액백 연결시켜주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N013 |


---

### [N013] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 플라즈마 솔루션 1L 수액백과 혈액백을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | Q015 |


---

### [Q015] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Lv1_Fluids |
| **NextIdentifier** | 문자열 | V019 |


---

### [V019] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N013_1 |

#### [V019_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_plasma_solution_1000ml |
| Registry | Contains | RuntimeState | sig.click_blood_transfusion_set |


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_blood→click_blood_transfusion_set (interaction-signal-integration-spec §2 참조).

- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_plasma_solution_1000ml, sig.click_blood_transfusion_set [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].


---

### [N013_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N013_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 플라즈마 솔루션 1L 수액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V019_1 |


---

### [V019_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V019_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N014 |

#### [V019_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_ps1_to_lv1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_ps1_to_lv1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [N014] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 혈액백을 클릭해 선택한 뒤, Level 1 rapid infuser와 연결하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V020 |


---

### [V020] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E024 |

#### [V020_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_blood_to_lv1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_blood_to_lv1 [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [E024] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | lv1_ready |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D021 |


---

### [D021] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | Level 1에 플라즈마 솔루션과 혈액백 연결 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q015_1 |


---

### [Q015_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q015_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Lv1_Fluids |
| **NextIdentifier** | 문자열 | CC_D_iv_patient_a |


---


<!-- ================= [P004 병렬 종료 및 환자 악화 시점] ================= -->

### [D022] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 그래도 혈압이 잘 안잡히네요... |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E025 |


---

### [E025] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | patient_crash_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D023 |


---

### [D023] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 심전도만 출력되고, 다른 활력징후가 출력되지 않습니다. 간호사 B 선생님, 환자 맥박 확인해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N016 |


---

### [N016] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 경동맥을 촉지해 맥박을 확인합니다. 목 부위를 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q018 |


---

### [Q018] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse |
| **NextIdentifier** | 문자열 | V022 |


---

### [V022] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q018_1 |

#### [V022_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_pulse_patient_a_r1 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_pulse_patient_a [사정(PatientController Assess 자동), spec §5.1~5.3]. **2026-07-29 완료. 환자 A 프리팹의 Patient Controller 중 Assess Actions로 assess_pulse 추가. 2026-07-30 기존 registryIdentifier였던 sig.check_pulse_patient_a 를 sig.check_pulse_patient_a_r1로 변경. 환자 A 프리팹(PatientTypeA)에 Patient Controller 컴포넌트 내 Assess Action으로 assess_pulse_r1 변경 완료.


---

### [Q018_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q018_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse |
| **NextIdentifier** | 문자열 | D024 |


---

### [D024] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 맥박 없습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D025 |


---

### [D025] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | PEA입니다. CPR 하겠습니다. 제가 팀 리더를 맡겠습니다. 간호사 A 선생님은 앰부백 짜주시고, 간호사 B 선생님은 가슴압박 해주세요. 간호사 C 선생님은 제세동기 연결해주시고, 간호사 D 선생님은 C-line으로 에피네프린 1mg 투여해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P005 |


---

### [P005] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P005_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D028 |

#### [P005_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N017 | CC_A_ambu | NurseA | nurse_a | - | All |
| N018 | CC_B_chestcomp | NurseB | nurse_b | - | All |
| N019 | CC_C_defib | NurseC | nurse_c | - | All |
| N020 | CC_D_epi | NurseD | nurse_d | - | All |


---


<!-- ================= [P005 병렬 브랜치 1] 플레이어 A (앰부백 산소화) ================= -->

### [N017] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백과 산소 저장낭을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q019 |


---

### [Q019] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_A |
| **NextIdentifier** | 문자열 | V023 |


---

### [V023] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N017_1 |

#### [V023_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_ambubag |
| Registry | Contains | RuntimeState | sig.click_reservoir_bag |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_ambubag, sig.click_reservoir_bag [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 확인 완료


---

### [N017_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자에게 연결된 T-piece를 클릭해 연결을 해제하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V023_1 |


---

### [V023_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N017_2 |

#### [V023_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.remove_tpiece |


- [x] (b) 배선 완료(2026-07-28): sig.remove_tpiece. `PatientTypeA`의 `TPieceRemoveInteractPoint(ScenarioActionInteractable)`에 `_completionSignal=remove_tpiece` 정합했고, `ScenarioActionInteractable.Interact()` 완료 시 Raise 경로를 사용한다. (검증 상태: 미검증)


---

### [N017_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백을 클릭해 선택한 뒤, 환자에게 삽입된 기관내관을 클릭해 연결하세요. 이후, 산소줄과 앰부백을 클릭해 연결합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V023_2 |


---

### [V023_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E026 |

#### [V023_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.connect_ambubag |
| Registry | Contains | RuntimeState | sig.connect_o2_to_ambu |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.connect_ambubag, sig.connect_o2_to_ambu [연결지점(IntravenousLineConnectionPoint 자동), spec §5.1~5.3].


---

### [E026] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_ambu_patient_a |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | C009 |


---

### [C009] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C009_Options 표 참조]** |

#### [C009_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| Full |  | #88AAFF | S003 |


---

### [S003] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | oxygen_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | N017_3 |


---

### [N017_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백을 클릭해 산소 공급을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | V023_4 |


---

### [V023_4] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V023_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E027 |

#### [V023_4_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.start_ambu_r1 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.start_ambu [아이템 사용(Item Use Signal), spec §5.1~5.3]. **2026-07-29 확인 완료

- [x] **2026-07-30 sig.start_ambu를 sig.start_ambu_r1로 변경, patient_a_critical.scenario.requirements.json에도 반영함.


---

### [E027] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_ambubagging |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L001 |


---

### [L001] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C010 |


---

### [C010] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 산소 제공량은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C010_Options 표 참조]** |

#### [C010_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 1500ml (다섯 손가락 모두를 이용해 백을 짠다) |  | #88AAFF | N017_retry_a |
| 약 600ml (엄지, 검지, 중지를 이용해 백을 짠다) |  | #88AAFF | L002 |


---

### [N017_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C010 |


---

### [L002] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C011 |


---

### [C011] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 심폐소생술 시 앰부 배깅(ambu-bagging)의 적절한 속도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C011_Options 표 참조]** |

#### [C011_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 10초에 1번 (분당 약 6회) |  | #88AAFF | N017_retry_b |
| 6초에 1번 (분당 약 10회) |  | #88AAFF | Q019_1 |
| 3초에 1번 (분당 약 20회) |  | #88AAFF | N017_retry_b |


---

### [N017_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N017_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 6초에 1번씩 눌러야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C011 |


---

### [Q019_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q019_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_A |
| **NextIdentifier** | 문자열 | CC_A_ambu |


---


<!-- ================= [P005 병렬 브랜치 2] 플레이어 B (가슴 압박) ================= -->

### [N018] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 가슴을 클릭해 가슴압박을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q020 |


---

### [Q020] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_B |
| **NextIdentifier** | 문자열 | V024 |


---

### [V024] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E028 |

#### [V024_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_to_start_comp |


- [x] (b) 배선 완료(2026-07-28): sig.click_to_start_comp. `PatientTypeA`의 `ChestCompStartInteractPoint(ScenarioActionInteractable)`에 `_completionSignal=click_to_start_comp` 정합했고, `ScenarioActionInteractable.Interact()` 완료 시 Raise 경로를 사용한다. (검증 상태: 미검증)
-> **2026-07-29 ChestCompStartInteractPoint 오브젝트를 추가하여 배정함. 그러나 V027노드와 같은 역할을 하기에 중복되지 않는지/겹치지 않는지 검토가 필요함.

---

### [E028] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_chest_compression |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L003 |


---

### [L003] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C012 |


---

### [C012] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 가슴 압박 깊이는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C012_Options 표 참조]** |

#### [C012_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 4cm |  | #88AAFF | N018_retry_a |
| 약 5cm |  | #88AAFF | L004 |
| 약 6cm |  | #88AAFF | N018_retry_a |


---

### [N018_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C012 |


---

### [L004] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C013 |


---

### [C013] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 성인의 정확한 가슴 압박 위치는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C013_Options 표 참조]** |

#### [C013_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 양측 유두선상의 중간지점 |  | #88AAFF | N018_retry_b |
| 흉골 하부 1/2 지점 |  | #88AAFF | L005 |


---

### [N018_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C013 |


---

### [L005] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C014 |


---

### [C014] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 정확한 가슴 압박 횟수는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C014_Options 표 참조]** |

#### [C014_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 분당 약 80~100회 |  | #88AAFF | N018_retry_c |
| 분당 약 100~120회 |  | #88AAFF | L006 |
| 분당 약 120~140회 |  | #88AAFF | N018_retry_c |


---

### [N018_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C014 |


---

### [L006] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C015 |


---

### [C015] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 4. 가슴압박 시 주의사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C015_Options 표 참조]** |

#### [C015_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 지쳐도 한 사람이 계속 가슴압박을 수행한다. |  | #88AAFF | N018_retry_d |
| 뼈가 부러진 것 같으면 멈춘다. |  | #88AAFF | N018_retry_d |
| 충분한 이완을 제공한다. |  | #88AAFF | Q020_1 |


---

### [N018_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N018_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 혈액 순환이 가능합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C015 |


---

### [Q020_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q020_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_B |
| **NextIdentifier** | 문자열 | CC_B_chestcomp |


---


<!-- ================= [P005 병렬 브랜치 3] 플레이어 C (제세동기) ================= -->

### [N019] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 제세동 카트를 환자 옆으로 가져오세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q021 |


---

### [Q021] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_C |
| **NextIdentifier** | 문자열 | V025 |


---

### [V025] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N019_1 |

#### [V025_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.patient_bed_position_reached_defib_cart_a_defibcart_to_patient |


- [x] (b) 배선 완료(2026-07-28): sig.patient_bed_position_reached_defib_cart_a_defibcart_to_patient. `MovingPatientBedController.PublishPositioningPointReached()`의 scoped signal(`patient_bed_position_reached_{mover}_{point}`) 발신 경로를 사용하며, `OverworldScene`의 mover=`defib_cart_a`, point=`defibcart_to_patient` 정합을 확인했다. (검증 상태: 미검증)


---

### [N019_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 제세동 패드를 획득하고, 환자 흉부를 클릭해 부착하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V025_1 |


---

### [V025_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V025_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E029 |

#### [V025_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_defibpad |
| Registry | Contains | RuntimeState | sig.interact_patient_chest |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_defibpad [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 확인 완료

- [x] 환자 A 흉부 부위 collider에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_patient_chest`로 설정한다. 또한 Activate On Interact에 defibpad_midaxillary_A와 defibpad_subclavicle_A를 추가하였다. ** 2026_07-30 작업 완료


---

### [E029] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | attach_defibpad |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | E030 |


---

### [E030] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | defib_ui_irregular |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | D026 |


---

### [D026] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 제세동기 준비가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L007 |


---

### [L007] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C016 |


---

### [C016] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 제세동기는 Sync 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 다음 중 제세동을 실시해야 하는 심전도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C016_Options 표 참조]** |

#### [C016_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| Asystole(무수축) |  | #88AAFF | N019_retry_a |
| PEA(무맥성 전기활동) |  | #88AAFF | N019_retry_a |
| VT(맥박이 있는 심실빈맥) |  | #88AAFF | N019_retry_a |
| VF(심실세동) |  | #88AAFF | L008 |


---

### [N019_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 VF(심실세동) 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C016 |


---

### [L008] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C017 |


---

### [C017] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C017_Options 표 참조]** |

#### [C017_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 150~200J(줄) |  | #88AAFF | L009 |
| 360J(줄) |  | #88AAFF | N019_retry_b |


---

### [N019_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 150~200J(줄)이 정답입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C017 |


---

### [L009] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C018 |


---

### [C018] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 제세동 등 전기충격 시 주의해야 할 사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C018_Options 표 참조]** |

#### [C018_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 꼬인 수액 줄을 풀어준다. |  | #88AAFF | N019_retry_c |
| 의료진이 손을 대어도 괜찮다. |  | #88AAFF | N019_retry_c |
| 의사의 지시가 있을 때에만 실시한다. |  | #88AAFF | N019_retry_c |
| 전기충격 전 모두 환자에게서 떨어지도록 지시한다. |  | #88AAFF | Q021_1 |


---

### [N019_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N019_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 감전되지 않도록 모두가 떨어지도록 지시해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C018 |


---

### [Q021_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q021_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_C |
| **NextIdentifier** | 문자열 | CC_C_defib |


---


<!-- ================= [P005 병렬 브랜치 4] 플레이어 D (에피네프린 투여) ================= -->

### [N020] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q022 |


---

### [Q022] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_D |
| **NextIdentifier** | 문자열 | V026 |


---

### [V026] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N020_1 |

#### [V026_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_epinephrine_ampule |
| Registry | Contains | RuntimeState | sig.click_syringe_5cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A008(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A008 → N020_1 로 재지정함. 산출물 `epinephrine_5cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 구 `epi_ready`/`epinephrine_syringe` → 정본 `epinephrine_5cc_syringe` 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_epi→click_epinephrine_ampule (interaction-signal-integration-spec §2 참조).

- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_epinephrine_ampule, sig.click_syringe_5cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-29 완료


---

### [N020_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | Push용 생리식염수를 준비합니다. 20cc 생리식염수와 20cc 주사기를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V026_1 |


---

### [V026_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N020_2 |

#### [V026_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_normal_saline_20ml |
| Registry | Contains | RuntimeState | sig.click_syringe_20cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A009(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A009 → N020_2 로 재지정함. 산출물 `normal_saline_20cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 구 `ns_20cc(_ready)` 산출물은 명명 규칙 위배로 제거, 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1)로 대체 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] a-1 아이템 식별자 정합(구 md→JSON 정본): click_ns_20cc→click_normal_saline_20ml (interaction-signal-integration-spec §2 참조).

- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_normal_saline_20ml, sig.click_syringe_20cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-30 확인 완료, 검증 필요


---

### [N020_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 인벤토리에서 조합하여 준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V026_2 |


---

### [V026_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D027 |

#### [V026_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_epi_r1 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_epi [아이템 사용(Item Use Signal), spec §5.1~5.3].
-> **2026-07-30 환자 A 프리팹 아래에 주사기 프리팹 추가 완료(epinephrine_5cc_syringe), 검토 및 연결 필요. registryIdentifier를 sig.push_epi_r1으로 수정

> OR 게이트 처리(B, 우선 채택): 에피네프린 주사기는 완제품이 18종(용량·게이지 변형)으로 존재하나,
> 이 게이트는 개별 변형 픽업이 아니라 **사용 시점 대표 시그널 `sig.push_epi`** 로 검사하므로 어떤 변형을
> 조합·투여했든 통과한다(OR 자연 성립). 정책 근거: `interaction-signal-integration-spec.md` §6,
> `crafting-recipes.md` §확정 요청 (*1).

[V026_2, V026_3, V029_2, V029_3에 대한 요구사항: (에피네프린이 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 다음 노드에서 생리식염수가 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 이후 모두 사라짐) 이 과정을 염두에 두고 작업이 필요합니다]

---

### [D027] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 에피네프린 1mg 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N020_3 |


---

### [N020_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V026_3 |


---

### [V026_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V026_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D027_1 |

#### [V026_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_ns_r1 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_ns_r1 [아이템 사용(Item Use Signal), spec §5.1~5.3].
-> **2026-07-30 환자 A 프리팹 아래에 주사기 프리팹 추가 완료(normal_saline_5cc_syringe), 검토 및 연결 필요. 기존 sig.push_ns에서 sig.push_ns_r1으로 수정 (V029_3과 겹치기 때문)

[V026_2, V026_3, V029_2, V029_3에 대한 요구사항: (에피네프린이 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 다음 노드에서 생리식염수가 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 이후 모두 사라짐) 이 과정을 염두에 두고 작업이 필요합니다]

---

### [D027_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D027_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 생리식염수 20cc 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L010 |


---

### [L010] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C019 |


---

### [C019] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C019_Options 표 참조]** |

#### [C019_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 1~2분에 한 번 |  | #88AAFF | N020_retry_a |
| 약 3~5분에 한 번 |  | #88AAFF | L011 |
| 약 5~10분에 한 번 |  | #88AAFF | N020_retry_a |
| 누군가 시킬 때 마다 |  | #88AAFF | N020_retry_a |


---

### [N020_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 에피네프린은 3~5분에 한 번 투여합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C019 |


---

### [L011] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C020 |


---

### [C020] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C020_Options 표 참조]** |

#### [C020_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약물만 주입 |  | #88AAFF | N020_retry_b |
| 약물 주입 후 생리식염수 주입 |  | #88AAFF | N020_retry_b |
| 약물 주입 후 생리식염수 주입, 이후 팔 들어올리기 |  | #88AAFF | Q022_1 |


---

### [N020_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N020_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 심장에 빠르게 도달시키기 위해 생리식염수 주입 후 팔을 들어올려야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C020 |


---

### [Q022_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q022_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_D |
| **NextIdentifier** | 문자열 | CC_D_epi |


---


<!-- ================= [P005 병렬 종료 및 2nd Cycle (P006) 진입] ================= -->

### [D028] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E031 |


---

### [E031] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | stop_ambu_and_comp |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | E032 |


---

### [E032] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | asystole_monitor_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D029 |


---

### [D029] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | Asystole입니다. 가슴압박과 앰부배깅 하시던 간호사 A, B 선생님끼리 교대 후 계속 가슴압박 해주세요. 간호사 C, D 선생님께서도 교대해서 역할을 수행해 주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P006 |


---

### [P006] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P006_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D033 |

#### [P006_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N021 | CC_A_chestcomp | NurseA | nurse_a | - | All |
| N022 | CC_B_ambu | NurseB | nurse_b | - | All |
| N023 | CC_C_epi | NurseC | nurse_c | - | All |
| N024 | CC_D_defib | NurseD | nurse_d | - | All |


---


<!-- ================= [P006 병렬 브랜치 1] 플레이어 A (가슴압박 교대) ================= -->

### [N021] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 가슴을 클릭해 가슴압박을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q023 |


---

### [Q023] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_A |
| **NextIdentifier** | 문자열 | V027 |


---

### [V027] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E033 |

#### [V027_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_chest |


- [x] 환자 A 흉부 압박 위치 collider에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_chest`로 설정한다.
-> **2026-07-30 ChestCompStartInteractPoint_2nd 오브젝트를 추가하여 배정함. 그러나 V024노드와 같은 역할을 하기에 중복되지 않는지/겹치지 않는지 검토가 필요함.


---

### [E033] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_chest_compression |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L012 |


---

### [L012] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C021 |


---

### [C021] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 가슴 압박 깊이는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C021_Options 표 참조]** |

#### [C021_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 4cm |  | #88AAFF | N021_retry_a |
| 약 5cm |  | #88AAFF | L013 |
| 약 6cm |  | #88AAFF | N021_retry_a |


---

### [N021_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 깊이는 약 5cm 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C021 |


---

### [L013] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C022 |


---

### [C022] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 성인의 정확한 가슴 압박 위치는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C022_Options 표 참조]** |

#### [C022_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 양측 유두선상의 중간지점 |  | #88AAFF | N021_retry_b |
| 흉골 하부 1/2 지점 |  | #88AAFF | L014 |


---

### [N021_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 성인의 정확한 가슴 압박 위치는 흉골 하부 1/2 지점입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C022 |


---

### [L014] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C023 |


---

### [C023] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 정확한 가슴 압박 횟수는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C023_Options 표 참조]** |

#### [C023_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 분당 약 80~100회 |  | #88AAFF | N021_retry_c |
| 분당 약 100~120회 |  | #88AAFF | L015 |
| 분당 약 120~140회 |  | #88AAFF | N021_retry_c |


---

### [N021_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 정확한 가슴 압박 횟수는 분당 약 100~120회 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C023 |


---

### [L015] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C024 |


---

### [C024] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 4. 가슴압박 시 주의사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C024_Options 표 참조]** |

#### [C024_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 지쳐도 한 사람이 계속 가슴압박을 수행한다. |  | #88AAFF | N021_retry_d |
| 뼈가 부러진 것 같으면 멈춘다. |  | #88AAFF | N021_retry_d |
| 충분한 이완을 제공한다. |  | #88AAFF | Q023_1 |


---

### [N021_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N021_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 가슴압박 시 누르는 만큼 충분한 이완을 제공해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C024 |


---

### [Q023_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q023_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_ChestComp_A |
| **NextIdentifier** | 문자열 | CC_A_chestcomp |


---


<!-- ================= [P006 병렬 브랜치 2] 플레이어 B (앰부백 산소화 교대) ================= -->

### [N022] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 앰부백을 클릭해 산소 공급을 시작하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q024 |


---

### [Q024] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_B |
| **NextIdentifier** | 문자열 | V028 |


---

### [V028] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E034 |

#### [V028_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.start_ambu_r2 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.start_ambu [아이템 사용(Item Use Signal), spec §5.1~5.3].
-> **2026-07-30 sig.start_ambu를 sig.start_ambu_r1로 변경,그러나 V023_4노드와 같은 역할을 하기에 중복되지 않는지/겹치지 않는지 검토가 필요함.

- [x] **2026-07-30 sig.start_ambu를 sig.start_ambu_r2로 변경, patient_a_critical.scenario.requirements.json에도 반영함.

---

### [E034] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | start_ambubagging |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediately |
| **NextIdentifier** | 문자열 | L016 |


---

### [L016] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L016 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C025 |


---

### [C025] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 성인의 정확한 산소 제공량은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C025_Options 표 참조]** |

#### [C025_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 600ml (엄지, 검지, 중지를 이용해 백을 짠다) |  | #88AAFF | L017 |
| 약 1500ml (다섯 손가락 모두를 이용해 백을 짠다) |  | #88AAFF | N022_retry_a |


---

### [N022_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N022_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. Tidal Volume을 고려해 약 600ml를 제공해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C025 |


---

### [L017] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L017 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C026 |


---

### [C026] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 심폐소생술 중 적절한 속도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C026_Options 표 참조]** |

#### [C026_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 10초에 1번 (분당 약 6회) |  | #88AAFF | N022_retry_b |
| 6초에 1번 (분당 약 10회) |  | #88AAFF | Q024_1 |
| 3초에 1번 (분당 약 20회) |  | #88AAFF | N022_retry_b |


---

### [N022_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N022_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 6초에 1번씩 눌러야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C026 |


---

### [Q024_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q024_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Ambu_B |
| **NextIdentifier** | 문자열 | CC_B_ambu |


---


<!-- ================= [P006 병렬 브랜치 3] 플레이어 C (에피네프린 투여 교대) ================= -->

### [N023] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 에피네프린 투여를 위한 준비를 합니다. 에피네프린 앰퓰과 5cc 주사기를 획득해 약물이 든 주사기를 완성하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q025 |


---

### [Q025] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_C |
| **NextIdentifier** | 문자열 | V029 |


---

### [V029] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N023_1 |

#### [V029_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_epinephrine_ampule |
| Registry | Contains | RuntimeState | sig.click_syringe_5cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A010(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A010 → N023_1 로 재지정함. 산출물 `epinephrine_5cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 산출물명 구 `epi_ready`/`epinephrine_syringe` → 정본 `epinephrine_5cc_syringe` 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_epinephrine_ampule, sig.click_syringe_5cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-30 완료


---

### [N023_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | Push용 생리식염수를 준비합니다. 20cc 생리식염수와 20cc 주사기를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V029_1 |


---

### [V029_1] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D030 |

#### [V029_1_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_normal_saline_20ml |
| Registry | Contains | RuntimeState | sig.click_syringe_20cc |


- [x] 조합은 노드가 아니라 crafting 시스템으로 처리됨. 구 md의 A011(CombineItem)을 제거하고 이 지점의 NextIdentifier를 A011 → D030 로 재지정함. 산출물 `normal_saline_20cc_syringe` 는 레지스트리 등록 완료(2026-07-09), 조합 완료 전제로 진행 (crafting-recipes.md 참조).

- [x] 명명충돌 확정요청: 구 `ns_20cc(_ready)` 산출물 제거 → 규칙적 산출물 `normal_saline_20cc_syringe`(관계 1) 대체 확정(crafting-recipes.md §확정 요청 [x], 2026-07-09).


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_normal_saline_20ml, sig.click_syringe_20cc [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3]. **2026-07-30 완료


---

### [D030] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 에피네프린 첫 투여 시점부터 4분 지났습니다. 간호사 C 선생님, 바로 에피네프린과 생리식염수 20cc 투여해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N023_2 |


---

### [N023_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 준비된 에피네프린 1mg을 클릭해 선택한 뒤, 중심정맥관을 클릭해 투여하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V029_2 |


---

### [V029_2] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D031 |

#### [V029_2_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_epi_r2 |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_epi [아이템 사용(Item Use Signal), spec §5.1~5.3].
-> **2026-07-30 환자 A 프리팹 아래에 주사기 프리팹 추가 완료(epinephrine_5cc_syringe), 검토 및 연결 필요. registryIdentifier를 sig.push_epi_r2으로 수정

[V026_2, V026_3, V029_2, V029_3에 대한 요구사항: (에피네프린이 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 다음 노드에서 생리식염수가 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 이후 모두 사라짐) 이 과정을 염두에 두고 작업이 필요합니다]


> OR 게이트 처리(B, 우선 채택): V026_2 와 동일하게 사용 시점 대표 시그널 `sig.push_epi` 로 검사하여
> 에피네프린 주사기 18종 변형 중 어느 것을 투여해도 통과한다. 정책: `interaction-signal-integration-spec.md` §6.


---

### [D031] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 에피네프린 1mg 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N023_3 |


---

### [N023_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 동일한 방법으로 준비된 생리식염수 20cc를 투여해 루멘 내 잔여 약물을 주입합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V029_3 |


---

### [V029_3] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V029_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D031_1 |

#### [V029_3_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.push_ns_r2 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.push_ns [아이템 사용(Item Use Signal), spec §5.1~5.3].
**2026-07-30 완료. 기존 sig.push_ns에서 sig.push_ns_r2으로 수정 (V026_3과 겹치기 때문)

[V026_2, V026_3, V029_2, V029_3에 대한 요구사항: (에피네프린이 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 다음 노드에서 생리식염수가 담긴 주사기 상호작용 -> 중심정맥관에 나타나고 주입된 것으로 간주 -> 이후 모두 사라짐) 이 과정을 염두에 두고 작업이 필요합니다]

---

### [D031_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D031_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 생리식염수 20cc 투여했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | L018 |


---

### [L018] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L018 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C027 |


---

### [C027] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 심정지 상황에서 에피네프린의 투여 간격은 어떻게 되는가? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C027_Options 표 참조]** |

#### [C027_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약 1~2분에 한 번 |  | #88AAFF | N023_retry_a |
| 약 3~5분에 한 번 |  | #88AAFF | L019 |
| 약 5~10분에 한 번 |  | #88AAFF | N023_retry_a |
| 누군가 시킬 때 마다 |  | #88AAFF | N023_retry_a |


---

### [N023_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 에피네프린은 3~5분에 한 번 투여합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C027 |


---

### [L019] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L019 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C028 |


---

### [C028] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 말초(팔)로 약물을 투여하는 경우, 적절한 투여 절차는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C028_Options 표 참조]** |

#### [C028_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 약물만 주입 |  | #88AAFF | N023_retry_b |
| 약물 주입 후 생리식염수 주입 |  | #88AAFF | N023_retry_b |
| 약물 주입 후 생리식염수 주입, 이후 팔 들어올리기 |  | #88AAFF | Q025_1 |


---

### [N023_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N023_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 심장에 빠르게 도달시키기 위해 생리식염수 주입 후 팔을 들어올려야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C028 |


---

### [Q025_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q025_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Epi_C |
| **NextIdentifier** | 문자열 | CC_C_epi |


---


<!-- ================= [P006 병렬 브랜치 4] 플레이어 D (제세동기 대기) ================= -->

### [N024] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 제세동기를 클릭해 역할을 부여받으세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q026 |


---

### [Q026] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_D |
| **NextIdentifier** | 문자열 | V030 |


---

### [V030] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | S004 |

#### [V030_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.interact_defib |


- [x] 제세동기 collider에 `ScenarioActionInteractable`을 붙이고 completion signal을 `interact_defib`로 설정한다. **2026-07-30 완료


---

### [S004] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | defib_on_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D032 |


---

### [D032] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 제세동기 준비가 완료되었습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | L020 |


---

### [L020] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L020 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C029 |


---

### [C029] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 1. 제세동기는 Sync 버튼을 눌러 Cardioversion을 제공할 수 있습니다. 다음 중 제세동을 실시해야 하는 심전도는? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C029_Options 표 참조]** |

#### [C029_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| Asystole(무수축) |  | #88AAFF | N024_retry_a |
| PEA(무맥성 전기활동) |  | #88AAFF | N024_retry_a |
| VT(맥박이 있는 심실빈맥) |  | #88AAFF | N024_retry_a |
| VF(심실세동) |  | #88AAFF | L021 |


---

### [N024_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 제시된 심전도 중 제세동이 필요한 심전도는 VF(심실세동) 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C029 |


---

### [L021] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L021 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C030 |


---

### [C030] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 2. 이상파형(Biphasic) 제세동기에서 필요한 에너지 양은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C030_Options 표 참조]** |

#### [C030_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 150~200J(줄) |  | #88AAFF | L022 |
| 360J(줄) |  | #88AAFF | N024_retry_b |


---

### [N024_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 150~200J(줄)이 정답입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C030 |


---

### [L022] DelayNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | L022 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Delay |
| **Duration** | ScenarioTimeValue | { "value": 4, "unit": "Seconds" } |
| **WaitUntil** | ScenarioDelayWaitUntil | WaitUntilDone |
| **NextIdentifier** | 문자열 | C031 |


---

### [C031] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 3. 제세동 등 전기충격 시 주의해야 할 사항은? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C031_Options 표 참조]** |

#### [C031_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 꼬인 수액 줄을 풀어준다. |  | #88AAFF | N024_retry_c |
| 의료진이 손을 대어도 괜찮다. |  | #88AAFF | N024_retry_c |
| 의사의 지시가 있을 때에만 실시한다. |  | #88AAFF | N024_retry_c |
| 전기충격 전 모두 환자에게서 떨어지도록 지시한다. |  | #88AAFF | Q026_1 |


---

### [N024_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N024_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 감전되지 않도록 모두가 떨어지도록 지시해야 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C031 |


---

### [Q026_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q026_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Defib_D |
| **NextIdentifier** | 문자열 | CC_D_defib |


---


<!-- ================= [P006 병렬 종료 및 ROSC 확인] ================= -->

### [D033] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 2분 지났습니다. 리듬 확인하겠습니다. 모두 떨어져 주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E035 |


---

### [E035] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | stop_ambu_and_comp |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | E036 |


---

### [E036] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | rosc_monitor_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D034 |


---

### [D034] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | QRS 보입니다. 간호사 A 선생님, 환자 맥박 있는지 확인해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N025 |


---

### [N025] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N025 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 목을 클릭해서 경동맥을 촉지합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q027 |


---

### [Q027] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse_ROSC |
| **NextIdentifier** | 문자열 | V031 |


---

### [V031] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | Q027_1 |

#### [V031_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_pulse_patient_a_r2 |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_pulse_patient_a [사정(PatientController Assess 자동), spec §5.1~5.3]. **2026-07-30 완료. 기존 registryIdentifier인 sig.check_pulse_patient_a에서 sig.check_pulse_patient_a_r2로 변경. 환자 A 프리팹(PatientTypeA)에 Patient Controller 컴포넌트 내 Assess Action으로 assess_pulse_r2 추가 완료.


---

### [Q027_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q027_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_Pulse_ROSC |
| **NextIdentifier** | 문자열 | D035 |


---

### [D035] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 환자 맥박 느껴집니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | D036 |


---

### [D036] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 환자 ROSC 되었습니다. 제가 검사랑 협진 의뢰 할테니 간호사 D 선생님이 의식상태 확인해주세요. 간호사 B 선생님, 의복 제거해서 추가 손상 있는지 사정해주세요. 간호사 A 선생님께서는 다시 분류구역으로 이동해서 환자 분류해주세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | P007 |


---

### [P007] ParallelNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | P007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P007_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | All |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | Reallocation |
| **NextIdentifier** | 문자열 | D037 |

#### [P007_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N026 | CC_A_triagearea | NurseA | triage_lead | - | All |
| N027 | CC_B_cut_patient_a | NurseB | procedure_team | - | All |
| N028 | CC_D_gcs_patient_a_rosc | NurseD | neuro_assessment | - | All |


---


<!-- ================= [P007 병렬 브랜치 1] 플레이어 A (분류 구역 복귀) ================= -->

### [N026] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N026 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 중증도 분류 구역으로 이동하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q028 |


---

### [Q028] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Return_Triage |
| **NextIdentifier** | 문자열 | V032 |


---

### [V032] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | E037 |

#### [V032_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.arrive_triagearea |


- [ ] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.arrive_triagearea [구역 진입(ScenarioTriggerZone), spec §5.1~5.3].
** 보류

---

### [E037] InvokeEventNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | E037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | player_a_move_to_triage |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | Q028_1 |


---

### [Q028_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q028_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Return_Triage |
| **NextIdentifier** | 문자열 | CC_A_triagearea |


---


<!-- ================= [P007 병렬 브랜치 2] 플레이어 B (의복 제거) ================= -->

### [N027] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N027 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 가위를 클릭해 획득하고, 환자를 클릭해 의복을 제거하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q029 |


---

### [Q029] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cut_Clothing |
| **NextIdentifier** | 문자열 | V033 |


---

### [V033] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | S005 |

#### [V033_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.click_scissors |
| Registry | Contains | RuntimeState | sig.remove_patient_clothing |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.click_scissors [아이템 픽업(MedicalItem.OnGet 자동), spec §5.1~5.3].

- [x] (b) 배선 완료(2026-07-28): sig.remove_patient_clothing. `PatientTypeA`의 `PatientClothingCutPoint(ScenarioActionInteractable)`에 `_completionSignal=remove_patient_clothing` 정합했고, `ScenarioActionInteractable.Interact()` 완료 시 Raise 경로를 사용한다. (검증 상태: 미검증)


---

### [S005] SoundNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | S005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | cutting_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | N027_1 |


---

### [N027_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N027_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 추가 외상은 확인되지 않습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q029_1 |


---

### [Q029_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q029_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Cut_Clothing |
| **NextIdentifier** | 문자열 | CC_B_cut_patient_a |


---


<!-- ================= [P007 병렬 브랜치 3] 플레이어 D (의식 상태 사정) ================= -->

### [N028] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 클릭해 환자의 의식 상태를 사정하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | Q030 |


---

### [Q030] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_ROSC |
| **NextIdentifier** | 문자열 | V034 |


---

### [V034] ValidatorNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | V034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | ScenarioValidatorCondition | RegistryContains |
| **WaitForCondition** | bool | true |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | N028_1 |

#### [V034_Rules] 검증 규칙 (RuntimeState 시그널)

| type | condition | registryType | registryIdentifier |
| --- | --- | --- | --- |
| Registry | Contains | RuntimeState | sig.check_gcs_a_rosc |


- [x] (a) 자동 계측 가능 — 에디터 Identifier 정합만 필요: sig.check_gcs_a_rosc [사정(PatientController Assess 자동), spec §5.1~5.3]. **2026-07-30 완료. 환자 A 프리팹 내 Patient Controller 컴포넌트, Assess Actions에 assess_gcs_rosc 추가 완료


---

### [N028_1] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자의 의식 상태를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N028_2 |


---

### [N028_2] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자를 불렀을 때 응답이 없고, 환자의 옆구리를 꼬집었을 때 불편해하며 피하려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C032 |


---

### [C032] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C032_Options 표 참조]** |

#### [C032_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) |  | #88AAFF | N028_retry_a |
| V(Verbal response, 음성에 반응 있음) |  | #88AAFF | N028_retry_a |
| P(Pain response, 통증에 반응 있음) |  | #88AAFF | N028_3 |
| U(Unconsciousness, 반응 없음) |  | #88AAFF | N028_retry_a |


---

### [N028_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 다른 자극에는 반응이 없다가, 통증에 반응을 하고 있습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C032 |


---

### [N028_3] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 옆구리를 꼬집자 잠시 눈을 떴다가 다시 감습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C033 |


---

### [C033] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C033_Options 표 참조]** |

#### [C033_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적) |  | #88AAFF | N028_retry_b |
| 3점(명령) |  | #88AAFF | N028_retry_b |
| 2점(통증) |  | #88AAFF | N028_4 |
| 1점(반응 없음) |  | #88AAFF | N028_retry_b |


---

### [N028_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 통증 자극에만 반응했음을 유의하십시오. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C033 |


---

### [N028_4] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. 현재 기관내삽관이 시행되어있는 상태입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | C034 |


---

### [C034] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C034_Options 표 참조]** |

#### [C034_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) |  | #88AAFF | N028_retry_c |
| 4점(혼란) |  | #88AAFF | N028_retry_c |
| 3점(부적절한 답변) |  | #88AAFF | N028_retry_c |
| 2점(신음소리) |  | #88AAFF | N028_retry_c |
| 1점(반응 없음) |  | #88AAFF | N028_retry_c |
| E(기관삽관) |  | #88AAFF | N028_5 |


---

### [N028_retry_c] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 기관삽관을 하는 경우 E로 처리(표기)합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C034 |


---

### [N028_5] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 손톱 뿌리쪽 피부에 압력을 가하자 움찔거리며 움직이려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C035 |


---

### [C035] ChoiceNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | C035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C035_Options 표 참조]** |

#### [C035_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 6점(명령 수행) |  | #88AAFF | N028_retry_d |
| 5점(통증 원인을 치우려고 손을 뻗음) |  | #88AAFF | N028_retry_d |
| 4점(통증에 회피) |  | #88AAFF | N028_6 |
| 3점(이상 굴곡) |  | #88AAFF | N028_retry_d |
| 2점(이상 신전) |  | #88AAFF | N028_retry_d |
| 1점(반응 없음) |  | #88AAFF | N028_retry_d |


---

### [N028_retry_d] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 오답입니다. 통증으로부터 회피하려 합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C035 |


---

### [N028_6] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | N028_6 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | GCS 측정 완료. E2 / V(E) / M4 = 총 6E점 입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q030_1 |


---

### [Q030_1] QuestControlNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | Q030_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Check_GCS_ROSC |
| **NextIdentifier** | 문자열 | CC_D_gcs_patient_a_rosc |


---


<!-- ================= [시나리오 A 종료] ================= -->

### [D037] DialogueNode

| 속성 | 타입 | 설명 |
| --- | --- | --- |
| **Identifier** | 문자열 | D037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **NextIdentifier** | 문자열 | (end) |


---


## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D037 |
| 종료 연출/설명 | ROSC 이후 신경학적 확인과 전신 노출을 마친 뒤, 검은 화면으로 fade out 되며 "시나리오 A 환자 대응 종료. 흉부외과로 환자를 이관하였습니다." 메시지를 표시하고 독립 종료한다. 다음 시나리오 자동 전환은 없다. `patient_b_c_ct`는 관리자가 별도 실행한다. |

- [x] R9: 종료 노드 = D037, NextIdentifier = (end)/null. JSON과 일치.
