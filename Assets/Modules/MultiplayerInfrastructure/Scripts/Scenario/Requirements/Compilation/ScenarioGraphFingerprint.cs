using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioGraphFingerprint
  {
    public static string Compute(ScenarioGraph graph)
    {
      if (graph == null) throw new ArgumentNullException(nameof(graph));
      var canonical = ScenarioGraphLoader.SaveToJson(graph, false);
      return ScenarioRequirementSourceHasher.ComputeSha256(Encoding.UTF8.GetBytes(canonical));
    }
  }
}
