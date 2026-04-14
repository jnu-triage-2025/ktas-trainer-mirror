---
title: "ScenarioEventIdentifierRegistry 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

ScenarioEventIdentifierRegistry는 시나리오의 InvokeEvent 식별자와 실제 씬 동작 코드를 연결하는 기능이다. 사용자 관점에서는 시나리오 문서의 이벤트가 실제 연출로 이어지게 하는 연결 장치다.

## 상세

- 시스템은 식별자 문자열로 이벤트 핸들러를 등록/조회/해제해야 한다.
- 동일 식별자 재등록 시 최신 핸들러를 적용해야 한다.
- 이벤트 실행은 즉시 완료 또는 코루틴 완료 대기 흐름을 지원해야 한다.
- 씬 수명주기에 맞춰 등록/해제가 가능해야 하며, 전체 초기화(clear)도 제공해야 한다.

## 기술적 세부 사항

- 핸들러 타입은 `IEnumerator` 반환 델리게이트를 사용한다.
- ScenarioController는 `TryGetHandler`로 조회 후 `MoveNextBehavior` 정책에 맞게 실행한다.
- 등록 입력값 검증(빈 식별자, null 핸들러)에서 예외를 발생시켜 잘못된 설정을 조기 발견한다.

## 참조

- [api:MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)
- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:event-registry](../content-definitions/scenario/event-registry.md)
