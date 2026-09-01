# <a id="TriageTrainer_SceneBootstrapper_IndevSceneBootstrapper"></a> Class IndevSceneBootstrapper

Namespace: [TriageTrainer.SceneBootstrapper](TriageTrainer.SceneBootstrapper.md)  
Assembly: Assembly\-CSharp.dll  

IndevScene용 자동 부트스트래퍼입니다.
IntroScene 흐름 없이 씬을 직접 재생하면, 자동으로 호스트(서버+클라이언트) 세션을 시작하고
SystemOverlayScene만 애디티브 로드합니다.
이후 프리팹 게임 오브젝트로 씬에 배치하여 사용합니다.

IndevScene은 자체 월드/스폰포인트를 갖춘 개발 씬이므로 OverworldScene을 로드하지 않습니다.
OverworldScene을 함께 로드하면 동명 스폰포인트(spawnpoint-commons)가 PlayerSpawnPointRegistry에서
IndevScene 스폰포인트를 덮어써 플레이어가 OverworldScene 위로 스폰되고, 월드 콘텐츠가 중복됩니다.

```csharp
[DefaultExecutionOrder(-1000)]
public class IndevSceneBootstrapper : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[IndevSceneBootstrapper](TriageTrainer.SceneBootstrapper.IndevSceneBootstrapper.md)

