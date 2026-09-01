# <a id="MultiplayerInfrastructure_UI_UIControllerABC"></a> Class UIControllerABC

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

UIControllerABC는 모든 UI 컨트롤러의 상속이 의도되는 추상 메서드입니다. 각 UI 요소들이 싱글톤 패턴이
의도되지 않았으므로, 외부에서 UI 컨트롤을 취득하는 데 있어서 별개의 레지스트리로부터 접근할 수 있도록 하고 있습니다.

UIControllerABC는 레지스트리를 통해 외부에서 접근 가능하도록 이 컨트롤을 레지스터하는 역할을 합니다.
새 일반 UI 컨트롤러는 MonoBehaviour를 직접 상속하지 말고 이 클래스를 상속해야 합니다.

```csharp
public abstract class UIControllerABC : UIDocumentControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md)

#### Derived

[ChatUIController](MultiplayerInfrastructure.UI.ChatUIController.md), 
[CrosshairUIController](MultiplayerInfrastructure.UI.CrosshairUIController.md), 
[DialoguePanelUIController](MultiplayerInfrastructure.UI.DialoguePanelUIController.md), 
[EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md), 
[GameEscapeMenuUIController](MultiplayerInfrastructure.UI.GameEscapeMenuUIController.md), 
[GraphicsSettingsUIController](MultiplayerInfrastructure.UI.GraphicsSettingsUIController.md), 
[HotbarUIController](MultiplayerInfrastructure.UI.HotbarUIController.md), 
[InteractableObjectHintUIController](MultiplayerInfrastructure.UI.InteractableObjectHintUIController.md), 
[InventoryUIController](MultiplayerInfrastructure.UI.InventoryUIController.md), 
[ItemSubmissionUIController](MultiplayerInfrastructure.UI.ItemSubmissionUIController.md), 
[KeyConfigUIController](MultiplayerInfrastructure.UI.KeyConfigUIController.md), 
[ProblemSheetUIController](MultiplayerInfrastructure.UI.ProblemSheetUIController.md), 
[QuestPreviewHudUIController](MultiplayerInfrastructure.UI.QuestPreviewHudUIController.md), 
[QuestUIController](MultiplayerInfrastructure.UI.QuestUIController.md), 
[SettingsUIController](MultiplayerInfrastructure.UI.SettingsUIController.md), 
[TimeDisplayUIController](MultiplayerInfrastructure.UI.TimeDisplayUIController.md), 
[TitleUIController](MultiplayerInfrastructure.UI.TitleUIController.md), 
[TriageAssessmentUIController](TriageTrainer.UI.TriageAssessmentUIController.md)

#### Inherited Members

[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Methods

### <a id="MultiplayerInfrastructure_UI_UIControllerABC_Awake"></a> Awake\(\)

```csharp
protected virtual void Awake()
```

### <a id="MultiplayerInfrastructure_UI_UIControllerABC_OnDestroy"></a> OnDestroy\(\)

```csharp
protected virtual void OnDestroy()
```

