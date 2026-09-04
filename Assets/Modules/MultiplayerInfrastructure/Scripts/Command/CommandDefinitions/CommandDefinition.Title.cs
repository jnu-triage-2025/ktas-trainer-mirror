using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_Title : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "title";
    public string Description => "Display screen titles and actionbar text.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("title <targets> clear", "Hide all title text."),
      new UsageLine("title <targets> reset", "Reset times and subtitle."),
      new UsageLine("title <targets> title <text>", "Show a title."),
      new UsageLine("title <targets> subtitle <text>", "Show a subtitle."),
      new UsageLine("title <targets> actionbar <text>", "Show actionbar text."),
      new UsageLine("title <targets> times <fadeIn> <stay> <fadeOut>", "Set timings, in ticks."),
      new UsageLine("  <targets>", PlayerTargetResolver.ShortSyntaxHint),
    };

    public string PermissionIdentifier => "title";

    private readonly ChatService _chat;

    public CommandDefinition_Title(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length < 2)
      {
        _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
        return;
      }

      string targetSelector = args[0];
      string sub = args[1].ToLowerInvariant();

      if (!PlayerTargetResolver.TryResolveConnections(sender, targetSelector, out List<NetworkConnection> targets, out string targetError))
      {
        _chat.SendSystemMessage(sender, targetError);
        return;
      }

      switch (sub)
      {
        case "clear":
          if (args.Length != 2)
          {
            _chat.SendSystemMessage(sender, "Usage: /title <targets> clear");
            return;
          }

          Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitleClear(t, out err), "Title cleared");
          return;

        case "reset":
          if (args.Length != 2)
          {
            _chat.SendSystemMessage(sender, "Usage: /title <targets> reset");
            return;
          }

          Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitleReset(t, out err), "Title reset");
          return;

        case "times":
          if (args.Length != 5)
          {
            _chat.SendSystemMessage(sender, "Usage: /title <targets> times <fadeIn> <stay> <fadeOut>");
            return;
          }

          if (!TryParseTicks(args[2], args[3], args[4], out int fadeIn, out int stay, out int fadeOut))
          {
            _chat.SendSystemMessage(sender, "Times must be non-negative integers in ticks.");
            return;
          }

          Dispatch(
            sender,
            targets,
            (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitleTimes(t, fadeIn, stay, fadeOut, out err),
            $"Title times set to {fadeIn}/{stay}/{fadeOut} ticks");
          return;

        case "title":
        case "subtitle":
        case "actionbar":
          if (args.Length < 3)
          {
            _chat.SendSystemMessage(sender, $"Usage: /title <targets> {sub} <text>");
            return;
          }

          string text = string.Join(' ', args[2..]).Trim();
          if (string.IsNullOrWhiteSpace(text))
          {
            _chat.SendSystemMessage(sender, "Text cannot be empty.");
            return;
          }

          if (sub == "title")
            Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchTitle(t, text, null, out err), "Title displayed");
          else if (sub == "subtitle")
            Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchSubtitle(t, text, out err), "Subtitle updated");
          else
            Dispatch(sender, targets, (IEnumerable<NetworkConnection> t, out string err) => _chat.TryDispatchActionbar(t, text, out err), "Actionbar displayed");
          return;
      }

      _chat.SendSystemMessage(sender, $"Unknown subcommand '{args[1]}'. Use /title for help.");
    }

    private delegate bool DispatchCall(IEnumerable<NetworkConnection> targets, out string error);

    private void Dispatch(
      NetworkConnection sender,
      List<NetworkConnection> targets,
      DispatchCall action,
      string successMessage)
    {
      if (action == null)
        return;

      if (!action(targets, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"{successMessage} ({targets.Count} target(s)).");
    }

    private bool TryParseTicks(string fadeInRaw, string stayRaw, string fadeOutRaw, out int fadeIn, out int stay, out int fadeOut)
    {
      fadeIn = 0;
      stay = 0;
      fadeOut = 0;

      if (!int.TryParse(fadeInRaw, out fadeIn))
        return false;
      if (!int.TryParse(stayRaw, out stay))
        return false;
      if (!int.TryParse(fadeOutRaw, out fadeOut))
        return false;

      if (fadeIn < 0 || stay < 0 || fadeOut < 0)
        return false;

      return true;
    }
  }
}
