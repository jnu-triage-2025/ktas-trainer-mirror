# <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression"></a> Class ItemMissingAssetSuppression

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

아이템 3D 모델(프리팹) 및 아이콘 스프라이트 누락 warning의 노이즈를 줄이기 위한 억제 헬퍼입니다.

<p>
<b>선언적(Attribute) 억제</b>: <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute" data-throw-if-not-resolved="false"></xref> 또는
<xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute" data-throw-if-not-resolved="false"></xref> 를 Item 파생 클래스에 적용하면
해당 클래스(및 <code>Inherited=true</code> 로 자식 클래스)의 누락 warning이 자동 생략됩니다.
</p>

<p>
<b>프로그래밍(RegisterIdentifier) 억제</b>: Item 클래스가 아닌 식별자(UI 아이콘 등)는
<xref href="MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.RegisterSuppressedSpriteIdentifier(System.String)" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.RegisterSuppressedModelIdentifier(System.String)" data-throw-if-not-resolved="false"></xref>
로 등록하면 해당 identifier의 warning이 생략됩니다.
</p>

```csharp
public static class ItemMissingAssetSuppression
```

#### Inheritance

object ← 
[ItemMissingAssetSuppression](MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_Clear"></a> Clear\(\)

등록된 모든 프로그래밍 억제 목록을 비웁니다(테스트/초기화용).

```csharp
public static void Clear()
```

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_EnsureScanned"></a> EnsureScanned\(\)

로딩된 모든 어셈블리에서 <xref href="MultiplayerInfrastructure.ItemSystem.Item" data-throw-if-not-resolved="false"></xref> 파생 클래스를 스캔하여
<xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute" data-throw-if-not-resolved="false"></xref>
가 적용된 클래스의 <code>Identifier</code> const 값을 캐시에 등록합니다.
최초 호출 시 1회만 실행되며 이후 호출은 무시됩니다.

```csharp
public static void EnsureScanned()
```

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_RegisterSuppressedModelIdentifier_System_String_"></a> RegisterSuppressedModelIdentifier\(string\)

Item 클래스가 아닌 identifier(예: UI 아이콘)의 3D 모델 누락 warning을 억제하도록 등록합니다.

```csharp
public static void RegisterSuppressedModelIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_RegisterSuppressedSpriteIdentifier_System_String_"></a> RegisterSuppressedSpriteIdentifier\(string\)

Item 클래스가 아닌 identifier(예: UI 아이콘)의 스프라이트 누락 warning을 억제하도록 등록합니다.

```csharp
public static void RegisterSuppressedSpriteIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_ShouldSuppressModelMissingWarning_MultiplayerInfrastructure_ItemSystem_Item_"></a> ShouldSuppressModelMissingWarning\(Item\)

지정 Item 인스턴스의 클래스에 <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute" data-throw-if-not-resolved="false"></xref> 가
적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.

```csharp
public static bool ShouldSuppressModelMissingWarning(Item item)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_ShouldSuppressModelMissingWarning_System_Type_"></a> ShouldSuppressModelMissingWarning\(Type\)

지정 Type에 <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute" data-throw-if-not-resolved="false"></xref> 가
적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.

```csharp
public static bool ShouldSuppressModelMissingWarning(Type itemType)
```

#### Parameters

`itemType` Type

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_ShouldSuppressModelMissingWarning_System_String_"></a> ShouldSuppressModelMissingWarning\(string\)

지정 identifier의 3D 모델 누락 warning을 생략해야 하는지 여부입니다.
Item 어트리뷰트 스캔 결과 및 <xref href="MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.RegisterSuppressedModelIdentifier(System.String)" data-throw-if-not-resolved="false"></xref> 등록을 확인합니다.

```csharp
public static bool ShouldSuppressModelMissingWarning(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_ShouldSuppressSpriteMissingWarning_MultiplayerInfrastructure_ItemSystem_Item_"></a> ShouldSuppressSpriteMissingWarning\(Item\)

지정 Item 인스턴스의 클래스에 <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute" data-throw-if-not-resolved="false"></xref> 가
적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.

```csharp
public static bool ShouldSuppressSpriteMissingWarning(Item item)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_ShouldSuppressSpriteMissingWarning_System_Type_"></a> ShouldSuppressSpriteMissingWarning\(Type\)

지정 Type에 <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute" data-throw-if-not-resolved="false"></xref> 가
적용되어 있는지 확인합니다. 결과는 타입별로 캐시됩니다.

```csharp
public static bool ShouldSuppressSpriteMissingWarning(Type itemType)
```

#### Parameters

`itemType` Type

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemMissingAssetSuppression_ShouldSuppressSpriteMissingWarning_System_String_"></a> ShouldSuppressSpriteMissingWarning\(string\)

지정 identifier의 스프라이트 누락 warning을 생략해야 하는지 여부입니다.
Item 어트리뷰트 스캔 결과 및 <xref href="MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.RegisterSuppressedSpriteIdentifier(System.String)" data-throw-if-not-resolved="false"></xref> 등록을 확인합니다.

```csharp
public static bool ShouldSuppressSpriteMissingWarning(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 bool

