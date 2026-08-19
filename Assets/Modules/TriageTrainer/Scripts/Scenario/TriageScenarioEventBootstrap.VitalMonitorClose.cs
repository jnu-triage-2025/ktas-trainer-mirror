using TriageTrainer.Entity;
using TriageTrainer.Entity.PatientMonitor.Models;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    // 모니터 오브젝트는 닫기와 무관하게 항상 월드에 남는다(환자 A 모니터와 동일한 동작).
    // 여기서는 시나리오 완료 시그널 발행만 무장(arm)한다.
    private void ConfigureVitalMonitorClose(PatientMonitorController monitorController,
      PatientController patient,
      string completionSignal)
    {
      if (monitorController == null || patient == null || string.IsNullOrWhiteSpace(completionSignal))
      {
        return;
      }

      monitorController.ArmScenarioClose(patient, completionSignal);
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

      // 시나리오는 모니터 오브젝트만 해석한다. 추적 대상(감시 환자) 설정은
      // 플레이어가 모니터의 '환자 선택' 상호작용으로 직접 수행해야 하므로 여기서 바인딩하지 않는다.
      monitorObject = monitorController.gameObject;
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
