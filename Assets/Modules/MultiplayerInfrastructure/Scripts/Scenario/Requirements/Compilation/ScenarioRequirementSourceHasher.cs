using System;
using System.Security.Cryptography;
using System.Text;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioRequirementSourceHasher
  {
    public static string ComputeSha256(byte[] sourceBytes)
    {
      if (sourceBytes == null) throw new ArgumentNullException(nameof(sourceBytes));
      using var sha = SHA256.Create();
      var hash = sha.ComputeHash(sourceBytes);
      var builder = new StringBuilder(64);
      foreach (var value in hash) builder.Append(value.ToString("x2"));
      return builder.ToString();
    }
  }
}
