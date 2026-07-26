using System;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// Groups item-related commands.
  /// </summary>
  public class CommandDefinition_Item : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "item";
    public string Description => "Manage and inspect registered items.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("item give <item> [count] [target]", "Give an item."),
      new UsageLine("item list", "List all registered items."),
    };
    public string PermissionIdentifier => "give";

    private readonly ChatService _chat;
    private readonly CommandDefinition_Give _giveCommand;

    public CommandDefinition_Item(ChatService chat)
    {
      _chat = chat;
      _giveCommand = new CommandDefinition_Give(chat);
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

      switch (args[0].ToLowerInvariant())
      {
        case "give":
          _giveCommand.Execute(sender, args.Skip(1).ToArray());
          return;

        case "list":
          if (args.Length != 1)
          {
            SendUsage(sender);
            return;
          }

          SendItemList(sender);
          return;

        default:
          _chat.SendSystemMessage(sender, $"Unknown item subcommand: {args[0]}");
          SendUsage(sender);
          return;
      }
    }

    private void SendItemList(NetworkConnection sender)
    {
      var itemIdentifiers = Registry.Registry.GetAll<Type>(RegistryType.Item)
        .Keys
        .OrderBy(identifier => identifier, StringComparer.Ordinal)
        .ToArray();

      if (itemIdentifiers.Length == 0)
      {
        _chat.SendSystemMessage(sender, "No items are registered.");
        return;
      }

      var lines = itemIdentifiers.Select(identifier =>
      {
        string displayName = Registry.Registry.GetItemDisplayName(identifier);
        return string.Equals(displayName, identifier, StringComparison.Ordinal)
          ? identifier
          : $"{identifier} ({displayName})";
      });

      _chat.SendSystemMessage(sender, $"Registered items ({itemIdentifiers.Length}):\n{string.Join("\n", lines)}");
    }

    private void SendUsage(NetworkConnection sender)
    {
      _chat.SendSystemMessage(sender, "Usage: /item give <item_identifier> [count=1] [target_identifier] | /item list");
    }
  }
}
