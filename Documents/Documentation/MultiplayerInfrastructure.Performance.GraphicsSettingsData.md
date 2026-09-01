# <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData"></a> Class GraphicsSettingsData

Namespace: [MultiplayerInfrastructure.Performance](MultiplayerInfrastructure.Performance.md)  
Assembly: Assembly\-CSharp.dll  

저장·복제 가능한 그래픽 설정 스냅샷입니다. 디스플레이, 품질, URP 및
일반적인 렌더링 옵션을 한 곳에서 관리합니다.

```csharp
[Serializable]
public class GraphicsSettingsData
```

#### Inheritance

object ← 
[GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

## Fields

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_AdditionalLightShadows"></a> AdditionalLightShadows

```csharp
public bool AdditionalLightShadows
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_AdditionalLights"></a> AdditionalLights

```csharp
public GraphicsAdditionalLights AdditionalLights
```

#### Field Value

 [GraphicsAdditionalLights](MultiplayerInfrastructure.Performance.GraphicsAdditionalLights.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_AdditionalLightsPerObject"></a> AdditionalLightsPerObject

```csharp
[Range(0, 8)]
public int AdditionalLightsPerObject
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_AnisotropicFiltering"></a> AnisotropicFiltering

```csharp
public AnisotropicFiltering AnisotropicFiltering
```

#### Field Value

 AnisotropicFiltering

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_AntiAliasing"></a> AntiAliasing

```csharp
public GraphicsAntiAliasing AntiAliasing
```

#### Field Value

 [GraphicsAntiAliasing](MultiplayerInfrastructure.Performance.GraphicsAntiAliasing.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_DepthTexture"></a> DepthTexture

```csharp
public bool DepthTexture
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_DynamicResolution"></a> DynamicResolution

```csharp
public bool DynamicResolution
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_FieldOfView"></a> FieldOfView

```csharp
[Range(40, 100)]
public float FieldOfView
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_FrameRateLimit"></a> FrameRateLimit

```csharp
public int FrameRateLimit
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_FullScreenMode"></a> FullScreenMode

```csharp
public FullScreenMode FullScreenMode
```

#### Field Value

 FullScreenMode

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_Gamma"></a> Gamma

```csharp
[Range(0.5, 1.5)]
public float Gamma
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_Hdr"></a> Hdr

```csharp
public bool Hdr
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_LodBias"></a> LodBias

```csharp
[Header("Detail and effects")]
[Range(0.25, 4)]
public float LodBias
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_MaximumLodLevel"></a> MaximumLodLevel

```csharp
[Range(0, 3)]
public int MaximumLodLevel
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_OpaqueTexture"></a> OpaqueTexture

```csharp
public bool OpaqueTexture
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_PixelLightCount"></a> PixelLightCount

```csharp
public int PixelLightCount
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_PostProcessing"></a> PostProcessing

```csharp
public bool PostProcessing
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_Profile"></a> Profile

```csharp
public GraphicsQualityProfile Profile
```

#### Field Value

 [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_RealtimeReflectionProbes"></a> RealtimeReflectionProbes

```csharp
public bool RealtimeReflectionProbes
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ReflectionProbeBlending"></a> ReflectionProbeBlending

```csharp
public bool ReflectionProbeBlending
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ReflectionProbeBoxProjection"></a> ReflectionProbeBoxProjection

```csharp
public bool ReflectionProbeBoxProjection
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_RefreshRate"></a> RefreshRate

```csharp
public int RefreshRate
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_RenderScale"></a> RenderScale

```csharp
[Header("Rendering")]
[Range(0.5, 2)]
public float RenderScale
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ResolutionHeight"></a> ResolutionHeight

```csharp
public int ResolutionHeight
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ResolutionWidth"></a> ResolutionWidth

```csharp
[Header("Display")]
public int ResolutionWidth
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ShadowCascades"></a> ShadowCascades

```csharp
[Range(1, 4)]
public int ShadowCascades
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ShadowDistance"></a> ShadowDistance

```csharp
[Range(0, 200)]
public float ShadowDistance
```

#### Field Value

 float

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_ShadowResolution"></a> ShadowResolution

```csharp
public GraphicsShadowResolution ShadowResolution
```

#### Field Value

 [GraphicsShadowResolution](MultiplayerInfrastructure.Performance.GraphicsShadowResolution.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_Shadows"></a> Shadows

```csharp
[Header("Lighting and shadows")]
public bool Shadows
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_SoftParticles"></a> SoftParticles

```csharp
public bool SoftParticles
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_SoftShadows"></a> SoftShadows

```csharp
public bool SoftShadows
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_TextureMipmapLimit"></a> TextureMipmapLimit

```csharp
[Header("Textures")]
[Range(0, 3)]
public int TextureMipmapLimit
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_TextureStreaming"></a> TextureStreaming

```csharp
public bool TextureStreaming
```

#### Field Value

 bool

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_TextureStreamingBudgetMb"></a> TextureStreamingBudgetMb

```csharp
[Range(64, 2048)]
public int TextureStreamingBudgetMb
```

#### Field Value

 int

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_VSync"></a> VSync

```csharp
public bool VSync
```

#### Field Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_Clone"></a> Clone\(\)

```csharp
public GraphicsSettingsData Clone()
```

#### Returns

 [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

### <a id="MultiplayerInfrastructure_Performance_GraphicsSettingsData_Sanitize"></a> Sanitize\(\)

```csharp
public void Sanitize()
```

