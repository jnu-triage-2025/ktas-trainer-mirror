# <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService"></a> Class PlayerQuestStateFlagService

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

플레이어별 퀘스트 상태 플래그 풀(Quest State Flag Pool)을 관리하는 정적 서비스.

<p>
풀은 플레이어 한 명당 문자열 집합 하나다. 퀘스트 진행이 "지금 이 플레이어에게 무엇이 요구되는가"를
표현해야 할 때 임의로 정한 식별자를 이 집합에 넣고, 그것을 소비하는 쪽(상호작용 노출 판정 등)이
보유 여부를 묻는다. 값은 PlayerQuestStateFlag 레지스트리에 UserDescriptor.Identifier(UUID)를
키로 저장한다.
</p>

<p>
역할 태그(<xref href="MultiplayerInfrastructure.Tag.PlayerTagService" data-throw-if-not-resolved="false"></xref>)와 저장 구조는 같지만 쓰임이 다르다. 역할 태그는 세션 내내
유지되는 배역이고, 이 풀은 퀘스트 단계마다 켜졌다 꺼지는 진행 상태다. 두 저장소를 나눠 두어야
병렬 브랜치 배정(requiredPlayerTags)이 퀘스트 상태 문자열에 영향을 받지 않는다.
</p>

<p>
변경은 서버 권위다. 서버가 값을 바꾸면 소유 플레이어의 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref>가 전체
옵저버에게 스냅샷을 복제하므로, 각 피어는 모든 플레이어의 플래그를 읽을 수 있다. 네트워크가
꺼진 오프라인 컨텍스트에서는 로컬 저장소만 갱신한다.
</p>

```csharp
public static class PlayerQuestStateFlagService
```

#### Inheritance

object ← 
[PlayerQuestStateFlagService](MultiplayerInfrastructure.Quest.PlayerQuestStateFlagService.md)

## Properties

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_CanMutate"></a> CanMutate

서버이거나 네트워크가 아예 꺼진 컨텍스트에서만 값을 바꿀 수 있다.
도구가 조작 가능 여부를 미리 알 수 있도록 공개한다.

```csharp
public static bool CanMutate { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_KnownFlags"></a> KnownFlags

등록된 플래그 어휘를 사전순으로 반환한다.

```csharp
public static IReadOnlyCollection<string> KnownFlags { get; }
```

#### Property Value

 IReadOnlyCollection<string\>

## Methods

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_ClearAll"></a> ClearAll\(\)

접속한 모든 플레이어의 플래그를 비운다. 시나리오가 시작·종료될 때 이전 회차의 상태가
남지 않게 한다.

<p>
전체 비우기는 서버에서만 수행하고 복제로 전파한다. 클라이언트가 자기 판단으로 비우면,
뒤늦게 접속해 버퍼된 스냅샷을 받은 피어가 그 스냅샷을 지워 버릴 수 있다.
</p>

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_ClearFlags_System_String_"></a> ClearFlags\(string\)

플레이어 한 명의 플래그 저장소를 비운다. 접속 종료/디스폰 정리 용도.

```csharp
public static void ClearFlags(string identifier)
```

#### Parameters

`identifier` string

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_ClearKnownFlags"></a> ClearKnownFlags\(\)

등록된 플래그 어휘를 비운다. 콘텐츠가 끝날 때 호출한다.

```csharp
public static void ClearKnownFlags()
```

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_GetFlags_System_String_"></a> GetFlags\(string\)

플레이어의 플래그 집합을 읽기 전용으로 반환한다. 없으면 빈 집합.

```csharp
public static IReadOnlyCollection<string> GetFlags(string identifier)
```

#### Parameters

`identifier` string

#### Returns

 IReadOnlyCollection<string\>

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_Has_System_String_System_String_"></a> Has\(string, string\)

플레이어가 플래그를 보유하고 있는지 확인한다. 읽기는 모든 피어에서 허용된다.

```csharp
public static bool Has(string identifier, string flag)
```

#### Parameters

`identifier` string

`flag` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_RegisterKnownFlags_System_Collections_Generic_IEnumerable_System_String__"></a> RegisterKnownFlags\(IEnumerable<string\>\)

콘텐츠가 쓰는 플래그 어휘를 등록한다. 풀에 값이 없어도 도구가 고를 수 있게 하려는 목적이며,
플래그 판정에는 관여하지 않는다. 풀은 여기 없는 문자열도 그대로 담는다.

```csharp
public static void RegisterKnownFlags(IEnumerable<string> flags)
```

#### Parameters

`flags` IEnumerable<string\>

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_ReplaceFlags_System_String_System_Collections_Generic_IReadOnlyList_System_String__"></a> ReplaceFlags\(string, IReadOnlyList<string\>\)

네트워크로 동기화된 스냅샷으로 로컬 집합을 교체한다. 서버/클라이언트 공용이며,
권위 검사를 거치지 않으므로 복제 경로에서만 호출한다.

```csharp
public static void ReplaceFlags(string identifier, IReadOnlyList<string> flags)
```

#### Parameters

`identifier` string

`flags` IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_Set_System_String_System_String_"></a> Set\(string, string\)

플래그 하나를 플레이어의 풀에 넣는다. 이미 있으면 아무 일도 하지 않는다.

```csharp
public static bool Set(string identifier, string flag)
```

#### Parameters

`identifier` string

`flag` string

#### Returns

 bool

실제로 추가되었으면 true.

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_SetForAll_System_String_System_Boolean_"></a> SetForAll\(string, bool\)

접속한 모든 플레이어에게 플래그를 적용하거나 해제한다.

```csharp
public static int SetForAll(string flag, bool value = true)
```

#### Parameters

`flag` string

`value` bool

#### Returns

 int

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_SetForAnyTag_System_Collections_Generic_IReadOnlyList_System_String__System_String_System_Boolean_"></a> SetForAnyTag\(IReadOnlyList<string\>, string, bool\)

지정한 역할 태그 중 하나라도 보유한 플레이어에게 플래그를 적용하거나 해제한다.

```csharp
public static int SetForAnyTag(IReadOnlyList<string> tags, string flag, bool value = true)
```

#### Parameters

`tags` IReadOnlyList<string\>

`flag` string

`value` bool

#### Returns

 int

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_SetForTag_System_String_System_String_System_Boolean_"></a> SetForTag\(string, string, bool\)

지정한 역할 태그를 보유한 모든 플레이어에게 플래그를 적용하거나 해제한다.

```csharp
public static int SetForTag(string tag, string flag, bool value = true)
```

#### Parameters

`tag` string

`flag` string

`value` bool

#### Returns

 int

실제로 상태가 바뀐 플레이어 수.

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_Unset_System_String_System_String_"></a> Unset\(string, string\)

플래그 하나를 플레이어의 풀에서 뺀다.

```csharp
public static bool Unset(string identifier, string flag)
```

#### Parameters

`identifier` string

`flag` string

#### Returns

 bool

실제로 제거되었으면 true.

### <a id="MultiplayerInfrastructure_Quest_PlayerQuestStateFlagService_FlagsChanged"></a> FlagsChanged

플래그 집합이 바뀐 플레이어의 UserDescriptor.Identifier 를 전달한다.

```csharp
public static event Action<string> FlagsChanged
```

#### Event Type

 Action<string\>

