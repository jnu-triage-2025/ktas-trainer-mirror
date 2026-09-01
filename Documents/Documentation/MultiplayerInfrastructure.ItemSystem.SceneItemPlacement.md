# <a id="MultiplayerInfrastructure_ItemSystem_SceneItemPlacement"></a> Class SceneItemPlacement

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

에디터에서 아이템을 씬에 컴파일 타임으로 배치할 때 사용하는 컴포넌트입니다.

■ 동작
  Start() 시점에 Registry에서 _itemIdentifier 에 해당하는 Item 인스턴스를 생성하고,
  자신의 위치에 ItemObject.Spawn 을 호출한 뒤 이 GameObject를 제거합니다.

■ 실행 순서
  TTRegistryPreloader.Awake() → (모든 Awake 완료) → SceneItemPlacement.Start()
  Start() 를 사용하므로 레지스트리 등록이 완료된 이후에 안전하게 아이템을 생성합니다.

■ 에디터 사용법
  1. 아이템을 놓을 위치에 빈 GameObject를 생성합니다.
  2. 이 컴포넌트를 추가합니다.
  3. 인스펙터의 드롭다운에서 아이템 종류를 선택합니다.
  4. 필요하면 Stack Count 를 조정합니다.

■ 주의
  _itemIdentifier 가 비어 있거나 Registry에 등록되지 않은 값이면 아무것도 스폰되지 않습니다.

```csharp
public class SceneItemPlacement : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[SceneItemPlacement](MultiplayerInfrastructure.ItemSystem.SceneItemPlacement.md)

## Fields

### <a id="MultiplayerInfrastructure_ItemSystem_SceneItemPlacement_DefaultSceneViewDisplayRange"></a> DefaultSceneViewDisplayRange

```csharp
public const float DefaultSceneViewDisplayRange = 25
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_ItemSystem_SceneItemPlacement_SceneViewDisplayRangePrefKey"></a> SceneViewDisplayRangePrefKey

```csharp
public const string SceneViewDisplayRangePrefKey = "MultiplayerInfrastructure.SceneItemPlacement.SceneViewDisplayRange"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_SceneItemPlacement_SceneViewVisibilityPrefKey"></a> SceneViewVisibilityPrefKey

```csharp
public const string SceneViewVisibilityPrefKey = "MultiplayerInfrastructure.SceneItemPlacement.SceneViewVisible"
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_ItemSystem_SceneItemPlacement_ItemIdentifier"></a> ItemIdentifier

인스펙터에서 설정한 아이템 식별자입니다.

```csharp
public string ItemIdentifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_ItemSystem_SceneItemPlacement_StackCount"></a> StackCount

```csharp
public int StackCount { get; }
```

#### Property Value

 int

