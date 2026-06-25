---
title: "Proposal: Add MovingPatientBed EntityType and register MovingPatientBed as Entity"
author: copilot
date: 2026-04-29
status: draft
tags: [registry, entity, triage-trainer]
---

요약
--
이 제안서는 `MovingPatientBed`를 런타임 `Entity`로 등록하도록 구현하고, 이를 위해 `Registry.EntityType`에 `MovingPatientBed` 항목을 추가하는 변경을 설명합니다. 또한 `MovingPatientBedController`가 서버로부터 할당받은 entity identifier를 통해 Registry에 등록되도록 구현합니다 (ItemObject 패턴 준용).

변경 필요성
--
- `MovingPatientBed`는 씬에서 고유하게 추적되어야 하는 오브젝트입니다(시나리오, UI, 네이티브 서비스의 참조 대상). Registry의 `Entity` 도메인에 명확히 등록되어야 기능 연계가 용이합니다.
- 기존 구현에서 `identifier`로 사용하던 `moving_patient_bed` 문자열은 논리적 타입 식별자에 가깝고 런타임 식별자와 구분되어야 합니다.

변경 내용
--
1. `Registry.Models.EntityType`에 `MovingPatientBed` 열거형 값 추가
2. `MovingPatientBedController`
   - 에디터 직렬화 필드 `_entityTypeIdentifier`로 논리적 타입(`moving_patient_bed`) 보존
   - Awake에서는 UUID 생성하지 않음 (서버 할당 대기)
   - `SetIdentifier(string identifier)` 메서드 추가 — 서버가 호출하여 entity identifier 할당
   - SetIdentifier() 호출 시에만 Registry.RegisterEntity(...) 호출
   - OnDestroy에서 식별자가 있을 경우 Registry.UnregisterEntity(...) 호출로 정리

영향도 및 마이그레이션
--
- 행위: Registry 타입 하나 추가 — 다른 코드에 영향을 주지 않음(기존 switch에 새로운 케이스 추가)
- 마이그레이션: 서버/씬 초기화 시스템이 씬의 MovingPatientBedController 오브젝트를 발견하면 SetIdentifier()를 호출해야 함
  - identifier 할당 전에는 Registry에 등록되지 않으므로 테스트/데모 환경에서도 식별자 없이 동작 가능

테스트 방안
--
1. 씬에 `MovingPatientBed` 프리팹 배치 후 플레이(에디터 재생) 전에 SetIdentifier() 호출 없음 — Registry에 미등록 상태 확인
2. 씬 초기화 코드에서 SetIdentifier("moving_patient_bed:...") 호출 — Registry.GetAllEntities(EntityType.MovingPatientBed)에 등록되는지 확인
3. 씬 언로드/게임종료 시 등록 해제되는지 확인
