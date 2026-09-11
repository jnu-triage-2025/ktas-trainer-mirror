using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private ChatUIController _chatUI;

    [Header("Key Configuration")]
    [SerializeField] private KeyCode _keyMovingRunning = KeyCode.LeftControl;
    [SerializeField] private KeyCode _keyToggleInventory = KeyCode.E;
    [SerializeField] private KeyCode _keySwitchCameraViewMode = KeyCode.K;
    [SerializeField] private KeyCode _keyOpenEscMenu = KeyCode.Escape;
    [SerializeField] private KeyCode _keyToggleChat = DefaultsKeyConfiguration.OpenChatUI;
    [SerializeField] private KeyCode _keyToggleCommand = DefaultsKeyConfiguration.OpenChatUIWithCommand;
    [SerializeField] private KeyCode _keyInteractInteractableObject = DefaultsKeyConfiguration.InteractInteractableObject;
    [Tooltip("대화/선택지를 다음으로 넘기거나 타이핑을 스킵하는 키.")]
    [SerializeField] private KeyCode _keyAdvanceDialogue = KeyCode.Space;
    [Tooltip("대화/선택지 확정용 보조 키. 상호작용 키·스페이스바·마우스 좌클릭과 동일하게 동작합니다.")]
    [SerializeField] private KeyCode _keyConfirmDialogue = KeyCode.Return;
    [SerializeField] private KeyCode _keyEscape = KeyCode.Escape;
    [SerializeField] private KeyCode _keySpectatorFlyDown = KeyCode.LeftShift;
    [SerializeField] private KeyCode _keyOpenQuestUI = DefaultsKeyConfiguration.OpenQuestUI;
    [SerializeField] private KeyCode _keyDropHeldItem = DefaultsKeyConfiguration.DropHeldItem;

    private const string CameraDistanceModifierActionId = "camera_distance_modifier";
    private const KeyCode DefaultCameraDistanceModifier = KeyCode.LeftAlt;

    // 연속된 휠 입력을 하나의 POV 조정으로 묶는다. 프레임마다 즉시 저장하면
    // PlayerPrefs 디스크 쓰기가 과도하게 발생하고, 1/3인칭 전환 여부도 올바르게 판단할 수 없다.
    private const float CameraDistanceGestureEndDelay = 0.15f;
    private bool _cameraDistanceGestureActive;
    private bool _cameraDistanceGestureChangedViewMode;
    private float _cameraDistanceGestureStartDistance;
    private float _cameraDistanceGestureLastScrollTime;

    private void Start_Input()
    {
      _chatUI = Registry.Registry.Get<ChatUIController>(RegistryType.UI, Registry.Registry.TypeKey<ChatUIController>());
      EnsureEscapeMenuController();
    }

    public void Update_Input()
    {
      // 오버레이가 열린 동안 게임 모드 변경 등에서 이동/커서 상태를 덮어써도 다음 프레임에 복구한다.
      EnsureOverlayDrivenPlayerState();

      // if (Input.GetKeyDown(KeyCode.F)) Debug.Log($"[PlayerController] F key pressed. IsOwner: {IsOwner}, IsClient: {IsClientInitialized}, IsServer: {IsServerInitialized}");

      // Update_Movement보다 먼저, 그리고 아래의 어떤 early return보다도 먼저 갱신해야 한다.
      UpdateJumpInputSuppression();
      FinalizeCameraDistanceGestureIfNeeded(HasCameraDistanceScrollInput());

      HandleChatInput();
      var escapeConsumed = HandleEscape();
      HandleDialogueInput();
      HandleIntravenousLineConnectionModeExit();

      if (HandleToggleInventory())
        return;

      if (HandleOpenQuestUIInput())
        return;

      // 인벤토리가 열려 있는 동안에는 핫바 숫자 키가 선택 변경 대신 슬롯 교환으로 동작한다.
      if (HandleInventoryHotbarSwapInput())
        return;

      if (!UIOverlayStack.IsEmpty())
        return;

      // 대화/선택지가 이번 프레임 입력을 이미 소비했다면 여기서 멈춘다.
      // 선택 확정·대화 진행은 그 즉시 대화창을 오버레이 스택에서 pop 하므로, 위의
      // IsEmpty() 검사만으로는 같은 프레임의 F/스페이스/엔터/좌클릭이 그대로 아래로 흘러
      // 주변 Interactable 상호작용이나 아이템 사용을 한 번 더 발동시킨다.
      if (IsDialogueInputConsumedThisFrame())
        return;

      if (!escapeConsumed)
        HandleEscapeMenuInput();

      if (IsSpectator)
      {
        HandleSpectatorInput();
        return;
      }

      HandleInteractInteractableObject();
      HandleHotbarControlInput();
      HandleSwitchCameraViewMode();
      HandleItemActionInput();
    }

    private bool HandleEscape()
    {
      if (!Input.GetKeyDown(_keyEscape))
        return false;
      if (UIOverlayStack.IsEmpty())
        return false;

      // 대화는 Escape로 취소해도 시나리오 진행 결과를 제출하지 않는다. 여기서 Pop 하면
      // 대화 UI만 사라진 채 시나리오가 다음 입력을 기다리는 불일치 상태가 된다.
      if (!_dialoguePanelUIController.IsUnityNull() && UIOverlayStack.IsTop(_dialoguePanelUIController))
      {
        // 다만 표시 중인 대화/선택지가 없는데 스택에만 남아 있는 항목은 입력을 막을 이유가 없다.
        // 그대로 두면 진행 키도 Escape 도 통하지 않는 잠금이 되므로 Escape 로 걷어낼 수 있게 한다.
        if (!_dialoguePanelUIController.IsPresenting)
          UIOverlayStack.Remove(_dialoguePanelUIController);
        return true;
      }

      UIOverlayStack.Pop();
      return true;
    }

    /// <summary>
    /// 인벤토리를 인벤토리 토글 키를 이용해 열거나 닫으려고 시도할 때의 처리를 대응합니다.
    /// 
    /// 이 메서드는 몇 개 로직이 중첩되어있습니다:
    /// 이 메서드는 인벤토리 UI 표시에 변화가 있을 때 true를 반환합니다.
    /// 이것은 Update_Input 메서드에서 인벤토리 토글 메서드를 재활용할 수 있게 하기 위함입니다.
    /// 다른 비슷한 유형의 메서드와의 일관성을 떨어뜨리고, 코드 이해에 혼란을 줄 수 있으므로
    /// Open/Close 메서드를 분리하여 따로 대응할 것인지는 이후에 고려해야 합니다.
    /// </summary>
    /// <returns>bool 인벤토리 UI 표시에 변화가 있는가?</returns>
    private bool HandleToggleInventory()
    {
      if (_inventoryUI == null)
        return false;

      if (Input.GetKeyDown(_keyToggleInventory))
      {
        if (UIOverlayStack.IsTop(_inventoryUI))
        {
          UIOverlayStack.Pop();
          return true;
        }

        if (UIOverlayStack.IsEmpty())
        {
          UIOverlayStack.Push(_inventoryUI);
          return true;
        }
      }

      return false;
    }

    private void HandleSwitchCameraViewMode()
    {
      if (Input.GetKeyDown(_keySwitchCameraViewMode))
        SwitchCameraViewMode();
    }

    private void HandleInteractInteractableObject()
    {
      // 대화 진행은 HandleDialogueInput 에서만 처리한다.
      // 같은 프레임에 TrySelectCurrentOption 이 두 번 호출되지 않도록 여기서 막는다.
      if (!_dialoguePanelUIController.IsUnityNull() && UIOverlayStack.IsTop(_dialoguePanelUIController))
      {
        HandleInteractablesSelectionInput();
        return;
      }

      if (Input.GetKeyDown(_keyInteractInteractableObject))
        TryInteractWithSelection();
      HandleInteractablesSelectionInput();
    }

    private void HandleSpectatorInput()
    {
      HandleSpectatorFollowInput();
      HandleSwitchCameraViewMode();
    }

    private void HandleSpectatorFollowInput()
    {
      if (Input.GetMouseButtonDown(1))
        TryStartSpectateFollowUnderCursor();

      if (_isSpectateFollowing && Input.GetKeyDown(_keySpectatorFlyDown))
        StopSpectateFollow();
    }

    private void TryStartSpectateFollowUnderCursor()
    {
      var cam = UnityEngine.Camera.main;
      if (cam == null)
        return;

      Ray ray = cam.ScreenPointToRay(Input.mousePosition);
      if (!Physics.Raycast(ray, out RaycastHit hit, 150f))
        return;
      if (hit.collider == null)
        return;

      var target = hit.collider.GetComponentInParent<PlayerController>();
      if (target == null || target == this)
        return;

      BeginSpectateFollow(target);
    }

    private void HandleChatInput()
    {
      // 스폰 시점에 채팅 UI가 아직 등록되지 않았을 수 있다(표시 전용 클라이언트의 늦은 오버레이 준비).
      // 한 번 비어 있다고 영영 포기하면 그 클라이언트는 채팅과 커맨드를 끝내 열 수 없다.
      if (_chatUI.IsUnityNull())
        _chatUI = Registry.Registry.Get<ChatUIController>(RegistryType.UI, Registry.Registry.TypeKey<ChatUIController>());
      if (_chatUI.IsUnityNull())
        return;

      bool hasOtherOverlay = !UIOverlayStack.IsEmpty() && !UIOverlayStack.IsTop(_chatUI);
      bool hasDialogueOverlayTop = !_dialoguePanelUIController.IsUnityNull() && UIOverlayStack.IsTop(_dialoguePanelUIController);

      if (Input.GetKeyDown(_keyToggleChat))
      {
        if (hasOtherOverlay)
          return;

        _chatUI.Open();
        return;
      }

      if (Input.GetKeyDown(_keyToggleCommand))
      {
        // 테스트/운영 커맨드 입력은 대화창이 최상단일 때도 열 수 있게 허용한다.
        // (Validator 게이트 대기 중 시그널 주입·조회 목적)
        if (hasOtherOverlay && !hasDialogueOverlayTop)
          return;

        _chatUI.OpenWithCommandStart();
        return;
      }

      if (!_chatUI.IsOpen)
        return;

      if (Input.GetKeyDown(DefaultsKeyConfiguration.SendChat))
      {
        _chatUI.HandleSubmitKey();
        return;
      }

      if (Input.GetKeyDown(DefaultsKeyConfiguration.CloseChatUI))
      {
        _chatUI.HandleCancelKey();
        return;
      }

      if (Input.GetKeyDown(KeyCode.Tab))
      {
        _chatUI.HandleTabKey();
        return;
      }

      if (Input.GetKeyDown(KeyCode.UpArrow))
      {
        _chatUI.HandleHistoryPreviousKey();
        return;
      }

      if (Input.GetKeyDown(KeyCode.DownArrow))
      {
        _chatUI.HandleHistoryNextKey();
      }
    }

    private void HandleEscapeMenuInput()
    {
      if (Input.GetKeyDown(_keyOpenEscMenu))
      {
        EnsureEscapeMenuController();
        if (_escapeMenuUIController.IsUnityNull())
          return;

        if (UIOverlayStack.IsTop(_escapeMenuUIController))
          UIOverlayStack.Pop();
        else
          if (UIOverlayStack.IsEmpty())
          UIOverlayStack.Push(_escapeMenuUIController);
      }
    }

    private void HandleHotbarControlInput()
    {
      // 숫자 키로 선택
      HandleHotbarInputNumkey();

      // 수정자 키를 누른 채 휠을 굴리면 핫바 선택 대신 카메라 거리(POV)를 조정한다.
      if (IsCameraDistanceModifierHeld())
      {
        HandleCameraDistanceInput();
        return;
      }

      if (_detector.IsUnityNull())
        return;
      // 마우스 휠로 선택
      if (_detector.InteractableNearbyExists)
        return;
      HandleHotbarInputMouseWheel();
    }

    private void HandleCameraDistanceInput()
    {
      if (_camControl.IsUnityNull())
        return;

      float scroll = Input.mouseScrollDelta.y;
      if (Mathf.Abs(scroll) <= Mathf.Epsilon)
        return;

      if (!_cameraDistanceGestureActive)
      {
        _cameraDistanceGestureActive = true;
        _cameraDistanceGestureChangedViewMode = false;
        _cameraDistanceGestureStartDistance = _camControl.DesiredThirdPersonDistance;
      }

      // 휠을 위로(scroll > 0) 굴리면 카메라를 가깝게, 아래로 굴리면 멀게 한다.
      var viewModeBeforeAdjustment = _camControl.CurrentViewMode;
      _camControl.AdjustThirdPersonDistance(scroll > 0 ? 1 : -1);
      _cameraDistanceGestureLastScrollTime = Time.unscaledTime;

      if (_camControl.CurrentViewMode != viewModeBeforeAdjustment)
        _cameraDistanceGestureChangedViewMode = true;
    }

    private bool IsCameraDistanceModifierHeld()
    {
      var modifierKey = KeyBindingRepository.GetBoundKey(
        CameraDistanceModifierActionId,
        DefaultCameraDistanceModifier
      );
      return modifierKey != KeyCode.None && Input.GetKey(modifierKey);
    }

    private bool HasCameraDistanceScrollInput()
      => IsCameraDistanceModifierHeld() && Mathf.Abs(Input.mouseScrollDelta.y) > Mathf.Epsilon;

    private void FinalizeCameraDistanceGestureIfNeeded(bool hasCameraDistanceScrollInput)
    {
      if (!_cameraDistanceGestureActive)
        return;
      // 이 프레임에도 Alt/Option+휠 입력이 있다면, 유휴 시간이 길었더라도 같은 제스처로 처리한다.
      if (hasCameraDistanceScrollInput)
        return;
      if (Time.unscaledTime - _cameraDistanceGestureLastScrollTime < CameraDistanceGestureEndDelay)
        return;

      if (_camControl.IsUnityNull())
      {
        _cameraDistanceGestureActive = false;
        return;
      }

      if (_cameraDistanceGestureChangedViewMode)
      {
        // 전환된 1/3인칭 시점은 유지하고, 다음 실행에 사용할 POV 거리만 조정 시작 전 값으로 저장한다.
        PersistCameraDistance(_cameraDistanceGestureStartDistance);
      }
      else
      {
        PersistCameraDistance(_camControl.DesiredThirdPersonDistance);
      }

      _cameraDistanceGestureActive = false;
    }

    private static void PersistCameraDistance(float distance)
    {
      Registry.Registry.Get<CameraDistancePreferenceService>(
          RegistryType.Service,
          Registry.Registry.TypeKey<CameraDistancePreferenceService>())
        ?.PersistDistance(distance);
    }

    /// <summary>
    /// 대화창(대화/선택지)이 최상단일 때의 진행·확정 입력을 처리합니다.
    /// 대화(Dialogue)와 선택지(Choice) 모두 동일한 키 집합을 받습니다:
    /// 상호작용 키(기본 F), 스페이스바, 엔터(키패드 엔터 포함), 마우스 좌클릭.
    /// </summary>
    private void HandleDialogueInput()
    {
      // A remote player may spawn before the scene dialogue UI is available.
      // Resolve the active overlay when it appears instead of keeping the missing startup reference.
      if (UIOverlayStack.Top is DialoguePanelUIController activeDialogue)
        _dialoguePanelUIController = activeDialogue;

      if (_dialoguePanelUIController.IsUnityNull())
        return;
      if (!UIOverlayStack.IsTop(_dialoguePanelUIController))
        return;

      // 대화 진행 키를 여기에 집중시켜 모든 경로가 PlayerController.Input 을 거치게 한다.
      if (IsDialogueAdvanceInputDown())
      {
        _dialoguePanelUIController.TrySelectCurrentOption();
      }

      HandleInteractablesSelectionInput();
    }

    /// <summary>
    /// 대화 진행/선택지 확정으로 취급하는 입력이 이번 프레임에 눌렸는지 여부.
    /// </summary>
    private bool IsDialogueAdvanceInputDown()
    {
      return Input.GetKeyDown(_keyInteractInteractableObject)
          || Input.GetKeyDown(_keyAdvanceDialogue)
          || Input.GetKeyDown(_keyConfirmDialogue)
          || Input.GetKeyDown(KeyCode.KeypadEnter)
          || Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 이번 프레임의 진행/확정 입력을 대화창 UI가 이미 소비했는지 여부.
    /// 힌트 목록 행을 직접 클릭한 경우처럼 Update_Input 밖(UI Toolkit 이벤트)에서
    /// 소비된 입력도 포함하므로, 월드 입력을 읽는 경로는 모두 이 값을 확인해야 한다.
    /// </summary>
    private bool IsDialogueInputConsumedThisFrame()
    {
      return !_dialoguePanelUIController.IsUnityNull()
          && _dialoguePanelUIController.HasConsumedInputThisFrame;
    }

    /// <summary>
    /// 이번 프레임에 월드 대상 좌클릭 입력을 사용할 수 있는지 여부.
    /// UI 오버레이가 열려 있거나 대화창이 이미 입력을 소비했다면 false.
    /// </summary>
    private bool IsWorldClickInputAvailable()
    {
      return UIOverlayStack.IsEmpty() && !IsDialogueInputConsumedThisFrame();
    }

    // 대화 진행 키(스페이스바)를 누른 채로 대화가 닫히면, 같은 프레임 또는 바로 다음 프레임에
    // 아직 눌려 있는 그 키가 점프("Jump" 축, 기본 Space)로 해석되어 플레이어가 튀어오른다.
    // UI가 소비한 키 입력은 한 번 뗄 때까지 월드 점프 입력에서 제외한다.
    private bool _jumpInputSuppressedUntilRelease;

    private void UpdateJumpInputSuppression()
    {
      // 키를 떼는 순간 억제 해제. 다시 누르면 그때부터는 정상적인 점프 입력이다.
      // 관전자 상승 입력은 "Jump" 축이 아니라 스페이스바를 직접 읽으므로 두 입력을 모두 본다.
      // (Jump 축이 스페이스바에서 분리되어 있으면 한쪽만 확인해서는 억제가 걸리거나 풀리지 않는다.)
      if (!Input.GetButton("Jump") && !Input.GetKey(KeyCode.Space))
      {
        _jumpInputSuppressedUntilRelease = false;
        return;
      }

      // 오버레이가 열려 있는 동안(대화창 포함), 그리고 대화창이 이번 프레임 입력을 소비한
      // 직후(오버레이가 막 pop 된 프레임)에 눌려 있던 키는 UI의 것으로 간주한다.
      if (!UIOverlayStack.IsEmpty() || IsDialogueInputConsumedThisFrame())
        _jumpInputSuppressedUntilRelease = true;
    }

    /// <summary>점프 입력이 눌려 있는지 여부. UI가 소비한 입력은 제외한다.</summary>
    private bool IsJumpInputHeld()
    {
      return !_jumpInputSuppressedUntilRelease && Input.GetButton("Jump");
    }

    /// <summary>이번 프레임에 점프 입력이 새로 눌렸는지 여부. UI가 소비한 입력은 제외한다.</summary>
    private bool IsJumpInputPressedThisFrame()
    {
      return !_jumpInputSuppressedUntilRelease && Input.GetButtonDown("Jump");
    }

    /// <summary>관전자 상승 입력(스페이스바)이 눌려 있는지 여부. UI가 소비한 입력은 제외한다.</summary>
    private bool IsSpectatorAscendInputHeld()
    {
      return !_jumpInputSuppressedUntilRelease && Input.GetKey(KeyCode.Space);
    }

    private void HandleItemActionInput()
    {
      if (Input.GetMouseButtonDown(0))
        TriggerAttack();
      if (Input.GetMouseButtonDown(1))
        TriggerUseItem();

      if (!IsRidableControlActive && Input.GetKeyDown(KeyCode.LeftShift))
      {
        if (TryDropCarriedReposable(out _))
          RefreshInteractableHintsNow();
      }

      if (Input.GetKeyDown(_keyDropHeldItem))
        DropHeldItem();
    }

    private bool HandleOpenQuestUIInput()
    {
      if (!Input.GetKeyDown(_keyOpenQuestUI))
        return false;

      if (_questUIController.IsUnityNull())
        _questUIController = FindQuestUIController();

      if (_questUIController.IsUnityNull())
        return false;

      if (UIOverlayStack.IsTop(_questUIController))
      {
        UIOverlayStack.Pop();
        return true;
      }

      if (UIOverlayStack.IsEmpty())
      {
        UIOverlayStack.Push(_questUIController);
        return true;
      }

      return false;
    }

    private void HandleIntravenousLineConnectionModeExit()
    {
      if (!IsIntravenousLineConnectionMode)
        return;

      if (Input.GetKeyDown(_keySpectatorFlyDown))
        SetIntravenousLineConnectionMode(false);
    }
  }
}
