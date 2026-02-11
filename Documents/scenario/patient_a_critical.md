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
| 시작 노드 Identifier | D001 |

## 시나리오 본문

| Identifier | NodeType | 내용(줄글) | NextIdentifier |
|---|---|---|---|
| D001 | Dialogue | 환자 A를 침대에 옮겨 처치실로 이동한다. | E001 |
| E001 | InvokeEvent | EventIdentifier로 move_patient_a_to_treatment를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D002 |
| D002 | Dialogue | 활력징후와 의식상태를 사정한다. 맥박 140, 혈압 70/40, 호흡 8회, SpO2 82%, GCS 8을 확인한다. | E002 |
| E002 | InvokeEvent | EventIdentifier로 assess_vitals_and_gcs_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D003 |
| D003 | Dialogue | A: 기도 확보를 위해 경추 고정 후 구강 석션을 시행한다. | E003 |
| E003 | InvokeEvent | EventIdentifier로 perform_airway_suction_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D004 |
| D004 | Dialogue | 의사 NPC가 삽관 및 지혈, IV 확보 지시를 내린다. | E004 |
| E004 | InvokeEvent | EventIdentifier로 doctor_order_intubation_iv_press_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | P001 |
| P001 | Parallel | 삽관 보조, 출혈 부위 압박, 양팔 IV 라인 확보를 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D005 |
| P001-B1 | ParallelBranch | 브랜치 시작 노드는 E005이며, 플레이어가 의사 NPC의 삽관을 보조(후두경/ET-tube/스타일렛/주사기/플라스터)한다. CompletionConditionIdentifier는 CC_INTUBATION_READY로 서술한다. | CC_INTUBATION_READY |
| P001-B2 | ParallelBranch | 브랜치 시작 노드는 E006이며, 플레이어가 하지 출혈 부위를 직접 압박한다. CompletionConditionIdentifier는 CC_PRESSURE_DONE로 서술한다. | CC_PRESSURE_DONE |
| P001-B3 | ParallelBranch | 브랜치 시작 노드는 E007이며, 플레이어가 양팔 IV 확보 및 수액 연결을 진행한다. CompletionConditionIdentifier는 CC_IV_READY로 서술한다. | CC_IV_READY |
| E005 | InvokeEvent | EventIdentifier로 assist_intubation_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_INTUBATION_READY |
| E006 | InvokeEvent | EventIdentifier로 apply_direct_pressure_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_PRESSURE_DONE |
| E007 | InvokeEvent | EventIdentifier로 establish_iv_and_fluids_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_IV_READY |
| D005 | Dialogue | 혈압이 유지되지 않아 C-line과 대량수액주입기를 연결하고 수액을 투여한다. | E008 |
| E008 | InvokeEvent | EventIdentifier로 connect_c_line_and_rapid_infuser_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D006 |
| D006 | Dialogue | 혈압 저하 후 맥박을 확인하자 PEA가 확인된다. | E009 |
| E009 | InvokeEvent | EventIdentifier로 check_pulse_and_detect_pea_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D007 |
| D007 | Dialogue | 의사 NPC 지시에 따라 CPR을 시작한다. | P002 |
| P002 | Parallel | 가슴압박, 백밸브마스크, 제세동 패드 부착, 에피네프린 준비/투여를 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D008 |
| P002-B1 | ParallelBranch | 브랜치 시작 노드는 E010이며, 플레이어가 가슴압박을 수행한다. CompletionConditionIdentifier는 CC_CPR_COMPRESSION으로 서술한다. | CC_CPR_COMPRESSION |
| P002-B2 | ParallelBranch | 브랜치 시작 노드는 E011이며, 플레이어가 백밸브마스크 산소화를 수행한다. CompletionConditionIdentifier는 CC_BVM_OXY로 서술한다. | CC_BVM_OXY |
| P002-B3 | ParallelBranch | 브랜치 시작 노드는 E012이며, 플레이어가 제세동 패드를 부착한다. CompletionConditionIdentifier는 CC_DEFIB_PAD로 서술한다. | CC_DEFIB_PAD |
| P002-B4 | ParallelBranch | 브랜치 시작 노드는 E013이며, 플레이어가 에피네프린을 준비해 중심정맥관으로 투여한다. CompletionConditionIdentifier는 CC_EPI_GIVEN으로 서술한다. | CC_EPI_GIVEN |
| E010 | InvokeEvent | EventIdentifier로 perform_cpr_compressions_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_CPR_COMPRESSION |
| E011 | InvokeEvent | EventIdentifier로 perform_bvm_oxygenation_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_BVM_OXY |
| E012 | InvokeEvent | EventIdentifier로 apply_defib_pads_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_DEFIB_PAD |
| E013 | InvokeEvent | EventIdentifier로 prepare_and_give_epinephrine_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_EPI_GIVEN |
| D008 | Dialogue | 리듬 확인 결과 Asystole이며 CPR을 지속한다. 이후 QRS가 보이고 맥박이 회복된다(ROSC). | E014 |
| E014 | InvokeEvent | EventIdentifier로 confirm_rosc_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | D009 |
| D009 | Dialogue | D: 신경학적 확인(동공 반응), E: 의복 제거 및 전신 검사 준비를 수행한다. | P003 |
| P003 | Parallel | 동공 반응 확인과 신체 노출을 병행한다. WaitMode는 WaitAll로 서술한다. AllocationType은 ByRole로 서술한다. | D010 |
| P003-B1 | ParallelBranch | 브랜치 시작 노드는 E015이며, 플레이어가 동공 반응을 확인한다. CompletionConditionIdentifier는 CC_PUPIL_CHECK로 서술한다. | CC_PUPIL_CHECK |
| P003-B2 | ParallelBranch | 브랜치 시작 노드는 E016이며, 플레이어가 의복 제거로 전신 검사를 준비한다. CompletionConditionIdentifier는 CC_EXPOSURE_DONE로 서술한다. | CC_EXPOSURE_DONE |
| E015 | InvokeEvent | EventIdentifier로 check_pupil_response_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_PUPIL_CHECK |
| E016 | InvokeEvent | EventIdentifier로 perform_exposure_a를 호출한다. MoveNextBehavior는 WaitUntilDone으로 서술한다. | CC_EXPOSURE_DONE |
| D010 | Dialogue | “해당 환자 대응 종료. 흉부외과로 환자를 이관하였습니다.” 메시지를 표시한다. | (end) |

## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D010 |
| 종료 연출/설명 | ROSC 이후 신경학적 확인과 전신 노출을 마친 뒤 종료된다. |
