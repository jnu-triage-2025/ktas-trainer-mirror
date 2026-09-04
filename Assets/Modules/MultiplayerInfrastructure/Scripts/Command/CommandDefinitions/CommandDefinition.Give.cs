using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Give : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "give";
    public string Description => "Give an item to a target.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("give <item> [count] [target]", "Give an item. Overflow drops in front."),
      new UsageLine("  <item>", "Registered item identifier."),
      new UsageLine("  [count]", "Amount to give. Default: 1."),
      new UsageLine("  [target]", PlayerTargetResolver.ShortSyntaxHint + ". Default: you."),
    };
    public string PermissionIdentifier => "give";

    private readonly ChatService _chat;

    public CommandDefinition_Give(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (!TryExecuteGive(sender, args, out string message))
      {
        _chat.SendSystemMessage(sender, message);
        return;
      }

      _chat.SendSystemMessage(sender, message);
    }

    /// <summary>
    /// 아이템 지급의 도메인 로직. 채팅 명령과 시나리오 노드가 동일한 검증·인벤토리·초과분 드롭
    /// 처리를 사용하도록, UI/권한/채팅 전송과 분리한다.
    /// </summary>
    public static bool TryExecuteGive(NetworkConnection sender, string[] args, out string message)
    {
      message = string.Empty;
      if (args == null || args.Length == 0)
      {
        message = "Usage: /give <item_identifier> [count=1] [target_identifier]";
        return false;
      }

      string itemIdentifier = args[0];
      if (!Registry.Registry.Contains(RegistryType.Item, itemIdentifier))
      {
        message = $"Item '{itemIdentifier}' is not registered.";
        return false;
      }

      int count = 1;
      string targetIdentifier = null;

      if (args.Length >= 2)
      {
        if (int.TryParse(args[1], out int parsedCount))
        {
          if (parsedCount <= 0)
          {
            message = "Count must be greater than 0.";
            return false;
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
        message = "Usage: /give <item_identifier> [count=1] [target_identifier]";
        return false;
      }

      if (!TryResolveTargetConnection(sender, targetIdentifier, out var targetConn, out var resolveError))
      {
        message = resolveError;
        return false;
      }

      if (!TryGetPlayerController(targetConn, out var targetPlayer))
      {
        message = "Target is not available.";
        return false;
      }

      var toGive = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (toGive == null)
      {
        message = $"Item '{itemIdentifier}' data is unavailable.";
        return false;
      }
      toGive.CurrentStackCount = count;

      bool fullyAdded = targetPlayer.TryAddItemToInventory(toGive, out ItemSystem.Item leftover);
      if (leftover != null && leftover.CurrentStackCount > 0)
      {
        targetPlayer.TryDropItemInFront(leftover);
      }

      int delivered = count - (leftover?.CurrentStackCount ?? 0);
      int dropped = leftover?.CurrentStackCount ?? 0;
      string targetDisplayName = ResolveTargetDisplayName(targetConn);

      if (fullyAdded)
      {
        message = $"Gave {delivered}x '{itemIdentifier}' to {targetDisplayName}.";
        return true;
      }

      message = $"Gave {delivered}x '{itemIdentifier}' to {targetDisplayName}. Dropped {dropped}x in front because inventory was full.";
      return true;
    }

    private static bool TryResolveTargetConnection(NetworkConnection sender, string rawTarget, out NetworkConnection target, out string error)
    {
      target = null;
      error = string.Empty;

      // 대상을 생략하면 실행자 자신에게 지급한다.
      if (string.IsNullOrWhiteSpace(rawTarget))
      {
        if (sender == null)
        {
          error = "System execution requires a target identifier.";
          return false;
        }

        target = sender;
        return true;
      }

      return PlayerTargetResolver.TryResolveSingleConnection(sender, rawTarget, out target, out error);
    }

    private static bool TryGetPlayerController(NetworkConnection conn, out PlayerController controller)
      => PlayerTargetResolver.TryGetController(conn, out controller);

    private static string ResolveTargetDisplayName(NetworkConnection connection)
      => PlayerTargetResolver.DescribeConnection(connection);
  }
}
