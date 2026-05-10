---
title: "PlayerController 코어 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

PlayerController는 플레이어 이동, 입력, 인벤토리, 상호작용, UI 연결을 통합하는 핵심 기능이다. 사용자 입장에서는 한 캐릭터의 조작 경험을 완성하는 중심 기능이다.

## 상세

- 시스템은 소유 플레이어 기준으로 입력을 처리하고, 비소유 객체에서는 중복 입력을 막아야 한다.
- UI가 열릴 때는 커서 잠금/이동 제어가 일관되게 바뀌어야 하며, 여러 UI가 겹칠 때도 정상 복귀해야 한다.
- 인벤토리는 추가/제거/개수 조회/드롭 기능을 제공해야 한다.
- 플레이어 게임모드(Player/Spectator) 전환 시 가시성, 이동, 입력 정책이 맞게 반영되어야 한다.

## 기술적 세부 사항

- partial class 구조로 네트워크, 이동, 입력, 인벤토리, 상호작용, UI를 분리한다.
- UIOverlayStack 방식으로 오버레이 중첩 진입/해제를 관리한다.
- 서버 측 PlayerGamemodeService와 연동해 게임모드 등록/해제를 수행한다.
- InventoryCommands, Item, Interactable 모듈과 결합해 런타임 행동을 완성한다.

## 참조

- [api:MultiplayerInfrastructure.Player.PlayerController](../../api-references/MultiplayerInfrastructure.Player.PlayerController.md)
- [api:MultiplayerInfrastructure.Player.PlayerController.InventoryCommands](../../api-references/MultiplayerInfrastructure.Player.PlayerController.InventoryCommands.md)
- [api:MultiplayerInfrastructure.Item.Item](../../api-references/MultiplayerInfrastructure.Item.Item.md)
