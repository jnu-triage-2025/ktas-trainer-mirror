using System.Reflection;
using NUnit.Framework;
using TriageTrainer.Entity;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class MovingPatientBedDisplayMarkerTests
  {
    private const string PrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/MinecraftBoatLikes/PatientMovingBed.prefab";

    [Test]
    public void PrefabHasOneMarkerForEachIntravenousAttachmentDisplay()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.GetComponentsInChildren<MovingPatientBedIntravenousStandDisplay>(true),
        Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<MovingPatientBedNormalSalineDisplay>(true),
        Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<MovingPatientBedPlasmaSolutionDisplay>(true),
        Has.Length.EqualTo(1));
    }

    [Test]
    public void ControllerRecoversClearedDisplayReferencesFromUniqueMarkers()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
      var instance = Object.Instantiate(prefab);
      try
      {
        var controller = instance.GetComponent<MovingPatientBedController>();
        Assert.That(controller, Is.Not.Null);

        SetField(controller, "_intravenousStandReference", null);
        SetField(controller, "_intravenousHangerHangedNormalSalineReference", null);
        SetField(controller, "_intravenousHangerHangedPlasmaSolutionReference", null);

        Invoke(controller, "RecoverIntravenousAttachmentDisplayReferences");

        Assert.That(GetField<MovingPatientBedIntravenousStandDisplay>(controller,
          "_intravenousStandReference"), Is.Not.Null);
        Assert.That(GetField<MovingPatientBedNormalSalineDisplay>(controller,
          "_intravenousHangerHangedNormalSalineReference"), Is.Not.Null);
        Assert.That(GetField<MovingPatientBedPlasmaSolutionDisplay>(controller,
          "_intravenousHangerHangedPlasmaSolutionReference"), Is.Not.Null);
      }
      finally
      {
        Object.DestroyImmediate(instance);
      }
    }

    private static void SetField(object target, string name, object value)
      => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

    private static T GetField<T>(object target, string name)
      => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

    private static void Invoke(object target, string name)
      => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
  }
}
