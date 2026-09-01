# <a id="MultiplayerInfrastructure_InteractableEntity_IInteractToggleable"></a> Interface IInteractToggleable

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

상호작용을 런타임에 활성/비활성 전환할 수 있는 Interactable 이 구현한다.
시나리오 그래프 노드(예: NPCControl / ItemSubmissionConfig)가 이 인터페이스로
개별 Interactable 의 활성 상태를 제어한다.

```csharp
public interface IInteractToggleable
```

## Methods

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteractToggleable_SetEnabled_System_Boolean_"></a> SetEnabled\(bool\)

```csharp
void SetEnabled(bool enabled)
```

#### Parameters

`enabled` bool

