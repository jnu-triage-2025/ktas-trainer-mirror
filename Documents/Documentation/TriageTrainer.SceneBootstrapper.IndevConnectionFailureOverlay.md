# <a id="TriageTrainer_SceneBootstrapper_IndevConnectionFailureOverlay"></a> Class IndevConnectionFailureOverlay

Namespace: [TriageTrainer.SceneBootstrapper](TriageTrainer.SceneBootstrapper.md)  
Assembly: Assembly\-CSharp.dll  

사전에 애디티브 로드된 네트워크 세션 종료 씬의 UXML/USS UI 컨트롤러입니다.
모든 세션 부트스트래퍼가 같은 씬을 미리 로드하므로, 장애 표시 시 추가 씬 로딩이 없습니다.

```csharp
[DisallowMultipleComponent]
public sealed class IndevConnectionFailureOverlay : UIDocumentControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[IndevConnectionFailureOverlay](TriageTrainer.SceneBootstrapper.IndevConnectionFailureOverlay.md)

## Methods

### <a id="TriageTrainer_SceneBootstrapper_IndevConnectionFailureOverlay_BeginConnectionAttempt_System_String_System_UInt16_"></a> BeginConnectionAttempt\(string, ushort\)

선택된 네트워크 엔드포인트에 대한 연결 시도를 시작했음을 표시합니다.

```csharp
public void BeginConnectionAttempt(string address, ushort port)
```

#### Parameters

`address` string

`port` ushort

### <a id="TriageTrainer_SceneBootstrapper_IndevConnectionFailureOverlay_ShowConnectionError_System_String_"></a> ShowConnectionError\(string\)

세션 시작 API 자체가 실패했을 때 즉시 오류 화면을 표시합니다.

```csharp
public void ShowConnectionError(string message)
```

#### Parameters

`message` string

