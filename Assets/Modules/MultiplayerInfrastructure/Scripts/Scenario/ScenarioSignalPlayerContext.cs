using System;
using System.Collections.Generic;
using FishNet.Connection;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 서버 권위 콜백 안에서 실제 행동 플레이어를 신호 발신자로 보존하는 일시적 컨텍스트.
  /// 네트워크 요청자 정보를 잃는 하위 콜백은 이 범위 안에서 <see cref="ScenarioInteractionSignals.Raise"/>를 호출한다.
  /// </summary>
  public static class ScenarioSignalPlayerContext
  {
    private static readonly Stack<Context> Contexts = new();

    internal static bool TryGetCurrent(out string playerIdentifier, out string playerDisplayName)
    {
      if (Contexts.Count > 0)
      {
        Context context = Contexts.Peek();
        playerIdentifier = context.PlayerIdentifier;
        playerDisplayName = context.PlayerDisplayName;
        return true;
      }
      playerIdentifier = null;
      playerDisplayName = null;
      return false;
    }

    public static IDisposable Push(NetworkConnection connection)
    {
      string identifier = ScenarioSignalParameterStore.ServerPlayerIdentifier;
      string displayName = identifier;
      if (connection != null && UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor))
      {
        identifier = descriptor.Identifier;
        displayName = descriptor.DisplayName;
      }
      return Push(identifier, displayName);
    }

    /// <summary>서버 코드가 이미 확인한 플레이어 식별자로 신호 발신자 범위를 연다.</summary>
    public static IDisposable Push(string playerIdentifier, string playerDisplayName)
    {
      string identifier = string.IsNullOrWhiteSpace(playerIdentifier)
        ? ScenarioSignalParameterStore.ServerPlayerIdentifier
        : playerIdentifier.Trim();
      string displayName = string.IsNullOrWhiteSpace(playerDisplayName) ? identifier : playerDisplayName.Trim();
      Contexts.Push(new Context(identifier, displayName));
      return new Scope();
    }

    private readonly struct Context
    {
      public string PlayerIdentifier { get; }
      public string PlayerDisplayName { get; }

      public Context(string playerIdentifier, string playerDisplayName)
      {
        PlayerIdentifier = playerIdentifier;
        PlayerDisplayName = playerDisplayName;
      }
    }

    private sealed class Scope : IDisposable
    {
      private bool _disposed;

      public void Dispose()
      {
        if (_disposed)
          return;
        _disposed = true;
        if (Contexts.Count > 0)
          Contexts.Pop();
      }
    }
  }
}
