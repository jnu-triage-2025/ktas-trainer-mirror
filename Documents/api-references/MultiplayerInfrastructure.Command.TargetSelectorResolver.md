# API 레퍼런스: `MultiplayerInfrastructure.Command.TargetSelectorResolver`

> **네임스페이스:** `MultiplayerInfrastructure.Command`  
> **형태:** `static class`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Command/TargetSelectorResolver.cs`

---

## 0. 문서 목적

`TargetSelectorResolver`는 채팅 커맨드에서 `<target>`에 들어오는 선택자 문자열을 파싱하고,
대상 플레이어 목록으로 해석하는 공통 유틸리티입니다.

---

## 1. 공개 API

```csharp
public static bool TryResolveTargets(
  NetworkConnection sender,
  string raw,
  out List<NetworkConnection> targets,
  out string error)
```

- `raw`는 `@`로 시작하는 선택자 문자열입니다.
- 성공 시 `targets`에 대상 연결 목록을 반환합니다.
- 실패 시 `error`에 이유가 채워집니다.

---

## 2. 지원 선택자

- `@p`: 가장 가까운 플레이어
- `@a`: 모든 플레이어
- `@r`: 무작위 플레이어
- `@s`: 실행자 자신
- `@e`: 모든 엔티티 (현재 플레이어만 허용)
- `@n`: 가장 가까운 엔티티 (현재 플레이어만 허용)

`@e`, `@n`은 엔티티 대상 구현이 완성되지 않아 `type=player` 조건이 필요합니다.
예: `@e[type=player]`

---

## 3. 지원 대상 선택 인자

- `x`, `y`, `z` (좌표)
- `distance` (구간)
- `dx`, `dy`, `dz` (직육면체 범위)
- `tag` (플레이어 태그 필터)
- `type` (엔티티 타입 필터)

### 거리 구간 예시
- `distance=..5`
- `distance=5`
- `distance=5..9`

### 태그 필터 예시
- `tag=afk`
- `tag="생존자"`
- `tag=!afk`
- `tag=` (태그가 없는 대상)

---

## 4. 해석 규칙

- 선택자 문자열은 `@x[키=값,키=값]` 형태를 사용합니다.
- 인자는 대소문자를 구분합니다.
- 지원하지 않는 인자 키는 무시됩니다.
- `@p/@n/@r`는 실행자 위치를 기준으로 동작합니다.
- `x/y/z`를 모두 제공하면 실행자 위치 없이도 기준점을 계산할 수 있습니다.
- `tag`는 `PlayerTagService` 기반으로 평가됩니다.

---

## 5. 제한 사항

- 엔티티 대상은 아직 지원하지 않습니다.
  - `@e` 또는 `@n` 사용 시 `type=player`만 허용됩니다.
- 선택자에 의해 다수 대상이 매칭되면, 단일 타겟이 필요한 커맨드는 실패합니다.

---

## 6. 사용 예시

```
/title @a[distance=..15] title Mission Start
/scenario execute @p[type=player] hospital_intro
/give bandage 3 @s
/tag add @r[tag=!afk] responder
```

---

## 관련 문서

- [api:MultiplayerInfrastructure.Chat.ChatService](MultiplayerInfrastructure.Chat.ChatService.md)
- [api:MultiplayerInfrastructure.Command.ChatCommandExtensions](MultiplayerInfrastructure.Command.ChatCommandExtensions.md)
