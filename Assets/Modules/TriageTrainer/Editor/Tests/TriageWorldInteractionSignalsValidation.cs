#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Scenario;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Editor.Tests
{
  /// <summary>배치 실행에서도 월드 신호 명세를 독립적으로 확인하는 최소 검증 명령입니다.</summary>
  public static class TriageWorldInteractionSignalsValidation
  {
    [MenuItem("Tools/Triage Trainer/Validate World Interaction Signals")]
    public static void Run()
    {
      var patientObject = new GameObject("signal-validation-patient");
      var suctionObject = new GameObject("signal-validation-suction");
      var oxyflowmeterObject = new GameObject("signal-validation-oxyflowmeter");
      var raised = new List<string>();
      void Capture(string signal) => raised.Add(signal);

      ScenarioInteractionSignals.OnSignalRegistered += Capture;
      try
      {
        var patient = patientObject.AddComponent<PatientController>();
        suctionObject.AddComponent<BoxCollider>();
        var suction = suctionObject.AddComponent<WallAttachedWallSuction>();
        oxyflowmeterObject.AddComponent<BoxCollider>();
        var oxyflowmeter = oxyflowmeterObject.AddComponent<WallAttachedOxyflowmeter>();
        patient.SetConnectedWallSuction(suction);

        Require(raised.Contains($"sig.patient_equipment_connected_patient_WallSuction_{suction.EntityIdentifier}"),
          "환자-벽면 석션 연결 신호에 환자·장비 식별자가 포함되지 않았습니다.");

        TriageWorldInteractionSignals.RaisePatientBedPositioningPointLatched("bed_b", "zone_1:bed_snap_point");
        Require(raised.Contains("sig.patient_bed_positioning_point_latched_bed_b_zone_1:bed_snap_point"),
          "침대 positioning point latch 신호에 침대·point 식별자가 포함되지 않았습니다.");

        suction.ApplyShownFromNetwork();
        suction.ApplyHiddenFromNetwork();
        oxyflowmeter.ApplyShownFromNetwork();
        oxyflowmeter.ApplyHiddenFromNetwork();
        Require(!suction.IsAttached && !oxyflowmeter.IsAttached,
          "네트워크 숨김 처리 후 장비의 설치 상태가 해제되지 않았습니다.");

        Debug.Log("[TriageWorldInteractionSignalsValidation] Passed.");
      }
      finally
      {
        ScenarioInteractionSignals.OnSignalRegistered -= Capture;
        foreach (string signal in raised) ScenarioInteractionSignals.Clear(signal);
        UnityEngine.Object.DestroyImmediate(oxyflowmeterObject);
        UnityEngine.Object.DestroyImmediate(suctionObject);
        UnityEngine.Object.DestroyImmediate(patientObject);
      }
    }

    private static void Require(bool condition, string message)
    {
      if (!condition) throw new InvalidOperationException(message);
    }
  }
}
#endif
