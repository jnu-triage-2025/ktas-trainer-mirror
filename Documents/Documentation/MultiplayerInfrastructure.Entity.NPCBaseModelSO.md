# <a id="MultiplayerInfrastructure_Entity_NPCBaseModelSO"></a> Class NPCBaseModelSO

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
[CreateAssetMenu(fileName = "New NPC Base Model", menuName = "Multiplayer Infrastructure/NPC Base Model")]
public class NPCBaseModelSO : ScriptableObject
```

#### Inheritance

object ← 
Object ← 
ScriptableObject ← 
[NPCBaseModelSO](MultiplayerInfrastructure.Entity.NPCBaseModelSO.md)

## Fields

### <a id="MultiplayerInfrastructure_Entity_NPCBaseModelSO_description"></a> description

```csharp
[TextArea]
[SerializeField]
public string description
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Entity_NPCBaseModelSO_displayName"></a> displayName

```csharp
[SerializeField]
public string displayName
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Entity_NPCBaseModelSO_identifier"></a> identifier

```csharp
[SerializeField]
public string identifier
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Entity_NPCBaseModelSO_scenarioInteracts"></a> scenarioInteracts

```csharp
[Header("Scenario Interacts")]
[SerializeField]
public List<NPCScenarioInteractDefinition> scenarioInteracts
```

#### Field Value

 List<[NPCScenarioInteractDefinition](MultiplayerInfrastructure.Entity.NPCScenarioInteractDefinition.md)\>

### <a id="MultiplayerInfrastructure_Entity_NPCBaseModelSO_submissionInteracts"></a> submissionInteracts

```csharp
[Header("Item Submission Interacts")]
[Tooltip("이 NPC 에게 아이템을 제출하는 상호작용 목록. Npc 가 런타임에 ItemSubmissionInteractable 을 자동 생성한다.")]
[SerializeField]
public List<NPCSubmissionInteractDefinition> submissionInteracts
```

#### Field Value

 List<[NPCSubmissionInteractDefinition](MultiplayerInfrastructure.Entity.NPCSubmissionInteractDefinition.md)\>

