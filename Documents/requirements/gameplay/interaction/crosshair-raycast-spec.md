---
title: Crosshair Raycast Functional Requirements
doc_type: requirement
status: active
updated: 2026-04-14
owner: gameplay
---

# 크로스헤어 레이캐스트 기능 요구사항

## 1. 목적

플레이어 화면 중앙 기준 레이캐스트를 통해 상호작용 후보를 안정적으로 식별하고, UI(크로스헤어)와 상호작용 시스템에 일관된 입력 신호를 제공한다.

## 2. 범위

- 대상 모듈: `MultiplayerInfrastructure.Player.PlayerController` (Raycast/Crosshair 분리 파일)
- 대상 UI: `MultiplayerInfrastructure.UI.CrosshairUIController`, `CrosshairElement`
- 제외: 실제 상호작용 처리 로직(아이템 획득, 문 열기 등)의 비즈니스 규칙

## 3. 기능 요구사항

| ID | 요구사항 |
|---|---|
| CR-001 | 시스템은 로컬 소유 플레이어(`IsOwner == true`)에서만 레이캐스트를 수행해야 한다. |
| CR-002 | 시스템은 카메라 뷰포트 중앙 `(0.5, 0.5)` 기준으로 매 프레임 레이를 생성해야 한다. |
| CR-003 | 시스템은 Inspector로 설정 가능한 `LayerMask`와 최대 거리(`RaycastMaxDistance`)를 사용해야 한다. |
| CR-004 | 레이캐스트 결과는 `RaycastHasHit`, `RaycastHit`, `RaycastHitObject`로 외부에서 읽을 수 있어야 한다. |
| CR-005 | `RaycastHasHit == false`인 경우 `RaycastHitObject`는 `null`을 반환해야 한다. |
| CR-006 | 카메라 참조 또는 필수 의존성이 없을 경우 시스템은 예외 없이 실패 안전(fail-safe)하게 동작해야 한다. |
| CR-007 | 크로스헤어 UI 표시 제어는 레이캐스트 로직과 분리되어야 한다. |
| CR-008 | 크로스헤어 UI는 코드에서 `SetCrosshairVisible(bool)`로 표시/숨김을 제어할 수 있어야 한다. |
| CR-009 | `_crosshairUI` 미지정 시 Registry 기반 자동 탐색 경로를 제공해야 한다. |
| CR-010 | 레이캐스트 결과 갱신은 입력 처리/상호작용 처리보다 먼저 완료되어야 한다. |

## 4. 비기능 요구사항

| ID | 요구사항 |
|---|---|
| CN-001 | 레이캐스트 처리 추가로 프레임 드랍이 유의미하게 증가해서는 안 된다. |
| CN-002 | 잘못된 레이어 설정이나 거리 설정 시 디버깅 가능한 로그/상태 확인 지점을 제공해야 한다. |
| CN-003 | UI 표시 제어 실패가 플레이어 이동/입력 루프를 중단시키면 안 된다. |

## 5. 수용 기준

| ID | 검증 항목 |
|---|---|
| CA-001 | 소유하지 않은 플레이어 객체에서 레이캐스트 상태가 갱신되지 않는다. |
| CA-002 | 중앙 조준점이 Collider를 가리킬 때 `RaycastHasHit`가 `true`가 된다. |
| CA-003 | 대상이 없으면 `RaycastHasHit == false`, `RaycastHitObject == null`이다. |
| CA-004 | `Raycast Layer Mask`에서 제외한 레이어 대상은 감지되지 않는다. |
| CA-005 | `SetCrosshairVisible(false)` 호출 시 UI가 즉시 숨겨진다. |

## 6. 추적성(API 매핑)

- `Documents/api-references/MultiplayerInfrastructure.UI.Crosshair.md`
- `Documents/api-references/MultiplayerInfrastructure.Player.PlayerController.md`
- `Documents/requirements/ui/miui_crosshair.md`
