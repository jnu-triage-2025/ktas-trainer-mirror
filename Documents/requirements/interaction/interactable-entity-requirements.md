---
title: "InteractableEntity 상호작용 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

InteractableEntity 기능은 플레이어가 월드 오브젝트와 상호작용하는 방법을 표준화한다. 사용자에게는 일관된 상호작용 힌트와 동작 결과를 제공하는 것이 핵심이다.

## 상세

- 상호작용 가능한 대상은 공통 인터페이스(IInteractable, IInteract)를 따라야 한다.
- HUD에는 텍스트, 아이콘, 색상 등 상호작용 정보를 표시해야 한다.
- 감지 기능은 주변 대상만 주기적으로 탐색하고, 변경 시점에만 UI 갱신을 유도해야 한다.
- 월드 아이템 획득은 서버 검증을 거쳐 동기화되어야 하며 실패 시 복구 가능해야 한다.

## 기술적 세부 사항

- `Interactable` 추상 기반 클래스로 단일 액션 구현을 단순화한다.
- `NearbyInteractablesDetector`가 반경 기반 감지와 이벤트 발행을 담당한다.
- `LootableItemInteractHandler`는 ItemObject 스폰 시 자동 부착되어 픽업 흐름을 표준화한다.
- PlayerController의 상호작용 입력 루프와 결합한다.

## 참조

- [api:MultiplayerInfrastructure.InteractableEntity](../../api-references/MultiplayerInfrastructure.InteractableEntity.md)
- [api:MultiplayerInfrastructure.Player.PlayerController](../../api-references/MultiplayerInfrastructure.Player.PlayerController.md)
- [change:item-system-pickup-examples](../../changes/2026-03-03-item-system-pickup-examples.md)
