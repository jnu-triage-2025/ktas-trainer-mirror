---
title: "QuestManager 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

QuestManager는 퀘스트 상태를 관리하고 플레이어 HUD와 패널에 진행 정보를 제공하는 기능이다. 사용자에게는 다음 해야 할 일을 분명하게 안내하는 학습 동선 기능이다.

## 상세

- 퀘스트는 추가, 갱신, 제거, 전체 정리 동작을 지원해야 한다.
- 트래킹 퀘스트는 미리보기 HUD와 목록 패널에 동시에 반영되어야 한다.
- 퀘스트에 웨이포인트 식별자가 포함되면 하이라이트 기능과 연동되어야 한다.
- 기능 플래그로 웨이포인트 자동 강조를 켜고 끌 수 있어야 한다.

## 기술적 세부 사항

- `AddOrUpdateQuest`에서 신규 퀘스트 여부를 판별하고 강조 로직을 호출한다.
- `FeatureFlags.HighlightAssignedWaypoint` 비트마스크로 하이라이트 동작을 제어한다.
- `QuestPreviewHudElement`, `QuestPanelElement`가 `WaypointIdentifier`를 표시한다.

## 참조

- [api:MultiplayerInfrastructure.Quest.QuestManager](../../api-references/MultiplayerInfrastructure.Quest.QuestManager.md)
- [api:MultiplayerInfrastructure.Registry.WaypointAnchor](../../api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md)
- [change:quest-waypoint-highlights](../../changes/2026-02-18-quest-waypoint-highlights.md)
