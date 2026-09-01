# <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController"></a> Class RegistryPreloaderController

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

RegistryPreloader는 게임 시작 시점에 필요한 레지스트리 항목을 미리 로드할 수 있도록 합니다.

레지스트리에 등록할 개별 항목들에 Registry 로직이 있음에도 별도로 작성한 것은,
이들 로직은 Unity Life Cycle 과정 내에서 호출되도록 되어있어, 하이어라키에 존재하지 않으면
레지스터되지 않기 때문입니다.

실제로는 ScriptableObject를 로드하여 등록합니다:
특정한 씬에 의존하거나, 관리가 적절히 되지 않을 수 있을 우려를 제거하기 위함입니다.

```csharp
public class RegistryPreloaderController : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[RegistryPreloaderController](MultiplayerInfrastructure.Registry.RegistryPreloaderController.md)

## Fields

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadEntitySO"></a> preloadEntitySO

```csharp
public RegistryPreloadEntitySO preloadEntitySO
```

#### Field Value

 [RegistryPreloadEntitySO](MultiplayerInfrastructure.Registry.RegistryPreloadEntitySO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadIconSpriteSO"></a> preloadIconSpriteSO

```csharp
public RegistryPreloadIconSpriteSO preloadIconSpriteSO
```

#### Field Value

 [RegistryPreloadIconSpriteSO](MultiplayerInfrastructure.Registry.RegistryPreloadIconSpriteSO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadInteractableEntitySO"></a> preloadInteractableEntitySO

```csharp
public RegistryPreloadInteractableEntitySO preloadInteractableEntitySO
```

#### Field Value

 [RegistryPreloadInteractableEntitySO](MultiplayerInfrastructure.Registry.RegistryPreloadInteractableEntitySO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadNpcSO"></a> preloadNpcSO

```csharp
public RegistryPreloadNpcSO preloadNpcSO
```

#### Field Value

 [RegistryPreloadNpcSO](MultiplayerInfrastructure.Registry.RegistryPreloadNpcSO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadPlayerCharacterSO"></a> preloadPlayerCharacterSO

```csharp
public RegistryPreloadPlayerCharacterSO preloadPlayerCharacterSO
```

#### Field Value

 [RegistryPreloadPlayerCharacterSO](MultiplayerInfrastructure.Registry.RegistryPreloadPlayerCharacterSO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadProblemSetSO"></a> preloadProblemSetSO

```csharp
public RegistryPreloadProblemSetSO preloadProblemSetSO
```

#### Field Value

 [RegistryPreloadProblemSetSO](MultiplayerInfrastructure.Registry.RegistryPreloadProblemSetSO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadScenarioGraphSO"></a> preloadScenarioGraphSO

```csharp
public RegistryPreloadScenarioGraphSO preloadScenarioGraphSO
```

#### Field Value

 [RegistryPreloadScenarioGraphSO](MultiplayerInfrastructure.Registry.RegistryPreloadScenarioGraphSO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadUIControllerSO"></a> preloadUIControllerSO

```csharp
public RegistryPreloadUIControllerSO preloadUIControllerSO
```

#### Field Value

 [RegistryPreloadUIControllerSO](MultiplayerInfrastructure.Registry.RegistryPreloadUIControllerSO.md)

### <a id="MultiplayerInfrastructure_Registry_RegistryPreloaderController_preloadWaypointSO"></a> preloadWaypointSO

```csharp
public RegistryPreloadWaypointSO preloadWaypointSO
```

#### Field Value

 [RegistryPreloadWaypointSO](MultiplayerInfrastructure.Registry.RegistryPreloadWaypointSO.md)

