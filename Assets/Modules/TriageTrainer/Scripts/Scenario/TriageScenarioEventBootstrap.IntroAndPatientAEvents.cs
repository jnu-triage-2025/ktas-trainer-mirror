using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterIntroAndPatientAEvents()
    {
      RegisterEvent_TriagePatientAAndPatientDummyDA();
      RegisterEvent_ShowPatientAUi();
      RegisterEvent_ShowPatientDummyDAUi();
      RegisterEvent_BcdToTriage();
      RegisterEvent_MovePatientAToTreatmentRoom();
      RegisterEvent_ActivateVitalMonitorUiPatientA();
      RegisterEvent_ShowSuctionChecklistUi();
      RegisterEvent_HideSuctionChecklistUi();
      RegisterEvent_VitalInfoPatientA();
      RegisterEvent_ShowChecklistIntu();
      RegisterEvent_HideChecklistIntu();
      RegisterEvent_ShowIvChecklist();
      RegisterEvent_HideIvChecklist();
      RegisterEvent_InsertEtTube();
      RegisterEvent_RemoveStylet();
      RegisterEvent_ConnectTPieceReady();
      RegisterEvent_Insert18gLeft();
      RegisterEvent_ConnectNs1Left();
      RegisterEvent_Insert18gRight();
      RegisterEvent_ConnectPs1Right();
      RegisterEvent_ApplyGauzePatientA();
      RegisterEvent_ApplyGauzeWithPlasterPatientA();
      RegisterEvent_PlayerAMoveToTriage();
      RegisterEvent_InsertCentralLineSet();
      RegisterEvent_Lv1Ready();
      RegisterEvent_ApplyAmbuPatientA();
      RegisterEvent_AttachDefibrillatorPad();
      RegisterEvent_DefibrillatorUiIrregular();
      RegisterEvent_StartAmbuBagging();
      RegisterEvent_StartChestCompression();
      RegisterEvent_StopAmbuAndComp();
      RegisterEvent_PatientCrashUi();
      RegisterEvent_AsystoleMonitorUi();
      RegisterEvent_RoscMonitorUi();
      RegisterEvent_ShowPatientACompletionTitle();
      RegisterEvent_PreparePatientAManualEntry();
      RegisterEvent_ArmPatientATriageReturn();
    }
  }
}
