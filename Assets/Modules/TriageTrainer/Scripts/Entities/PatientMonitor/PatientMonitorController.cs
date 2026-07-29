using System;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using TriageTrainer.Entity.Patient;

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

  /// <summary>
  /// PatientMonitor의 공통 데이터/네트워크/파형 기반입니다.
  /// 실제 출력 정책은 SinglePatientMonitorController 또는 DualPatientMonitorController가 담당합니다.
  /// </summary>
  public abstract partial class PatientMonitorController : NetworkBehaviour
  {
    protected virtual void Reset()
    {
      _patientTrackingMethod = PatientTrackingMethod.Interactable;
      _onEnterAnotherPatientAlreadyPatientExists = OnEnterAnotherPatientAlreadyPatientExists.Refresh;
    }

    protected void SetDefaultPatientTrackingMethod(PatientTrackingMethod method)
    {
      _patientTrackingMethod = method;
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

    public PatientTrackingMethod PatientTrackingMethod => _patientTrackingMethod;
    public OnEnterAnotherPatientAlreadyPatientExists OnEnterAnotherPatientAlreadyPatientExists => _onEnterAnotherPatientAlreadyPatientExists;

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
      // DualPatientMonitorController creates missing display children before calling base.OnEnable.
      // Calculate the auto-managed collider after that structure is complete.
      EnsureInteractionCollider();
      uiDocument = GetComponent<UIDocument>();
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

    void CreateGraphUI()
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

    private void OpenSingleDetail()
    {
      if (!_enableDetailedContentOverlay || _singleMonitorContent == null)
        return;

      if (!TriageTrainer.Entity.PatientMonitor.PatientMonitorDetailOverlay.Open(
            this,
            new[] { _singleMonitorContent },
            RestoreSingleMonitorContent))
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

    protected void RequestClose()
    {
      _closeRequested?.Invoke();
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
      UpdatePatientTrackingLocation();
      UpdateTrackingLine();
      if (ecgGraphElement == null && _displayViews.Count == 0) return;

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

      UpdateLabels();
    }

    private void TickSample(float dt)
    {
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
      ecgGraphElement?.AddValue(ecgVoltage);

      float plethCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorPleth.bpm);
      float artCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorART.bpm);
      float cvpCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorCVP.bpm);

      float plethVoltage = PlethWaveformCalculator.Calculate(monitorPleth, plethCycleNorm);
      float artVoltage = ARTWaveformCalculator.Calculate(monitorART, artCycleNorm);
      float cvpVoltage = CVPWaveformCalculator.Calculate(monitorCVP, cvpCycleNorm);
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
    protected virtual void OnValidate()
    {
      EnsureInteractionCollider();
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
      }

      if (isActiveAndEnabled && uiDocument != null)
        ConfigureDisplayLayout();

      if (Application.isPlaying && isActiveAndEnabled)
      {
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
      bool hasPatient = patientState?.Descriptor != null;
      var numerics = monitorNumerics;

      // 측정 불가(-1) 여부. 측정 불가인 경우 폴백(> 0f) 대신 -?- 로 표시한다.
      bool bpmUnavailable = IsUnavailable(numerics.bpm);
      bool prUnavailable = IsUnavailable(numerics.pulseRate);
      bool nibpUnavailable = IsUnavailable(monitorNIBP.systolic) || IsUnavailable(monitorNIBP.diastolic);
      // SpO2는 numerics.spo2(> 0f)를 우선하고, 없으면 pleth.spo2로 폴백한다.
      // 두 값이 모두 측정 불가(-1)이면 -?- 로 표시한다.
      bool spo2Unavailable = IsUnavailable(numerics.spo2) && IsUnavailable(monitorPleth.spo2);

      float bpmValue = numerics.bpm > 0f ? numerics.bpm : _currentParameters.bpm;
      float prValue = numerics.pulseRate > 0f ? numerics.pulseRate : monitorPleth.bpm;
      float spo2Value = numerics.spo2 > 0f ? numerics.spo2 : monitorPleth.spo2;
      float piValue = numerics.perfusionIndex > 0f ? numerics.perfusionIndex : (hasPatient ? 3.0f : 0f);

      if (ecgValueLabel != null)
      {
        ecgValueLabel.text = bpmUnavailable ? $"HR {UnavailableDisplay}" : $"HR {Mathf.RoundToInt(_currentParameters.bpm)}";
      }

      if (plethValueLabel != null)
      {
        plethValueLabel.text = spo2Unavailable
          ? $"SpO2 {UnavailableDisplay}"
          : $"SpO2 {Mathf.RoundToInt(monitorPleth.spo2)}%";
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
  }
}
