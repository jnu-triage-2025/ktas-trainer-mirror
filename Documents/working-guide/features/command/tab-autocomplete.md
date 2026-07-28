# 채팅 커맨드 Tab 자동완성 가이드

## 목적

채팅 입력창에서 `Tab`을 누르면 현재 커서가 있는 토큰을 커맨드, 하위 커맨드, 레지스트리 식별자, 플레이어 이름 등으로 완성할 수 있습니다. 자동완성 UI와 입력 이벤트는 공통 코드에 연결되어 있으므로, 새 커맨드는 커맨드 정의에 사용법을 정확히 추가하는 것만으로 대부분의 기능을 사용할 수 있습니다.

핵심 구현은 [`ChatCommandCompletionService`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/ChatCommandCompletionService.cs)이며, 입력창과의 연결은 [`ChatUIController`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/ChatUIController.cs), [`ChatPanelElement`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/ChatPanelElement.cs), [`PlayerController.Input`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Input.cs)에 있습니다.

## 새 커맨드에서 자동완성 사용하기

### 1. `UsageLine`을 실행 문법과 동일하게 작성하기

새 커맨드는 [`IChatCommandModel`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/IChatCommandModel.cs)과 함께 [`IChatCommandUsage`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/IChatCommandModel.cs)를 구현하고, 실행 가능한 전체 문법을 `UsageLines`에 등록합니다.

```csharp
public sealed class CommandDefinition_Example : IChatCommandModel, IChatCommandUsage
{
  public string CommandEntry => "example";

  public IReadOnlyList<UsageLine> UsageLines => new[]
  {
    new UsageLine("example list", "List available entries."),
    new UsageLine("example use <item> <target>", "Use an item on a target."),
  };

  // Description, PermissionIdentifier, Execute 등 IChatCommandModel 멤버는 생략.
}
```

위 정의만으로 다음 후보가 자동으로 수집됩니다.

| 입력 예 | 후보 종류 |
| --- | --- |
| `/exa` | 등록된 커맨드 이름 (`/example`) |
| `/example ` | `list`, `use` |
| `/example use ` | 등록된 아이템 식별자 |
| `/example use bandage ` | 접속 플레이어 이름과 타겟 셀렉터 |

`UsageLine.Syntax`는 파서가 실제로 받는 문법과 맞춰야 합니다. 특히 현재 자동완성 파서는 첫 토큰이 `CommandEntry`와 같은 사용법만 인수 경로 탐색에 사용합니다. 따라서 `new UsageLine("<target>", ...)`처럼 커맨드 이름을 생략한 보조 설명 행은 도움말에는 표시되지만 자동완성 경로에는 사용되지 않습니다. 보조 설명이 필요하더라도 자동완성 대상 문법은 `example <target>`처럼 완전한 경로로 작성합니다.

리터럴과 인수는 공백으로 구분하고, 선택 인수는 `[count]`, 필수 인수는 `<item>`처럼 표시합니다. 대소문자 비교는 커맨드와 사용법 모두 대소문자를 구분하지 않지만, 실제 파서와 `UsageLine`의 표기를 일치시키는 편이 안전합니다.

### 2. 지원되는 플레이스홀더를 활용하기

`<...>` 또는 `[...]`로 감싼 토큰은 의미 이름에 따라 후보가 연결됩니다. 이름은 밑줄과 하이픈을 제거한 뒤 비교됩니다.

| 플레이스홀더 이름에 포함할 문자열 | 후보 소스 |
| --- | --- |
| `item` | `RegistryType.Item`에 등록된 아이템 식별자 |
| `target`, `targets`, `player`, `player1`, `player2`, `user`, `name` | 접속 플레이어 표시 이름과 `@s`, `@a`, `@p`, `@r`, `@e`, `@n` |
| `waypoint` | `RegistryType.Waypoint` 식별자 |
| `preset` | 등록된 EntityPreset 식별자 |
| `scenario` | `RegistryType.ScenarioGraph` 식별자 |
| `problem` | `RegistryType.ProblemSet` 식별자 |
| `model`, `character` | `RegistryType.PlayerModel` 식별자 |
| `command` | 등록된 커맨드 이름. `/help <command>`에서 사용할 수 있도록 `/` 없이 반환 |
| `<a|b|c>` 또는 `[a|b|c]` | `a`, `b`, `c`라는 리터럴 대안 |

