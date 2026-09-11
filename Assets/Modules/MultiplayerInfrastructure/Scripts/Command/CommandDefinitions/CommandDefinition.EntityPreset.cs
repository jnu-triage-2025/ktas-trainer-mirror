using System;
using System.Globalization;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public class CommandDefinition_EntityPreset : IChatCommandModel, IChatCommandPipelineCommand, IChatCommandUsage
  {
    public string CommandEntry => "entitypreset";
    public string Description => "Spawn and manage entity presets.";
    public System.Collections.Generic.IReadOnlyList<UsageLine> UsageLines => new[]
    {
      new UsageLine("entitypreset list", "List available entity presets."),
      new UsageLine("entitypreset spawn <preset> <x> <y> <z>", "Spawn at world coordinates."),
      new UsageLine("entitypreset spawn <preset> <target>", "Spawn at a target's position."),
      new UsageLine("  <preset>", "Registered entity preset identifier."),
      new UsageLine("  <target>", PlayerTargetResolver.ShortSyntaxHint + ", or entity id."),
    };
    public string PermissionIdentifier => "entitypreset";

    private readonly ChatService _chat;

    public CommandDefinition_EntityPreset(ChatService chat)
    {
      _chat = chat;
    }

    public void Execute(NetworkConnection sender, string[] args)
    {
      TryExecute(sender, args, suppressSystemMessages: false, out _, out _);
    }

    public bool TryExecute(
      NetworkConnection sender,
      string[] args,
      bool suppressSystemMessages,
      out System.Collections.Generic.IReadOnlyList<string> pipelineValues,
      out string error)
    {
      pipelineValues = Array.Empty<string>();
      error = string.Empty;

      if (_chat == null)
      {
        error = "Chat service is unavailable.";
        return false;
      }

      if (args == null || args.Length == 0)
      {
        if (!suppressSystemMessages)
          SendUsage(sender);
        error = "Usage: /entitypreset list | /entitypreset spawn <identifier> <x> <y> <z> | /entitypreset spawn <identifier> <target>";
        return false;
      }

      if (string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
      {
        if (!HandleList(sender, args, suppressSystemMessages, out var listError))
        {
          error = listError;
          return false;
        }

        return true;
      }

      if (!string.Equals(args[0], "spawn", StringComparison.OrdinalIgnoreCase) || args.Length < 3)
      {
        if (!suppressSystemMessages)
          SendUsage(sender);
        error = "Usage: /entitypreset spawn <identifier> <x> <y> <z> | /entitypreset spawn <identifier> <target>";
        return false;
      }

      string presetIdentifier = args[1];
      if (string.IsNullOrWhiteSpace(presetIdentifier))
      {
        error = "Entity preset identifier is required.";
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      if (args.Length == 5)
      {
        if (!TryParsePosition(args[2], args[3], args[4], out var position))
        {
          error = "Invalid position. Usage: /entitypreset spawn <identifier> <x> <y> <z>";
          if (!suppressSystemMessages)
            _chat.SendSystemMessage(sender, error);
          return false;
        }

        return TrySpawnAndNotify(sender, presetIdentifier, position, suppressSystemMessages, out pipelineValues, out error);
      }

      if (args.Length == 3)
      {
        if (!TryResolveTargetPosition(sender, args[2], out var position, out var resolveError))
        {
          error = resolveError;
          if (!suppressSystemMessages)
            _chat.SendSystemMessage(sender, error);
          return false;
        }

        return TrySpawnAndNotify(sender, presetIdentifier, position, suppressSystemMessages, out pipelineValues, out error);
      }

      error = "Usage: /entitypreset spawn <identifier> <x> <y> <z> | /entitypreset spawn <identifier> <target>";
      if (!suppressSystemMessages)
        SendUsage(sender);
      return false;
    }

    private bool HandleList(NetworkConnection sender, string[] args, bool suppressSystemMessages, out string error)
    {
      error = string.Empty;
      if (args.Length != 1)
      {
        error = "Usage: /entitypreset list";
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      var presets = Registry.Registry.GetAllEntityPresets();
      if (presets == null || presets.Count == 0)
      {
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, "No entity presets are registered.");
        return true;
      }

      var identifiers = presets.Keys
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .OrderBy(id => id, StringComparer.Ordinal)
        .ToArray();

      if (identifiers.Length == 0)
      {
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, "No entity presets are registered.");
        return true;
      }

      if (!suppressSystemMessages)
        _chat.SendSystemMessage(sender, $"Available entity presets ({identifiers.Length}): {string.Join(", ", identifiers)}");
      return true;
    }

    private bool TrySpawnAndNotify(
      NetworkConnection sender,
      string presetIdentifier,
      Vector3 position,
      bool suppressSystemMessages,
      out System.Collections.Generic.IReadOnlyList<string> pipelineValues,
      out string error)
    {
      pipelineValues = Array.Empty<string>();
      error = string.Empty;

      if (!Registry.Registry.TrySpawnEntityPreset(presetIdentifier, position, Quaternion.identity, out _, out var descriptor, out var spawnError))
      {
        error = spawnError;
        if (!suppressSystemMessages)
          _chat.SendSystemMessage(sender, error);
        return false;
      }

      pipelineValues = new[] { descriptor.Identifier };

      if (!suppressSystemMessages)
      {
        _chat.SendSystemNotification(sender, $"엔티티 프리셋 {presetIdentifier}이(가) {descriptor.Identifier}로 스폰되었습니다.");
      }

      return true;
    }

    private static bool TryParsePosition(string rawX, string rawY, string rawZ, out Vector3 position)
    {
      position = Vector3.zero;
      if (!float.TryParse(rawX, NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
        return false;
      if (!float.TryParse(rawY, NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        return false;
      if (!float.TryParse(rawZ, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        return false;

      position = new Vector3(x, y, z);
      return true;
    }

    private bool TryResolveTargetPosition(NetworkConnection sender, string target, out Vector3 position, out string error)
    {
      position = Vector3.zero;
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(target))
      {
        error = "Target is required.";
        return false;
      }

      if (PlayerTargetResolver.TryResolveSingleConnection(sender, target, out var connection, out string playerError))
        return TryResolveConnectionPosition(connection, out position, out error);

      // 플레이어가 아니어도 레지스트리에 등록된 엔티티라면 그 위치를 쓴다.
      if (Registry.Registry.TryGetEntity(PlayerNameQuery.Normalize(target), out var descriptor)
          && descriptor?.GameObject != null)
      {
        position = descriptor.GameObject.transform.position;
        return true;
      }

      error = playerError;
      return false;
    }

    private static bool TryResolveConnectionPosition(NetworkConnection connection, out Vector3 position, out string error)
    {
      position = Vector3.zero;
      error = string.Empty;

      if (connection == null)
      {
        error = "Target connection is not available.";
        return false;
      }

      if (!PlayerTargetResolver.TryGetController(connection, out PlayerController controller))
      {
        error = "Target player is not available.";
        return false;
      }

      position = controller.transform.position;
      return true;
    }

    private void SendUsage(NetworkConnection sender)
    {
      _chat.SendSystemMessage(sender, ChatCommandHelp.GetHelpPage(this));
    }
  }
}
