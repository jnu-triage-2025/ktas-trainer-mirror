using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 서버의 외부 접속 허용/불가(커넥션 게이트)를 전환하는 채팅 명령어.
  /// 사용법: /conngate open | /conngate close
  /// </summary>
  public class CommandDefinition_ConnGate : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "conngate";
    public string Description => "Open or close the server connection gate for external clients.";
    public string PermissionIdentifier => "conngate";

    public IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("open", "Allow external connections and resume LAN broadcast."),
      new UsageLine("close", "Block new external connections and stop LAN broadcast."),
    };

    private readonly ChatService _chat;

    public CommandDefinition_ConnGate(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (args == null || args.Length == 0)
      {
        string status = ConnectionGateService.IsOpen ? "open" : "closed";
        _chat?.SendSystemMessage(sender, $"Connection gate is currently {status}. Usage: /conngate <open|close>");
        return;
      }

      string sub = args[0].ToLowerInvariant();
      switch (sub)
      {
        case "open":
          if (ConnectionGateService.IsOpen)
          {
            _chat?.SendSystemMessage(sender, "Connection gate is already open.");
            return;
          }

          ConnectionGateService.Open();
          _chat?.SendSystemMessage(sender, "Connection gate opened.");
          break;

        case "close":
          if (!ConnectionGateService.IsOpen)
          {
            _chat?.SendSystemMessage(sender, "Connection gate is already closed.");
            return;
          }

          ConnectionGateService.Close();
          _chat?.SendSystemMessage(sender, "Connection gate closed. New external connections will be rejected.");
          break;

        default:
          _chat?.SendSystemMessage(sender, $"Unknown subcommand '{args[0]}'. Usage: /conngate <open|close>");
          break;
      }
    }
  }
}
