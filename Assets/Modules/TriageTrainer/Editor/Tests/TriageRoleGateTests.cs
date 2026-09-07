using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using NUnit.Framework;
using TriageTrainer.Utils;

namespace TriageTrainer.Tests
{
  public sealed class TriageRoleGateTests
  {
    private const string Holder = "role-gate-holder";
    private const string Other = "role-gate-other";

    [SetUp]
    public void SetUp()
    {
      UserDescriptorService.Register(9001, new UserDescriptor(Holder, "holder"));
      UserDescriptorService.Register(9002, new UserDescriptor(Other, "other"));
      PlayerTagService.ReplaceTags(Holder, new[] { "nurse_b" });
      PlayerTagService.ReplaceTags(Other, System.Array.Empty<string>());
    }

    [TearDown]
    public void TearDown()
    {
      PlayerTagService.ReplaceTags(Holder, System.Array.Empty<string>());
      PlayerTagService.ReplaceTags(Other, System.Array.Empty<string>());
      UserDescriptorService.Unregister(Holder);
      UserDescriptorService.Unregister(Other);
    }

    [Test]
    public void RoleHolderPassesAndOthersAreRejectedWhileTheHolderIsConnected()
    {
      Assert.That(TriageRoleGate.IsAllowed(Holder, "nurse_b"), Is.True);
      Assert.That(TriageRoleGate.IsAllowed(Other, "nurse_b"), Is.False,
        "담당자가 접속 중이면 다른 플레이어는 그 역할의 처치를 수행할 수 없어야 한다.");
    }

    [Test]
    public void AnyConnectedPlayerPassesOnceNoOneHoldsTheRole()
    {
      UserDescriptorService.Unregister(Holder);
      Assert.That(TriageRoleGate.IsRoleHeldByAnyConnectedPlayer("nurse_b"), Is.False);
      Assert.That(TriageRoleGate.IsAllowed(Other, "nurse_b"), Is.True,
        "담당자가 이탈하면 그 역할의 처치를 아무도 할 수 없는 상태가 되어서는 안 된다.");
    }

    [Test]
    public void EmptyRoleRequirementAlwaysPasses()
    {
      Assert.That(TriageRoleGate.IsAllowed(Other, null), Is.True);
      Assert.That(TriageRoleGate.IsAllowed(null, "nurse_b"), Is.False);
    }
  }
}
