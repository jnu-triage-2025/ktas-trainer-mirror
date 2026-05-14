using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// Play 모드에서 A/B 테스트 포인트를 자동 생성하고 라인 연결 동작을 검증합니다.
  /// </summary>
  public class LineConnectorSandboxBootstrap : MonoBehaviour
  {
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateEndpoints = true;

    [Header("Animation")]
    [SerializeField] private bool animatePointB = true;
    [SerializeField, Min(0.01f)] private float animationSpeed = 1.2f;
    [SerializeField] private Vector3 animationAxis = Vector3.right;
    [SerializeField, Min(0f)] private float animationAmplitude = 0.35f;

    [Header("Initial Positions")]
    [SerializeField] private Vector3 pointAStart = new Vector3(-0.3f, 1.1f, 0f);
    [SerializeField] private Vector3 pointBStart = new Vector3(0.3f, 1.1f, 0f);

    private Transform pointA;
    private Transform pointB;
    private IntravenousLineGrounded connector;
    private Vector3 pointBBasePosition;

    private void Awake()
    {
      connector = GetComponent<IntravenousLineGrounded>();
      if (connector == null)
      {
        connector = gameObject.AddComponent<IntravenousLineGrounded>();
      }

      if (autoCreateEndpoints)
      {
        CreateEndpointsIfNeeded();
      }

      if (pointA != null && pointB != null)
      {
        connector.SetEndpoints(pointA.gameObject, pointB.gameObject);
        pointBBasePosition = pointB.position;
      }
    }

    private void Update()
    {
      if (!animatePointB || pointB == null)
      {
        return;
      }

      Vector3 axis = animationAxis.sqrMagnitude > 0f ? animationAxis.normalized : Vector3.right;
      float offset = Mathf.Sin(Time.time * animationSpeed) * animationAmplitude;
      pointB.position = pointBBasePosition + axis * offset;
    }

    private void CreateEndpointsIfNeeded()
    {
      Transform existingA = transform.Find("PointA");
      Transform existingB = transform.Find("PointB");

      pointA = existingA != null ? existingA : CreateEndpoint("PointA", pointAStart, new Color(0.1f, 0.9f, 0.2f, 1f));
      pointB = existingB != null ? existingB : CreateEndpoint("PointB", pointBStart, new Color(0.9f, 0.2f, 0.2f, 1f));
    }

    private Transform CreateEndpoint(string endpointName, Vector3 localPosition, Color tint)
    {
      GameObject endpoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      endpoint.name = endpointName;
      endpoint.transform.SetParent(transform, false);
      endpoint.transform.localPosition = localPosition;
      endpoint.transform.localScale = Vector3.one * 0.05f;

      Renderer renderer = endpoint.GetComponent<Renderer>();
      if (renderer != null)
      {
        Material material = new Material(Shader.Find("Standard"));
        material.name = "M_Runtime_" + endpointName;
        material.color = tint;
        renderer.sharedMaterial = material;
      }

      return endpoint.transform;
    }

    private void OnGUI()
    {
      GUI.Label(
        new Rect(10f, 10f, 520f, 60f),
        "LineConnector Sandbox\n" +
        "- PointA(초록), PointB(빨강) 사이가 연결됩니다.\n" +
        "- animatePointB를 켜면 PointB가 좌우로 움직이며 선 갱신을 검증합니다.");
    }
  }
}
