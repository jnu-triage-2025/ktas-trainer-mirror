# <a id="MultiplayerInfrastructure_UI_TimeDisplayUIController"></a> Class TimeDisplayUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

시간 표시(스톱워치/카운트다운) HUD 컨트롤러.

화면 상단 중앙에 hh:mm:ss 를 표기한다. 값의 진행/방향/표시 여부는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState" data-throw-if-not-resolved="false"></xref> 정적 저장소에서 매 프레임 읽어 렌더한다.
저장소는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref> 가 서버 권한으로 동기화하므로
모든 클라이언트가 동일한 값을 본다.

비차단 HUD 이므로 <xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref> 를 사용하지 않는다.

UI 구성은 코드 전용이다: 빈 UIDocument(+ PanelSettings)에 <xref href="MultiplayerInfrastructure.UI.TimeDisplayElement" data-throw-if-not-resolved="false"></xref> 를
C# 으로 생성해 부착하며, 시각 스타일은 요소의 인라인 스타일에서 관리한다(uxml/uss/StyleSheet 불필요).

```csharp
[RequireComponent(typeof(UIDocument))]
public class TimeDisplayUIController : UIControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[TimeDisplayUIController](MultiplayerInfrastructure.UI.TimeDisplayUIController.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Methods

### <a id="MultiplayerInfrastructure_UI_TimeDisplayUIController_OnDestroy"></a> OnDestroy\(\)

```csharp
protected override void OnDestroy()
```

