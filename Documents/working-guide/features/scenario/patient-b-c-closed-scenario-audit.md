---
title: "patient_b_c_ct 닫힌 플레이 감사"
doc_type: audit
domain: scenario
status: active
updated: 2026-07-29
---

# patient_b_c_ct 닫힌 플레이 감사

이 문서는 이전 점검 결과를 그대로 신뢰하지 않고 저장소의 JSON, 코드, 프리팹,
ScriptableObject, 씬 직렬화 데이터를 다시 대조한 결과다. 여기서 **완료**는 파일에
구성이 존재한다는 뜻이고, **검증 필요**는 플레이 모드에서 실제 동작까지 아직 증명되지
않았다는 뜻이다.

## 판정 요약

| 항목 | 판정 | 근거 |
|---|---|---|
| 시나리오 JSON 구조 | 완료 | `SPAWN_B` 진입점, 232개 node key, 종료 이벤트 존재 |
| 이벤트 핸들러 | 완료 | JSON의 21개 `eventIdentifier`가 모두 `Register(...)`와 일치 |
| 퀘스트 definition | 완료 | 12개 Quest identifier가 quest 파일에 정의됨 |
| 환자 B/C 프리팹 핵심 컴포넌트 | 완료 | 두 프리팹에 NetworkObject, PatientController, CapsuleCollider 존재 |
| FishNet spawnable 등록 | 완료 | 두 프리팹 GUID가 `DefaultPrefabObjects.asset`에 존재 |
| `patient_b`/`patient_c` preset | 완료 | EntityPreset Requirements SO에 베드 결합 포함 등록 |
| `dummy_b` preset | 미완료 | JSON은 spawn하지만 EntityPreset Requirements SO와 프리팹 등록 근거가 없음 |
| B/C 씬 연출 참조 | 검증 필요 | IngameScene의 B/C 오브젝트·UI·침대·CT·간호사 Transform 참조가 비어 있음 |
| 트리아지 도착 counter | 미완료 | 씬에서 `enter_triage_zone_{id}` producer 설정을 찾지 못함 |
| GCS/활력/얼굴/더미 상호작용 | 미완료 또는 매핑 검증 필요 | 요구 신호 producer 및 프리팹 Identifier 매핑이 확인되지 않음 |
| 환자 C 부상 시각물 정합 | 불일치 | JSON 바인딩은 좌측 상완이나 C 프리팹의 치료 표시 지원은 우측 상완임 |

## 이전 점검 주장에 대한 재판정

### 사실이 아니었던 주장

- `PatientTypeBMale.prefab`와 `PatientTypeBFemale.prefab`에 NetworkObject와
  PatientController가 없다는 주장은 현재 저장소와 불일치한다.
- 두 프리팹이 FishNet DefaultPrefabObjects에 등록되지 않았다는 주장은 사실이 아니다.
- `patient_b`와 `patient_c` EntityPreset이 없다는 주장은 사실이 아니다.
- 이벤트 핸들러가 등록되지 않았다는 문제는 확인되지 않았다.
- 퀘스트 definition이 비어 있다는 문제는 현재 quest 파일 기준으로 해결되어 있다.

### 현재도 유효한 문제

#### 1. `dummy_b` spawn 계약이 닫히지 않음

`patient_b_c_ct.scenario.json`은 `SPAWN_DUMMY_B`에서 `dummy_b`를 요구한다.
그러나 [EntityPreset Registry Requirements SO](../../../../Assets/Modules/TriageTrainer/ScriptableObjects/EntityPreset%20Registry%20Requirements%20SO.asset)
에는 `dummy_b` entry가 없고, 저장소에서 전용 프리팹도 확인되지 않는다.

필요한 작업:

- 분류용 더미 프리팹 확정
- NetworkObject/필요한 상호작용 컴포넌트 구성
- `dummy_b` EntityPreset 등록
- `click_dummy_b` producer 연결
- 호스트와 원격 클라이언트 spawn 검증

#### 2. 씬 연출 연결이 파일상 증명되지 않음

[IngameScene.unity](../../../../Assets/Scenes/IngameScene.unity)의
`TriageScenarioEventBootstrap`에서 다음 B/C 참조가 모두 `{fileID: 0}`이다.

- patient B/C/dummy 오브젝트 및 spawn point
- 처치 침대와 처치 구역 point
- B/C vital monitor, controller, panel
- B/C pupil reflex UI와 indicator
- CT room point와 fade panel
- 거즈/플라스터/IV 시각물
- Nurse A/B/C/D Transform

