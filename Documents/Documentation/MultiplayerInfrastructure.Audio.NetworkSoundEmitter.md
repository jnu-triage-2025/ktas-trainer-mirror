# <a id="MultiplayerInfrastructure_Audio_NetworkSoundEmitter"></a> Class NetworkSoundEmitter

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

월드 오브젝트에 붙여 효과음을 자신의 현재 위치에서 서버 전역 재생하도록 하는 진입점입니다.
애니메이션 이벤트, 상호작용 코드, UnityEvent에서 <xref href="MultiplayerInfrastructure.Audio.NetworkSoundEmitter.Play" data-throw-if-not-resolved="false"></xref>를 호출할 수 있습니다.

```csharp
public sealed class NetworkSoundEmitter : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[NetworkSoundEmitter](MultiplayerInfrastructure.Audio.NetworkSoundEmitter.md)

## Methods

### <a id="MultiplayerInfrastructure_Audio_NetworkSoundEmitter_Play"></a> Play\(\)

현재 Transform의 월드 좌표를 기준으로 효과음을 공유 재생합니다.

```csharp
[ContextMenu("Play Shared Sound")]
public bool Play()
```

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_NetworkSoundEmitter_PlayAt_UnityEngine_Vector3_"></a> PlayAt\(Vector3\)

호출자가 지정한 월드 좌표를 기준으로 효과음을 공유 재생합니다.

```csharp
public bool PlayAt(Vector3 position)
```

#### Parameters

`position` Vector3

#### Returns

 bool

