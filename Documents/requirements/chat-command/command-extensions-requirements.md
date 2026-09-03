---
title: "채팅 커맨드 확장 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

채팅 커맨드 확장은 운영자/플레이어가 런타임 상태를 제어하기 위한 기능이다. 현재 핵심은 `/give`, `/clean`, `/title`, `/entitypreset`, `/tag`, `/problemsheet` 명령과 대상 선택자/파이프라인 지원이다.

## 상세

- `/give`는 아이템 식별자 검증 후 대상 타겟에게 수량만큼 지급해야 한다.
- 인벤토리 여유가 부족하면 남은 수량을 월드 드롭으로 처리해야 한다.
- `/clean`은 전체 정리, 특정 아이템 전체 제거, 개수 지정 제거를 지원해야 한다.
- 잘못된 구문/식별자는 실패 메시지로 즉시 안내되어야 한다.
- `/title`은 타이틀/서브타이틀/액션바 표시와 타이밍 설정을 지원해야 한다.
- `/entitypreset`은 프리셋 목록 조회와 좌표/대상 기반 스폰을 지원해야 한다.
- `/tag`는 플레이어뿐 아니라 Entity 식별자를 대상으로 태그 부여/변경/조회가 가능해야 한다.
- `/problemsheet`는 목록 조회(`/problemsheet list`)와 대상 실행(`/problemsheet <target> <problem-identifier> [problem-index]`)을 지원해야 한다.
- `/problemsheet`는 파이프라인 결과값으로 마지막 채점 결과를 반환해야 하며, 값은 정답 `0`, 오답 `1`이어야 한다.
- `<target>`는 선택자(@p/@a/@r/@s/@e/@n) 및 인자(x,y,z,distance,dx,dy,dz,tag,type)를 지원해야 한다.
- 플레이어를 인자로 받는 모든 커맨드(`/permission user`, `/scoreboard players`, `/tag`, `/signal player`, `/server kick|ban` 등)는 선택자와 `id:<uuid>`, `name:<displayName>`, 표시 이름을 동일하게 해석해야 한다.
- 여러 대상을 지정하는 선택자를 사용하면, 대상마다 동작을 반복 수행하고 결과를 대상별로 안내해야 한다. 단일 대상만 허용하는 인자는 실패 사유를 반환해야 한다.
- `&`와 `|`를 통한 명령 파이프라인 실행을 지원해야 한다.

## 기술적 세부 사항

- 커맨드 정의는 `CommandDefinition.Give.cs`, `CommandDefinition.Clean.cs`, `CommandDefinition.Title.cs`에 구현된다.
- 대상 해석은 호출자 기본값, `@s`, `fish:<clientId>`, `<clientId>`를 지원한다.
- 선택자 파서는 `TargetSelectorResolver`에서 공통 처리한다.
- 플레이어 인자의 사용자 해석은 `PlayerTargetResolver`에서 공통 처리하며, 내부적으로 `TargetSelectorResolver`와 `UserDescriptorService`를 사용한다.
- `@e`/`@n`은 현재 플레이어만 지원하며 `type=player` 조건을 요구한다.
- 시스템 실행(`TryExecuteSystemCommand`)에서는 sender 컨텍스트 없는 경우를 고려해야 한다.
- 파이프라인 반환값 전달이 필요한 커맨드는 `IChatCommandPipelineCommand`를 구현한다.
- ProblemSheet 관련 파이프라인 결과는 서버가 보유한 마지막 문제 판정 코드(`0|1`)를 사용해야 한다.

## 참조

- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](../../api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
- [api:MultiplayerInfrastructure.Chat.ChatService](../../api-references/MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Command.PlayerTargetResolver](../../api-references/MultiplayerInfrastructure.Command.PlayerTargetResolver.md)
- [api:MultiplayerInfrastructure.Command.TargetSelectorResolver](../../api-references/MultiplayerInfrastructure.Command.TargetSelectorResolver.md)
- [change:chat-command-give-clean-datapack](../../changes/2026-02-18-chat-command-give-clean-datapack.md)
