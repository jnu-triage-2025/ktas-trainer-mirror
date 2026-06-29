---
title: "Scenario 사전 요구사항 검증(Preflight) 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "2-implementing"
flags: []
---

## 개요

Scenario 사전 요구사항 검증(Preflight) 기능은 시나리오를 시작하기 직전에, 그 시나리오가 정상
동작하기 위해 필요한 씬 요소(인터랙션 대상, 트리거존, 이벤트 핸들러, 아이템, 웨이포인트, 엔티티
프리셋 등)가 실제로 준비되어 있는지 한 번에 점검하는 기능이다. 누락이 발견되면 운영자에게 즉시
경고하여, 특히 개발 씬(IndevScene)처럼 실제 오브젝트가 갖춰지지 않은 환경에서 시나리오가 의도와
다르게 흘러가는 원인을 시작 시점에 바로 알 수 있게 한다.

## 상세

- 시스템은 시나리오 시작 시점에 그래프가 요구하는 요소 목록을 자동으로 수집해야 한다.
- 시스템은 각 요구 요소가 현재 환경(레지스트리/씬)에 존재하는지 검사하고, "충족 / 누락 /
  불확실(정보성)"로 분류해야 한다.
- 누락이 발견되면, 누락 요약과 항목별 상세(어떤 노드가 어떤 식별자를 요구하는지)를 경고로
  출력해야 한다.
- 경고 출력 채널은 **콘솔**과 **인게임 채팅** 두 가지이며, 각각 켜고 끌 수 있어야 한다. 기본값은
  **두 채널 모두 켜짐**이다.
- 누락이 있을 때의 행동은 두 가지 중 하나를 선택할 수 있어야 한다.
  - **경고 후 계속 진행(기본)**: 시나리오를 정상적으로 시작하되 경고만 남긴다(기존 무중단 철학과
    일치).
  - **시작 중단**: 시나리오를 시작하지 않고 중단한다.
- Preflight 기능 자체를 끌 수 있어야 하며, 끄면 기존과 완전히 동일하게 동작해야 한다.
- 신호(`sig.*`)처럼 런타임에 게임플레이가 올리는 값은 정적으로 존재 여부를 판정할 수 없으므로,
  누락으로 단정하지 않고 "정보성"으로만 보고해야 한다(누락 집계에서 제외).

## 개발 씬 보조

- 개발 씬에서 실제 오브젝트 없이 흐름을 끝까지 시연할 수 있도록, 누락된 인터랙션 대상은 간이
  원기둥(Cylinder)으로, 트리거존은 통과형 넓은 큐브(Cube, isTrigger)로 즉석 생성하는 보조 유틸을
  제공해야 한다.
- 보조 유틸이 생성한 오브젝트는 요구 식별자 그대로 레지스트리에 등록되어, 인터랙션/통과 시
  시나리오 신호가 정상적으로 올라가야 한다.
- 보조 유틸은 개발 씬 전용이며, 본 게임 씬을 오염시키지 않도록 명시적으로만 실행되어야 한다.

## 기술적 세부 사항

- 구현 위치: `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Preflight/`
  - `ScenarioRequirement`, `ScenarioRequirementKind`, `ScenarioRequirementStatus`
  - `ScenarioRequirementsCollector`(그래프 → 요구 목록, 정적/부작용 없음)
  - `ScenarioRequirementsChecker`(요구 목록 → 검사 결과 리포트)
  - `ScenarioPreflightPolicy`(채널 토글/누락 시 행동)
- 통합 지점: `ScenarioController.StartScenario(...)` 진입부에서 Preflight 를 1회 호출한다.
  정책이 `AbortStart` 이고 누락이 있으면 시작을 중단(return)한다.
- 검사 기준:
  - InteractionTarget → `Registry.TryGetEntity(identifier, ...)`
  - Waypoint → `Registry.Contains(RegistryType.Waypoint, ...)` / `WaypointAnchor.TryGet`
  - EntityPreset → `Registry.TryGetEntityPreset(...)`
  - EventHandler → `ScenarioEventIdentifierRegistry.TryGetHandler(...)`
  - Signal(`sig.*`) → 정적 판정 불가, 정보성으로만 나열
- 경고 채널: 콘솔은 `Debug.LogWarning`, 인게임 채팅은 `ScenarioController.AppendSystemChatMessage`
  경로를 재사용한다.

## 참조

- 제안서: `Agents/Proposals/done/2026-06-26-scenario-preflight-requirements/Feature Proposal - Scenario Preflight Requirements.md`
- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)
- [scenario-runtime-validation-requirements](./scenario-runtime-validation-requirements.md)
