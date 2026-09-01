# <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen"></a> Class DefaultsLoadingScreen

Namespace: [MultiplayerInfrastructure.Definitions](MultiplayerInfrastructure.Definitions.md)  
Assembly: Assembly\-CSharp.dll  

로딩 화면에 표시할 기본 안내 문구를 관리합니다.

```csharp
public static class DefaultsLoadingScreen
```

#### Inheritance

object ← 
[DefaultsLoadingScreen](MultiplayerInfrastructure.Definitions.DefaultsLoadingScreen.md)

## Fields

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_GenericLoadingMessage"></a> GenericLoadingMessage

```csharp
public const string GenericLoadingMessage = "로드 중.."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_IndevSceneLoadingMessage"></a> IndevSceneLoadingMessage

```csharp
public const string IndevSceneLoadingMessage = "IndevScene 로드 중.."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_IngameSceneLoadingMessage"></a> IngameSceneLoadingMessage

```csharp
public const string IngameSceneLoadingMessage = "게임 시작 중.."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_IntroSceneLoadingMessage"></a> IntroSceneLoadingMessage

```csharp
public const string IntroSceneLoadingMessage = "로드 중.."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_OverworldSceneLoadingMessage"></a> OverworldSceneLoadingMessage

```csharp
public const string OverworldSceneLoadingMessage = "세계 리소스 로드 중.."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_SystemOverlaySceneLoadingMessage"></a> SystemOverlaySceneLoadingMessage

```csharp
public const string SystemOverlaySceneLoadingMessage = "시스템 로드 중.."
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_TutorialSceneLoadingMessage"></a> TutorialSceneLoadingMessage

```csharp
public const string TutorialSceneLoadingMessage = "튜토리얼 정보 로드 중.."
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Definitions_DefaultsLoadingScreen_GetSceneLoadingMessage_System_String_"></a> GetSceneLoadingMessage\(string\)

등록된 씬에는 전용 문구를, 그 외 씬에는 간략한 기본 문구를 반환합니다.

```csharp
public static string GetSceneLoadingMessage(string sceneName)
```

#### Parameters

`sceneName` string

#### Returns

 string

