using UnityEngine;
using UnityEngine.UIElements;
using MultiplayerInfrastructure.UI;

namespace TriageTrainer.Entity.PatientMonitor
{
  /// <summary>
  /// 모니터 콘텐츠를 출력 대상(월드 UIDocument 또는 UI UIDocument)과 분리하는 표시 뷰입니다.
  /// </summary>
  public sealed class PatientMonitorDisplayView
  {
    private const string GeneratedRootName = "PatientMonitorGeneratedContent";
    private readonly PatientMonitorPlaneType _type;
    private PatientMonitorGraphElement[] _graphs;
    private Label[] _graphValues;
    private Label[] _metricValues;
    private VisualElement _generatedRoot;
    private VisualElement _contentRoot;

    public PatientMonitorDisplayView(PatientMonitorPlaneType type)
    {
      _type = type;
    }

    public void Build(UIDocument document, Color[] colors, float lineThickness, int points)
    {
      if (document == null || document.rootVisualElement == null)
        return;

      VisualElement root = document.rootVisualElement;
      root.Q<VisualElement>(GeneratedRootName)?.RemoveFromHierarchy();

      _generatedRoot = new VisualElement { name = GeneratedRootName };
      _generatedRoot.style.flexGrow = 1f;
      _generatedRoot.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.05f));
      _generatedRoot.style.paddingLeft = 8f;
      _generatedRoot.style.paddingRight = 8f;
      _generatedRoot.style.paddingTop = 8f;
      _generatedRoot.style.paddingBottom = 8f;
      _generatedRoot.style.overflow = Overflow.Hidden;
      root.Add(_generatedRoot);

      // 이 모니터는 월드 표시 전용이며 화면 UI 입력 표면이 아니다.
      // 동적 UI를 추가할 때는 콘텐츠뿐 아니라 생성 컨테이너까지 전체 서브트리를
      // 반드시 Ignore해야 한다. 자식만 Ignore하면 컨테이너가 UI 히트 대상이 된다.
      SetGeneratedNonInteractive(_generatedRoot);

      _contentRoot = new VisualElement { name = "PatientMonitorContent" };
      _contentRoot.style.flexGrow = 1f;
      _contentRoot.style.overflow = Overflow.Hidden;
      _generatedRoot.Add(_contentRoot);

      _graphs = new PatientMonitorGraphElement[4];
      _graphValues = new Label[4];
      _metricValues = new Label[12];

      if (_type == PatientMonitorPlaneType.Metrics)
      {
        BuildMetrics(_contentRoot);
        SetGeneratedNonInteractive(_contentRoot);
        return;
      }

      var column = new VisualElement { name = "PatientMonitorGraphContent" };
      column.style.flexGrow = 1f;
      column.style.flexDirection = FlexDirection.Column;
      string[] names = { "ECG", "PLETH", "ART", "CVP" };
      MonitorChannelId[] channels = { MonitorChannelId.ECG, MonitorChannelId.PLETH, MonitorChannelId.ART, MonitorChannelId.CVP };
      float[] mins = { -1.5f, 0f, -5f, -1f };
      float[] maxs = { 2.5f, 1.05f, 165f, 31f };

      for (int i = 0; i < names.Length; i++)
      {
        var row = new VisualElement();
        row.style.flexGrow = 1f;
        row.style.flexBasis = 0f;
        row.style.minHeight = 48f;
        row.style.marginBottom = 3f;
        row.style.position = Position.Relative;
        row.style.overflow = Overflow.Hidden;

        var graph = new PatientMonitorGraphElement();
        graph.style.flexGrow = 1f;
        graph.style.marginTop = 14f;
        graph.SetColor(colors[i]);
        graph.SetLineWidth(lineThickness);
        graph.MaxPoints = points;
        graph.SetChannel(channels[i], names[i]);
        graph.SetRange(mins[i], maxs[i]);

        var name = new Label(names[i]);
        name.style.position = Position.Absolute;
        name.style.left = 4f;
        name.style.top = 2f;
        name.style.fontSize = 10f;
        name.style.color = new StyleColor(colors[i]);
        name.style.unityFontStyleAndWeight = FontStyle.Bold;

        var value = new Label();
        value.style.position = Position.Absolute;
        value.style.right = 4f;
        value.style.top = 2f;
        value.style.fontSize = 10f;
        value.style.color = new StyleColor(colors[i]);
        value.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(graph);
        row.Add(name);
        row.Add(value);
        column.Add(row);
        _graphs[i] = graph;
        _graphValues[i] = value;
      }

