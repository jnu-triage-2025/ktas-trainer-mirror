# <a id="MultiplayerInfrastructure_UI_UIDocumentInteractionPolicy"></a> Class UIDocumentInteractionPolicy

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

모든 UIDocument의 표시 및 포인터 히트테스트 규약입니다.
상속할 수 없는 NetworkBehaviour 기반 월드 UI도 이 정책을 직접 사용합니다.

```csharp
public static class UIDocumentInteractionPolicy
```

#### Inheritance

object ← 
[UIDocumentInteractionPolicy](MultiplayerInfrastructure.UI.UIDocumentInteractionPolicy.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_UIDocumentInteractionPolicy_Forget_UnityEngine_UIElements_UIDocument_"></a> Forget\(UIDocument\)

```csharp
public static void Forget(UIDocument document)
```

#### Parameters

`document` UIDocument

### <a id="MultiplayerInfrastructure_UI_UIDocumentInteractionPolicy_Refresh_UnityEngine_UIElements_UIDocument_"></a> Refresh\(UIDocument\)

상태를 가진 UIDocument의 root 재생성 및 동적 자식 추가 뒤에 정책을 재적용합니다.

```csharp
public static void Refresh(UIDocument document)
```

#### Parameters

`document` UIDocument

### <a id="MultiplayerInfrastructure_UI_UIDocumentInteractionPolicy_SetPickingEnabled_UnityEngine_UIElements_UIDocument_System_Boolean_"></a> SetPickingEnabled\(UIDocument, bool\)

```csharp
public static void SetPickingEnabled(UIDocument document, bool enabled)
```

#### Parameters

`document` UIDocument

`enabled` bool

### <a id="MultiplayerInfrastructure_UI_UIDocumentInteractionPolicy_SetSubtreePickingMode_UnityEngine_UIElements_VisualElement_UnityEngine_UIElements_PickingMode_"></a> SetSubtreePickingMode\(VisualElement, PickingMode\)

월드 표시처럼 영구적으로 비상호작용인 임의의 트리에 사용합니다.

```csharp
public static void SetSubtreePickingMode(VisualElement root, PickingMode mode)
```

#### Parameters

`root` VisualElement

`mode` PickingMode

### <a id="MultiplayerInfrastructure_UI_UIDocumentInteractionPolicy_SetVisible_UnityEngine_UIElements_UIDocument_System_Boolean_"></a> SetVisible\(UIDocument, bool\)

```csharp
public static void SetVisible(UIDocument document, bool visible)
```

#### Parameters

`document` UIDocument

`visible` bool

