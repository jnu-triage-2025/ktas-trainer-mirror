using System;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Entity.PatientMonitor.Models;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void ConfigureVitalMonitorClose(PatientMonitorController monitorController,
      PatientController patient,
      GameObject monitorObject,
      GameObject panelObject,
      string completionSignal)
    {
      if (monitorController == null || patient == null || string.IsNullOrWhiteSpace(completionSignal))
      {
        return;
      }

      monitorController.ArmScenarioClose(patient, completionSignal, () =>
      {
        SetActiveIfPresent(panelObject, false);
        SetActiveIfPresent(monitorObject, false);
      });
    }

    private void ResolvePatientVitalMonitor(GameObject patientObject,
      ref GameObject monitorObject,
      ref PatientMonitorController monitorController)
    {
      var patient = patientObject != null ? patientObject.GetComponent<PatientController>() : null;
      if (patient == null)
        return;

      if (monitorController == null && monitorObject != null)
        monitorController = monitorObject.GetComponentInChildren<PatientMonitorController>(true);

      monitorController ??= FindPatientVitalMonitor(patient);
      if (monitorController == null)
        return;

      monitorObject = monitorController.gameObject;
      if (!ReferenceEquals(monitorController.MonitoringPatient, patient))
        monitorController.SetPresentationPatient(patient);
    }

    private static PatientMonitorController FindPatientVitalMonitor(PatientController patient)
    {
      if (patient == null)
        return null;

      if (patient.MonitoringPatientMonitor != null)
        return patient.MonitoringPatientMonitor;

      var monitors = FindObjectsByType<PatientMonitorController>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);
      for (int i = 0; i < monitors.Length; i++)
      {
        if (monitors[i] != null && ReferenceEquals(monitors[i].MonitoringPatient, patient))
          return monitors[i];
      }

      var zones = FindObjectsByType<PatientCareDescriptionZone>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      PatientMonitorController nearest = null;
      float nearestDistance = float.PositiveInfinity;
      for (int zoneIndex = 0; zoneIndex < zones.Length; zoneIndex++)
      {
        var zone = zones[zoneIndex];
        if (zone == null ||
            (!ReferenceEquals(zone.CurrentPatient, patient) && !zone.ContainsWorldPosition(patient.transform.position)))
          continue;

        for (int monitorIndex = 0; monitorIndex < monitors.Length; monitorIndex++)
        {
          var monitor = monitors[monitorIndex];
          if (monitor == null || !zone.ContainsWorldPosition(monitor.transform.position))
            continue;

          float distance = (monitor.transform.position - patient.transform.position).sqrMagnitude;
          if (distance >= nearestDistance)
            continue;

          nearest = monitor;
          nearestDistance = distance;
        }
      }

      return nearest;
    }
  }
}
