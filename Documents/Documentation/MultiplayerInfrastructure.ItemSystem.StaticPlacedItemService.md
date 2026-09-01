# <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService"></a> Class StaticPlacedItemService

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 의 남은 획득 가능 횟수(Remains)를 서버 권위로 관리하는 정적 서비스입니다.

<p>
StaticPlacedItem 은 네트워크 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이므로,
상태의 진실 원천(source of truth)은 서버에만 존재해야 합니다. 이 서비스가 그 역할을 합니다.
</p>

<p>모든 변이 메서드는 서버에서만 호출되어야 합니다(PlayerTagService 와 동일한 규약).</p>

키 구성:
- 전역(Global) 모드: entityIdentifier 하나당 Remains 하나.
- 로컬(Local) 모드: (entityIdentifier, userIdentifier) 조합당 Remains 하나.

```csharp
public static class StaticPlacedItemService
```

#### Inheritance

object ← 
[StaticPlacedItemService](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemService.md)

## Methods

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_ClearAll"></a> ClearAll\(\)

모든 상태를 초기화합니다.

<p>
서버 세션 시작(<xref href="FishNet.Transporting.LocalConnectionState.Started" data-throw-if-not-resolved="false"></xref>) 및
종료(<xref href="FishNet.Transporting.LocalConnectionState.Stopped" data-throw-if-not-resolved="false"></xref>) 시
<code>FishNetSupport.ServerManager_OnServerConnectionState</code>에서 자동 호출됩니다.
</p>

<p>
static Dictionary는 Unity 도메인 리로드 없이 프로세스 수명 내내 유지되므로,
이 메서드를 호출하지 않으면 이전 세션의 획득 상태가 다음 세션에도 남습니다.
</p>

```csharp
public static void ClearAll()
```

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_ClearEntity_System_String_"></a> ClearEntity\(string\)

특정 엔티티의 모든 상태(전역 + 모든 유저)를 제거합니다(서버 전용).

```csharp
public static void ClearEntity(string entityIdentifier)
```

#### Parameters

`entityIdentifier` string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_ClearUser_System_String_"></a> ClearUser\(string\)

특정 유저의 모든 Local 상태를 제거합니다(서버 전용).
기본적으로 플레이어가 접속을 종료하면 호출하여 재접속 시 상태를 초기화합니다.

```csharp
public static void ClearUser(string userIdentifier)
```

#### Parameters

`userIdentifier` string

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_DecreaseGlobalRemains_System_String_System_Int32_System_Int32_"></a> DecreaseGlobalRemains\(string, int, int\)

전역 Remains 를 <code class="paramref">delta</code> 만큼 감소시키고, 감소 후 값을 반환합니다(서버 전용).

```csharp
public static int DecreaseGlobalRemains(string entityIdentifier, int delta, int fallbackInitial)
```

#### Parameters

`entityIdentifier` string

`delta` int

`fallbackInitial` int

#### Returns

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_DecreaseLocalRemains_System_String_System_String_System_Int32_System_Int32_"></a> DecreaseLocalRemains\(string, string, int, int\)

특정 유저의 Local Remains 를 <code class="paramref">delta</code> 만큼 감소시키고, 감소 후 값을 반환합니다(서버 전용).

```csharp
public static int DecreaseLocalRemains(string entityIdentifier, string userIdentifier, int delta, int fallbackInitial)
```

#### Parameters

`entityIdentifier` string

`userIdentifier` string

`delta` int

`fallbackInitial` int

#### Returns

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_EnsureGlobalRemains_System_String_System_Int32_"></a> EnsureGlobalRemains\(string, int\)

전역 Remains 값을 아직 초기화하지 않았다면 <code class="paramref">initialRemains</code> 로 초기화합니다(서버 전용).
이미 값이 있으면 유지합니다(멱등).

```csharp
public static void EnsureGlobalRemains(string entityIdentifier, int initialRemains)
```

#### Parameters

`entityIdentifier` string

`initialRemains` int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_EnsureLocalRemains_System_String_System_String_System_Int32_"></a> EnsureLocalRemains\(string, string, int\)

특정 유저의 Local Remains 를 아직 초기화하지 않았다면 초기화합니다(서버 전용, 멱등).

```csharp
public static void EnsureLocalRemains(string entityIdentifier, string userIdentifier, int initialRemains)
```

#### Parameters

`entityIdentifier` string

`userIdentifier` string

`initialRemains` int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_GetGlobalRemains_System_String_System_Int32_"></a> GetGlobalRemains\(string, int\)

전역 Remains 를 조회합니다. 초기화되지 않았다면 <code class="paramref">fallback</code> 을 반환합니다.

```csharp
public static int GetGlobalRemains(string entityIdentifier, int fallback)
```

#### Parameters

`entityIdentifier` string

`fallback` int

#### Returns

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_GetLocalRemains_System_String_System_String_System_Int32_"></a> GetLocalRemains\(string, string, int\)

특정 유저의 Local Remains 를 조회합니다. 없으면 <code class="paramref">fallback</code> 을 반환합니다.

```csharp
public static int GetLocalRemains(string entityIdentifier, string userIdentifier, int fallback)
```

#### Parameters

`entityIdentifier` string

`userIdentifier` string

`fallback` int

#### Returns

 int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_RestoreGlobalRemains_System_String_System_Int32_"></a> RestoreGlobalRemains\(string, int\)

선점 감소했던 전역 Remains 를 <code class="paramref">delta</code> 만큼 되돌립니다(서버 전용).
픽업 확정 실패/중단 시 예약 복원에 사용됩니다.

```csharp
public static void RestoreGlobalRemains(string entityIdentifier, int delta)
```

#### Parameters

`entityIdentifier` string

`delta` int

### <a id="MultiplayerInfrastructure_ItemSystem_StaticPlacedItemService_RestoreLocalRemains_System_String_System_String_System_Int32_"></a> RestoreLocalRemains\(string, string, int\)

선점 감소했던 특정 유저의 Local Remains 를 <code class="paramref">delta</code> 만큼 되돌립니다(서버 전용).
픽업 확정 실패/중단 시 예약 복원에 사용됩니다.

```csharp
public static void RestoreLocalRemains(string entityIdentifier, string userIdentifier, int delta)
```

#### Parameters

`entityIdentifier` string

`userIdentifier` string

`delta` int

