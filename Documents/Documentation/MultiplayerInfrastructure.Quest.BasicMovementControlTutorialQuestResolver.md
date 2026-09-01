# <a id="MultiplayerInfrastructure_Quest_BasicMovementControlTutorialQuestResolver"></a> Class BasicMovementControlTutorialQuestResolver

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

기본 이동 조작 튜토리얼 전용 퀘스트 처리기.
WASD와 마우스 기본 조작이 있었던 프레임의 시간을 누적한다. 총 3초 이상 조작하고
키보드와 마우스를 각각 한 번 이상 조작해야 이동 조작 퀘스트를 완료한다.
이 세부 조건은 퀘스트 표시 항목으로 노출하지 않는다.

```csharp
[DisallowMultipleComponent]
[RequireComponent(typeof(QuestManager))]
public sealed class BasicMovementControlTutorialQuestResolver : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[BasicMovementControlTutorialQuestResolver](MultiplayerInfrastructure.Quest.BasicMovementControlTutorialQuestResolver.md)

## Fields

### <a id="MultiplayerInfrastructure_Quest_BasicMovementControlTutorialQuestResolver_CompletionSignalIdentifier"></a> CompletionSignalIdentifier

```csharp
public const string CompletionSignalIdentifier = "tutorial_player_moved"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Quest_BasicMovementControlTutorialQuestResolver_QuestIdentifier"></a> QuestIdentifier

```csharp
public const string QuestIdentifier = "tutorial-move"
```

#### Field Value

 string

