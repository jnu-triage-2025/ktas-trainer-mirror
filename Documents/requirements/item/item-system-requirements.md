---
title: "ItemSystem 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

ItemSystem은 월드 아이템과 인벤토리 아이템을 연결하는 기능이다. 사용자에게는 획득, 사용, 드롭, 상호작용이 자연스럽게 이어지는 체감 품질을 제공한다.

## 상세

- 아이템은 초기화 시 데이터 정합성 검사를 수행하고 등록 가능 상태를 보장해야 한다.
- 월드 상호작용으로 획득 요청 시 서버 검증을 거쳐 인벤토리에 반영되어야 한다.
- 인벤토리 추가 실패나 초과 수량은 월드 드롭/롤백 정책으로 처리되어야 한다.
- 아이템 식별자와 아이콘 로딩 규칙은 일관된 네이밍 규칙을 가져야 한다.

## 기술적 세부 사항

- Item Lifecycle Awake에서 base model, ItemData, identifier, icon 순으로 보정한다.
- 유효성 실패 시 Registry 등록을 중단하고 경고 로그를 남긴다.
- `TryAddItemToInventory`와 `TryDropItemInFront`가 지급/초과 처리 핵심 경로다.

## 참조

- [api:MultiplayerInfrastructure.Item.Item](../../api-references/MultiplayerInfrastructure.Item.Item.md)
- [api:MultiplayerInfrastructure.Player.PlayerController.InventoryCommands](../../api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md)
- [change:item-system-pickup-examples](../../changes/2026-03-03-item-system-pickup-examples.md)
