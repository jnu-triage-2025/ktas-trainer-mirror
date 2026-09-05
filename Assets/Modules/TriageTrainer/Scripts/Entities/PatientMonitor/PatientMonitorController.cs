using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using TriageTrainer.Entity.Patient;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  [System.Flags]
  public enum PatientTrackingMethod
  {
    None = 0,
    DependsOnPatientCareZone = 1 << 0,
    Interactable = 1 << 1
  }

  public enum OnEnterAnotherPatientAlreadyPatientExists
  {
    Refresh,
    IgnoreNewEnter
  }

  /// <summary>환자가 연결되지 않은 동안 모니터에 표시할 데이터의 원천입니다.</summary>
  public enum DisconnectedPatientDisplayMode
  {
    /// <summary>모든 수치를 측정 불가(-1)로 표시하고, 파형은 기준선으로 유지합니다.</summary>
    UnavailableValues = 0,
    /// <summary>인스펙터에 설정된 모니터 기본값을 더미 데이터로 재생합니다.</summary>
    PlayDummyValues = 1
  }

  /// <summary>
  /// PatientMonitor의 공통 데이터/네트워크/파형 기반입니다.
  /// 실제 출력 정책은 SinglePatientMonitorController 또는 DualPatientMonitorController가 담당합니다.
  /// </summary>
  public abstract partial class PatientMonitorController : NetworkBehaviour
  {
    protected override void Reset()
    {
      // 기본값은 상호작용 추적만이다. 케어존 자동 추적은 필요한 모니터에서만 켠다.
      _patientTrackingMethod = PatientTrackingMethod.Interactable;
      _onEnterAnotherPatientAlreadyPatientExists = OnEnterAnotherPatientAlreadyPatientExists.Refresh;
    }

    public enum ECGDisplayMode
    {
      Preset,
      Custom
    }

    [Header("ECG Settings")]
    [SerializeField] private ECGDisplayMode displayMode = ECGDisplayMode.Preset;
    [SerializeField] private ECGRhythmType rhythmPreset = ECGRhythmType.NormalSinus;
    [SerializeField, Min(0f)] private float sampleRate = 200f;
    [SerializeField, Min(0f)] private float rhythmTransitionSeconds = 0.35f;
    [SerializeField] private PatientController patientState;

    [Header("환자 추적 방법")]
    [SerializeField] private PatientTrackingMethod _patientTrackingMethod = PatientTrackingMethod.Interactable;
    [SerializeField] private OnEnterAnotherPatientAlreadyPatientExists _onEnterAnotherPatientAlreadyPatientExists = OnEnterAnotherPatientAlreadyPatientExists.Refresh;

    [Header("환자 미연결 표시")]
    [SerializeField] private DisconnectedPatientDisplayMode _disconnectedPatientDisplayMode = DisconnectedPatientDisplayMode.UnavailableValues;

    public PatientTrackingMethod PatientTrackingMethod => _patientTrackingMethod;
    public OnEnterAnotherPatientAlreadyPatientExists OnEnterAnotherPatientAlreadyPatientExists => _onEnterAnotherPatientAlreadyPatientExists;
    public DisconnectedPatientDisplayMode DisconnectedPatientDisplayMode => _disconnectedPatientDisplayMode;

    [Header("Graph Appearance")]
    public Color ecgColor = Color.green;
    public Color plethColor = new Color(0f, 1f, 0.53f);
    public Color artColor = new Color(1f, 0.33f, 0.33f);
    public Color cvpColor = new Color(0.33f, 0.66f, 1f);
    public float lineThickness = 2.0f;
    [Range(10, 6000)] public int resolution = 2400;
    [SerializeField, Min(1f)] private float horizontalSecondsVisible = 12f;

    [Header("Layout")]
    [Tooltip("모니터 콘텐츠를 화면 대부분으로 확대하는 '자세히 보기' Overlay를 표시합니다.")]
    [SerializeField] private bool _enableDetailedContentOverlay = true;
    [SerializeField, Min(0f)] private float panelPadding = 4f;
    [SerializeField, Min(0f)] private float rowSpacing = 2f;
    [SerializeField, Min(28f)] private float minRowHeight = 44f;
    [SerializeField, Min(0f)] private float graphTopOffset = 10f;
    [SerializeField, Range(8, 16)] private int labelFontSize = 9;
    [SerializeField, Min(120f)] private float numericsPanelWidth = 176f;
    [SerializeField, Min(100f)] private float numericsPanelMinWidth = 136f;
    [SerializeField, Min(120f)] private float numericsPanelMaxWidth = 208f;

    protected UIDocument uiDocument;
    private PatientMonitorGraphElement ecgGraphElement;
    private PatientMonitorGraphElement plethGraphElement;
    private PatientMonitorGraphElement artGraphElement;
    private PatientMonitorGraphElement cvpGraphElement;

    private Label ecgValueLabel;
    private Label plethValueLabel;
    private Label artValueLabel;
    private Label cvpValueLabel;

    private Label bpmNumericLabel;
    private Label pvcsNumericLabel;
    private Label stNumericLabel;
    private Label prNumericLabel;
    private Label piNumericLabel;
    private Label spo2NumericLabel;
    private Label artNumericLabel;
    private Label cvpNumericLabel;
    private Label nibpNumericLabel;
    private Label t1NumericLabel;
    private Label t2NumericLabel;
    private Label deltaTNumericLabel;

    private float ecgLastBeatTime;
    private float ecgNextBeatInterval = 1f;

    private float currentTime;
    private float sampleAccumulator;
    private float _nextLabelUpdateTime;

    private ECGParameters _currentParameters;
    private ECGParameters _targetParameters;
    private float _transitionTimer;
    private ECGRuntimeState _ecgRuntimeState;
    private Action _closeRequested;
    private VisualElement _singleMonitorContent;
    private VisualElement _singleMonitorContentParent;
    protected readonly List<TriageTrainer.Entity.PatientMonitor.PatientMonitorDisplayView> _displayViews = new();

    public bool EnableDetailedContentOverlay => _enableDetailedContentOverlay;
    protected virtual void OnEnable()
    {
      // DualPatientMonitorController 는 base.OnEnable 을 호출하기 전에 없는 표시 자식을
      // 만든다. 자동 관리 콜라이더는 그 구조가 완성된 뒤에 계산한다.
      EnsureInteractionCollider();
      uiDocument = GetComponent<UIDocument>();
      // 더미 재생 값은 환자 상태가 반영되기 전, 인스펙터에 저장된 값으로 고정한다.
      CaptureDummyParametersIfNeeded();
      ResolvePatientStateIfNeeded();
      _currentParameters = ResolveConfiguredParameters();
      _targetParameters = _currentParameters;
      PullParametersFromPatientState();
      ConfigurePatientTracking();
      ecgNextBeatInterval = ComputeBaseInterval(_currentParameters.bpm);
      if (BuildsSinglePlaneGraphic)
        CreateGraphUI();
      ConfigureDisplayLayout();
      UpdateTrackingLine();
    }

    protected virtual bool BuildsSinglePlaneGraphic => true;

    protected virtual void ConfigureDisplayLayout()
    {
      _displayViews.Clear();
      var root = uiDocument != null ? uiDocument.rootVisualElement : null;
      if (root != null)
        root.style.display = DisplayStyle.Flex;
    }

    private void ResetGraphHistoryToDisconnectedValue()
    {
      ecgGraphElement?.ClearAndFillHistory(0f);
      plethGraphElement?.ClearAndFillHistory(0f);
      artGraphElement?.ClearAndFillHistory(0f);
      cvpGraphElement?.ClearAndFillHistory(0f);
      for (int i = 0; i < _displayViews.Count; i++)
        _displayViews[i].ResetGraphHistoryToValue(0f);
    }

    private void CreateGraphUI()
    {
      var root = uiDocument.rootVisualElement;
      root.Clear();

      var container = new VisualElement();
      container.style.flexGrow = 1;
      container.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.05f));
      container.style.paddingBottom = panelPadding;
      container.style.paddingTop = panelPadding;
      container.style.paddingLeft = panelPadding;
      container.style.paddingRight = panelPadding;
      container.style.flexDirection = FlexDirection.Column;
      container.style.overflow = Overflow.Hidden;

      var body = new VisualElement();
      body.style.flexGrow = 1f;
      body.style.flexDirection = FlexDirection.Row;
      body.style.overflow = Overflow.Hidden;

      var waveformColumn = new VisualElement();
      waveformColumn.style.flexGrow = 1f;
      waveformColumn.style.flexBasis = 0f;
      waveformColumn.style.flexDirection = FlexDirection.Column;
      waveformColumn.style.marginRight = 4f;

      var numericsColumn = new VisualElement();
      numericsColumn.style.width = Mathf.Clamp(numericsPanelWidth, numericsPanelMinWidth, numericsPanelMaxWidth);
      numericsColumn.style.minWidth = numericsPanelMinWidth;
      numericsColumn.style.maxWidth = numericsPanelMaxWidth;
      numericsColumn.style.flexShrink = 0f;
      numericsColumn.style.marginLeft = 2f;
      numericsColumn.style.paddingLeft = 4f;
      numericsColumn.style.paddingRight = 2f;
      numericsColumn.style.paddingTop = 2f;
      numericsColumn.style.paddingBottom = 2f;
      numericsColumn.style.backgroundColor = new StyleColor(new Color(0.02f, 0.02f, 0.02f, 0.35f));
      numericsColumn.style.flexDirection = FlexDirection.Column;
      numericsColumn.style.justifyContent = Justify.FlexStart;
      numericsColumn.style.overflow = Overflow.Visible;

      ecgGraphElement = CreateRow(waveformColumn, "ECG", ecgColor, out ecgValueLabel, MonitorChannelId.ECG, -1.5f, 2.5f);
      plethGraphElement = CreateRow(waveformColumn, "PLETH", plethColor, out plethValueLabel, MonitorChannelId.PLETH, 0f, 1.05f);
      artGraphElement = CreateRow(waveformColumn, "ART", artColor, out artValueLabel, MonitorChannelId.ART, -5f, 165f);
      cvpGraphElement = CreateRow(waveformColumn, "CVP", cvpColor, out cvpValueLabel, MonitorChannelId.CVP, -1f, 31f);

      BuildNumericsColumn(numericsColumn);

      body.Add(waveformColumn);
      body.Add(numericsColumn);
      container.Add(body);

      root.Add(container);
      _singleMonitorContent = container;
      _singleMonitorContentParent = root;
      SetVisualTreeNonInteractive(container);
      ClearRuntimeMonitorPanelSelection();
    }

    protected virtual void OpenDetailedContentOverlay() => OpenSingleDetail();

    protected virtual void CloseDetailedContentOverlay() => CloseSingleDetail();

    public void OpenPresentation()
    {
      OpenDetailedContentOverlay();
    }

    private void OpenSingleDetail()
    {
      if (!_enableDetailedContentOverlay || _singleMonitorContent == null)
        return;

      if (!TriageTrainer.Entity.PatientMonitor.PatientMonitorDetailOverlay.Open(
            this,
            new[] { _singleMonitorContent },
            RestoreSingleMonitorContent,
            RequestClose))
        return;

    }

    private void CloseSingleDetail()
    {
      TriageTrainer.Entity.PatientMonitor.PatientMonitorDetailOverlay.Close(this);
    }

    private void RestoreSingleMonitorContent()
    {
      _singleMonitorContentParent?.Add(_singleMonitorContent);
    }

    /// <summary>
    /// 시나리오가 모니터를 열 때 설정하는 종료 콜백입니다. UI 표현과 시나리오 완료
    /// 신호의 결합은 호출자에게 두어, 모니터 자체는 특정 환자/시나리오 식별자를 알지 않습니다.
    /// </summary>
    public void SetCloseRequestedHandler(Action handler)
    {
      _closeRequested = handler;
    }

    private string _scenarioClosePatientIdentifier;
    private string _scenarioCloseSignal;
    private bool _scenarioCloseArmed;
    private Action _scenarioCloseHandler;
    // 네트워크 세션에서 시나리오가 이 모니터의 닫기 완료를 승인했음을 나타내는 서버 권한 상태.
    // 시나리오 이벤트(UI 활성화)가 실행된 클라이언트에서 ArmScenarioClose 로 설정되며,
    // 실행 모드(ServerAuthoritative/호환 Local)와 무관하게 동일하게 동작한다.
    private string _serverScenarioClosePatientIdentifier;
    private string _serverScenarioCloseSignal;
    private bool _serverScenarioCloseArmed;

    public void ArmScenarioClose(PatientController patient, string completionSignal)
    {
      if (patient == null || string.IsNullOrWhiteSpace(completionSignal))
        return;

      string normalizedSignal = ScenarioInteractionSignals.Normalize(completionSignal);
      _scenarioClosePatientIdentifier = patient.Identifier;
      _scenarioCloseSignal = normalizedSignal;

      if (InstanceFinder.IsOffline)
      {
        _scenarioCloseArmed = true;
      }
      else if (IsServerStarted)
      {
        ArmScenarioCloseOnServer(patient.Identifier, normalizedSignal);
      }
      else if (IsClientInitialized)
      {
        CmdArmScenarioClose(patient.Identifier, normalizedSignal);
      }

      void HandleScenarioCloseRequested()
      {
        // 모니터 오브젝트/패널은 닫아도 월드에 그대로 남는다. 오버레이 UI는 스스로 닫힌다.
        if (InstanceFinder.IsOffline)
        {
          // 오프라인 arm은 시나리오 이벤트 시점에만 설정되므로 결과와 무관하게 종료 처리한다.
          TryCompleteScenarioClose(null, patient, normalizedSignal);
          return;
        }

        if (IsServerStarted)
        {
          // 호스트: 동일 인스턴스에서 즉시 승인 판정. 거부 시 재시도할 수 있도록 핸들러를 다시 등록한다.
          if (!TryCompleteScenarioClose(InstanceFinder.ClientManager?.Connection, patient, normalizedSignal))
            SetCloseRequestedHandler(HandleScenarioCloseRequested);
          return;
        }

        if (IsClientInitialized)
        {
          // arm 은 이벤트 시점에 한 번만 시도되는데, 그때 환자가 아직 케어존 밖에 있었거나 서버 그래프가
          // 준비되지 않았으면 조용히 거부된다. 그 뒤 담당자가 모니터를 닫아도 서버는 "arm 되지 않음"으로
          // 계속 거부하므로, 닫기 요청마다 같은 인자로 arm 을 다시 보내 그 사이 조건이 갖춰졌으면
          // 승인되게 한다. 서버 처리는 멱등이고 같은 오브젝트의 ServerRpc 는 순서가 보장된다.
          CmdArmScenarioClose(patient.Identifier, normalizedSignal);
          // 서버가 거부하면 TargetScenarioCloseRejected 로 핸들러가 재등록되어 재시도할 수 있다.
          CmdCompleteScenarioClose(patient, normalizedSignal);
          return;
        }

        Debug.LogWarning(
          "[PatientMonitorController] 시나리오 모니터 닫기가 네트워크 컨텍스트 없이 요청되어 무시됩니다.", this);
      }

      _scenarioCloseHandler = HandleScenarioCloseRequested;
      SetCloseRequestedHandler(HandleScenarioCloseRequested);
    }

    /// <summary>
    /// 시나리오 수동 진입 준비 체인이 모니터를 단계 이전 상태로 되돌릴 때 호출한다.
    /// 열려 있던 상세 오버레이를 닫고, 이전 단계에서 걸어 둔 닫기 완료 신호 arm 을 해제한다.
    /// arm 이 남아 있으면 다음 단계에서 모니터를 닫는 순간 지나간 단계의 완료 신호가 다시 올라간다.
    /// </summary>
    public void CloseForScenarioReset()
    {
      CloseSingleDetail();
      SetCloseRequestedHandler(null);
      _scenarioCloseHandler = null;
      _scenarioClosePatientIdentifier = null;
      _scenarioCloseSignal = null;
      _scenarioCloseArmed = false;
      if (InstanceFinder.IsOffline || IsServerStarted)
      {
        _serverScenarioClosePatientIdentifier = null;
        _serverScenarioCloseSignal = null;
        _serverScenarioCloseArmed = false;
      }
    }

    private void ArmScenarioCloseOnServer(string patientIdentifier, string normalizedSignal)
    {
      _serverScenarioClosePatientIdentifier = patientIdentifier;
      _serverScenarioCloseSignal = normalizedSignal;
      _serverScenarioCloseArmed = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdArmScenarioClose(string patientIdentifier, string normalizedSignal,
      NetworkConnection sender = null)
    {
      // arm 자체는 권한이 아니라 '시나리오가 이 모니터를 열었다'는 사실 기록이다.
      // 실제 권한 검증은 완료(CmdCompleteScenarioClose) 시점에 수행된다.
      string rejection = GetScenarioArmRejection(sender, patientIdentifier, normalizedSignal);
      if (rejection != null)
      {
        // 거부를 조용히 삼키면 담당자는 모니터를 닫아도 완료 신호가 올라가지 않는 이유를 알 수 없고,
        // 뒤따르는 게이트는 타임아웃까지 기다린다. 원인을 서버와 담당 클라이언트 양쪽에 남긴다.
        Debug.LogWarning(
          $"[PatientMonitorController] 시나리오 모니터 닫기 arm 이 거부되었습니다: {rejection} " +
          $"(patient='{patientIdentifier}', signal='{normalizedSignal}', " +
          $"sender={(sender != null ? sender.ClientId.ToString() : "null")}, monitor='{name}')", this);
        if (sender != null)
          TargetScenarioArmRejected(sender, rejection);
        return;
      }

      ArmScenarioCloseOnServer(patientIdentifier, normalizedSignal);
    }

    private string GetScenarioArmRejection(NetworkConnection sender, string patientIdentifier,
      string normalizedSignal)
    {
      if (sender == null
          || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || descriptor == null)
        return "발신 플레이어를 서버 세션에서 확인할 수 없습니다.";
      if (!PlayerTagService.HasTag(descriptor.Identifier, "nurse_b"))
        return "발신 플레이어에게 nurse_b 태그가 없습니다.";
      if (!IsValidPatientBCMonitorClose(patientIdentifier, normalizedSignal))
        return "환자/시그널 조합이 유효하지 않습니다.";

      var patient = FindPatient(patientIdentifier);
      if (patient == null)
        return "대상 환자를 서버에서 찾을 수 없습니다.";
      if (!IsAuthoritativeCareZoneMonitorForPatient(this, patient))
        return "모니터가 환자의 케어존 안에 있지 않습니다(환자 침대가 아직 케어존에 정박하지 않았을 수 있습니다).";
      if (ScenarioController.Instance == null
          || !ScenarioController.Instance.CanAcceptPatientBCMonitorClose(sender.ClientId, normalizedSignal))
        return "시나리오 컨트롤러가 승인하지 않습니다(활성 그래프/롤 브랜치/시그널 상태 확인).";

      return null;
    }

    [TargetRpc]
    private void TargetScenarioArmRejected(NetworkConnection connection, string rejection)
    {
      Debug.LogWarning(
        $"[PatientMonitorController] 서버가 시나리오 모니터 닫기 arm 을 거부했습니다: {rejection} " +
        "모니터를 닫을 때 다시 시도합니다. (monitor='" + name + "')", this);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompleteScenarioClose(PatientController patient, string normalizedSignal,
      NetworkConnection sender = null)
    {
      if (TryCompleteScenarioClose(sender, patient, normalizedSignal))
        return;

      if (sender != null)
        TargetScenarioCloseRejected(sender);
    }

    [TargetRpc]
    private void TargetScenarioCloseRejected(NetworkConnection connection)
    {
      // 승인 거부 시 사용자가 다시 시도할 수 있도록 종료 핸들러를 재등록한다.
      if (_closeRequested == null && _scenarioCloseHandler != null)
        SetCloseRequestedHandler(_scenarioCloseHandler);
    }

    internal bool TryCompleteScenarioClose(NetworkConnection sender, PatientController patient,
      string normalizedSignal)
    {
      string rejection = GetScenarioCloseRejection(sender, patient, normalizedSignal);
      if (rejection != null)
      {
        Debug.LogWarning(
          $"[PatientMonitorController] 시나리오 모니터 닫기 완료가 거부되었습니다: {rejection} " +
          $"(patient='{(patient != null ? patient.Identifier : "<null>")}', signal='{normalizedSignal}', " +
          $"sender={(sender != null ? sender.ClientId.ToString() : "offline/null")}, monitor='{name}')", this);
        return false;
      }

      _scenarioCloseArmed = false;
      _serverScenarioCloseArmed = false;
      using (ScenarioSignalPlayerContext.Push(sender))
        ScenarioInteractionSignals.Raise(normalizedSignal);
      return true;
    }

    private string GetScenarioCloseRejection(NetworkConnection sender, PatientController patient,
      string normalizedSignal)
    {
      if (patient == null)
        return "대상 환자가 없습니다.";
      if (!IsValidPatientBCMonitorClose(patient.Identifier, normalizedSignal))
        return "환자/시그널 조합이 유효하지 않습니다.";
      if (!IsAuthoritativeCareZoneMonitorForPatient(this, patient))
        return "모니터가 환자의 케어존 안에 있지 않습니다.";

      if (InstanceFinder.IsOffline)
      {
        if (!_scenarioCloseArmed
            || !string.Equals(patient.Identifier, _scenarioClosePatientIdentifier, StringComparison.Ordinal)
            || !string.Equals(normalizedSignal, _scenarioCloseSignal, StringComparison.Ordinal))
          return "시나리오가 이 모니터의 닫기를 arm하지 않았습니다.";
        return null;
      }

      if (!_serverScenarioCloseArmed
          || !string.Equals(patient.Identifier, _serverScenarioClosePatientIdentifier, StringComparison.Ordinal)
          || !string.Equals(normalizedSignal, _serverScenarioCloseSignal, StringComparison.Ordinal))
        return "시나리오가 이 모니터의 닫기를 arm하지 않았습니다.";
      if (sender == null
          || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || descriptor == null)
        return "발신 플레이어를 서버 세션에서 확인할 수 없습니다.";
      if (!PlayerTagService.HasTag(descriptor.Identifier, "nurse_b"))
        return "발신 플레이어에게 nurse_b 태그가 없습니다.";
      if (ScenarioController.Instance == null
          || !ScenarioController.Instance.CanAcceptPatientBCMonitorClose(sender.ClientId, normalizedSignal))
        return "시나리오 컨트롤러가 승인하지 않습니다(활성 그래프/롤 브랜치/시그널 상태 확인).";

      return null;
    }

    internal static bool IsAuthoritativeCareZoneMonitorForPatient(
      PatientMonitorController monitor,
      PatientController patient)
    {
      if (monitor == null || patient == null)
        return false;

      var zones = FindObjectsByType<PatientCareDescriptionZone>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      for (int i = 0; i < zones.Length; i++)
      {
        var zone = zones[i];
        if (zone != null
            && ReferenceEquals(zone.CurrentPatient, patient)
            && zone.ContainsWorldPosition(patient.transform.position)
            && zone.ContainsWorldPosition(monitor.transform.position))
          return true;
      }

      return false;
    }

    private static bool IsValidPatientBCMonitorClose(string patientIdentifier, string signal)
      => string.Equals(patientIdentifier, "patient_a", StringComparison.Ordinal)
         && string.Equals(signal, "sig.close_vital_ui_a", StringComparison.Ordinal)
         || string.Equals(patientIdentifier, "patient_b", StringComparison.Ordinal)
         && string.Equals(signal, "sig.close_vital_ui_b", StringComparison.Ordinal)
         || string.Equals(patientIdentifier, "patient_c", StringComparison.Ordinal)
         && string.Equals(signal, "sig.close_vital_ui_c", StringComparison.Ordinal);

    protected void RequestClose()
    {
      var handler = _closeRequested;
      _closeRequested = null;
      handler?.Invoke();
    }

    private static void SetVisualTreeNonInteractive(VisualElement root)
    {
      if (root == null)
        return;

      var stack = new Stack<VisualElement>();
      stack.Push(root);
      while (stack.Count > 0)
      {
        var current = stack.Pop();
        if (current == null)
          continue;

        current.pickingMode = PickingMode.Ignore;
        current.focusable = false;

        for (int i = 0; i < current.childCount; i++)
          stack.Push(current[i]);
      }
    }

    private static void ClearRuntimeMonitorPanelSelection()
    {
      var eventSystem = EventSystem.current;
      if (eventSystem == null)
        return;

      var selected = eventSystem.currentSelectedGameObject;
      if (selected == null)
        return;

      if (selected.name.StartsWith("PatientMonitorPanelSettings", System.StringComparison.Ordinal))
        eventSystem.SetSelectedGameObject(null);
    }

    private void BuildNumericsColumn(VisualElement parent)
    {
      AddMetricPair(parent,
        "BPM", "60", ecgColor, 16, out bpmNumericLabel,
        "PVCs", "0", ecgColor, 11, out pvcsNumericLabel);

      stNumericLabel = AddMetric(parent, "ST", "I 0.0 II 0.0 III 0.0\naVR 0.0 aVL 0.0 aVF 0.0\nV1 0.0 V2 0.0 V3 0.0\nV4 0.0 V5 0.0 V6 0.0", Color.white, 7);

      AddMetricPair(parent,
        "PR", "74", plethColor, 14, out prNumericLabel,
        "PI", "3.00", plethColor, 11, out piNumericLabel);

      spo2NumericLabel = AddMetric(parent, "%SpO2", "99%", plethColor, 18);

      AddMetricPair(parent,
        "ART", "120/80 (93) mmHg", artColor, 12, out artNumericLabel,
        "CVP", "12 mmHg", cvpColor, 12, out cvpNumericLabel);

      AddMetricPair(parent,
        "NIBP", "120/82 (95) mmHg", new Color(1f, 0.85f, 0.3f), 12, out nibpNumericLabel,
        "ΔT", "4.2°C", Color.white, 10, out deltaTNumericLabel);

      AddMetricPair(parent,
        "T1", "36.5°C", Color.white, 12, out t1NumericLabel,
        "T2", "32.3°C", Color.white, 12, out t2NumericLabel);
    }

    private Label AddMetric(VisualElement parent, string title, string initialValue, Color color, int valueFontSize)
    {
      var block = CreateMetricBlock(title, initialValue, color, valueFontSize, out var valueLabel);
      parent.Add(block);
      return valueLabel;
    }

    private void AddMetricPair(
      VisualElement parent,
      string leftTitle,
      string leftValue,
      Color leftColor,
      int leftFontSize,
      out Label leftLabel,
      string rightTitle,
      string rightValue,
      Color rightColor,
      int rightFontSize,
      out Label rightLabel)
    {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.marginBottom = 1f;
      row.style.overflow = Overflow.Visible;

      var leftBlock = CreateMetricBlock(leftTitle, leftValue, leftColor, leftFontSize, out leftLabel);
      leftBlock.style.flexGrow = 1f;
      leftBlock.style.flexBasis = 0f;
      leftBlock.style.marginRight = 2f;

      var rightBlock = CreateMetricBlock(rightTitle, rightValue, rightColor, rightFontSize, out rightLabel);
      rightBlock.style.flexGrow = 1f;
      rightBlock.style.flexBasis = 0f;

      row.Add(leftBlock);
      row.Add(rightBlock);
      parent.Add(row);
    }

    private VisualElement CreateMetricBlock(string title, string initialValue, Color color, int valueFontSize, out Label valueLabel)
    {
      var block = new VisualElement();
      block.style.paddingBottom = 2f;
      block.style.borderBottomWidth = 1f;
      block.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.08f));
      block.style.flexShrink = 0f;
      block.style.overflow = Overflow.Visible;

      var titleLabel = new Label(title);
      titleLabel.style.fontSize = 8f;
      titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      titleLabel.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.75f));
      titleLabel.style.marginBottom = 0f;

      valueLabel = new Label(initialValue);
      valueLabel.style.whiteSpace = WhiteSpace.Normal;
      valueLabel.style.fontSize = valueFontSize;
      valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      valueLabel.style.color = new StyleColor(color);
      valueLabel.style.overflow = Overflow.Visible;
      valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;

      block.Add(titleLabel);
      block.Add(valueLabel);
      return block;
    }

    private PatientMonitorGraphElement CreateRow(VisualElement container, string name, Color color, out Label valueLabel, MonitorChannelId channelId, float rangeMin, float rangeMax)
    {
      var row = new VisualElement();
      row.style.flexGrow = 1f;
      row.style.flexBasis = 0f;
      row.style.minHeight = minRowHeight;
      row.style.marginBottom = rowSpacing;
      row.style.position = Position.Relative;
      row.style.overflow = Overflow.Hidden;

      var nameLabel = new Label(name);
      nameLabel.style.position = Position.Absolute;
      nameLabel.style.left = 4f;
      nameLabel.style.top = 2f;
      nameLabel.style.color = new StyleColor(color);
      nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      nameLabel.style.fontSize = labelFontSize;

      valueLabel = new Label(string.Empty);
      valueLabel.style.position = Position.Absolute;
      valueLabel.style.right = 4f;
      valueLabel.style.top = 2f;
      valueLabel.style.color = new StyleColor(color);
      valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      valueLabel.style.fontSize = labelFontSize;

      var graph = new PatientMonitorGraphElement();
      graph.style.flexGrow = 1f;
      graph.style.marginTop = graphTopOffset;
      graph.SetColor(color);
      graph.SetLineWidth(lineThickness);
      graph.MaxPoints = ResolveHorizontalPoints();
      // 생성 직후에도 전체 표시 폭만큼 기준선 이력을 확보해 새 파형이 오른쪽에서
      // 들어와 왼쪽으로 밀리도록 한다.
      graph.ClearAndFillHistory(0f);
      graph.SetChannel(channelId, name);
      graph.SetRange(rangeMin, rangeMax);

      row.Add(graph);
      row.Add(nameLabel);
      row.Add(valueLabel);
      container.Add(row);
      return graph;
    }

    protected virtual void Update()
    {
      TryResolvePendingMonitoringPatient();
      UpdatePatientTrackingLocation();
      UpdateTrackingLine();
      if (ecgGraphElement == null && _displayViews.Count == 0)
        return;

      PullParametersFromPatientState();
      currentTime += Time.deltaTime;
      sampleAccumulator += Time.deltaTime;

      ApplyTransition(Time.deltaTime);

      float dtPerSample = 1f / Mathf.Max(1f, sampleRate);
      int maxSteps = 8;
      int steps = 0;

      while (sampleAccumulator >= dtPerSample && steps < maxSteps)
      {
        sampleAccumulator -= dtPerSample;
        TickSample(dtPerSample);
        steps++;
      }

      // 수치 문자열과 UI Toolkit text mesh는 생체 신호 샘플마다 갱신할 필요가 없다.
      // 10Hz로 제한해 문자열 할당과 text mesh 재생성을 줄인다.
      if (Time.unscaledTime >= _nextLabelUpdateTime)
      {
        _nextLabelUpdateTime = Time.unscaledTime + 0.1f;
        UpdateLabels();
      }
    }

    private void TickSample(float dt)
    {
      // 미연결 상태에서는 남아 있는 환자 참조나 리듬 프리셋과 관계없이 어떤 파형도
      // 생성하지 않는다. -1은 수치의 측정 불가 센티널이고, 그래프에는 모든 채널에서
      // 가시적인 0 기준선을 넣는다.
      if (IsDisplayingUnavailableValues())
      {
        AddSamplesToGraphs(0f, 0f, 0f, 0f);
        return;
      }

      float baseInterval = ComputeBaseInterval(_currentParameters.bpm);
      if (baseInterval != float.MaxValue && ecgNextBeatInterval == float.MaxValue)
      {
        ecgNextBeatInterval = baseInterval;
      }

      if (currentTime - ecgLastBeatTime >= ecgNextBeatInterval)
      {
        ecgLastBeatTime += ecgNextBeatInterval;
        ecgNextBeatInterval = baseInterval;
        if (_currentParameters.irregularity > 0f && ecgNextBeatInterval != float.MaxValue)
        {
          float variance = (UnityEngine.Random.value - 0.5f) * 2f * _currentParameters.irregularity * baseInterval * 0.5f;
          ecgNextBeatInterval += variance;
          ecgNextBeatInterval = Mathf.Max(0.2f, ecgNextBeatInterval);
        }

        ECGWaveformCalculator.OnBeat(rhythmPreset, ref _ecgRuntimeState);
        if (ECGWaveformCalculator.ConsumePendingPrematureBeat(ref _ecgRuntimeState))
        {
          ecgNextBeatInterval *= 0.55f;
        }
      }

      float ecgCycleNorm = CalculateCycleNorm(currentTime, ecgLastBeatTime, _currentParameters.bpm);
      float ecgVoltage = ECGWaveformCalculator.Calculate(_currentParameters, rhythmPreset, ecgCycleNorm, dt, ref _ecgRuntimeState);

      float plethCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorPleth.bpm);
      float artCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorART.bpm);
      float cvpCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorCVP.bpm);

      float plethVoltage = PlethWaveformCalculator.Calculate(monitorPleth, plethCycleNorm);
      float artVoltage = ARTWaveformCalculator.Calculate(monitorART, artCycleNorm);
      float cvpVoltage = CVPWaveformCalculator.Calculate(monitorCVP, cvpCycleNorm);
      AddSamplesToGraphs(ecgVoltage, plethVoltage, artVoltage, cvpVoltage);
    }

    private void AddSamplesToGraphs(float ecgVoltage, float plethVoltage, float artVoltage, float cvpVoltage)
    {
      ecgGraphElement?.AddValue(ecgVoltage);
      plethGraphElement?.AddValue(plethVoltage);
      artGraphElement?.AddValue(artVoltage);
      cvpGraphElement?.AddValue(cvpVoltage);
      for (int i = 0; i < _displayViews.Count; i++)
      {
        _displayViews[i].AddSamples(ecgVoltage, plethVoltage, artVoltage, cvpVoltage);
      }
    }

    private static float CalculateCycleNorm(float time, float beatTime, float bpm)
    {
      if (bpm <= 0f)
      {
        return 0f;
      }

      float period = 60f / bpm;
      if (period <= 0f)
      {
        return 0f;
      }

      float t = (time - beatTime) % period;
      if (t < 0f)
      {
        t += period;
      }

      return t / period;
    }

    // 인스펙터에서 값 변경 시 실시간 반영을 위해
    protected override void OnValidate()
    {
      base.OnValidate();
      // OnValidate 중에는 AddComponent가 금지되므로 생성은 미루고 갱신만 수행한다.
      EnsureInteractionCollider(allowCreate: false);
      EnsureInteractEntry(InteractIdSelectPatient, IsPatientTrackingMethodEnabled(PatientTrackingMethod.Interactable));
      RebuildInteractEntryMap();

      _targetParameters = ResolveConfiguredParameters();
      ApplyTrackingLineSettings();

      if (ecgGraphElement != null)
      {
        ecgGraphElement.SetColor(ecgColor);
        plethGraphElement.SetColor(plethColor);
        artGraphElement.SetColor(artColor);
        cvpGraphElement.SetColor(cvpColor);

        ecgGraphElement.SetLineWidth(lineThickness);
        plethGraphElement.SetLineWidth(lineThickness);
        artGraphElement.SetLineWidth(lineThickness);
        cvpGraphElement.SetLineWidth(lineThickness);

        int horizontalPoints = ResolveHorizontalPoints();
        ecgGraphElement.MaxPoints = horizontalPoints;
        plethGraphElement.MaxPoints = horizontalPoints;
        artGraphElement.MaxPoints = horizontalPoints;
        cvpGraphElement.MaxPoints = horizontalPoints;

        // 버퍼 크기만 늘리면 기존 샘플 수가 새 표시 폭을 채우지 못해 파형이
        // 가로로 확대된다. 설정 변경 후에도 전체 폭의 기준선을 유지한다.
        ecgGraphElement.ClearAndFillHistory(0f);
        plethGraphElement.ClearAndFillHistory(0f);
        artGraphElement.ClearAndFillHistory(0f);
        cvpGraphElement.ClearAndFillHistory(0f);
      }

      if (isActiveAndEnabled && uiDocument != null)
        ConfigureDisplayLayout();

      if (Application.isPlaying && isActiveAndEnabled)
      {
        // Inspector에서 미연결 정책을 바꿀 때 전이 감지가 없더라도 즉시 반영한다.
        if (IsPatientMonitorDisconnected())
        {
          _hasAppliedDisconnectedDisplayPolicy = false;
          PullParametersFromPatientState();
        }

        UnconfigurePatientTracking();
        ConfigurePatientTracking();
      }
    }

    protected int ResolveHorizontalPoints()
    {
      int byTime = Mathf.RoundToInt(sampleRate * horizontalSecondsVisible);
      return Mathf.Clamp(byTime, 10, resolution);
    }

    public void SetRhythm(ECGRhythmType rhythm, bool animateTransition = true)
    {
      displayMode = ECGDisplayMode.Preset;
      rhythmPreset = rhythm;
      PushParametersToPatientState();

      _targetParameters = ECGParameters.FromRhythm(rhythm);

      if (!animateTransition || rhythmTransitionSeconds <= 0f)
      {
        _currentParameters = _targetParameters;
        _transitionTimer = 0f;
      }
      else
      {
        _transitionTimer = rhythmTransitionSeconds;
      }
    }

    public void SetCustomParameters(ECGParameters customParameters, bool animateTransition = true)
    {
      displayMode = ECGDisplayMode.Custom;
      monitorECG = customParameters;
      PushParametersToPatientState();

      _targetParameters = customParameters;

      if (!animateTransition || rhythmTransitionSeconds <= 0f)
      {
        _currentParameters = _targetParameters;
        _transitionTimer = 0f;
      }
      else
      {
        _transitionTimer = rhythmTransitionSeconds;
      }
    }

    private void ApplyTransition(float deltaTime)
    {
      if (_transitionTimer <= 0f)
      {
        _currentParameters = _targetParameters;
        return;
      }

      _transitionTimer = Mathf.Max(0f, _transitionTimer - deltaTime);
      float t = 1f - (_transitionTimer / Mathf.Max(0.0001f, rhythmTransitionSeconds));
      _currentParameters = LerpParameters(_currentParameters, _targetParameters, t);
    }

    private static float ComputeBaseInterval(float bpm)
    {
      return bpm > 0f ? 60f / bpm : float.MaxValue;
    }

    private static ECGParameters LerpParameters(ECGParameters from, ECGParameters to, float t)
    {
      return new ECGParameters
      {
        bpm = Mathf.Lerp(from.bpm, to.bpm, t),
        irregularity = Mathf.Lerp(from.irregularity, to.irregularity, t),
        pAmp = Mathf.Lerp(from.pAmp, to.pAmp, t),
        pWidth = Mathf.Lerp(from.pWidth, to.pWidth, t),
        qAmp = Mathf.Lerp(from.qAmp, to.qAmp, t),
        rAmp = Mathf.Lerp(from.rAmp, to.rAmp, t),
        sAmp = Mathf.Lerp(from.sAmp, to.sAmp, t),
        tAmp = Mathf.Lerp(from.tAmp, to.tAmp, t),
        tWidth = Mathf.Lerp(from.tWidth, to.tWidth, t),
        uAmp = Mathf.Lerp(from.uAmp, to.uAmp, t),
        stElevation = Mathf.Lerp(from.stElevation, to.stElevation, t),
        noise = Mathf.Lerp(from.noise, to.noise, t),
        qrsWidthScale = Mathf.Lerp(from.qrsWidthScale, to.qrsWidthScale, t)
      };
    }

    /// <summary>
    /// 측정 불가(무의식/호흡 없음/맥박 없음/혈압 측정 불가)를 모니터에 표시할 때 사용하는 문자열.
    /// 프리셋 노드에서 수치 필드에 -1을 지정하면 이 값으로 표시된다.
    /// </summary>
    private const string UnavailableDisplay = "-?-";

    /// <summary>
    /// 수치가 "측정 불가"(<see cref="PatientMedicalState.MonitorValueUnavailable"/>)를 나타내는지 판정한다.
    /// </summary>
    private static bool IsUnavailable(float value)
    {
      return value <= PatientMedicalState.MonitorValueUnavailable;
    }

    private void UpdateLabels()
    {
      if (IsDisplayingUnavailableValues())
      {
        SetUnavailableLabels();
        return;
      }

      bool hasPatient = patientState?.Descriptor != null;
      var numerics = monitorNumerics;

      // 측정 불가(-1) 여부. 측정 불가인 경우 폴백 대신 -?- 로 표시한다.
      bool bpmUnavailable = IsUnavailable(numerics.bpm);
      bool prUnavailable = IsUnavailable(numerics.pulseRate);
      bool nibpUnavailable = IsUnavailable(monitorNIBP.systolic) || IsUnavailable(monitorNIBP.diastolic);
      // SpO2는 numerics.spo2를 우선하고, 그것이 측정 불가이면 pleth.spo2로 폴백한다.
      // 두 값이 모두 측정 불가(-1)이면 -?- 로 표시한다.
      bool spo2Unavailable = IsUnavailable(numerics.spo2) && IsUnavailable(monitorPleth.spo2);

      // 0 은 "값이 없음"이 아니라 임상적으로 유효한 값이다(무수축/무맥의 심박수 0).
      // 폴백 조건에 `> 0f` 를 쓰면 심박수 0 이 ECG 프로파일 설정값으로 대체되어, 무수축 환자가
      // 정상 심박수로 표시된다. 값 부재는 측정 불가 센티넬(-1)로만 표현되므로 그 기준으로 판정한다.
      float bpmValue = IsUnavailable(numerics.bpm) ? _currentParameters.bpm : numerics.bpm;
      float prValue = IsUnavailable(numerics.pulseRate) ? monitorPleth.bpm : numerics.pulseRate;
      float spo2Value = IsUnavailable(numerics.spo2) ? monitorPleth.spo2 : numerics.spo2;
      float piValue = IsUnavailable(numerics.perfusionIndex)
        ? (hasPatient ? 3.0f : 0f)
        : numerics.perfusionIndex;

      if (ecgValueLabel != null)
      {
        // ECG 채널의 HR 은 파형을 만들어 내는 실제 파라미터 값을 그대로 표시한다.
        ecgValueLabel.text = bpmUnavailable ? $"HR {UnavailableDisplay}" : $"HR {Mathf.RoundToInt(_currentParameters.bpm)}";
      }

      if (plethValueLabel != null)
      {
        // 숫자 라벨(spo2NumericLabel)과 동일한 해석 값을 사용한다. monitorPleth.spo2 를 직접
        // 읽으면 numerics.spo2 가 유효한 경우 두 라벨이 서로 다른 값을 표시한다.
        plethValueLabel.text = spo2Unavailable
          ? $"SpO2 {UnavailableDisplay}"
          : $"SpO2 {Mathf.RoundToInt(spo2Value)}%";
      }

      if (artValueLabel != null)
      {
        int map = Mathf.RoundToInt(monitorART.diastolic + (monitorART.systolic - monitorART.diastolic) / 3f);
        artValueLabel.text = $"{Mathf.RoundToInt(monitorART.systolic)}/{Mathf.RoundToInt(monitorART.diastolic)} ({map})";
      }

      if (cvpValueLabel != null)
      {
        cvpValueLabel.text = $"{monitorCVP.mean:0.0} mmHg";
      }

      if (bpmNumericLabel != null)
      {
        bpmNumericLabel.text = bpmUnavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(bpmValue)}";
      }

      if (pvcsNumericLabel != null)
      {
        pvcsNumericLabel.text = $"{Mathf.RoundToInt(numerics.pvcs)}";
      }

      if (stNumericLabel != null)
      {
        var st = monitorSTLeads;
        stNumericLabel.text =
          $"I {st.i:+0.0;-0.0;0.0} II {st.ii:+0.0;-0.0;0.0} III {st.iii:+0.0;-0.0;0.0}\n" +
          $"aVR {st.avr:+0.0;-0.0;0.0} aVL {st.avl:+0.0;-0.0;0.0} aVF {st.avf:+0.0;-0.0;0.0}\n" +
          $"V1 {st.v1:+0.0;-0.0;0.0} V2 {st.v2:+0.0;-0.0;0.0} V3 {st.v3:+0.0;-0.0;0.0}\n" +
          $"V4 {st.v4:+0.0;-0.0;0.0} V5 {st.v5:+0.0;-0.0;0.0} V6 {st.v6:+0.0;-0.0;0.0}";
      }

      if (prNumericLabel != null)
      {
        prNumericLabel.text = prUnavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(prValue)}";
      }

      if (piNumericLabel != null)
      {
        piNumericLabel.text = $"{piValue:0.00}";
      }

      if (spo2NumericLabel != null)
      {
        spo2NumericLabel.text = spo2Unavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(spo2Value)}%";
      }

      if (artNumericLabel != null)
      {
        int map = Mathf.RoundToInt(monitorART.diastolic + (monitorART.systolic - monitorART.diastolic) / 3f);
        artNumericLabel.text = $"{Mathf.RoundToInt(monitorART.systolic)}/{Mathf.RoundToInt(monitorART.diastolic)} ({map}) mmHg";
      }

      if (cvpNumericLabel != null)
      {
        cvpNumericLabel.text = $"{monitorCVP.mean:0.0} mmHg";
      }

      if (nibpNumericLabel != null)
      {
        if (nibpUnavailable)
        {
          nibpNumericLabel.text = UnavailableDisplay;
        }
        else
        {
          int map = Mathf.RoundToInt(monitorNIBP.diastolic + (monitorNIBP.systolic - monitorNIBP.diastolic) / 3f);
          nibpNumericLabel.text = $"{Mathf.RoundToInt(monitorNIBP.systolic)}/{Mathf.RoundToInt(monitorNIBP.diastolic)} ({map}) mmHg";
        }
      }

      if (t1NumericLabel != null)
      {
        t1NumericLabel.text = $"{monitorTemperature.t1:0.0}°C";
      }

      if (t2NumericLabel != null)
      {
        t2NumericLabel.text = $"{monitorTemperature.t2:0.0}°C";
      }

      if (deltaTNumericLabel != null)
      {
        float deltaT = Mathf.Abs(monitorTemperature.t1 - monitorTemperature.t2);
        deltaTNumericLabel.text = $"{deltaT:0.0}°C";
      }

      if (_displayViews.Count > 0)
      {
        string st =
          $"I {monitorSTLeads.i:+0.0;-0.0;0.0} II {monitorSTLeads.ii:+0.0;-0.0;0.0} III {monitorSTLeads.iii:+0.0;-0.0;0.0}\n" +
          $"aVR {monitorSTLeads.avr:+0.0;-0.0;0.0} aVL {monitorSTLeads.avl:+0.0;-0.0;0.0} aVF {monitorSTLeads.avf:+0.0;-0.0;0.0}\n" +
          $"V1 {monitorSTLeads.v1:+0.0;-0.0;0.0} V2 {monitorSTLeads.v2:+0.0;-0.0;0.0} V3 {monitorSTLeads.v3:+0.0;-0.0;0.0}\n" +
          $"V4 {monitorSTLeads.v4:+0.0;-0.0;0.0} V5 {monitorSTLeads.v5:+0.0;-0.0;0.0} V6 {monitorSTLeads.v6:+0.0;-0.0;0.0}";
        string[] metrics = {
          bpmUnavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(bpmValue)}",
          $"{Mathf.RoundToInt(numerics.pvcs)}", st,
          prUnavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(prValue)}", $"{piValue:0.00}",
          spo2Unavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(spo2Value)}%",
          $"{Mathf.RoundToInt(monitorART.systolic)}/{Mathf.RoundToInt(monitorART.diastolic)} ({Mathf.RoundToInt(monitorART.diastolic + (monitorART.systolic - monitorART.diastolic) / 3f)}) mmHg",
          $"{monitorCVP.mean:0.0} mmHg",
          nibpUnavailable ? UnavailableDisplay : $"{Mathf.RoundToInt(monitorNIBP.systolic)}/{Mathf.RoundToInt(monitorNIBP.diastolic)} ({Mathf.RoundToInt(monitorNIBP.diastolic + (monitorNIBP.systolic - monitorNIBP.diastolic) / 3f)}) mmHg",
          $"{Mathf.Abs(monitorTemperature.t1 - monitorTemperature.t2):0.0}°C", $"{monitorTemperature.t1:0.0}°C", $"{monitorTemperature.t2:0.0}°C" };
        for (int i = 0; i < _displayViews.Count; i++)
        {
          _displayViews[i].SetGraphValues(ecgValueLabel?.text, plethValueLabel?.text, artValueLabel?.text, cvpValueLabel?.text);
          _displayViews[i].SetMetricValues(metrics);
        }
      }
    }

    private void SetUnavailableLabels()
    {
      if (ecgValueLabel != null)
        ecgValueLabel.text = $"HR {UnavailableDisplay}";
      if (plethValueLabel != null)
        plethValueLabel.text = $"SpO2 {UnavailableDisplay}";
      if (artValueLabel != null)
        artValueLabel.text = UnavailableDisplay;
      if (cvpValueLabel != null)
        cvpValueLabel.text = UnavailableDisplay;
      if (bpmNumericLabel != null)
        bpmNumericLabel.text = UnavailableDisplay;
      if (pvcsNumericLabel != null)
        pvcsNumericLabel.text = UnavailableDisplay;
      if (stNumericLabel != null)
        stNumericLabel.text = UnavailableDisplay;
      if (prNumericLabel != null)
        prNumericLabel.text = UnavailableDisplay;
      if (piNumericLabel != null)
        piNumericLabel.text = UnavailableDisplay;
      if (spo2NumericLabel != null)
        spo2NumericLabel.text = UnavailableDisplay;
      if (artNumericLabel != null)
        artNumericLabel.text = UnavailableDisplay;
      if (cvpNumericLabel != null)
        cvpNumericLabel.text = UnavailableDisplay;
      if (nibpNumericLabel != null)
        nibpNumericLabel.text = UnavailableDisplay;
      if (t1NumericLabel != null)
        t1NumericLabel.text = UnavailableDisplay;
      if (t2NumericLabel != null)
        t2NumericLabel.text = UnavailableDisplay;
      if (deltaTNumericLabel != null)
        deltaTNumericLabel.text = UnavailableDisplay;

      for (int i = 0; i < _displayViews.Count; i++)
      {
        _displayViews[i].SetGraphValues(UnavailableDisplay, UnavailableDisplay, UnavailableDisplay, UnavailableDisplay);
        _displayViews[i].SetMetricValues(
          UnavailableDisplay, UnavailableDisplay, UnavailableDisplay, UnavailableDisplay,
          UnavailableDisplay, UnavailableDisplay, UnavailableDisplay, UnavailableDisplay,
          UnavailableDisplay, UnavailableDisplay, UnavailableDisplay, UnavailableDisplay);
      }
    }
  }
}
