using System;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Character : IChatCommandModel
  {
    public string CommandEntry => "character";
    public string Description =>
      "Set or list player character models.\n"
      + "  /character list\n"
      + "  /character set <model-id>\n"
      + "  /character set <target-id> <model-id>";

    public bool RequiresAdmin => false;

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
      _chat.SendSystemMessage(sender, "Usage: /character list | /character set <model-id> | /character set <target-id> <model-id>");
    }

    private void HandleSetSelf(NetworkConnection sender, string modelIdentifier)
    {
      if (!TryResolveControllerByConnection(sender, out var controller))
      {
        _chat.SendSystemMessage(sender, "Unable to locate your target.");
        return;
      }

      ApplyAndBroadcast(sender, controller, modelIdentifier);
    }

    private void HandleSetTarget(NetworkConnection sender, string playerIdentifier, string modelIdentifier)
    {
      if (!TryResolveController(playerIdentifier, sender, out var controller, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      ApplyAndBroadcast(sender, controller, modelIdentifier);
    }

    private void ApplyAndBroadcast(NetworkConnection sender, PlayerController controller, string modelIdentifier)
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

      string targetName = ResolveDisplayName(controller.Owner);
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

      if (playerIdentifier.StartsWith("@", StringComparison.Ordinal))
      {
        if (!TargetSelectorResolver.TryResolveTargets(sender, playerIdentifier, out var targets, out error))
          return false;

        if (targets.Count != 1)
        {
          error = $"Target selector matched {targets.Count} targets; expected 1.";
          return false;
        }

        if (!TryResolveControllerByConnection(targets[0], out controller))
        {
          error = "Target is invalid.";
          return false;
        }

        return true;
      }

      if (string.Equals(playerIdentifier, "@self", StringComparison.OrdinalIgnoreCase)
          || string.Equals(playerIdentifier, "@s", StringComparison.OrdinalIgnoreCase))
      {
        if (!TryResolveControllerByConnection(sender, out controller))
        {
          error = "Unable to locate your target.";
          return false;
        }

        return true;
      }

      if (TryResolveConnectionBySelector(playerIdentifier, out var targetConnection)
          && TryResolveControllerByConnection(targetConnection, out controller))
      {
        return true;
      }

      error = $"Target '{playerIdentifier}' was not found.";
      return false;
    }

    private static bool TryResolveConnectionBySelector(string selector, out NetworkConnection connection)
    {
      connection = null;
      if (string.IsNullOrWhiteSpace(selector))
        return false;

      if (selector.StartsWith("fish:", StringComparison.OrdinalIgnoreCase))
      {
        string rawClientId = selector.Substring("fish:".Length);
        return TryResolveConnectionByClientId(rawClientId, out connection);
      }

      if (selector.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
      {
        string userIdentifier = selector.Substring("id:".Length);
        return TryResolveConnectionByUserIdentifier(userIdentifier, out connection);
      }

      if (selector.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
      {
        string displayName = selector.Substring("name:".Length);
        return TryResolveConnectionByDisplayName(displayName, out connection);
      }

      if (int.TryParse(selector, out _))
      {
        return TryResolveConnectionByClientId(selector, out connection);
      }

      // Fallback order: user identifier first, then display name.
      if (TryResolveConnectionByUserIdentifier(selector, out connection))
        return true;

      return TryResolveConnectionByDisplayName(selector, out connection);
    }

    private static bool TryResolveConnectionByClientId(string rawClientId, out NetworkConnection connection)
    {
      connection = null;
      if (!int.TryParse(rawClientId, out int clientId))
        return false;

      return TryGetConnectionByClientId(clientId, out connection);
    }

    private static bool TryResolveConnectionByUserIdentifier(string userIdentifier, out NetworkConnection connection)
    {
      connection = null;
      if (!Registry.Registry.TryGetEntityByOwnerUserIdentifier(userIdentifier, out var descriptor) || descriptor == null)
        return false;

      if (descriptor.ClientId == null)
        return false;

      return TryGetConnectionByClientId(descriptor.ClientId.Value, out connection);
    }

    private static bool TryResolveConnectionByDisplayName(string displayName, out NetworkConnection connection)
    {
      connection = null;
      if (!UserDescriptorService.TryGetByDisplayName(displayName, out var descriptor))
        return false;

      if (!UserDescriptorService.TryGetClientId(descriptor.Identifier, out int clientId))
        return false;

      return TryGetConnectionByClientId(clientId, out connection);
    }

    private static bool TryGetConnectionByClientId(int clientId, out NetworkConnection connection)
    {
      connection = null;

      var clients = FishNet.InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return false;

      foreach (var pair in clients)
      {
        if (pair.Value != null && pair.Value.ClientId == clientId)
        {
          connection = pair.Value;
          return true;
        }
      }

      return false;
    }

    private static bool TryResolveControllerByConnection(NetworkConnection connection, out PlayerController controller)
    {
      controller = null;
      if (connection == null || connection.FirstObject == null)
        return false;

      return connection.FirstObject.TryGetComponent(out controller);
    }

    private static string ResolveDisplayName(NetworkConnection connection)
    {
      if (connection != null && UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor))
        return descriptor.DisplayName;

      return connection?.ClientId.ToString() ?? "Unknown";
    }
  }
}