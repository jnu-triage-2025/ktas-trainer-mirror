# <a id="MultiplayerInfrastructure_UI"></a> Namespace MultiplayerInfrastructure.UI

### Namespaces

 [MultiplayerInfrastructure.UI.Models](MultiplayerInfrastructure.UI.Models.md)

### Classes

 [ChatPanelElement](MultiplayerInfrastructure.UI.ChatPanelElement.md)

 [ChatUIController](MultiplayerInfrastructure.UI.ChatUIController.md)

 [InventoryUIView.CraftableRecipeDisplay](MultiplayerInfrastructure.UI.InventoryUIView.CraftableRecipeDisplay.md)

조합 패널에 표시할 레시피 1건.

 [CrosshairElement](MultiplayerInfrastructure.UI.CrosshairElement.md)

화면 중앙에 표시되는 크로스헤어 VisualElement입니다.
단순 점(dot) 형태로 렌더링됩니다.

 [CrosshairUIController](MultiplayerInfrastructure.UI.CrosshairUIController.md)

CrosshairUIController는 화면 중앙 크로스헤어 UI를 표시합니다.

레이캐스트는 PlayerController.Raycast에서 수행되며,
이 컨트롤러는 UI 요소기만 담당합니다.

레이캐스트 결과는 PlayerController의 PlayerController.RaycastHasHit,
PlayerController.RaycastHit, PlayerController.RaycastHitObject에서 접근합니다.

 [DatapackSelectionUIController](MultiplayerInfrastructure.UI.DatapackSelectionUIController.md)

Data-only controller for DatapackSelectionUI. Layout and visual styling live in
DatapackSelectionUI.uxml/.uss, matching the IntroScene UIDocument workflow.

 [DialogueElement](MultiplayerInfrastructure.UI.DialogueElement.md)

 [DialoguePanelUIController](MultiplayerInfrastructure.UI.DialoguePanelUIController.md)

시나리오 UI 패널을 제어하는 컨트롤러.
InteractableObjectHintUIController와 연동하여 시나리오 선택지를 표시합니다.

 [EntityOverheadLabelElement](MultiplayerInfrastructure.UI.EntityOverheadLabelElement.md)

엔티티 위에 띄우는 일반화된 오버헤드 라벨(뱃지) 비주얼 엘리먼트.

<p>
플레이어 이름표를 머리 위에 띄우듯, 임의의 월드 엔티티 위에 "색상 사각형 + 텍스트" 형태의 라벨을 표시한다.
색상 사각형과 텍스트 색상은 독립적으로 지정할 수 있어, 트리아지 등급(색상 + 명칭) 같은 도메인 표기에
재사용된다. 색상 사각형은 숨길 수 있어(NPC 이름표처럼) 텍스트만 표시할 수도 있다.
위치(화면 좌표) 갱신은 <xref href="MultiplayerInfrastructure.UI.EntityOverheadLabelUIController" data-throw-if-not-resolved="false"></xref> 가 담당한다.
</p>

 [EntityOverheadLabelUIController](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.md)

엔티티 위에 라벨(뱃지)을 띄우는 일반화된 오버헤드 라벨 컨트롤러.

