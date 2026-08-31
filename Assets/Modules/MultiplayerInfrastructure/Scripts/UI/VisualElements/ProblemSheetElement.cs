using System;
using MultiplayerInfrastructure.Problem;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class ProblemSheetElement : VisualElement
  {
    private readonly ProblemPromptElement _prompt;
    private readonly ProblemChoiceElement _choice;
    private readonly ProblemShortAnswerElement _shortAnswer;
    private readonly Label _result;
    private readonly Label _progress;
    private readonly VisualElement _actions;
    private readonly Button _nextButton;
    private readonly Button _closeButton;
    private bool _canProgressToNext;
    private bool _isLastProblem;
    private bool _lastProblemSolved;
    private bool _retryOnWrong;
    private bool _gradedFinalized;

    private ProblemDefinition _boundProblem;

    public event Action<bool, int, string> OnGraded;
    public event Action OnNextRequested;
    public event Action OnCloseRequested;

    public ProblemSheetElement()
    {
      name = "problem-sheet-root";
      style.flexDirection = FlexDirection.Column;
      style.paddingLeft = 12;
      style.paddingRight = 12;
      style.paddingTop = 12;
      style.paddingBottom = 12;
      style.backgroundColor = new Color(0f, 0f, 0f, 0.74f);

      _prompt = new ProblemPromptElement();
      Add(_prompt);

      _choice = new ProblemChoiceElement();
      _choice.OnChoiceSelected += HandleChoiceSelected;
      Add(_choice);

      _shortAnswer = new ProblemShortAnswerElement();
      _shortAnswer.OnSubmit += HandleShortAnswerSubmit;
      Add(_shortAnswer);

      _result = new Label();
      _result.style.marginTop = 10;
      _result.style.unityFontStyleAndWeight = FontStyle.Bold;
      Add(_result);

      _progress = new Label();
      _progress.style.marginTop = 6;
      _progress.style.color = new Color(0.78f, 0.88f, 1f, 0.95f);
      Add(_progress);

      _actions = new VisualElement();
      _actions.style.flexDirection = FlexDirection.Row;
      _actions.style.marginTop = 10;
      Add(_actions);

      _nextButton = new Button(() => OnNextRequested?.Invoke())
      {
        text = "다음 문제"
      };
      _nextButton.style.marginRight = 8;
      _actions.Add(_nextButton);

      _closeButton = new Button(() => OnCloseRequested?.Invoke())
      {
        text = "닫기"
      };
      _actions.Add(_closeButton);
    }

    public void Bind(ProblemDefinition problem, bool canProgressToNext, bool isLastProblem, int problemOrder, int totalCount, bool retryOnWrong)
    {
      _boundProblem = problem;
      _canProgressToNext = canProgressToNext;
      _isLastProblem = isLastProblem;
      _retryOnWrong = retryOnWrong;
      _lastProblemSolved = false;
      _gradedFinalized = false;
      _result.text = string.Empty;
      _progress.text = totalCount > 0 ? $"문제 {problemOrder}/{totalCount}" : string.Empty;

      Texture2D figureTexture = null;
      if (problem != null && !string.IsNullOrWhiteSpace(problem.Figure))
      {
        Registry.Registry.TryResolveProblemFigureReference(problem.Figure, out figureTexture);
      }

      _prompt.Bind(problem?.Prompt, figureTexture);

      bool hasChoice = problem?.Choice?.Options != null && problem.Choice.Options.Count > 0;
      _choice.Bind(hasChoice ? problem.Choice.Options : null);

      bool hasShortAnswer = problem?.ShortAnswer != null;
      _shortAnswer.Bind(hasShortAnswer);
      if (hasShortAnswer)
        _shortAnswer.FocusInput();

      _nextButton.style.display = DisplayStyle.None;
      _closeButton.text = "닫기";
      _closeButton.style.display = DisplayStyle.Flex;

      SetAnswerInputsEnabled(true);
    }

    public void Bind(ProblemDefinition problem)
      => Bind(problem, canProgressToNext: false, isLastProblem: true, problemOrder: 1, totalCount: 1, retryOnWrong: true);

    private void HandleChoiceSelected(int selectedIndex)
    {
      bool correct = ProblemAnswerEvaluator.EvaluateChoice(_boundProblem?.Choice, selectedIndex);
      ShowResult(correct, selectedIndex, null);
    }

    private void HandleShortAnswerSubmit(string input)
    {
      bool correct = ProblemAnswerEvaluator.EvaluateShortAnswer(_boundProblem?.ShortAnswer, input);
      ShowResult(correct, -1, input);
    }

    private void ShowResult(bool correct, int selectedChoiceIndex, string shortAnswer)
    {
      if (_gradedFinalized)
        return;

      _result.text = correct ? "정답" : "오답";
      _result.style.color = correct ? new Color(0.4f, 0.95f, 0.5f, 1f) : new Color(1f, 0.4f, 0.4f, 1f);
      if (correct && _canProgressToNext)
        _nextButton.style.display = DisplayStyle.Flex;

      if (correct && _isLastProblem)
        _lastProblemSolved = true;

      _closeButton.text = _lastProblemSolved ? "완료" : "닫기";

      if (correct || !_retryOnWrong)
      {
        _gradedFinalized = true;
        SetAnswerInputsEnabled(false);
      }

      OnGraded?.Invoke(correct, selectedChoiceIndex, shortAnswer);
    }

    private void SetAnswerInputsEnabled(bool enabled)
    {
      _choice?.SetEnabled(enabled);
      _shortAnswer?.SetEnabled(enabled);
    }
  }
}
