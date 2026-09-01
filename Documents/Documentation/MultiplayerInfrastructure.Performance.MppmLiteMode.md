# <a id="MultiplayerInfrastructure_Performance_MppmLiteMode"></a> Class MppmLiteMode

Namespace: [MultiplayerInfrastructure.Performance](MultiplayerInfrastructure.Performance.md)  
Assembly: Assembly\-CSharp.dll  

MPPM 추가 Editor 인스턴스를 저사양 클라이언트로 전환합니다.
기본 세션은 조작할 수 있도록 화면을 유지하고, HeadlessLite 태그에서만 표현을 숨깁니다.
Main Editor와 일반 Player 빌드에는 절대로 적용하지 않습니다.

```csharp
public static class MppmLiteMode
```

#### Inheritance

object ← 
[MppmLiteMode](MultiplayerInfrastructure.Performance.MppmLiteMode.md)

## Fields

### <a id="MultiplayerInfrastructure_Performance_MppmLiteMode_FullClientTag"></a> FullClientTag

```csharp
public const string FullClientTag = "FullClient"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Performance_MppmLiteMode_HeadlessLiteTag"></a> HeadlessLiteTag

```csharp
public const string HeadlessLiteTag = "HeadlessLite"
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_Performance_MppmLiteMode_IsActive"></a> IsActive

```csharp
public static bool IsActive { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Performance_MppmLiteMode_IsHeadless"></a> IsHeadless

```csharp
public static bool IsHeadless { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Performance_MppmLiteMode_CreateSettings"></a> CreateSettings\(\)

```csharp
public static GraphicsSettingsData CreateSettings()
```

#### Returns

 [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

### <a id="MultiplayerInfrastructure_Performance_MppmLiteMode_StripVisuals_UnityEngine_GameObject_"></a> StripVisuals\(GameObject\)

동적으로 생성된 객체의 표현 컴포넌트를 Lite 클론에서 비활성화합니다.
원래 상태는 기록되며 Lite 해제 시 복원됩니다.

```csharp
public static void StripVisuals(GameObject root)
```

#### Parameters

`root` GameObject