<p>
플레이어 이름을 머리 위에 표기하듯, 임의의 월드 엔티티(Transform) 위에 "색상 사각형 + 텍스트" 라벨을
스크린 스페이스(UI Toolkit)로 표시한다. 도메인 코드는 <xref href="MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.SetLabel(UnityEngine.Transform%2cMultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent)" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.RemoveLabel(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 로
대상별 라벨을 등록/해제하기만 하면 되고, 월드→스크린 투영과 위치 갱신은 이 컨트롤러가 매 프레임 수행한다.
</p>

<p>
배치: 씬에 <xref href="UnityEngine.UIElements.UIDocument" data-throw-if-not-resolved="false"></xref> 와 함께 배치한다. 카메라는 지정되지 않으면 Camera.main 을 사용한다.
</p>

 [GameEscapeMenuUIController](MultiplayerInfrastructure.UI.GameEscapeMenuUIController.md)

 [GraphicsSettingsUIController](MultiplayerInfrastructure.UI.GraphicsSettingsUIController.md)

그래픽 설정 UI의 UIDocument 컨트롤러입니다.

역할:
  - <xref href="MultiplayerInfrastructure.UI.TextureQualityOptionElement" data-throw-if-not-resolved="false"></xref> 타일 4개를 동적으로 생성합니다.
  - 타일 클릭 시 선택 상태를 갱신하고 "적용 및 저장" 버튼 활성화를 조절합니다.
  - "적용 및 저장" 클릭 시 <xref href="MultiplayerInfrastructure.Performance.TexturePerformanceService" data-throw-if-not-resolved="false"></xref>를 통해 설정을 저장합니다.
  - <xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref>를 구현하여 <xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>으로 열고 닫습니다.

 [HotbarControl](MultiplayerInfrastructure.UI.HotbarControl.md)

 [HotbarUIController](MultiplayerInfrastructure.UI.HotbarUIController.md)

 [Icon](MultiplayerInfrastructure.UI.Icon.md)

UI에서 공통으로 사용하는 아이콘 오버레이입니다.

 [InteractableObjectHintList](MultiplayerInfrastructure.UI.InteractableObjectHintList.md)

 [InteractableObjectHintListElement](MultiplayerInfrastructure.UI.InteractableObjectHintListElement.md)

 [InteractableObjectHintUIController](MultiplayerInfrastructure.UI.InteractableObjectHintUIController.md)

InteractableObjectHintUIController는 InteractableObjectHintUI를 사용하는 데 필요한
컨트롤을 제공합니다. PlayerController등에서 이 컨트롤을 제어하는 것이 의도됩니다.

다이얼로그 모드를 지원하여, 대화 중에는 선택지만 표시하고
대화 종료 후 원래 상호작용 객체 목록을 복원합니다.

 [InventoryUIController](MultiplayerInfrastructure.UI.InventoryUIController.md)

MonoBehaviour controller that owns the view, handles overlay lifecycle, and is accessed via Registry.Registry.

 [InventoryUIView](MultiplayerInfrastructure.UI.InventoryUIView.md)

Pure view responsible for inventory slot visuals and interactions.
Intended to be instantiated from UXML; controller owns lifecycle and data binding.

 [ItemDurabilityBar](MultiplayerInfrastructure.UI.ItemDurabilityBar.md)

인벤토리 계열 슬롯에서 공통으로 사용하는 아이템 내구도 막대입니다.

 [ItemSubmissionUIController](MultiplayerInfrastructure.UI.ItemSubmissionUIController.md)

아이템 제출 패널의 MonoBehaviour 컨트롤러.

흐름:
 1) <xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable" data-throw-if-not-resolved="false"></xref> 가 <xref href="MultiplayerInfrastructure.UI.ItemSubmissionUIController.Open(MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable%2cMultiplayerInfrastructure.Player.PlayerController)" data-throw-if-not-resolved="false"></xref> 을 호출 → 요구 아이템으로 패널을 구성하고 오버레이로 push.
 2) 매 프레임 플레이어 인벤토리 보유량으로 요구 칸 표시/제출 버튼 활성 상태를 갱신.
 3) 제출 버튼 클릭 → 요구 아이템을 소모(제거)하고, Interactable 에 완료를 통지 → 서버 세션 전역 신호가 올라간다.

인벤토리는 소유자(owner) 클라이언트 로컬 권한이므로, 소모/검증은 소유자 클라이언트에서 수행하고
완료 신호만 서버 권한 경로(<xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref>)로 라우팅한다.

 [ItemSubmissionUIView](MultiplayerInfrastructure.UI.ItemSubmissionUIView.md)

아이템 제출 패널의 순수 뷰.

