---
title: "채팅 커맨드 확장 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

채팅 커맨드 확장은 운영자/플레이어가 런타임 상태를 제어하기 위한 기능이다. 현재 핵심은 `/give`, `/clean`, `/title` 명령과 대상 선택자 지원이다.

## 상세

- `/give`는 아이템 식별자 검증 후 대상 타겟에게 수량만큼 지급해야 한다.
- 인벤토리 여유가 부족하면 남은 수량을 월드 드롭으로 처리해야 한다.
- `/clean`은 전체 정리, 특정 아이템 전체 제거, 개수 지정 제거를 지원해야 한다.
- 잘못된 구문/식별자는 실패 메시지로 즉시 안내되어야 한다.
- `/title`은 타이틀/서브타이틀/액션바 표시와 타이밍 설정을 지원해야 한다.
- `<target>`는 선택자(@p/@a/@r/@s/@e/@n) 및 인자(x,y,z,distance,dx,dy,dz,tag,type)를 지원해야 한다.

## 기술적 세부 사항

- 커맨드 정의는 `CommandDefinition.Give.cs`, `CommandDefinition.Clean.cs`, `CommandDefinition.Title.cs`에 구현된다.
- 대상 해석은 호출자 기본값, `@s`, `fish:<clientId>`, `<clientId>`를 지원한다.
- 선택자 파서는 `TargetSelectorResolver`에서 공통 처리한다.
- `@e`/`@n`은 현재 플레이어만 지원하며 `type=player` 조건을 요구한다.
- 시스템 실행(`TryExecuteSystemCommand`)에서는 sender 컨텍스트 없는 경우를 고려해야 한다.

## 참조

- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](../../api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
- [api:MultiplayerInfrastructure.Chat.ChatService](../../api-references/MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Command.TargetSelectorResolver](../../api-references/MultiplayerInfrastructure.Command.TargetSelectorResolver.md)
- [change:chat-command-give-clean-datapack](../../changes/2026-02-18-chat-command-give-clean-datapack.md)
