using MultiplayerInfrastructure.FishNetSupports;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

    [Test]
    public void DuplicateProviderIsPromotedWhenPreviousProviderUnregisters()
    {
      var firstTransformObject = new UnityEngine.GameObject("First Spawn");
      var secondTransformObject = new UnityEngine.GameObject("Second Spawn");
      var first = new SpawnPointProvider(firstTransformObject.transform, true);
      var second = new SpawnPointProvider(secondTransformObject.transform, true);
      try
      {
        PlayerSpawnPointRegistry.Register(first);
        LogAssert.Expect(LogType.Error, "[PlayerSpawnPointRegistry] Duplicate provider identifier 'test-spawn'. Registration is pending until the active provider leaves.");
        PlayerSpawnPointRegistry.Register(second);
        Assert.That(PlayerSpawnPointRegistry.TryGet("test-spawn", out var initial), Is.True);
        Assert.That(initial, Is.SameAs(firstTransformObject.transform));

        PlayerSpawnPointRegistry.Unregister(first);

        Assert.That(PlayerSpawnPointRegistry.TryGet("test-spawn", out var promoted), Is.True);
        Assert.That(promoted, Is.SameAs(secondTransformObject.transform));
      }
      finally
      {
        PlayerSpawnPointRegistry.Unregister(first);
        PlayerSpawnPointRegistry.Unregister(second);
        UnityEngine.Object.DestroyImmediate(firstTransformObject);
        UnityEngine.Object.DestroyImmediate(secondTransformObject);
      }
    }

    private sealed class SpawnPointProvider : IPlayerSpawnPointProvider
    {
      private readonly UnityEngine.Transform _spawnTransform;
      private readonly bool _isAvailable;

      public SpawnPointProvider(UnityEngine.Transform spawnTransform = null, bool isAvailable = false)
      {
        _spawnTransform = spawnTransform;
        _isAvailable = isAvailable;
      }

      public string Identifier => "test-spawn";
      public UnityEngine.Transform SpawnTransform => _spawnTransform;
      public bool IsAvailable => _isAvailable;
    }
  }
}
