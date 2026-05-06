using UnityEngine;
using UnityEngine.UIElements;
using TriageTrainer.Entity.Patient;
using TriageTrainer.Patient;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  [RequireComponent(typeof(UIDocument))]
  public class PatientMonitorController : MonoBehaviour
  {
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
    [SerializeField] private PatientMonitorParameters monitorParameters = PatientMonitorParameters.Default;
    [SerializeField] private PatientStateABC patientState;

    [Header("Graph Appearance")]
    public Color ecgColor = Color.green;
    public Color plethColor = new Color(0f, 1f, 0.53f);
    public Color artColor = new Color(1f, 0.33f, 0.33f);
    public Color cvpColor = new Color(0.33f, 0.66f, 1f);
    public float lineThickness = 2.0f;
    [Range(10, 6000)] public int resolution = 2400;
    [SerializeField, Min(1f)] private float horizontalSecondsVisible = 12f;

    [Header("Layout")]
    [SerializeField, Min(0f)] private float panelPadding = 4f;
    [SerializeField, Min(0f)] private float rowSpacing = 2f;
    [SerializeField, Min(28f)] private float minRowHeight = 44f;
    [SerializeField, Min(0f)] private float graphTopOffset = 10f;
    [SerializeField, Range(8, 16)] private int labelFontSize = 9;
    [SerializeField, Min(120f)] private float numericsPanelWidth = 176f;
    [SerializeField, Min(100f)] private float numericsPanelMinWidth = 136f;
    [SerializeField, Min(120f)] private float numericsPanelMaxWidth = 208f;

    private UIDocument uiDocument;
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

    void OnEnable()
    {
      uiDocument = GetComponent<UIDocument>();
      ResolvePatientStateIfNeeded();
      PullParametersFromPatientState();
      _currentParameters = ResolveConfiguredParameters();
      _targetParameters = _currentParameters;
      ecgNextBeatInterval = ComputeBaseInterval(_currentParameters.bpm);
      CreateGraphUI();
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

    void Update()
    {
      if (ecgGraphElement == null) return;

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
          float variance = (Random.value - 0.5f) * 2f * _currentParameters.irregularity * baseInterval * 0.5f;
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
      ecgGraphElement.AddValue(ecgVoltage);

      float plethCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorParameters.pleth.bpm);
      float artCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorParameters.art.bpm);
      float cvpCycleNorm = CalculateCycleNorm(currentTime, 0f, monitorParameters.cvp.bpm);

      plethGraphElement.AddValue(PlethWaveformCalculator.Calculate(monitorParameters.pleth, plethCycleNorm));
      artGraphElement.AddValue(ARTWaveformCalculator.Calculate(monitorParameters.art, artCycleNorm));
      cvpGraphElement.AddValue(CVPWaveformCalculator.Calculate(monitorParameters.cvp, cvpCycleNorm));
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
    private void OnValidate()
    {
      _targetParameters = ResolveConfiguredParameters();

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
    }

    private int ResolveHorizontalPoints()
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
      monitorParameters.ecg = customParameters;
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

    private ECGParameters ResolveConfiguredParameters()
    {
      return displayMode == ECGDisplayMode.Preset ? ECGParameters.FromRhythm(rhythmPreset) : monitorParameters.ecg;
    }

    private void ResolvePatientStateIfNeeded()
    {
      if (patientState != null)
      {
        return;
      }

      patientState = GetComponentInParent<PatientStateABC>();
    }

    private void PullParametersFromPatientState()
    {
      if (patientState?.Descriptor == null)
      {
        return;
      }

      monitorParameters = patientState.Descriptor.monitorParameters;
      if (displayMode == ECGDisplayMode.Custom)
      {
        _targetParameters = monitorParameters.ecg;
      }
    }

    private void PushParametersToPatientState()
    {
      if (patientState?.Descriptor == null)
      {
        return;
      }

      patientState.Descriptor.monitorParameters = monitorParameters;
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

    public void SetARTParameters(ARTParameters parameters)
    {
      monitorParameters.art = parameters;
      PushParametersToPatientState();
    }

    public void SetCVPParameters(CVPParameters parameters)
    {
      monitorParameters.cvp = parameters;
      PushParametersToPatientState();
    }

    public void SetPlethParameters(PlethParameters parameters)
    {
      monitorParameters.pleth = parameters;
      PushParametersToPatientState();
    }

    public void SetNumericsParameters(NumericsParameters parameters)
    {
      monitorParameters.numerics = parameters;
      PushParametersToPatientState();
    }

    public void SetNIBPParameters(NIBPParameters parameters)
    {
      monitorParameters.nibp = parameters;
      PushParametersToPatientState();
    }

    public void SetTemperatureParameters(TemperatureParameters parameters)
    {
      monitorParameters.temperature = parameters;
      PushParametersToPatientState();
    }

    public void SetSTLeadValues(STLeadValues values)
    {
      monitorParameters.stLeads = values;
      PushParametersToPatientState();
    }

    private void UpdateLabels()
    {
      var numerics = monitorParameters.numerics;
      float bpmValue = numerics.bpm > 0f ? numerics.bpm : _currentParameters.bpm;
      float prValue = numerics.pulseRate > 0f ? numerics.pulseRate : monitorParameters.pleth.bpm;
      float spo2Value = numerics.spo2 > 0f ? numerics.spo2 : monitorParameters.pleth.spo2;
      float piValue = numerics.perfusionIndex > 0f ? numerics.perfusionIndex : 3.0f;

      if (ecgValueLabel != null)
      {
        ecgValueLabel.text = $"HR {Mathf.RoundToInt(_currentParameters.bpm)}";
      }

      if (plethValueLabel != null)
      {
        plethValueLabel.text = $"SpO2 {Mathf.RoundToInt(monitorParameters.pleth.spo2)}%";
      }

      if (artValueLabel != null)
      {
        int map = Mathf.RoundToInt(monitorParameters.art.diastolic + (monitorParameters.art.systolic - monitorParameters.art.diastolic) / 3f);
        artValueLabel.text = $"{Mathf.RoundToInt(monitorParameters.art.systolic)}/{Mathf.RoundToInt(monitorParameters.art.diastolic)} ({map})";
      }

      if (cvpValueLabel != null)
      {
        cvpValueLabel.text = $"{monitorParameters.cvp.mean:0.0} mmHg";
      }

      if (bpmNumericLabel != null)
      {
        bpmNumericLabel.text = $"{Mathf.RoundToInt(bpmValue)}";
      }

      if (pvcsNumericLabel != null)
      {
        pvcsNumericLabel.text = $"{Mathf.RoundToInt(numerics.pvcs)}";
      }

      if (stNumericLabel != null)
      {
        var st = monitorParameters.stLeads;
        stNumericLabel.text =
          $"I {st.i:+0.0;-0.0;0.0} II {st.ii:+0.0;-0.0;0.0} III {st.iii:+0.0;-0.0;0.0}\n" +
          $"aVR {st.avr:+0.0;-0.0;0.0} aVL {st.avl:+0.0;-0.0;0.0} aVF {st.avf:+0.0;-0.0;0.0}\n" +
          $"V1 {st.v1:+0.0;-0.0;0.0} V2 {st.v2:+0.0;-0.0;0.0} V3 {st.v3:+0.0;-0.0;0.0}\n" +
          $"V4 {st.v4:+0.0;-0.0;0.0} V5 {st.v5:+0.0;-0.0;0.0} V6 {st.v6:+0.0;-0.0;0.0}";
      }

      if (prNumericLabel != null)
      {
        prNumericLabel.text = $"{Mathf.RoundToInt(prValue)}";
      }

      if (piNumericLabel != null)
      {
        piNumericLabel.text = $"{piValue:0.00}";
      }

      if (spo2NumericLabel != null)
      {
        spo2NumericLabel.text = $"{Mathf.RoundToInt(spo2Value)}%";
      }

      if (artNumericLabel != null)
      {
        int map = Mathf.RoundToInt(monitorParameters.art.diastolic + (monitorParameters.art.systolic - monitorParameters.art.diastolic) / 3f);
        artNumericLabel.text = $"{Mathf.RoundToInt(monitorParameters.art.systolic)}/{Mathf.RoundToInt(monitorParameters.art.diastolic)} ({map}) mmHg";
      }

      if (cvpNumericLabel != null)
      {
        cvpNumericLabel.text = $"{monitorParameters.cvp.mean:0.0} mmHg";
      }

      if (nibpNumericLabel != null)
      {
        int map = Mathf.RoundToInt(monitorParameters.nibp.diastolic + (monitorParameters.nibp.systolic - monitorParameters.nibp.diastolic) / 3f);
        nibpNumericLabel.text = $"{Mathf.RoundToInt(monitorParameters.nibp.systolic)}/{Mathf.RoundToInt(monitorParameters.nibp.diastolic)} ({map}) mmHg";
      }

      if (t1NumericLabel != null)
      {
        t1NumericLabel.text = $"{monitorParameters.temperature.t1:0.0}°C";
      }

      if (t2NumericLabel != null)
      {
        t2NumericLabel.text = $"{monitorParameters.temperature.t2:0.0}°C";
      }

      if (deltaTNumericLabel != null)
      {
        float deltaT = Mathf.Abs(monitorParameters.temperature.t1 - monitorParameters.temperature.t2);
        deltaTNumericLabel.text = $"{deltaT:0.0}°C";
      }
    }
  }
}
