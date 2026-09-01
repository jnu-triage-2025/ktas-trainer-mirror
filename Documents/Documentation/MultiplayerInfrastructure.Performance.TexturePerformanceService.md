# <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService"></a> Class TexturePerformanceService

Namespace: [MultiplayerInfrastructure.Performance](MultiplayerInfrastructure.Performance.md)  
Assembly: Assembly\-CSharp.dll  

종합 그래픽 설정을 저장하고 즉시 적용하는 서비스입니다.
기존 텍스처 품질 API는 하위 호환을 위해 유지합니다.

```csharp
public class TexturePerformanceService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[TexturePerformanceService](MultiplayerInfrastructure.Performance.TexturePerformanceService.md)

## Properties

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_CurrentProfile"></a> CurrentProfile

```csharp
public GraphicsQualityProfile CurrentProfile { get; }
```

#### Property Value

 [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_CurrentQuality"></a> CurrentQuality

```csharp
public TextureQuality CurrentQuality { get; }
```

#### Property Value

 [TextureQuality](MultiplayerInfrastructure.UI.Models.TextureQuality.md)

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_CurrentSettings"></a> CurrentSettings

```csharp
public GraphicsSettingsData CurrentSettings { get; }
```

#### Property Value

 [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

## Methods

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_LoadAndApply"></a> LoadAndApply\(\)

```csharp
public void LoadAndApply()
```

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_ReapplySceneSettings"></a> ReapplySceneSettings\(\)

```csharp
public void ReapplySceneSettings()
```

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_ResetToDefault"></a> ResetToDefault\(\)

```csharp
public void ResetToDefault()
```

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_SetProfile_MultiplayerInfrastructure_Performance_GraphicsQualityProfile_"></a> SetProfile\(GraphicsQualityProfile\)

```csharp
public void SetProfile(GraphicsQualityProfile profile)
```

#### Parameters

`profile` [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_SetQuality_MultiplayerInfrastructure_UI_Models_TextureQuality_"></a> SetQuality\(TextureQuality\)

기존 텍스처 설정 호출자를 위한 호환 API입니다.

```csharp
public void SetQuality(TextureQuality quality)
```

#### Parameters

`quality` [TextureQuality](MultiplayerInfrastructure.UI.Models.TextureQuality.md)

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_SetSettings_MultiplayerInfrastructure_Performance_GraphicsSettingsData_"></a> SetSettings\(GraphicsSettingsData\)

```csharp
public void SetSettings(GraphicsSettingsData settings)
```

#### Parameters

`settings` [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_ToQualitySettingsAntiAliasing_MultiplayerInfrastructure_Performance_GraphicsAntiAliasing_"></a> ToQualitySettingsAntiAliasing\(GraphicsAntiAliasing\)

```csharp
public static int ToQualitySettingsAntiAliasing(GraphicsAntiAliasing value)
```

#### Parameters

`value` [GraphicsAntiAliasing](MultiplayerInfrastructure.Performance.GraphicsAntiAliasing.md)

#### Returns

 int

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_OnQualityChanged"></a> OnQualityChanged

```csharp
public event Action<TextureQuality> OnQualityChanged
```

#### Event Type

 Action<[TextureQuality](MultiplayerInfrastructure.UI.Models.TextureQuality.md)\>

### <a id="MultiplayerInfrastructure_Performance_TexturePerformanceService_OnSettingsChanged"></a> OnSettingsChanged

```csharp
public event Action<GraphicsSettingsData> OnSettingsChanged
```

#### Event Type

 Action<[GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)\>

