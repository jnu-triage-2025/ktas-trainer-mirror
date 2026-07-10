# 아이템 자동 조합 시스템 설정 가이드

## 개요
아이템 자동 조합 시스템은 인벤토리에 특정 재료가 충족되면 즉시 정해진 결과 아이템으로 변환되는 로직입니다. 시나리오에 의존하지 않고 인벤토리 레벨에서 항상 동작합니다.

## 주요 구성 요소
- `ItemCombineRecipe` : 하나의 조합 레시피를 정의 (재료 → 결과)
- `ItemCombineRecipeRegistry` : 레시피 등록 및 매칭 로직 제공
- `PlayerController.Inventory.TryAutoCombineItems()` : 아이템 추가 후 자동 조합 시도
- `RegisteringMultiplayerInfrastructureSupport.Item.RegisterAllCombineRecipes()` : 기본 레시피 등록 위치

## 레시피 등록 방법
1. `RegisteringMultiplayerInfrastructureSupport.Item.cs` 파일을 엽니다.
2. `RegisterAllCombineRecipes()` 메서드 내에 새로운 레시피를 추가합니다.
   ```csharp
   private static void RegisterAllCombineRecipes()
   {
       // 기존 레시피 예시
       ItemCombineRecipeRegistry.Register(
           new ItemCombineRecipe()
               .Requires("laryngoscope_blade", 1)
               .Requires("laryngoscope_handle", 1)
               .Produces("laryngoscope", 1)
       );

       ItemCombineRecipeRegistry.Register(
           new ItemCombineRecipe()
               .Requires("endotracheal_tube", 1)
               .Requires("stylet", 1)
               .Produces("endotracheal_tube_prepared", 1)
       );

       // === 여기에 새 레시피 추가 ===
       // 예: "철괴 2개 + 목재 4개 → 강철 기둥 1개"
       ItemCombineRecipeRegistry.Register(
           new ItemCombineRecipe()
               .Requires("iron_ingot", 2)
               .Requires("wood", 4)
               .Produces("steel_pillar", 1)
       );
   }
   ```
3. 변경 후 Unity 에디터에서 스크립트를 다시 컴파일합니다.

## 아이템 설명 업데이트 (선택 사항)
조합 가능 아이템의 설명에 자동 조합 안내 문구를 추가하면 플레이어가 직관적으로 이해하기 쉽습니다.
예시:
```csharp
public const string Description = "[철괴, 목재]를 획득하면 [강철 기둥]으로 자동 조합됩니다.";
```

## 검증 방법
1. Play 모드에서 `/give 철괴 2` 및 `/give 나무 4` 순서대로 지급합니다.
2. 인벤토리에 재료가 채워지는 즉시 자동으로 강철 기둥 1개가 생성되고 재료가 소모되는지 확인합니다.
3. 에디터 콘솔에 `[PlayerController] AutoCombine:` 로그가 출력되는지 확인합니다.
4. 한쪽 재료만 부족한 경우 조합이 일어나지 않는지도 확인합니다.

## 주의 사항
- 레시피는 **첫 번째 충족된 레시피**부터 적용됩니다. 중복 가능한 조합이 있을 경우 순서에 주의하세요.
- 연속 조합이 가능하며, while 루프로 재료가 충분한 한 계속 반복합니다.
- 에디터 전용 디버그 로그는 배포 빌드에서는 나타나지 않습니다.
