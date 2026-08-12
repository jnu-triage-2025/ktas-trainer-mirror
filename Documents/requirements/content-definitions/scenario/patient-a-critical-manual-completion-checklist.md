# 환자 A 시나리오 — 사람 작업자 완료 체크리스트

## 목적

아래 항목은 코드와 JSON만으로 안전하게 완료할 수 없다. Unity 씬·프리팹의 실제 오브젝트 참조, 애니메이션, 의료 교육 의도 또는 다중 플레이 검증이 필요하다. 각 항목은 작업자가 Play Mode에서 완료 기준까지 확인해야 “해결됨”으로 변경할 수 있다.

## Unity YAML 참조 반영 현황 (2026-08-12)

| 체크리스트 | YAML 반영 | 반영 내용 | 남은 사람 작업 |
|---|---|---|---|
| 1. 체크리스트 UI | 불가 | 세 주요 씬에 할당 가능한 흡인·기관삽관·IV 패널 객체가 없음 | UI 제작·배치 후 참조 할당 |
| 2. SPAWN_A 자식 | 부분 완료 | `PatientTypeA.prefab`의 enum 기반 `ChildGameObjects`에 처치 결과 객체가 이미 직렬화되어 있음을 확인 | 단계별 표시 정책 확정과 Play Mode 검수 |
| 4. 거즈·플라스터 | 완료(참조) | `IndevScene`, `OverworldScene` bootstrap을 환자 A의 `GauzeOnPatient_A`, `GauzeWithPlaster_A` 인스턴스에 연결 | 순서·신호·네트워크 검수 |
| 5. 양측 18G | 부분 완료(참조) | 두 씬 bootstrap을 환자 A의 `18g_left`, `18g_right` 인스턴스에 연결 | NS/PS 라인 표현 제작·배선 및 물리 연결 검수 |
| 6. 중심정맥관 | 완료(참조) | 두 씬 bootstrap을 환자 A의 `Cline_A` 인스턴스에 연결 | 제출 인원 정책과 네트워크 검수 |
| 7. Level 1 | 불가 | 표시 자식과 `IntravenousLineConnectionPoint` 대상 컴포넌트가 프리팹에 없음 | 모델·포트 객체를 먼저 제작 |
| 8. 앰부백 | 부분 완료(참조) | 두 씬 bootstrap을 환자 A의 `Ambu_ready_A` 인스턴스에 연결 | 실제 양 끝 연결점과 신호 검수 |
| 9. 앰부배깅 | 불가 | `IsAmbuBagging` 파라미터를 가진 Animator/Controller가 없음 | 애니메이션 제작 후 배열 할당 |

`IngameScene`에는 환자 A 프리팹 인스턴스가 없으므로 런타임에 생성될 객체를 씬 YAML의 직접 참조로 지정할 수 없다. 따라서 해당 씬의 환자 A 처치 표현 필드는 비워 두고 identifier 기반 런타임 탐색을 유지한다. 위 “완료(참조)”는 직렬화 배선만 완료됐다는 뜻이며, 각 절의 최종 완료 기준을 충족했다는 뜻은 아니다.

## 1. 체크리스트 UI 배선 — E007, E025 등

담당: Unity UI 작업자 + 시나리오 QA

현재 `OverworldScene`, `IngameScene`, `IndevScene`의 `TriageScenarioEventBootstrap`에서 `_suctionChecklistUiPanel`, `_ivChecklistUiPanel` 등이 `None`이다.

작업 방법:

1. 실제 플레이에 사용하는 씬을 열고 `TriageScenarioEventBootstrap` 오브젝트를 선택한다.
2. 흡인·기관삽관·IV 체크리스트용 UI GameObject를 각각 대응 필드에 할당한다.
3. UI는 씬 시작 시 비활성 상태로 두고, `show_*` 이벤트에서만 활성화되며 `hide_*` 이벤트에서 다시 비활성화되는지 확인한다.
4. E025 `patient_crash_ui`가 별도 경고 패널을 뜻하는지, 모니터 수치 변경만을 뜻하는지 기획자와 확정한다. 별도 패널이 필요하면 직렬화 필드와 이벤트 handler를 추가 배선한다.

