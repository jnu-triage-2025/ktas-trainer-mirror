using System;
using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 커맨드의 플레이어 인자를 사용자 설명자로 해석한다.
  ///
  /// 허용 토큰:
  ///   @a / @p / @r / @s / @e / @n  — 대상 선택자. [tag=..], [distance=..] 등 인자를 붙일 수 있다.
  ///   @self                        — @s의 별칭.
  ///   id:&lt;uuid&gt;                    — 사용자 식별자.
  ///   name:&lt;displayName&gt;           — 표시 이름.
  ///   그 외                         — 식별자로 먼저 찾고, 없으면 표시 이름으로 찾는다.
  ///
  /// 선택자는 스폰된 플레이어만 매칭하므로, 접속하지 않은 사용자는 식별자나 표시 이름으로 지정해야 한다.
  /// </summary>
  public static class PlayerTargetResolver
  {
    /// <summary>도움말·오류 메시지에 사용하는 토큰 문법 안내.</summary>
    public const string TokenSyntaxHint = "@selector (@a, @p, @r, @s), id:<uuid>, name:<displayName>, or a display name";

    /// <summary>토큰이 대상 선택자 형식인지 여부.</summary>
    public static bool IsSelector(string token)
      => !string.IsNullOrWhiteSpace(token) && token.TrimStart().StartsWith('@');

    /// <summary>토큰이 가리키는 모든 사용자를 해석한다.</summary>
    public static bool TryResolve(
      NetworkConnection sender,
      string token,
      out List<UserDescriptor> descriptors,
      out string error)
    {
      descriptors = new List<UserDescriptor>();
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(token))
      {
        error = "Player token is required.";
        return false;
      }

      string trimmed = token.Trim();

      if (trimmed.StartsWith('@'))
        return TryResolveSelector(sender, trimmed, out descriptors, out error);

      if (trimmed.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
      {
        string identifier = trimmed.Substring("id:".Length);
        if (!UserDescriptorService.TryGetByIdentifier(identifier, out var byIdentifier))
        {
          error = $"No player found with identifier '{identifier}'.";
          return false;
        }

        descriptors.Add(byIdentifier);
        return true;
      }

      if (trimmed.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
      {
        string displayName = trimmed.Substring("name:".Length);
        if (!UserDescriptorService.TryGetByDisplayName(displayName, out var byDisplayName))
        {
          error = $"No player found with name '{displayName}'.";
          return false;
        }

        descriptors.Add(byDisplayName);
        return true;
      }

      // 폴백: 식별자로 먼저 찾고, 없으면 표시 이름으로 찾는다.
      if (UserDescriptorService.TryGetByIdentifier(trimmed, out var descriptor)
          || UserDescriptorService.TryGetByDisplayName(trimmed, out descriptor))
      {
        descriptors.Add(descriptor);
        return true;
      }

      error = $"Player '{trimmed}' not found. Use {TokenSyntaxHint}.";
      return false;
    }

    /// <summary>대상이 정확히 한 명이어야 하는 커맨드용 해석.</summary>
    public static bool TryResolveSingle(
      NetworkConnection sender,
      string token,
      out UserDescriptor descriptor,
      out string error)
    {
      descriptor = null;

      if (!TryResolve(sender, token, out var descriptors, out error))
        return false;

      if (descriptors.Count > 1)
      {
        error = $"Target selector '{token.Trim()}' matched {descriptors.Count} players; expected 1.";
        return false;
      }

      descriptor = descriptors[0];
      return true;
    }

    private static bool TryResolveSelector(
      NetworkConnection sender,
      string token,
      out List<UserDescriptor> descriptors,
      out string error)
    {
      descriptors = new List<UserDescriptor>();
      error = string.Empty;

      string normalized = NormalizeSelfAlias(token);

      // 인자가 없는 @s는 세션만으로 해석해, 아직 플레이어가 스폰되지 않은 실행자도 자신을 지정할 수 있게 한다.
      if (string.Equals(normalized, "@s", StringComparison.OrdinalIgnoreCase))
      {
        if (sender == null)
        {
          error = "@s cannot be used from system execution.";
          return false;
        }

        if (!UserDescriptorService.TryGetByClientId(sender.ClientId, out var self) || self == null)
        {
          error = "Unable to find your user descriptor.";
          return false;
        }

        descriptors.Add(self);
        return true;
      }

      if (!TargetSelectorResolver.TryResolveTargets(sender, normalized, out var targets, out error))
        return false;

      foreach (NetworkConnection connection in targets)
      {
        if (connection == null)
          continue;

        if (!UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor) || descriptor == null)
          continue;

        if (!descriptors.Exists(existing => string.Equals(existing.Identifier, descriptor.Identifier, StringComparison.Ordinal)))
          descriptors.Add(descriptor);
      }

      if (descriptors.Count == 0)
      {
        error = $"No player session matched the selector '{token}'.";
        return false;
      }

      return true;
    }

    /// <summary>@self[...] 형태를 @s[...]로 바꾼다.</summary>
    private static string NormalizeSelfAlias(string token)
    {
      const string selfPrefix = "@self";
      if (!token.StartsWith(selfPrefix, StringComparison.OrdinalIgnoreCase))
        return token;

      return "@s" + token.Substring(selfPrefix.Length);
    }
  }
}
