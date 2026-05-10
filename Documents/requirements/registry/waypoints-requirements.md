---
title: "WaypointAnchor 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

WaypointAnchor는 목표 지점 표시와 하이라이트 연출을 제공하는 기능이다. 사용자에게는 현재 목표 위치를 빠르게 인지하게 하는 내비게이션 보조 기능이다.

## 상세

- 웨이포인트는 식별자로 등록/조회 가능해야 한다.
- 하이라이트 호출 시 시각적 펄스 효과를 제공해야 한다.
- 퀘스트 HUD와 시나리오 노드에서 동일한 방식으로 재사용 가능해야 한다.
- 중복 식별자/미등록 식별자에 대한 진단 가능성이 확보되어야 한다.

## 기술적 세부 사항

- `TryGet(identifier)` 정적 조회 구조를 제공한다.
- `Highlight()`는 스프라이트 시각화 객체를 필요 시 생성하고 펄스 코루틴을 실행한다.
- `RegisterToRegistry`/`UnregisterFromRegistry`로 수명주기 등록 상태를 유지한다.

## 참조

- [api:MultiplayerInfrastructure.Registry.WaypointAnchor](../../api-references/MultiplayerInfrastructure.Registry.WaypointAnchor.md)
- [api:MultiplayerInfrastructure.Quest.QuestManager](../../api-references/MultiplayerInfrastructure.Quest.QuestManager.md)
- [change:quest-waypoint-highlights](../../changes/2026-02-18-quest-waypoint-highlights.md)
