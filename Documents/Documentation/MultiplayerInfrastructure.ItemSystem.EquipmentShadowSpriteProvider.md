# <a id="MultiplayerInfrastructure_ItemSystem_EquipmentShadowSpriteProvider"></a> Class EquipmentShadowSpriteProvider

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

장비 슬롯의 그림자 스프라이트를 제공하는 정적 클래스입니다.

<p>
그림자 스프라이트는 <code>.png</code> 파일로 Resources 폴더에 존재하며,
리소스 경로가 장비 슬롯 타입별로 하드코딩됩니다.
최초 로드 후 캐시되어 재사용됩니다.
</p>

<p>
Z-Ordering: 장비 슬롯에서 그림자는 z+2(UI 위, 아이템 아래)에 위치합니다.
</p>

```csharp
public static class EquipmentShadowSpriteProvider
```

#### Inheritance

object ← 
[EquipmentShadowSpriteProvider](MultiplayerInfrastructure.ItemSystem.EquipmentShadowSpriteProvider.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_EquipmentShadowSpriteProvider_ClearCache"></a> ClearCache\(\)

캐시를 초기화합니다(Resources 언로드/도메인 리로드 시).

```csharp
public static void ClearCache()
```

### <a id="MultiplayerInfrastructure_ItemSystem_EquipmentShadowSpriteProvider_GetShadowTexture_MultiplayerInfrastructure_Player_EquipmentSlotType_"></a> GetShadowTexture\(EquipmentSlotType\)

지정 장비 슬롯 타입에 해당하는 그림자 텍스처를 반환합니다.
최초 호출 시 Resources 에서 로드하며, 이후 캐시된 값을 반환합니다.
로드 실패 시 null 을 반환합니다.

```csharp
public static Texture2D GetShadowTexture(EquipmentSlotType slotType)
```

#### Parameters

`slotType` [EquipmentSlotType](MultiplayerInfrastructure.Player.EquipmentSlotType.md)

#### Returns

 Texture2D

### <a id="MultiplayerInfrastructure_ItemSystem_EquipmentShadowSpriteProvider_GetShadowTextureForSlot_MultiplayerInfrastructure_ItemSystem_Item_MultiplayerInfrastructure_Player_EquipmentSlotType_"></a> GetShadowTextureForSlot\(Item, EquipmentSlotType\)

지정 아이템이 특정 장비 슬롯에 장착되었을 때의 그림자 텍스처를 반환합니다.
현재 구현은 슬롯 타입 기반으로 그림자를 반환합니다(아이템과 무관).

```csharp
public static Texture2D GetShadowTextureForSlot(Item item, EquipmentSlotType slotType)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

`slotType` [EquipmentSlotType](MultiplayerInfrastructure.Player.EquipmentSlotType.md)

#### Returns

 Texture2D

