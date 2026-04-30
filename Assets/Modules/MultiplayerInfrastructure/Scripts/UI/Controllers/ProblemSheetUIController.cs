using System;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Problem;
using MultiplayerInfrastructure.Registry;
using Unity.VisualScripting;
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

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public bool IsOpen => _problemSheet != null && _problemSheet.style.display != DisplayStyle.None;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      BindElement();
      HideImmediately();
    }

    public bool OpenProblemSet(string problemSetIdentifier, int index = 0)
    {
      if (!Registry.Registry.TryGetProblemSet(problemSetIdentifier, out var set, out _))
        return false;

      if (set?.Problems == null || set.Problems.Count == 0)
        return false;

      int clamped = Mathf.Clamp(index, 0, set.Problems.Count - 1);
      OpenProblem(set.Problems[clamped]);
      return true;
    }

    public void OpenProblem(ProblemDefinition problem)
    {
      EnsurePanel();
      _problemSheet?.Bind(problem);

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
      NotifyPlayerOverlay(true);
    }

    private void HidePanel()
    {
      if (_problemSheet != null)
        _problemSheet.style.display = DisplayStyle.None;

      NotifyPlayerOverlay(false);
    }

    private void HideImmediately()
    {
      EnsurePanel();
      _problemSheet.style.display = DisplayStyle.None;
    }

    private static void NotifyPlayerOverlay(bool expanding)
    {
      var playerController = Registry.Registry.GetFirstEntityComponent<PlayerController>(EntityType.Player, each => each != null && each.IsOwner);
      if (!playerController.IsUnityNull())
      {
        if (expanding) playerController.EnterUIOverlayMode();
        else playerController.ExitUIOverlayMode();
      }
    }
  }
}
