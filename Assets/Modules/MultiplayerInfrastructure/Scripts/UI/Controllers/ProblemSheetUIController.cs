using System;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Problem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Chat;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class ProblemSheetUIController : UIControllerABC, IUIOverlay
  {
    [SerializeField] private string _problemSheetRootName = "problem-sheet-root";
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.ProblemSheetUISortOrder;

    private UIDocument _uiDocument;
    private ProblemSheetElement _problemSheet;
    private ChatService _chatService;
    private ProblemSetDefinition _activeSet;
    private string _activeSetIdentifier;
    private int _currentIndex;
    private bool _singleProblemMode;
    private bool _alreadyAwardedCurrent;
    private bool _alreadyFinalizedCurrent;
    private int _lastGradeCode = 1;

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public int LastGradeCode => _lastGradeCode;

    public bool IsOpen => _problemSheet != null && _problemSheet.style.display != DisplayStyle.None;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      _chatService = Registry.Registry.Get<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>());
      BindElement();
      HideImmediately();
    }

    private void OnEnable()
    {
      if (!IsOpen)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_uiDocument));
    }

    public bool OpenProblemSet(string problemSetIdentifier, int index = 0, bool singleProblemMode = false)
    {
      if (!Registry.Registry.TryGetProblemSet(problemSetIdentifier, out var set, out _))
        return false;

      if (set?.Problems == null || set.Problems.Count == 0)
        return false;

      _activeSet = set;
      _activeSetIdentifier = set.Identifier;
      _singleProblemMode = singleProblemMode;

      int clamped = Mathf.Clamp(index, 0, set.Problems.Count - 1);
      OpenProblemByIndex(clamped);
      return true;
    }

    public void OpenProblem(ProblemDefinition problem)
    {
      _activeSet = null;
      _activeSetIdentifier = string.Empty;
      _singleProblemMode = true;
      _alreadyAwardedCurrent = false;
      _alreadyFinalizedCurrent = false;
      _lastGradeCode = 1;

      EnsurePanel();
      bool retryOnWrong = problem?.Grading?.RetryOnWrong != false;
      _problemSheet?.Bind(problem, canProgressToNext: false, isLastProblem: true, problemOrder: 1, totalCount: 1, retryOnWrong: retryOnWrong);

      if (!UIOverlayStack.IsTop(this))
      {
        UIOverlayStack.Push(this);
        return;
      }

      ShowPanel();
    }

    public void Close()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        HidePanel();
    }

    public void OnOverlayPushed()
    {
      ShowPanel();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      HidePanel();
      OverlayPopped?.Invoke();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument.rootVisualElement;
      _problemSheet = root.Q<ProblemSheetElement>(_problemSheetRootName);
      if (_problemSheet == null)
      {
        _problemSheet = new ProblemSheetElement();
        root.Add(_problemSheet);
      }

      _problemSheet.OnGraded += HandleGraded;
      _problemSheet.OnNextRequested += HandleNextRequested;
      _problemSheet.OnCloseRequested += HandleCloseRequested;
    }

    private void OpenProblemByIndex(int index)
    {
      if (_activeSet?.Problems == null || _activeSet.Problems.Count == 0)
        return;

      _currentIndex = Mathf.Clamp(index, 0, _activeSet.Problems.Count - 1);
      _alreadyAwardedCurrent = false;
      _alreadyFinalizedCurrent = false;
      _lastGradeCode = 1;
      bool isLast = _currentIndex >= _activeSet.Problems.Count - 1;
      bool canProgress = !_singleProblemMode && !isLast;
      var currentProblem = _activeSet.Problems[_currentIndex];
      bool retryOnWrong = currentProblem?.Grading?.RetryOnWrong != false;
      _problemSheet?.Bind(currentProblem, canProgress, isLast, _currentIndex + 1, _activeSet.Problems.Count, retryOnWrong);

      if (!UIOverlayStack.IsTop(this))
      {
        UIOverlayStack.Push(this);
        return;
      }

      ShowPanel();
    }

    private void HandleGraded(bool correct)
    {
      _lastGradeCode = correct ? 0 : 1;

      var currentProblem = (_activeSet?.Problems != null && _currentIndex >= 0 && _currentIndex < _activeSet.Problems.Count)
        ? _activeSet.Problems[_currentIndex]
        : null;

      bool retryOnWrong = currentProblem?.Grading?.RetryOnWrong != false;
      bool finalized = correct || !retryOnWrong;
      if (!finalized || _alreadyFinalizedCurrent)
      {
        if (correct && !_alreadyAwardedCurrent)
        {
          _alreadyAwardedCurrent = true;
          if (_chatService != null && !string.IsNullOrWhiteSpace(_activeSetIdentifier))
            _chatService.ReportProblemGrade(_activeSetIdentifier, _currentIndex, _lastGradeCode);
        }
        return;
      }

      _alreadyFinalizedCurrent = true;

      if (_chatService != null && !string.IsNullOrWhiteSpace(_activeSetIdentifier))
        _chatService.ReportProblemGrade(_activeSetIdentifier, _currentIndex, _lastGradeCode);

      if (!correct || _alreadyAwardedCurrent)
        return;

      _alreadyAwardedCurrent = true;
    }

    private void HandleNextRequested()
    {
      if (_activeSet?.Problems == null)
        return;

      if (_singleProblemMode)
        return;

      int next = _currentIndex + 1;
      if (next >= _activeSet.Problems.Count)
      {
        Close();
        return;
      }

      OpenProblemByIndex(next);
    }

    private void HandleCloseRequested()
    {
      Close();
    }

    private void EnsurePanel()
    {
      if (_problemSheet == null)
        BindElement();
    }

    private void ShowPanel()
    {
      EnsurePanel();
      _problemSheet.style.display = DisplayStyle.Flex;
      SetDocumentRootInteractable(_uiDocument, true);
    }

    private void HidePanel()
    {
      if (_problemSheet != null)
        _problemSheet.style.display = DisplayStyle.None;
      SetDocumentRootInteractable(_uiDocument, false);
    }

    private void HideImmediately()
    {
      EnsurePanel();
      _problemSheet.style.display = DisplayStyle.None;
      SetDocumentRootInteractable(_uiDocument, false);
    }
  }
}