완료 기준: E007/E025 진입 프레임에 의도한 UI가 한 번 나타나고, 종료 이벤트 직후 사라지며, 호스트와 모든 클라이언트에서 같은 상태를 보인다.

## 2. SPAWN_A 단계별 자식 오브젝트 목록 확정

담당: 환자 프리팹 작업자 + 시나리오 기획자

현재 코드는 확인된 제세동 패드와 주사기 표현만 초기 숨김 처리한다. 나머지 환자 A 자식이 어느 이벤트에서 보여야 하는지는 완전한 목록이 없다.

YAML 확인 결과: `PatientTypeA.prefab`의 `PatientTypeAState.treatmentDisplayState.ChildGameObjects`에는 좌·우 18G, 중심정맥관, 후두경, 기관내관 2단계, T-piece, 앰부백, 거즈, 거즈+플라스터, 경추보호대가 enum 항목별로 이미 연결되어 있다. 이 기존 참조는 유지했으며, 표시 시점 정책만 아직 수동 확정 대상이다.

작업 방법:

1. `PatientTypeA.prefab`을 Prefab Mode로 열어 환자 본체를 제외한 의료 장비·처치 결과 자식을 모두 목록화한다.
2. 각 자식에 대해 `초기 표시`, `표시 이벤트`, `숨김/리셋 이벤트`를 표로 확정한다.
3. 가능하면 이름 문자열보다 `PatientTreatmentDisplayStateABC.ChildGameObjects`의 enum 기반 참조로 편입한다.
4. 별도 단계 표현은 이벤트 bootstrap의 직렬화 참조 또는 명시적인 stage-display 컴포넌트로 배선한다.

완료 기준: SPAWN_A 직후 다음 단계 장비가 보이지 않고, 각 이벤트 전후 스크린샷에서 정해진 표현만 바뀐다. 프리팹 자식 이름 변경에도 동작이 깨지지 않아야 한다.

## 3. V015 산소벽·T-piece 물리 연결

담당: 장비 프리팹 작업자 + 인터랙션 프로그래머

작업 방법:

1. 벽 산소 구성품 양쪽에 실제 연결 컴포넌트와 접근 가능한 Collider를 배치한다.
2. 두 번째 벽 구성품 연결 완료 시에만 `sig.connect_wall_component_2`를 발생시킨다.
3. 산소줄과 T-piece의 획득을 각각 검증할지 기획자가 결정한다. 둘 다 필요하면 `click_o2_line`, `click_tpiece` 두 규칙으로 분리하고 JSON Validator를 갱신한다.
4. T-piece와 유량계의 양 끝 연결점 identifier를 `connect_tpiece_and_oxyflow` 명세와 일치시키고 좌클릭 연결을 지원한다.
5. V015_4의 유량계 조작은 단순 연결 신호와 별도의 `interact_oxyflow_wall` 완료 동작으로 구현한다.

완료 기준: 역순·빈손·잘못된 연결점으로는 신호가 발생하지 않고, 올바른 순서의 물리 연결 1회당 대응 신호가 정확히 한 번 발생한다.

## 4. V016 장갑·거즈·플라스터 지혈 UX

담당: 의료 UX 기획자 + 환자 프리팹 작업자

YAML 반영: `IndevScene`과 `OverworldScene`의 `_patientAGauzeVisual`, `_patientAGauzeWithPlasterVisual`을 환자 A 프리팹의 기존 처치 결과 인스턴스에 연결했다. 참조 할당 단계는 완료됐고 상호작용 순서와 네트워크 동기화 검수는 남아 있다.

작업 방법:

1. 멸균장갑의 우클릭 동작을 `선택/내려놓기`로 유지할지 `착용`으로 바꿀지 확정한다. 착용이라면 인벤토리 소비, 손 상태 표시, `sig.wear_glove`를 하나의 완료 처리로 묶는다.
2. 환자 A의 실제 출혈 부위에 item-use target 또는 환부 전용 상호작용 지점을 배치한다.
3. 거즈 적용 전에는 플라스터 적용을 막고, 거즈 후에만 플라스터 메뉴가 나타나게 한다.
4. 거즈와 플라스터 표현 GameObject를 `GauzePatchedOnThorax`, `GauzeDressingDoneOnThorax`에 정확히 연결한다.