레이아웃:
 - 배경: 화면 전체를 덮는 반투명 딤(dim)으로 뒤 배경을 어둡게 처리(모달 느낌 강조)
 - 헤더: 제목(좌) + × 닫기 버튼(우) + 하단 구분선
 - 설명 문구: 무엇을 해야 하는지 짧게 안내
 - "필요 아이템" 섹션 라벨
 - 요구 아이템 그리드: 슬롯마다 아이콘(placeholder) + 아이템 이름 + 보유/요구 수량 세로 배치
   · 미충족 → 아이콘 흐림/회색, 파란 수량 텍스트, navy 테두리
   · 충족   → 아이콘 선명, 녹색 수량 텍스트, 녹색 테두리·배경, 살짝 확대(scale) 강조
 - 제출(녹색) / 취소(빨간) 버튼 행: hover/active 시 색상·scale 트랜지션

 [KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)

키 설정 항목 하나를 나타내는 데이터 클래스입니다.
actionId와 actionDisplayName으로 어떤 기능인지 식별하고,
boundKey로 현재 할당된 키코드를 저장합니다.

 [KeyBindingRepository](MultiplayerInfrastructure.UI.KeyBindingRepository.md)

키 바인딩 설정을 PlayerPrefs를 통해 로컬에 저장하고 불러오는 저장소입니다.

저장 키 형식: "KeyBinding_{actionId}" → int(KeyCode)

 [KeyConfigEntryElement](MultiplayerInfrastructure.UI.KeyConfigEntryElement.md)

키 설정 목록의 항목 하나를 나타내는 VisualElement입니다.
좌측에 기능 이름, 우측에 할당된 키 이름을 표시하며,
강조(Focused) 상태, 선택(Selected) 상태, 리바인딩 대기(Rebinding) 상태를 지원합니다.

 [KeyConfigUIController](MultiplayerInfrastructure.UI.KeyConfigUIController.md)

키 설정 UI의 UIDocument 컨트롤러입니다.

역할:
  - 좌측 ScrollView에 <xref href="MultiplayerInfrastructure.UI.KeyConfigEntryElement" data-throw-if-not-resolved="false"></xref> 목록을 동적으로 생성합니다.
  - 우측 <xref href="MultiplayerInfrastructure.UI.KeyboardLayoutElement" data-throw-if-not-resolved="false"></xref>에 현재 바인딩을 반영합니다.
  - 키보드 키 클릭 → 좌측 목록 스크롤 및 강조
  - 목록 항목 클릭 → 키보드에서 해당 키 강조

 [KeyboardLayoutElement](MultiplayerInfrastructure.UI.KeyboardLayoutElement.md)

키보드 레이아웃을 시각화하는 VisualElement입니다.
각 키를 흰색 배경 + 회색 테두리 VisualElement로 직접 그립니다.

레이어 구성:
  Layer 0 - 키 오버레이 (할당된 기능 레이블, 클릭 영역)
  Layer 1 - 키 이름 레이블 (Q, W, E … 등 작은 텍스트)

 [LoadingScreen](MultiplayerInfrastructure.UI.LoadingScreen.md)

씬과 카메라의 수명과 무관하게 표시되는 전역 로딩 화면입니다.
여러 비동기 작업이 겹쳐도 마지막 작업이 끝날 때까지 화면을 유지합니다.

 [OverflowScrollView](MultiplayerInfrastructure.UI.OverflowScrollView.md)

오버플로우가 생길 때만 <xref href="MultiplayerInfrastructure.UI.ReusableVerticalScrollbar" data-throw-if-not-resolved="false"></xref>를 표시하는 ScrollView 래퍼.

 [ProblemChoiceElement](MultiplayerInfrastructure.UI.ProblemChoiceElement.md)

 [ProblemPromptElement](MultiplayerInfrastructure.UI.ProblemPromptElement.md)

 [ProblemSheetElement](MultiplayerInfrastructure.UI.ProblemSheetElement.md)

 [ProblemSheetUIController](MultiplayerInfrastructure.UI.ProblemSheetUIController.md)

 [ProblemShortAnswerElement](MultiplayerInfrastructure.UI.ProblemShortAnswerElement.md)

 [QuestPanelElement](MultiplayerInfrastructure.UI.QuestPanelElement.md)

