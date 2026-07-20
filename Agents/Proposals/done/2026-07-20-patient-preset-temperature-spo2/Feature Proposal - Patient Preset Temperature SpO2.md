### 개요

`PatientMedicalStatePreset` 프리셋 노드에 **심부 체온(체온, °C)** 과 **산소포화도(SpO2, %)** 필드를 추가한다. 이 두 값은 이미 `PatientMedicalState` 모델(`bodyTemperature.celsius`, `numerics.spo2`/`pleth.spo2`)과 환자 상태 모니터에 존재하고 렌더링되고 있었으나, 시나리오 JSON 프리셋으로는 설정할 수 없었다. 본 변경으로 시나리오 작성자가 다른 vital(GCS/호흡수/맥박/혈압 등)과 동일한 방식으로 체온·SpO2 초기값과 점진 전이(Gradual)를 선언할 수 있다.

이 문서는 함께 수행한 **미구현 항목 갭 분석**과, 이번 범위에서 제외한 **연기 항목 로드맵**도 함께 기록한다.

### 해결하려는 문제 상황

시나리오 작성자로서, 환자 A/B/C의 원본(_origin) 활력값 중 체온과 SpO2를 프리셋 노드에 넣고 싶다. 왜냐하면 현재는 활력 UI 이벤트(`activate_vital_monitor_ui_*`)로만 표기되고 프리셋 정본(scenario.json)에는 값이 누락되어 있어(요구 문서 `PRESET-BC-1`), 환자 상태 모델과 문서/모니터 표기가 불일치하기 때문이다.

- 환자 A: 체온 35.9°, SpO2 82%
- 환자 B/C: 체온 37.8°, SpO2 93% (원본 R7/e-1 기준)

### 사용자 경험 목표

프리셋 노드에 `bodyTemperatureCelsius`, `spo2` 를 기입하면, 적용 시 환자 상태 모니터의 체온(T1)·SpO2 수치가 즉시(또는 Gradual 전이로 점진적으로) 반영된다. 값 미기입(null)은 현재 값 유지, `-1`은 측정 불가(`-?-`)로 기존 규약과 동일하게 동작한다.

### 제안 (구현 완료)

기존 프리셋 4-place 계약 + RPC + 스키마에 두 필드를 추가했다. 두 값은 **수치(Numeric) 프리셋 필드**로 취급하여 즉시 적용·Gradual 보간·RPC 전파·모니터 브리지를 모두 지원한다.

변경 파일 (MultiplayerInfrastructure):
1. `ScenarioPatientMedicalStatePresetNode` — `BodyTemperatureCelsius`(float?), `Spo2`(int?)
2. `ScenarioPatientMedicalStatePresetNodeDTO` — `bodyTemperatureCelsius`, `spo2`
3. `ScenarioGraphLoader.ConvertPatientMedicalStatePreset` / `ConvertToDTO` — 왕복 매핑
4. `Resources/Schema/scenario.schema.json` — `bodyTemperatureCelsius`(number), `spo2`(integer)

변경 파일 (TriageTrainer):
5. `PatientController.MedicalState.cs`
   - `ApplyNumericPresetFields`: 체온을 `bodyTemperature.celsius` 에 적용
   - `BridgeNumericVitalsToMonitor`: 체온 → `temperature.t1`, SpO2 → `numerics.spo2` + `pleth.spo2`
   - `GradualNumericTransitionRoutine`: 체온(float, 반올림 없음)·SpO2(int) 보간 추가
   - `RpcSyncVitalMedicalState`: 파라미터 `bodyTemperatureCelsius`(float, 값 없음=NaN), `spo2`(int, 값 없음=PresetSentinelNone) 추가 및 클라이언트 적용

시나리오 정본:
6. 환자 A/B/C 프리셋 노드(markdown + scenario.json)에 값 반영

### 자세한 달성 목표

- `PatientDescriptor` 는 변경하지 않는다. 체온·SpO2는 의료 상태(`PatientMedicalState`)에 속하며 descriptor에는 원래 없다(올바른 위치).
- 모니터 소비 경로와 일치: `PatientMonitorController` 는 체온을 `temperature.t1`, SpO2를 `numerics.spo2`(폴백 `pleth.spo2`)에서 읽는다.
- `-1`(측정 불가)은 `PatientMedicalState.MonitorValueUnavailable` 로 매핑되어 `-?-` 표시.
- 체온은 float 이므로 RPC에서 별도 float 파라미터로 전송하고 "값 없음"을 `NaN` 으로 인코딩(int 센티넬 재사용 불가).

