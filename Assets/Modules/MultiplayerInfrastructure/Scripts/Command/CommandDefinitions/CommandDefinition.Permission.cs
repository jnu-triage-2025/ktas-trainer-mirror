using System;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Permission;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// /permission 커맨드 — 권한(role/user) 관리.
  ///
  /// 서브커맨드 목록:
  ///   role list
  ///   role info &lt;role&gt;
  ///   role add &lt;role&gt;
  ///   role remove &lt;role&gt;
  ///   role default [role]
  ///   role set &lt;role&gt; perm add &lt;permission&gt;
  ///   role set &lt;role&gt; perm remove &lt;permission&gt;
  ///   role set &lt;role&gt; contains add &lt;role2&gt;
  ///   role set &lt;role&gt; contains remove &lt;role2&gt;
  ///   user get &lt;player&gt;
  ///   user set &lt;player&gt; &lt;role&gt;
  ///   reset
  /// </summary>
  public class CommandDefinition_Permission : IChatCommandModel, IChatCommandUsage
  {
    public string CommandEntry => "permission";
    public string Description => "Manage command permissions and roles.";

    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("permission role list",                         "List all roles."),
      new UsageLine("permission role info <role>",                  "Show a role's permissions and inheritance."),
      new UsageLine("permission role add <role>",                   "Create a new role."),
      new UsageLine("permission role remove <role>",                "Delete a role."),
      new UsageLine("permission role default [role]",               "Get or set the default role."),
      new UsageLine("permission role set <role> perm add <perm>",   "Grant a permission to a role."),
      new UsageLine("permission role set <role> perm remove <perm>","Revoke a permission from a role."),
      new UsageLine("permission role set <role> contains add <r2>", "Add role inheritance."),
      new UsageLine("permission role set <role> contains remove <r2>","Remove role inheritance."),
      new UsageLine("permission user get <player>",                 "Show a player's current role."),
      new UsageLine("permission user set <player> <role>",          "Assign a role to a player."),
      new UsageLine("permission reset",                             "Reset permissions.json to defaults."),
      new UsageLine("  <player>", "Display name, id:<uuid>, or name:<displayName>."),
      new UsageLine("  <perm>",   "Permission identifier, e.g. 'scenario', 'tp', '*', 'scenario.*'."),
    };

    public string PermissionIdentifier => "permission";

    private readonly ChatService _chat;

    public CommandDefinition_Permission(ChatService chat)
    {
      _chat = chat;
    }

    // ── Execute ──────────────────────────────────────────────────────────────

    public void Execute(NetworkConnection sender, string[] args)
    {
      if (_chat == null)
        return;

      if (args == null || args.Length == 0)
      {
        SendUsage(sender);
        return;
      }

      string sub = args[0].ToLowerInvariant();
      string[] rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

      switch (sub)
      {
        case "role":
          HandleRole(sender, rest);
          return;
        case "user":
          HandleUser(sender, rest);
          return;
        case "reset":
          HandleReset(sender);
          return;
        default:
          SendUsage(sender);
          return;
      }
    }

    // ── /permission role ─────────────────────────────────────────────────────

    private void HandleRole(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission role <list|info|add|remove|default|set>");
        return;
      }

      string action = args[0].ToLowerInvariant();
      string[] rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

      switch (action)
      {
        case "list":
          HandleRoleList(sender);
          return;
        case "info":
          HandleRoleInfo(sender, rest);
          return;
        case "add":
          HandleRoleAdd(sender, rest);
          return;
        case "remove":
          HandleRoleRemove(sender, rest);
          return;
        case "default":
          HandleRoleDefault(sender, rest);
          return;
        case "set":
          HandleRoleSet(sender, rest);
          return;
        default:
          _chat.SendSystemMessage(sender, $"Unknown role sub-command '{args[0]}'. Use: list, info, add, remove, default, set.");
          return;
      }
    }

    private void HandleRoleList(NetworkConnection sender)
    {
      var roles = PermissionService.GetRoles();
      string defaultRole = PermissionService.GetDefaultRole();

      if (roles.Count == 0)
      {
        _chat.SendSystemMessage(sender, "No roles defined.");
        return;
      }

      var lines = roles.Select(r => r == defaultRole ? $"{r} (default)" : r);
      _chat.SendSystemMessage(sender, $"Roles ({roles.Count}):\n" + string.Join("\n", lines.Select(l => $"  {l}")));
    }

    private void HandleRoleInfo(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission role info <role>");
        return;
      }

      string roleName = args[0];
      if (!PermissionService.TryGetRoleDefinition(roleName, out var perms, out var contains, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      string permList = perms.Count > 0 ? string.Join(", ", perms) : "(none)";
      string containsList = contains.Count > 0 ? string.Join(", ", contains) : "(none)";
      string defaultRole = PermissionService.GetDefaultRole();
      string defaultTag = string.Equals(roleName, defaultRole, StringComparison.OrdinalIgnoreCase) ? " [default]" : "";

      _chat.SendSystemMessage(sender,
        $"Role: {roleName}{defaultTag}\n" +
        $"  inherits: {containsList}\n" +
        $"  permissions: {permList}");
    }

    private void HandleRoleAdd(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission role add <role>");
        return;
      }

      string roleName = args[0];
      if (!PermissionService.AddRole(roleName, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"Role '{roleName}' created.");
    }

    private void HandleRoleRemove(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission role remove <role>");
        return;
      }

      string roleName = args[0];
      if (!PermissionService.RemoveRole(roleName, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"Role '{roleName}' removed.");
    }

    private void HandleRoleDefault(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, $"Default role: {PermissionService.GetDefaultRole()}");
        return;
      }

      string roleName = args[0];
      if (!PermissionService.SetDefaultRole(roleName, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"Default role set to '{roleName}'.");
    }

    /// /permission role set <role> perm add|remove <perm>
    /// /permission role set <role> contains add|remove <role2>
    private void HandleRoleSet(NetworkConnection sender, string[] args)
    {
      // args: <role> <perm|contains> <add|remove> <value>
      if (args.Length < 4)
      {
        _chat.SendSystemMessage(sender,
          "Usage: /permission role set <role> <perm|contains> <add|remove> <value>");
        return;
      }

      string roleName = args[0];
      string fieldType = args[1].ToLowerInvariant();
      string subAction = args[2].ToLowerInvariant();
      string value = args[3];

      if (fieldType == "perm" || fieldType == "permission" || fieldType == "permissions")
      {
        if (subAction == "add")
        {
          if (!PermissionService.AddPermissionToRole(roleName, value, out string error))
          {
            _chat.SendSystemMessage(sender, error);
            return;
          }
          _chat.SendSystemMessage(sender, $"Added permission '{value}' to role '{roleName}'.");
        }
        else if (subAction == "remove")
        {
          if (!PermissionService.RemovePermissionFromRole(roleName, value, out string error))
          {
            _chat.SendSystemMessage(sender, error);
            return;
          }
          _chat.SendSystemMessage(sender, $"Removed permission '{value}' from role '{roleName}'.");
        }
        else
        {
          _chat.SendSystemMessage(sender, "Use 'add' or 'remove'.");
        }
        return;
      }

      if (fieldType == "contains" || fieldType == "inherit" || fieldType == "inherits")
      {
        if (!PermissionService.TryGetRoleDefinition(roleName, out var perms, out var contains, out string defError))
        {
          _chat.SendSystemMessage(sender, defError);
          return;
        }

        var newContains = new System.Collections.Generic.List<string>(contains);

        if (subAction == "add")
        {
          if (!PermissionService.GetRoles().Any(r => string.Equals(r, value, StringComparison.OrdinalIgnoreCase)))
          {
            _chat.SendSystemMessage(sender, $"Role '{value}' does not exist.");
            return;
          }

          if (newContains.Contains(value, StringComparer.OrdinalIgnoreCase))
          {
            _chat.SendSystemMessage(sender, $"Role '{roleName}' already inherits '{value}'.");
            return;
          }

          newContains.Add(value);
        }
        else if (subAction == "remove")
        {
          int removed = newContains.RemoveAll(r => string.Equals(r, value, StringComparison.OrdinalIgnoreCase));
          if (removed == 0)
          {
            _chat.SendSystemMessage(sender, $"Role '{roleName}' does not inherit '{value}'.");
            return;
          }
        }
        else
        {
          _chat.SendSystemMessage(sender, "Use 'add' or 'remove'.");
          return;
        }

        if (!PermissionService.SetRolePermissions(roleName, perms, newContains, out string error))
        {
          _chat.SendSystemMessage(sender, error);
          return;
        }

        _chat.SendSystemMessage(sender,
          subAction == "add"
            ? $"Role '{roleName}' now inherits '{value}'."
            : $"Role '{roleName}' no longer inherits '{value}'.");
        return;
      }

      _chat.SendSystemMessage(sender, "Field must be 'perm' or 'contains'.");
    }

    // ── /permission user ──────────────────────────────────────────────────────

    private void HandleUser(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission user <get|set> ...");
        return;
      }

      string action = args[0].ToLowerInvariant();
      string[] rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

      switch (action)
      {
        case "get":
          HandleUserGet(sender, rest);
          return;
        case "set":
          HandleUserSet(sender, rest);
          return;
        default:
          _chat.SendSystemMessage(sender, "Use 'get' or 'set'.");
          return;
      }
    }

    private void HandleUserGet(NetworkConnection sender, string[] args)
    {
      if (args.Length == 0)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission user get <player>");
        return;
      }

      if (!TryResolveUserDescriptor(args[0], sender, out var descriptor, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      string role = PermissionService.GetUserRole(descriptor.Identifier);
      _chat.SendSystemMessage(sender, $"{descriptor.DisplayName} → role: {role}");
    }

    private void HandleUserSet(NetworkConnection sender, string[] args)
    {
      if (args.Length < 2)
      {
        _chat.SendSystemMessage(sender, "Usage: /permission user set <player> <role>");
        return;
      }

      if (!TryResolveUserDescriptor(args[0], sender, out var descriptor, out string resolveError))
      {
        _chat.SendSystemMessage(sender, resolveError);
        return;
      }

      string roleName = args[1];
      if (!PermissionService.SetUserRole(descriptor.Identifier, roleName, out string error))
      {
        _chat.SendSystemMessage(sender, error);
        return;
      }

      _chat.SendSystemMessage(sender, $"Set role of '{descriptor.DisplayName}' to '{roleName}'.");
    }

    // ── /permission reset ─────────────────────────────────────────────────────

    private void HandleReset(NetworkConnection sender)
    {
      PermissionService.ResetToDefaults();
      _chat.SendSystemMessage(sender, "permissions.json reset to defaults.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool TryResolveUserDescriptor(string playerToken, NetworkConnection sender,
                                                  out UserDescriptor descriptor, out string error)
    {
      descriptor = null;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(playerToken))
      {
        error = "Player token is required.";
        return false;
      }

      // @s / @self → sender
      if (string.Equals(playerToken, "@s", StringComparison.OrdinalIgnoreCase)
          || string.Equals(playerToken, "@self", StringComparison.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = "@s cannot be used from system execution.";
          return false;
        }

        if (!UserDescriptorService.TryGetByClientId(sender.ClientId, out descriptor))
        {
          error = "Unable to find your user descriptor.";
          return false;
        }

        return true;
      }

      // id:<uuid>
      if (playerToken.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
      {
        string uid = playerToken.Substring("id:".Length);
        if (!UserDescriptorService.TryGetByIdentifier(uid, out descriptor))
        {
          error = $"No player found with identifier '{uid}'.";
          return false;
        }

        return true;
      }

      // name:<displayName>
      if (playerToken.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
      {
        string name = playerToken.Substring("name:".Length);
        if (!UserDescriptorService.TryGetByDisplayName(name, out descriptor))
        {
          error = $"No player found with name '{name}'.";
          return false;
        }

        return true;
      }

      // Fallback: try identifier, then display name
      if (UserDescriptorService.TryGetByIdentifier(playerToken, out descriptor))
        return true;

      if (UserDescriptorService.TryGetByDisplayName(playerToken, out descriptor))
        return true;

      error = $"Player '{playerToken}' not found. Use id:<uuid> or name:<displayName>.";
      return false;
    }

    private void SendUsage(NetworkConnection sender)
    {
      _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
    }
  }
}
