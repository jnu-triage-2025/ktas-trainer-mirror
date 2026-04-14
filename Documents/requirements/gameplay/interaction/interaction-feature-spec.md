---
title: Interaction Feature Functional Requirements
doc_type: requirement
status: active
updated: 2026-04-14
owner: gameplay
---

# 상호작용 기능 요구사항

## 1. 목적

시나리오 이벤트(`InvokeEvent`)와 월드 상호작용(`IInteractable`, `IInteract`)을 일관된 규칙으로 연결하여, 기능 추가 시 구현 품질과 재사용성을 보장한다.

## 2. 범위

- 대상: `Assets/Modules/TriageTrainer/` 하위의 콘텐츠 구현
- 대상 시스템: Interactable, Item, Animator, FX, UI 연계
- 제외: `MultiplayerInfrastructure` 기반 모듈 직접 수정

## 3. 기능 요구사항

| ID | 요구사항 |
|---|---|
| IR-001 | 각 상호작용 기능은 고유 `EventIdentifier`를 가져야 한다. |
| IR-002 | 모든 `EventIdentifier`는 시나리오 이벤트 레지스트리에 문서화되어야 한다. |
| IR-003 | 상호작용 대상은 `IInteractable` 또는 `Interactable` 기반으로 구현되어야 한다. |
| IR-004 | HUD 표시 텍스트/아이콘/색상은 각 상호작용 단위(`IInteract`)로 정의 가능해야 한다. |
| IR-005 | 플레이어 입력으로 실행되는 상호작용은 실패 조건과 안내 메시지를 정의해야 한다. |
| IR-006 | 기능 카드는 실행 주체, 대상, 선행 조건, 입력, 출력, 완료 조건, 실패 처리를 포함해야 한다. |
| IR-007 | 서버 권한이 필요한 상호작용(예: 월드 아이템 획득/소멸)은 서버 승인 흐름을 따라야 한다. |
| IR-008 | 구현 산출물(프리팹/스크립트)은 식별자 기반 폴더 규칙을 따라야 한다. |
| IR-009 | 상호작용 완료 시 시나리오 진행에 필요한 이벤트 완료 신호를 전달할 수 있어야 한다. |
| IR-010 | 기반 인프라 수정 필요 시 기능 제안서 작성 절차를 선행해야 한다. |

## 4. 데이터/아티팩트 요구사항

| 구분 | 요구사항 |
|---|---|
| 스크립트 | `(PascalCaseIdentifier)Controller.cs` 명명 규칙 사용 |
| 인터랙터블 프리팹 | `Assets/Modules/TriageTrainer/Prefabs/Interactables/<identifier>/` |
| 아이템 프리팹 | `Assets/Modules/TriageTrainer/Prefabs/Items/<identifier>/` |
| 기능 카드 문서 | 요구 필드 누락 없이 작성 |

## 5. 수용 기준

| ID | 검증 항목 |
|---|---|
| IA-001 | 신규 기능이 `EventIdentifier`로 시나리오 노드와 연결된다. |
| IA-002 | 상호작용 키 입력 시 올바른 핸들러가 호출된다. |
| IA-003 | 선행 조건 미충족 시 실패 처리/가이드 메시지가 노출된다. |
| IA-004 | 멀티플레이 환경에서 권한 검증이 필요한 동작이 서버 기준으로 확정된다. |
| IA-005 | 기능 카드 기준으로 재현 가능한 테스트 시나리오를 작성할 수 있다. |

## 6. 예시 시나리오 요구

- 베드 이동: 인원 조건 확인, 웨이포인트 도착 시 완료 이벤트 발생
- 앰부백 산소화: 연결 상태 확인, 카운트 기반 완료, 미연결 시 경고 처리

## 7. 추적성(API 매핑)

- `Documents/api-references/MultiplayerInfrastructure.InteractableEntity.md`
- `Documents/api-references/MultiplayerInfrastructure.Player.PlayerController.md`
- `Documents/requirements/content-definitions/scenario/event-registry.md`
