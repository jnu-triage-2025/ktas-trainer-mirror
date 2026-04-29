---
title: "ChatService 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

ChatService는 플레이어 채팅 전송과 시스템 메시지, 커맨드 실행을 담당하는 기능이다. 사용자에게는 협업 커뮤니케이션과 운영 명령 진입점을 제공한다.

## 상세

- 일반 채팅과 시스템 메시지는 형식을 구분해 표시되어야 한다.
- 채팅 전송은 쿨다운 제한으로 스팸을 제어해야 한다.
- 슬래시 명령(`/`)은 서버에서 검증 후 실행되어야 한다.
- 운영 로직은 플레이어 입력 없이도 시스템 명령 실행 API를 사용할 수 있어야 한다.

## 기술적 세부 사항

- `SendChatServerRpc`, `ExecuteCommandServerRpc`, `ReceiveChatObserversRpc` 흐름으로 동작한다.
- `TryExecuteSystemCommand`는 sender 없는 서버 실행 경로를 제공한다.
- `ChatCommandService`와 결합해 `/help`, `/give`, `/clean`, `/gamemode`, `/scenario`, `/title`를 실행한다.
- 타이틀 표시는 타겟 RPC로 전달되며 클라이언트 로컬 타이밍 값을 사용한다.

## 참조

- [api:MultiplayerInfrastructure.Chat.ChatService](../../api-references/MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](../../api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
- [api:MultiplayerInfrastructure.Datapack.DatapackRuntimeService](../../api-references/MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md)
