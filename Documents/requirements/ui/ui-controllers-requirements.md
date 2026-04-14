---
title: "UI Controllers 공통 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

UI Controllers는 채팅, 인벤토리, 퀘스트, ESC 메뉴 등 화면 단위 패널의 수명주기와 입력 연결을 담당하는 기능이다. 사용자에게는 패널 간 일관된 열기/닫기 경험을 제공해야 한다.

## 상세

- UI 컨트롤러는 공통 기반(UIControllerABC)에서 초기화 규칙을 공유해야 한다.
- 오버레이 UI는 입력 잠금/해제와 연동되어 플레이 조작 충돌이 없어야 한다.
- 채팅/인벤토리/퀘스트/그래픽 설정 등 각 패널은 독립 제어 가능해야 한다.
- 시나리오 및 상호작용 힌트 UI는 플레이 상태에 따라 즉시 갱신되어야 한다.

## 기술적 세부 사항

- 주요 구현체: ChatUIController, InventoryUIController, QuestUIController, CrosshairUIController, DialoguePanelUIController, HotbarUIController, GameEscapeMenuUIController 등.
- `IUIOverlay` 인터페이스로 오버레이 패널 공통 동작을 정의한다.
- PlayerController의 UIOverlayStack과 결합해 중첩 오버레이를 관리한다.

## 참조

- [api:MultiplayerInfrastructure.Player.PlayerController](../../api-references/MultiplayerInfrastructure.Player.PlayerController.md)
- [api:MultiplayerInfrastructure.UI.Crosshair](../../api-references/MultiplayerInfrastructure.UI.Crosshair.md)
- [api:requirements-ui](../ui/README.md)
