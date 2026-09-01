# <a id="MultiplayerInfrastructure_UI_LoadingScreen"></a> Class LoadingScreen

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

씬과 카메라의 수명과 무관하게 표시되는 전역 로딩 화면입니다.
여러 비동기 작업이 겹쳐도 마지막 작업이 끝날 때까지 화면을 유지합니다.

```csharp
[DefaultExecutionOrder(-2000)]
public sealed class LoadingScreen : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LoadingScreen](MultiplayerInfrastructure.UI.LoadingScreen.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_LoadingScreen_Begin_System_String_"></a> Begin\(string\)

로딩 상태를 시작하고, Dispose 시 해당 작업을 완료합니다.

```csharp
public static IDisposable Begin(string message = "로드 중..")
```

#### Parameters

`message` string

#### Returns

 IDisposable

### <a id="MultiplayerInfrastructure_UI_LoadingScreen_LoadSceneAsync_System_String_System_String_"></a> LoadSceneAsync\(string, string\)

단일 씬 전환을 시작합니다. 코루틴은 이 영구 오브젝트가 실행하므로
출발 씬의 UI가 파괴되어도 로딩 작업의 종료 처리가 보장됩니다.

```csharp
public static void LoadSceneAsync(string sceneName, string message = null)
```

#### Parameters

`sceneName` string

`message` string

### <a id="MultiplayerInfrastructure_UI_LoadingScreen_Report_System_String_System_Nullable_System_Single__"></a> Report\(string, float?\)

현재 로딩 화면의 안내 문구와 진행률을 갱신합니다.

```csharp
public static void Report(string message, float? progress = null)
```

#### Parameters

`message` string

`progress` float?

