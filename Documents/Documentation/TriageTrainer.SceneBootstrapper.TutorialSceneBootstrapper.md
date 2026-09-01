# <a id="TriageTrainer_SceneBootstrapper_TutorialSceneBootstrapper"></a> Class TutorialSceneBootstrapper

Namespace: [TriageTrainer.SceneBootstrapper](TriageTrainer.SceneBootstrapper.md)  
Assembly: Assembly\-CSharp.dll  

TutorialScene용 자동 부트스트래퍼입니다.
IntroScene 흐름 없이 씬을 직접 재생하면, 자동으로 호스트(서버+클라이언트) 세션을 시작하고
SystemOverlayScene을 애디티브 로드합니다.
이후 프리팹 게임 오브젝트로 씬에 배치하여 사용합니다.

```csharp
[DefaultExecutionOrder(-1000)]
public class TutorialSceneBootstrapper : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[TutorialSceneBootstrapper](TriageTrainer.SceneBootstrapper.TutorialSceneBootstrapper.md)