완료 기준: 장갑 → 거즈 → 플라스터 순서만 통과하며 각각 `wear_glove`, `apply_gauze`, `apply_plaster_on_gauze`가 한 번 발생하고 시각 표현도 단계별로 남는다.

## 5. V017 양측 18G와 두 수액 라인

담당: IV 프리팹 작업자 + 네트워크 상호작용 프로그래머

YAML 반영: `IndevScene`과 `OverworldScene`의 `_patientA18gLeftVisual`, `_patientA18gRightVisual`을 환자 A 프리팹의 좌·우 18G 인스턴스에 각각 연결했다. `_patientANs1LeftConnectedVisual`, `_patientAPs1RightConnectedVisual`은 대응하는 라인 결과 객체가 없어 미할당 상태를 유지한다.

작업 방법:

1. 플레이 구간에 18G 캐뉼라를 두 번 획득할 수 있게 배치하거나 첫 삽입 후 두 번째를 스폰한다.
2. 환자 A 좌·우 팔에 서로 구분되는 삽입 지점과 메뉴를 배치하고 `insert_iv_patient_a_left/right` 신호를 연결한다.
3. 수액 걸대에 생리식염수와 플라즈마 백을 먼저 거는 동작을 구현한다.
4. 각 수액줄과 각 팔 캐뉼라의 물리 연결점을 배선하고 좌·우 신호가 교차 발생하지 않게 한다.
5. 호스트와 원격 클라이언트가 각각 연결을 수행해 소유권·상태 동기화를 확인한다.

완료 기준: 한 개 캐뉼라를 양팔에 중복 사용할 수 없고, 실제 라인이 연결되기 전에는 V017_2/V017_3이 통과하지 않는다.

## 6. 중심정맥관 제출과 E023 표현

담당: 멀티플레이 QA + 환자 프리팹 작업자

YAML 반영: `IndevScene`과 `OverworldScene`의 `_patientACentralLineVisual`을 환자 A 프리팹의 `Cline_A` 인스턴스에 연결했다. E023 표현 참조 할당은 완료됐고 제출 인원·신호·네트워크 검수는 남아 있다.

작업 방법:

1. `ISC_PASS_CENTRAL_LINE_SET` 제출 지점을 한 명이 수행 가능한지 기획 명세와 비교한다.
2. 1인 수행이 의도라면 한 클라이언트가 세트를 들고 의사 제출 지점에 전달해 `sig.pass_central_line_set`이 발생하는지 확인한다.
3. 협업이 의도라면 필요한 역할·인원·동시 행동을 UI에 명시하고 2인 테스트 케이스로 고정한다.
4. E023 시점에는 중심정맥관 결과 표현만 나타나도록 환자 A의 관련 자식 참조를 정리한다.

완료 기준: 명시된 인원으로 처음부터 끝까지 진행 가능하고, 제출 전후 환자 표현과 신호가 정확히 일치한다.

## 7. Level 1 Rapid Infuser 프리팹 완성

담당: 장비 프리팹 작업자

현재 `level1_rapid_infuser.prefab`의 `_normalSalineDisplay`, `_plasmaSolutionDisplay`, `_bloodTransfusionSetDisplay`, `_ivConnectionPoint`가 할당되지 않았다. 코드는 품목 소비와 신호를 처리하지만 시각적·물리적 완성 상태는 아니다.

작업 방법:

1. 장비 모델 아래에 플라즈마 및 수혈세트 연결 후 표시할 GameObject를 만들거나 기존 모델을 지정한다.
2. 각 표시 객체를 controller의 display 필드에 할당하고 초기 비활성화한다.
3. 환자 또는 라인과 연결할 `IntravenousLineConnectionPoint`를 장비의 실제 포트 위치에 추가해 `_ivConnectionPoint`에 할당한다.
4. 플라즈마 연결 전에는 수혈세트 메뉴가 나타나지 않는지 확인한다.
5. 저장→복원 후 플라즈마·수혈세트 표시와 연결 상태가 유지되는지 확인한다.

완료 기준: 플라즈마 후 수혈세트 순서로 모델이 바뀌며 V019_1/V020이 각각 한 번 통과하고, 재접속한 클라이언트도 같은 상태를 본다.

## 8. V023 앰부백·산소 연결

담당: 기도 장비 프리팹 작업자 + 인터랙션 프로그래머

