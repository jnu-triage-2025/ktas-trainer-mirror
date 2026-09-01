# <a id="MultiplayerInfrastructure_Registry_WaypointAnchor"></a> Class WaypointAnchor

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class WaypointAnchor : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[WaypointAnchor](MultiplayerInfrastructure.Registry.WaypointAnchor.md)

## Properties

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_Identifier"></a> Identifier

```csharp
public string Identifier { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_IsQuestMarkerVisible"></a> IsQuestMarkerVisible

```csharp
public bool IsQuestMarkerVisible { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_SupportsHighlight"></a> SupportsHighlight

```csharp
public bool SupportsHighlight { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_ConfigureIdentifier_System_String_"></a> ConfigureIdentifier\(string\)

```csharp
public void ConfigureIdentifier(string value)
```

#### Parameters

`value` string

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_Highlight"></a> Highlight\(\)

```csharp
public void Highlight()
```

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_SetQuestMarkerVisible_System_Boolean_"></a> SetQuestMarkerVisible\(bool\)

이 waypoint가 현재 퀘스트의 이동 목표일 때 quest-marker 아이콘을 표시한다.
NPC 머리 위 마크와 같은 오버헤드 라벨 경로를 쓰므로 크기가 동일하고 지형에 가려지지 않는다.

```csharp
public void SetQuestMarkerVisible(bool visible)
```

#### Parameters

`visible` bool

### <a id="MultiplayerInfrastructure_Registry_WaypointAnchor_TryGet_System_String_MultiplayerInfrastructure_Registry_WaypointAnchor__"></a> TryGet\(string, out WaypointAnchor\)

```csharp
public static bool TryGet(string identifier, out WaypointAnchor anchor)
```

#### Parameters

`identifier` string

`anchor` [WaypointAnchor](MultiplayerInfrastructure.Registry.WaypointAnchor.md)

#### Returns

 bool

