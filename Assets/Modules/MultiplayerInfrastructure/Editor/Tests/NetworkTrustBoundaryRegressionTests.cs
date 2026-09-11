using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MultiplayerInfrastructure.Chat;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Editor.Tests
{
  public sealed class NetworkTrustBoundaryRegressionTests
  {
    [Test]
    public void ChatMessageEscapesMarkupAndAppliesLengthLimit()
    {
      MethodInfo sanitize = typeof(ChatService).GetMethod(
        "SanitizeChatMessage",
        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

      Assert.That(sanitize, Is.Not.Null);
      string result = (string)sanitize.Invoke(null, new object[] { "<b>" + new string('x', 600) });

      Assert.That(result, Does.StartWith("&lt;b&gt;"));
      Assert.That(result, Does.Not.Contain("<b>"));
      Assert.That(result.Length, Is.LessThanOrEqualTo(524));
    }

    [Test]
    public void PlayerSystemMessagesAreBroadcastWithThePlayerNamePrefix()
    {
      string source = File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs");

      Assert.That(source, Does.Contain("ReceiveChatObserversRpc(FormatPlayerSystemMessage(conn, message))"));
      Assert.That(source, Does.Contain("return $\"({GetDisplayName(conn)}) {message}\";"));
      Assert.That(source, Does.Not.Contain("TargetReceiveSystemMessage"));
    }

    [Test]
    public void SystemNotificationsReachEveryObserverEvenUnderSystemExecution()
    {
      string source = File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Chat/ChatService.cs");

      Assert.That(source, Does.Contain("public void SendSystemNotification(NetworkConnection actor, string message)"));
      Assert.That(source, Does.Contain("ReceiveChatObserversRpc(FormatPlayerSystemMessage(actor, message))"));
      Assert.That(source, Does.Contain("ReceiveChatObserversRpc(formatted)"));
    }

    [Test]
    public void TriageScenarioSystemMessagesAreBroadcastFromTheAuthoritativeServer()
    {
      string source = File.ReadAllText(
        "Assets/Modules/TriageTrainer/Scripts/Scenario/TriageScenarioEventBootstrap.cs");

      Assert.That(source, Does.Contain("if (!TryBroadcastSystemMessage(message))"));
      Assert.That(source, Does.Contain("_chatService.BroadcastSystemMessage(message);"));
    }

    [TestCase("Assets/Modules/TriageTrainer/Scripts/Entities/Stretcher/StretcherController.cs")]
    [TestCase("Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/MinecraftBoatLikeControl.cs")]
    public void VehicleInputRpcRejectsNonFiniteValues(string path)
    {
      string source = File.ReadAllText(path);
      Assert.That(source, Does.Contain("!float.IsFinite(forward) || !float.IsFinite(turn)"));
    }

    [Test]
    public void StaticDisplaymentUsesAuthoritativeItemExchange()
    {
      string source = File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.StaticObjectDisplayment.cs");

      Assert.That(source, Does.Contain("TryGetServerSharedItemExchange"));
      Assert.That(source, Does.Contain("authoritativeItemIdentifier"));
      Assert.That(source, Does.Contain("consumeCount != authoritativeConsumeCount"));
      Assert.That(source, Does.Contain("CountItemInInventory(authoritativeItemIdentifier)"));
      Assert.That(source, Does.Contain("RemoveItemFromInventory(authoritativeItemIdentifier, authoritativeConsumeCount)"));
      Assert.That(source, Does.Contain("TryAddItemToInventory(authoritativeItem)"),
        "회수한 설치 장비는 다음 재설치 검증을 위해 서버 인벤토리에도 복원해야 합니다.");
      Assert.That(Regex.IsMatch(source, @"if \(IsServerStarted\)\s*return;"), Is.True,
        "호스트의 TargetRpc 미러가 서버에서 이미 복원한 아이템을 중복 지급하면 안 됩니다.");
      Assert.That(source.IndexOf("ServerConfirmStaticObjectApplySuccess(entityIdentifier, claimant)", System.StringComparison.Ordinal),
        Is.GreaterThan(source.IndexOf("RemoveItemFromInventory(authoritativeItemIdentifier, authoritativeConsumeCount)", System.StringComparison.Ordinal)));
      Assert.That(source.IndexOf("TargetGrantStaticObjectDisplaymentItem(claimant, authoritativeItemIdentifier)", StringComparison.Ordinal),
        Is.GreaterThan(source.IndexOf("TryAddItemToInventory(authoritativeItem)", StringComparison.Ordinal)),
        "서버 인벤토리를 복원한 뒤 원격 소유자에게 결과를 미러링해야 합니다.");
    }

    private static readonly string[] RuntimeScriptRoots =
    {
      "Assets/Modules/MultiplayerInfrastructure/Scripts",
      "Assets/Modules/TriageTrainer/Scripts",
    };

    /// <summary>
    /// FishNet 코드젠은 [ObserversRpc]/[TargetRpc] 원본 메서드를 "송신부"로 치환하고 송신부에
    /// IsServer 가드를 삽입한다. 따라서 클라이언트에서 실행되는 client RPC 본문이 다른 client RPC
    /// 메서드를 호출하면 그 호출은 경고 로그 없이 무시되어, 조용한 무동작(silent no-op)이 된다.
    /// 공통 처리는 RPC 가 아닌 일반 메서드로 분리해 각 수신부가 직접 호출해야 한다.
    /// </summary>
    [Test]
    public void ClientRpcBodiesDoNotInvokeOtherClientRpcs()
    {
      var violations = new List<string>();

      foreach (string root in RuntimeScriptRoots)
      {
        Assert.That(Directory.Exists(root), Is.True, $"Runtime script root was not found: {root}");

        foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
          if (path.Contains("/Tests/", StringComparison.Ordinal)
              || path.Contains("\\Tests\\", StringComparison.Ordinal))
            continue;

          CollectClientRpcChainViolations(path, violations);
        }
      }

      Assert.That(violations, Is.Empty,
        "A client RPC body invokes another client RPC, which FishNet silently drops on clients:"
        + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }

    [Test]
    public void HostHotbarIsBoundOnlyByTheLocallyOwnedPlayer()
    {
      // 호스트에서는 원격 플레이어의 서버 인벤토리 변경도 PlayerController 인스턴스를 거친다.
      // 그 경로가 레지스트리의 로컬 핫바를 잡거나 바인딩하면 호스트 핫바에 다른 사람의 아이템이 보인다.
      string hotbarSource = File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Hotbar.cs");
      string itemSource = File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Item.cs");
      string inventorySource = File.ReadAllText(
        "Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Inventory.cs");

      Assert.That(hotbarSource, Does.Contain("private bool IsLocalHotbarOwner => NetworkObject == null || IsOwner;"));
      Assert.That(itemSource, Does.Contain("if (_hotbarUI == null && IsLocalHotbarOwner)"));

      int bindCalls = Regex.Matches(inventorySource, @"_hotbarUI\?\.BindInventory\(_slots\);").Count;
      int guardedBindCalls = Regex.Matches(
        inventorySource, @"if \(IsLocalHotbarOwner\)\s*_hotbarUI\?\.BindInventory\(_slots\);").Count;
      Assert.That(bindCalls, Is.GreaterThan(0));
      Assert.That(guardedBindCalls, Is.EqualTo(bindCalls),
        "Every hotbar binding in PlayerController.Inventory.cs must be guarded by IsLocalHotbarOwner.");
    }

    private static void CollectClientRpcChainViolations(string path, List<string> violations)
    {
      string[] lines = File.ReadAllLines(path);
      var clientRpcNames = new HashSet<string>(StringComparer.Ordinal);
      var declarationLineByName = new Dictionary<string, int>(StringComparer.Ordinal);

      var attributeExpression = new Regex(@"^\s*\[\s*(ObserversRpc|TargetRpc)\b", RegexOptions.Compiled);
      var declarationExpression = new Regex(
        @"^\s*(?:private|public|protected|internal)(?:\s+\w+)*\s+void\s+(\w+)\s*\(", RegexOptions.Compiled);

      // 1단계: 이 파일에 선언된 client RPC 메서드 이름을 모은다.
      for (int i = 0; i < lines.Length; i++)
      {
        if (!attributeExpression.IsMatch(lines[i]))
          continue;

        for (int j = i + 1; j < Math.Min(i + 6, lines.Length); j++)
        {
          Match declaration = declarationExpression.Match(lines[j]);
          if (!declaration.Success)
            continue;

          clientRpcNames.Add(declaration.Groups[1].Value);
          declarationLineByName[declaration.Groups[1].Value] = j;
          break;
        }
      }

      if (clientRpcNames.Count < 2)
        return;

      // 2단계: 각 client RPC 본문이 다른 client RPC 를 호출하는지 확인한다.
      foreach (KeyValuePair<string, int> entry in declarationLineByName)
      {
        int start = entry.Value;
        if (IsExpressionBodiedMember(lines, start))
          continue;

        int depth = 0;
        bool entered = false;

        for (int i = start; i < lines.Length; i++)
        {
          string line = StripLineComment(lines[i]);
          depth += CountOccurrences(line, '{') - CountOccurrences(line, '}');
          if (line.Contains('{'))
            entered = true;

          if (i > start && entered)
          {
            foreach (string candidate in clientRpcNames)
            {
              if (string.Equals(candidate, entry.Key, StringComparison.Ordinal))
                continue;
              if (Regex.IsMatch(line, @"(?<![\w.])" + Regex.Escape(candidate) + @"\s*\("))
                violations.Add($"{path}:{i + 1}  {entry.Key} -> {candidate}");
            }
          }

          if (entered && depth <= 0)
            break;
        }
      }
    }

    private static bool IsExpressionBodiedMember(string[] lines, int declarationLine)
    {
      for (int i = declarationLine; i < Math.Min(declarationLine + 6, lines.Length); i++)
      {
        string line = StripLineComment(lines[i]);
        if (line.Contains("=>", StringComparison.Ordinal))
          return true;
        if (line.Contains('{'))
          return false;
      }

      return false;
    }

    private static string StripLineComment(string line)
    {
      int index = line.IndexOf("//", StringComparison.Ordinal);
      return index >= 0 ? line.Substring(0, index) : line;
    }

    private static int CountOccurrences(string value, char target)
    {
      int count = 0;
      for (int i = 0; i < value.Length; i++)
      {
        if (value[i] == target)
          count++;
      }

      return count;
    }
  }
}
