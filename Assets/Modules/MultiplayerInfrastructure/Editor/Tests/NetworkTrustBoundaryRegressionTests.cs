using System.IO;
using System.Reflection;
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
      Assert.That(source.IndexOf("ServerConfirmStaticObjectApplySuccess(entityIdentifier, claimant)", System.StringComparison.Ordinal),
        Is.GreaterThan(source.IndexOf("RemoveItemFromInventory(authoritativeItemIdentifier, authoritativeConsumeCount)", System.StringComparison.Ordinal)));
    }
  }
}
