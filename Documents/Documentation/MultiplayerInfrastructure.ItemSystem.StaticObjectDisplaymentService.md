# <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplaymentService"></a> Class StaticObjectDisplaymentService

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 의 "표시(적용/설치) 여부"를 서버 권위로 관리하는 정적 서비스입니다.

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 는 네트워크 오브젝트가 아니라 각 프로세스에 로컬로 존재하는
"맵의 일부"이므로, 표시 상태의 진실 원천(source of truth)은 서버에만 존재해야 합니다. 이 서비스가
entityIdentifier 단위로 "표시됨(설치/적용됨)" 집합을 보관하며, 신규 접속자 동기화 시 이 상태를 사용합니다.
(<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemService" data-throw-if-not-resolved="false"></xref> 와 동일한 서버 권위 · static 상태 설계.)
</p>

<p>모든 변이 메서드는 서버에서만 호출되어야 합니다.</p>

```csharp
public static class StaticObjectDisplaymentService
```

#### Inheritance

object ← 
[StaticObjectDisplaymentService](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentService.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplaymentService_ClearAll"></a> ClearAll\(\)

모든 상태를 초기화합니다.

<p>
서버 세션 시작/종료 시 <code>FishNetSupport.ServerManager_OnServerConnectionState</code> 에서 자동 호출됩니다.
static 상태는 Unity 도메인 리로드 없이 프로세스 수명 내내 유지되므로, 호출하지 않으면 이전 세션의
표시 상태가 다음 세션에도 남습니다.
</p>

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplaymentService_ClearShown_System_String_"></a> ClearShown\(string\)

해당 오브젝트의 표시(설치/적용) 상태를 해제합니다(서버 전용, 멱등).

```csharp
public static bool ClearShown(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

#### Returns

 bool

이번 호출로 상태가 새로 변경되었으면 true.

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplaymentService_GetAllShown"></a> GetAllShown\(\)

표시(설치/적용)된 모든 entityIdentifier 를 열거합니다(신규 접속자 동기화용).

```csharp
public static IReadOnlyCollection<string> GetAllShown()
```

#### Returns

 IReadOnlyCollection<string\>

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplaymentService_IsShown_System_String_"></a> IsShown\(string\)

해당 오브젝트가 서버 기준으로 표시(설치/적용)된 상태인지 조회합니다.

```csharp
public static bool IsShown(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_ItemSystem_StaticObjectDisplaymentService_SetShown_System_String_"></a> SetShown\(string\)

해당 오브젝트를 표시(설치/적용)된 상태로 표시합니다(서버 전용, 멱등).

```csharp
public static bool SetShown(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

#### Returns

 bool

이번 호출로 상태가 새로 변경되었으면 true(중복 호출이면 false).

