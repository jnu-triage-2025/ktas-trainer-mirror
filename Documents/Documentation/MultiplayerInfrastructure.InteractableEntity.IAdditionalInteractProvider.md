# <a id="MultiplayerInfrastructure_InteractableEntity_IAdditionalInteractProvider"></a> Interface IAdditionalInteractProvider

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

기존 <xref href="MultiplayerInfrastructure.InteractableEntity.IInteractable" data-throw-if-not-resolved="false"></xref> 컨트롤러에 같은 GameObject의 기능 컴포넌트가
조건부 상호작용 항목을 보탤 때 사용한다.

```csharp
public interface IAdditionalInteractProvider
```

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_IAdditionalInteractProvider_AdditionalInteracts"></a> AdditionalInteracts

```csharp
IEnumerable<IInteract> AdditionalInteracts { get; }
```

#### Property Value

 IEnumerable<[IInteract](MultiplayerInfrastructure.InteractableEntity.IInteract.md)\>