YAML 반영: `IndevScene`과 `OverworldScene`의 `_patientAAmbuConnectedVisual`을 환자 A 프리팹의 `Ambu_ready_A` 인스턴스에 연결했다. 이는 연결 완료 후 표시 참조만 해결하며, 기관내관·산소줄의 물리 연결점은 별도 작업이다.

작업 방법:

1. T-piece 제거 뒤 기관내관 끝에 앰부백을 연결하는 메뉴와 연결점을 추가한다.
2. 앰부백의 산소 유입 포트와 산소줄에 별도 연결점을 배치한다.
3. `sig.connect_ambubag`과 `sig.connect_o2_to_ambu`를 각각 실제 양 끝 연결 완료 콜백에서 발생시킨다.
4. 크로스헤어 좌클릭, 거리 제한, 잘못된 포트 거부를 확인한다.

완료 기준: 두 물리 연결을 모두 하지 않으면 V023_2가 통과하지 않으며, T-piece 제거 메뉴와 앰부백 연결 메뉴가 동시에 모순되게 노출되지 않는다.

## 9. C009 선택지와 E027 앰부배깅 연출

담당: 교육 기획자 + 애니메이터 + 카메라/입력 프로그래머

현재 모든 주요 씬의 `_ambuBaggingAnimators` 배열이 비어 있다.

작업 방법:

1. C009를 확인 버튼 성격의 단일 선택으로 유지할지, 평가 문항으로 만들고 오답을 추가할지 결정한다.
2. 앰부백·환자 또는 수행자 Animator에 `IsAmbuBagging` 파라미터와 실제 반복 애니메이션을 만든다.
3. 해당 Animator들을 씬의 `_ambuBaggingAnimators`에 할당한다.
4. E027 시작 시 플레이어 이동·시점 입력을 잠그고 연출 카메라로 전환하며, 종료 이벤트에서 반드시 원래 입력과 카메라를 복원한다.
5. 중도 이탈, 시나리오 중단, 네트워크 disconnect에서도 입력 잠금이 남지 않는 정리 경로를 확인한다.

완료 기준: E027 동안 애니메이션과 카메라가 고정되고 플레이어가 임의 이동할 수 없으며, 종료 후 즉시 정상 조작으로 복귀한다.

## 10. V025 제세동 카트 실제 이동

담당: 네트워크 이동 프로그래머 + 멀티플레이 QA

작업 방법:

1. 카트 상호작용이 단순 신호 발신이 아니라 조종/밀기 상태로 전환되는지 확인한다.
2. 목적지 trigger는 카트 root 또는 권위 있는 NetworkObject의 실제 위치 진입만 인정하도록 한다.
3. 클릭만 하고 이동하지 않은 경우 `patient_bed_position_reached_defib_cart_a_defibcart_to_patient`가 발생하지 않는지 확인한다.
4. 원격 플레이어가 이동할 때 위치와 완료 판정이 서버에서 일치하는지 확인한다.

완료 기준: 카트가 지정 위치에 도달하기 전에는 V025가 통과하지 않고, 도달 후 모든 피어에서 동일 위치와 완료 상태를 본다.

## 11. 전체 시나리오 사람 검수

담당: 최소 2명의 QA + 의료 내용 검수자 1명

권장 절차:

1. 새 세션에서 RuntimeState 신호를 초기화하고 t03부터 t09까지 중간 노드 스킵 없이 진행한다.
2. 한 명은 플레이하고 다른 한 명은 노드 identifier, 발생 신호, UI 상태, 오브젝트 상태를 기록한다.
3. 같은 절차를 호스트 수행과 원격 클라이언트 수행으로 바꿔 반복한다.
4. 의료 검수자는 안내 문구와 실제 수행 순서, 용어, 장비 조합이 교육 의도에 맞는지 승인한다.
5. 각 실패는 “입력 불가 / 메뉴 없음 / 신호 없음 / 신호 조기 발생 / UI 잔존 / 네트워크 불일치” 중 하나로 분류한다.

최종 완료 기준: t03~t09를 호스트와 원격 클라이언트가 각각 한 번씩 막힘 없이 완주하고, 모든 Validator 신호가 실제 조작 이후 정확히 한 번 발생하며, 다음 단계 UI와 오브젝트만 표시된다.
