# <a id="MultiplayerInfrastructure_Commons_IconSpriteReference"></a> Class IconSpriteReference

Namespace: [MultiplayerInfrastructure.Commons](MultiplayerInfrastructure.Commons.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public class IconSpriteReference
```

#### Inheritance

object ← 
[IconSpriteReference](MultiplayerInfrastructure.Commons.IconSpriteReference.md)

## Properties

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_IconDefinition"></a> IconDefinition

```csharp
public IconSpriteDefinitions IconDefinition { get; }
```

#### Property Value

 [IconSpriteDefinitions](MultiplayerInfrastructure.Commons.IconSpriteDefinitions.md)

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_IconRegistryIdentifier"></a> IconRegistryIdentifier

```csharp
public string IconRegistryIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_IsExplicitNone"></a> IsExplicitNone

```csharp
public bool IsExplicitNone { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_IsUnspecified"></a> IsUnspecified

명시적으로 아이콘이 지정되지 않았는지 여부.
이 경우 사용처에서 컨텍스트별 기본 아이콘(예: 시나리오 실행 아이콘)을 적용할 수 있습니다.
직접 스프라이트, 정의(enum), 레지스트리 식별자 중 어느 것도 지정되지 않았고,
명시적 None도 아닌 상태를 의미합니다.

```csharp
public bool IsUnspecified { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_Sprite"></a> Sprite

```csharp
public Sprite Sprite { get; }
```

#### Property Value

 Sprite

## Methods

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_Clone"></a> Clone\(\)

```csharp
public IconSpriteReference Clone()
```

#### Returns

 [IconSpriteReference](MultiplayerInfrastructure.Commons.IconSpriteReference.md)

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_Resolve"></a> Resolve\(\)

```csharp
public Sprite Resolve()
```

#### Returns

 Sprite

### <a id="MultiplayerInfrastructure_Commons_IconSpriteReference_Resolve_System_String_"></a> Resolve\(string\)

아이콘을 해석합니다. 명시적으로 아이콘이 지정되지 않았고(<xref href="MultiplayerInfrastructure.Commons.IconSpriteReference.IsUnspecified" data-throw-if-not-resolved="false"></xref>)
<code class="paramref">defaultRegistryIdentifier</code> 가 주어지면 해당 식별자의 아이콘을 기본값으로 사용합니다.
명시적 None(<xref href="MultiplayerInfrastructure.Commons.IconSpriteReference.IsExplicitNone" data-throw-if-not-resolved="false"></xref>)인 경우에는 기본값을 적용하지 않고 null을 반환합니다.

```csharp
public Sprite Resolve(string defaultRegistryIdentifier)
```

#### Parameters

`defaultRegistryIdentifier` string

#### Returns

 Sprite