임무(퀘스트) 저널 UI.

레이아웃:
 - 배경: 화면 전체를 덮는 반투명 딤(dim)
 - 헤더: 제목 + 추적 수(좌) / 필터 탭(중앙) / × 닫기(우)
 - 좌측 목록: 분류(추적 중 · 진행 중 · 완료)별로 묶인 임무 항목.
   항목마다 마름모 마커 + 제목 + 현재 목표 요약 + 상태 배지를 표시하고,
   선택한 항목은 강조 테두리로 구분한다.
 - 우측 상세: 선택한 임무의 제목 → 현재 목표 → 설명 → 세부 목표 →
   정보 타일(진행도·세부 목표·상태·범위) → 고지 배너 → 추적 토글 버튼.

스타일은 QuestPanelUI.uss 에 정의한다(팔레트는 ItemSubmissionUI / InventoryUI 와 동일).

 [QuestPreviewHudElement](MultiplayerInfrastructure.UI.QuestPreviewHudElement.md)

 [QuestPreviewHudUIController](MultiplayerInfrastructure.UI.QuestPreviewHudUIController.md)

 [QuestUIController](MultiplayerInfrastructure.UI.QuestUIController.md)

 [ReusableVerticalScrollbar](MultiplayerInfrastructure.UI.ReusableVerticalScrollbar.md)

ScrollView와 함께 사용할 수 있는 경량 세로 스크롤바입니다.
호출자가 현재 오프셋/콘텐츠 크기를 공급하고, 사용자의 이동 요청을 받아 실제 ScrollView에 반영합니다.

 [ScenarioSelectionInteractable](MultiplayerInfrastructure.UI.ScenarioSelectionInteractable.md)

시나리오 선택지를 IInteract로 래핑하는 클래스.
InteractableObjectHintUIController에서 표시할 수 있도록 합니다.

 [SceneUIIntroSceneController](MultiplayerInfrastructure.UI.SceneUIIntroSceneController.md)

 [SettingsUIController](MultiplayerInfrastructure.UI.SettingsUIController.md)

통합 설정 창의 그래픽 프로파일 및 세부 옵션 탭입니다.

 [TextureQualityOptionElement](MultiplayerInfrastructure.UI.TextureQualityOptionElement.md)

그래픽 설정 UI에서 텍스처 품질 단계 하나를 나타내는 VisualElement입니다.

각 옵션(Ultra / High / Medium / Low)마다 하나씩 생성되며,
클릭 시 <xref href="MultiplayerInfrastructure.UI.TextureQualityOptionElement.OnOptionSelected" data-throw-if-not-resolved="false"></xref> 이벤트로 선택된 품질을 상위에 알립니다.
<xref href="MultiplayerInfrastructure.UI.TextureQualityOptionElement.SetActive(System.Boolean)" data-throw-if-not-resolved="false"></xref>로 현재 선택된 항목임을 강조합니다.

 [TimeDisplayElement](MultiplayerInfrastructure.UI.TimeDisplayElement.md)

시간 표시(스톱워치/카운트다운) HUD 의 시각 요소.

화면 가로 중앙, 세로 top 정렬로 hh:mm:ss 를 표기한다.
정방향(스톱워치)이면 증가, 역방향(카운트다운)이면 감소하는 값을
<xref href="MultiplayerInfrastructure.UI.TimeDisplayUIController" data-throw-if-not-resolved="false"></xref> 가 매 프레임 주입한다.

비차단 HUD 이므로 pickingMode 는 Ignore 로 두어 하위 UI 클릭을 가로채지 않는다.
(<xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref> 는 모달 전용이므로 사용하지 않는다.)

 [TimeDisplayUIController](MultiplayerInfrastructure.UI.TimeDisplayUIController.md)

