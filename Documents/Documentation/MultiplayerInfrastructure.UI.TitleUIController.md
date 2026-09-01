# <a id="MultiplayerInfrastructure_UI_TitleUIController"></a> Class TitleUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[RequireComponent(typeof(UIDocument))]
public class TitleUIController : UIControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[TitleUIController](MultiplayerInfrastructure.UI.TitleUIController.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Methods

### <a id="MultiplayerInfrastructure_UI_TitleUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ClearActionbar"></a> ClearActionbar\(\)

```csharp
public void ClearActionbar()
```

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ClearAll"></a> ClearAll\(\)

```csharp
public void ClearAll()
```

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ClearTitle"></a> ClearTitle\(\)

```csharp
public void ClearTitle()
```

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ResetTimesAndSubtitle"></a> ResetTimesAndSubtitle\(\)

```csharp
public void ResetTimesAndSubtitle()
```

### <a id="MultiplayerInfrastructure_UI_TitleUIController_SetTimes_System_Int32_System_Int32_System_Int32_"></a> SetTimes\(int, int, int\)

```csharp
public void SetTimes(int fadeInTicks, int stayTicks, int fadeOutTicks)
```

#### Parameters

`fadeInTicks` int

`stayTicks` int

`fadeOutTicks` int

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ShowActionbar_System_String_"></a> ShowActionbar\(string\)

```csharp
public void ShowActionbar(string actionbar)
```

#### Parameters

`actionbar` string

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ShowActionbar_System_String_System_Int32_System_Int32_System_Int32_"></a> ShowActionbar\(string, int, int, int\)

지정한 틱(20틱 = 1초) 타이밍으로 액션바를 한 번 표시한다.
이 표시에만 타이밍이 적용되며 <xref href="MultiplayerInfrastructure.UI.TitleUIController.SetTimes(System.Int32%2cSystem.Int32%2cSystem.Int32)" data-throw-if-not-resolved="false"></xref>의 공유 기본값은 유지된다.

```csharp
public void ShowActionbar(string actionbar, int fadeInTicks, int stayTicks, int fadeOutTicks)
```

#### Parameters

`actionbar` string

`fadeInTicks` int

`stayTicks` int

`fadeOutTicks` int

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ShowSubtitle_System_String_"></a> ShowSubtitle\(string\)

```csharp
public void ShowSubtitle(string subtitle)
```

#### Parameters

`subtitle` string

### <a id="MultiplayerInfrastructure_UI_TitleUIController_ShowTitle_System_String_System_String_"></a> ShowTitle\(string, string\)

```csharp
public void ShowTitle(string title, string subtitle = null)
```

#### Parameters

`title` string

`subtitle` string

