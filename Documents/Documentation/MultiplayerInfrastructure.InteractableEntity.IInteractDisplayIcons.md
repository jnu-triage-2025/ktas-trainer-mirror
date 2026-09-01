# <a id="MultiplayerInfrastructure_InteractableEntity_IInteractDisplayIcons"></a> Interface IInteractDisplayIcons

Namespace: [MultiplayerInfrastructure.InteractableEntity](MultiplayerInfrastructure.InteractableEntity.md)  
Assembly: Assembly\-CSharp.dll  

하나의 상호작용 항목에 여러 표시 아이콘을 제공할 때 구현합니다.
각 아이콘은 힌트 UI에서 독립된 정사각형 슬롯에 원본 비율을 유지해 표시됩니다.

```csharp
public interface IInteractDisplayIcons
```

## Properties

### <a id="MultiplayerInfrastructure_InteractableEntity_IInteractDisplayIcons_DisplayIcons"></a> DisplayIcons

```csharp
IReadOnlyList<Sprite> DisplayIcons { get; }
```

#### Property Value

 IReadOnlyList<Sprite\>