시간 표시(스톱워치/카운트다운) HUD 컨트롤러.

화면 상단 중앙에 hh:mm:ss 를 표기한다. 값의 진행/방향/표시 여부는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState" data-throw-if-not-resolved="false"></xref> 정적 저장소에서 매 프레임 읽어 렌더한다.
저장소는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref> 가 서버 권한으로 동기화하므로
모든 클라이언트가 동일한 값을 본다.

비차단 HUD 이므로 <xref href="MultiplayerInfrastructure.UI.UIOverlayStack" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.UI.IUIOverlay" data-throw-if-not-resolved="false"></xref> 를 사용하지 않는다.

UI 구성은 코드 전용이다: 빈 UIDocument(+ PanelSettings)에 <xref href="MultiplayerInfrastructure.UI.TimeDisplayElement" data-throw-if-not-resolved="false"></xref> 를
C# 으로 생성해 부착하며, 시각 스타일은 요소의 인라인 스타일에서 관리한다(uxml/uss/StyleSheet 불필요).

 [TitleUIController](MultiplayerInfrastructure.UI.TitleUIController.md)

 [TitleUIElement](MultiplayerInfrastructure.UI.TitleUIElement.md)

 [UIControllerABC](MultiplayerInfrastructure.UI.UIControllerABC.md)

UIControllerABC는 모든 UI 컨트롤러의 상속이 의도되는 추상 메서드입니다. 각 UI 요소들이 싱글톤 패턴이
의도되지 않았으므로, 외부에서 UI 컨트롤을 취득하는 데 있어서 별개의 레지스트리로부터 접근할 수 있도록 하고 있습니다.

UIControllerABC는 레지스트리를 통해 외부에서 접근 가능하도록 이 컨트롤을 레지스터하는 역할을 합니다.
새 일반 UI 컨트롤러는 MonoBehaviour를 직접 상속하지 말고 이 클래스를 상속해야 합니다.

 [UIDocumentControllerABC](MultiplayerInfrastructure.UI.UIDocumentControllerABC.md)

UIDocument를 제어하는 모든 MonoBehaviour 기반 UI의 공통 부모입니다.
표시 상태와 포인터 히트테스트 상태를 항상 함께 관리합니다.
새 UIDocument 기반 UI 컨트롤러는 반드시 이 클래스를 직접 또는 UIControllerABC를 통해 상속해야 합니다.
NetworkBehaviour 등 단일 상속 때문에 불가능한 경우에는 UIDocumentInteractionPolicy를 사용해야 합니다.

 [UIDocumentInteractionPolicy](MultiplayerInfrastructure.UI.UIDocumentInteractionPolicy.md)

모든 UIDocument의 표시 및 포인터 히트테스트 규약입니다.
상속할 수 없는 NetworkBehaviour 기반 월드 UI도 이 정책을 직접 사용합니다.

 [UIDocumentInteractionStateGuard](MultiplayerInfrastructure.UI.UIDocumentInteractionStateGuard.md)

동적 VisualElement와 UIDocument root 재생성 뒤에도 입력 정책을 유지합니다.

 [UIOverlayStack](MultiplayerInfrastructure.UI.UIOverlayStack.md)

 [UIRuntimeEventSystemGuard](MultiplayerInfrastructure.UI.UIRuntimeEventSystemGuard.md)

 [UIScaleOptionElement](MultiplayerInfrastructure.UI.UIScaleOptionElement.md)

그래픽 설정 UI에서 UI 배율 단계 하나를 나타내는 VisualElement입니다.

