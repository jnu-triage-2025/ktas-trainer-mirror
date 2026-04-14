---
title: "InventoryCommands 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

InventoryCommands 기능은 커맨드/시스템이 인벤토리를 직접 제어할 수 있도록 제공하는 기능이다. 사용자에게는 지급, 정리, 수량 관리 결과가 일관되게 반영되는 운영 품질을 제공한다.

## 상세

- 시스템은 아이템 추가 시 남은 수량(leftover)을 별도로 반환해야 한다.
- 전체 삭제, 부분 삭제, 식별자별 전체 삭제, 보유량 조회 기능을 지원해야 한다.
- 인벤토리 초과 수량은 플레이어 전방 드롭 처리와 결합되어야 한다.
- 커맨드(`/give`, `/clean`) 실행 결과와 API 결과가 동일한 규칙을 가져야 한다.

## 기술적 세부 사항

- `TryAddItemToInventory(..., out leftover)`, `ClearInventory`, `RemoveItemFromInventory`, `RemoveAllOfItemFromInventory`, `CountItemInInventory`를 제공한다.
- `TryDropItemInFront`는 `ItemSpawnUtility` 경유로 월드 스폰을 수행한다.
- 런타임 드롭 시 `Item.ApplyRuntimeItemData`로 수량/식별자/아이콘 보정을 적용한다.

## 참조

- [api:MultiplayerInfrastructure.Player.PlayerController.InventoryCommands](../../api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md)
- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](../../api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
- [api:MultiplayerInfrastructure.Item.Item](../../api-references/MultiplayerInfrastructure.Item.Item.md)
