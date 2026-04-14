---
title: "Registry 프리로더 저작/운영 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

Registry 프리로더 저작/운영 기능은 씬 시작 시 필요한 레지스트리 데이터를 일괄 등록해, 런타임 조회 실패를 줄이고 컨텐츠 초기화 일관성을 보장하는 기능이다. 비개발 사용자 관점에서는 "시작 직후 필요한 데이터가 빠짐없이 준비되어야 한다"는 요구를 충족한다.

## 상세

- 프리로더는 시나리오 그래프, 아이콘, NPC, 웨이포인트, 엔티티, 인터랙터블 엔티티, UI 컨트롤러를 Awake 시점에 자동 등록해야 한다.
- 각 등록 항목은 식별자/참조 유효성 검사를 통과한 경우에만 등록되어야 하며, 잘못된 항목은 무시되어야 한다.
- UI 컨트롤러 등록은 명시 식별자가 없을 때 타입 기반 키(`Registry.TypeKey`)를 사용해 기본 키 정책을 보장해야 한다.
- 프리로더는 씬 하이어라키 존재 여부에 따라 등록 누락이 발생할 수 있는 항목을 ScriptableObject 기반으로 보완해야 한다.
- 운영 문서에는 "무엇을 어떤 ScriptableObject에 넣어야 하는지"가 기능 단위로 명확히 제시되어야 한다.

## 기술적 세부 사항

- 구현 클래스는 `RegistryPreloaderController`이며 `Awake()`에서 7개 Preload 메서드를 순차 호출한다.
- `Registry.Register(...)` 또는 `Registry.RegisterIconSprite(...)`를 통해 실제 등록을 수행한다.
- 시나리오 그래프는 `RegistryType.ScenarioGraph`, NPC는 `RegistryType.Npc`, 웨이포인트는 `RegistryType.Waypoint`, 서비스 오브젝트는 `RegistryType.Service`, 인터랙터블은 `RegistryType.InteractableEntity`, UI는 `RegistryType.UI`에 기록된다.
- 각 Preload 루프는 null/empty 방어 조건을 포함해 런타임 예외 대신 항목 스킵 정책을 사용한다.
- 결과적으로 저작 단계에서 ScriptableObject 데이터만 일관되게 구성하면, 런타임 초기 등록 품질을 동일하게 유지할 수 있다.

## 참조

- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
- [api:MultiplayerInfrastructure.Scenario.SerializeSupport](../../api-references/MultiplayerInfrastructure.Scenario.SerializeSupport.md)
- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [change:2026-03-26-registry-preloader-validation-tooling](../../changes/2026-03-26-registry-preloader-validation-tooling.md)
