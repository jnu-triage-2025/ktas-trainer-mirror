# <a id="TriageTrainer_MultiplayerInfrastructureSupports_RegisteringMultiplayerInfrastructureSupport"></a> Class RegisteringMultiplayerInfrastructureSupport

Namespace: [TriageTrainer.MultiplayerInfrastructureSupports](TriageTrainer.MultiplayerInfrastructureSupports.md)  
Assembly: Assembly\-CSharp.dll  

MultiplayerInfrastructure 레지스트리 연동 지원 MonoBehaviour입니다.

Awake() 에서 아이템 정의를 Registry에 등록하고,
등록된 모든 아이템에 대해 필요한 리소스(아이콘 스프라이트, 3D 모델 프리팹)가
Resources 폴더 내에 존재하는지 검증합니다.

```csharp
public class RegisteringMultiplayerInfrastructureSupport : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[RegisteringMultiplayerInfrastructureSupport](TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.md)

## Methods

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_RegisteringMultiplayerInfrastructureSupport_RegisterAllCombineRecipes"></a> RegisterAllCombineRecipes\(\)

아이템 자동 조합 레시피를 등록합니다.

레시피 형식:
  new ItemCombineRecipe(결과_식별자)
    .Requires(재료_식별자, 필요_수량)
    ...
    .Produces(생성_수량);

새 조합 규칙 추가 시 이 메서드에만 등록하면 됩니다.

```csharp
public static void RegisterAllCombineRecipes()
```

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_RegisteringMultiplayerInfrastructureSupport_RegisterAllItems"></a> RegisterAllItems\(\)

```csharp
public static void RegisterAllItems()
```

### <a id="TriageTrainer_MultiplayerInfrastructureSupports_RegisteringMultiplayerInfrastructureSupport_ValidateItemResources"></a> ValidateItemResources\(\)

Registry에 등록된 모든 아이템 식별자에 대해 필요한 리소스가
Resources 폴더 내에 존재하는지 확인합니다.

검증 항목:
  • 아이콘 스프라이트 : Resources/{DefaultsItemRegistry.ItemTexturesPath}/{id}
  • 3D 모델 프리팹   : Resources/Models/Items/{id}

누락된 리소스는 Debug.LogWarning 으로 출력됩니다.

```csharp
public void ValidateItemResources()
```

