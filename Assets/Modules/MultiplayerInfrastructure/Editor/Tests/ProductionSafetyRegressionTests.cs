using System.Collections;
using System.Reflection;
using System.Threading;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Variable;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests
{
  public sealed class ProductionSafetyRegressionTests
  {
    [Test]
    public void ConnectionTimeoutsOverrideDisabledDetectionForBothPeers()
    {
      var go = new GameObject("connection-timeout-test");
      go.SetActive(false);
      try
      {
        var manager = go.AddComponent<FishNet.Managing.NetworkManager>();
        var client = go.AddComponent<FishNet.Managing.Client.ClientManager>();
        var server = go.AddComponent<FishNet.Managing.Server.ServerManager>();
        var transportManager = go.AddComponent<FishNet.Managing.Transporting.TransportManager>();
        var transport = go.AddComponent<FishNet.Transporting.Tugboat.Tugboat>();
        transportManager.Transport = transport;
        typeof(FishNet.Managing.NetworkManager).GetProperty("ClientManager").SetValue(manager, client);
        typeof(FishNet.Managing.NetworkManager).GetProperty("ServerManager").SetValue(manager, server);
        typeof(FishNet.Managing.NetworkManager).GetProperty("TransportManager").SetValue(manager, transportManager);
        client.SetRemoteServerTimeout(FishNet.Managing.RemoteTimeoutType.Disabled, 180);
        server.SetRemoteClientTimeout(FishNet.Managing.RemoteTimeoutType.Disabled, 180);
        var support = go.AddComponent<MultiplayerInfrastructure.FishNetSupports.FishNetSupport>();
        var supportType = support.GetType();
        supportType.GetField("networkManager", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(support, manager);
        supportType.GetMethod("ConfigureConnectionTimeouts", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(support, null);

        Assert.That(ReadPrivate(client, "_remoteServerTimeoutDuration"), Is.EqualTo(15));
        Assert.That(ReadPrivate(server, "_remoteClientTimeoutDuration"), Is.EqualTo(15));
        Assert.That(ReadPrivate(client, "_remoteServerTimeout"), Is.EqualTo(FishNet.Managing.RemoteTimeoutType.Development));
        Assert.That(ReadPrivate(server, "_remoteClientTimeout"), Is.EqualTo(FishNet.Managing.RemoteTimeoutType.Development));
        // Tugboat.GetTimeout reports the maximum supported value, not the configured value.
        Assert.That(ReadPrivate(transport, "_clientTimeout"), Is.EqualTo(15));
        Assert.That(ReadPrivate(transport, "_serverTimeout"), Is.EqualTo(15));
      }
      finally
      {
        Object.DestroyImmediate(go);
      }
    }

    private static object ReadPrivate(object target, string name)
      => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    [TestCase("  Alice  ", "Alice")]
    [TestCase("한글 이름", "한글 이름")]
    public void DisplayNameNormalizationTrimsValidNames(string input, string expected)
    {
      Assert.That(UserDescriptorService.TryNormalizeDisplayName(input, out string normalized, out _), Is.True);
      Assert.That(normalized, Is.EqualTo(expected));
    }

    [Test]
    public void DisplayNameNormalizationRejectsControlCharactersAndOversizeNames()
    {
      Assert.That(UserDescriptorService.TryNormalizeDisplayName("Alice\nAdmin", out _, out _), Is.False);
      Assert.That(UserDescriptorService.TryNormalizeDisplayName(
        new string('a', UserDescriptorService.MaxDisplayNameLength + 1), out _, out _), Is.False);
    }

    [Test]
    public void DisplayNameLookupUsesStableIdentifierTieBreakForLegacyDuplicates()
    {
      const string laterIdentifier = "zzzzzzzz-0000-0000-0000-000000000000";
      const string earlierIdentifier = "aaaaaaaa-0000-0000-0000-000000000000";
      UserDescriptorService.Register(7, new UserDescriptor(laterIdentifier, "duplicate"));
      UserDescriptorService.Register(8, new UserDescriptor(earlierIdentifier, "duplicate"));
      try
      {
        Assert.That(UserDescriptorService.TryGetByDisplayName("duplicate", out var result), Is.True);
        Assert.That(result.Identifier, Is.EqualTo(earlierIdentifier));
      }
      finally
      {
        UserDescriptorService.Unregister(laterIdentifier);
        UserDescriptorService.Unregister(earlierIdentifier);
      }
    }

    [Test]
    public void JoinMessageIsBroadcastOnlyAfterClientDisplayNameIsAccepted()
    {
      string source = System.IO.File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Network.cs");

      int serverStart = source.IndexOf("public override void OnStartServer()", System.StringComparison.Ordinal);
      int clientNameCommand = source.IndexOf("private void CmdSetDisplayName(string displayName)", System.StringComparison.Ordinal);
      int joinMessage = source.IndexOf("BroadcastJoinMessage(normalized);", System.StringComparison.Ordinal);

      Assert.That(serverStart, Is.GreaterThanOrEqualTo(0));
      Assert.That(clientNameCommand, Is.GreaterThan(serverStart));
      Assert.That(joinMessage, Is.GreaterThan(clientNameCommand));
      Assert.That(source.Substring(serverStart, clientNameCommand - serverStart),
        Does.Not.Contain("이(가) 들어왔습니다."));
    }

    [Test]
    public void ClearSessionStateRemovesObjectivesAndScores()
    {
      var objectives = (IDictionary)typeof(SessionVariableService)
        .GetField("_objectives", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
      var scores = (IDictionary)typeof(SessionVariableService)
        .GetField("_scoresByObjective", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
      objectives.Add("old", new SessionVariableService.ObjectiveDefinition("old", "dummy"));
      scores.Add("old", new System.Collections.Generic.Dictionary<string, int> { ["player"] = 5 });

      SessionVariableService.ClearSessionState();

      Assert.That(objectives.Count, Is.Zero);
      Assert.That(scores.Count, Is.Zero);
    }

    /// <summary>
    /// 권한 식별자를 선언하지 않은 커맨드는 거부되어야 한다. 이전 구현은 빈 식별자를 무조건
    /// 허용했고, 그 결과 데이터팩 별칭이 권한 경계를 우회했다.
    /// </summary>
    [Test]
    public void BlankPermissionIdentifierIsDenied()
    {
      Assert.That(Permission.PermissionService.HasPermission("any-user", null), Is.False);
      Assert.That(Permission.PermissionService.HasPermission("any-user", string.Empty), Is.False);
      Assert.That(Permission.PermissionService.HasPermission("any-user", "   "), Is.False);
    }

    /// <summary>
    /// 기본 role 은 매핑이 없는 모든 접속자에게 부여되므로, 다른 참가자나 세션 전체에
    /// 영향을 주는 권한을 포함해서는 안 된다.
    /// </summary>
    [Test]
    public void DefaultRoleDoesNotGrantSessionDisruptingPermissions()
    {
      MethodInfo buildDefaults = typeof(Permission.PermissionService).GetMethod(
        "BuildDefaultFile", BindingFlags.Static | BindingFlags.NonPublic);
      Assert.That(buildDefaults, Is.Not.Null);

      object file = buildDefaults.Invoke(null, null);
      string defaultRole = (string)file.GetType().GetField("default").GetValue(file);
      var permissions = (IDictionary)file.GetType().GetField("permissions").GetValue(file);

      Assert.That(defaultRole, Is.EqualTo("user"));

      var effective = new System.Collections.Generic.HashSet<string>(
        System.StringComparer.OrdinalIgnoreCase);
      CollectRolePermissions(permissions, defaultRole, effective,
        new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase));

      foreach (string forbidden in new[]
        { "kick", "ban", "server", "permission", "tp", "clean", "entitypreset", "gamerule", "conngate" })
      {
        Assert.That(effective, Does.Not.Contain(forbidden),
          $"Default role must not grant '{forbidden}'.");
      }
    }

    private static void CollectRolePermissions(
      IDictionary permissions,
      string role,
      System.Collections.Generic.HashSet<string> result,
      System.Collections.Generic.HashSet<string> visited)
    {
      if (string.IsNullOrWhiteSpace(role) || !visited.Add(role) || !permissions.Contains(role))
        return;

      object definition = permissions[role];
      var granted = (System.Collections.Generic.List<string>)definition.GetType()
        .GetField("permissions").GetValue(definition);
      var inherited = (System.Collections.Generic.List<string>)definition.GetType()
        .GetField("contains").GetValue(definition);

      if (granted != null)
      {
        foreach (string entry in granted)
          result.Add(entry);
      }

      if (inherited == null)
        return;

      foreach (string parent in inherited)
        CollectRolePermissions(permissions, parent, result, visited);
    }

    /// <summary>
    /// 데이터팩 별칭은 고유 권한을 갖지 않으며, 권한 검사를 통과시키는 빈 식별자를
    /// 다시 도입해서는 안 된다.
    /// </summary>
    [Test]
    public void DatapackAliasDoesNotDeclareBlankPermissionIdentifier()
    {
      string source = System.IO.File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Command/DatapackCommandAlias.cs");
      Assert.That(source, Does.Not.Contain("PermissionIdentifier => string.Empty"));
      Assert.That(source, Does.Contain("AliasPermissionIdentifier"));

      string dispatcher = System.IO.File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Command/CommandService.cs");
      Assert.That(dispatcher, Does.Contain("HasAllAliasTargetPermissions"));
    }

    [Test]
    public void LanDiscoveryStopToleratesAlreadyDisposedCancellationSource()
    {
      var gameObject = new GameObject("LAN Discovery Test");
      try
      {
        var service = gameObject.AddComponent<LanDiscoveryService>();
        var cancellation = new CancellationTokenSource();
        cancellation.Dispose();
        typeof(LanDiscoveryService)
          .GetField("_listenCts", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.SetValue(service, cancellation);

        Assert.DoesNotThrow(service.StopDiscovery);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }
}
