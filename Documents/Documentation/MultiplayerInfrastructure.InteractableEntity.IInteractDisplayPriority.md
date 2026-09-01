# <a id="MultiplayerInfrastructure_InteractableEntity_IInteractDisplayPriority"></a> Interface IInteractDisplayPriority

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

주변 상호작용 목록에서 기본 감지 순서보다 먼저 표시되어야 하는 항목이 선택적으로 구현합니다.
값이 클수록 먼저 표시되며, 같은 값인 항목의 기존 감지 순서는 유지됩니다.

```csharp
public interface IInteractDisplayPriority
```

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteractDisplayPriority_DisplayPriority"></a> DisplayPriority

```csharp
int DisplayPriority { get; }
```

#### Property Value

 int

