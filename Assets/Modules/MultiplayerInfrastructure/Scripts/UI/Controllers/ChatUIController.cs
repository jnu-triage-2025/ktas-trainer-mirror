using System;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class ChatUIController : UIControllerABC, IUIOverlay
  {
    [Header("UXML references")]
    [SerializeField] private string _chatRootName = DefaultsChatControl.ChatRootName;
    [SerializeField] private string _chatLogName = DefaultsChatControl.ChatLogName;
    [SerializeField] private string _chatInputName = DefaultsChatControl.ChatInputName;
    
    private UIDocument _uiDocument;
    private VisualElement _chatRoot;
    private ScrollView _chatLog;
    private TextField _inputField;
    private ChatUIController_ChatLogView _chatLogView;
    
    public bool IsOpened => _chatRoot != null && _chatRoot.style.visibility == Visibility.Visible;
    public bool IsFocused => _inputField != null &&
      _inputField.focusController != null && _inputField.focusController.focusedElement == _inputField;

    public event Action<string> OnSubmitted;
    public event Action OnCancelled;
    public event Action OverlayPushed;
    public event Action OverlayPopped;

    protected override void Awake()
    {
      base.Awake();

      _chatLogView = GetComponent<ChatUIController_ChatLogView>();
    }

    void Start()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      _uiDocument.sortingOrder = DefaultsUIDocument.ChatPanelUISortOrder;

      BindElements();
      RegisterKeyboardCallbacks();
      HideInstantly();
    }

    void BindElements()
    {
      VisualElement root = _uiDocument.rootVisualElement;
      _chatRoot = root.Q<VisualElement>(name: _chatRootName);
      _chatLog = root.Q<ScrollView>(name: _chatLogName);
      _inputField = root.Q<TextField>(name: _chatInputName);
      Debug.Log($"[ChatUIController] Bound elements: {root} {_chatRoot}, {_chatLog}, {_inputField}");
    }

    void RegisterKeyboardCallbacks()
    {
      if (_inputField == null) return;
      
      _inputField.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }
    
    private void OnKeyDown(KeyDownEvent evt)
    {
      if (evt.keyCode == KeyboardConfigurationRegistry.SendChat)
      {
        evt.StopImmediatePropagation();
        string text = ExtractCurrentInput();
        OnSubmitted?.Invoke(text);
      }
      else if (evt.keyCode == KeyCode.Escape)
      {
        evt.StopImmediatePropagation();
        OnCancelled?.Invoke();
      }
    }

    public void OpenInput()
    {
      if (!UIOverlayStack.IsTop(this))
        UIOverlayStack.Push(this); 
    }

    public void FocusInput()
    {
      if (!UIOverlayStack.IsTop(this))
        UIOverlayStack.Push(this);

      if (_inputField == null) return;
      
      _inputField.SetValueWithoutNotify(string.Empty);
      // When making an element visible/interactable, it is safer to schedule 
      // the focus for the next UI update to ensure the element is ready to receive input.
      _inputField.Focus();
      _inputField.cursorIndex = 0;
      _inputField.SelectRange(0, 0);
    }

    public void UnfocusInput()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      
      if (_inputField != null)
      {
        _inputField.SetValueWithoutNotify(string.Empty);
        _inputField.Blur();
      }
    }
    
    private void ToggleRoot(bool expanding)
    {
      if (_chatRoot == null)
        return;

      _chatRoot.style.visibility = expanding ? Visibility.Visible : Visibility.Hidden;
      _chatLogView?.OnChatVisibilityChanged(expanding);
      

      /*
        대부분의 상황에서 이 함수는 CurrentSessionPlayInfoRegistry.Instance.PlayerController가
        등록되고 난 후에 호출되므로, 아래의 조건문에 의해 카메라 처리가 수행되지 않게 되지는 않는다.

        다음 조건문에 의해 카메라 처리가 수행되지 않는 것은, 이 클래스 인스턴스가 MonoBehaviour 오브젝트로서
        초기화되는 시점에 HideInstantly()가 호출되는 시점 뿐이다.

        이 경우에는 플레이어 컨트롤러가 아직 등록되지 않았기 때문에, 카메라 잠금 처리가 수행되지 않는다.

        참고: 설정된 초기값에 의해 HideInstantly()가 호출되지 않더라도 문제되지는 않으나, 명확한 처리를 위해
        이 함수가 호출되도록 하였다.
      */
      var playerController = CurrentSessionPlayInfoRegistry.Get<PlayerController>();
      if (!playerController.IsUnityNull())
      {
        if (expanding) playerController.EnterUIOverlayMode();
        else playerController.ExitUIOverlayMode();
      }
    }

    public void HideInstantly()
    {
      ToggleRoot(expanding: false);
      _inputField?.SetValueWithoutNotify(string.Empty);
    }
    
    public string ExtractCurrentInput()
    {
      if (_inputField == null)
        return string.Empty;

      string current = _inputField.text;
      _inputField.SetValueWithoutNotify(string.Empty);
      return current;
    }

    // IUIOverlay
    public void OnOverlayPushed()
    {
      ToggleRoot(expanding: true);
      FocusInput();
      OverlayPushed?.Invoke();
    }
    public void OnOverlayPopped()
    {
      ToggleRoot(expanding: false);
      _inputField?.SetValueWithoutNotify(string.Empty);
      OverlayPopped?.Invoke();
    }
  }
}
