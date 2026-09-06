using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>A server-owned rendezvous; departed participants do not count as successful work.</summary>
  public sealed class ScenarioCompletionBarrier
  {
    private readonly HashSet<int> _expected;
    private readonly HashSet<int> _completed = new();
    public double Deadline { get; }
    public bool Released { get; private set; }
    public bool TimedOut { get; private set; }

    public ScenarioCompletionBarrier(IEnumerable<int> expected, double deadline)
    {
      _expected = new HashSet<int>(expected);
      Deadline = deadline;
    }

    public bool Complete(int clientId)
      => _expected.Contains(clientId) && _completed.Add(clientId);

    public bool Evaluate(ISet<int> connected, double now)
    {
      if (Released) return true;
      bool complete = _expected.All(id => !connected.Contains(id) || _completed.Contains(id));
      TimedOut = !complete && now >= Deadline;
      Released = complete || TimedOut;
      return Released;
    }
  }
}
