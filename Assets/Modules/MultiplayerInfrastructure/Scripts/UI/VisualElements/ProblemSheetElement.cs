using System;
using MultiplayerInfrastructure.Problem;
using MultiplayerInfrastructure.Registry;
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

    private ProblemDefinition _boundProblem;

    public event Action<bool> OnGraded;

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
    }

    public void Bind(ProblemDefinition problem)
    {
      _boundProblem = problem;
      _result.text = string.Empty;

      Texture2D figureTexture = null;
      if (problem != null && !string.IsNullOrWhiteSpace(problem.FigureIdentifier))
      {
        Registry.Registry.TryResolveProblemFigureReference(problem.FigureIdentifier, out figureTexture);
      }

      _prompt.Bind(problem?.Prompt, figureTexture);

      bool hasChoice = problem?.Choice?.Options != null && problem.Choice.Options.Count > 0;
      _choice.Bind(hasChoice ? problem.Choice.Options : null);

      bool hasShortAnswer = problem?.ShortAnswer != null;
      _shortAnswer.Bind(hasShortAnswer);
      if (hasShortAnswer)
        _shortAnswer.FocusInput();
    }

    private void HandleChoiceSelected(int selectedIndex)
    {
      bool correct = ProblemAnswerEvaluator.EvaluateChoice(_boundProblem?.Choice, selectedIndex);
      ShowResult(correct);
    }

    private void HandleShortAnswerSubmit(string input)
    {
      bool correct = ProblemAnswerEvaluator.EvaluateShortAnswer(_boundProblem?.ShortAnswer, input);
      ShowResult(correct);
    }

    private void ShowResult(bool correct)
    {
      _result.text = correct ? "정답" : "오답";
      _result.style.color = correct ? new Color(0.4f, 0.95f, 0.5f, 1f) : new Color(1f, 0.4f, 0.4f, 1f);
      OnGraded?.Invoke(correct);
    }
  }
}
