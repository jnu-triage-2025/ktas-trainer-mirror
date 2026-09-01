# <a id="MultiplayerInfrastructure_InteractableEntity_ItemRequirement"></a> Struct ItemRequirement

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

아이템 제출에서 요구되는 단일 아이템을 식별자와 수량으로 표현한다.
인벤토리 아이템은 <code>CurrentIdentifier</code> 와 <code>CurrentStackCount</code> 로 식별되므로,
이 구조체는 그와 동일한 (identifier, count) 쌍으로 요구 사항을 정의한다.

```csharp
[Serializable]
public struct ItemRequirement
```

## Constructors

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemRequirement__ctor_System_String_System_Int32_"></a> ItemRequirement\(string, int\)

```csharp
public ItemRequirement(string identifier, int count)
```

#### Parameters

`identifier` string

`count` int

## Fields

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemRequirement_count"></a> count

```csharp
public int count
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemRequirement_identifier"></a> identifier

```csharp
public string identifier
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemRequirement_IsValid"></a> IsValid

```csharp
public bool IsValid { get; }
```

#### Property Value

 bool

