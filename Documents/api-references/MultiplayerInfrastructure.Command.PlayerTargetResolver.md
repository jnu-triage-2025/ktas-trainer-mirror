# API 레퍼런스: `MultiplayerInfrastructure.Command.PlayerTargetResolver`

> **네임스페이스:** `MultiplayerInfrastructure.Command`  
> **형태:** `static class`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/PlayerTargetResolver.cs`

---

## 0. 문서 목적

`PlayerTargetResolver`는 채팅 커맨드의 플레이어 인자를 `UserDescriptor` 목록으로 해석하는 공통 유틸리티입니다.
`TargetSelectorResolver`가 선택자를 연결(`NetworkConnection`) 목록으로 해석하는 반면, 이 클래스는 선택자와 사용자 식별자,
표시 이름을 모두 같은 방식으로 처리해서 사용자 단위로 동작하는 커맨드가 동일한 문법을 제공하도록 만듭니다.

---

## 1. 공개 API

```csharp
public static bool TryResolve(
  NetworkConnection sender,
  string token,
  out List<UserDescriptor> descriptors,
  out string error)

public static bool TryResolveSingle(
  NetworkConnection sender,
  string token,
  out UserDescriptor descriptor,
  out string error)

public static bool IsSelector(string token)

public const string TokenSyntaxHint
```

- `TryResolve`는 토큰이 가리키는 모든 사용자를 반환합니다. `@a`처럼 여러 대상을 지정하는 선택자를 지원해야 하는 커맨드가 사용합니다.
- `TryResolveSingle`은 대상이 정확히 한 명이어야 하는 인자에 사용하며, 두 명 이상이 매칭되면 실패 사유를 `error`에 채웁니다.
- `TokenSyntaxHint`는 도움말과 오류 메시지에 표기하는 토큰 문법 안내 문자열입니다.

---

## 2. 허용 토큰

- `@a`, `@p`, `@r`, `@s`, `@e`, `@n`: 대상 선택자입니다. `TargetSelectorResolver`가 지원하는 인자(`tag`, `distance` 등)를 그대로 사용할 수 있습니다.
- `@self`: `@s`의 별칭이며, `@self[tag=triage]`처럼 인자를 붙인 형태도 `@s`와 동일하게 처리됩니다.
- `id:<uuid>`: 사용자 식별자로 조회합니다.
- `name:<displayName>`: 표시 이름으로 조회합니다.
- 접두사가 없는 문자열: 사용자 식별자로 먼저 조회하고, 찾지 못하면 표시 이름으로 조회합니다.

---

## 3. 해석 규칙

- 인자가 없는 `@s`와 `@self`는 실행자의 세션만으로 해석하기 때문에, 플레이어가 아직 스폰되지 않은 실행자도 자신을 지정할 수 있습니다.
- 그 밖의 선택자는 `TargetSelectorResolver`를 거쳐 연결 목록을 얻은 뒤, 각 연결을 `UserDescriptorService`로 사용자 설명자로 변환합니다.
- 동일한 사용자가 여러 번 매칭되면 식별자를 기준으로 한 번만 반환합니다.
- 시스템 실행처럼 `sender`가 없는 상황에서 `@s`를 사용하면 실패합니다.

---

## 4. 제한 사항

- 선택자는 씬에 스폰된 플레이어만 매칭합니다. 접속하지 않은 사용자를 지정하려면 식별자나 표시 이름을 사용해야 합니다.
- 사용자 설명자가 등록되지 않은 연결은 결과에서 제외됩니다.

---

## 5. 사용 예시

```
/permission user set @a op
/permission user get @s
/scoreboard players add @a[tag=triage] score 1
/tag show @r
/signal player name:Kim triage.done
/server kick id:0f3c9b6a
```

---

## 관련 문서

- [api:MultiplayerInfrastructure.Command.TargetSelectorResolver](MultiplayerInfrastructure.Command.TargetSelectorResolver.md)
- [api:MultiplayerInfrastructure.Permission](MultiplayerInfrastructure.Permission.md)
- [api:MultiplayerInfrastructure.Chat.ChatService](MultiplayerInfrastructure.Chat.ChatService.md)
