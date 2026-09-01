# <a id="MultiplayerInfrastructure_ItemSystem_ItemObject"></a> Class ItemObject

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

인게임에서 아이템 하나를 나타내는 MonoBehaviour 컴포넌트입니다.

■ 런타임 계층 구조
  ItemObject  ← 이 컴포넌트를 보유. 코드에 의해 즉석 생성됨
  └ ItemGroundedModel  ← Resources/Models/Items/{identifier} 에서 로드된 3D 모델

■ 사용 방법
  var obj = ItemObject.Spawn(myStoneItem, transform.position);

```csharp
public class ItemObject : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[ItemObject](MultiplayerInfrastructure.ItemSystem.ItemObject.md)

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_AuthoritativePosition"></a> AuthoritativePosition

```csharp
public Vector3 AuthoritativePosition { get; }
```

#### Property Value

 Vector3

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_AuthoritativeRotation"></a> AuthoritativeRotation

```csharp
public Quaternion AuthoritativeRotation { get; }
```

#### Property Value

 Quaternion

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_GroundedModel"></a> GroundedModel

로드된 3D 모델 자식 오브젝트입니다. 모델이 없으면 null 입니다.

```csharp
public GameObject GroundedModel { get; }
```

#### Property Value

 GameObject

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_Identifier"></a> Identifier

서버가 부여한 전역 엔티티 식별자입니다.
null 이면 엔티티 저장소에 등록되지 않습니다.

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_IsGrounded"></a> IsGrounded

서버 권위 물리 동기화에 사용하는 현재 상태입니다.

```csharp
public bool IsGrounded { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_Item"></a> Item

이 ItemObject가 보유한 Item 인스턴스입니다.

```csharp
public Item Item { get; }
```

#### Property Value

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_ApplyAuthoritativeState_UnityEngine_Vector3_UnityEngine_Quaternion_System_Boolean_"></a> ApplyAuthoritativeState\(Vector3, Quaternion, bool\)

서버에서 받은 물리/부유 상태를 클라이언트 표현에 적용합니다.

```csharp
public void ApplyAuthoritativeState(Vector3 position, Quaternion rotation, bool grounded)
```

#### Parameters

`position` Vector3

`rotation` Quaternion

`grounded` bool

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_Spawn_MultiplayerInfrastructure_ItemSystem_Item_UnityEngine_Vector3_System_Nullable_UnityEngine_Vector3__System_String_"></a> Spawn\(Item, Vector3, Vector3?, string\)

지정 위치에 Item을 나타내는 ItemObject를 즉석으로 생성합니다.
Collider는 자동으로 추가되며, 이동은 Rigidbody 없이 transform으로 처리됩니다.

```csharp
public static ItemObject Spawn(Item item, Vector3 position, Vector3? throwForce = null, string entityIdentifier = null)
```

#### Parameters

`item` [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

데이터 소스. null 불가.

`position` Vector3

월드 스폰 위치.

`throwForce` Vector3?

기존 호출부 호환을 위해 유지되는 인자입니다. 현재 월드 아이템은 물리 impulse를 사용하지 않습니다.

`entityIdentifier` string

#### Returns

 [ItemObject](MultiplayerInfrastructure.ItemSystem.ItemObject.md)

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_TriggerAttackAnimation"></a> TriggerAttackAnimation\(\)

공격 시 아이템을 앞으로 짧게 밀었다가 되돌리는 애니메이션입니다.

```csharp
public void TriggerAttackAnimation()
```

### <a id="MultiplayerInfrastructure_ItemSystem_ItemObject_TriggerUseAnimation"></a> TriggerUseAnimation\(\)

사용 시 아이템을 위-아래로 짧게 튀기는 애니메이션입니다.

```csharp
public void TriggerUseAnimation()
```