코드의 Registry/alias fallback이 있으므로 “반드시 런타임 실패”라고 단정할 수는 없다.
다만 정적 파일만으로는 실제 대상이 해석된다는 증거가 없으므로 Production 플레이 전에
Registry snapshot과 `Validate Event Wiring`으로 확인해야 한다.

#### 3. 트리아지 도착 신호 설정이 없음

JSON의 `V039`는 `sig.all_triage_patients_arrived`를 기다리고, counter는
`enter_triage_zone_` prefix와 threshold 3을 사용한다. 하지만 씬 직렬화에서
`ScenarioTriggerZone`의 `_perEntitySignalTemplate` 또는 이에 준하는
`enter_triage_zone_{id}` 설정을 확인하지 못했다.

필수 대상은 다음 세 개다.

- `enter_triage_zone_patient_b`
- `enter_triage_zone_patient_c`
- `enter_triage_zone_dummy_b`

#### 4. 상호작용 신호 producer/Identifier가 닫히지 않음

JSON은 53개의 고유 `sig.*` 신호를 참조한다. 현재 코드·문서 감사에서 다음 계열은
실제 플레이 동작 또는 에디터 매핑을 추가로 증명해야 한다.

- `check_gcs_patient_b/c`
- `check_vital_patient_b/c`
- `click_patient_b_face`, `click_patient_c_face`
- `click_dummy_b`
- `click_patient_b`, `click_patient_c`
- `select_patient_b`, `select_patient_c`
- 베드 손잡이 4개 신호

반면 `click_humidifier_bottle`, `click_sterile_distilled_water`,
`click_flowmeter`는 `MedicalItem.OnGet()` 경로가 있으므로 “producer 미구현”으로
분류하면 안 된다. 다만 실제 아이템 Identifier와 신호 이름의 정합은 별도 확인한다.

#### 5. 무한 대기 게이트

47개 Validator가 `waitForCondition=true`다. 그중 일부만 120초 timeout과
`ForceAdvance`를 가진다. 다음 게이트들은 timeout이 없으므로 producer가 누락되면
영구 대기할 수 있다.

- 초기 환자/더미 클릭 및 도착 게이트
- 베드 손잡이 게이트
- 전극·모니터 연결·모니터 닫기
- 펜라이트와 얼굴 평가
- IV 연결 전후의 아이템/장비 게이트
- wall suction·산소·장갑·플라스터 게이트

정상 학습 경로로 유지할 게이트와 timeout 복구를 허용할 게이트를 설계 문서에서
각각 확정해야 한다.

#### 6. 환자 C 부상 정합

문서와 JSON의 상태 바인딩은 C의 거즈/플라스터를 `LeftArm`으로 사용한다.
그러나 [PatientTypeBFemale.prefab](../../../../Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab)의
`DisplaySupports`는 `RightArm` 거즈/플라스터를 제공한다.

이는 “현재 JSON에 C의 무릎 부상이 남아 있다”는 뜻은 아니다. JSON에는 부상 부위가
직접 저장되지 않는다. 실제 문제는 임상 저작, state prefab의 시각물, binding event key가
서로 다른 방향을 가리킨다는 점이다. 좌측 상완으로 확정할 경우 C 프리팹과 근력 사정
대사까지 함께 정리해야 한다.

## 완료로 기록할 수 있는 항목

- B/C 프리팹의 NetworkObject, PatientController, CapsuleCollider
- FishNet spawnable prefab 등록
- `patient_b`/`patient_c` preset 및 `bed_b`/`bed_c` unwrap+link
- 21개 이벤트 핸들러 등록
- 12개 quest definition
- PRESET_B/PRESET_C의 체온 37.8°C, SpO2 93%
- 환자별 거즈·플라스터·비강캐뉼라·wall suction·oxyflowmeter 바인딩
- N092 이후 `fade_out_patient_b_c` 종료 이벤트
- `nurse_a`~`nurse_d` 역할 태그와 다섯 Parallel의 태그 구조

## 닫힌 플레이 승인 조건

다음 조건을 모두 만족하기 전에는 `patient_b_c_ct.scenario.json`을 Production용
완료본으로 승인하지 않는다.

1. `dummy_b` preset과 spawn 결과가 호스트/원격에서 확인될 것
2. B/C의 Registry 및 Bootstrap 참조가 모두 해석될 것
3. 트리아지 세 엔티티의 per-entity signal이 counter를 통과할 것
4. 모든 필수 Validator의 gameplay producer와 Identifier 매핑이 증명될 것
5. C의 좌우 부상·동공·근력·IV 시각물과 대사가 임상적으로 일치할 것
6. 미배선 게이트가 0개이거나, 각 게이트에 승인된 timeout 복구가 있을 것
7. Requirements Production 검증에서 unresolved Error가 0개일 것

