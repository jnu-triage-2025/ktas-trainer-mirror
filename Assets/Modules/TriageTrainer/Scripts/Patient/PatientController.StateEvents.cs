using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Entity;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 상태(state) 변경을 세분화된 C# 이벤트로 노출하고, 범용
  /// <see cref="IScenarioEntityStateEventSource"/> 를 구현하여 시나리오 그래프가
  /// 이 이벤트들을 신호로 변환할 수 있게 한다.
  ///
  /// <para>설계 원칙</para>
  /// <list type="bullet">
  /// <item><b>State</b>: 실제 값은 기존 저장소(<see cref="PatientDisplayState"/> 처치 플래그,
  ///   <see cref="PatientMedicalState"/>, 트리아지 SyncVar)가 그대로 보유한다. 본 파일은
  ///   그 값들의 <b>변경 시점</b>을 이벤트로만 노출한다(값 이중화를 하지 않는다).</item>
  /// <item><b>이벤트</b>: 처치 적용/해제, 활력(의료상태) 변경, 트리아지 제출을 각각
  ///   <c>On...Applied / On...Removed / On...Changed / On...Submitted</c> 로 노출한다.</item>
  /// <item><b>시나리오 연동</b>: 이벤트는 서버(또는 오프라인 호스트) 컨텍스트의 상태 적용
  ///   지점에서 발생하므로, 그 지점에서 <c>ScenarioInteractionSignals.Raise</c> 가 서버 권위로
  ///   동작한다. 실제 신호 매핑은 시나리오의
  ///   <c>EntityStateSignalBinding</c> 노드가
  ///   <see cref="IScenarioEntityStateEventSource"/> 를 통해 등록한다.</item>
  /// </list>
  ///
  /// <para><b>이벤트 목록(검토용)</b> — 아래 <see cref="GetStateEventNames"/> 가 런타임에서
  /// 동일 목록을 반환한다:</para>
  /// <list type="table">
  /// <listheader><term>이벤트 이름</term><term>C# 이벤트</term><term>인자(key)</term></listheader>
  /// <item><term>TreatmentApplied</term><term><see cref="OnTreatmentApplied"/></term><term>처치 표시 항목명(<see cref="TreatmentDisplay"/>)</term></item>
  /// <item><term>TreatmentRemoved</term><term><see cref="OnTreatmentRemoved"/></term><term>처치 표시 항목명</term></item>
  /// <item><term>VitalChanged</term><term><see cref="OnVitalChanged"/></term><term>(없음)</term></item>
  /// <item><term>TriageSubmitted</term><term><see cref="OnTriageSubmitted"/></term><term>트리아지 등급명(<see cref="TriageLevel"/>)</term></item>
  /// <item><term>EquipmentConnected</term><term>(C# 이벤트 없음)</term><term>장비 유형명(<see cref="EquipmentTypeBed"/>/<see cref="EquipmentTypePatientMonitor"/> 등)</term></item>
  /// <item><term>EquipmentDisconnected</term><term>(C# 이벤트 없음)</term><term>장비 유형명</term></item>
  /// </list>
  /// </summary>
  public partial class PatientController : IScenarioEntityStateEventSource
  {
    // ── 세분화 상태 이벤트(도메인 타입) ──

    /// <summary>처치 표시 항목이 새로 켜졌을 때(false→true) 발생. 인자는 켜진 항목.</summary>
    public event Action<TreatmentDisplay> OnTreatmentApplied;

    /// <summary>처치 표시 항목이 꺼졌을 때(true→false) 발생. 인자는 꺼진 항목.</summary>
    public event Action<TreatmentDisplay> OnTreatmentRemoved;

    /// <summary>의료 상태(활력/모니터 수치 포함)가 변경되었을 때 발생. 인자는 변경된 스냅샷.</summary>
    public event Action<PatientMedicalState> OnVitalChanged;

    /// <summary>트리아지 등급이 제출/확정되었을 때 발생. 인자는 확정된 등급.</summary>
    public event Action<TriageLevel> OnTriageSubmitted;

    // ── 범용 상태 이벤트 소스(IScenarioEntityStateEventSource) ──

    /// <summary>상태 이벤트 이름 상수. 시나리오 노드의 eventName 과 일치해야 한다.</summary>
    public const string StateEventTreatmentApplied = "TreatmentApplied";
    public const string StateEventTreatmentRemoved = "TreatmentRemoved";
    public const string StateEventVitalChanged = "VitalChanged";
    public const string StateEventTriageSubmitted = "TriageSubmitted";

    /// <summary>장비가 환자에게 연결되었을 때 발생. key는 장비 유형명(<see cref="EquipmentTypeBed"/> 등).</summary>
    public const string StateEventEquipmentConnected = "EquipmentConnected";
    /// <summary>장비가 환자로부터 해제되었을 때 발생. key는 장비 유형명.</summary>
    public const string StateEventEquipmentDisconnected = "EquipmentDisconnected";

    /// <summary>
    /// 시나리오 <c>EntityStateSignalBinding</c> 노드가 (eventName, key) 조합에 대해 등록한 핸들러.
    /// key 가 비어 있으면 해당 이벤트의 모든 발생에 대해 호출된다.
    /// </summary>
    private readonly List<ScenarioStateEventBinding> _scenarioStateEventBindings = new();

    private readonly struct ScenarioStateEventBinding
    {
      public readonly string EventName;
      public readonly string Key; // null/empty = 모든 key 매칭
      public readonly Action<string> Callback;

      public ScenarioStateEventBinding(string eventName, string key, Action<string> callback)
      {
        EventName = eventName;
        Key = key;
        Callback = callback;
      }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetStateEventNames() => StateEventNames;

    private static readonly string[] StateEventNames =
    {
      StateEventTreatmentApplied,
      StateEventTreatmentRemoved,
      StateEventVitalChanged,
      StateEventTriageSubmitted,
      StateEventEquipmentConnected,
      StateEventEquipmentDisconnected,
    };

    /// <inheritdoc />
    public bool RegisterStateEventListener(string eventName, string key, Action<string> onFired)
    {
      if (string.IsNullOrWhiteSpace(eventName) || onFired == null)
        return false;

      if (Array.IndexOf(StateEventNames, eventName) < 0)
        return false;

      _scenarioStateEventBindings.Add(new ScenarioStateEventBinding(eventName, key, onFired));
      return true;
    }

    /// <inheritdoc />
    public void UnregisterStateEventListeners(string eventName)
    {
      if (string.IsNullOrWhiteSpace(eventName))
      {
        _scenarioStateEventBindings.Clear();
        return;
      }

      for (int i = _scenarioStateEventBindings.Count - 1; i >= 0; i--)
      {
        if (string.Equals(_scenarioStateEventBindings[i].EventName, eventName, StringComparison.Ordinal))
          _scenarioStateEventBindings.RemoveAt(i);
      }
    }

    /// <summary>
    /// 등록된 시나리오 상태 이벤트 바인딩 중 (eventName, key) 에 매칭되는 것들의 콜백을 호출한다.
    /// key 가 없는 바인딩(모든 key 매칭)도 함께 호출된다.
    /// </summary>
    private void DispatchScenarioStateEvent(string eventName, string key)
    {
      if (_scenarioStateEventBindings.Count == 0)
        return;

      // 콜백이 재진입(등록/해제)할 수 있으므로 스냅샷을 뜬 뒤 순회한다.
      for (int i = 0; i < _scenarioStateEventBindings.Count; i++)
      {
        var binding = _scenarioStateEventBindings[i];
        if (!string.Equals(binding.EventName, eventName, StringComparison.Ordinal))
          continue;

        bool keyMatches = string.IsNullOrEmpty(binding.Key)
                          || string.Equals(binding.Key, key, StringComparison.Ordinal);
        if (keyMatches)
          binding.Callback?.Invoke(key);
      }
    }

    // ── 이벤트 발생 지점(각 상태 적용 로직에서 호출) ──

    /// <summary>처치 표시 항목 전이 발생 시 호출(true=켜짐, false=꺼짐).</summary>
    private void RaiseTreatmentStateEvent(TreatmentDisplay display, bool active)
    {
      if (display == TreatmentDisplay.None)
        return;

      string key = display.ToString();
      if (active)
      {
        OnTreatmentApplied?.Invoke(display);
        DispatchScenarioStateEvent(StateEventTreatmentApplied, key);
      }
      else
      {
        OnTreatmentRemoved?.Invoke(display);
        DispatchScenarioStateEvent(StateEventTreatmentRemoved, key);
      }
    }

    /// <summary>의료 상태 변경 발생 시 호출.</summary>
    private void RaiseVitalChangedEvent(PatientMedicalState state)
    {
      OnVitalChanged?.Invoke(state);
      DispatchScenarioStateEvent(StateEventVitalChanged, null);
    }

    /// <summary>트리아지 확정 발생 시 호출.</summary>
    private void RaiseTriageSubmittedEvent(TriageLevel level)
    {
      OnTriageSubmitted?.Invoke(level);
      DispatchScenarioStateEvent(StateEventTriageSubmitted, level.ToString());
    }
  }
}
