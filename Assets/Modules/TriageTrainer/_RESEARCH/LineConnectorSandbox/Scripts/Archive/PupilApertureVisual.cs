using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// 홍채 링(가운데가 뚫린 메쉬)을 생성하고 동공 구멍 직경을 갱신합니다.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilApertureVisual : MonoBehaviour
  {
    [SerializeField] private MeshFilter targetMeshFilter;
    [SerializeField, Range(12, 128)] private int ringSegments = 64;
    [SerializeField, Min(0.001f)] private float irisOuterDiameterMeters = 0.011f;
    [SerializeField, Min(0.0001f)] private float irisThicknessMeters = 0.0009f;
    [SerializeField, Min(0.0005f)] private float pupilDiameterMeters = 0.005f;

    private Mesh runtimeRingMesh;

    private int lastSegments = -1;
    private float lastOuterDiameter = -1f;
    private float lastThickness = -1f;
    private float lastPupilDiameter = -1f;

    private void OnEnable()
    {
      EnsureTargetMeshFilter();
      RebuildIfNeeded(true);
    }

    private void OnValidate()
    {
      ringSegments = Mathf.Clamp(ringSegments, 12, 128);
      irisOuterDiameterMeters = Mathf.Max(0.001f, irisOuterDiameterMeters);
      irisThicknessMeters = Mathf.Max(0.0001f, irisThicknessMeters);
      pupilDiameterMeters = Mathf.Max(0.0005f, pupilDiameterMeters);

      EnsureTargetMeshFilter();
      RebuildIfNeeded(true);
    }

    private void OnDestroy()
    {
      if (runtimeRingMesh == null)
      {
        return;
      }

      if (Application.isPlaying)
      {
        Destroy(runtimeRingMesh);
      }
      else
      {
        DestroyImmediate(runtimeRingMesh);
      }

      runtimeRingMesh = null;
    }

    public void Configure(
      MeshFilter meshFilter,
      float outerDiameterMeters,
      float thicknessMeters,
      int segments)
    {
      targetMeshFilter = meshFilter;
      irisOuterDiameterMeters = Mathf.Max(0.001f, outerDiameterMeters);
      irisThicknessMeters = Mathf.Max(0.0001f, thicknessMeters);
      ringSegments = Mathf.Clamp(segments, 12, 128);

      RebuildIfNeeded(true);
    }

    public void SetPupilDiameter(float diameterMeters)
    {
      pupilDiameterMeters = Mathf.Max(0.0005f, diameterMeters);
      RebuildIfNeeded(false);
    }

    private void EnsureTargetMeshFilter()
    {
      if (targetMeshFilter != null)
      {
        return;
      }

      targetMeshFilter = GetComponent<MeshFilter>();
      if (targetMeshFilter == null)
      {
        targetMeshFilter = gameObject.AddComponent<MeshFilter>();
      }
    }

    private void RebuildIfNeeded(bool force)
    {
      EnsureTargetMeshFilter();
      if (targetMeshFilter == null)
      {
        return;
      }

      bool changed =
        lastSegments != ringSegments ||
        !Mathf.Approximately(lastOuterDiameter, irisOuterDiameterMeters) ||
        !Mathf.Approximately(lastThickness, irisThicknessMeters) ||
        !Mathf.Approximately(lastPupilDiameter, pupilDiameterMeters);

      if (!force && !changed)
      {
        return;
      }

      BuildRingMesh();

      lastSegments = ringSegments;
      lastOuterDiameter = irisOuterDiameterMeters;
      lastThickness = irisThicknessMeters;
      lastPupilDiameter = pupilDiameterMeters;
    }

    private void BuildRingMesh()
    {
      if (runtimeRingMesh == null)
      {
        runtimeRingMesh = new Mesh
        {
          name = "Msh_Runtime_IrisAperture"
        };
      }
      else
      {
        runtimeRingMesh.Clear();
      }

      int segments = Mathf.Clamp(ringSegments, 12, 128);
      float outerRadius = Mathf.Max(0.0005f, irisOuterDiameterMeters * 0.5f);
      float maxInnerRadius = outerRadius * 0.92f;
      float innerRadius = Mathf.Clamp(pupilDiameterMeters * 0.5f, 0.0001f, maxInnerRadius);
      float halfThickness = Mathf.Max(0.00005f, irisThicknessMeters * 0.5f);

      int vertexCount = (segments + 1) * 4;
      Vector3[] vertices = new Vector3[vertexCount];
      Vector2[] uvs = new Vector2[vertexCount];
      int[] triangles = new int[segments * 24];

      for (int i = 0; i <= segments; i++)
      {
        float normalized = i / (float)segments;
        float radians = normalized * Mathf.PI * 2f;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        Vector3 outer = new Vector3(cos * outerRadius, sin * outerRadius, 0f);
        Vector3 inner = new Vector3(cos * innerRadius, sin * innerRadius, 0f);

        int baseIndex = i * 4;
        vertices[baseIndex + 0] = new Vector3(outer.x, outer.y, halfThickness);
        vertices[baseIndex + 1] = new Vector3(inner.x, inner.y, halfThickness);
        vertices[baseIndex + 2] = new Vector3(outer.x, outer.y, -halfThickness);
        vertices[baseIndex + 3] = new Vector3(inner.x, inner.y, -halfThickness);

        Vector2 uvOuter = new Vector2((cos + 1f) * 0.5f, (sin + 1f) * 0.5f);
        Vector2 uvInner = new Vector2(
          (cos * (innerRadius / outerRadius) + 1f) * 0.5f,
          (sin * (innerRadius / outerRadius) + 1f) * 0.5f);

        uvs[baseIndex + 0] = uvOuter;
        uvs[baseIndex + 1] = uvInner;
        uvs[baseIndex + 2] = uvOuter;
        uvs[baseIndex + 3] = uvInner;
      }

      int triangleIndex = 0;
      for (int i = 0; i < segments; i++)
      {
        int current = i * 4;
        int next = (i + 1) * 4;

        // Front face
        triangles[triangleIndex++] = current + 0;
        triangles[triangleIndex++] = next + 0;
        triangles[triangleIndex++] = current + 1;

        triangles[triangleIndex++] = current + 1;
        triangles[triangleIndex++] = next + 0;
        triangles[triangleIndex++] = next + 1;

        // Back face
        triangles[triangleIndex++] = current + 2;
        triangles[triangleIndex++] = current + 3;
        triangles[triangleIndex++] = next + 2;

        triangles[triangleIndex++] = current + 3;
        triangles[triangleIndex++] = next + 3;
        triangles[triangleIndex++] = next + 2;

        // Outer wall
        triangles[triangleIndex++] = current + 0;
        triangles[triangleIndex++] = current + 2;
        triangles[triangleIndex++] = next + 0;

        triangles[triangleIndex++] = next + 0;
        triangles[triangleIndex++] = current + 2;
        triangles[triangleIndex++] = next + 2;

        // Inner wall
        triangles[triangleIndex++] = current + 1;
        triangles[triangleIndex++] = next + 1;
        triangles[triangleIndex++] = current + 3;

        triangles[triangleIndex++] = next + 1;
        triangles[triangleIndex++] = next + 3;
        triangles[triangleIndex++] = current + 3;
      }

      runtimeRingMesh.vertices = vertices;
      runtimeRingMesh.uv = uvs;
      runtimeRingMesh.triangles = triangles;
      runtimeRingMesh.RecalculateNormals();
      runtimeRingMesh.RecalculateBounds();

      targetMeshFilter.sharedMesh = runtimeRingMesh;
    }
  }
}
