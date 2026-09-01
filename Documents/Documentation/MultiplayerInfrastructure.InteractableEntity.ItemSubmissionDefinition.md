# <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition"></a> Class ItemSubmissionDefinition

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

아이템 제출 상호작용의 요구 사항/표시/완료 신호를 담는 직렬화 가능한 설정.

이 정의는 두 경로에서 값을 얻을 수 있다:
 1) 프리셋 기본값: <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable" data-throw-if-not-resolved="false"></xref> 컴포넌트(또는 스폰되는 프리팹)에 인스펙터로 사전 설정된다.
 2) 그래프 노드 오버라이드: 시나리오 그래프 노드가 런타임에 요구 아이템/완료 신호를 덮어쓴다.

완료 처리는 서버 세션 전역 신호(<xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref>)로 이루어진다.

```csharp
[Serializable]
public sealed class ItemSubmissionDefinition
```

#### Inheritance

object ← 
[ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

## Fields

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_completionSignalIdentifier"></a> completionSignalIdentifier

```csharp
[Tooltip("제출 성공 시 올릴 서버 세션 전역 신호 식별자('sig.' 접두사는 자동 정규화됨).")]
public string completionSignalIdentifier
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_consumeOnce"></a> consumeOnce

```csharp
[Tooltip("한 번 제출에 성공하면 이후 상호작용을 비활성화할지 여부.")]
public bool consumeOnce
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_displayText"></a> displayText

```csharp
[Tooltip("상호작용 힌트에 표시할 짧은 텍스트. 비어 있으면 기본값이 사용된다.")]
public string displayText
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_requiredItems"></a> requiredItems

```csharp
[Tooltip("요구되는 아이템 목록(식별자 + 수량). 모든 항목이 충족되어야 제출할 수 있다.")]
public List<ItemRequirement> requiredItems
```

#### Field Value

 List<[ItemRequirement](MultiplayerInfrastructure.InteractableEntity.ItemRequirement.md)\>

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_submitButtonText"></a> submitButtonText

```csharp
[Tooltip("제출 버튼 라벨.")]
public string submitButtonText
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_title"></a> title

```csharp
[Tooltip("제출 패널 제목.")]
public string title
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_Clone"></a> Clone\(\)

```csharp
public ItemSubmissionDefinition Clone()
```

#### Returns

 [ItemSubmissionDefinition](MultiplayerInfrastructure.InteractableEntity.ItemSubmissionDefinition.md)

### <a id="MultiplayerInfrastructure_InteractableEntity_ItemSubmissionDefinition_GetValidRequirements"></a> GetValidRequirements\(\)

유효한(식별자/수량이 채워진) 요구 아이템만 반환한다.

```csharp
public IReadOnlyList<ItemRequirement> GetValidRequirements()
```

#### Returns

 IReadOnlyList<[ItemRequirement](MultiplayerInfrastructure.InteractableEntity.ItemRequirement.md)\>

