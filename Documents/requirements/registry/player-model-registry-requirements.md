---
title: "PlayerModel Registry 확장 요구사항"
domain: "module-features.triage-trainer"
progress: "3-implemented"
flags: []
---

## 개요

PlayerModel 기능을 안정적으로 운영하기 위해 Registry 분류 체계와 콘텐츠 등록 절차를 확장하는 요구사항이다. 사용자 관점에서는 모델 데이터가 누락되지 않고, 접속 시점에 일관된 외형 표시가 보장되어야 한다.

## 상세

- Registry 분류에 플레이어 모델 전용 타입이 있어야 한다.
- 모델 등록은 코드 하드코딩 대신 데이터 자산(ScriptableObject) 기반으로 유지 가능해야 한다.
- 등록 프로세스는 기존 RegisteringMultiplayerInfrastructureSupport partial 구조와 동일한 생명주기 패턴을 따라야 한다.
- 등록 항목은 최소한 식별자(ID)와 프리팹 참조를 포함해야 한다.
- 잘못된 등록 데이터는 런타임 경고로 빠르게 파악 가능해야 한다.

## 기술적 세부 사항

- `RegistryType.PlayerModel`을 추가하고, `Registry.ResolveRegistry`에 대응 저장소를 연결한다.
- `PlayerModelRegistryRequirementsSO`가 `PlayerModelRegistryRequirement[]`를 보관한다.
- `RegisteringMultiplayerInfrastructureSupport.Awake()`에서 `Awake_PlayerModel()`를 호출해 등록/검증을 수행한다.
- 등록은 `Registry.Register(RegistryType.PlayerModel, identifier, prefab)` 호출로 반영한다.

## 참조

- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
- [api:TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel](../../api-references/TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel.md)
- [api:MultiplayerInfrastructure.Player.PlayerModel](../../api-references/MultiplayerInfrastructure.Player.PlayerModel.md)
