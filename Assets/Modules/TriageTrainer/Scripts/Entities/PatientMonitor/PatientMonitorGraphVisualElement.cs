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
    private const float RepaintIntervalSeconds = 1f / 20f;
    private const int MinimumRenderedPointCount = 64;
    private const int MaximumRenderedPointCount = 512;

    private float[] _dataPoints = new float[300];
    private int _dataPointCount;
    private int _nextDataPointIndex;
    private float _nextRepaintTime;
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
      set
      {
        int newMaxPoints = Mathf.Max(2, value);
        if (newMaxPoints == maxPoints && _dataPoints.Length == newMaxPoints)
          return;

        ResizeDataBuffer(newMaxPoints);
        maxPoints = newMaxPoints;
      }
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
      _dataPoints[_nextDataPointIndex] = value;
      _nextDataPointIndex = (_nextDataPointIndex + 1) % _dataPoints.Length;
      _dataPointCount = Mathf.Min(_dataPointCount + 1, _dataPoints.Length);

      // 생체 신호 계산은 원래 sample rate로 유지하되 UI mesh 재생성은 20Hz로 제한한다.
      // 여러 샘플이 같은 프레임에 생성돼도 그래프는 한 번만 무효화된다.
      float now = Time.unscaledTime;
      if (now < _nextRepaintTime)
        return;

      _nextRepaintTime = now + RepaintIntervalSeconds;
      MarkDirtyRepaint();
    }

    /// <summary>
    /// 저장된 샘플만 제거합니다. 다음 샘플이 표시 폭 전체로 확대되어 보일 수 있으므로,
    /// 연속적인 모니터 스크롤 표현에는 <see cref="ClearAndFillHistory"/>를 사용해야 합니다.
    /// </summary>
    public void ClearHistory()
    {
      _dataPointCount = 0;
      _nextDataPointIndex = 0;
      _nextRepaintTime = 0f;
      MarkDirtyRepaint();
    }

    /// <summary>
    /// 먼저 저장된 샘플을 제거한 뒤, 표시 폭 전체를 지정 값으로 채웁니다. 새 파형은
    /// 오른쪽에서 시작해 기준선을 밀어내므로, 그래프가 가로로 확대되는 연출이 발생하지 않습니다.
    /// </summary>
    public void ClearAndFillHistory(float value)
    {
      ClearHistory();
      for (int i = 0; i < _dataPoints.Length; i++)
        _dataPoints[i] = value;

      _dataPointCount = _dataPoints.Length;
      _nextDataPointIndex = 0;
      MarkDirtyRepaint();
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
      if (_dataPointCount < 2)
        return;

      painter.lineWidth = lineWidth;
      painter.strokeColor = lineColor;
      painter.BeginPath();

      int targetRenderedPointCount = Mathf.Clamp(Mathf.CeilToInt(width), MinimumRenderedPointCount, MaximumRenderedPointCount);
      float rangeSpan = Mathf.Max(0.0001f, _range.max - _range.min);

      float MapY(float voltage)
      {
        float normalized = (voltage - _range.min) / rangeSpan;
        return height - (normalized * height);
      }

      float MapX(int sampleIndex) => width * sampleIndex / Mathf.Max(1, _dataPointCount - 1);

      painter.MoveTo(new Vector2(0, MapY(GetDataPoint(0))));

      // 각 시간 구간의 min/max를 시간 순서대로 보존한다. 단순 stride 샘플링은
      // 짧은 ECG peak/PVC를 완전히 누락할 수 있으므로 의료 파형에는 부적합하다.
      int interiorCount = Mathf.Max(0, _dataPointCount - 2);
      int binCount = Mathf.Min(interiorCount, Mathf.Max(1, (targetRenderedPointCount - 2) / 2));
      int lastDrawnIndex = 0;
      for (int bin = 0; bin < binCount; bin++)
      {
        int start = 1 + (bin * interiorCount / binCount);
        int end = 1 + ((bin + 1) * interiorCount / binCount);
        if (end <= start)
          continue;

        int minIndex = start;
        int maxIndex = start;
        float minValue = GetDataPoint(start);
        float maxValue = minValue;
        for (int i = start + 1; i < end; i++)
        {
          float value = GetDataPoint(i);
          if (value < minValue) { minValue = value; minIndex = i; }
          if (value > maxValue) { maxValue = value; maxIndex = i; }
        }

        int firstIndex = Mathf.Min(minIndex, maxIndex);
        int secondIndex = Mathf.Max(minIndex, maxIndex);
        if (firstIndex > lastDrawnIndex)
        {
          painter.LineTo(new Vector2(MapX(firstIndex), MapY(GetDataPoint(firstIndex))));
          lastDrawnIndex = firstIndex;
        }
        if (secondIndex > lastDrawnIndex)
        {
          painter.LineTo(new Vector2(MapX(secondIndex), MapY(GetDataPoint(secondIndex))));
          lastDrawnIndex = secondIndex;
        }
      }

      int latestIndex = _dataPointCount - 1;
      if (latestIndex > lastDrawnIndex)
        painter.LineTo(new Vector2(width, MapY(GetDataPoint(latestIndex))));

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

    private float GetDataPoint(int chronologicalIndex)
    {
      int oldestIndex = _dataPointCount == _dataPoints.Length ? _nextDataPointIndex : 0;
      return _dataPoints[(oldestIndex + chronologicalIndex) % _dataPoints.Length];
    }

    private void ResizeDataBuffer(int newSize)
    {
      var resized = new float[newSize];
      int copyCount = Mathf.Min(_dataPointCount, newSize);
      int sourceStart = _dataPointCount - copyCount;
      for (int i = 0; i < copyCount; i++)
        resized[i] = GetDataPoint(sourceStart + i);

      _dataPoints = resized;
      _dataPointCount = copyCount;
      _nextDataPointIndex = copyCount % newSize;
    }

  }
}
