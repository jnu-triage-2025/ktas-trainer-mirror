# <a id="MultiplayerInfrastructure_Quest_QuestManager"></a> Class QuestManager

Namespace: [MultiplayerInfrastructure.Quest](MultiplayerInfrastructure.Quest.md)  
Assembly: Assembly\-CSharp.dll  

Manages quest lifecycle and tracked selections shared by UI controllers.

```csharp
public class QuestManager : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[QuestManager](MultiplayerInfrastructure.Quest.QuestManager.md)

## Properties

### <a id="MultiplayerInfrastructure_Quest_QuestManager_Quests"></a> Quests

```csharp
public IReadOnlyList<QuestData> Quests { get; }
```

#### Property Value

 IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_TrackedQuests"></a> TrackedQuests

```csharp
public IReadOnlyList<QuestData> TrackedQuests { get; }
```

#### Property Value

 IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

## Methods

### <a id="MultiplayerInfrastructure_Quest_QuestManager_AddOrUpdateQuest_MultiplayerInfrastructure_Quest_QuestData_System_Boolean_"></a> AddOrUpdateQuest\(QuestData, bool\)

```csharp
public void AddOrUpdateQuest(QuestData quest, bool notify = true)
```

#### Parameters

`quest` [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

`notify` bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_ClearAll"></a> ClearAll\(\)

```csharp
public void ClearAll()
```

### <a id="MultiplayerInfrastructure_Quest_QuestManager_CompleteQuest_System_String_"></a> CompleteQuest\(string\)

특수 게임플레이 resolver가 명시적으로 완료한 퀘스트를 반영한다.
일반 퀘스트는 criteria 평가를 계속 사용하며, 입력 교육처럼 criteria로 표현할 수 없는
누적 행동은 해당 resolver만 이 경로를 호출한다.

```csharp
public bool CompleteQuest(string questId)
```

#### Parameters

`questId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_EvaluateAllQuestProgress"></a> EvaluateAllQuestProgress\(\)

```csharp
public void EvaluateAllQuestProgress()
```

### <a id="MultiplayerInfrastructure_Quest_QuestManager_EvaluateQuestProgress_System_String_"></a> EvaluateQuestProgress\(string\)

```csharp
public bool EvaluateQuestProgress(string questId)
```

#### Parameters

`questId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_ExpireQuestPresentation_System_String_"></a> ExpireQuestPresentation\(string\)

```csharp
public void ExpireQuestPresentation(string questId)
```

#### Parameters

`questId` string

### <a id="MultiplayerInfrastructure_Quest_QuestManager_GetActiveWaypointIdentifier_MultiplayerInfrastructure_Quest_QuestData_"></a> GetActiveWaypointIdentifier\(QuestData\)

```csharp
public static string GetActiveWaypointIdentifier(QuestData quest)
```

#### Parameters

`quest` [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_Quest_QuestManager_GetActiveWaypointIdentifiers_MultiplayerInfrastructure_Quest_QuestData_"></a> GetActiveWaypointIdentifiers\(QuestData\)

```csharp
public static IReadOnlyList<string> GetActiveWaypointIdentifiers(QuestData quest)
```

#### Parameters

`quest` [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

#### Returns

 IReadOnlyList<string\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_GetQuestTasks_MultiplayerInfrastructure_Quest_QuestData_"></a> GetQuestTasks\(QuestData\)

```csharp
public static IReadOnlyList<QuestCompletionCriteria> GetQuestTasks(QuestData quest)
```

#### Parameters

`quest` [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

#### Returns

 IReadOnlyList<[QuestCompletionCriteria](MultiplayerInfrastructure.Quest.QuestCompletionCriteria.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_HasQuest_System_String_"></a> HasQuest\(string\)

```csharp
public bool HasQuest(string questId)
```

#### Parameters

`questId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_RemoveQuest_System_String_"></a> RemoveQuest\(string\)

```csharp
public void RemoveQuest(string questId)
```

#### Parameters

`questId` string

### <a id="MultiplayerInfrastructure_Quest_QuestManager_ResetProgressForSessionEnd"></a> ResetProgressForSessionEnd\(\)

세션 종료 시 기본 정책(진행 상태 초기화)을 적용한다.
<xref href="MultiplayerInfrastructure.Quest.QuestData.PersistProgressOnSessionEnd" data-throw-if-not-resolved="false"></xref>가 true인 퀘스트만 유지한다.

```csharp
public void ResetProgressForSessionEnd()
```

### <a id="MultiplayerInfrastructure_Quest_QuestManager_SetQuestPreviewImmediateTransition_System_Boolean_"></a> SetQuestPreviewImmediateTransition\(bool\)

```csharp
public void SetQuestPreviewImmediateTransition(bool enabled)
```

#### Parameters

`enabled` bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_SetQuests_System_Collections_Generic_IEnumerable_MultiplayerInfrastructure_Quest_QuestData__System_Boolean_"></a> SetQuests\(IEnumerable<QuestData\>, bool\)

```csharp
public void SetQuests(IEnumerable<QuestData> quests, bool clearExisting = true)
```

#### Parameters

`quests` IEnumerable<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

`clearExisting` bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_SetTracked_System_String_System_Boolean_"></a> SetTracked\(string, bool\)

```csharp
public void SetTracked(string questId, bool tracked)
```

#### Parameters

`questId` string

`tracked` bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_TryGetQuest_System_String_MultiplayerInfrastructure_Quest_QuestData__"></a> TryGetQuest\(string, out QuestData\)

```csharp
public bool TryGetQuest(string questId, out QuestData quest)
```

#### Parameters

`questId` string

`quest` [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Quest_QuestManager_OnQuestCompleted"></a> OnQuestCompleted

```csharp
public event Action<QuestData> OnQuestCompleted
```

#### Event Type

 Action<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_OnQuestListChanged"></a> OnQuestListChanged

```csharp
public event Action<IReadOnlyList<QuestData>> OnQuestListChanged
```

#### Event Type

 Action<IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_OnQuestPresentationExpired"></a> OnQuestPresentationExpired

```csharp
public event Action<string> OnQuestPresentationExpired
```

#### Event Type

 Action<string\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_OnQuestPreviewImmediateTransitionChanged"></a> OnQuestPreviewImmediateTransitionChanged

```csharp
public event Action<bool> OnQuestPreviewImmediateTransitionChanged
```

#### Event Type

 Action<bool\>

### <a id="MultiplayerInfrastructure_Quest_QuestManager_OnTrackedQuestsChanged"></a> OnTrackedQuestsChanged

```csharp
public event Action<IReadOnlyList<QuestData>> OnTrackedQuestsChanged
```

#### Event Type

 Action<IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>\>

