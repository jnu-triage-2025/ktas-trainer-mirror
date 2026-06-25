# 평가 보고서: 변환된 시나리오 JSON이 최초 기획을 처리할 수 있는가

- 작성일: 2026-06-25
- 평가 대상
  - 원본 기획: `Documents/requirements/content-definitions/scenario/_origin/시뮬레이션 사례 + 평가 루브릭 (4차 수정).txt`
  - 중간 산출물(마크다운): `patient_a_critical.md`, `patient_b_c_ct.md`
  - 게임 시스템이 로드하는 최종 산출물(JSON):
    - `Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.json`
    - `Assets/Modules/TriageTrainer/Resources/Scenario/patient_b_c_ct.json`
- 엔진: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`
- 관련 제안: 본 디렉터리의 `Feature Proposal - Scenario Validator Gate Timeout.md`

---

## 0. 결론 요약

1. **JSON 그래프 자체는 건전하다.** 두 파일 모두 진짜 댕글링 참조(존재하지 않는 노드로의 연결) 0건, 도달 불가 노드 0건, retry 루프·병렬 수렴 라벨 정상. 노드 타입은 전부 엔진이 처리 경로를 가진 타입으로만 구성되어 있다.
2. **그러나 "최초 기획(평가 루브릭)의 핵심 의도 = 필수 처치를 수행해야만 진행"은 현재 JSON+엔진 조합으로 실현되지 않는다.** 두 가지 상반된 위험이 동시에 존재한다.
   - (A) 신호가 배선된 게이트라면 `waitForCondition=true`로 인해 **신호가 없을 때 영구 정지(hang)** 한다(엔진에 타임아웃·실패 분기 없음).
   - (B) 실제로는 84개 신호 중 대부분이 **게임플레이에서 한 번도 Raise되지 않으므로**, 미배선 게이트가 첫 진행 차단 지점이 되어 세션이 멈춘다.
3. **문서(spec/flags)와 엔진 코드 사이에 사실 불일치가 있다.** `interaction-signal-integration-spec.md` §0과 두 `*.unsupported.flags.json`은 "신호가 없으면 게이트는 그냥 통과(Advance)"라고 기술하지만, 실제 엔진은 통과시키지 않고 무한 대기한다. 같은 spec의 §0.1은 이 hang을 올바르게 인정하고 있어 문서가 자기모순 상태다.
4. **본 디렉터리의 제안서(Validator 게이트 타임아웃·실패 분기)는 상황에 적절하다.** 위 (A)의 직접 원인(`ScenarioController.cs:1224-1228`)을 정확히 겨냥하며, spec §0.1이 명시한 선행 조건(G-6)과 일치하고, 하위호환을 유지한다. 제안 작업을 진행할 가치가 있다. 단, 본 평가가 식별한 추가 갭(아래 §2, §3)은 제안서 범위 밖이므로 별도 후속이 필요하다.

---

## 1. JSON 무결성 평가 (양호)

| 항목 | patient_a_critical | patient_b_c_ct |
|---|---|---|
| 노드 저장 | `nodes` 맵 | `nodes` 맵 |
| 시작 노드(JSON) | `SPAWN_A` (→ MD 시작 `D005`) | `SPAWN_B` (→ MD 시작 `E038`) |
| 총 노드 | 345 | 222 |
| 진짜 댕글링 참조 | 0 | 0 |
| 도달 불가 노드 | 0 | 0 |
| 실제 종료 노드 | `D037` (MD 일치) | `N092` (MD 본문 일치) |
| Validator | 57개 (전부 `onFailure=Ignore`, `waitForCondition=true`) | 47개 (동일) |

- `CC_*` 참조(A 42건 / B·C 20건)는 댕글링이 아니라 엔진이 의도적으로 사용하는 **병렬 브랜치 수렴 라벨**이다(`ScenarioController.cs`의 `ExecuteBranch`/`RunBranchChain`). 각 라벨은 해당 브랜치 체인의 종료 노드 `nextIdentifier`와 1:1로 일치한다.
- Choice retry 루프(AVPU/GCS/근력/CPR 퀴즈 등) 전부 해당 Choice로 되돌아오도록 올바르게 배선됨.
- 노드 타입 분포상 엔진 미지원 타입은 없음.

→ **순수 그래프 관점에서 JSON은 기획의 흐름(분기·재응시·병렬 협력·종료)을 표현하는 데 결함이 없다.**

---

## 2. 기획 의도 실현 가능성 평가 (미흡)

### 2.1 (치명) `waitForCondition=true` 게이트의 무한 대기

- `ScenarioController.ExecuteValidatorNode`는 `WaitForCondition`이 참이면 `yield return new WaitUntil(() => EvaluateValidator(node))` 후 `Advance()` 하고 그대로 `yield break` 한다(`ScenarioController.cs:1224-1228`). 이 분기에서는 `OnFailure`/`FailureNextIdentifier`를 **전혀 참조하지 않으며 타임아웃도 없다.** 브랜치 내부 게이트(`ExecuteValidatorGate`, `:1274-1278`)도 동일하다.
- 두 시나리오의 Validator 104개가 전부 `waitForCondition=true`이므로, 해당 조건 신호가 끝내 오지 않으면 그 지점에서 세션 전체가 정지한다.
- 따라서 spec/flags의 "신호 없으면 통과"는 **사실과 다르다**. 통과가 아니라 정지다.

### 2.2 (치명) 신호 배선 부재 — 게이트를 풀 신호가 거의 올라오지 않음

- 게임플레이가 Validator 신호(`sig.*`)를 Raise하는 지점은 실질적으로 **3곳뿐**이다.
  - 아이템 획득: `MedicalItem.OnGet` → `sig.<id>` / `sig.click_<id>`.
  - 연결: `IntravenousLineConnectionService`(IV/산소/벽/모니터 연결 완료).
  - 환자 인터랙션: `PatientController.RaisePatientInteractionSignals`(선택 클릭/들것 들기/이송).
- 그 외 카테고리 — **적용(apply_*), 착용(wear_glove), 삽입(insert_iv_*), 주입(push_*), 흡인(suction_*), 제거(remove_*), NPC 전달(pass_*), 사정(check_*), 모니터 닫기(close_vital_ui_*), 가슴압박/제세동/유량계 클릭, 구역 진입(enter_triage_zone/arrive_triagearea)** 등 — 은 게임플레이 로직 자체가 없어 신호가 절대 올라가지 않는다(`flags.json`의 `notWired_gameplayMissing` 목록과 코드 실측 일치).
- 결과: 실제 플레이에서는 **첫 미배선 게이트에서 멈춘다.** 현재 미배선 구간을 넘기는 유일한 실용 수단은 개발자 커맨드 `/scenario signal <cond>`(`CommandDefinition.Scenario.cs`)로 신호를 수동 주입하는 것뿐이다.

### 2.3 (중대) 다인 협력 = 병렬 노드의 기본값이 1인 데모에서 세션 종료

- `ParallelNode`(ByRole, WaitAll)는 각 브랜치를 자격(`requiredPlayerTags`)에 맞는 **서로 다른** 플레이어에게 1:1 배정한다(`ScenarioController.cs:1574-1638`).
- 자격을 충족하는 플레이어가 부족해 미배정 브랜치가 생기면 `whenBranchingPlayerNotMatched` 정책에 위임되는데, **기본값이 `Panic`**(`ScenarioParallelNode.cs:32`)이라 미지정 시 `EndScenario()`로 시나리오가 종료된다.
- 즉 4인(또는 2인 그룹) 협력을 전제로 한 병렬 구간은 **인원/태그가 충족되지 않으면 데모가 중단**된다. JSON의 `whenBranchingPlayerNotMatched`가 비어 있어 기본값(Panic)이 적용되는지 반드시 확인해야 한다(§4 체크리스트).

### 2.4 기획서가 요구하나 시스템이 표현/실행하지 못하는 항목

원본 루브릭/마크다운에는 있으나 현재 엔진 또는 신호 계층이 처리하지 못하는 대표 항목:

- **가슴압박 흉곽 움직임·깍지 손 모션, 백밸브 누르는 모션, 펜라이트 반응 모션, 동공 반응**: 원본이 "구현 중요 ★"로 강조한 시각 표현. 신호/이벤트로 일부 UI는 띄우나 인터랙션 완료 계측은 미구현(`apply_*`, `check_pulse_*`, `pupil_reflex_*`의 후속 신호 없음).
- **CPR 2분 × 2사이클, 역할 교대(A↔B, C↔D)**: 마크다운/JSON에는 병렬·Choice로 표현되어 있으나, "동시 수행" 타이밍과 미수행 판정의 강제는 §2.1/§2.2 때문에 실효성이 없다.
- **PlayerMove/NPCMove/CameraTarget**: 엔진에 case는 있으나 실제 이동·카메라 제어가 미구현 스텁(`ScenarioController.cs:1088-1096, 1138-1146, 1158-1162`). 환자/플레이어 이동 연출은 InvokeEvent 핸들러(예: `move_patientA_to_treatmentroom`)에 의존하며, 이동 노드 자체는 시간 대기만 한다.
- **평가 기록(루브릭 수행/미수행 판정)**: 미수행을 기록할 훅(G-3)이 없다. 제안서가 "타임아웃을 평가 기록으로 남긴다"고 했으나 기록 수신부는 아직 별개 미구현.

---

## 3. 시스템 구현 미비점 (개발 후속 대상)

| # | 미비점 | 근거 | 영향 | 후속 |
|---|---|---|---|---|
| S-1 | `waitForCondition=true` 게이트 타임아웃·실패 분기 부재 | `ScenarioController.cs:1224-1228`, `1274-1278` | 미배선/미설정 게이트에서 영구 hang | **본 제안서로 해결 예정** |
| S-2 | 신호 Raise 지점이 3곳뿐(적용/삽입/주입/흡인/사정/전달/진입 미구현) | `interaction-signal-integration-spec.md §0,§2` + 코드 실측 | 핵심 처치 게이트가 통과 불가 | 신호 배선 또는 선행 게임플레이 구현 (별도) |
| S-3 | 병렬 `whenBranchingPlayerNotMatched` 정책 | `ScenarioParallelNode.cs:32`, `ScenarioController.cs:1628-1804` | (해결) 두 JSON 11개 Parallel은 원래 `Ignore`였음(Panic 위험 없음). 단 `Ignore`는 인원 부족 시 처치 브랜치를 조용히 스킵(수행 누락) | **해결: 2026-06-25 11개 전부 `Reallocation`으로 변경** → 인원 무관 모든 처치 브랜치 실행. 변환규칙 문서화 |
| S-4 | PlayerMove/NPCMove/CameraTarget 실효 미구현(스텁) | `ScenarioController.cs:1088-1096,1138-1146,1158-1162` | 이동/카메라 연출이 시간 대기로만 처리, CameraTarget은 조건식이 반전되어 진입조차 안 함 | 인프라 구현 또는 이벤트 위임 명문화 |
| S-5 | 평가 기록 훅(G-3) 미구현 | spec §0.1(3) | 루브릭 "수행/미수행" 판정 근거 미생성 | 제안서 채택 후 이벤트 수신부 구현 |
| S-6 | 문서-코드 불일치: "신호 없으면 통과" 서술 | spec §0(L26-27), 두 flags.json `status`/`conversionState` | 운영자가 데모가 끝까지 진행된다고 오해 | 문서 정정 (§5) |
| S-7 | `disaster_intro.unsupported.flags.json` 구 스키마 잔존 | 파일 내 `todo.validate.*`/폐기 조합신호 기록 | 분류가 현행 JSON/코드와 불일치 | 재작성 또는 폐기 표기 |
| S-8 | Validator `TargetCount` 카운트 소스 불일치 | Parallel은 `ServerManager.Clients`(`:1495`), Validator PlayerCount는 `ClientManager.Clients`(`:1855`) | 서버/호스트 컨텍스트에서 인원 판정 어긋날 수 있음 | 카운트 소스 일원화 검토 |

---

## 4. 인간 작업자가 직접 확인해야 할 항목 (체크리스트)

> 아래는 코드/AI가 자동 판정할 수 없거나, 임상·운영 판단이 필요하거나, Unity 에디터 인스펙터에서 사람이 설정·검수해야 하는 항목이다.

### 4.1 임상/콘텐츠 검수
- [ ] **(TODO-DATA-6) 환자 B 활력징후 정규화 확정**: 원본 기본정보(체온 37.8℃)와 변환 본문(체온 37.3℃, SpO2 93%)이 다르다. 임상적으로 어느 값이 맞는지 확정.
- [ ] **환자 A 체온 35.9℃**: 원본 기본정보에는 체온이 없는데 변환본은 35.9℃를 추가했다(N005_4/D010). 의도된 값인지 확인.
- [ ] **AVPU/GCS/근력 정답 및 오답 해설 문구**의 임상 정확성(C004~C008, C039~C043, C045~C050 등) 검수.
- [ ] **KTAS 정답(긴급=2, 더미=비응급 5)** 분류 기준이 기관 프로토콜과 일치하는지 검수.
- [ ] 산소 10L, 에피네프린 1mg, 18G/20G 캐뉼라 등 **수치·용량·기구 선택의 정답** 검수.

### 4.2 문서 정합성 (사람 확정 필요)
- [ ] **(TODO-DATA-5) 종료 노드 표기**: `patient_b_c_ct.md` 「종료 조건」 표가 종료 노드를 `D063`으로 적었으나 실제(JSON·MD 본문) 종료 노드는 `N092`다. 원저자 확인 후 표를 `N092`로 정정.
- [ ] `interaction-signal-integration-spec.md` §0의 "신호 없으면 통과(Advance)" 서술을 **"무한 대기(hang)"**로 정정(§0.1과 정합). 두 `*.unsupported.flags.json`의 동일 서술도 정정.
- [ ] spec §0.1이 참조하는 제안서 경로 `Agents/Proposals/스케줄됨/...`가 실제 폴더명 `Agents/Proposals/scheduled/...`와 다르다. 경로 표기 정정.

### 4.3 Unity 에디터 설정 검수 (배선 전제 — 미설정 시 hang)
- [ ] **의료 아이템 프리팹 Identifier ↔ `sig.click_<id>` 1:1 정합**: gloves, endotracheal_tube, plasma_solution_1000ml, electrode, penlight, 20g 등. (flags.json은 2026-06-25 기준 일치라 기록 → 현 시점 재확인)
- [ ] **연결 지점(`IntravenousLineConnectionPoint`) Identifier**를 조건명으로 설정: connect_cannula_and_ns1(_b/_c), connect_wall_component_1/2, connect_wall_component_and_yankauer, connect_patient_and_monitor_b/c, connect_nasal_and_o2 등.
- [ ] **환자/더미 `PatientController` Identifier**: `patient_a` / `patient_b` / `patient_c` / `dummy_b` 로 지정(click_/select_/grab_ 게이트 전제).
- [ ] **웨이포인트 존재 확인**: `wp_treatment_room`, `wp_treatment_area`, `wp_ct_room`.
- [ ] **사운드 클립 배치**: `S001`의 `tape_sound` 등 `Resources/Sound/<id>` 존재(없으면 스킵되지만 연출 누락).

### 4.4 멀티플레이 실행 검수
- [x] **병렬 구간 인원/태그 매칭**: (해결, 2026-06-25) 두 시나리오 11개 `ParallelNode`의 `whenBranchingPlayerNotMatched`를 모두 `Reallocation`으로 명시 → 인원 부족 시에도 모든 처치 브랜치가 실행됨(라운드로빈 재배정, 태그 자격은 미적용). 4인 완전체 + 엄격 역할 분리가 필요한 평가 모드를 원하면 해당 노드만 `Ignore`로 되돌릴 수 있음.
- [ ] 플레이어 4인에게 **태그(triage_lead, airway_team, bleeding_control, iv_team 등)**가 실제로 부여되는 경로가 있는지(역할 선택 단계). 없으면 ByRole 배정 실패.
- [ ] **Choice 선택의 동기화**: 현 ChoiceNode는 서버 권한이 아닌 로컬 처리(`ScenarioController`는 MonoBehaviour, `SelectOption`에 RPC 없음). 다인 환경에서 누가 선택하면 어떻게 동기화되는지 실측 확인.

### 4.5 엔드-투-엔드 플레이 검증
- [ ] `/scenario signal <cond>`로 각 게이트를 순차 통과시키며 **두 시나리오를 처음부터 끝까지(D037 / N092) 완주** 가능한지 1회 검증.
- [ ] 신호 수동 주입 없이 순수 게임플레이만으로 어디까지 진행되는지(=첫 hang 지점) 기록.

---

## 5. 제안서 적절성 판단

본 디렉터리의 `Feature Proposal - Scenario Validator Gate Timeout.md`는 **적절하며 진행 가치가 있다.**

- 근거 정확성: 제안서가 지목한 무한 대기 원인(`ExecuteValidatorNode`/`ExecuteValidatorGate`의 `waitForCondition` 분기에서 OnFailure 미처리)은 `ScenarioController.cs:1224-1228, 1274-1278`에서 그대로 확인됨.
- 설계 정합성: spec §0.1이 G-2(수행 강제)의 **선행 조건(G-6)**으로 이 기능을 지목하고 있어 로드맵과 일치.
- 안전성: 기본값 `KeepWaiting`(기존 동작 유지)으로 하위호환을 보장 → 기존 리그레션 JSON 영향 없음. `MultiplayerInfrastructure` 재사용 전제와도 부합.

다만 제안서만으로는 기획 실현이 완성되지 않는다. **제안서는 (A) hang 위험만 제거**하며, 실제 "수행해야 진행"을 작동시키려면 §3의 **S-2(신호 배선)**, **S-3(병렬 정책)**, **S-5(평가 기록)**가 별도로 필요하다. 따라서 본 제안 채택과 병행하여 위 후속 항목들을 단계적으로 추진할 것을 권고한다.
