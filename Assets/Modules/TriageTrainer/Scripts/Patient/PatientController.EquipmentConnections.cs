using System;
using System.Text;
using MultiplayerInfrastructure.Logging;
using TriageTrainer.Entity.PatientMonitor.Models;
using UnityEngine;

namespace TriageTrainer.Entity
{
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

      if (previous != null)
      {
        LogConnectionChange(EquipmentTypePatientMonitor, connected: false, previous);
        OnEquipmentDisconnected?.Invoke(EquipmentTypePatientMonitor, previous);
        RaiseEquipmentStateEvent(EquipmentTypePatientMonitor, connected: false);
      }

      if (monitor != null)
      {
        LogConnectionChange(EquipmentTypePatientMonitor, connected: true, monitor);
        OnEquipmentConnected?.Invoke(EquipmentTypePatientMonitor, monitor);
        RaiseEquipmentStateEvent(EquipmentTypePatientMonitor, connected: true);
      }
    }

    /// <summary>
    /// 환자 모니터가 이 환자에 대한 모니터링을 해제할 때 호출한다.
    /// </summary>
    public void ClearMonitoringPatientMonitor(PatientMonitorController monitor)
    {
      if (monitor != null && !ReferenceEquals(_monitoringPatientMonitor, monitor))
        return;

      var previous = _monitoringPatientMonitor;
      _monitoringPatientMonitor = null;

      if (previous != null)
      {
        LogConnectionChange(EquipmentTypePatientMonitor, connected: false, previous);
        OnEquipmentDisconnected?.Invoke(EquipmentTypePatientMonitor, previous);
        RaiseEquipmentStateEvent(EquipmentTypePatientMonitor, connected: false);
      }
    }

    // ── IV Fluid (수액백 / 급속주입기) ──
    //
    // 좌/우 팔 각각 하나의 수액 공급원(침대 IV 스탠드 수액백 또는 Level1RapidInfuser)을
    // 추적한다. MonoBehaviour 로 타입을 완화하여 두 유형 모두 수용한다.

    /// <summary>좌측 팔에 연결된 수액 공급원(침대 또는 급속주입기). 없으면 null.</summary>
    private MonoBehaviour _ivFluidLeftArm;

    /// <summary>우측 팔에 연결된 수액 공급원(침대 또는 급속주입기). 없으면 null.</summary>
    private MonoBehaviour _ivFluidRightArm;

    public MonoBehaviour IVFluidLeftArm => _ivFluidLeftArm;
    public MonoBehaviour IVFluidRightArm => _ivFluidRightArm;

    /// <summary>
    /// IV 수액 연결을 설정한다.
    /// </summary>
    /// <param name="isLeftArm">true=좌측 팔, false=우측 팔</param>
    /// <param name="fluidSource">수액 공급원 컴포넌트(침대 또는 급속주입기)</param>
    public void SetIVFluidConnection(bool isLeftArm, MonoBehaviour fluidSource)
    {
      if (isLeftArm)
        SetIVFluidConnectionInternal(ref _ivFluidLeftArm, EquipmentTypeIVFluidLeftArm, fluidSource);
      else
        SetIVFluidConnectionInternal(ref _ivFluidRightArm, EquipmentTypeIVFluidRightArm, fluidSource);
    }

    /// <summary>
    /// IV 수액 연결을 해제한다.
    /// </summary>
    /// <param name="isLeftArm">true=좌측 팔, false=우측 팔</param>
    public void ClearIVFluidConnection(bool isLeftArm)
    {
      if (isLeftArm)
        SetIVFluidConnectionInternal(ref _ivFluidLeftArm, EquipmentTypeIVFluidLeftArm, null);
      else
        SetIVFluidConnectionInternal(ref _ivFluidRightArm, EquipmentTypeIVFluidRightArm, null);
    }

    private void SetIVFluidConnectionInternal(ref MonoBehaviour slot, string equipmentType, MonoBehaviour fluidSource)
    {
      if (ReferenceEquals(slot, fluidSource))
        return;

      var previous = slot;
      slot = fluidSource;

      if (previous != null)
      {
        LogConnectionChange(equipmentType, connected: false, previous);
        OnEquipmentDisconnected?.Invoke(equipmentType, previous);
        RaiseEquipmentStateEvent(equipmentType, connected: false);
      }

      if (fluidSource != null)
      {
        LogConnectionChange(equipmentType, connected: true, fluidSource);
        OnEquipmentConnected?.Invoke(equipmentType, fluidSource);
        RaiseEquipmentStateEvent(equipmentType, connected: true);
      }
    }

    // ── Wall Suction (벽면 석션) — future use ──

    /// <summary>현재 이 환자에 연결된 벽면 석션. 연결 메커니즘 미구현(null=미연결).</summary>
    private WallAttachedWallSuction _connectedWallSuction;

    public WallAttachedWallSuction ConnectedWallSuction => _connectedWallSuction;

    public void SetConnectedWallSuction(WallAttachedWallSuction suction)
    {
      if (ReferenceEquals(_connectedWallSuction, suction))
        return;

      var previous = _connectedWallSuction;
      _connectedWallSuction = suction;

      if (suction != null)
      {
        LogConnectionChange(EquipmentTypeWallSuction, connected: true, suction);
        OnEquipmentConnected?.Invoke(EquipmentTypeWallSuction, suction);
        RaiseEquipmentStateEvent(EquipmentTypeWallSuction, connected: true);
      }
      else if (previous != null)
      {
        LogConnectionChange(EquipmentTypeWallSuction, connected: false, previous);
        OnEquipmentDisconnected?.Invoke(EquipmentTypeWallSuction, previous);
        RaiseEquipmentStateEvent(EquipmentTypeWallSuction, connected: false);
      }
    }

    public void ClearConnectedWallSuction()
    {
      SetConnectedWallSuction(null);
    }

    // ── Oxygen Flowmeter (산소 유량계) — future use ──

    /// <summary>현재 이 환자에 연결된 산소 유량계. 연결 메커니즘 미구현(null=미연결).</summary>
    private WallAttachedOxyflowmeter _connectedOxyflowmeter;

    public WallAttachedOxyflowmeter ConnectedOxyflowmeter => _connectedOxyflowmeter;

    public void SetConnectedOxyflowmeter(WallAttachedOxyflowmeter flowmeter)
    {
      if (ReferenceEquals(_connectedOxyflowmeter, flowmeter))
        return;

      var previous = _connectedOxyflowmeter;
      _connectedOxyflowmeter = flowmeter;

      if (flowmeter != null)
      {
        LogConnectionChange(EquipmentTypeOxyflowmeter, connected: true, flowmeter);
        OnEquipmentConnected?.Invoke(EquipmentTypeOxyflowmeter, flowmeter);
        RaiseEquipmentStateEvent(EquipmentTypeOxyflowmeter, connected: true);
      }
      else if (previous != null)
      {
        LogConnectionChange(EquipmentTypeOxyflowmeter, connected: false, previous);
        OnEquipmentDisconnected?.Invoke(EquipmentTypeOxyflowmeter, previous);
        RaiseEquipmentStateEvent(EquipmentTypeOxyflowmeter, connected: false);
      }
    }

    public void ClearConnectedOxyflowmeter()
    {
      SetConnectedOxyflowmeter(null);
    }

    // ── Bed connection event bridging ──
    //
    // _currentBed 는 이미 SetCurrentBed() 로 관리되지만, 기존 코드에 이벤트/로그가 없다.
    // 기존 SetCurrentBed() 에서 호출하는 브리지 메서드를 추가해 통일된 이벤트/로그를 제공한다.

    /// <summary>
    /// 침대 연결 변경 시 기존 SetCurrentBed() 에서 호출되는 브리지.
    /// EquipmentConnected/Disconnected 이벤트와 로그를 발생시킨다.
    /// </summary>
    internal void NotifyBedConnectionChanged(MovingPatientBedController previousBed, MovingPatientBedController newBed)
    {
      if (ReferenceEquals(previousBed, newBed))
        return;

      if (previousBed != null)
      {
        LogConnectionChange(EquipmentTypeBed, connected: false, previousBed);
        OnEquipmentDisconnected?.Invoke(EquipmentTypeBed, previousBed);
        RaiseEquipmentStateEvent(EquipmentTypeBed, connected: false);
      }

      if (newBed != null)
      {
        LogConnectionChange(EquipmentTypeBed, connected: true, newBed);
        OnEquipmentConnected?.Invoke(EquipmentTypeBed, newBed);
        RaiseEquipmentStateEvent(EquipmentTypeBed, connected: true);
      }
    }

    // ── Logging ──

    private void LogConnectionChange(string equipmentType, bool connected, MonoBehaviour equipment)
    {
      string equipmentName = equipment != null ? equipment.gameObject.name : "(null)";
      string action = connected ? "connected" : "disconnected";
      string message = $"Equipment {action}: patient='{Identifier}' type={equipmentType} equipment={equipmentName}";

      GameLogService.Write(GameLogCategory.Interaction, message, tag: Identifier);
    }

    // ── Scenario State Event dispatch ──

    private void RaiseEquipmentStateEvent(string equipmentType, bool connected)
    {
      string eventName = connected
        ? StateEventEquipmentConnected
        : StateEventEquipmentDisconnected;

      DispatchScenarioStateEvent(eventName, equipmentType);
    }

    // ── Debug / Summary ──

    /// <summary>
    /// 현재 모든 장비 연결 상태를 문자열로 요약한다(디버그/인스펙터 표시용).
    /// </summary>
    public string GetConnectionSummary()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"Patient '{Identifier}' Equipment Connections:");
      sb.AppendLine($"  Bed            : {FormatRef(_currentBed)}");
      sb.AppendLine($"  Monitor        : {FormatRef(_monitoringPatientMonitor)}");
      sb.AppendLine($"  IV Left Arm    : {FormatRef(_ivFluidLeftArm)}");
      sb.AppendLine($"  IV Right Arm   : {FormatRef(_ivFluidRightArm)}");
      sb.AppendLine($"  Wall Suction   : {FormatRef(_connectedWallSuction)}");
      sb.AppendLine($"  Oxyflowmeter   : {FormatRef(_connectedOxyflowmeter)}");
      return sb.ToString();
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
      if (obj == null) return "(not connected)";
      if (obj is MonoBehaviour mb) return mb.gameObject.name;
      return obj.ToString();
    }
  }
}
