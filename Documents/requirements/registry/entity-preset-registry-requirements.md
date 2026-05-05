---
title: "Entity Preset 레지스트리 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

Entity Preset은 "스폰 가능한 엔티티 구성"을 프리팹 단위로 사전 등록해두고, 런타임에는 식별자만으로 재사용할 수 있게 하는 레지스트리 기능이다. 목적은 콘텐츠 반복 제작 비용을 줄이고, 운영 중 동일한 엔티티 세트를 일관되게 재현하는 것이다.

## 상세

- Entity Preset은 최소한 다음 정보를 가져야 한다.
  - 프리셋 식별자
  - 엔티티 타입
  - 스폰할 프리팹 참조
  - 표시 이름(선택)
  - 네트워크 엔티티 여부 플래그
- 프리셋 등록은 데이터 자산(ScriptableObject) 기반으로 관리 가능해야 한다.
- 프리셋 스폰은 런타임 엔티티 식별자를 자동 생성하고 Entity 레지스트리에 등록해야 한다.
- 프리셋이 누락되었거나 프리팹이 비어 있으면 스폰을 거부하고 오류를 반환해야 한다.

## 기술적 세부 사항

- `RegistryType.EntityPreset`을 추가하고 전용 저장소를 `Registry.ResolveRegistry`에 연결한다.
- 모델 클래스: `EntityPresetDefinition`
  - 경로: `Assets/Modules/MultiplayerInfrastructure/Scripts/Registry/Models/EntityPresetDefinition.cs`
- Registry 확장: `Registry.EntityPreset.cs`
  - `RegisterEntityPreset(...)`
  - `TryGetEntityPreset(...)`
  - `GetAllEntityPresets()`
  - `TrySpawnEntityPreset(...)`
- 스폰 성공 시 런타임 식별자는 `entitypreset:<presetIdentifier>:<guid>` 형식으로 생성한다.
- TriageTrainer 연동은 `RegisteringMultiplayerInfrastructureSupport` partial에 추가한다.
  - `Awake_EntityPreset()` 호출
  - `EntityPresetRegistryRequirementsSO` 기반 등록

## 참조

- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
- [api:TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel](../../api-references/TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel.md)