그 밖의 이름(예: `<count>`, `<objective>`, `<signal>`)은 자동으로 후보를 만들지 않습니다. 숫자 위치나 좌표 리터럴 `x`, `y`, `z`도 마찬가지입니다. 후보가 필요한 경우 아래의 직접 제공 인터페이스를 사용합니다.

선택 인수가 연속될 때는 일부 생략도 지원합니다. 예를 들어 `/give <item> [count] [target]`에서 count 위치에 숫자가 아닌 플레이어 이름 또는 `@`로 시작하는 값을 입력하면 target 후보가 제시될 수 있습니다. 다만 선택 인수의 의미가 서로 모호한 커맨드는 명시적인 provider를 구현하는 편이 좋습니다.

## 동적·맥락 의존 후보 확장하기

Usage 문법만으로 표현할 수 없는 후보(서버 상태, 이전 인수에 따른 하위 명령, 현재 선택된 객체 등)는 [`IChatCommandCompletion`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/IChatCommandCompletion.cs)을 커맨드 정의에 추가 구현합니다.

```csharp
using System;
using System.Collections.Generic;

public sealed class CommandDefinition_Example :
  IChatCommandModel, IChatCommandUsage, IChatCommandCompletion
{
  public IReadOnlyList<string> GetCompletions(
    int argIndex, string[] previousArgs, string partial)
  {
    if (argIndex == 0)
      return new[] { "give", "clear" };

    if (argIndex == 1
        && previousArgs.Length > 0
        && string.Equals(previousArgs[0], "give", StringComparison.OrdinalIgnoreCase))
      return ChatCommandCompletionService.CollectPlayersAndSelectors();

    return Array.Empty<string>();
  }

  // IChatCommandModel, IChatCommandUsage 멤버는 생략.
}
```

[`IChatCommandCompletion`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/IChatCommandCompletion.cs)의 인수 규칙은 다음과 같습니다.

- `argIndex`: 커맨드 이름을 제외한 현재 인수의 0부터 시작하는 위치. `/give en`의 `en`은 0입니다.
- `previousArgs`: 현재 토큰보다 앞에 있는 인수만 담긴 배열. 현재 토큰은 포함하지 않습니다.
- `partial`: 현재 토큰. 빈 문자열일 수 있습니다.
- 반환값: 현재 위치에 넣을 후보 문자열만 반환합니다. 인수 자동완성 후보에는 `/`를 붙이지 않습니다.

서비스가 반환값을 현재 `partial`에 대해 대소문자 무시 접두사 검색하고, 빈 값·중복을 제거한 뒤 정렬합니다. provider가 적용되지 않는 문맥에서는 빈 목록을 반환하면 `UsageLine` 기반 자동완성으로 넘어갑니다. provider가 후보를 반환했지만 현재 접두사와 맞는 후보가 하나도 없어도 Usage 기반 후보를 시도하므로, provider는 해당 위치에서 실제로 가능한 후보만 반환해야 합니다.

공통 후보가 필요하면 다음 정적 helper를 재사용할 수 있습니다.

```csharp
ChatCommandCompletionService.CollectItemIdentifiers();
ChatCommandCompletionService.CollectEntityPresetIdentifiers();
ChatCommandCompletionService.CollectWaypointIdentifiers();
ChatCommandCompletionService.CollectPlayerModelIdentifiers();
ChatCommandCompletionService.CollectScenarioIdentifiers();
ChatCommandCompletionService.CollectProblemSetIdentifiers();
ChatCommandCompletionService.CollectPlayersAndSelectors();
```

