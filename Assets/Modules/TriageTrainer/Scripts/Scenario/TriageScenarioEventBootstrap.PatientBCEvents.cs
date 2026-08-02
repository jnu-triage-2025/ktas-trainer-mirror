using System.Collections;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterPatientBCEvents()
    {
      RegisterEvent_TriagePatientBPatientCPatientDummyDB();
      RegisterEvent_ShowPatientBUi();
      RegisterEvent_ShowPatientDummyDBUi();
      RegisterEvent_ShowPatientCUi();
      RegisterEvent_MovePatientB();
      RegisterEvent_MovePatientC();
      RegisterEvent_ActivateVitalMonitorUiPatientB();
      RegisterEvent_ActivateVitalMonitorUiPatientC();
      RegisterEvent_PupilReflexPatientB();
      RegisterEvent_PupilReflexPatientC();
      RegisterEvent_MovePatientsToCt();
      RegisterEvent_FadeOutPatientBC();
      RegisterEvent_ApplyGauzePatientB();
      RegisterEvent_ApplyGauzeWithPlasterPatientB();
      RegisterEvent_ApplyGauzePatientC();
      RegisterEvent_ApplyGauzeWithPlasterPatientC();
      RegisterEvent_Insert20gRightPatientB();
      RegisterEvent_ConnectNs1RightPatientB();
      RegisterEvent_Insert20gLeftPatientC();
      RegisterEvent_ConnectNs1LeftPatientC();
      RegisterEvent_AttachPatientBedPairs();
      RegisterPatientBRecognitionEvents();
    }

  }
}
