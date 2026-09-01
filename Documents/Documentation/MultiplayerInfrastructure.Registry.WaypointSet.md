# <a id="MultiplayerInfrastructure_Registry_WaypointSet"></a> Class WaypointSet

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

순서가 있는 waypoint 묶음이다. <xref href="MultiplayerInfrastructure.Registry.WaypointSet._waypoints" data-throw-if-not-resolved="false"></xref> 목록의 인덱스가 이동
순서이며, 각 waypoint는 독립된 waypoint로도 계속 레지스트리에 등록된다.

```csharp
[DisallowMultipleComponent]
public sealed class WaypointSet : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[WaypointSet](MultiplayerInfrastructure.Registry.WaypointSet.md)

## Properties

### <a id="MultiplayerInfrastructure_Registry_WaypointSet_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Registry_WaypointSet_Waypoints"></a> Waypoints

```csharp
public IReadOnlyList<WaypointAnchor> Waypoints { get; }
```

#### Property Value

 IReadOnlyList<[WaypointAnchor](MultiplayerInfrastructure.Registry.WaypointAnchor.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Registry_WaypointSet_ConfigureIdentifier_System_String_"></a> ConfigureIdentifier\(string\)

```csharp
public void ConfigureIdentifier(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Registry_WaypointSet_ConfigureWaypoints_System_Collections_Generic_IEnumerable_MultiplayerInfrastructure_Registry_WaypointAnchor__"></a> ConfigureWaypoints\(IEnumerable<WaypointAnchor\>\)

이동 순서대로 waypoint 참조를 구성한다.

```csharp
public void ConfigureWaypoints(IEnumerable<WaypointAnchor> waypoints)
```

#### Parameters

`waypoints` IEnumerable<[WaypointAnchor](MultiplayerInfrastructure.Registry.WaypointAnchor.md)\>

### <a id="MultiplayerInfrastructure_Registry_WaypointSet_TryGet_System_String_MultiplayerInfrastructure_Registry_WaypointSet__"></a> TryGet\(string, out WaypointSet\)

```csharp
public static bool TryGet(string identifier, out WaypointSet waypointSet)
```

#### Parameters

`identifier` string

`waypointSet` [WaypointSet](MultiplayerInfrastructure.Registry.WaypointSet.md)

#### Returns

 bool

