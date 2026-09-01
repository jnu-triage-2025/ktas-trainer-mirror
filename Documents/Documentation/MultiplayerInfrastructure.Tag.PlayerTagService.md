# <a id="MultiplayerInfrastructure_Tag_PlayerTagService"></a> Class PlayerTagService

Namespace: [MultiplayerInfrastructure.Tag](MultiplayerInfrastructure.Tag.md)  
Assembly: Assembly\-CSharp.dll  

플레이어 태그를 관리하는 정적 서비스.
태그는 PlayerTag 레지스트리에 UserDescriptor.Identifier(UUID)를 키로 저장됩니다.
모든 메서드는 서버 측에서 호출되어야 합니다.

```csharp
public static class PlayerTagService
```

#### Inheritance

object ← 
[PlayerTagService](MultiplayerInfrastructure.Tag.PlayerTagService.md)

## Methods

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_AddTag_System_String_System_String_"></a> AddTag\(string, string\)

플레이어에게 태그를 추가합니다. 이미 존재하는 태그는 무시합니다.

```csharp
public static void AddTag(string uuid, string tag)
```

#### Parameters

`uuid` string

`tag` string

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_AddTagToIdentifier_System_String_System_String_"></a> AddTagToIdentifier\(string, string\)

```csharp
public static void AddTagToIdentifier(string identifier, string tag)
```

#### Parameters

`identifier` string

`tag` string

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_ChangeTag_System_String_System_String_System_String_"></a> ChangeTag\(string, string, string\)

플레이어의 'fromTag'를 'toTag'로 교체합니다.

```csharp
public static bool ChangeTag(string uuid, string fromTag, string toTag)
```

#### Parameters

`uuid` string

`fromTag` string

`toTag` string

#### Returns

 bool

교체에 성공했으면 true, fromTag가 없었으면 false.

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_ChangeTagForIdentifier_System_String_System_String_System_String_"></a> ChangeTagForIdentifier\(string, string, string\)

```csharp
public static bool ChangeTagForIdentifier(string identifier, string fromTag, string toTag)
```

#### Parameters

`identifier` string

`fromTag` string

`toTag` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_ClearTags_System_String_"></a> ClearTags\(string\)

플레이어 태그 저장소를 완전히 제거합니다.
주로 연결 종료/디스폰 시 정리 용도로 사용합니다.

```csharp
public static void ClearTags(string uuid)
```

#### Parameters

`uuid` string

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_GetTags_System_String_"></a> GetTags\(string\)

플레이어의 태그 목록을 읽기 전용으로 반환합니다. 없으면 빈 리스트.

```csharp
public static IReadOnlyList<string> GetTags(string uuid)
```

#### Parameters

`uuid` string

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_GetTagsByIdentifier_System_String_"></a> GetTagsByIdentifier\(string\)

```csharp
public static IReadOnlyList<string> GetTagsByIdentifier(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_HasTag_System_String_System_String_"></a> HasTag\(string, string\)

플레이어가 특정 태그를 보유하고 있는지 확인합니다.

```csharp
public static bool HasTag(string uuid, string tag)
```

#### Parameters

`uuid` string

`tag` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_HasTagOnIdentifier_System_String_System_String_"></a> HasTagOnIdentifier\(string, string\)

```csharp
public static bool HasTagOnIdentifier(string identifier, string tag)
```

#### Parameters

`identifier` string

`tag` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_RemoveTag_System_String_System_String_"></a> RemoveTag\(string, string\)

플레이어에게서 태그를 제거합니다.

```csharp
public static bool RemoveTag(string uuid, string tag)
```

#### Parameters

`uuid` string

`tag` string

#### Returns

 bool

태그가 실제로 제거되었으면 true, 없었으면 false.

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_RemoveTagFromIdentifier_System_String_System_String_"></a> RemoveTagFromIdentifier\(string, string\)

```csharp
public static bool RemoveTagFromIdentifier(string identifier, string tag)
```

#### Parameters

`identifier` string

`tag` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Tag_PlayerTagService_ReplaceTags_System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> ReplaceTags\(string, IReadOnlyList<string\>\)

네트워크 동기화된 태그 스냅샷으로 로컬 태그 목록을 교체합니다.
서버/클라이언트 공용으로 사용됩니다.

```csharp
public static void ReplaceTags(string uuid, IReadOnlyList<string> tags)
```

#### Parameters

`uuid` string

`tags` IReadOnlyList<string\>

