# <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport"></a> Class FishNetSupport

Namespace: [MultiplayerInfrastructure.FishNetSupports](MultiplayerInfrastructure.FishNetSupports.md)  
Assembly: Assembly\-CSharp.dll  

Facade component for FishNet runtime controls from scene scripts.
This class is split by concerns using partial declarations.

```csharp
[DisallowMultipleComponent]
public class FishNetSupport : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[FishNetSupport](MultiplayerInfrastructure.FishNetSupports.FishNetSupport.md)

## Properties

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_Instance"></a> Instance

```csharp
public static FishNetSupport Instance { get; }
```

#### Property Value

 [FishNetSupport](MultiplayerInfrastructure.FishNetSupports.FishNetSupport.md)

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_IsClientStarted"></a> IsClientStarted

```csharp
public bool IsClientStarted { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_IsServerStarted"></a> IsServerStarted

```csharp
public bool IsServerStarted { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_ConfigureServerBindAddress_System_String_"></a> ConfigureServerBindAddress\(string\)

서버 소켓이 바인딩할 주소를 설정합니다.
데디케이티드 서버는 클라이언트 접속 주소가 아니라 이 값으로 수신 인터페이스를 결정합니다.

```csharp
public void ConfigureServerBindAddress(string bindAddress)
```

#### Parameters

`bindAddress` string

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_ConfigureTransport_MultiplayerInfrastructure_Session_SessionInformationModel_"></a> ConfigureTransport\(SessionInformationModel\)

```csharp
public void ConfigureTransport(SessionInformationModel sessionInformation)
```

#### Parameters

`sessionInformation` [SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_ConnectToExistingServer_MultiplayerInfrastructure_Session_SessionInformationModel_"></a> ConnectToExistingServer\(SessionInformationModel\)

```csharp
public bool ConnectToExistingServer(SessionInformationModel sessionInformation)
```

#### Parameters

`sessionInformation` [SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_PrepareDeferredPlayerSpawning"></a> PrepareDeferredPlayerSpawning\(\)

```csharp
public void PrepareDeferredPlayerSpawning()
```

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_PrepareSystemSceneObserverBinding"></a> PrepareSystemSceneObserverBinding\(\)

Additive 로드된 시스템/백엔드 씬(SystemOverlayScene 등)의 공유 Scene NetworkObject가
모든 클라이언트에게 관측되도록, 접속하는 각 Connection을 해당 씬들에 등록하는 바인더를 준비한다.
이 처리가 없으면 ChatService 등 non-global Scene NetworkObject가 원격 클라이언트에서 스폰되지 않아
Chat/Command Service가 동작하지 않는다.

```csharp
public void PrepareSystemSceneObserverBinding()
```

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_ResolveNetworkManagerInHierarchy"></a> ResolveNetworkManagerInHierarchy\(\)

Tries to resolve a NetworkManager from this object first, then scene hierarchy.

```csharp
public bool ResolveNetworkManagerInHierarchy()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_SetNetworkHudCanvasVisible_System_Boolean_"></a> SetNetworkHudCanvasVisible\(bool\)

```csharp
public void SetNetworkHudCanvasVisible(bool visible)
```

#### Parameters

`visible` bool

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_StartClient"></a> StartClient\(\)

```csharp
public void StartClient()
```

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_StartDedicatedServer_MultiplayerInfrastructure_Session_SessionInformationModel_System_String_"></a> StartDedicatedServer\(SessionInformationModel, string\)

데디케이티드 서버(헤드리스)로 세션을 시작합니다.
로컬 클라이언트를 붙이지 않으며, 지정한 주소로 서버 소켓을 바인딩합니다.

```csharp
public bool StartDedicatedServer(SessionInformationModel sessionInformation, string bindAddress)
```

#### Parameters

`sessionInformation` [SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

`bindAddress` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_StartServer"></a> StartServer\(\)

```csharp
public void StartServer()
```

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_StartSession_MultiplayerInfrastructure_Session_SessionInformationModel_System_Boolean_System_Boolean_"></a> StartSession\(SessionInformationModel, bool, bool\)

Applies launch info and starts host/client according to the mode.
When <code class="paramref">startLocalClient</code> is false the local client is skipped,
which is how a dedicated (headless) server runs.

```csharp
public bool StartSession(SessionInformationModel sessionInformation, bool isOpeningServer, bool startLocalClient = true)
```

#### Parameters

`sessionInformation` [SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

`isOpeningServer` bool

`startLocalClient` bool

#### Returns

 bool

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_StopClient"></a> StopClient\(\)

```csharp
public void StopClient()
```

### <a id="MultiplayerInfrastructure_FishNetSupports_FishNetSupport_StopServer"></a> StopServer\(\)

```csharp
public void StopServer()
```

