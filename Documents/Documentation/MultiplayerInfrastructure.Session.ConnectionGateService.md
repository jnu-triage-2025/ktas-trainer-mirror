# <a id="MultiplayerInfrastructure_Session_ConnectionGateService"></a> Class ConnectionGateService

Namespace: [MultiplayerInfrastructure.Session](MultiplayerInfrastructure.Session.md)  
Assembly: Assembly\-CSharp.dll  

서버의 외부 접속 허용/불가(커넥션 게이트) 상태를 관리하는 정적 서비스.
직렬화하지 않으며, 런타임 중에만 유효하다.

- 게이트가 닫히면 새로운 외부 접속을 거부하고 LAN 브로드캐스트를 중단한다.
- 게이트가 열리면 "로컬 포트 OOO에 서버가 개방되었습니다." 메시지를
  인게임 채팅과 로그로 발송하고, LAN 브로드캐스트를 재개한다.
- 이미 접속한 클라이언트의 연결은 끊지 않는다.

```csharp
public static class ConnectionGateService
```

#### Inheritance

object ← 
[ConnectionGateService](MultiplayerInfrastructure.Session.ConnectionGateService.md)

## Properties

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_IsOpen"></a> IsOpen

외부 접속 허용 여부. 기본값은 true(허용).

```csharp
public static bool IsOpen { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_Port"></a> Port

현재 서버 포트.

```csharp
public static ushort Port { get; }
```

#### Property Value

 ushort

## Methods

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_Close"></a> Close\(\)

외부 접속을 불가 상태로 전환한다.
열린 상태에서 닫힌 상태로 전환될 때 LAN 브로드캐스트를 중단한다.
이미 닫힌 상태이면 아무 동작도 하지 않는다.
이미 접속한 클라이언트의 연결은 유지된다.

```csharp
public static void Close()
```

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_Open"></a> Open\(\)

외부 접속을 허용 상태로 전환한다.
닫힌 상태에서 열린 상태로 전환될 때 채팅/로그 알림과 LAN 브로드캐스트를 재개한다.
이미 열린 상태이면 아무 동작도 하지 않는다.

```csharp
public static void Open()
```

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_ResetState"></a> ResetState\(\)

세션 종료 시 상태를 초기화한다. 다음 세션에서 깨끗한 상태로 시작하도록 한다.

```csharp
public static void ResetState()
```

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_SetLanBroadcastConfig_System_String_System_Int32_"></a> SetLanBroadcastConfig\(string, int\)

LAN 브로드캐스트 재개에 필요한 설정을 저장한다.
IngameSceneBootstrapper가 LAN 디스커버리를 시작할 때 호출한다.

```csharp
public static void SetLanBroadcastConfig(string sessionName, int gamePort)
```

#### Parameters

`sessionName` string

`gamePort` int

### <a id="MultiplayerInfrastructure_Session_ConnectionGateService_SetPort_System_UInt16_"></a> SetPort\(ushort\)

서버 포트를 설정한다. 부트스트래퍼가 세션 시작 시 호출한다.

```csharp
public static void SetPort(ushort port)
```

#### Parameters

`port` ushort

