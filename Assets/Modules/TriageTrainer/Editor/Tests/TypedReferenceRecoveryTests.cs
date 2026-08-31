using System.Reflection;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Tests
{
  public sealed class TypedReferenceRecoveryTests
  {
    [Test]
    public void PlayerPrefabUsesTypedBodyAndSpectatorMarkers()
    {
      const string path = "Assets/Modules/MultiplayerInfrastructure/Prefabs/Player.prefab";
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.GetComponentsInChildren<PlayerCharacterBody>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PlayerSpectatorMarkerObject>(true), Has.Length.EqualTo(1));
    }

    [Test]
    public void RapidInfuserPrefabHasOneMarkerForEachFluidDisplay()
    {
      const string path = "Assets/Modules/TriageTrainer/Prefabs/Entities/MinecraftBoatLikes/level1_rapid_infuser.prefab";
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.GetComponentsInChildren<Level1RapidInfuserNormalSalineDisplay>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<Level1RapidInfuserPlasmaSolutionDisplay>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<Level1RapidInfuserBloodBagDisplay>(true), Has.Length.EqualTo(1));
    }

    [Test]
    public void PatientTypeAPrefabHasOneMarkerForEachRecoveredTreatmentVisual()
    {
      const string path = "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA.prefab";
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.GetComponentsInChildren<PatientAEtTubePreparedVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientAEtTubeInsertedVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientATPieceConnectedVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientAGauzeVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientAGauzeWithPlasterVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientA18gLeftVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientA18gRightVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientACentralLineVisualMarker>(true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren<PatientAAmbuConnectedVisualMarker>(true), Has.Length.EqualTo(1));
    }

    [TestCase("Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab", typeof(PatientBGauzeVisualMarker), typeof(PatientBGauzeWithPlasterVisualMarker), typeof(PatientB20gRightVisualMarker))]
    [TestCase("Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBFemale.prefab", typeof(PatientCGauzeVisualMarker), typeof(PatientCGauzeWithPlasterVisualMarker), typeof(PatientC20gLeftVisualMarker))]
    public void PatientTypeBAndCPrefabsHaveOneMarkerForEachImplementedTreatmentVisual(
      string path,
      System.Type gauzeType,
      System.Type plasterType,
      System.Type catheterType)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
      Assert.That(prefab, Is.Not.Null);
      Assert.That(prefab.GetComponentsInChildren(gauzeType, true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren(plasterType, true), Has.Length.EqualTo(1));
      Assert.That(prefab.GetComponentsInChildren(catheterType, true), Has.Length.EqualTo(1));
    }

    [Test]
    public void BootstrapRecoversPatientVisualFromTypedChild()
    {
      var bootstrapObject = new GameObject("bootstrap");
      var patient = new GameObject("patient-a");
      var visual = new GameObject("renamed-visual");
      try
      {
        visual.transform.SetParent(patient.transform, false);
        var marker = visual.AddComponent<PatientAGauzeVisualMarker>();
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        SetField(bootstrap, "_patientAObject", patient);
        SetField(bootstrap, "_patientAGauzeVisual", null);

        Invoke(bootstrap, "RecoverTypedReferences");

        Assert.That(GetField<PatientAGauzeVisualMarker>(bootstrap, "_patientAGauzeVisual"), Is.SameAs(marker));
      }
      finally
      {
        Object.DestroyImmediate(bootstrapObject);
        Object.DestroyImmediate(patient);
      }
    }

    [Test]
    public void BootstrapRecoversUnambiguousOverworldWaypointsByExactIdentifier()
    {
      var bootstrapObject = new GameObject("bootstrap");
      var triagePoint = CreateWaypoint("scen_b:quest_arrival_triage_area");
      var patientBPoint = CreateWaypoint("ct:patient_target_pos_b");
      var patientCPoint = CreateWaypoint("ct:patient_target_pos_c");
      try
      {
        var bootstrap = bootstrapObject.AddComponent<TriageScenarioEventBootstrap>();
        Invoke(bootstrap, "RecoverTypedReferences");

        Assert.That(GetField<Transform>(bootstrap, "_triageArrivalPoint"), Is.SameAs(triagePoint.transform));
        Assert.That(GetField<Transform>(bootstrap, "_patientBCtRoomPoint"), Is.SameAs(patientBPoint.transform));
        Assert.That(GetField<Transform>(bootstrap, "_patientCCtRoomPoint"), Is.SameAs(patientCPoint.transform));
      }
      finally
      {
        Object.DestroyImmediate(bootstrapObject);
        Object.DestroyImmediate(triagePoint);
        Object.DestroyImmediate(patientBPoint);
        Object.DestroyImmediate(patientCPoint);
      }
    }

    private static GameObject CreateWaypoint(string identifier)
    {
      var gameObject = new GameObject(identifier);
      gameObject.AddComponent<WaypointAnchor>().ConfigureIdentifier(identifier);
      return gameObject;
    }

    private static void SetField(object target, string name, object value)
      => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

    private static T GetField<T>(object target, string name)
      => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target);

    private static void Invoke(object target, string name)
      => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
  }
}
