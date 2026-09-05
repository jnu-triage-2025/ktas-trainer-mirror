using System;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Character : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "character";
    public string Description => "Set or list player character models.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("character list", "List available character models."),
      new UsageLine("character set <model>", "Apply a model to yourself."),
      new UsageLine("character set <target> <model>", "Apply a model to a target."),
      new UsageLine("  <target>", PlayerTargetResolver.ShortSyntaxHint + "."),
      new UsageLine("  <model>", "Registered player model identifier."),
    };

    public string PermissionIdentifier => "character";

    private readonly ChatService _chat;

    public CommandDefinition_Character(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length == 0)
      {
        SendUsage(sender);
        return;
      }

      if (string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
      {
        if (args.Length != 1)
        {
          _chat.SendSystemMessage(sender, "Usage: /character list");
          return;
        }

        HandleList(sender);
        return;
      }

      if (!string.Equals(args[0], "set", StringComparison.OrdinalIgnoreCase))
      {
        SendUsage(sender);
        return;
      }

      if (args.Length == 2)
      {
        HandleSetSelf(sender, args[1]);
        return;
      }

      if (args.Length == 3)
      {
        HandleSetTarget(sender, args[1], args[2]);
        return;
      }

      SendUsage(sender);
    }

    private void HandleList(NetworkConnection sender)
    {
      var modelRegistry = Registry.Registry.GetAll<object>(RegistryType.PlayerModel);
      if (modelRegistry == null || modelRegistry.Count == 0)
      {
        _chat.SendSystemMessage(sender, "No player character models are registered.");
        return;
      }

      var identifiers = modelRegistry.Keys
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToArray();

      if (identifiers.Length == 0)
      {
        _chat.SendSystemMessage(sender, "No player character models are registered.");
        return;
      }

      _chat.SendSystemMessage(sender, $"Available character models ({identifiers.Length}): {string.Join(", ", identifiers)}");
    }

    private void SendUsage(NetworkConnection sender)
    {
      _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
    }

    private void HandleSetSelf(NetworkConnection sender, string modelIdentifier)
    {
      if (!TryResolveControllerByConnection(sender, out var controller))
      {
        _chat.SendSystemMessage(sender, "Unable to locate your target.");
        return;
      }

      ApplyAndBroadcast(sender, null, controller, modelIdentifier);
    }

    private void HandleSetTarget(NetworkConnection sender, string playerIdentifier, string modelIdentifier)
    {
      if (!TryResolveController(playerIdentifier, sender, out var controller, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      ApplyAndBroadcast(sender, playerIdentifier, controller, modelIdentifier);
    }

    private void ApplyAndBroadcast(NetworkConnection sender, string targetToken, PlayerController controller, string modelIdentifier)
    {
      if (controller == null)
      {
        _chat.SendSystemMessage(sender, "Target is invalid.");
        return;
      }

      if (string.IsNullOrWhiteSpace(modelIdentifier))
      {
        _chat.SendSystemMessage(sender, "model-id is required.");
        return;
      }

      if (!controller.ApplyPlayerModelByIdentifierServer(modelIdentifier))
      {
        _chat.SendSystemMessage(sender, $"Failed to set character model to '{modelIdentifier}'.");
        return;
      }

      string targetName = ResolveDisplayName(targetToken, controller);
      _chat.BroadcastSystemMessage($"{targetName}가 {modelIdentifier}캐릭터로 변경했습니다.");
    }

    private bool TryResolveController(string playerIdentifier, NetworkConnection sender, out PlayerController controller, out string error)
    {
      controller = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(playerIdentifier))
      {
        error = "target-id is required.";
        return false;
      }

      return PlayerTargetResolver.TryResolveSingleController(sender, playerIdentifier, out controller, out error);
    }

    private static bool TryResolveControllerByConnection(NetworkConnection connection, out PlayerController controller)
      => PlayerTargetResolver.TryGetController(connection, out controller);

    private static string ResolveDisplayName(string targetToken, PlayerController controller)
      => PlayerTargetResolver.DescribeTarget(targetToken, controller);
  }
}
