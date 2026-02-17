using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioQuizNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Quiz;
    public string NextIdentifier { get; set; }

    public string Question { get; set; }
    public IReadOnlyList<string> Options { get; set; }
    public int CorrectIndex { get; set; }
    public string OnCorrectNextIdentifier { get; set; }
    public string OnIncorrectNextIdentifier { get; set; }
    public string FeedbackCorrect { get; set; }
    public string FeedbackIncorrect { get; set; }
  }
}
