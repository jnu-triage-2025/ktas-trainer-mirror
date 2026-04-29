---
title: "TriageTrainer 이동식 환자 침대 상호작용 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
updated: 2026-04-27
---

## 개요

이 문서는 TriageTrainer의 이동식 환자 침대(Moving Patient Bed)와 환자(Patient) 상호작용 기능 요구사항을 현재 API 기준으로 정리한다. 목표는 플레이어가 환자를 침대에 눕히고, 협동으로 침대를 이동시키며, 치료 아이템 시각 상태를 침대/환자에 반영하는 흐름을 일관되게 제공하는 것이다.

## 상세

- 이동식 환자 침대는 `IInteractable`/`IInteract` 구현체로 동작해야 한다.
- 기본 식별자/표시값은 다음 정책을 따른다.
  - entityTypeIdentifier (에디터에 보이는 "타입 식별자"): `moving_patient_bed` (로컬/테스트용 기본값)
  - entityRuntimeIdentifier (서버가 부여한 런타임 고유 식별자): `moving_patient_bed:{uuid}` 형태이나, 서버 할당 전까지는 `null`일 수 있음
    - 서버가 씬 로드 시 `SetIdentifier()` 호출로 할당 (ItemObject 패턴 준용)
    - 서버 할당 시에만 Registry에 등록됨
  - displayText: `이동식 환자 침대`
  - displayColor: `Color.white`
  - displayIcon: 미지정 시 `null` 허용
- 침대는 `Weight`(기본 0) 속성을 가지며, 이동에 필요한 최소 상호작용 인원을 결정해야 한다.
- 침대에 눕힐 수 있는 대상은 `IReposable` 구현체로 제한한다.
- 환자는 `IReposable`을 구현하며 기본 무게는 4를 사용한다.
- 침대 이동은 토글/홀드 모드를 지원해야 하며(기본 토글), 홀드 모드에서는 상호작용 키 해제 시 이동 상태에서 빠져야 한다.
- 침대에 설정된 아이템 식별자-시각 오브젝트 쌍은 에디터 직렬화 리스트로 보관하고, 런타임에는 딕셔너리 캐시로 조회해야 한다.
- 침대 또는 환자에 아이템 사용/공격 이벤트가 전달되면 현재 핸들링 아이템 식별자를 기준으로 대응 시각 오브젝트를 활성화해야 한다.
- 인원 부족, 들어올리기 실패 등 사용자 피드백은 채팅 UI에 표시하되 동일 메시지는 3초 이내 반복 노출하지 않아야 한다.

## 기술적 세부 사항

- 현재 시스템 기준 침대 컨트롤러는 `MonoBehaviour` + `IInteractable`/`IInteract` 조합으로 구현한다.
  - 구 프롬프트의 `Entity.Entity` 상속 요구는 현행 인터랙터블 구조와 다르며, 현재 코드베이스에서는 필수 요건이 아니다. 다만 이동식 침대는 서버가 할당한 entity identifier를 통해 Registry의 Entity로 등록되어야 한다.
  - 서버/초기화 시스템은 씬의 MovingPatientBedController 오브젝트를 발견하면 `SetIdentifier(identifier)` 메서드를 호출하여 entity identifier를 할당 (ItemObject 패턴 준용).
  - identifier가 할당되지 않으면 Registry에 등록되지 않음 (테스트 환경 등에서도 동작 가능하도록)
- 최소 이동 인원은 `max(침대 Weight, 현재 눕혀진 대상 Weight)`로 계산한다.
- 침대에 대상을 눕히면 repose anchor 하위로 부모를 변경하고 로컬 위치/회전을 초기화한다.
- 침대에서 대상을 들어올릴 때는 `PlayerController.TryPickUpReposable(...)`를 통해 운반 상태를 전환하며, 실패 시 원위치 복원해야 한다.
- repose anchor는 씬 편집 가시성을 위해 기즈모로 표시해야 한다.
- 시나리오 이벤트와의 연계(특정 침대 위치 도달 시 트리거)는 침대 단독 책임이 아니라 시나리오 계층에서 침대 상태/위치를 참조해 처리할 수 있어야 한다.

### 구 프롬프트 대비 정합성 정리

- 유지되는 요구
  - 협동 이동(무게 기반 인원 제한)
  - `IReposable` 기반 눕힘/들기
  - 아이템 식별자-시각 오브젝트 매핑 활성화
  - 토글/홀드 이동 모드
  - 채팅 메시지 스로틀(3초)
- 조정된 요구
  - 경로 표기는 현재 프로젝트 구조(`Scripts/Entities` 및 관련 프리팹 참조) 기준으로 해석한다.
  - `IReposable`는 이미 MultiplayerInfrastructure에 존재하므로 신규 제안 대상이 아니다.
- 미반영(추가 구현 필요) 요구
  - "맨손 + 환자 운반 중 Attack/Use 시 침대에 즉시 눕힘"의 직접 진입 플로우는 현행 침대 `OnAttacked`/`OnItemUsed` 구현에 포함되지 않는다.

## 참조

- [api:MultiplayerInfrastructure.InteractableEntity](../../api-references/MultiplayerInfrastructure.InteractableEntity.md)
- [api:MultiplayerInfrastructure.Player.PlayerController](../../api-references/MultiplayerInfrastructure.Player.PlayerController.md)
- [api:MultiplayerInfrastructure.Item.Item](../../api-references/MultiplayerInfrastructure.Item.Item.md)
- [api:TriageTrainer.Scenario.TriageScenarioEventBootstrap](../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)
