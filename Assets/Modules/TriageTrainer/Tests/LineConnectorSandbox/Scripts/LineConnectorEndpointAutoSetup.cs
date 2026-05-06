using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  [ExecuteAlways]
  [RequireComponent(typeof(IntravenousLineGrounded))]
  public class LineConnectorEndpointAutoSetup : MonoBehaviour
  {
    [Header("Endpoint Names")]
    [SerializeField] private string pointAName = "PointA";
    [SerializeField] private string pointBName = "PointB";

    [Header("Local Positions")]
    [SerializeField] private Vector3 pointALocalPosition = new Vector3(-0.05f, 0f, 0f);
    [SerializeField] private Vector3 pointBLocalPosition = new Vector3(0.05f, 0f, 0f);

    [Header("Auto Apply")]
    [SerializeField] private bool applyInAwake = true;
    [SerializeField] private bool createIfMissing = true;

    private IntravenousLineGrounded connector;
    private Transform pointA;
    private Transform pointB;

    private void Awake()
    {
      if (!applyInAwake)
      {
        return;
      }

      SetupAndBind();
    }

    private void OnValidate()
    {
      if (!isActiveAndEnabled)
      {
        return;
      }

      SetupAndBind();
    }

    [ContextMenu("Setup And Bind Endpoints")]
    public void SetupAndBind()
    {
      connector = GetComponent<IntravenousLineGrounded>();
      if (connector == null)
      {
        return;
      }

      pointA = transform.Find(pointAName);
      pointB = transform.Find(pointBName);

      if (createIfMissing)
      {
        if (pointA == null)
        {
          pointA = CreateEndpoint(pointAName, pointALocalPosition);
        }

        if (pointB == null)
        {
          pointB = CreateEndpoint(pointBName, pointBLocalPosition);
        }
      }

      if (pointA != null)
      {
        pointA.localPosition = pointALocalPosition;
      }

      if (pointB != null)
      {
        pointB.localPosition = pointBLocalPosition;
      }

      connector.SetEndpoints(
        pointA != null ? pointA.gameObject : null,
        pointB != null ? pointB.gameObject : null);
    }

    private Transform CreateEndpoint(string endpointName, Vector3 localPosition)
    {
      GameObject endpoint = new GameObject(endpointName);
      endpoint.transform.SetParent(transform, false);
      endpoint.transform.localPosition = localPosition;
      endpoint.transform.localRotation = Quaternion.identity;
      endpoint.transform.localScale = Vector3.one;
      return endpoint.transform;
    }
  }
}
