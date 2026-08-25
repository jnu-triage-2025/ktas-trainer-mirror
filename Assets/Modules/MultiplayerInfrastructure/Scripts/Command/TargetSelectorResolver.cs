using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace MultiplayerInfrastructure.Command
{
  public static class TargetSelectorResolver
  {
    private const string PlayerType = "player";

    private class SelectorArgs
    {
      public float? X;
      public float? Y;
      public float? Z;
      public float? Dx;
      public float? Dy;
      public float? Dz;
      public FloatRange? Distance;
      public readonly List<FilterString> Tags = new();
      public readonly List<FilterString> Types = new();

      public bool HasAnyCoordinate => X.HasValue || Y.HasValue || Z.HasValue;
      public bool HasBox => Dx.HasValue || Dy.HasValue || Dz.HasValue;
    }

    private readonly struct SelectorRequest
    {
      public readonly string Selector;
      public readonly SelectorArgs Args;

      public SelectorRequest(string selector, SelectorArgs args)
      {
        Selector = selector;
        Args = args;
      }
    }

    private readonly struct FilterString
    {
      public readonly string Value;
      public readonly bool Negated;

      public FilterString(string value, bool negated)
      {
        Value = value;
        Negated = negated;
      }
    }

    private readonly struct FloatRange
    {
      public readonly bool HasMin;
      public readonly bool HasMax;
      public readonly float Min;
      public readonly float Max;

      public FloatRange(float min, float max, bool hasMin, bool hasMax)
      {
        Min = min;
        Max = max;
        HasMin = hasMin;
        HasMax = hasMax;
      }

      public bool Contains(float value)
      {
        if (HasMin && value < Min)
          return false;
        if (HasMax && value > Max)
          return false;
        return true;
      }
    }

    private readonly struct PlayerInfo
    {
      public readonly NetworkConnection Connection;
      public readonly PlayerController Controller;
      public readonly Vector3 Position;

      public PlayerInfo(NetworkConnection connection, PlayerController controller)
      {
        Connection = connection;
        Controller = controller;
        Position = controller != null ? controller.transform.position : Vector3.zero;
      }
    }

    public static bool TryResolveTargets(NetworkConnection sender, string raw, out List<NetworkConnection> targets, out string error)
    {
      targets = new List<NetworkConnection>();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(raw))
      {
        error = "Target selector is required.";
        return false;
      }

      if (!raw.StartsWith('@'))
      {
        error = "Target selector must start with '@'.";
        return false;
      }

      if (!TryParseSelector(raw, out SelectorRequest request, out error))
        return false;

      return TryResolveByRequest(sender, request, out targets, out error);
    }

    private static bool TryParseSelector(string raw, out SelectorRequest request, out string error)
    {
      request = default;
      error = string.Empty;

      int bracketIndex = raw.IndexOf('[');
      string selectorPart = bracketIndex >= 0 ? raw.Substring(0, bracketIndex) : raw;
      string argsPart = string.Empty;

      if (bracketIndex >= 0)
      {
        if (!raw.EndsWith(']'))
        {
          error = "Invalid selector arguments.";
          return false;
        }

        argsPart = raw.Substring(bracketIndex + 1, raw.Length - bracketIndex - 2);
      }

      if (selectorPart.Length < 2)
      {
        error = "Invalid selector.";
        return false;
      }

      string selector = selectorPart.Substring(1).ToLowerInvariant();
      if (!IsSupportedSelector(selector))
      {
        error = "Unknown target selector.";
        return false;
      }

      var args = new SelectorArgs();
      if (!string.IsNullOrWhiteSpace(argsPart))
      {
        foreach (string token in SplitArgs(argsPart))
        {
          if (string.IsNullOrWhiteSpace(token))
            continue;

          int eqIndex = token.IndexOf('=');
          if (eqIndex <= 0 || eqIndex == token.Length - 1)
            continue;

          string key = token.Substring(0, eqIndex).Trim();
          string value = token.Substring(eqIndex + 1).Trim();
          value = Unquote(value);

          switch (key)
          {
            case "x":
              if (TryParseFloat(value, out float x))
                args.X = x;
              break;
            case "y":
              if (TryParseFloat(value, out float y))
                args.Y = y;
              break;
            case "z":
              if (TryParseFloat(value, out float z))
                args.Z = z;
              break;
            case "dx":
              if (TryParseFloat(value, out float dx))
                args.Dx = dx;
              break;
            case "dy":
              if (TryParseFloat(value, out float dy))
                args.Dy = dy;
              break;
            case "dz":
              if (TryParseFloat(value, out float dz))
                args.Dz = dz;
              break;
            case "distance":
              if (TryParseRange(value, out FloatRange range))
                args.Distance = range;
              break;
            case "tag":
              args.Tags.Add(ParseFilter(value));
              break;
            case "type":
              args.Types.Add(ParseFilter(value));
              break;
          }
        }
      }

      request = new SelectorRequest(selector, args);
      return true;
    }

    private static bool TryResolveByRequest(NetworkConnection sender, SelectorRequest request, out List<NetworkConnection> targets, out string error)
    {
      targets = new List<NetworkConnection>();
      error = string.Empty;

      if (!ValidateEntitySelectorSupport(request, out error))
        return false;

      var players = GetAllPlayerInfos();

      if (request.Selector == "s")
      {
        if (!TryGetSenderInfo(sender, players, out PlayerInfo senderInfo, out error))
          return false;

        if (!TryBuildOrigin(sender, request.Args, senderInfo.Position, requireSenderOrigin: true, out Vector3 origin, out error))
          return false;

        if (PassFilters(senderInfo, request.Args, origin))
          targets.Add(senderInfo.Connection);
      }
      else
      {
        bool requireOrigin = request.Selector == "p" || request.Selector == "n" || request.Selector == "r"
          || request.Args.HasAnyCoordinate || request.Args.Distance.HasValue || request.Args.HasBox;

        if (!TryBuildOrigin(sender, request.Args, Vector3.zero, requireOrigin, out Vector3 origin, out error))
          return false;

        var filtered = players
          .Where(info => PassFilters(info, request.Args, origin))
          .ToList();

        if (filtered.Count > 0)
        {
          switch (request.Selector)
          {
            case "a":
            case "e":
              targets.AddRange(filtered.Select(info => info.Connection));
              break;
            case "p":
            case "n":
              var nearest = filtered
                .OrderBy(info => (info.Position - origin).sqrMagnitude)
                .First();
              targets.Add(nearest.Connection);
              break;
            case "r":
              int index = UnityEngine.Random.Range(0, filtered.Count);
              targets.Add(filtered[index].Connection);
              break;
          }
        }
      }

      if (targets.Count == 0)
      {
        error = "No targets matched the selector.";
        return false;
      }

      targets = targets
        .Where(t => t != null)
        .GroupBy(t => (int)t.ClientId)
        .Select(g => g.First())
        .ToList();

      if (targets.Count == 0)
      {
        error = "No valid targets matched the selector.";
        return false;
      }

      return true;
    }

    private static bool ValidateEntitySelectorSupport(SelectorRequest request, out string error)
    {
      error = string.Empty;
      bool isEntitySelector = request.Selector == "e" || request.Selector == "n";
      if (!isEntitySelector)
        return true;

      if (request.Args.Types.Count == 0)
      {
        error = "Entity targets are not supported yet. Use @e[type=player] or player selectors.";
        return false;
      }

      foreach (var typeFilter in request.Args.Types)
      {
        if (typeFilter.Negated)
        {
          error = "Entity targets are not supported yet. Use @e[type=player] or player selectors.";
          return false;
        }

        if (!string.Equals(typeFilter.Value, PlayerType, StringComparison.OrdinalIgnoreCase))
        {
          error = "Entity targets are not supported yet. Use @e[type=player] or player selectors.";
          return false;
        }
      }

      return true;
    }

    private static bool TryGetSenderInfo(NetworkConnection sender, List<PlayerInfo> players, out PlayerInfo info, out string error)
    {
      info = default;
      error = string.Empty;

      if (sender == null)
      {
        error = "Unable to locate the command executor.";
        return false;
      }

      foreach (var candidate in players)
      {
        if (candidate.Connection == sender)
        {
          info = candidate;
          return true;
        }
      }

      error = "Unable to locate the command executor's target on the server.";
      return false;
    }

    private static bool TryBuildOrigin(
      NetworkConnection sender,
      SelectorArgs args,
      Vector3 fallbackOrigin,
      bool requireSenderOrigin,
      out Vector3 origin,
      out string error)
    {
      origin = fallbackOrigin;
      error = string.Empty;

      if (requireSenderOrigin || args.HasAnyCoordinate || args.Distance.HasValue || args.HasBox)
      {
        bool missingCoord = !args.X.HasValue || !args.Y.HasValue || !args.Z.HasValue;
        bool needsSender = requireSenderOrigin || args.Distance.HasValue || args.HasBox || missingCoord;

        Vector3 senderPos = fallbackOrigin;
        if (needsSender && !TryGetSenderPosition(sender, out senderPos))
        {
          error = "Unable to resolve selector origin.";
          return false;
        }

        if (needsSender)
        {
          origin = new Vector3(
            args.X ?? senderPos.x,
            args.Y ?? senderPos.y,
            args.Z ?? senderPos.z);
        }
        else
        {
          origin = new Vector3(args.X.Value, args.Y.Value, args.Z.Value);
        }
      }
      else if (args.HasAnyCoordinate)
      {
        origin = new Vector3(
          args.X ?? origin.x,
          args.Y ?? origin.y,
          args.Z ?? origin.z);
      }

      return true;
    }

    private static bool PassFilters(PlayerInfo info, SelectorArgs args, Vector3 origin)
    {
      if (args.Distance.HasValue)
      {
        float dist = Vector3.Distance(origin, info.Position);
        if (!args.Distance.Value.Contains(dist))
          return false;
      }

      if (args.HasBox)
      {
        Vector3 min = new Vector3(
          Mathf.Min(origin.x, origin.x + (args.Dx ?? 0f)),
          Mathf.Min(origin.y, origin.y + (args.Dy ?? 0f)),
          Mathf.Min(origin.z, origin.z + (args.Dz ?? 0f)));
        Vector3 max = new Vector3(
          Mathf.Max(origin.x, origin.x + (args.Dx ?? 0f)),
          Mathf.Max(origin.y, origin.y + (args.Dy ?? 0f)),
          Mathf.Max(origin.z, origin.z + (args.Dz ?? 0f)));

        Vector3 pos = info.Position;
        if (pos.x < min.x || pos.x > max.x || pos.y < min.y || pos.y > max.y || pos.z < min.z || pos.z > max.z)
          return false;
      }

      if (args.Types.Count > 0)
      {
        foreach (var typeFilter in args.Types)
        {
          if (string.Equals(typeFilter.Value, PlayerType, StringComparison.OrdinalIgnoreCase))
          {
            if (typeFilter.Negated)
              return false;
          }
        }
      }

      if (args.Tags.Count > 0)
      {
        var tags = GetTags(info.Connection);

        foreach (var filter in args.Tags)
        {
          if (string.IsNullOrEmpty(filter.Value))
          {
            if (filter.Negated)
            {
              if (tags.Count == 0)
                return false;
            }
            else
            {
              if (tags.Count != 0)
                return false;
            }
            continue;
          }

          bool hasTag = tags.Contains(filter.Value);
          if (filter.Negated && hasTag)
            return false;
          if (!filter.Negated && !hasTag)
            return false;
        }
      }

      return true;
    }

    private static List<PlayerInfo> GetAllPlayerInfos()
    {
      var found = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      var result = new List<PlayerInfo>(found.Length);

      foreach (var player in found)
      {
        if (player == null || player.Owner == null)
          continue;

        result.Add(new PlayerInfo(player.Owner, player));
      }

      return result;
    }

    private static bool TryGetSenderPosition(NetworkConnection sender, out Vector3 position)
    {
      position = Vector3.zero;
      if (sender == null)
        return false;

      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      foreach (var player in players)
      {
        if (player != null && player.Owner == sender)
        {
          position = player.transform.position;
          return true;
        }
      }

      return false;
    }

    private static HashSet<string> GetTags(NetworkConnection connection)
    {
      var tags = new HashSet<string>(StringComparer.Ordinal);
      if (connection == null)
        return tags;

      if (!UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor) || descriptor == null)
        return tags;

      var list = PlayerTagService.GetTagsByIdentifier(descriptor.Identifier);
      foreach (var tag in list)
      {
        if (!string.IsNullOrEmpty(tag))
          tags.Add(tag);
      }

      return tags;
    }

    private static bool IsSupportedSelector(string selector)
    {
      return selector == "p" || selector == "a" || selector == "r" || selector == "s" || selector == "e" || selector == "n";
    }

    private static IEnumerable<string> SplitArgs(string raw)
    {
      var result = new List<string>();
      if (string.IsNullOrWhiteSpace(raw))
        return result;

      bool inQuotes = false;
      int start = 0;

      for (int i = 0; i < raw.Length; i++)
      {
        char c = raw[i];
        if (c == '"')
          inQuotes = !inQuotes;

        if (c == ',' && !inQuotes)
        {
          result.Add(raw.Substring(start, i - start).Trim());
          start = i + 1;
        }
      }

      if (start < raw.Length)
        result.Add(raw.Substring(start).Trim());

      return result;
    }

    private static string Unquote(string value)
    {
      if (string.IsNullOrEmpty(value))
        return value;

      if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        return value.Substring(1, value.Length - 2);

      return value;
    }

    private static FilterString ParseFilter(string raw)
    {
      if (string.IsNullOrEmpty(raw))
        return new FilterString(string.Empty, false);

      if (raw.StartsWith('!'))
        return new FilterString(raw.Substring(1), true);

      return new FilterString(raw, false);
    }

    private static bool TryParseFloat(string raw, out float value)
    {
      return float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseRange(string raw, out FloatRange range)
    {
      range = default;
      if (string.IsNullOrWhiteSpace(raw))
        return false;

      int dots = raw.IndexOf("..", StringComparison.Ordinal);
      if (dots >= 0)
      {
        string left = raw.Substring(0, dots);
        string right = raw.Substring(dots + 2);

        bool hasMin = !string.IsNullOrWhiteSpace(left);
        bool hasMax = !string.IsNullOrWhiteSpace(right);

        float min = 0f;
        float max = 0f;

        if (hasMin && !TryParseFloat(left, out min))
          return false;
        if (hasMax && !TryParseFloat(right, out max))
          return false;

        if (hasMin && hasMax && min > max)
          return false;

        range = new FloatRange(min, max, hasMin, hasMax);
        return true;
      }

      if (!TryParseFloat(raw, out float exact))
        return false;

      range = new FloatRange(exact, exact, true, true);
      return true;
    }
  }
}
