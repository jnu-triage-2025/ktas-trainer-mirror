# 아이템 자동 조합 설정 가이드

이 가이드는 **복수의 아이템을 인벤토리에 모두 획득했을 때 자동으로 하나의 아이템으로 합쳐지는** 자동 조합 기능을 설명합니다. 비전문가 작업자도 따라 할 수 있도록 단계별로 설명합니다.

---

## 0. 기능 개요

자동 조합은 다음과 같이 동작합니다.

1. 플레이어가 아이템을 획득하여 인벤토리에 추가됩니다.
2. 시스템이 등록된 레시피 목록을 검사합니다.
3. 레시피의 재료가 모두 충족되어 있으면 **재료를 소비하고 결과 아이템을 즉시 지급**합니다.
4. 이 과정은 연속으로 발생할 수 있습니다(예: 조합 결과물이 또 다른 레시피의 재료가 되는 경우).

현재 등록된 레시피:

| 재료 | → | 결과 |
|---|---|---|
| 후두경 블레이드 × 1 + 후두경 손잡이 × 1 | → | 후두경 × 1 |
| 기관내관 × 1 + 스타일렛 × 1 | → | 기관내관 (준비 완료) × 1 |

---

## 1. 새 레시피 추가 방법

새로운 자동 조합 규칙을 추가하려면 **한 곳의 파일만 수정**하면 됩니다.

### 파일 위치

```
Assets/Modules/TriageTrainer/Scripts/MultiplayerInfrastructureSupports/
  RegisteringMultiplayerInfrastructureSupport.Item.cs
```

### 수정 위치

파일 안에 `RegisterAllCombineRecipes()` 메서드가 있습니다. 이 메서드 안에 새 레시피를 추가합니다.

```csharp
public static void RegisterAllCombineRecipes()
{
  ItemCombineRecipeRegistry.Clear();

  // 여기에 레시피를 추가합니다.

  // 예시: A 2개 + B 1개 → C 1개
  ItemCombineRecipeRegistry.Register(
    new ItemCombineRecipe("c")        // 결과 아이템 Identifier
      .Requires("a", 2)               // 재료 a 2개
      .Requires("b", 1)               // 재료 b 1개
      .Produces(1));                  // 결과 1개 생성
}
```

### 레시피 작성 규칙

| 항목 | 설명 |
|---|---|
| `new ItemCombineRecipe("결과_식별자")` | 결과 아이템의 `Identifier`를 문자열로 입력합니다. |
| `.Requires("재료_식별자", 수량)` | 필요한 재료를 추가합니다. 수량을 생략하면 기본값 1이 사용됩니다. 여러 재료가 필요하면 `.Requires(...)` 를 연달아 작성합니다. |
| `.Produces(수량)` | 조합 시 생성될 결과 아이템 수량입니다. 생략하면 기본값 1이 사용됩니다. |

> **중요:** `Identifier` 값은 아이템 정의 파일(`*.cs`)의 `public const string Identifier` 값과 정확히 일치해야 합니다. 오타가 있으면 조합이 동작하지 않습니다.

---

## 2. 재료·결과 아이템의 Identifier 확인 방법

아이템 정의 파일은 다음 경로에 있습니다:

```
Assets/Modules/TriageTrainer/Scripts/Items/Definitions/
```

예를 들어 `후두경 블레이드`의 Identifier를 확인하려면 `LaryngoScopeBlade.cs`를 열어 `const string Identifier` 값을 확인합니다.

```csharp
// LaryngoScopeBlade.cs
public const string Identifier = "laryngoscope_blade";  ← 이 값을 사용
```

---

## 3. 아이템 Description에 조합 안내 문구 추가하기

조합 가능한 아이템은 **인벤토리 툴팁**에 조합 안내 문구가 표시되도록 `Description`을 업데이트하는 것을 권장합니다.

형식 예시:

```
[재료 A, 재료 B]을 획득하면 [결과 아이템]으로 자동 조합됩니다.
```

수정 위치는 해당 아이템의 정의 파일 `const string Description` 입니다.

```csharp
// LaryngoScopeBlade.cs (예시)
public const string Description = "후두경 손잡이와 결합하여 사용합니다. "
  + "[후두경 블레이드, 후두경 손잡이]을 획득하면 [후두경]으로 자동 조합됩니다.";
```

> Description은 인벤토리 슬롯에 마우스를 올릴 때 표시되는 툴팁에 출력됩니다.

---

## 4. 동작 확인 방법

1. Unity 에디터에서 **Play 모드**를 시작합니다.
2. 채팅창에 `/give <identifier>` 명령어로 재료 아이템을 지급합니다.
   - 예: `/give laryngoscope_blade` 후 `/give laryngoscope_handle`
3. 두 재료가 모두 인벤토리에 들어오는 순간 자동으로 `후두경`으로 바뀌는지 확인합니다.
4. 에디터 콘솔에서 `[PlayerController] AutoCombine:` 로그를 통해 어떤 재료가 소비되어 어떤 아이템이 생성되었는지 확인할 수 있습니다.

---

## 5. 자주 겪는 문제

| 증상 | 원인 / 해결 |
|---|---|
| 두 재료를 획득했는데 조합이 안 됨 | `RegisterAllCombineRecipes()`에 레시피가 등록되었는지 확인합니다. Identifier 오타가 없는지 확인합니다. |
| 결과 아이템이 생성되지 않고 재료만 사라짐 | 결과 아이템 Identifier가 `RegisterAllItems()`에 등록되어 있는지 확인합니다. |
| 조합이 여러 번 반복됨 | 레시피가 중복 등록되어 있을 수 있습니다. `RegisterAllCombineRecipes()` 안에 동일한 레시피가 두 번 선언되지 않았는지 확인합니다. |
| 에디터 콘솔에 AutoCombine 로그가 안 보임 | 이 로그는 `UNITY_EDITOR` 빌드에서만 출력됩니다. 플레이 모드에서 Console 창이 열려 있는지 확인합니다. |

---

## 6. 관련 문서

- API 레퍼런스(도메인 전문가용): [`../../../api-references/MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md`](../../../api-references/MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)
- 아이템 정의/등록 방법: [`../../item-authoring.md`](../../item-authoring.md)
