---
title: "TriageTrainer Patient Models 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

TriageTrainer Patient Models는 환자 상태, 처치 표시, 시나리오 환자 타입 데이터를 정의하는 기능이다. 사용자에게는 환자 상태 판단과 처치 우선순위 교육의 데이터 기반을 제공한다.

## 상세

- 환자 기술자(PatientDescriptor)는 생체징후, 의식, 호흡, 약물 요구 등 핵심 상태를 포함해야 한다.
- 환자 타입별 상태(예: Type A/B Male/Female)는 시나리오 단계에서 일관된 상태를 제공해야 한다.
- 치료 표시 모델은 UI 표시 대상과 상태 객체를 분리해 관리해야 한다.
- 모델 구조는 향후 자동 생성/평가 기능 확장을 고려해야 한다.

## 기술적 세부 사항

- `PatientDescriptor`가 상태 집합의 중심 모델이다.
- `PatientStateABC`, `PatientTreatmentDisplayStateABC` 추상 구조로 타입별 구현을 분리한다.
- `Models` 하위 열거형/값 객체(BloodPressure, Consciousness, Respiration 등) 조합으로 세부 상태를 표현한다.

## 참조

- [api:TriageTrainer.Entity.PatientMonitor](../../api-references/TriageTrainer.Entity.PatientMonitor.md)
- [api:patient_a_critical](../content-definitions/scenario/patient_a_critical.md)
- [api:patient_b_c_ct](../content-definitions/scenario/patient_b_c_ct.md)
