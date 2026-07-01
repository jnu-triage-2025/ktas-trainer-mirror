using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Clean : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "clean";
    public string Description => "Clean your inventory.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("clean", "Remove all items."),
      new UsageLine("clean <item>", "Remove all of one item."),
      new UsageLine("clean <item> <count>", "Remove up to count of one item."),
    };
    public bool RequiresAdmin => false;

    private readonly ChatService _chat;

    public CommandDefinition_Clean(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (sender == null || sender.FirstObject == null || !sender.FirstObject.TryGetComponent(out PlayerController player) || player == null)
      {
        _chat.SendSystemMessage(sender, "Unable to locate your target. /clean requires target context.");
        return;
      }

      if (args == null || args.Length == 0)
      {
        int removedAll = player.ClearInventory();
        _chat.SendSystemMessage(sender, $"Inventory cleaned. Removed {removedAll} item(s).");
        return;
      }

      if (args.Length > 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /clean [item_identifier] [count]");
        return;
      }

      string itemIdentifier = args[0];
      if (int.TryParse(itemIdentifier, out _))
      {
        _chat.SendSystemMessage(sender, "Invalid syntax: count can be used only when item identifier is specified.");
        return;
      }

      if (!Registry.Registry.Contains(RegistryType.Item, itemIdentifier))
      {
        _chat.SendSystemMessage(sender, $"Item '{itemIdentifier}' is not registered.");
        return;
      }

      if (args.Length == 1)
      {
        int removed = player.RemoveAllOfItemFromInventory(itemIdentifier);
        _chat.SendSystemMessage(sender, $"Removed {removed}x '{itemIdentifier}'.");
        return;
      }

      if (!int.TryParse(args[1], out int count) || count <= 0)
      {
        _chat.SendSystemMessage(sender, "Count must be a positive integer.");
        return;
      }

      int removedCount = player.RemoveItemFromInventory(itemIdentifier, count);
      _chat.SendSystemMessage(sender, $"Removed {removedCount}x '{itemIdentifier}' (requested {count}).");
    }
  }
}
