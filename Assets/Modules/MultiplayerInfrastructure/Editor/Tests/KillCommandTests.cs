using System.Linq;
using MultiplayerInfrastructure.Command;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Command
{
  public sealed class KillCommandTests
  {
    [Test]
    public void CommandServiceRegistersKillCommand()
    {
      var host = new GameObject(nameof(CommandServiceRegistersKillCommand));
      try
      {
        var service = host.AddComponent<ChatCommandService>();
        service.Initialize(null);

        Assert.That(service.GetCommands().Any(command => command.CommandEntry == "kill"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(host);
      }
    }

    [Test]
    public void UsageDocumentsPlayerAndEntityTargets()
    {
      var command = new CommandDefinition_Kill(null);
      var syntaxes = command.UsageLines.Select(line => line.Syntax).ToArray();

      Assert.That(syntaxes, Does.Contain("kill <player>"));
      Assert.That(syntaxes, Does.Contain("kill <entity_identifier>"));
    }
  }
}
