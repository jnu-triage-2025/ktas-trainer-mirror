---
title: "EntityPreset 명령 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

`/entitypreset` 명령은 사전에 등록된 엔티티 프리셋을 조회하고 인게임에 즉시 배치하기 위한 운영 기능이다. 운영자/시나리오 저작자는 긴 수동 배치 절차 없이 식별자만으로 동일한 오브젝트 구성을 반복적으로 재현할 수 있어야 한다.

## 상세

- `list` 하위 명령은 현재 등록된 프리셋 식별자 목록을 출력해야 한다.
- `spawn` 하위 명령은 두 가지 입력 형태를 지원해야 한다.
  - `/entitypreset spawn <identifier> <x> <y> <z>`
  - `/entitypreset spawn <identifier> <target>`
- `<identifier>`는 Entity Preset 레지스트리에 등록된 프리셋 식별자여야 한다.
- `<target>`은 단일 대상 해석이 가능한 선택자/식별자여야 하며, 대상 위치를 스폰 위치로 사용해야 한다.
- 대상 또는 프리셋이 유효하지 않으면 사용자에게 즉시 실패 사유를 반환해야 한다.
- 스폰 성공 시 생성된 런타임 엔티티 식별자를 반환값으로 제공해야 한다.
- 파이프라인이 없는 일반 실행에서는 `엔티티 프리셋 <preset>가 <entity>로 스폰되었습니다.` 메시지를 출력해야 한다.
- 파이프라인 실행(`|`)에서는 스폰 결과 식별자를 후속 명령의 `{}` 치환값으로 전달해야 한다.

## 기술적 세부 사항

- 구현 위치: `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandDefinitions/CommandDefinition.EntityPreset.cs`
- 커맨드 엔트리: `entitypreset`
- 하위 명령: `list`, `spawn`
- `list`는 `Registry.GetAllEntityPresets()`를 기반으로 식별자를 정렬해 출력한다.
- 좌표 입력 파싱은 `InvariantCulture` 기준 실수 파싱을 사용한다.
- `<target>` 해석은 다음 순서를 지원한다.
  - `@selector` (공통 `TargetSelectorResolver` 재사용, 단일 타겟 요구)
  - `@s`, `@self`
  - `fish:<clientId>`, `<clientId>`
  - Registry에 등록된 Entity 식별자(해당 Entity의 `transform.position`)
- 실제 스폰 및 엔티티 등록은 `Registry.TrySpawnEntityPreset(...)`로 위임한다.
- 파이프라인 모드에서는 `IChatCommandPipelineCommand` 구현을 통해 반환값 리스트를 제공한다.

## 참조

- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](../../api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
- [api:MultiplayerInfrastructure.Command.TargetSelectorResolver](../../api-references/MultiplayerInfrastructure.Command.TargetSelectorResolver.md)
- [api:MultiplayerInfrastructure.Registry](../../api-references/MultiplayerInfrastructure.Registry.md)
