---
title: "TriageTrainer Patient Models 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

TriageTrainer Patient Models는 환자 기본 프로필, 의료 상태, 처치 표시 상태를 분리해 관리하는 기능이다. 사용자 관점에서는 환자 상태 판단과 처치 우선순위 교육의 데이터 기반을 제공하며, 개발 관점에서는 시나리오/모니터/상호작용 시스템이 동일 환자 데이터를 일관되게 참조할 수 있어야 한다.

## 상세

- 환자 기본 프로필(`PatientDescriptor`)은 환자 식별/인적 정보(식별자, 이름, 성별, 나이, 혈액형)를 제공해야 한다.
- 환자 의료 상태(`PatientMedicalState`)는 활력징후, 의식/호흡, 건강 문제, 요구 약물, 모니터 파라미터를 포함해야 한다.
- 환자 타입별 상태(`PatientStateABC` 파생)는 시나리오 단계에서 일관된 초기 상태와 표시 정책을 제공해야 한다.
- 치료 표시 모델(`PatientTreatmentDisplayStateABC`, `PatientDisplayState`)은 의료 상태와 분리되어 시각 오브젝트 토글 상태를 관리해야 한다.
- 모델 구조는 향후 자동 생성/사후 평가 기능 확장을 고려해야 한다.

## 기술적 세부 사항

- `PatientController`는 `PatientDescriptor`와 `PatientMedicalState`를 함께 보유하며, 의료 상태 변경 알림과 네트워크 동기화를 수행한다.
- `PatientDescriptor`는 기본 프로필 데이터 클래스이며, 생체징후/치료 절차 데이터 자체는 `PatientMedicalState`가 담당한다.
- `PatientMedicalState`는 `bloodPressure`, `pulse`, `consciousness`, `respiration`, `requiredDrugs` 및 모니터 파라미터(`ecg/art/cvp/pleth/numerics/nibp/temperature/stLeads`)를 포함한다.
- 타입별 상태는 `PatientStateABC`와 `PatientTreatmentDisplayStateABC` 추상 구조로 분리되며, `PatientTypeAState`, `PatientTypeBMaleState`, `PatientTypeBFemaleState` 등 파생 구현이 존재한다.
- 표시 계층은 `PatientDisplayState`와 `PatientTreatmentDisplayingChildGameObjects`를 통해 환자 모델 자식 오브젝트 활성화 상태를 관리한다.

## 참조

- [api:TriageTrainer.Entity.PatientController](../../api-references/entities/patient-controller-reference.md)
- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:patient_a_critical](../content-definitions/scenario/patient_a_critical.md)
- [api:patient_b_c_ct](../content-definitions/scenario/patient_b_c_ct.md)
