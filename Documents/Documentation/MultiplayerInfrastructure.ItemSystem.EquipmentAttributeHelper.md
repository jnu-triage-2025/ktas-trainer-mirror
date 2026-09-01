# <a id="MultiplayerInfrastructure_ItemSystem_EquipmentAttributeHelper"></a> Class EquipmentAttributeHelper

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

장비 Attribute 검사를 위한 정적 헬퍼 클래스입니다.
Item 인스턴스의 타입에 적용된 장비 Attribute를 리플렉션으로 확인합니다.
결과는 타입별로 캐시되어 반복 조회 시 O(1)입니다.

```csharp
public static class EquipmentAttributeHelper
```

#### Inheritance

object ← 
[EquipmentAttributeHelper](MultiplayerInfrastructure.ItemSystem.EquipmentAttributeHelper.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_EquipmentAttributeHelper_ClearCache"></a> ClearCache\(\)

캐시를 초기화합니다(테스트/도메인 리로드 용).

```csharp
public static void ClearCache()
```

### <a id="MultiplayerInfrastructure_ItemSystem_EquipmentAttributeHelper_IsEquippableGlove_MultiplayerInfrastructure_ItemSystem_Item_"></a> IsEquippableGlove\(Item\)

지정 Item 인스턴스의 클래스에 <xref href="MultiplayerInfrastructure.ItemSystem.EquippableGloveAttribute" data-throw-if-not-resolved="false"></xref> 가
적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.

```csharp
public static bool IsEquippableGlove(Item item)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_EquipmentAttributeHelper_IsEquippableGlove_System_Type_"></a> IsEquippableGlove\(Type\)

지정 Type에 <xref href="MultiplayerInfrastructure.ItemSystem.EquippableGloveAttribute" data-throw-if-not-resolved="false"></xref> 가
적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.

```csharp
public static bool IsEquippableGlove(Type itemType)
```

#### Parameters

`itemType` Type

#### Returns

 bool

