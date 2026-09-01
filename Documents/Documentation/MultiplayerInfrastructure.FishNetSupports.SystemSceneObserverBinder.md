# <a id="MultiplayerInfrastructure_FishNetSupports_SystemSceneObserverBinder"></a> Class SystemSceneObserverBinder

Namespace: [MultiplayerInfrastructure.FishNetSupports](MultiplayerInfrastructure.FishNetSupports.md)  
Assembly: Assembly\-CSharp.dll  

IngameScene가 SystemOverlayScene/OverworldScene을 Unity SceneManager로 Additive 로드하기 때문에,
이들 씬은 FishNet의 SceneManager 로드 파이프라인이나 Global Scene 등록을 거치지 않는다.

FishNet은 non-global Scene NetworkObject를, 해당 씬에 연결(Connection)이 등록(AddConnectionToScene)된
클라이언트에게만 Observer로 스폰한다. 그런데 이 프로젝트는 FishNet Scene 관리를 사용하지 않으므로
(Global Scene도 없고, 클라이언트는 start scene으로 빈 브로드캐스트를 보낸다) 원격 클라이언트의 Connection은
SystemOverlayScene에 절대 등록되지 않는다.

그 결과 SystemOverlayScene에 배치된 ChatService(및 다른 백엔드 Scene NetworkObject)는 호스트에서만
스폰되고 원격 클라이언트에서는 스폰/관측되지 않아, ServerRpc/ObserversRpc가 동작하지 않는다
(= Chat/Command Service가 동작하지 않는 증상).

이 컴포넌트는 각 클라이언트가 start scene 로드를 마치는 시점(OnClientLoadedStartScenes)에,
지정된 시스템/백엔드 씬들에 해당 Connection을 등록하여 공유 Scene NetworkObject들이 모든
클라이언트에게 관측되도록 한다. 서버(호스트)에서만 동작한다.

```csharp
[DisallowMultipleComponent]
public sealed class SystemSceneObserverBinder : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[SystemSceneObserverBinder](MultiplayerInfrastructure.FishNetSupports.SystemSceneObserverBinder.md)

## Methods

### <a id="MultiplayerInfrastructure_FishNetSupports_SystemSceneObserverBinder_Configure_FishNet_Managing_NetworkManager_System_String___"></a> Configure\(NetworkManager, string\[\]\)

```csharp
public void Configure(NetworkManager networkManager, string[] systemSceneNames = null)
```

#### Parameters

`networkManager` NetworkManager

`systemSceneNames` string\[\]

