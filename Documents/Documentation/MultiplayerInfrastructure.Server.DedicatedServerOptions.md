# <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions"></a> Class DedicatedServerOptions

Namespace: [MultiplayerInfrastructure.Server](MultiplayerInfrastructure.Server.md)  
Assembly: Assembly\-CSharp.dll  

데디케이티드 서버(헤드리스) 실행 옵션입니다.
커맨드라인 인자를 해석하여 서버 세션을 시작하는 데 필요한 값을 담습니다.
이 타입은 Unity API에 의존하지 않으므로 EditMode 테스트에서 그대로 검증할 수 있습니다.

```csharp
public sealed class DedicatedServerOptions
```

#### Inheritance

object ← 
[DedicatedServerOptions](MultiplayerInfrastructure.Server.DedicatedServerOptions.md)

## Fields

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DedicatedServerSwitch"></a> DedicatedServerSwitch

플레이어 빌드에서도 데디케이티드 모드를 강제하는 스위치입니다.

```csharp
public const string DedicatedServerSwitch = "dedicatedserver"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DedicatedServerSwitchAlias"></a> DedicatedServerSwitchAlias

<xref href="MultiplayerInfrastructure.Server.DedicatedServerOptions.DedicatedServerSwitch" data-throw-if-not-resolved="false"></xref>의 짧은 별칭입니다.

```csharp
public const string DedicatedServerSwitchAlias = "server"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DefaultBindAddress"></a> DefaultBindAddress

```csharp
public const string DefaultBindAddress = "0.0.0.0"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DefaultSessionName"></a> DefaultSessionName

```csharp
public const string DefaultSessionName = "KTAS Dedicated Server"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DefaultTargetFrameRate"></a> DefaultTargetFrameRate

```csharp
public const int DefaultTargetFrameRate = 60
```

#### Field Value

 int

## Properties

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_BindAddress"></a> BindAddress

서버 소켓이 바인딩할 주소입니다. 기본값은 모든 인터페이스입니다.

```csharp
public string BindAddress { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DatapackIds"></a> DatapackIds

세션에서 사용할 데이터팩 식별자 목록입니다.

```csharp
public IReadOnlyList<string> DatapackIds { get; }
```

#### Property Value

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_DatapacksPath"></a> DatapacksPath

런타임 데이터팩 폴더의 경로입니다.
값이 없으면 실행 파일과 같은 위치의 <code>DataPacks</code> 폴더를 사용합니다.

```csharp
public string DatapacksPath { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_IsExplicitlyRequested"></a> IsExplicitlyRequested

커맨드라인에 데디케이티드 서버 스위치가 명시되었는지 여부입니다.

```csharp
public bool IsExplicitlyRequested { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_Port"></a> Port

서버가 수신할 포트입니다.

```csharp
public ushort Port { get; }
```

#### Property Value

 ushort

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_SessionName"></a> SessionName

LAN 목록과 로그에 표시할 세션 이름입니다.

```csharp
public string SessionName { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_StartScene"></a> StartScene

서버가 진입할 시작 씬 이름입니다.

```csharp
public string StartScene { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_TargetFrameRate"></a> TargetFrameRate

서버 루프의 목표 프레임 레이트입니다. 0 이하이면 제한하지 않습니다.

```csharp
public int TargetFrameRate { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_UseLanDiscovery"></a> UseLanDiscovery

LAN 검색 브로드캐스트를 사용할지 여부입니다.

```csharp
public bool UseLanDiscovery { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_Warnings"></a> Warnings

해석하지 못한 값에 대한 경고 메시지입니다.

```csharp
public IReadOnlyList<string> Warnings { get; }
```

#### Property Value

 IReadOnlyList<string\>

## Methods

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_Parse_System_Collections_Generic_IReadOnlyList_System_String__MultiplayerInfrastructure_Session_SessionConfiguration_"></a> Parse\(IReadOnlyList<string\>, SessionConfiguration\)

커맨드라인 인자를 해석합니다.
<code class="paramref">defaults</code>가 주어지면 인자로 지정되지 않은 값의 기본값으로 사용합니다.

```csharp
public static DedicatedServerOptions Parse(IReadOnlyList<string> args, SessionConfiguration defaults = null)
```

#### Parameters

`args` IReadOnlyList<string\>

`defaults` [SessionConfiguration](MultiplayerInfrastructure.Session.SessionConfiguration.md)

#### Returns

 [DedicatedServerOptions](MultiplayerInfrastructure.Server.DedicatedServerOptions.md)

### <a id="MultiplayerInfrastructure_Server_DedicatedServerOptions_ToString"></a> ToString\(\)

로그에 남길 요약 문자열을 만듭니다.

```csharp
public override string ToString()
```

#### Returns

 string

