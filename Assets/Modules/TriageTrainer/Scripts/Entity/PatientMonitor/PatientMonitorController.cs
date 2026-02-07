using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  [RequireComponent(typeof(UIDocument))]
  public class PatientMonitorController : MonoBehaviour
  {
    [Header("ECG Settings")]
    public ECGParameters parameters = ECGParameters.Normal;

    [Header("Graph Appearance")]
    public Color graphColor = Color.green;
    public float lineThickness = 2.5f;
    [Range(10, 1000)] public int resolution = 200; // 그래프 해상도

    private UIDocument uiDocument;
    private ECGGraphElement graphElement;

    // 시뮬레이션 변수들
    private float lastBeatTime = 0f;
    private float nextBeatInterval = 1f;
    private float currentTime = 0f;

    void OnEnable()
    {
      uiDocument = GetComponent<UIDocument>();
      CreateGraphUI();
    }

    void CreateGraphUI()
    {
      var root = uiDocument.rootVisualElement;
      root.Clear();

      // 배경 컨테이너 (모니터 느낌)
      var container = new VisualElement();
      container.style.flexGrow = 1;
      container.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.05f)); // 검은 배경
      container.style.justifyContent = Justify.Center;
      container.style.paddingBottom = 10;
      container.style.paddingTop = 10;
      container.style.paddingLeft = 10;
      container.style.paddingRight = 10;

      // 그래프 엘리먼트 생성 및 추가
      graphElement = new ECGGraphElement();
      graphElement.style.flexGrow = 1;
      graphElement.SetColor(graphColor);
      graphElement.SetLineWidth(lineThickness);

      // 모눈종이 효과 (선택 사항)
      var grid = new VisualElement();
      grid.style.position = Position.Absolute;
      grid.style.left = 0; grid.style.top = 0; grid.style.right = 0; grid.style.bottom = 0;
      grid.style.opacity = 0.2f;
      // 실제 텍스처가 없다면 단순 색상으로 처리하거나 셰이더 사용 권장. 여기서는 생략.

      container.Add(grid);
      container.Add(graphElement);
      root.Add(container);
    }

    void Update()
    {
      if (graphElement == null) return;

      // 1. 시간 흐름 계산
      currentTime += Time.deltaTime;

      // 2. 비트 타이밍 계산 (심박수 및 부정맥 적용)
      float baseInterval = parameters.bpm > 0 ? 60f / parameters.bpm : float.MaxValue;

      // 다음 비트가 발생해야 할 시간이 지났는지 확인
      if (currentTime - lastBeatTime >= nextBeatInterval)
      {
        lastBeatTime += nextBeatInterval;

        // 다음 간격 계산 (부정맥 적용)
        nextBeatInterval = baseInterval;
        if (parameters.irregularity > 0)
        {
          float variance = (Random.value - 0.5f) * 2 * parameters.irregularity * baseInterval * 0.5f;
          nextBeatInterval += variance;
        }
      }

      // 3. 현재 전압 계산
      float voltage = CalculateVoltage(currentTime, lastBeatTime, nextBeatInterval);

      // 4. 그래프 업데이트
      graphElement.AddValue(voltage);
    }

    // 수학적 모델 (React 코드의 Gaussian 로직 포팅)
    float CalculateVoltage(float t, float beatTime, float interval)
    {
      float dt = t - beatTime; // 현재 비트로부터의 경과 시간
      float val = 0f;

      // 심장 무수축 (Asystole)
      if (parameters.bpm <= 0)
      {
        return (Random.value - 0.5f) * parameters.noise;
      }

      float qrsMult = parameters.qrsWidthScale > 0 ? parameters.qrsWidthScale : 1.0f;

      // P Wave (심방 탈분극) - R파(0.0) 기준 약 -0.16초
      // 여기서는 beatTime을 P파 시작점이 아닌 R파 기준으로 잡는 것이 계산이 편하므로
      // 편의상 beatTime + 0.2초를 R파 시점이라고 가정하고 오프셋을 조정합니다.
      // 하지만 실시간 시뮬레이션에서는 '현재 비트'가 시작된 후 순차적으로 파형이 나옵니다.
      // P(0.1s) -> QRS(0.2s) -> T(0.4s) 순서로 배치해 봅니다.

      // P Wave (0.1초 부근)
      val += Gaussian(dt, 0.1f, parameters.pAmp, parameters.pWidth);

      // Q Wave (0.18초 부근)
      val += Gaussian(dt, 0.18f, parameters.qAmp, 0.02f * qrsMult);

      // R Wave (0.2초 - 주 심실 수축)
      val += Gaussian(dt, 0.2f, parameters.rAmp, 0.03f * qrsMult);

      // S Wave (0.22초 부근)
      val += Gaussian(dt, 0.22f, parameters.sAmp, 0.03f * qrsMult);

      // ST Elevation (S파 이후 T파 이전)
      if (parameters.stElevation != 0 && dt > 0.25f && dt < 0.4f)
      {
        float stShape = Mathf.Exp(-Mathf.Pow(dt - 0.3f, 2) / (2 * 0.1f * 0.1f));
        val += parameters.stElevation * stShape;
      }

      // T Wave (0.45초 부근)
      val += Gaussian(dt, 0.45f, parameters.tAmp, parameters.tWidth);

      // U Wave (0.65초 부근)
      if (parameters.uAmp != 0)
      {
        val += Gaussian(dt, 0.65f, parameters.uAmp, 0.06f);
      }

      // 노이즈 추가
      val += (Random.value - 0.5f) * parameters.noise;

      return val;
    }

    float Gaussian(float t, float center, float amp, float width)
    {
      if (width == 0) return 0;
      return amp * Mathf.Exp(-Mathf.Pow(t - center, 2) / (2 * width * width));
    }

    // 인스펙터에서 값 변경 시 실시간 반영을 위해
    private void OnValidate()
    {
      if (graphElement != null)
      {
        graphElement.SetColor(graphColor);
        graphElement.SetLineWidth(lineThickness);
      }
    }
  }
}
