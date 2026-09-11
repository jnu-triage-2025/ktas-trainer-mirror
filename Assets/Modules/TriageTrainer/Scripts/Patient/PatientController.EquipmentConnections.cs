using System;
using System.Collections.Generic;
using System.Text;
using MultiplayerInfrastructure.Logging;
using TriageTrainer.Entity.PatientMonitor.Models;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>환자에 외부 장비가 연결된 상태를 모아 보관하는 참조 묶음.</summary>
  [Serializable]
  public struct PatientSupportExternalRefs
  {
    [SerializeField] private MovingPatientBedController _patientBed;
    [SerializeField] private List<MonoBehaviour> _intravenousFluids;
    [SerializeField] private WallAttachedWallSuction _suctionWall;
    [SerializeField] private WallAttachedOxyflowmeter _oxyflowmeter;
    [SerializeField] private List<WallAttachedWallSuction> _suctionWalls;
    [SerializeField] private List<WallAttachedOxyflowmeter> _oxyflowmeters;

    public MovingPatientBedController PatientBed
    {
      get => _patientBed;
      set => _patientBed = value;
    }

    public IReadOnlyList<MonoBehaviour> IntravenousFluids => _intravenousFluids ??= new List<MonoBehaviour>();
    public WallAttachedWallSuction SuctionWall
    {
      get => _suctionWall;
      set => _suctionWall = value;
    }

    public WallAttachedOxyflowmeter Oxyflowmeter
    {
      get => _oxyflowmeter;
      set => _oxyflowmeter = value;
    }

    public IReadOnlyList<WallAttachedWallSuction> SuctionWalls => _suctionWalls ??= new List<WallAttachedWallSuction>();
    public IReadOnlyList<WallAttachedOxyflowmeter> Oxyflowmeters => _oxyflowmeters ??= new List<WallAttachedOxyflowmeter>();

    public void SetSuctionWalls(IReadOnlyList<WallAttachedWallSuction> sources)
    {
      _suctionWalls ??= new List<WallAttachedWallSuction>();
      _suctionWalls.Clear();
      if (sources != null)
        for (int i = 0; i < sources.Count; i++)
          if (sources[i] != null && !_suctionWalls.Contains(sources[i]))
            _suctionWalls.Add(sources[i]);
      SuctionWall = _suctionWalls.Count > 0 ? _suctionWalls[0] : null;
    }

    public void SetOxyflowmeters(IReadOnlyList<WallAttachedOxyflowmeter> sources)
    {
      _oxyflowmeters ??= new List<WallAttachedOxyflowmeter>();
      _oxyflowmeters.Clear();
      if (sources != null)
        for (int i = 0; i < sources.Count; i++)
          if (sources[i] != null && !_oxyflowmeters.Contains(sources[i]))
            _oxyflowmeters.Add(sources[i]);
      Oxyflowmeter = _oxyflowmeters.Count > 0 ? _oxyflowmeters[0] : null;
    }

    public void SetIntravenousFluid(int index, MonoBehaviour fluidSource)
    {
      _intravenousFluids ??= new List<MonoBehaviour>();
      while (_intravenousFluids.Count <= index)
        _intravenousFluids.Add(null);
      _intravenousFluids[index] = fluidSource;
    }

    /// <summary>Inspector Reset 직후 이전 프리팹 기본값과 같은 빈 장비 컬렉션을 만든다.</summary>
    public void InitializeEmptyCollections()
    {
      _intravenousFluids = new List<MonoBehaviour>();
      _suctionWalls = new List<WallAttachedWallSuction>();
      _oxyflowmeters = new List<WallAttachedOxyflowmeter>();
    }

    public MonoBehaviour GetIntravenousFluid(int index) =>
      _intravenousFluids != null && index >= 0 && index < _intravenousFluids.Count
        ? _intravenousFluids[index]
        : null;
  }

  /// <summary>
  /// 환자-장비 연결 상태 추적 파셜.
  ///
  /// <para>
  /// 각 장비(침대, 모니터, 수액, 벽면 석션/산소유량계)가 이 환자에게 연결/해제될 때
  /// 장비 측에서 호출하는 진입점(Set/Clear)을 제공하고, 변경 시 C# 이벤트 + 로그 +
  /// 시나리오 상태 이벤트를 발생시킨다.
  /// </para>
  ///
  /// <para>
  /// <b>네트워크 설계:</b> 연결 상태의 진실 원천(authority)은 각 장비 컨트롤러가 보유한다
  /// (침대: SyncVar, 모니터: _monitoringPatient, 급속주입기: SyncVar).
  /// 본 파일의 필드는 "환자 측에서 장비 참조를 역조회"하기 위한 로컬 미러이며,
  /// 장비가 연결/해제 시 직접 Set/Clear 를 호출해 갱신한다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    // ── 장비 유형 식별자(이벤트 key + 로그 태그 + 시나리오 바인딩 key) ──

    public const string EquipmentTypeBed = "Bed";
    public const string EquipmentTypePatientMonitor = "PatientMonitor";
    public const string EquipmentTypeIVFluidLeftArm = "IVFluidLeftArm";
    public const string EquipmentTypeIVFluidRightArm = "IVFluidRightArm";
    public const string EquipmentTypeWallSuction = "WallSuction";
    public const string EquipmentTypeOxyflowmeter = "Oxyflowmeter";

    // ── C# 이벤트 ──

    /// <summary>
    /// 장비가 이 환자에게 연결되었을 때 발생.
    /// 인자: (장비 유형 식별자, 장비 컴포넌트 참조).
    /// </summary>
    public event Action<string, MonoBehaviour> OnEquipmentConnected;

    /// <summary>
    /// 장비가 이 환자로부터 해제되었을 때 발생.
    /// 인자: (장비 유형 식별자, 해제된 장비 컴포넌트 참조).
    /// </summary>
    public event Action<string, MonoBehaviour> OnEquipmentDisconnected;

    // ── 공통 알림 헬퍼 ──
    //
    // 모든 장비 슬롯 교체에서 동일한 순서(해제→연결)의 이벤트를 발생시킨다.
    // Unity fake-null(파괴된 MonoBehaviour)도 안전하게 처리한다.

    /// <summary>
    /// 장비 슬롯 교체에 따른 로그, C# 이벤트, 시나리오 상태 이벤트를 발생시킨다.
    /// 항상 <b>해제(disconnect) → 연결(connect)</b> 순서로 발생한다.
    /// <paramref name="previous"/> 와 <paramref name="next"/> 가 동일하면(ReferenceEquals) 아무것도 하지 않는다.
    /// </summary>
    private void NotifyEquipmentSwap(string equipmentType, MonoBehaviour previous, MonoBehaviour next)
    {
      if (ReferenceEquals(previous, next))
        return;

      // 1) 해제 알림 (이전 장비가 실제로 존재했던 경우)
      if (previous != null)
      {
        LogConnectionChange(equipmentType, connected: false, previous);
        OnEquipmentDisconnected?.Invoke(equipmentType, previous);
        NotifyPatientBCEquipmentDisconnected(equipmentType, previous);
        RaiseEquipmentStateEvent(equipmentType, connected: false);
        TriageWorldInteractionSignals.RaisePatientEquipmentDisconnected(Identifier, equipmentType, previous);

        if (previous is WallAttachedOxyflowmeter previousFlowmeter)
        {
          // 벽에서 실제로 회수된 경우에만 raw "Detached" 를 올린다. CareZone 이 회수 이벤트를
          // 먼저 받아 이 연결을 끊기 때문에, OnAnyOxyflowmeterAttachmentChanged 만으로는
          // 대상 판정이 불가능한 순서가 존재한다.
          if (!previousFlowmeter.IsAttached)
            ReportOxyflowmeterAttachment(previousFlowmeter, attached: false);
          // 유량계는 그대로 설치돼 있는데 이 환자와의 연결만 끊긴 경우는 raw 상태 변화가
          // 아니므로(= EquipmentDisconnected 가 담당) 발신 없이 추적만 해제한다.
          else if (ReferenceEquals(_reportedOxyflowmeter, previousFlowmeter))
            _reportedOxyflowmeter = null;
        }
      }

      // 2) 연결 알림 (새 장비가 실제로 존재하는 경우)
      if (next != null)
      {
        LogConnectionChange(equipmentType, connected: true, next);
        OnEquipmentConnected?.Invoke(equipmentType, next);
        if (ShouldCreditPatientBCEquipmentConnection(equipmentType))
        {
          RaiseEquipmentStateEvent(equipmentType, connected: true);
          TriageWorldInteractionSignals.RaisePatientEquipmentConnected(Identifier, equipmentType, next);
        }

        // 처치 단계 크레딧 게이팅과 무관하게, 연결된 유량계가 이미 설치되어 있으면 즉시 raw 신호를 올린다.
        // 아직 설치 전이라면 OnAnyOxyflowmeterAttachmentChanged 가 실제 설치 시점에 올린다.
        if (next is WallAttachedOxyflowmeter connectedFlowmeter && connectedFlowmeter.IsAttached)
          ReportOxyflowmeterAttachment(connectedFlowmeter, attached: true);
      }
    }

    /// <summary>
    /// "Attached" 로 보고한 유량계. 회수 시에는 CareZone 이 먼저 연결을 끊어
    /// <c>_supportExternalRefs.Oxyflowmeter</c> 가 이미 비워지므로, 짝이 되는 "Detached" 를
    /// 판정하려면 보고 시점의 대상을 따로 들고 있어야 한다.
    /// </summary>
    private WallAttachedOxyflowmeter _reportedOxyflowmeter;

    /// <summary>
    /// 씬의 어느 산소 유량계든 설치/회수 상태가 바뀌면 호출된다(정적 이벤트).
    /// 이 환자에게 연결된(또는 이 환자가 설치로 보고했던) 유량계일 때만 raw 상태 이벤트를 올린다.
    /// </summary>
    private void OnAnyOxyflowmeterAttachmentChanged(WallAttachedOxyflowmeter source, bool attached)
    {
      if (attached)
      {
        if (!ReferenceEquals(source, _supportExternalRefs.Oxyflowmeter))
          return;
      }
      else if (!ReferenceEquals(source, _reportedOxyflowmeter))
      {
        // 이미 연결이 끊긴 뒤라도, 설치로 보고했던 유량계의 회수는 반드시 짝을 맞춰 올린다.
        return;
      }

      ReportOxyflowmeterAttachment(source, attached);
    }

    /// <summary>
    /// 산소 유량계의 raw 설치/회수 상태를 시나리오 상태 이벤트로 올린다.
    /// 연결 알림 경로(<see cref="NotifyEquipmentSwap"/>)와 정적 설치 이벤트 경로는 실행 순서가
    /// 상황에 따라 뒤바뀌고 둘 다 필요하므로(이미 설치된 유량계가 나중에 연결되는 경우와 그 반대),
    /// 마지막 보고 대상을 기준으로 같은 전이가 두 번 발신되지 않도록 억제한다.
    /// </summary>
    private void ReportOxyflowmeterAttachment(WallAttachedOxyflowmeter source, bool attached)
    {
      if (source == null)
        return;

      if (attached)
      {
        if (ReferenceEquals(_reportedOxyflowmeter, source))
          return;
        _reportedOxyflowmeter = source;
      }
      else
      {
        if (!ReferenceEquals(_reportedOxyflowmeter, source))
          return;
        _reportedOxyflowmeter = null;
      }

      DispatchScenarioStateEvent(StateEventOxyflowmeterAttachmentChanged, attached ? "Attached" : "Detached");
    }

    // ── Patient Monitor (환자 상태 모니터 역참조) ──

    /// <summary>현재 이 환자를 모니터링 중인 환자 모니터. 없으면 null.</summary>
    private PatientMonitorController _monitoringPatientMonitor;

    public PatientMonitorController MonitoringPatientMonitor => _monitoringPatientMonitor;

    /// <summary>
    /// 환자 모니터가 이 환자를 모니터링 대상으로 바인딩할 때 호출한다.
    /// </summary>
    public void SetMonitoringPatientMonitor(PatientMonitorController monitor)
    {
      if (ReferenceEquals(_monitoringPatientMonitor, monitor))
        return;

      var previous = _monitoringPatientMonitor;
      _monitoringPatientMonitor = monitor;
      NotifyEquipmentSwap(EquipmentTypePatientMonitor, previous, monitor);
    }

    /// <summary>
    /// 환자 모니터가 이 환자에 대한 모니터링을 해제할 때 호출한다.
    /// 현재 바인딩된 모니터와 동일한 모니터만 해제할 수 있다(다른 모니터의 오작동 방지).
    /// </summary>
    public void ClearMonitoringPatientMonitor(PatientMonitorController monitor)
    {
      if (!ReferenceEquals(_monitoringPatientMonitor, monitor))
        return;

      var previous = _monitoringPatientMonitor;
      _monitoringPatientMonitor = null;
      NotifyEquipmentSwap(EquipmentTypePatientMonitor, previous, null);
    }

    // ── IV Fluid (수액백 / 급속주입기) ──
    //
    // 좌/우 팔 각각 하나의 수액 공급원(침대 IV 스탠드 수액백 또는 Level1RapidInfuser)을
    // 추적한다. MonoBehaviour 로 타입을 완화하여 두 유형 모두 수용한다.

    public MonoBehaviour IVFluidLeftArm => _supportExternalRefs.GetIntravenousFluid(0);
    public MonoBehaviour IVFluidRightArm => _supportExternalRefs.GetIntravenousFluid(1);

    /// <summary>
    /// IV 수액 연결을 설정한다. 기존 연결이 있으면 해제 후 새 연결로 교체한다.
    /// </summary>
    /// <param name="isLeftArm">true=좌측 팔, false=우측 팔</param>
    /// <param name="fluidSource">수액 공급원 컴포넌트(침대 또는 급속주입기). null이면 해제.</param>
    public void SetIVFluidConnection(bool isLeftArm, MonoBehaviour fluidSource)
    {
      if (isLeftArm)
      {
        var previous = _supportExternalRefs.GetIntravenousFluid(0);
        if (ReferenceEquals(previous, fluidSource))
          return;
        _supportExternalRefs.SetIntravenousFluid(0, fluidSource);
        NotifyEquipmentSwap(EquipmentTypeIVFluidLeftArm, previous, fluidSource);
      }
      else
      {
        var previous = _supportExternalRefs.GetIntravenousFluid(1);
        if (ReferenceEquals(previous, fluidSource))
          return;
        _supportExternalRefs.SetIntravenousFluid(1, fluidSource);
        NotifyEquipmentSwap(EquipmentTypeIVFluidRightArm, previous, fluidSource);
      }
    }

    /// <summary>
    /// IV 수액 연결을 해제한다.
    /// </summary>
    /// <param name="isLeftArm">true=좌측 팔, false=우측 팔</param>
    public void ClearIVFluidConnection(bool isLeftArm, MonoBehaviour expectedSource = null)
    {
      var current = isLeftArm ? IVFluidLeftArm : IVFluidRightArm;
      if (expectedSource != null && !ReferenceEquals(current, expectedSource))
        return;

      SetIVFluidConnection(isLeftArm, null);
    }

    // ── Wall Suction (벽면 석션) — future use ──

    /// <summary>현재 이 환자에 연결된 벽면 석션. 연결 메커니즘 미구현(null=미연결).</summary>
    public WallAttachedWallSuction ConnectedWallSuction => _supportExternalRefs.SuctionWall;

    public void SetConnectedWallSuction(WallAttachedWallSuction suction)
    {
      if (ReferenceEquals(_supportExternalRefs.SuctionWall, suction))
        return;

      var previous = _supportExternalRefs.SuctionWall;
      _supportExternalRefs.SuctionWall = suction;
      _supportExternalRefs.SetSuctionWalls(suction == null ? null : new[] { suction });
      NotifyEquipmentSwap(EquipmentTypeWallSuction, previous, suction);
    }

    public void SetConnectedWallSuctionConnections(IReadOnlyList<WallAttachedWallSuction> sources)
    {
      var previous = ConnectedWallSuction;
      _supportExternalRefs.SetSuctionWalls(sources);
      NotifyEquipmentSwap(EquipmentTypeWallSuction, previous, ConnectedWallSuction);
    }

    public void ClearConnectedWallSuction()
    {
      SetConnectedWallSuction(null);
    }

    public void ClearConnectedWallSuction(WallAttachedWallSuction expected)
    {
      if (expected != null && ReferenceEquals(_supportExternalRefs.SuctionWall, expected))
        SetConnectedWallSuction(null);
    }

    // ── Oxygen Flowmeter (산소 유량계) — future use ──

    /// <summary>현재 이 환자에 연결된 산소 유량계. 연결 메커니즘 미구현(null=미연결).</summary>
    public WallAttachedOxyflowmeter ConnectedOxyflowmeter => _supportExternalRefs.Oxyflowmeter;

    public void SetConnectedOxyflowmeter(WallAttachedOxyflowmeter flowmeter)
    {
      if (ReferenceEquals(_supportExternalRefs.Oxyflowmeter, flowmeter))
        return;

      var previous = _supportExternalRefs.Oxyflowmeter;
      _supportExternalRefs.Oxyflowmeter = flowmeter;
      _supportExternalRefs.SetOxyflowmeters(flowmeter == null ? null : new[] { flowmeter });
      NotifyEquipmentSwap(EquipmentTypeOxyflowmeter, previous, flowmeter);
    }

    public void SetConnectedOxyflowmeterConnections(IReadOnlyList<WallAttachedOxyflowmeter> sources)
    {
      var previous = ConnectedOxyflowmeter;
      _supportExternalRefs.SetOxyflowmeters(sources);
      NotifyEquipmentSwap(EquipmentTypeOxyflowmeter, previous, ConnectedOxyflowmeter);
    }

    public void ClearConnectedOxyflowmeter()
    {
      SetConnectedOxyflowmeter(null);
    }

    public void ClearConnectedOxyflowmeter(WallAttachedOxyflowmeter expected)
    {
      if (expected != null && ReferenceEquals(_supportExternalRefs.Oxyflowmeter, expected))
        SetConnectedOxyflowmeter(null);
    }

    // ── 침대 연결 이벤트 브리징 ──
    //
    // _currentBed 는 이미 SetCurrentBed() 로 관리되지만, 기존 코드에 이벤트/로그가 없다.
    // 기존 SetCurrentBed() 에서 호출하는 브리지 메서드를 추가해 통일된 이벤트/로그를 제공한다.

    /// <summary>
    /// 침대 연결 변경 시 기존 SetCurrentBed() 에서 호출되는 브리지.
    /// EquipmentConnected/Disconnected 이벤트와 로그를 발생시킨다.
    /// <c>SetCurrentBed</c> 내부에서만 호출되어야 한다(private).
    /// </summary>
    private void NotifyBedConnectionChanged(MovingPatientBedController previousBed, MovingPatientBedController newBed)
    {
      NotifyEquipmentSwap(EquipmentTypeBed, previousBed, newBed);
    }

    // ── 로깅 ──

    /// <summary>
    /// 장비 연결 변경을 로그로 기록한다. Unity fake-null(파괴된 MonoBehaviour)을 안전하게 처리한다.
    /// </summary>
    private void LogConnectionChange(string equipmentType, bool connected, MonoBehaviour equipment)
    {
      // Unity의 == 연산자는 파괴된 오브젝트를 null로 판정하므로,
      // C# ReferenceEquals 대신 == null 을 사용해 fake-null을 방어한다.
      string equipmentName = (equipment == null) ? "(null)" : equipment.gameObject.name;
      string action = connected ? "connected" : "disconnected";
      string message = $"Equipment {action}: patient='{Identifier}' type={equipmentType} equipment={equipmentName}";

      GameLogService.Write(GameLogCategory.Interaction, message, tag: Identifier);
    }

    // ── 시나리오 상태 이벤트 디스패치 ──

    private void RaiseEquipmentStateEvent(string equipmentType, bool connected)
    {
      string eventName = connected
        ? StateEventEquipmentConnected
        : StateEventEquipmentDisconnected;

      DispatchScenarioStateEvent(eventName, equipmentType);
    }

    // ── 디버그 / 요약 ──

    /// <summary>
    /// CareZone 장비 인식과 실제 물리 라인 연결 상태를 구분해 요약한다.
    /// </summary>
    public string GetConnectionSummary()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"Patient '{Identifier}' Equipment Status:");
      sb.AppendLine("  CareZone-assigned equipment references:");
      sb.AppendLine($"  Bed            : {FormatRef(_supportExternalRefs.PatientBed)}");
      sb.AppendLine($"  Monitor        : {FormatRef(_monitoringPatientMonitor)}");
      sb.AppendLine($"  IV Left Arm    : {FormatRef(IVFluidLeftArm)}");
      sb.AppendLine($"  IV Right Arm   : {FormatRef(IVFluidRightArm)}");
      sb.AppendLine($"  Wall Suction   : {FormatRef(_supportExternalRefs.SuctionWall)}");
      sb.AppendLine($"  Oxyflowmeter   : {FormatRef(_supportExternalRefs.Oxyflowmeter)}");
      sb.AppendLine("  Physical oxygen line:");
      sb.AppendLine($"  Flowmeter port : {FormatOxygenLineEndpoint(_supportExternalRefs.Oxyflowmeter?.OxyLineConnectionPoint)}");
      sb.AppendLine($"  Patient port   : {FormatOxygenLineEndpoint(ConfiguredOxygenMaskAttachmentPoint)}");
      sb.AppendLine($"  Connected      : {IsOxygenLinePhysicallyConnected()}");
      sb.Append(GetPatientBCOxygenTreatmentSummary());
      return sb.ToString();
    }

    private bool IsOxygenLinePhysicallyConnected()
    {
      var flowmeterPort = _supportExternalRefs.Oxyflowmeter?.OxyLineConnectionPoint;
      var patientPort = ConfiguredOxygenMaskAttachmentPoint;
      return flowmeterPort != null
             && patientPort != null
             && flowmeterPort.IsPhysicallyConnectedTo(patientPort);
    }

    private static string FormatOxygenLineEndpoint(TriageTrainer.Entity.LineConnection.LineConnectionPoint endpoint)
    {
      if (endpoint == null)
        return "(not configured)";
      return $"{endpoint.gameObject.name} (active={endpoint.isActiveAndEnabled}, id={endpoint.ConnectionIdentifier})";
    }

    [ContextMenu("Debug/Log All Equipment Connections")]
    public void DebugLogAllConnections()
    {
      string summary = GetConnectionSummary();
      Debug.Log(summary, this);
      GameLogService.Write(GameLogCategory.Misc, summary, tag: Identifier);
    }

    private static string FormatRef(object obj)
    {
      if (obj is MonoBehaviour mb)
        return mb == null ? "(destroyed)" : mb.gameObject.name;
      if (obj == null)
        return "(not connected)";
      return obj.ToString();
    }
  }
}
