using MultiplayerInfrastructure.FishNetSupports;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.FishNetSupports
{
  public sealed class PlayerSpawnPointRegistryTests
  {
    [Test]
    public void UnregisterDoesNotNotifySpawnWaiters()
    {
      var provider = new SpawnPointProvider();
      var changeCount = 0;
      System.Action changed = () => changeCount++;
      PlayerSpawnPointRegistry.Changed += changed;

      try
      {
        PlayerSpawnPointRegistry.Register(provider);
        Assert.That(changeCount, Is.EqualTo(1));

        PlayerSpawnPointRegistry.Unregister(provider);
        Assert.That(changeCount, Is.EqualTo(1));
      }
      finally
      {
        PlayerSpawnPointRegistry.Unregister(provider);
        PlayerSpawnPointRegistry.Changed -= changed;
      }
    }

    private sealed class SpawnPointProvider : IPlayerSpawnPointProvider
    {
      public string Identifier => "test-spawn";
      public UnityEngine.Transform SpawnTransform => null;
      public bool IsAvailable => false;
    }
  }
}
