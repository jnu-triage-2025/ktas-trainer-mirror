# <a id="MultiplayerInfrastructure_UI_HotbarUIController"></a> Class HotbarUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[RequireComponent(typeof(UIDocument))]
public class HotbarUIController : UIControllerABC
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[HotbarUIController](MultiplayerInfrastructure.UI.HotbarUIController.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Properties

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_SelectedSlot"></a> SelectedSlot

```csharp
public int SelectedSlot { get; }
```

#### Property Value

 int

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_SlotCount"></a> SlotCount

현재 핫바가 실제로 표시 중인 슬롯 수. 초기화 전에는 설정값을 반환한다.

```csharp
public int SlotCount { get; }
```

#### Property Value

 int

## Methods

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_BindInventory_System_Collections_Generic_IReadOnlyList_InventorySlotModelDTO__"></a> BindInventory\(IReadOnlyList<InventorySlotModelDTO\>\)

```csharp
public void BindInventory(IReadOnlyList<InventorySlotModelDTO> inventory)
```

#### Parameters

`inventory` IReadOnlyList<InventorySlotModelDTO\>

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_CycleSelection_System_Int32_"></a> CycleSelection\(int\)

```csharp
public void CycleSelection(int direction)
```

#### Parameters

`direction` int

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_SetSelectedIndex_System_Int32_"></a> SetSelectedIndex\(int\)

```csharp
public void SetSelectedIndex(int index)
```

#### Parameters

`index` int

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_SetupHotbarUI"></a> SetupHotbarUI\(\)

```csharp
public void SetupHotbarUI()
```

### <a id="MultiplayerInfrastructure_UI_HotbarUIController_OnSelectedSlotChanged"></a> OnSelectedSlotChanged

```csharp
public event Action OnSelectedSlotChanged
```

#### Event Type

 Action

