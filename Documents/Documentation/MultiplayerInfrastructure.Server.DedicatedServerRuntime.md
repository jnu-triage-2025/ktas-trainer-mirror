# <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime"></a> Class DedicatedServerRuntime

Namespace: [MultiplayerInfrastructure.Server](MultiplayerInfrastructure.Server.md)  
Assembly: Assembly\-CSharp.dll  

데디케이티드 서버(헤드리스) 실행을 부트스트랩합니다.

Dedicated Server 서브타겟으로 빌드하면 <code>UNITY_SERVER</code>가 정의되어 자동으로 활성화되며,
일반 플레이어 빌드에서도 <code>-dedicatedServer</code>(또는 <code>-server</code>) 인자를 주면 활성화됩니다.
활성화되면 IntroScene의 UI 흐름을 건너뛰고, 커맨드라인 옵션으로 구성한 세션 정보를
런타임 레지스트리에 등록한 뒤 시작 씬(<xref href="MultiplayerInfrastructure.Server.DedicatedServerOptions.StartScene" data-throw-if-not-resolved="false"></xref>)으로 진입합니다.
이후 세션 시작은 IngameSceneBootstrapper가 담당합니다.

```csharp
public static class DedicatedServerRuntime
```

#### Inheritance

object ← 
[DedicatedServerRuntime](MultiplayerInfrastructure.Server.DedicatedServerRuntime.md)

## Fields

### <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime_DefaultDatapackFolderName"></a> DefaultDatapackFolderName

실행 파일과 같은 위치에 만드는 기본 데이터팩 폴더의 이름입니다.

```csharp
public const string DefaultDatapackFolderName = "DataPacks"
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime_IsActive"></a> IsActive

현재 실행이 데디케이티드 서버 모드인지 여부입니다.

```csharp
public static bool IsActive { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime_Options"></a> Options

데디케이티드 서버 모드에서 사용 중인 옵션입니다. 비활성 상태에서는 <code>null</code>입니다.

```csharp
public static DedicatedServerOptions Options { get; }
```

#### Property Value

 [DedicatedServerOptions](MultiplayerInfrastructure.Server.DedicatedServerOptions.md)

## Methods

### <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime_CreateSessionInformation_MultiplayerInfrastructure_Server_DedicatedServerOptions_"></a> CreateSessionInformation\(DedicatedServerOptions\)

커맨드라인 옵션으로 세션 정보를 구성합니다.

```csharp
public static SessionInformationModel CreateSessionInformation(DedicatedServerOptions options)
```

#### Parameters

`options` [DedicatedServerOptions](MultiplayerInfrastructure.Server.DedicatedServerOptions.md)

#### Returns

 [SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

### <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime_DisableFishNetHeadlessAutoStart"></a> DisableFishNetHeadlessAutoStart\(\)

FishNet NetworkManager의 헤드리스 자동 서버 개방을 비활성화합니다.
씬이 로드될 때마다 호출되며, NetworkManager의 Awake가 끝난 뒤이자
Start가 실행되기 전에 적용됩니다.

```csharp
public static void DisableFishNetHeadlessAutoStart()
```

### <a id="MultiplayerInfrastructure_Server_DedicatedServerRuntime_ShouldActivate_System_Boolean_System_Boolean_"></a> ShouldActivate\(bool, bool\)

서버 빌드 여부와 커맨드라인 스위치를 근거로 데디케이티드 모드 활성화를 판단합니다.

```csharp
public static bool ShouldActivate(bool isServerBuild, bool explicitlyRequested)
```

#### Parameters

`isServerBuild` bool

`explicitlyRequested` bool

#### Returns

 bool