각 단계(1~4)마다 하나씩 생성되며,
클릭 시 <xref href="MultiplayerInfrastructure.UI.UIScaleOptionElement.OnOptionSelected" data-throw-if-not-resolved="false"></xref> 이벤트로 선택된 배율을 상위에 알립니다.
<xref href="MultiplayerInfrastructure.UI.UIScaleOptionElement.SetActive(System.Boolean)" data-throw-if-not-resolved="false"></xref>로 현재 선택된 항목임을 강조합니다.

 [HotbarControl.UxmlSerializedData](MultiplayerInfrastructure.UI.HotbarControl.UxmlSerializedData.md)

 [ProblemChoiceElement.UxmlSerializedData](MultiplayerInfrastructure.UI.ProblemChoiceElement.UxmlSerializedData.md)

 [KeyboardLayoutElement.UxmlSerializedData](MultiplayerInfrastructure.UI.KeyboardLayoutElement.UxmlSerializedData.md)

 [KeyConfigEntryElement.UxmlSerializedData](MultiplayerInfrastructure.UI.KeyConfigEntryElement.UxmlSerializedData.md)

 [ChatPanelElement.UxmlSerializedData](MultiplayerInfrastructure.UI.ChatPanelElement.UxmlSerializedData.md)

 [InventoryUIView.UxmlSerializedData](MultiplayerInfrastructure.UI.InventoryUIView.UxmlSerializedData.md)

 [CrosshairElement.UxmlSerializedData](MultiplayerInfrastructure.UI.CrosshairElement.UxmlSerializedData.md)

 [DialogueElement.UxmlSerializedData](MultiplayerInfrastructure.UI.DialogueElement.UxmlSerializedData.md)

 [InteractableObjectHintListElement.UxmlSerializedData](MultiplayerInfrastructure.UI.InteractableObjectHintListElement.UxmlSerializedData.md)

 [InteractableObjectHintList.UxmlSerializedData](MultiplayerInfrastructure.UI.InteractableObjectHintList.UxmlSerializedData.md)

 [ItemSubmissionUIView.UxmlSerializedData](MultiplayerInfrastructure.UI.ItemSubmissionUIView.UxmlSerializedData.md)

 [ProblemPromptElement.UxmlSerializedData](MultiplayerInfrastructure.UI.ProblemPromptElement.UxmlSerializedData.md)

 [ProblemShortAnswerElement.UxmlSerializedData](MultiplayerInfrastructure.UI.ProblemShortAnswerElement.UxmlSerializedData.md)

 [QuestPanelElement.UxmlSerializedData](MultiplayerInfrastructure.UI.QuestPanelElement.UxmlSerializedData.md)

 [QuestPreviewHudElement.UxmlSerializedData](MultiplayerInfrastructure.UI.QuestPreviewHudElement.UxmlSerializedData.md)

 [TextureQualityOptionElement.UxmlSerializedData](MultiplayerInfrastructure.UI.TextureQualityOptionElement.UxmlSerializedData.md)

 [TimeDisplayElement.UxmlSerializedData](MultiplayerInfrastructure.UI.TimeDisplayElement.UxmlSerializedData.md)

 [TitleUIElement.UxmlSerializedData](MultiplayerInfrastructure.UI.TitleUIElement.UxmlSerializedData.md)

 [ProblemSheetElement.UxmlSerializedData](MultiplayerInfrastructure.UI.ProblemSheetElement.UxmlSerializedData.md)

 [UIScaleOptionElement.UxmlSerializedData](MultiplayerInfrastructure.UI.UIScaleOptionElement.UxmlSerializedData.md)

### Structs

 [InventoryUIView.CraftableRecipeDisplay.Ingredient](MultiplayerInfrastructure.UI.InventoryUIView.CraftableRecipeDisplay.Ingredient.md)

 [EntityOverheadLabelUIController.LabelContent](MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent.md)

단일 라벨의 표시 내용.

### Interfaces

 [IOverheadPresentationAnchorProvider](MultiplayerInfrastructure.UI.IOverheadPresentationAnchorProvider.md)

 [IUIOverlay](MultiplayerInfrastructure.UI.IUIOverlay.md)

### Enums

 [InteractableHintUIMode](MultiplayerInfrastructure.UI.InteractableHintUIMode.md)

 [SettingsUIController.SettingsTab](MultiplayerInfrastructure.UI.SettingsUIController.SettingsTab.md)

