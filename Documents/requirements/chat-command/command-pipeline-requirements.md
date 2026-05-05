---
title: "채팅 명령 파이프라인 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

채팅 명령 파이프라인은 여러 명령의 실행 결과를 조합해 후속 명령 입력으로 전달하는 운영 자동화 기능이다. 반복 작업(예: 스폰 직후 태그 부여)을 단일 명령으로 수행할 수 있어야 한다.

## 상세

- `&`는 독립 명령 병렬군(논리적 병렬)으로 해석한다.
  - `a & b & c`는 세 명령을 각각 실행한다.
  - 반환값은 `[a의 반환..., b의 반환..., c의 반환...]` 순서 리스트로 간주한다.
- `|`는 전 단계 반환값을 다음 단계로 전달하는 파이프 연산자로 해석한다.
- 후속 명령의 `{}`는 전달값 자리표시자다.
  - 앞 단계 반환값을 `{}` 순서대로 치환한다.
  - 필요한 `{}` 개수보다 전달값이 부족하면 명령은 실패해야 한다.
- 명령은 0개 이상 반환값을 가질 수 있다.
  - 예: `a`가 2개 반환값을 주면 `a & b | d {} {} {}` 형태가 유효할 수 있다.

## 기술적 세부 사항

- 파이프라인 파싱/실행은 `ChatService.TryExecuteCommandInternal` 내부에서 수행한다.
- 단계 분리 규칙
  - `|` 기준 stage 분리
  - stage 내부는 `&` 기준으로 개별 명령 분리
- `{}` 치환은 후속 stage 실행 직전에 첫 번째 `{}`부터 순차 치환한다.
- 파이프라인 반환 지원 커맨드는 `IChatCommandPipelineCommand`를 구현한다.
- 비파이프라인 커맨드는 기존 `IChatCommandModel.Execute` 동작을 유지한다.

## 참조

- [api:MultiplayerInfrastructure.Chat.ChatService](../../api-references/MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](../../api-references/MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
