# API 레퍼런스: TriageTrainer.Scenario.TriageScenarioEventBootstrap

> 네임스페이스: TriageTrainer.Scenario  
> 파일 위치: Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap*.cs

## 0. 개요

TriageScenarioEventBootstrap은 TriageTrainer 시나리오 이벤트를 ScenarioEventIdentifierRegistry에 등록하는 통합 부트스트랩입니다.

핵심 목적:
- 기반 MultiplayerInfrastructure 수정 없이 이벤트 구현 연결
- Inspector 기반 참조 주입 + Registry 자동 해석 지원
- 플레이모드 검증용 스냅샷/스모크 테스트 제공

## 1. 라이프사이클

| 시점 | 동작 |
|---|---|
| Awake | 시나리오 그래프 TextAsset Registry 등록 |
| OnEnable | Intro/PatientA + PatientBC 이벤트 등록 |
| OnDisable | 등록 이벤트 일괄 해제 |

## 2. 이벤트 등록 구조

- 분리 파일:
  - TriageScenarioEventBootstrap.IntroAndPatientAEvents.cs
  - TriageScenarioEventBootstrap.PatientBCEvents.cs
- 각 RegisterEvent_* 메서드는 코루틴 핸들러를 Registry.RegisterScenarioEvent로 연결

## 3. 자동 참조 해석

주요 지원:
- Entity/Npc Registry 조회
- alias 배열 기반 보조 해석
- 최종 fallback(GameObject.Find)

핵심 메서드:
- ResolveRuntimeReferencesIfNeeded()
- ResolveEntityObject(...)
- ResolveByAliases(...)
- LogUnresolvedTargetsIfAny()

## 4. 디버그/검증 도구

ContextMenu:
- Log Registry Snapshot: 등록 엔티티/키 확인
- Validate Event Wiring: 핵심 참조 누락 검사
- Run Core Smoke Test: 플레이모드 이벤트 기본 동작 점검

## 5. 인스펙터 주요 설정 영역

| 섹션 | 목적 |
|---|---|
| Scenario Graph Registration | 런타임 그래프 식별자/에셋 등록 |
| triage_patientA_patientDummyDA | 환자/베드/스폰 포인트 연결 |
| patient_a_critical (P1 MVP) | 모니터/체크리스트/시각 오브젝트 제어 |
| patient_b_c_ct intro (MVP) | 환자 B/C 스폰, 모니터, CT 이동 |
| B_C_D_to_triage | 간호사 이동 연출 |
| Auto Resolve | Registry/alias 자동 연결 및 로깅 |

## 6. 운영 가이드

- 식별자 문자열은 event-registry와 항상 동일하게 유지합니다.
- 실서버 배포 전 Validate Event Wiring + Smoke Test를 수행합니다.
- 이벤트 추가 시 RegisterIntroAndPatientAEvents/RegisterPatientBCEvents 라우터에 누락 없이 연결합니다.

## 7. 관련 문서

- requirements/content-definitions/scenario/event-registry.md
- requirements/content-definitions/scenario/event-handler-file-index.md
- requirements/content-definitions/scenario/event-mapping.md