# Tag Module

플레이어 태그는 시나리오가 참가자의 역할을 구분하는 데 쓰는 문자열입니다. `PlayerTagService`가 서버에서 태그를 저장하고 옵저버에게 동기화하며, `/tag` 커맨드와 시나리오의 PlayerTag 노드가 태그를 부여하거나 제거합니다.

## 태그 정의와 권한 정책

`PlayerTagDefinitionService`는 태그마다 "부여하거나 제거할 때 `tag` 커맨드 권한이 필요한지"를 담은 정의(`PlayerTagDefinition`)를 관리합니다.

| 상황 | `/tag add`, `/tag remove`, `/tag change` 실행 가능 여부 |
| :-- | :-- |
| 요청자에게 `tag` 권한이 있음 | 태그 정의와 무관하게 실행할 수 있습니다. |
| 권한이 없고, 태그가 `requiresPermission: false`로 정의됨 | 실행할 수 있습니다. `change`는 `from`과 `to` 두 태그가 모두 그렇게 정의되어야 합니다. |
| 권한이 없고, 태그가 `requiresPermission: true`로 정의되었거나 정의되지 않음 | 거부됩니다. |

`/tag show`는 정의와 무관하게 `tag` 권한을 요구합니다. 정의되지 않은 태그가 권한을 요구하는 것은 `PlayerTagDefinitionService.DefaultRequiresPermission`의 기본값(true)에 따른 동작입니다.

이 판단은 `ChatCommandService`가 `IChatCommandPermissionExemption`을 통해 수행합니다. 역할 권한 검사에 실패한 뒤에만 커맨드에 면제 여부를 묻기 때문에, 권한이 있는 사용자의 동작은 바뀌지 않습니다. 데이터팩 별칭이 `/tag`를 실행하는 경우에는 별칭이 대상 커맨드에 실제로 전달할 인자로 같은 판단을 내립니다.

## 정의 출처

정의는 두 곳에서 읽습니다. 같은 태그를 여러 출처가 정의하면 나중에 등록된 정의가 유효하고, 그 출처가 해제되면 이전 정의로 되돌아갑니다.

1. **내장 정의 파일** `Resources/Tag/*.tags.json`: 첫 조회 때 모든 모듈의 Resources 폴더에서 자동으로 읽습니다. 간호사 역할 태그 `nurse_a`부터 `nurse_d`까지는 `Assets/Modules/TriageTrainer/Resources/Tag/triage_roles.tags.json`에서 권한이 필요 없는 태그로 선언되어 있습니다.
2. **데이터팩** `tagDefinitions`: 데이터팩이 등록될 때 함께 등록되고, 해제될 때 함께 해제됩니다. 형식은 [Datapack 작성 가이드](../../../../../Documents/requirements/datapack/datapack-authoring-guide.md)를 참고합니다.

내장 정의 파일의 형식은 다음과 같습니다.

```json
{
  "tags": [
    { "identifier": "nurse_a", "requiresPermission": false, "description": "간호사 A 역할" }
  ]
}
```

- `identifier`: 태그 식별자입니다. 플레이어에게 저장되는 문자열과 대소문자까지 같아야 합니다.
- `requiresPermission`: 생략하면 `true`입니다. 실수로 생략한 정의가 권한 면제 통로가 되지 않도록 안전 측 기본값을 둡니다.
- `description`: 사람이 읽는 설명이며 동작에는 쓰이지 않습니다.

## 코드에서 정의를 다루는 방법

```csharp
PlayerTagDefinitionService.Define("observer", requiresPermission: false, source: "my-feature");
bool gated = PlayerTagDefinitionService.RequiresPermission("observer"); // false
PlayerTagDefinitionService.Undefine("my-feature");
```

`source`는 등록을 묶어서 해제하기 위한 문자열입니다. 데이터팩은 `datapack:<packId>`, 내장 파일은 `resources:<에셋 이름>`을 씁니다.
