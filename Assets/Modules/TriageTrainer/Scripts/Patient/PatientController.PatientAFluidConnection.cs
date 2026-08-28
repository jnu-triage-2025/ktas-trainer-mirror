using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.ItemDefinitions;
using UnityEngine;
using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 A의 수액 연결 상호작용 부분 구현. 좌측 정맥로에는 생리식염수(N/S)를, 우측 정맥로에는
  /// 플라즈마 솔루션을 연결한다.
  ///
  /// <para>
  /// 예전에는 플레이어가 <c>IntravenousLineConnectionPoint</c> 의 "수액 줄 연결 시작"과
  /// "여기에 수액 줄 연결"을 차례로 사용해서 두 지점을 직접 이었다. 지금은 그 상호작용이 모든
  /// 연결 지점에서 잠겨 있으므로, 환자 B/C 의 "생리식염수 연결"과 같은 방식으로 환자 쪽 전용
  /// 상호작용 한 번에 연결을 완성한다. 줄 오브젝트는
  /// <see cref="LineConnectionService.TryCreateAutomaticConnection"/> 이 생성하고, 시나리오
  /// 진행 신호는 이 파일에서 직접 올린다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    /// <summary>좌측 정맥로에 생리식염수를 잇는 상호작용 식별자. 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientANormalSalineConnect = "patient_a_normal_saline_connect";

    /// <summary>우측 정맥로에 플라즈마 솔루션을 잇는 상호작용 식별자. 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAPlasmaSolutionConnect = "patient_a_plasma_solution_connect";

    /// <summary>환자 A 프리팹에 배치된 좌·우 캐뉼라 포트의 연결 지점 Identifier.</summary>
    private const string PatientALeftCannulaPortIdentifier = "patient_a_cannula_left_port";
    private const string PatientARightCannulaPortIdentifier = "patient_a_cannula_right_port";

    /// <summary>연결 완료 시 시나리오와 퀘스트가 기다리는 신호. 기존 수액 줄 연결 경로와 같은 이름을 유지한다.</summary>
    private const string PatientANormalSalineConnectedSignal = "connect_cannula_and_ns1";
    private const string PatientAPlasmaSolutionConnectedSignal = "connect_ps1_right";

    private IntravenousLineConnectionPoint _patientALeftCannulaPort;
    private IntravenousLineConnectionPoint _patientARightCannulaPort;

    private sealed class PatientAFluidConnectInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      private readonly bool _isLeftArm;

      public PatientAFluidConnectInteract(PatientController owner, bool isLeftArm)
      {
        _owner = owner;
        _isLeftArm = isLeftArm;
      }

      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => _isLeftArm
        ? InteractIdPatientANormalSalineConnect
        : InteractIdPatientAPlasmaSolutionConnect;

      public string DisplayText => _isLeftArm ? "생리식염수 연결" : "플라즈마 솔루션 연결";
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor) => _owner.CanConnectPatientAFluid(_isLeftArm);

      public void Interact(Transform interactor) => _owner.TryConnectPatientAFluid(interactor, _isLeftArm);
    }

    /// <summary>환자 A 수액 연결 상호작용 항목을 등록한다(<c>BuildInteractEntries</c> 에서 호출).</summary>
    private void AddPatientAFluidConnectInteracts()
    {
      _interacts.Add(new PatientAFluidConnectInteract(this, isLeftArm: true));
      _interacts.Add(new PatientAFluidConnectInteract(this, isLeftArm: false));
    }

    /// <summary>
    /// 해당 팔의 수액 연결이 지금 가능한지 판정한다. 캐뉼라가 삽입되어 있고, 침대 수액걸이에
    /// 해당 수액이 걸려 있으며, 아직 그 수액과 이어지지 않은 상태여야 한다.
    /// </summary>
    private bool CanConnectPatientAFluid(bool isLeftArm)
    {
      if (!IsPatientA || !IsPatientACannulaInserted(isLeftArm))
        return false;

      return TryGetPatientAFluidConnectionPoints(isLeftArm, out var patientPoint, out var fluidPoint)
             && !patientPoint.IsPhysicallyConnectedTo(fluidPoint);
    }

    /// <summary>
    /// 해당 팔에 캐뉼라가 삽입되어 있는지 확인한다. 환자 A는 18G를 사용하지만, 처치 표현이
    /// 20G까지 지원하는 프리팹도 같은 정맥로로 취급한다.
    /// </summary>
    private bool IsPatientACannulaInserted(bool isLeftArm) =>
      IsTreatmentDisplayActive(isLeftArm
        ? TreatmentDisplay.Syringe18GInsertedIntoLeftArm
        : TreatmentDisplay.Syringe18GInsertedIntoRightArm)
      || IsTreatmentDisplayActive(isLeftArm
        ? TreatmentDisplay.Syringe20GInsertedIntoLeftArm
        : TreatmentDisplay.Syringe20GInsertedIntoRightArm);

    private bool TryGetPatientAFluidConnectionPoints(
      bool isLeftArm,
      out IntravenousLineConnectionPoint patientPoint,
      out IntravenousLineConnectionPoint fluidPoint)
    {
      patientPoint = null;
      fluidPoint = null;

      var bed = CurrentBed;
      if (bed == null)
        return false;

      bool hasFluidPoint = isLeftArm
        ? bed.TryGetNormalSalineConnectionPoint(out fluidPoint)
        : bed.TryGetPlasmaSolutionConnectionPoint(out fluidPoint);
      if (!hasFluidPoint || fluidPoint == null)
        return false;

      patientPoint = ResolvePatientACannulaPort(isLeftArm);
      return patientPoint != null;
    }

    /// <summary>
    /// 환자 A 프리팹의 좌·우 캐뉼라 포트를 Identifier 로 찾는다. 우측 포트는 처치 표현 자식에
    /// 들어 있어 비활성 상태로 시작하므로 비활성 자식까지 함께 검색한다.
    /// </summary>
    private IntravenousLineConnectionPoint ResolvePatientACannulaPort(bool isLeftArm)
    {
      var cached = isLeftArm ? _patientALeftCannulaPort : _patientARightCannulaPort;
      if (cached != null)
        return cached;

      string identifier = isLeftArm
        ? PatientALeftCannulaPortIdentifier
        : PatientARightCannulaPortIdentifier;
      var points = GetComponentsInChildren<IntravenousLineConnectionPoint>(true);
      for (int i = 0; i < points.Length; i++)
      {
        var point = points[i];
        if (point == null
            || !string.Equals(point.Identifier, identifier, System.StringComparison.Ordinal))
          continue;

        if (isLeftArm)
          _patientALeftCannulaPort = point;
        else
          _patientARightCannulaPort = point;
        return point;
      }

      return null;
    }

    /// <summary>
    /// 수액 연결 상호작용을 처리한다. 수액세트를 갖고 있어야 하며, 클라이언트에서는 서버에
    /// 처리를 요청한다.
    /// </summary>
    private bool TryConnectPatientAFluid(Transform interactor, bool isLeftArm)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return false;

      if (player.CountItemInInventory(IntravenousSet.Identifier) < 1)
      {
        ShowRequiredItemDialogue("수액세트를 갖고 있지 않다.", "수액세트를 찾자.");
        return false;
      }

      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdConnectPatientAFluid(isLeftArm);
        return true;
      }

      bool connected = TryConnectPatientAFluidAuthoritative(isLeftArm, player);
      player.RefreshInteractableHintsNow();
      return connected;
    }

    private bool TryConnectPatientAFluidAuthoritative(bool isLeftArm, PlayerController player = null)
    {
      if (!CanConnectPatientAFluid(isLeftArm)
          || (player != null && player.CountItemInInventory(IntravenousSet.Identifier) < 1)
          || !TryGetPatientAFluidConnectionPoints(isLeftArm, out var patientPoint, out var fluidPoint))
        return false;

      var service = LineConnectionService.TopologyService
                    ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
      if (service == null || !service.TryCreateAutomaticConnection(fluidPoint, patientPoint))
        return false;

      if (player != null && player.RemoveItemFromInventory(IntravenousSet.Identifier, 1) != 1)
      {
        service.DisconnectAutomaticConnection(fluidPoint, patientPoint);
        return false;
      }

      SetIVFluidConnection(isLeftArm, fluidPoint);
      MI.Scenario.ScenarioInteractionSignals.Raise(isLeftArm
        ? PatientANormalSalineConnectedSignal
        : PatientAPlasmaSolutionConnectedSignal);
      return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdConnectPatientAFluid(bool isLeftArm, NetworkConnection sender = null)
    {
      if (!IsPatientA
          || !TryResolveTreatmentActor(sender, null, out var player, out var actorIdentifier,
            out var actorDisplayName))
        return;

      using (MI.Scenario.ScenarioSignalPlayerContext.Push(actorIdentifier, actorDisplayName))
        TryConnectPatientAFluidAuthoritative(isLeftArm, player);
    }
  }
}
