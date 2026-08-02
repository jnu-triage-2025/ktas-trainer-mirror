using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEngine;

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
  }
}
