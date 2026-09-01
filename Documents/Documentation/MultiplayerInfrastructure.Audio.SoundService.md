# <a id="MultiplayerInfrastructure_Audio_SoundService"></a> Class SoundService

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

지정한 위치에서 효과음을 재생합니다. 네트워크 동기화 없이 호출한 클라이언트의 로컬에서만 재생됩니다.

```csharp
public sealed class SoundService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[SoundService](MultiplayerInfrastructure.Audio.SoundService.md)

## Methods

### <a id="MultiplayerInfrastructure_Audio_SoundService_IsValidSoundResourceIdentifier_System_String_"></a> IsValidSoundResourceIdentifier\(string\)

Resources 식별자가 안전한 형식인지 확인합니다.

```csharp
public static bool IsValidSoundResourceIdentifier(string soundResourceIdentifier)
```

#### Parameters

`soundResourceIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_SoundService_PlayAtPosition_System_String_UnityEngine_Vector3_System_Single_System_Single_"></a> PlayAtPosition\(string, Vector3, float, float\)

지정 위치에서 효과음을 재생합니다.

```csharp
public static bool PlayAtPosition(string soundResourceIdentifier, Vector3 position, float volume = 1, float spatialBlend = 1)
```

#### Parameters

`soundResourceIdentifier` string

Resources/Sound 아래의 클립 식별자입니다.

`position` Vector3

3D 감쇠 기준이 되는 월드 좌표입니다.

`volume` float

0에서 1 사이의 음량입니다.

`spatialBlend` float

0은 2D, 1은 완전한 3D 재생입니다.

#### Returns

 bool

재생을 시작했으면 true입니다.

