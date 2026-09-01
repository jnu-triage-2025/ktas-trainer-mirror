# <a id="MultiplayerInfrastructure_UI_ItemDurabilityBar"></a> Class ItemDurabilityBar

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

인벤토리 계열 슬롯에서 공통으로 사용하는 아이템 내구도 막대입니다.

```csharp
public static class ItemDurabilityBar
```

#### Inheritance

object ← 
[ItemDurabilityBar](MultiplayerInfrastructure.UI.ItemDurabilityBar.md)

## Fields

### <a id="MultiplayerInfrastructure_UI_ItemDurabilityBar_FillName"></a> FillName

```csharp
public const string FillName = "DurabilityFill"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_UI_ItemDurabilityBar_TrackName"></a> TrackName

```csharp
public const string TrackName = "DurabilityTrack"
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_UI_ItemDurabilityBar_Ensure_UnityEngine_UIElements_VisualElement_System_String_System_String_"></a> Ensure\(VisualElement, string, string\)

```csharp
public static VisualElement Ensure(VisualElement slot, string trackClass, string fillClass)
```

#### Parameters

`slot` VisualElement

`trackClass` string

`fillClass` string

#### Returns

 VisualElement

### <a id="MultiplayerInfrastructure_UI_ItemDurabilityBar_GetColor_System_Single_"></a> GetColor\(float\)

```csharp
public static Color GetColor(float ratio)
```

#### Parameters

`ratio` float

#### Returns

 Color

### <a id="MultiplayerInfrastructure_UI_ItemDurabilityBar_Update_UnityEngine_UIElements_VisualElement_MultiplayerInfrastructure_ItemSystem_Item_"></a> Update\(VisualElement, Item\)

```csharp
public static void Update(VisualElement track, Item item)
```

#### Parameters

`track` VisualElement

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

