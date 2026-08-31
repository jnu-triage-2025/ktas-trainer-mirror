using System.Reflection;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using UnityEngine;
using RuntimeRegistry = MultiplayerInfrastructure.Registry.Registry;

namespace TriageTrainer.Tests
{
  public sealed class LifecycleSafetyRegressionTests
  {
    [Test]
    public void SpawnPointRegistryPrunesDestroyedInterfaceProvider()
    {
      InvokeStaticReset(typeof(PlayerSpawnPointRegistry));
      var host = new GameObject("destroyed-spawn-provider");
      try
      {
        var provider = host.AddComponent<DestroyableSpawnPointProvider>();
        provider.IdentifierValue = "destroyed-provider";
        PlayerSpawnPointRegistry.Register(provider);
        Object.DestroyImmediate(host);

        Assert.DoesNotThrow(() => PlayerSpawnPointRegistry.TryGet("destroyed-provider", out _));
        Assert.That(PlayerSpawnPointRegistry.TryGet("destroyed-provider", out _), Is.False);
      }
      finally
      {
        if (host != null)
          Object.DestroyImmediate(host);
        InvokeStaticReset(typeof(PlayerSpawnPointRegistry));
      }
    }

    [Test]
    public void RegistrySubsystemResetClearsRuntimeValuesAndSubscribers()
    {
      const string identifier = "test:lifecycle-reset";
      int notificationCount = 0;
      RuntimeRegistry.OnEntryRegistered += (_, _, _) => notificationCount++;
      RuntimeRegistry.Register(RegistryType.RuntimeState, identifier, new object());
      Assert.That(notificationCount, Is.EqualTo(1));

      InvokeStaticReset(typeof(RuntimeRegistry));

      Assert.That(RuntimeRegistry.Contains(RegistryType.RuntimeState, identifier), Is.False);
      RuntimeRegistry.Register(RegistryType.RuntimeState, identifier, new object());
      Assert.That(notificationCount, Is.EqualTo(1));

      InvokeStaticReset(typeof(RuntimeRegistry));
    }

    private static void InvokeStaticReset(System.Type type)
      => type.GetMethod("ResetOnSubsystemRegistration", BindingFlags.Static | BindingFlags.NonPublic)
        ?.Invoke(null, null);
  }

  public sealed class DestroyableSpawnPointProvider : MonoBehaviour, IPlayerSpawnPointProvider
  {
    public string IdentifierValue;
    public string Identifier => IdentifierValue;
    public Transform SpawnTransform => transform;
    public bool IsAvailable => isActiveAndEnabled;
  }
}