### 가용성과 테스트

- 즉시 적용 / Gradual 전이 / 측정 불가(-1) / 값 미기입(null) 조합에서 모니터 체온·SpO2가 기대대로 표시되는지 확인.
- 기존 프리셋 JSON 역직렬화 및 schema 검증이 유지되는지 확인(추가 필드는 optional).

---

## 부록: 미구현 항목 갭 분석 및 연기 로드맵

다른 모델이 지목한 "미구현" 목록을 실제 코드와 대조한 결과. 상태: (A) 이미 구현 / (B) 이번 작업 / (C) 연기.

| # | 항목 | 상태 | 비고 |
|---|------|------|------|
| 1 | 다인 협업 병렬 브랜치 | (C) | `ByRole`은 브랜치당 1인 고정. 다인/분리·합류는 대규모 설계 변경 |
| 2 | 지연 실행(Delay) | (A) | `ScenarioDelayNode` 이미 존재(`nodeType:"Delay"`) |
| 3 | 참여자·대상별 신호 식별/수량 계측 | (C) | 4-b와 연동. 신규 노드/tracker 필요 |
| 4-a | 이벤트→추가 신호 등록 노드 | (A) | `ScenarioSignalListenerNode` 이미 존재 |
| 4-b | Patient state 재구현 + 이벤트 리스너 + ScenarioNode 처리 | (B) 구현 완료 | 아래 "부록 2" 참조 |
| 5 | 미구현 게임플레이 신호 producer | (C) | 4-b 선행. 튜토리얼은 우선 스킵 |
| 6 | 퀘스트 완료→시나리오 signal/분기 | (A) 부분 | `OnCompleteSignalIdentifier` + `quest.completed.<id>` 존재 |
| 7 | 체온/SpO2 프리셋 확장 | (B) | 본 제안으로 구현 완료 |

### 연기 항목 상세

**1. 다인 협업 병렬 브랜치** — `ScenarioParallelAllocationType.ByRole` 은 브랜치당 정확히 1인(distinct)만 배정한다. 한 처치를 2인이 함께 수행하려면 (a) 브랜치 다중 플레이어 배정(min/max 인원, 합류 조건) 또는 (b) fork/join 프리미티브 추가가 필요하다. 네트워크 결정성(`ScenarioNetworkRelay`, 결정적 RNG)과 `WaitMode` 재설계 필요. 리스크 높음 → 독립 설계 문서 권장.

**3. 참여자·대상별 신호 식별 및 수량 계측** — 들것 2인 파지(손잡이/참여자별), 트리아지 구역 도착(환자별/인원 수량), 환자 B/C 처치(공용 sticky 대신 환자별 결과 신호), A의 18G 2개(좌·우별 또는 인벤토리 수량). 현재 signal은 전역 sticky 문자열 키라 대상별/수량 개념이 없다. 키 네임스페이싱(`sig.iv_18g.{patient}.{side}`) + count tracker 또는 전용 노드(`SignalCounter`/`TargetedSignal`) 신설 필요. 4-b 선행.

**4-b. Patient state 재구현 + 이벤트 리스너 + ScenarioNode 처리** — **구현 완료(2026-07-20). 아래 "부록 2" 참조.**

**5. 미구현 게임플레이 신호 producer** — 환자 A(기관내 삽관 전달·스타일렛 제거·주사기 전달 등 ~20개), 환자 B/C(IV 삽입, 활력 UI 닫기, 얼굴/비강 장비 클릭, 더미 클릭 등). 튜토리얼(이동/모자 NPC 대화/waypoint/택배/제출)은 우선 스킵. 환자 producer는 4-b 완료 후.

### 진행 순서 제안
1. (완료) 7. 체온/SpO2 프리셋 확장
2. (완료) 4-b Patient state 이벤트 + 시나리오 연동 — 부록 2
3. 3 + 5 환자별 신호 producer / 대상·수량 계측 (4-b 의존) — 다음 단계
4. 1 다인 협업 브랜치 (독립 설계 문서, 멀티플레이 결정성 검증 포함)

---

## 부록 2: 4-b Patient 상태 이벤트 + 시나리오 연동 (구현 완료)

### 개요
환자 상태 변경을 세분화된 C# 이벤트로 노출하고, 그 이벤트를 시나리오 신호로 변환하는 범용 경로를 추가했다. 값 이중화 없이(기존 `PatientMedicalState`/`PatientDisplayState`/트리아지 SyncVar 가 값을 그대로 보유) 변경 시점만 이벤트로 노출한다.

