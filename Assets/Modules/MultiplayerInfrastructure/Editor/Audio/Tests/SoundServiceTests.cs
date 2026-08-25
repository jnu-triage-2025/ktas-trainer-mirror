using MultiplayerInfrastructure.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Audio
{
  public sealed class SoundServiceTests
  {
    private const string SoundServicePrefabPath =
      "Assets/Modules/MultiplayerInfrastructure/Prefabs/Systems/MI_ SoundService.prefab";
    private const string BackendSystemPrefabPath =
      "Assets/Modules/MultiplayerInfrastructure/Prefabs/MultiplayerInfrastructureBackendSystem.prefab";

    [Test]
    public void Prefabs_ContainDedicatedSoundServiceAndBackendInstance()
    {
      var soundServicePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SoundServicePrefabPath);
      Assert.That(soundServicePrefab, Is.Not.Null);
      Assert.That(soundServicePrefab.GetComponent<SoundService>(), Is.Not.Null);

      var backendSystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackendSystemPrefabPath);
      Assert.That(backendSystemPrefab, Is.Not.Null);

      var instance = backendSystemPrefab.transform.Find("Services/MI: SoundService");
      Assert.That(instance, Is.Not.Null);
      Assert.That(instance.GetComponent<SoundService>(), Is.Not.Null);
    }

    [TestCase("alarm")]
    [TestCase("Sound/alarm")]
    [TestCase("hospital/monitor/alarm")]
    public void IsValidSoundResourceIdentifier_AcceptsResourcesRelativeIdentifiers(string identifier)
    {
      Assert.That(SoundService.IsValidSoundResourceIdentifier(identifier), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("/alarm")]
    [TestCase("../alarm")]
    [TestCase("folder\\alarm")]
    public void IsValidSoundResourceIdentifier_RejectsUnsafeIdentifiers(string identifier)
    {
      Assert.That(SoundService.IsValidSoundResourceIdentifier(identifier), Is.False);
    }
  }
}
