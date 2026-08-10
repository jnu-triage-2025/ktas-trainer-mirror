using FishNet.Object;
using TriageTrainer.Patient;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 처치 부착물 표시 상태(TreatmentDisplay)의 네트워크 동기화 파셜.
  ///
  /// <para>
  /// <see cref="SetTreatmentDisplay"/> 는 로컬 GameObject 를 토글하는 순수 로컬 연산이다.
  /// 멀티플레이 환경에서 모든 피어(관전자/늦은 입장 포함)가 동일한 초기 표시 상태를 보려면
  /// 서버 권한 RPC 를 통해 변경을 전파해야 한다.
  /// </para>
  ///
  /// <para>흐름</para>
  /// <list type="number">
  /// <item>서버 컨텍스트에서 <see cref="SetTreatmentDisplayNetworked"/> 호출
  ///   → 로컬 적용 후 <see cref="RpcSyncTreatmentDisplay"/> 로 모든 클라이언트에 브로드캐스트.</item>
  /// <item>클라이언트 컨텍스트에서 호출할 때는 <see cref="CmdSetTreatmentDisplay"/> 가
  ///   서버를 경유한다(서버에서 적용 후 Observers 로 전파).</item>
  /// <item><see cref="RpcSyncTreatmentDisplay"/> 는 <c>BufferLast=true</c> 이므로 늦게 접속한
  ///   클라이언트도 마지막 상태를 수신한다. 단, 서로 다른 display 항목마다 별도 버퍼가 아니라
  ///   "가장 마지막 호출"만 버퍼링되므로, 초기 상태를 일괄 설정할 때는 전체를 한 번에
  ///   동기화하는 <see cref="SyncAllDisplayStatesNetworked"/> 를 사용하는 것이 권장된다.
  ///   1차 목표(시나리오 EntityInit 으로 초기 상태 설정)에서는 이미 스폰 시점(BufferLast
  ///   수신 이전)에 처리되므로, 스폰 완료 후 <see cref="SyncAllDisplayStatesNetworked"/> 를
  ///   한 번 호출해 전 피어를 정렬하는 것이 안전하다.
  ///   </item>
  /// </list>
  ///
  /// 주의: <c>BufferLast=true</c> 를 가진 <see cref="RpcSyncTreatmentDisplay"/> 는
  /// "마지막 항목 1개"만 버퍼된다. 다수 항목을 순차 설정하면 오직 마지막 항목만 늦은 클라이언트에
  /// 전달된다. 따라서 초기 상태 일괄 설정 후에는 반드시 <see cref="SyncAllDisplayStatesNetworked"/>
  /// 를 호출해야 한다.
  /// </summary>
  public partial class PatientController
  {
    /// <summary>
    /// 처치 표시 상태를 네트워크 전체에 동기화해서 설정한다.
    /// 서버에서 호출하면 로컬 적용 후 즉시 모든 클라이언트에 전파하고,
    /// 클라이언트에서 호출하면 ServerRpc 를 통해 서버를 경유한다.
    /// </summary>
    public void SetTreatmentDisplayNetworked(TreatmentDisplay display, bool active)
    {
      if (IsFishNetServerStarted)
      {
        SetTreatmentDisplay(display, active);
        RpcSyncTreatmentDisplay(display, active);
      }
      else if (IsFishNetClientInitialized)
      {
        CmdSetTreatmentDisplay(display, active);
      }
      else
      {
        // 오프라인/단독 실행 → 로컬만 적용
        SetTreatmentDisplay(display, active);
      }
    }

    /// <summary>
    /// 현재 패치된 모든 DisplayState 를 한 번에 모든 클라이언트에 동기화한다.
    /// 초기 상태 일괄 설정(EntityInit) 후 반드시 서버에서 호출해야 늦은 입장 클라이언트에
    /// 전체 상태가 정확히 전달된다.
    /// </summary>
    public void SyncAllDisplayStatesNetworked()
    {
      if (!IsFishNetServerStarted)
      {
        Debug.LogWarning("[PatientController] SyncAllDisplayStatesNetworked must be called on the server.", this);
        return;
      }

      var state = GetPatientDisplayState();
      if (state == null)
        return;

      var flags = state.DisplayState;
      SyncAllDisplayStates(flags);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdSetTreatmentDisplay(TreatmentDisplay display, bool active)
    {
      SetTreatmentDisplay(display, active);
      RpcSyncTreatmentDisplay(display, active);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcSyncTreatmentDisplay(TreatmentDisplay display, bool active)
    {
      if (IsFishNetServerStarted)
        return;

      SetTreatmentDisplay(display, active);
    }

    // 서버에서 호출: 현재 DisplayState 구조체 전체를 한 번의 RPC 로 브로드캐스트한다.
    // 다수 항목을 동시에 적용한 뒤 늦은 입장 클라이언트에 전체 상태를 전달하는 용도.
    [ServerRpc(RequireOwnership = false)]
    private void CmdSyncAllDisplayStates(PatientTreatmentDisplayModel flags)
    {
      ApplyDisplayModelLocally(flags);
      RpcSyncAllDisplayStates(flags);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcSyncAllDisplayStates(PatientTreatmentDisplayModel flags)
    {
      if (IsFishNetServerStarted)
        return;

      ApplyDisplayModelLocally(flags);
    }

    private void SyncAllDisplayStates(PatientTreatmentDisplayModel flags)
    {
      if (!IsFishNetServerStarted)
        return;

      ApplyDisplayModelLocally(flags);
      RpcSyncAllDisplayStates(flags);
    }

    /// <summary>
    /// DisplayModel 의 모든 필드를 로컬에 일괄 적용한다(SetActive 포함).
    /// </summary>
    private void ApplyDisplayModelLocally(PatientTreatmentDisplayModel flags)
    {
      var state = GetPatientDisplayState();
      if (state == null)
        return;

      state.DisplayState = flags;

      // 각 항목의 자식 GameObject 를 플래그에 따라 SetActive 한다.
      ApplyDisplayChildObject(state, TreatmentDisplay.Syringe18GInsertedIntoLeftArm, flags.Syringe18GInsertedIntoLeftArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.Syringe18GInsertedIntoRightArm, flags.Syringe18GInsertedIntoRightArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.Syringe20GInsertedIntoLeftArm, flags.Syringe20GInsertedIntoLeftArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.Syringe20GInsertedIntoRightArm, flags.Syringe20GInsertedIntoRightArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian, flags.CentralVenousCatheterInsertedIntoSubclavian);
      ApplyDisplayChildObject(state, TreatmentDisplay.LaryngoscopeInserted, flags.LaryngoscopeInserted);
      ApplyDisplayChildObject(state, TreatmentDisplay.EndotrachealTubeStyletInserted, flags.EndotrachealTubeStyletInserted);
      ApplyDisplayChildObject(state, TreatmentDisplay.EndotrachealTubeInsertDone, flags.EndotrachealTubeInsertDone);
      ApplyDisplayChildObject(state, TreatmentDisplay.TPieceAttachedToNasalCannula, flags.TPieceAttachedToNasalCannula);
      ApplyDisplayChildObject(state, TreatmentDisplay.AmbuBagAttachedToEndotrachealTube, flags.AmbuBagAttachedToEndotrachealTube);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzePatchedOnThorax, flags.GauzePatchedOnThorax);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzeDressingDoneOnThorax, flags.GauzeDressingDoneOnThorax);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzePatchedOnRightArm, flags.GauzePatchedOnRightArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzeDressingDoneOnRightArm, flags.GauzeDressingDoneOnRightArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzePatchedOnLeftArm, flags.GauzePatchedOnLeftArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzeDressingDoneOnLeftArm, flags.GauzeDressingDoneOnLeftArm);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzePatchedOnRightEyebrow, flags.GauzePatchedOnRightEyebrow);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzeDressingDoneOnRightEyebrow, flags.GauzeDressingDoneOnRightEyebrow);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzePatchedOnLeftEyebrow, flags.GauzePatchedOnLeftEyebrow);
      ApplyDisplayChildObject(state, TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow, flags.GauzeDressingDoneOnLeftEyebrow);
      ApplyDisplayChildObject(state, TreatmentDisplay.NasalCannulaApplied, flags.NasalCannulaApplied);
      ApplyDisplayChildObject(state, TreatmentDisplay.CervicalCollarOnNeck, flags.CervicalCollarOnNeck);
      ApplyDisplayChildObject(state, TreatmentDisplay.IntravenousStandAttached, flags.IntravenousStandAttached);
      ApplyDisplayChildObject(state, TreatmentDisplay.IntravenousHangerAttached, flags.IntravenousHangerAttached);
      ApplyDisplayChildObject(state, TreatmentDisplay.IntravenousFluidAttached, flags.IntravenousFluidAttached);
    }

    private static void ApplyDisplayChildObject(PatientDisplayState state, TreatmentDisplay display, bool active)
    {
      var go = GetDisplayChildObject(state, display);
      if (go != null)
        go.SetActive(active);
    }
  }
}
