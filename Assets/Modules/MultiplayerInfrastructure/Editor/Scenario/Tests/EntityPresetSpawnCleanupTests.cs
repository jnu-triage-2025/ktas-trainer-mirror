using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Tests
{
  public sealed class EntityPresetSpawnCleanupTests
  {
    [Test]
    public void SpawnResultIncludesUnwrappedChildForScenarioCleanup()
    {
      string suffix = Guid.NewGuid().ToString("N");
      string parentPresetIdentifier = $"test_parent_{suffix}";
      string childPresetIdentifier = $"test_child_{suffix}";
      string parentEntityIdentifier = $"test_parent_entity_{suffix}";
      string childEntityIdentifier = $"test_child_entity_{suffix}";
      var parentPrefab = new GameObject("ParentPrefab");
      var childPrefab = new GameObject("BedPrefab");
      IReadOnlyList<GameObject> spawnedObjects = null;

      try
      {
        Registry.Registry.RegisterEntityPreset(
          childPresetIdentifier,
          EntityType.MovingPatientBed,
          childPrefab);
        Registry.Registry.RegisterEntityPreset(
          parentPresetIdentifier,
          EntityType.Patient,
          parentPrefab,
          childReferences: new[]
          {
            new EntityPresetChildReference
            {
              childPresetIdentifier = childPresetIdentifier,
              spawnedEntityIdentifier = childEntityIdentifier,
              unwrapOnSpawn = true
            }
          });

        bool spawned = Registry.Registry.TrySpawnEntityPreset(
          parentPresetIdentifier,
          Vector3.zero,
          Quaternion.identity,
          parentEntityIdentifier,
          out var parent,
          out _,
          out var error,
          out spawnedObjects);

        Assert.That(spawned, Is.True, error);
        Assert.That(spawnedObjects, Has.Count.EqualTo(2));
        Assert.That(spawnedObjects[0], Is.SameAs(parent));
        Assert.That(spawnedObjects[1].transform.parent, Is.EqualTo(parent.transform.parent),
          "unwrap 된 침대는 별도 루트이지만 시나리오 종료 정리 목록에는 포함되어야 한다");
      }
      finally
      {
        if (spawnedObjects != null)
        {
          for (int i = spawnedObjects.Count - 1; i >= 0; i--)
          {
            if (spawnedObjects[i] != null)
              UnityEngine.Object.DestroyImmediate(spawnedObjects[i]);
          }
        }

        Registry.Registry.UnregisterEntity(parentEntityIdentifier);
        Registry.Registry.UnregisterEntity(childEntityIdentifier);
        Registry.Registry.UnregisterEntityPreset(parentPresetIdentifier);
        Registry.Registry.UnregisterEntityPreset(childPresetIdentifier);
        UnityEngine.Object.DestroyImmediate(parentPrefab);
        UnityEngine.Object.DestroyImmediate(childPrefab);
      }
    }
  }
}