### 추가한 이벤트 (검토용 목록)
`PatientController.StateEvents.cs` 에 C# 이벤트로 노출하며, 런타임 `GetStateEventNames()` 가 동일 목록을 반환한다.

| 이벤트 이름(문자열) | C# 이벤트 | 인자(key) | 발생 지점 |
|---|---|---|---|
| `TreatmentApplied` | `OnTreatmentApplied(TreatmentDisplay)` | 처치 표시 항목명 | `SetTreatmentDisplay` (false→true 전이) |
| `TreatmentRemoved` | `OnTreatmentRemoved(TreatmentDisplay)` | 처치 표시 항목명 | `SetTreatmentDisplay` (true→false 전이) |
| `VitalChanged` | `OnVitalChanged(PatientMedicalState)` | (없음) | `NotifyMedicalStateChanged` |
| `TriageSubmitted` | `OnTriageSubmitted(TriageLevel)` | 트리아지 등급명 | `ApplyAssessedTriage` / `ApplyAssessedTriageLocalOnly` |

### 시나리오 연동
- 범용 인터페이스 `MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource` — 엔티티가 명명된 상태 이벤트에 리스너를 등록/해제하고 지원 이벤트 목록을 노출. `PatientController` 가 구현.
- 신규 시나리오 노드 `EntityStateSignalBinding` (`ScenarioNodeType.EntityStateSignalBinding`) — 대상 엔티티의 상태 이벤트(+선택적 `eventKey` 필터)를 관찰해 `outputSignalIdentifier` 신호를 `ScenarioInteractionSignals.Raise` 로 발신. `operation` Register/Unregister, `consumeOnce` 지원.
- 런타임 추적/정리: `ScenarioEntityStateSignalBindings` (등록/해제/시나리오 시작·종료 시 ClearAll). 동일 (엔티티,이벤트) 다중 바인딩도 콜백 보관+재구성으로 정확히 해제.
- 이벤트는 서버(호스트) 권위 상태 적용 지점에서 발생하므로 신호 발신이 서버 권위로 전 피어에 전파된다.

### 변경/추가 파일
- 신규(MI): `Scripts/Entity/IScenarioEntityStateEventSource.cs`, `Scripts/Scenario/ScenarioEntityStateSignalBindings.cs`, `Scripts/Scenario/Models/ScenarioGraphNodes/ScenarioEntityStateSignalBindingNode.cs`, `.../ScenarioGraphNodesDTO/ScenarioEntityStateSignalBindingNodeDTO.cs`
- 신규(TriageTrainer): `Scripts/Patient/PatientController.StateEvents.cs`
- 신규(콘텐츠): `Resources/Scenario/entity_state_signal_binding_debug.scenario.json`
- 수정(MI): `ScenarioNodeType`, `ScenarioController`(switch 2곳 + Execute/Resolve + ClearAll 2곳), `ScenarioGraphLoader`(convert 왕복 + dispatch 2곳), `ScenarioNodeDTOConverter`, `ScenarioRequirementCompiler`(등록+extractor), `ScenarioNodeRuntimeLookupRegistry`(등록), `Resources/Schema/scenario.schema.json`(enum+dispatch+$defs), Editor `ScenarioNodeFactory`
- 수정(TriageTrainer): `PatientController.TreatmentDisplay.cs`(전이 이벤트 훅), `PatientController.MedicalState.cs`(VitalChanged 훅), `PatientController.Triage.cs`(TriageSubmitted 훅 2곳)

### 검증
- 스키마: `EntityStateSignalBinding` 노드가 포함된 그래프가 schema 검증 통과, `bindingIdentifier` 누락은 거부됨.
- 노드 카탈로그 불변식: 노드 타입 32종 + `TagModification` 별칭 = 33 discriminator, RequirementCompiler / RuntimeLookupRegistry 완전성 검사 유지.
- 디버그 시나리오 `entity_state_signal_binding_debug.scenario.json` 로 처치 완료/트리아지 제출 → 신호 발생 흐름 수동 확인 가능.

### 남은 확장(연기)
- 동일 (엔티티,이벤트)에 서로 다른 `eventKey` 다중 바인딩은 동작하나, 향후 콜백 단위 해제 API 로 정교화 여지 있음.
- 에디터 그래프 UI(SearchWindow/InspectorView)에는 신규 노드 항목 미노출(JSON 저작은 완전 지원). 별도 에디터 확장으로 후속.
- 3/5 환자별 신호 producer 는 이 이벤트/노드를 활용해 다음 단계에서 구현.
