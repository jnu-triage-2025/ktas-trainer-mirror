# <a id="MultiplayerInfrastructure_UI_QuestPreviewHudElement"></a> Class QuestPreviewHudElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class QuestPreviewHudElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[QuestPreviewHudElement](MultiplayerInfrastructure.UI.QuestPreviewHudElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_QuestPreviewHudElement__ctor"></a> QuestPreviewHudElement\(\)

```csharp
public QuestPreviewHudElement()
```

## Methods

### <a id="MultiplayerInfrastructure_UI_QuestPreviewHudElement_GetCurrentObjective_MultiplayerInfrastructure_Quest_QuestData_"></a> GetCurrentObjective\(QuestData\)

```csharp
public static string GetCurrentObjective(QuestData quest)
```

#### Parameters

`quest` [QuestData](MultiplayerInfrastructure.Quest.QuestData.md)

#### Returns

 string

### <a id="MultiplayerInfrastructure_UI_QuestPreviewHudElement_SetQuests_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Quest_QuestData__System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Quest_QuestData__"></a> SetQuests\(IReadOnlyList<QuestData\>, IReadOnlyList<QuestData\>\)

```csharp
public void SetQuests(IReadOnlyList<QuestData> tracked, IReadOnlyList<QuestData> completed)
```

#### Parameters

`tracked` IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

`completed` IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

### <a id="MultiplayerInfrastructure_UI_QuestPreviewHudElement_SetTrackedQuests_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Quest_QuestData__"></a> SetTrackedQuests\(IReadOnlyList<QuestData\>\)

```csharp
public void SetTrackedQuests(IReadOnlyList<QuestData> tracked)
```

#### Parameters

`tracked` IReadOnlyList<[QuestData](MultiplayerInfrastructure.Quest.QuestData.md)\>

