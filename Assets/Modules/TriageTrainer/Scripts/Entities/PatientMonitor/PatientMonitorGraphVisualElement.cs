using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  public enum MonitorChannelId
  {
    ECG,
    PLETH,
    ART,
    CVP
  }

  public struct GraphRange
  {
    public float min;
    public float max;

    public GraphRange(float min, float max)
    {
      this.min = min;
      this.max = max;
    }
  }

  [UxmlElement]
  public partial class PatientMonitorGraphElement : VisualElement
  {
    private readonly List<float> dataPoints = new List<float>();
    private readonly Color _gridColor = new Color(1f, 1f, 1f, 0.08f);
    private readonly Dictionary<MonitorChannelId, GraphRange> _defaultRanges = new Dictionary<MonitorChannelId, GraphRange>
    {
      { MonitorChannelId.ECG, new GraphRange(-1.5f, 2.5f) },
      { MonitorChannelId.PLETH, new GraphRange(0f, 1.05f) },
      { MonitorChannelId.ART, new GraphRange(-5f, 165f) },
      { MonitorChannelId.CVP, new GraphRange(-1f, 31f) }
    };

    private int maxPoints = 300; // 화면에 보여질 포인트 수
    private float lineWidth = 2.0f;
    private Color lineColor = Color.green;
    private string _channelName = "ECG";
    private MonitorChannelId _channelId = MonitorChannelId.ECG;
    private GraphRange _range = new GraphRange(-1.5f, 2.5f);

    public PatientMonitorGraphElement()
    {
      // 매 프레임 다시 그리기 위해 설정
      generateVisualContent += OnGenerateVisualContent;
    }

    [UxmlAttribute("max-points")]
    public int MaxPoints
    {
      get => maxPoints;
      set => maxPoints = Mathf.Max(2, value);
    }

    [UxmlAttribute("line-width")]
    public float LineWidth
    {
      get => lineWidth;
      set => lineWidth = Mathf.Max(0f, value);
    }

    [UxmlAttribute("line-color")]
    public Color LineColor
    {
      get => lineColor;
      set => lineColor = value;
    }

    public void AddValue(float value)
    {
      dataPoints.Add(value);
      if (dataPoints.Count > maxPoints)
      {
        dataPoints.RemoveAt(0);
      }
      MarkDirtyRepaint(); // 다시 그리기 요청
    }

    public void SetColor(Color color) => lineColor = color;
    public void SetLineWidth(float width) => lineWidth = width;

    public void SetChannel(MonitorChannelId channelId, string channelName)
    {
      _channelId = channelId;
      _channelName = channelName;
      if (_defaultRanges.TryGetValue(channelId, out var range))
      {
        _range = range;
      }
      MarkDirtyRepaint();
    }

    public void SetRange(float min, float max)
    {
      if (Mathf.Approximately(min, max))
      {
        max = min + 1f;
      }

      _range = new GraphRange(Mathf.Min(min, max), Mathf.Max(min, max));
      MarkDirtyRepaint();
    }

    // 실제 드로잉 로직 (MeshGenerationContext 사용)
    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
      var painter = mgc.painter2D;

      float width = contentRect.width;
      float height = contentRect.height;
      if (width <= 0f || height <= 0f)
        return;

      DrawGrid(painter, width, height);
      if (dataPoints.Count < 2)
        return;

      painter.lineWidth = lineWidth;
      painter.strokeColor = lineColor;
      painter.BeginPath();

      float stepX = width / (maxPoints - 1);
      float rangeSpan = Mathf.Max(0.0001f, _range.max - _range.min);

      float MapY(float voltage)
      {
        float normalized = (voltage - _range.min) / rangeSpan;
        return height - (normalized * height);
      }

      painter.MoveTo(new Vector2(0, MapY(dataPoints[0])));

      for (int i = 1; i < dataPoints.Count; i++)
      {
        float x = i * stepX;
        float y = MapY(dataPoints[i]);
        painter.LineTo(new Vector2(x, y));
      }

      painter.Stroke();
    }

    private void DrawGrid(Painter2D painter, float width, float height)
    {
      painter.lineWidth = 1f;
      painter.strokeColor = _gridColor;

      const int horizontalDivisions = 3;
      for (int i = 0; i <= horizontalDivisions; i++)
      {
        float y = i * (height / horizontalDivisions);
        painter.BeginPath();
        painter.MoveTo(new Vector2(0f, y));
        painter.LineTo(new Vector2(width, y));
        painter.Stroke();
      }

      const float verticalSpacing = 60f;
      for (float x = 0f; x <= width; x += verticalSpacing)
      {
        painter.BeginPath();
        painter.MoveTo(new Vector2(x, 0f));
        painter.LineTo(new Vector2(x, height));
        painter.Stroke();
      }
    }

  }
}
