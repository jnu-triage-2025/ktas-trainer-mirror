using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEngine;
using UnityEngine.TestTools;

namespace TriageTrainer.Tests
{
  public sealed class TriageWorldInteractionSignalsTests
  {
    [Test]
    public void PatientEquipmentConnectionIncludesPatientTypeAndEquipmentIdentifier()
    {
      var patientObject = new GameObject("patient_b");
      var equipmentObject = new GameObject("wall_suction_zone_7");
      var raised = new List<string>();

      void Capture(string signal) => raised.Add(signal);
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        equipmentObject.AddComponent<BoxCollider>();
        var equipment = equipmentObject.AddComponent<WallAttachedWallSuction>();

        patient.SetConnectedWallSuction(equipment);

        Assert.That(raised, Does.Contain(
          $"sig.patient_equipment_connected_patient_WallSuction_{equipment.EntityIdentifier}"));
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        foreach (string signal in raised)
          ScenarioInteractionSignals.Clear(signal);
        Object.DestroyImmediate(equipmentObject);
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void PositioningPointSignalsAreScopedByBedAndPoint()
    {
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        TriageWorldInteractionSignals.RaisePatientBedPositioningPointLatched("bed_b", "zone_1:bed_snap_point");
        TriageWorldInteractionSignals.RaisePatientBedPositioningPointUnlatched("bed_b", "zone_1:bed_snap_point");

        Assert.That(raised, Does.Contain("sig.patient_bed_positioning_point_latched_bed_b_zone_1:bed_snap_point"));
        Assert.That(raised, Does.Contain("sig.patient_bed_positioning_point_unlatched_bed_b_zone_1:bed_snap_point"));
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        ScenarioInteractionSignals.Clear("patient_bed_positioning_point_latched_bed_b_zone_1:bed_snap_point");
        ScenarioInteractionSignals.Clear("patient_bed_positioning_point_unlatched_bed_b_zone_1:bed_snap_point");
      }
    }

    [Test]
    public void CareZonePatientEnteredAlsoRaisesZoneIndependentPatientSignal()
    {
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        TriageWorldInteractionSignals.RaiseCareZonePatientEntered("zone_0:zone", "patient_b");
        Assert.That(raised, Does.Contain("sig.carezone_patient_entered_zone_0:zone_patient_b"));
        Assert.That(raised, Does.Contain("sig.carezone_patient_entered_patient_b"));
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        foreach (string signal in raised)
          ScenarioInteractionSignals.Clear(signal);
      }
    }

    [Test]
    public void CareZoneDetectsPatientMovedByTransformWithoutPhysicsTrigger()
    {
      // 환자·침대·Zone은 모두 Rigidbody가 없어 물리 트리거가 발생하지 않는다.
      // Zone의 폴링 경로가 transform 이동만으로 진입/이탈 신호를 발생시키는지 검증한다.
      var zoneObject = new GameObject("zone_poll");
      var patientObject = new GameObject("patient_b");
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);
      LogAssert.ignoreFailingMessages = true;
      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        var zone = zoneObject.AddComponent<PatientCareDescriptionZone>();
        zone.SetIdentifierForEditor("zone_0:zone");
        zone.ConfigureArea(Vector3.zero, new Vector3(4f, 4f, 4f));

        var patient = patientObject.AddComponent<PatientController>();
        patient.ApplySpawnedEntityIdentifier("patient_b");

        var poll = typeof(PatientCareDescriptionZone).GetMethod("PollPatients", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(poll, Is.Not.Null);

        // 구역 밖: 진입 신호가 없어야 한다.
        patientObject.transform.position = new Vector3(100f, 0f, 0f);
        Physics.SyncTransforms();
        poll.Invoke(zone, null);
        Assert.That(raised, Does.Not.Contain("sig.carezone_patient_entered_patient_b"));

        // Rigidbody 없이 transform만 갱신해 구역 안으로 이동(침대 이동과 동일한 방식).
        patientObject.transform.position = Vector3.zero;
        Physics.SyncTransforms();
        poll.Invoke(zone, null);
        Assert.That(raised, Does.Contain("sig.carezone_patient_entered_patient_b"));

        // 구역 밖으로 이동하면 이탈 신호가 발생해야 한다.
        patientObject.transform.position = new Vector3(100f, 0f, 0f);
        Physics.SyncTransforms();
        poll.Invoke(zone, null);
        Assert.That(raised, Does.Contain("sig.carezone_patient_exited_zone_0:zone_patient_b"));
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        foreach (string signal in raised)
          ScenarioInteractionSignals.Clear(signal);
        LogAssert.ignoreFailingMessages = false;
        Object.DestroyImmediate(patientObject);
        Object.DestroyImmediate(zoneObject);
      }
    }
  }
}