      _contentRoot.Add(column);
      SetGeneratedNonInteractive(_contentRoot);
    }
    public VisualElement ContentRoot => _contentRoot;

    public void RestoreContentToMonitor()
    {
      if (_generatedRoot == null || _contentRoot == null)
        return;

      _generatedRoot.Add(_contentRoot);
      _contentRoot.style.paddingLeft = StyleKeyword.Null;
      _contentRoot.style.paddingRight = StyleKeyword.Null;
      _contentRoot.style.marginLeft = StyleKeyword.Null;
    }

    private static void SetGeneratedNonInteractive(VisualElement root)
    {
      UIDocumentInteractionPolicy.SetSubtreePickingMode(root, PickingMode.Ignore);
      SetSubtreeNonFocusable(root);
    }

    private static void SetSubtreeNonFocusable(VisualElement root)
    {
      if (root == null)
        return;

      root.focusable = false;
      for (int i = 0; i < root.childCount; i++)
        SetSubtreeNonFocusable(root[i]);
    }

    private void BuildMetrics(VisualElement root)
    {
      var column = new VisualElement { name = "PatientMonitorMetricsContent" };
      column.style.flexGrow = 1f;
      column.style.flexDirection = FlexDirection.Column;
      column.style.justifyContent = Justify.FlexStart;
      string[] titles = { "BPM", "PVCs", "ST", "PR", "PI", "%SpO2", "ART", "CVP", "NIBP", "ΔT", "T1", "T2" };
      for (int i = 0; i < titles.Length; i++)
      {
        var block = new VisualElement();
        block.style.flexGrow = i == 2 ? 2f : 1f;
        block.style.flexBasis = 0f;
        block.style.minHeight = i == 2 ? 92f : 38f;
        block.style.marginBottom = 3f;
        block.style.borderBottomWidth = 1f;
        block.style.borderBottomColor = new StyleColor(new Color(1f, 1f, 1f, 0.08f));
        var title = new Label(titles[i]);
        title.style.fontSize = 10f;
        title.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.75f));
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        var value = new Label("-");
        value.style.flexGrow = 1f;
        value.style.fontSize = i == 2 ? 9f : 18f;
        value.style.color = Color.white;
        value.style.unityTextAlign = TextAnchor.MiddleRight;
        value.style.whiteSpace = WhiteSpace.Normal;
        block.Add(title);
        block.Add(value);
        column.Add(block);
        _metricValues[i] = value;
      }
      root.Add(column);
    }

    public void AddSamples(float ecg, float pleth, float art, float cvp)
    {
      if (_graphs == null || _type != PatientMonitorPlaneType.Graph)
        return;
      _graphs[0]?.AddValue(ecg);
      _graphs[1]?.AddValue(pleth);
      _graphs[2]?.AddValue(art);
      _graphs[3]?.AddValue(cvp);
    }

    public void SetGraphValues(string ecg, string pleth, string art, string cvp)
    {
      if (_graphValues == null) return;
      SetLabelText(_graphValues[0], ecg);
      SetLabelText(_graphValues[1], pleth);
      SetLabelText(_graphValues[2], art);
      SetLabelText(_graphValues[3], cvp);
    }

    public void SetMetricValues(params string[] values)
    {
      if (_metricValues == null) return;
      for (int i = 0; i < _metricValues.Length && i < values.Length; i++)
        if (_metricValues[i] != null && _metricValues[i].text != values[i])
          _metricValues[i].text = values[i];
    }

    private static void SetLabelText(Label label, string value)
    {
      if (label != null && label.text != value)
        label.text = value;
    }
  }
}
