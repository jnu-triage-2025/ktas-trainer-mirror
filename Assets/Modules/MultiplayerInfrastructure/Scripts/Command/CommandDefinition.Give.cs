using System;
using FishNet;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Give : IChatCommandModel
  {
    public string CommandEntry => "give";
    public string Description => "Give an item. Usage: /give <item_identifier> [count=1] [player_identifier]";
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;

    public CommandDefinition_Give(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /give <item_identifier> [count=1] [player_identifier]");
        return;
      }

      string itemIdentifier = args[0];
      if (!Registry.Registry.Contains(RegistryType.Item, itemIdentifier))
      {
        _chat.SendSystemMessage(sender, $"Item '{itemIdentifier}' is not registered.");
        return;
      }

      int count = 1;
      string targetIdentifier = null;

      if (args.Length >= 2)
      {
        if (int.TryParse(args[1], out int parsedCount))
        {
          if (parsedCount <= 0)
          {
            _chat.SendSystemMessage(sender, "Count must be greater than 0.");
            return;
          }

          count = parsedCount;
          if (args.Length >= 3)
            targetIdentifier = args[2];
        }
        else
        {
          targetIdentifier = args[1];
        }
      }

      if (args.Length > 3)
      {
        _chat.SendSystemMessage(sender, "Usage: /give <item_identifier> [count=1] [player_identifier]");
        return;
      }

      if (!TryResolveTargetConnection(sender, targetIdentifier, out var targetConn, out var resolveError))
      {
        _chat.SendSystemMessage(sender, resolveError);
        return;
      }

      if (!TryGetPlayerController(targetConn, out var targetPlayer))
      {
        _chat.SendSystemMessage(sender, "Target player is not available.");
        return;
      }

      var toGive = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (toGive == null)
      {
        _chat.SendSystemMessage(sender, $"Item '{itemIdentifier}' data is unavailable.");
        return;
      }
      toGive.CurrentStackCount = count;

      bool fullyAdded = targetPlayer.TryAddItemToInventory(toGive, out ItemSystem.Item leftover);
      if (leftover != null && leftover.CurrentStackCount > 0)
      {
        targetPlayer.TryDropItemInFront(leftover);
      }

      int delivered = count - (leftover?.CurrentStackCount ?? 0);
      int dropped = leftover?.CurrentStackCount ?? 0;

      if (fullyAdded)
      {
        _chat.SendSystemMessage(sender, $"Gave {delivered}x '{itemIdentifier}' to player {targetConn.ClientId}.");
        return;
      }

      _chat.SendSystemMessage(sender, $"Gave {delivered}x '{itemIdentifier}' to player {targetConn.ClientId}. Dropped {dropped}x in front because inventory was full.");
    }

    private bool TryResolveTargetConnection(NetworkConnection sender, string rawTarget, out NetworkConnection target, out string error)
    {
      target = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(rawTarget))
      {
        if (sender == null)
        {
          error = "System execution requires a target player identifier.";
          return false;
        }

        target = sender;
        return true;
      }

      string lowered = rawTarget.ToLowerInvariant();
      if (lowered == "@s")
      {
        if (sender == null)
        {
          error = "@s cannot be used from system execution.";
          return false;
        }

        target = sender;
        return true;
      }

      if (lowered.StartsWith("fish:", StringComparison.Ordinal))
      {
        string idText = rawTarget.Substring("fish:".Length);
        if (!int.TryParse(idText, out int clientId))
        {
          error = "Invalid FishNet player identifier after 'fish:'.";
          return false;
        }

        target = FindConnectionByClientId(clientId);
        if (target == null)
        {
          error = $"No player found for fish id '{clientId}'.";
          return false;
        }

        return true;
      }

      if (int.TryParse(rawTarget, out int rawClientId))
      {
        target = FindConnectionByClientId(rawClientId);
        if (target == null)
        {
          error = $"No player found for client id '{rawClientId}'.";
          return false;
        }

        return true;
      }

      error = "Unknown target selector. Use fish:<id>, <id>, or @s.";
      return false;
    }

    private NetworkConnection FindConnectionByClientId(int clientId)
    {
      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return null;

      foreach (var kvp in clients)
      {
        var candidate = kvp.Value;
        if (candidate != null && candidate.ClientId == clientId)
          return candidate;
      }

      return null;
    }

    private bool TryGetPlayerController(NetworkConnection conn, out PlayerController controller)
    {
      controller = null;
      if (conn == null || conn.FirstObject == null)
        return false;

      return conn.FirstObject.TryGetComponent(out controller) && controller != null;
    }
  }
}
