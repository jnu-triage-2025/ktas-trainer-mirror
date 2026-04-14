---
title: "TriageTrainer Item Definitions 기능 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

TriageTrainer Item Definitions는 시뮬레이션 절차에서 사용하는 의료 아이템 식별자와 표시 정보를 정의하는 기능이다. 사용자에게는 시나리오 단계별 정확한 물품 인지를 제공한다.

## 상세

- 각 아이템 정의는 고유 식별자, 표시명, 설명을 가져야 한다.
- 아이템 정의는 MedicalItem 기반 규칙을 따라 일관된 형식을 유지해야 한다.
- 시나리오 이벤트/인터랙션에서 동일 식별자를 참조할 수 있어야 한다.
- 신규 아이템 추가 시 정의 파일만으로도 기본 정보 표시는 가능해야 한다.

## 기술적 세부 사항

- `Assets/Modules/TriageTrainer/Scripts/Items/Definitions/*.cs` 다수 파일이 동일 패턴으로 구현된다.
- 대표 패턴은 `Identifier`, `DisplayName`, `Description` 상수 선언 방식이다.
- 인벤토리/상호작용 연동은 MultiplayerInfrastructure Item 시스템과 결합해 동작한다.

## 참조

- [api:MultiplayerInfrastructure.Item.Item](../../api-references/MultiplayerInfrastructure.Item.Item.md)
- [api:item-authoring-guide](../../working-guide/item-authoring.md)
- [api:event-mapping](../content-definitions/scenario/event-mapping.md)
