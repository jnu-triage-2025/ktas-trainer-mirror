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

| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D001 | Dialogue | 환자 B/C를 처치 구역으로 이동시킨다. | E001 |
| E001 | InvokeEvent | EventIdentifier로 move_patient_b_to_treatment를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| D002 | Dialogue | 의식상태(GCS)와 활력징후를 사정한다. 의식 기면, 호흡 24회, SpO2 93%, 맥박 120, 혈압 140/86, 체온 37.8을 확인한다. | E002 |
| E002 | InvokeEvent | EventIdentifier로 assess_vitals_and_gcs_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D003 |
| D003 | Dialogue | 산소화를 위해 비강 캐뉼라를 연결한다. | E003 |
| E003 | InvokeEvent | EventIdentifier로 apply_nasal_cannula_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D004 |
| D004 | Dialogue | 의사 NPC가 “동공 반사 확인, 산소 3L, 지혈, IV 확보”를 지시한다. | E004 |
| E004 | InvokeEvent | EventIdentifier로 doctor_order_neuro_oxygen_hemostasis_iv_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | P001 |
| P001 | Parallel | 산소 3L 조절과 지혈/IV 확보, 동공 반응 확인을 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D005 |
| P001-B1 | ParallelBranch | 브랜치 시작 노드는 E005이며, 산소 3L 조절과 붕대 적용 및 IV 확보/수액 연결을 수행한다. CompletionConditionIdentifier는 CC_OXY_IV_BANDAGE로 서술한다. | CC_OXY_IV_BANDAGE |
| P001-B2 | ParallelBranch | 브랜치 시작 노드는 E006이며, 펜라이트로 동공 반응을 확인하고 의사에게 보고한다. CompletionConditionIdentifier는 CC_PUPIL_REPORT로 서술한다. | CC_PUPIL_REPORT |
| E005 | InvokeEvent | EventIdentifier로 perform_oxygen_hemostasis_iv_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_OXY_IV_BANDAGE |
| E006 | InvokeEvent | EventIdentifier로 check_pupil_and_report_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_PUPIL_REPORT |
| D005 | Dialogue | 의사 NPC가 “뇌손상 의심, Brain CT 진행”을 지시한다. | E007 |
| E007 | InvokeEvent | EventIdentifier로 order_brain_ct_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D006 |
| D006 | Dialogue | 플레이어 A와 C가 함께 베드를 잡고 CT실로 이동한다. | E008 |
| E008 | InvokeEvent | EventIdentifier로 move_to_ct_room_b를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D007 |
| D007 | Dialogue | CT실 도달로 시나리오를 종료한다. | (end) |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D007 |
| 종료 연출/설명 | CT실 도달 시 종료된다. |
