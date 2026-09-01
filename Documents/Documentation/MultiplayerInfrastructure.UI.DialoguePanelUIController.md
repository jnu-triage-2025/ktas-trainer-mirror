# <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController"></a> Class DialoguePanelUIController

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 UI 패널을 제어하는 컨트롤러.
InteractableObjectHintUIController와 연동하여 시나리오 선택지를 표시합니다.

```csharp
[RequireComponent(typeof(UIDocument))]
public class DialoguePanelUIController : UIControllerABC, IUIOverlay
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md) ← 
[UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md) ← 
[DialoguePanelUIController](MultiplayerInfrastructure.UI.DialoguePanelUIController.md)

#### Implements

[IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

#### Inherited Members

[UIControllerABC.Awake\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_Awake), 
[UIControllerABC.OnDestroy\(\)](MultiplayerInfrastructure.UI.UIControllerABC.md\#MultiplayerInfrastructure\_UI\_UIControllerABC\_OnDestroy), 
[UIDocumentControllerABC.SetDocumentRootInteractable\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootInteractable\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentRootPickingEnabled\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentRootPickingEnabled\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.SetDocumentVisible\(UIDocument, bool\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetDocumentVisible\_UnityEngine\_UIElements\_UIDocument\_System\_Boolean\_), 
[UIDocumentControllerABC.NeutralizeDocumentRootWhenReady\(UIDocument\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_NeutralizeDocumentRootWhenReady\_UnityEngine\_UIElements\_UIDocument\_), 
[UIDocumentControllerABC.SetSubtreePickingMode\(VisualElement, PickingMode\)](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md\#MultiplayerInfrastructure\_UI\_UIDocumentControllerABC\_SetSubtreePickingMode\_UnityEngine\_UIElements\_VisualElement\_UnityEngine\_UIElements\_PickingMode\_)

## Fields

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnAdvanceRequested"></a> OnAdvanceRequested

다음 진행이 요청되었을 때 발생 (선택지 없이 진행할 때)

```csharp
public UnityEvent OnAdvanceRequested
```

#### Field Value

 UnityEvent

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnNodeDisplayed"></a> OnNodeDisplayed

```csharp
public UnityEvent<IScenarioNode> OnNodeDisplayed
```

#### Field Value

 UnityEvent<[IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)\>

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnScenarioEnded"></a> OnScenarioEnded

```csharp
public UnityEvent OnScenarioEnded
```

#### Field Value

 UnityEvent

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnScenarioStarted"></a> OnScenarioStarted

```csharp
public UnityEvent OnScenarioStarted
```

#### Field Value

 UnityEvent

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnSelectionMade"></a> OnSelectionMade

```csharp
public UnityEvent<int> OnSelectionMade
```

#### Field Value

 UnityEvent<int\>

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnTypingCompleted"></a> OnTypingCompleted

타이핑이 완료되었을 때 발생

```csharp
public UnityEvent OnTypingCompleted
```

#### Field Value

 UnityEvent

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_PlayerNamePlaceholder"></a> PlayerNamePlaceholder

시나리오 대화의 speakerName / dialogueContent에서 플레이어 이름으로 치환되는
이스케이프 워드입니다.

JSON 시나리오 작성 예:

<pre><code class="lang-csharp">{ "speakerName": "{PLAYER_NAME}", "dialogueContent": "안녕하세요, {PLAYER_NAME}씨!" }</code></pre>

```csharp
public const string PlayerNamePlaceholder = "{PLAYER_NAME}"
```

#### Field Value

 string

## Properties

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_CurrentDialogueOwner"></a> CurrentDialogueOwner

현재 대화창 계열 UI를 점유 중인 그래프 식별자(없으면 null).

```csharp
public string CurrentDialogueOwner { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_HasActiveSelections"></a> HasActiveSelections

선택지가 표시되어 있는지 여부

```csharp
public bool HasActiveSelections { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_HasConsumedInputThisFrame"></a> HasConsumedInputThisFrame

이번 프레임에 대화 진행/선택 입력을 이미 소비했는지 여부.
선택지를 확정하면 대화창이 곧바로 오버레이 스택에서 빠지기 때문에,
같은 프레임의 입력이 월드 상호작용으로 이어지는 것을 이 값으로 차단한다.

```csharp
public bool HasConsumedInputThisFrame { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_IsScenarioActive"></a> IsScenarioActive

시나리오가 진행 중인지 여부

```csharp
public bool IsScenarioActive { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_IsTyping"></a> IsTyping

현재 텍스트 타이핑 중인지 여부

```csharp
public bool IsTyping { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_IsWaitingForInput"></a> IsWaitingForInput

입력 대기 중인지 여부

```csharp
public bool IsWaitingForInput { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_Awake"></a> Awake\(\)

```csharp
protected override void Awake()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_ClearDialogueOwner"></a> ClearDialogueOwner\(\)

대화창 점유자를 해제한다(시나리오 종료 시).

```csharp
public void ClearDialogueOwner()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_CompleteTyping"></a> CompleteTyping\(\)

타이핑을 완료합니다. (SkipTyping 별칭)

```csharp
public void CompleteTyping()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_DismissDialogue"></a> DismissDialogue\(\)

```csharp
public void DismissDialogue()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_DismissPresentationNode"></a> DismissPresentationNode\(\)

표시 전용 역할 브랜치의 현재 노드 UI만 닫고 시나리오 프레젠테이션 연결은 유지한다.
다음 TargetRpc 노드가 같은 컨트롤러를 다시 사용할 수 있다.

```csharp
public void DismissPresentationNode()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_DisplayChoice_System_String_System_String_System_String_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Scenario_ScenarioChoiceOption__"></a> DisplayChoice\(string, string, string, IReadOnlyList<ScenarioChoiceOption\>\)

선택지 표시

```csharp
public void DisplayChoice(string speakerName, string dialogueContent, string portraitIdentifier, IReadOnlyList<ScenarioChoiceOption> options)
```

#### Parameters

`speakerName` string

`dialogueContent` string

`portraitIdentifier` string

`options` IReadOnlyList<[ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md)\>

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_DisplayDialogue_System_String_System_String_System_String_"></a> DisplayDialogue\(string, string, string\)

대화 노드 표시

```csharp
public void DisplayDialogue(string speakerName, string dialogueContent, string portraitIdentifier)
```

#### Parameters

`speakerName` string

`dialogueContent` string

`portraitIdentifier` string

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_DisplayDialogue_System_String_System_String_System_String_System_Boolean_"></a> DisplayDialogue\(string, string, string, bool\)

```csharp
public void DisplayDialogue(string speakerName, string dialogueContent, string portraitIdentifier, bool interactionRequired)
```

#### Parameters

`speakerName` string

`dialogueContent` string

`portraitIdentifier` string

`interactionRequired` bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_DisplayDisinteractableDialogue_System_String_System_String_System_String_"></a> DisplayDisinteractableDialogue\(string, string, string\)

입력과 overlay를 점유하지 않고 대화 UI에 전체 텍스트를 즉시 표시한다.

```csharp
public void DisplayDisinteractableDialogue(string speakerName, string dialogueContent, string portraitIdentifier)
```

#### Parameters

`speakerName` string

`dialogueContent` string

`portraitIdentifier` string

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_EndScenario"></a> EndScenario\(\)

시나리오 종료

```csharp
public void EndScenario()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_EnsureOverlayActive"></a> EnsureOverlayActive\(\)

```csharp
public void EnsureOverlayActive()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_Hide"></a> Hide\(\)

패널 숨김 (별칭)

```csharp
public void Hide()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_HideDisinteractableDialogue"></a> HideDisinteractableDialogue\(\)

```csharp
public void HideDisinteractableDialogue()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_HidePanel"></a> HidePanel\(\)

패널 숨김

```csharp
public void HidePanel()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_IsDialogueOwnedByOther_System_String_"></a> IsDialogueOwnedByOther\(string\)

<code class="paramref">owningGraphIdentifier</code> 이외의 다른 흐름이 대화창을 점유 중이면 true.
점유자가 없거나 동일 식별자이면 false.

```csharp
public bool IsDialogueOwnedByOther(string owningGraphIdentifier)
```

#### Parameters

`owningGraphIdentifier` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_MarkDialogueOwner_System_String_"></a> MarkDialogueOwner\(string\)

대화창 점유자를 등록/갱신한다.

```csharp
public void MarkDialogueOwner(string owningGraphIdentifier)
```

#### Parameters

`owningGraphIdentifier` string

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnOverlayPopped"></a> OnOverlayPopped\(\)

```csharp
public void OnOverlayPopped()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OnOverlayPushed"></a> OnOverlayPushed\(\)

```csharp
public void OnOverlayPushed()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_SelectOption_System_Int32_"></a> SelectOption\(int\)

특정 인덱스의 선택지를 선택합니다.

```csharp
public void SelectOption(int index)
```

#### Parameters

`index` int

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_SetDisinteractableDialogueOpacity_System_Single_"></a> SetDisinteractableDialogueOpacity\(float\)

```csharp
public void SetDisinteractableDialogueOpacity(float opacity)
```

#### Parameters

`opacity` float

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_SetInteractableHintUI_MultiplayerInfrastructure_UI_InteractableObjectHintUIController_"></a> SetInteractableHintUI\(InteractableObjectHintUIController\)

InteractableHintUI 컨트롤러를 설정합니다.
ScenarioController에서 호출됩니다.

```csharp
public void SetInteractableHintUI(InteractableObjectHintUIController hintUI)
```

#### Parameters

`hintUI` [InteractableObjectHintUIController](MultiplayerInfrastructure.UI.InteractableObjectHintUIController.md)

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_SetWaitingForInput_System_Boolean_"></a> SetWaitingForInput\(bool\)

입력 대기 상태 설정

```csharp
public void SetWaitingForInput(bool waiting)
```

#### Parameters

`waiting` bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_Show"></a> Show\(\)

패널 표시 (별칭)

```csharp
public void Show()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_ShowPanel"></a> ShowPanel\(\)

패널 표시

```csharp
public void ShowPanel()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_SkipTyping"></a> SkipTyping\(\)

타이핑을 스킵하고 전체 텍스트를 즉시 표시합니다.

```csharp
public void SkipTyping()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_StartScenario_MultiplayerInfrastructure_Scenario_ScenarioController_"></a> StartScenario\(ScenarioController\)

시나리오 시작

```csharp
public void StartScenario(ScenarioController controller)
```

#### Parameters

`controller` [ScenarioController](MultiplayerInfrastructure.Scenario.ScenarioController.md)

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_TogglePanel"></a> TogglePanel\(\)

패널 토글

```csharp
public void TogglePanel()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_TryPresentTransientDialogue_System_String_System_String_System_Single_System_String_System_Action_"></a> TryPresentTransientDialogue\(string, string, float, string, Action\)

시나리오 그래프와 무관한 짧은 안내 대화를 안전하게 표시한다.
기존 Dialogue/Choice가 UI를 점유 중이면 요청을 큐에 보관해 종료 후 재생하므로,
시나리오 대화의 입력 상태·선택지·오버레이를 덮어쓰지 않는다.

```csharp
public bool TryPresentTransientDialogue(string speakerName, string dialogueContent, float duration = 3, string portraitIdentifier = null, Action onFinished = null)
```

#### Parameters

`speakerName` string

`dialogueContent` string

`duration` float

`portraitIdentifier` string

`onFinished` Action

#### Returns

 bool

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_TrySelectCurrentOption"></a> TrySelectCurrentOption\(\)

현재 선택된 옵션을 선택합니다.
ScenarioController에서 호출됩니다.

```csharp
public void TrySelectCurrentOption()
```

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OverlayPopped"></a> OverlayPopped

```csharp
public event Action OverlayPopped
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_DialoguePanelUIController_OverlayPushed"></a> OverlayPushed

```csharp
public event Action OverlayPushed
```

#### Event Type

 Action

