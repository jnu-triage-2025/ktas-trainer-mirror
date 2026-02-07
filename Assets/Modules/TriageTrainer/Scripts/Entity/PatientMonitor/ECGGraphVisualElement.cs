using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  [UxmlElement]
  public partial class ECGGraphElement : VisualElement
  {
    private List<float> dataPoints = new List<float>();
    private int maxPoints = 300; // 화면에 보여질 포인트 수
    private float lineWidth = 2.0f;
    private Color lineColor = Color.green;

    public ECGGraphElement()
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

    // 실제 드로잉 로직 (MeshGenerationContext 사용)
    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
      if (dataPoints.Count < 2) return;

      var painter = mgc.painter2D;
      painter.lineWidth = lineWidth;
      painter.strokeColor = lineColor;
      painter.BeginPath();

      float width = contentRect.width;
      float height = contentRect.height;
      float stepX = width / (maxPoints - 1);

      // Y축 스케일링 (전압 -1.5 ~ 2.5 범위를 높이에 매핑)
      float MapY(float voltage)
      {
        // 전압 범위: -1.5mV ~ 2.5mV (총 4.0)
        // 화면 좌표계는 위가 0, 아래가 height이므로 반전 필요
        float normalized = (voltage - (-1.5f)) / 4.0f;
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
  }
}
