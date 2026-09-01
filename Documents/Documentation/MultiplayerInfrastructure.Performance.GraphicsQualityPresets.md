# <a id="MultiplayerInfrastructure_Performance_GraphicsQualityPresets"></a> Class GraphicsQualityPresets

Namespace: [MultiplayerInfrastructure.Performance](MultiplayerInfrastructure.Performance.md)  
Assembly: Assembly\-CSharp.dll  

프로젝트의 5단계 권장 그래픽 프리셋을 생성합니다.

```csharp
public static class GraphicsQualityPresets
```

#### Inheritance

object ← 
[GraphicsQualityPresets](MultiplayerInfrastructure.Performance.GraphicsQualityPresets.md)

## Fields

### <a id="MultiplayerInfrastructure_Performance_GraphicsQualityPresets_DefaultProfile"></a> DefaultProfile

에디터와 프로젝트 베이스라인에서 사용하는 기본 프로파일입니다.

```csharp
public const GraphicsQualityProfile DefaultProfile = Low
```

#### Field Value

 [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsQualityPresets_RuntimeDefaultProfile"></a> RuntimeDefaultProfile

플레이어 설정이 없을 때 런타임에서 사용하는 기본 프로파일입니다.

```csharp
public const GraphicsQualityProfile RuntimeDefaultProfile = Medium
```

#### Field Value

 [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

## Methods

### <a id="MultiplayerInfrastructure_Performance_GraphicsQualityPresets_ApplyPresetValues_MultiplayerInfrastructure_Performance_GraphicsSettingsData_MultiplayerInfrastructure_Performance_GraphicsQualityProfile_"></a> ApplyPresetValues\(GraphicsSettingsData, GraphicsQualityProfile\)

```csharp
public static void ApplyPresetValues(GraphicsSettingsData s, GraphicsQualityProfile profile)
```

#### Parameters

`s` [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

`profile` [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsQualityPresets_Create_MultiplayerInfrastructure_Performance_GraphicsQualityProfile_"></a> Create\(GraphicsQualityProfile\)

```csharp
public static GraphicsSettingsData Create(GraphicsQualityProfile profile)
```

#### Parameters

`profile` [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

#### Returns

 [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

