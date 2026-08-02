---
title: "환자 B 의식 확인·역할 분기 설정 가이드"
doc_type: guide
domain: scenario
status: active
updated: 2026-08-02
---

# 환자 B 의식 확인·역할 분기 설정 가이드

이 문서는 `patient_b_c_ct` 시나리오의 환자 B 처치를 Unity Editor에서 실행하기 위해 씬 담당자가 확인해야 할 설정을 설명한다. 데이터 파일과 런타임 코드는 준비되어 있지만, 씬 좌표와 실제 장비 배치는 프로젝트별 씬에 맞춰 지정해야 한다.

## 1. Overworld Initializer 위치 지정

1. `OverworldGameObjectInitializer`가 있는 씬을 연다.
2. 인스펙터에서 다음 네 위치를 실제 트리아지 공간에 맞춰 지정한다.
   - Patient B Spawn Position → `scen_b:patient_spawnpoint_b`
   - Patient C Spawn Position → `scen_b:patient_spawnpoint_c`
   - Patient Dummy D Spawn Position → `scen_b:patient_spawnpoint_dummy_d_a`
   - Triage Arrival Position → `scen_b:quest_arrival_triage_area`
3. 기본값 `(-1, -1, -1)`을 그대로 사용하지 않는다.
4. **Set**을 실행하고 생성된 웨이포인트와 도착 판정 BoxCollider가 통로·벽과 겹치지 않는지 확인한다.
5. 도착 판정 영역은 네 플레이어가 각각 들어올 수 있을 만큼 넓게 조절한다. 각 플레이어 진입 시 `quest_arrival_triage_area_{player-id}` 신호가 발생한다. 이 신호는 진입마다 재발행되며, 실행 중 중복 집계는 퀘스트의 distinct 식별자 판정이 막는다. 따라서 시나리오를 다시 실행해도 같은 플레이어의 도착을 다시 계측할 수 있다.

## 2. 역할과 환자 프리셋 확인

1. 세션에 참여하는 네 플레이어에 `nurse_a`, `nurse_b`, `nurse_c`, `nurse_d` 태그가 정확히 하나씩 부여되는지 확인한다.
2. EntityPreset Registry에 `patient_b`, `patient_c`, `patient_dummy_d_b`가 등록되어 있는지 확인한다.
3. 환자 B/C의 침대 결합 설정은 [환자 B/C EntityPreset 구성 가이드](./patient-b-c-entitypreset-setup-guide.md)를 따른다.
4. 네 역할 중 하나가 없으면 `Parallel(ByRole)`의 `Panic` 정책에 따라 환자 B 처치를 시작하지 않는다.

## 3. 처치 구역과 장비 확인

1. 환자 B/C 목적 구역에 `PatientCareDescriptionZone`과 베드 스냅 포인트가 있어야 한다.
2. 환자가 들어오면 각각 `carezone_patient_entered_patient_b`, `carezone_patient_entered_patient_c` 신호가 발생하는지 확인한다.
3. 환자 B 처치에 쓰는 활력징후 모니터, 산소유량계, 비강 캐뉼라, 거즈, 플라스터와 20G 정맥로/수액 상호작용이 씬 또는 프리셋에 존재해야 한다.
4. 정맥로 계약은 오른팔이며 신호는 `insert_iv_patient_b_right`, 수액 연결은 `connect_cannula_and_ns1_patient_b`이다.

## 4. 의식 확인 입력 설정

`PatientController`가 의식 확인 단계에서만 “말 걸기”, “근력 확인”, “동공반사 확인” 상호작용을 노출한다. 별도 컴포넌트를 수동으로 추가하지 않는다.

의식 확인 상호작용과 마이크 감지는 `nurse_a` 역할 클라이언트에만 활성화된다. 다른 역할의 직접 상호작용이나 조작된 원격 완료 요청도 서버 역할 검증에서 거부된다.

- 기본 게임룰: 마이크 사용 안 함, 상호작용 사용.
- `usability` 데이터팩: 마이크 사용, 상호작용도 함께 사용.
- 마이크를 켜려면 `/gamerule UseMicInRecognitionCheck true`를 사용한다.
- 상호작용을 끄려면 먼저 마이크를 켠 뒤 `/gamerule DisableInteractionInRecognitionCheck true`를 사용한다.
- 입력 경로가 모두 없어지는 `(false, true)` 조합은 거부된다.

앱은 시작 후 마이크 권한을 미리 요청한다. 권한이 없거나 장치가 없으면 상호작용으로 진행한다. 마이크 입력은 RMS 0.02 이상이 1초 유지되면 의식 확인 1~3단계를 완료하며, 4단계는 상호작용만 사용한다.

## 5. 플레이 검증 순서

1. 호스트와 원격 클라이언트 네 명으로 세션을 시작한다.
2. 각 역할이 도착 구역에 들어가 도착 퀘스트가 완료되는지 확인한다.
3. B/C/더미 환자를 분류한다. 일부러 오분류하여 안내 후 분류 상태가 초기화되고 재시도가 가능한지 먼저 확인한다.
4. B/C를 처치 구역으로 이동시키고 두 도착 신호를 확인한다.
5. 환자 B 병렬 처치에서 각 원격 역할에 자기 대화와 선택지만 표시되는지 확인한다. 간호사 C의 활력 UI가 C 클라이언트에서 열리고 닫기 신호가 서버 진행에 반영되는지도 확인한다.
6. 의식 확인은 상호작용 경로와 마이크 경로를 각각 시험한다.
7. 9개 평가 선택의 로그에 평가 식별자, 선택 인덱스, 의도 정답 인덱스, 정답 여부가 남는지 확인한다.
8. 모든 역할 처치가 끝난 뒤 환자 B 완료 안내에서 시나리오가 종료되는지 확인한다.

## 관련 문서

- [환자 B/C/CT 시나리오 정의](../../../requirements/content-definitions/scenario/patient_b_c_ct.md)
- [의식 확인 API 레퍼런스](../../../api-references/TriageTrainer.Patient.RecognitionCheck.md)
- [ScenarioGraph 노드 레퍼런스](../../../api-references/MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md)
