---
title: "Scenario SerializeSupport 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

Scenario SerializeSupport는 JSON 시나리오를 실행 가능한 그래프로 변환하는 기능이다. 사용자 관점에서는 작성된 시나리오 문서/JSON이 런타임에서 오류 없이 동작하도록 보장하는 기반이다.

## 상세

- 시스템은 JSON 스키마 검증을 통해 형식 오류를 조기에 탐지해야 한다.
- nodeType별 DTO 역직렬화와 도메인 노드 변환을 일관되게 수행해야 한다.
- 태그 선언/사용 불일치 시 경고를 제공해 작성 오류를 줄여야 한다.
- 오류는 파악 가능한 예외 메시지(경로 포함)로 제공되어야 한다.

## 기술적 세부 사항

- `ScenarioGraphLoader.LoadFromJson`이 파이프라인 진입점이다.
- `ScenarioNodeDTOConverter`가 nodeType 분기 역직렬화를 담당한다.
- `ScenarioJsonSchemaValidator`가 schema 기반 검증 실패를 `ScenarioSchemaValidationException`으로 반환한다.
- PlayTTS/TagModification 등 확장 노드 타입도 변환 대상에 포함된다.

## 참조

- [api:MultiplayerInfrastructure.Scenario.SerializeSupport](../../api-references/MultiplayerInfrastructure.Scenario.SerializeSupport.md)
- [api:scenario-graph-spec](../content-definitions/scenario/scenario-graph-spec.md)
- [api:json-conversion-rules](../content-definitions/scenario/json-conversion-rules.md)