provider는 `Tab`을 누를 때마다 호출될 수 있으므로 커맨드 상태를 변경하지 않아야 합니다. 네트워크 RPC, 블로킹 작업, 긴 로딩을 수행하지 말고, 서버 전용 데이터는 로컬에서 조회 가능하게 준비하거나 후보가 없을 때 안전하게 빈 목록을 반환합니다. 자동완성은 편의 기능이며, 실제 실행 시 권한·존재 여부·대상 유효성 검사는 기존 커맨드 실행 로직에서 계속 수행해야 합니다.

## 사용자 동작과 세션 규칙

- `/`로 시작하면 커맨드 모드, 그 외에는 일반 채팅의 플레이어 이름 자동완성 모드입니다.
- 같은 입력 결과에서 `Tab`을 반복하면 후보를 순환합니다.
- `Tab` 외 키 입력, 텍스트 수정, 커서 이동은 다음 자동완성 세션을 시작하게 합니다.
- 커서가 토큰 중간에 있어도 해당 토큰 전체를 후보로 치환합니다.
- 완료된 토큰 뒤에는 필요한 경우 공백을 추가해 다음 인수를 바로 입력할 수 있게 합니다. 기존 공백이나 뒤쪽 텍스트는 중복 삽입하지 않고 보존합니다.
- 입력창에 포커스가 없을 때의 `Tab`은 커맨드 자동완성으로 처리하지 않습니다.

예를 들어 `/item g`에서 `Tab`을 누르면 `/item give `가 되고, 다시 `Tab`을 누르면 원래 `/item g`를 기준으로 다음 후보를 찾습니다. 따라서 provider가 반환하는 후보는 이미 이전에 자동완성된 문자열을 전제로 만들지 말고, 전달받은 `previousArgs`와 `partial`만 기준으로 계산해야 합니다.

## 구현 체크리스트

새 커맨드를 추가할 때 다음 순서로 확인합니다.

1. [`ChatCommandService`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs)에 커맨드가 등록되고 `CommandEntry`가 중복되지 않는지 확인합니다.
2. 실행 파서가 허용하는 모든 주요 경로를 완전한 `UsageLine`으로 작성합니다.
3. 아이템·대상·웨이포인트 등 지원되는 플레이스홀더 이름을 사용합니다.
4. 숫자·객체 선택·서버 상태처럼 Usage만으로 후보를 만들 수 없는 인수는 `IChatCommandCompletion`을 구현합니다.
5. `/command`, 하위 커맨드, 빈 인수, 접두사 인수, 반복 `Tab`, 일반 키 입력 후 재시작을 확인합니다.
6. 커서가 토큰 앞·중간·끝에 있는 경우와 토큰 뒤에 이미 텍스트가 있는 경우를 확인합니다.
7. 후보가 없는 초기화 시점과 레지스트리에 데이터가 없는 경우에도 오류 없이 입력이 유지되는지 확인합니다.

자동완성 동작을 수정해야 할 때는 후보 수집뿐 아니라 입력창 포커스·키 이벤트 연결도 함께 확인합니다. 관련 진입점은 [`ChatUIController.HandleTabKey`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/ChatUIController.cs), [`ChatPanelElement.InputKeyPressed`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/UI/VisualElements/ChatPanelElement.cs), [`PlayerController.Input`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Input.cs)입니다.

## 관련 구현

- [`ChatCommandCompletionService.cs`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/ChatCommandCompletionService.cs): 후보 수집, 필터링, Tab 순환 세션
- [`IChatCommandCompletion.cs`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/IChatCommandCompletion.cs): 커맨드별 동적 후보 provider 계약
- [`IChatCommandModel.cs`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/IChatCommandModel.cs): `IChatCommandModel`, `IChatCommandUsage`, `UsageLine` 정의
- [`CommandService.cs`](../../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs): 커맨드 등록·조회
- [`give-clean-datapack.md`](./give-clean-datapack.md): 기존 `/give`, `/clean` 사용법과 관련 구현 예
