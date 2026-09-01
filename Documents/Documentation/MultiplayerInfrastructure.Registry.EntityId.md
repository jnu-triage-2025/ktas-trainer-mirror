# <a id="MultiplayerInfrastructure_Registry_EntityId"></a> Class EntityId

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

씬 고정 엔티티에 사용할 안정적인 식별자를 생성/보정하는 유틸리티입니다.

- 비어 있으면 현재 GameObject 이름 기반 접두어 + GUID를 생성
- 이미 값이 있으면 그대로 유지

```csharp
public static class EntityId
```

#### Inheritance

object ← 
[EntityId](MultiplayerInfrastructure.Registry.EntityId.md)

## Methods

### <a id="MultiplayerInfrastructure_Registry_EntityId_Ensure_System_String_UnityEngine_GameObject_System_String_"></a> Ensure\(string, GameObject, string\)

```csharp
public static string Ensure(string currentIdentifier, GameObject target, string prefix)
```

#### Parameters

`currentIdentifier` string

`target` GameObject

`prefix` string

#### Returns

 string

