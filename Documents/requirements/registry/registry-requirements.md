---
title: "Registry 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

Registry는 프로젝트 전반에서 공통으로 사용하는 중앙 레지스트리 기능이다. 사용자 입장에서는 시스템이 객체를 안정적으로 찾고, 씬이 바뀌어도 핵심 참조가 유지되는 기반 기능으로 이해하면 된다.

## 상세

- 시스템은 아이템, 시나리오, UI, 엔티티, 서비스 등 서로 다른 대상을 분류해 저장하고 조회해야 한다.
- 같은 이름으로 등록된 항목은 최신값으로 갱신되어야 하며, 잘못된 입력(빈 식별자, null)은 안전하게 무시해야 한다.
- 시나리오 JSON(TextAsset)과 아이콘 경로 문자열은 조회 시점에 실제 객체로 해석되어야 한다.
- 엔티티는 단순 오브젝트가 아니라 설명자(EntityDescriptor) 단위로 관리되어야 하며, 플레이어/클라이언트 기준 조회가 가능해야 한다.

## 기술적 세부 사항

- RegistryType 분류 체계를 통해 저장소를 분리한다.
- `Get<T>`는 캐스팅 실패 시 기본값 반환을 원칙으로 하며, 특수 타입(ScenarioGraph, IconSprite, Entity)은 지연 해석 로직을 가진다.
- `RegisterEntity`, `TryGetEntityByClientId`, `GetAllEntities(EntityType)` 등 엔티티 전용 API를 지원한다.
- `PreloadScenarioGraph`로 시나리오 파싱 지연을 사전에 해소한다.

## 참조

- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
- [api:multiplayer-infrastructure-overview](../../api-references/architecture/multiplayer-infrastructure-overview.md)
- [change:registry-preloader-validation-tooling](../../changes/2026-03-26-registry-preloader-validation-tooling.md)
