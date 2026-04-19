using System;

using UnityEngine;
using UnityEngine.Rendering;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  [ExecuteAlways]
  [DisallowMultipleComponent]
  [RequireComponent(typeof(LineRenderer))]
  public class IntravenousLineGrounded : MonoBehaviour
  {
    private enum ConnectorRenderMode
    {
      TubeMesh = 0,
      LineRenderer = 1,
    }

    private enum ConnectorPreset
    {
      Custom = 0,
      IvLineLight = 1,
      OxygenLine = 2,
      SuctionLine = 3,
      DefibCable = 4,
    }

    [Header("Endpoints")]
    [SerializeField] private IntravenousLineGroundedEndpoint endpoints;

    [Header("Preset")]
    [SerializeField] private ConnectorPreset preset = ConnectorPreset.IvLineLight;
    [SerializeField] private bool applyPresetOnAwake = true;

    [Header("Play Mode Stability")]
    [SerializeField] private bool lockSerializedOptionsDuringPlay = true;

    [Header("Shape")]
    [SerializeField, Min(2)] private int segments = 18;
    [SerializeField, Min(0f)] private float sagAmount = 0.02f;
    [SerializeField, Min(1)] private int renderSamplesPerSegment = 3;

    [Header("Simulation")]
    [SerializeField] private bool usePhysicsSimulation = true;
    [SerializeField, Min(1)] private int simulationStepsPerFrame = 2;
    [SerializeField, Min(1)] private int solverIterations = 8;
    [SerializeField, Range(0f, 1f)] private float gravityScale = 0.18f;
    [SerializeField, Range(0f, 1f)] private float velocityDamping = 0.1f;
    [SerializeField, Min(0f)] private float slackLength = 0.06f;

    [Header("Collision")]
    [SerializeField] private bool collideWithWorld = true;
    [SerializeField, Min(0.0005f)] private float collisionRadius = 0.006f;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Style")]
    [SerializeField] private ConnectorRenderMode renderMode = ConnectorRenderMode.TubeMesh;
    [SerializeField] private Color lineColor = new Color(0.95f, 0.98f, 1f, 0.16f);
    [SerializeField, Min(0.001f)] private float lineWidth = 0.01f;

    [Header("Tube Quality")]
    [SerializeField, Min(0.0005f)] private float tubeRadius = 0.004f;
    [SerializeField, Range(6, 32)] private int tubeSides = 14;
    [SerializeField, Range(1f, 20f)] private float tubeUvTiling = 8f;
    [SerializeField, Range(0f, 1f)] private float tubeSmoothness = 0.92f;

    private const string TubeVisualName = "__TubeVisual";
    private const float Epsilon = 0.000001f;

    private readonly Collider[] overlapResults = new Collider[24];

    private LineRenderer lineRenderer;
    private Transform tubeVisual;
    private MeshFilter tubeMeshFilter;
    private MeshRenderer tubeMeshRenderer;

    private Mesh runtimeTubeMesh;
    private Material runtimeMaterial;

    private Vector3[] centerPoints = Array.Empty<Vector3>();
    private Vector3[] sourceCenterPoints = Array.Empty<Vector3>();
    private Vector3[] vertices = Array.Empty<Vector3>();
    private Vector3[] normals = Array.Empty<Vector3>();
    private Vector2[] uvs = Array.Empty<Vector2>();
    private int[] triangles = Array.Empty<int>();

    private Vector3[] simulatedPoints = Array.Empty<Vector3>();
    private Vector3[] previousSimulatedPoints = Array.Empty<Vector3>();
    private bool simulationInitialized;

    private void Reset()
    {
      ApplySelectedPreset();
      EnsureCoreComponents();
      ApplyRendererDefaults();
      EnsureMaterial();
    }

    private void Awake()
    {
      bool shouldApplyPresetOnAwake = applyPresetOnAwake &&
        !(Application.isPlaying && lockSerializedOptionsDuringPlay);

      if (shouldApplyPresetOnAwake)
      {
        ApplySelectedPreset();
      }

      EnsureCoreComponents();
      ApplyRendererDefaults();
      EnsureMaterial();
    }

    private void OnEnable()
    {
      simulationInitialized = false;
      RefreshVisual();
    }

    private void OnDisable()
    {
      simulationInitialized = false;
    }

    private void LateUpdate()
    {
      RefreshVisual();
    }

    private void RefreshVisual()
    {
      EnsureCoreComponents();

      if (!endpoints.TryGetPositions(out Vector3 start, out Vector3 end))
      {
        SetRenderersEnabled(false);
        return;
      }

      ApplyRendererDefaults();
      EnsureMaterial();
      SetRenderersEnabled(true);

      if (renderMode == ConnectorRenderMode.TubeMesh)
      {
        RenderTube(start, end);
      }
      else
      {
        RenderLine(start, end);
      }
    }

    private void OnValidate()
    {
      if (!isActiveAndEnabled)
      {
        return;
      }

      RefreshVisual();
    }

    private void OnDestroy()
    {
      SafeDestroy(runtimeTubeMesh);
      runtimeTubeMesh = null;

      SafeDestroy(runtimeMaterial);
      runtimeMaterial = null;
    }

    public void SetEndpoints(IntravenousLineGroundedEndpoint endpointBinding)
    {
      endpoints = endpointBinding;
      simulationInitialized = false;
    }

    public void SetEndpoints(GameObject startPoint, GameObject endPoint)
    {
      endpoints = new IntravenousLineGroundedEndpoint(startPoint, endPoint);
      simulationInitialized = false;
    }

    public void SetEndpoints(Transform startPoint, Transform endPoint)
    {
      SetEndpoints(
        startPoint != null ? startPoint.gameObject : null,
        endPoint != null ? endPoint.gameObject : null);
      simulationInitialized = false;
    }

    [ContextMenu("Preset/Apply Selected Preset")]
    private void ApplySelectedPresetContextMenu()
    {
      ApplySelectedPreset();

      EnsureCoreComponents();
      ApplyRendererDefaults();
      EnsureMaterial();
      simulationInitialized = false;
    }

    [ContextMenu("Preset/IV Light")]
    private void ApplyIvPresetContextMenu()
    {
      preset = ConnectorPreset.IvLineLight;
      ApplySelectedPresetContextMenu();
    }

    [ContextMenu("Preset/Oxygen Line")]
    private void ApplyOxygenPresetContextMenu()
    {
      preset = ConnectorPreset.OxygenLine;
      ApplySelectedPresetContextMenu();
    }

    [ContextMenu("Preset/Suction Line")]
    private void ApplySuctionPresetContextMenu()
    {
      preset = ConnectorPreset.SuctionLine;
      ApplySelectedPresetContextMenu();
    }

    [ContextMenu("Preset/Defib Cable")]
    private void ApplyDefibPresetContextMenu()
    {
      preset = ConnectorPreset.DefibCable;
      ApplySelectedPresetContextMenu();
    }

    private void ApplySelectedPreset()
    {
      switch (preset)
      {
        case ConnectorPreset.Custom:
          return;

        case ConnectorPreset.IvLineLight:
          ApplyPreset(
            color: new Color(0.94f, 0.98f, 1f, 0.15f),
            width: 0.009f,
            radius: 0.0038f,
            sides: 16,
            smoothness: 0.95f,
            segmentCount: 20,
            baseSag: 0.012f,
            gravity: 0.16f,
            damping: 0.14f,
            slack: 0.055f,
            collisionSize: 0.0055f,
            stepCount: 2,
            iterationCount: 10);
          return;

        case ConnectorPreset.OxygenLine:
          ApplyPreset(
            color: new Color(0.86f, 0.98f, 1f, 0.18f),
            width: 0.01f,
            radius: 0.0042f,
            sides: 16,
            smoothness: 0.92f,
            segmentCount: 18,
            baseSag: 0.014f,
            gravity: 0.18f,
            damping: 0.12f,
            slack: 0.05f,
            collisionSize: 0.006f,
            stepCount: 2,
            iterationCount: 10);
          return;

        case ConnectorPreset.SuctionLine:
          ApplyPreset(
            color: new Color(1f, 1f, 1f, 0.24f),
            width: 0.012f,
            radius: 0.0052f,
            sides: 14,
            smoothness: 0.88f,
            segmentCount: 16,
            baseSag: 0.016f,
            gravity: 0.22f,
            damping: 0.16f,
            slack: 0.06f,
            collisionSize: 0.0065f,
            stepCount: 2,
            iterationCount: 8);
          return;

        case ConnectorPreset.DefibCable:
          ApplyPreset(
            color: new Color(0.12f, 0.12f, 0.12f, 0.9f),
            width: 0.009f,
            radius: 0.004f,
            sides: 12,
            smoothness: 0.72f,
            segmentCount: 14,
            baseSag: 0.01f,
            gravity: 0.14f,
            damping: 0.22f,
            slack: 0.03f,
            collisionSize: 0.005f,
            stepCount: 1,
            iterationCount: 7);
          return;

        default:
          return;
      }
    }

    private void ApplyPreset(
      Color color,
      float width,
      float radius,
      int sides,
      float smoothness,
      int segmentCount,
      float baseSag,
      float gravity,
      float damping,
      float slack,
      float collisionSize,
      int stepCount,
      int iterationCount)
    {
      renderMode = ConnectorRenderMode.TubeMesh;

      lineColor = color;
      lineWidth = width;
      tubeRadius = radius;
      tubeSides = sides;
      tubeSmoothness = smoothness;

      segments = segmentCount;
      sagAmount = baseSag;

      usePhysicsSimulation = true;
      simulationStepsPerFrame = stepCount;
      solverIterations = iterationCount;
      gravityScale = gravity;
      velocityDamping = damping;
      slackLength = slack;

      collideWithWorld = true;
      collisionRadius = collisionSize;
    }

    private void EnsureCoreComponents()
    {
      EnsureLineRenderer();
      EnsureTubeVisual();
      EnsureRuntimeTubeMesh();
    }

    private void EnsureLineRenderer()
    {
      if (lineRenderer == null)
      {
        lineRenderer = GetComponent<LineRenderer>();
      }
    }

    private void EnsureTubeVisual()
    {
      if (tubeVisual == null)
      {
        Transform existing = transform.Find(TubeVisualName);
        if (existing != null)
        {
          tubeVisual = existing;
        }
        else
        {
          GameObject child = new GameObject(TubeVisualName);
          child.transform.SetParent(transform, false);
          tubeVisual = child.transform;
        }
      }

      if (tubeMeshFilter == null)
      {
        tubeMeshFilter = tubeVisual.GetComponent<MeshFilter>();
        if (tubeMeshFilter == null)
        {
          tubeMeshFilter = tubeVisual.gameObject.AddComponent<MeshFilter>();
        }
      }

      if (tubeMeshRenderer == null)
      {
        tubeMeshRenderer = tubeVisual.GetComponent<MeshRenderer>();
        if (tubeMeshRenderer == null)
        {
          tubeMeshRenderer = tubeVisual.gameObject.AddComponent<MeshRenderer>();
        }
      }
    }

    private void EnsureRuntimeTubeMesh()
    {
      if (runtimeTubeMesh == null)
      {
        runtimeTubeMesh = new Mesh
        {
          name = "MESH_Runtime_IntravenousLineGrounded"
        };

        runtimeTubeMesh.MarkDynamic();
      }

      if (tubeMeshFilter != null && tubeMeshFilter.sharedMesh != runtimeTubeMesh)
      {
        tubeMeshFilter.sharedMesh = runtimeTubeMesh;
      }
    }

    private void ApplyRendererDefaults()
    {
      if (lineRenderer != null)
      {
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCornerVertices = 8;
        lineRenderer.numCapVertices = 8;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
      }

      if (tubeMeshRenderer != null)
      {
        tubeMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        tubeMeshRenderer.receiveShadows = false;
      }
    }

    private void EnsureMaterial()
    {
      Material assignedMaterial = null;
      bool usingRuntimeMaterial = false;

      if (tubeMeshRenderer != null && tubeMeshRenderer.sharedMaterial != null)
      {
        assignedMaterial = tubeMeshRenderer.sharedMaterial;
      }
      else if (lineRenderer != null && lineRenderer.sharedMaterial != null)
      {
        assignedMaterial = lineRenderer.sharedMaterial;
      }

      if (assignedMaterial == null)
      {
        if (runtimeMaterial == null)
        {
          Shader shader = FindPreferredTubeShader();
          if (shader == null)
          {
            return;
          }

          runtimeMaterial = new Material(shader)
          {
            name = "M_Runtime_TransparentTube"
          };
        }

        assignedMaterial = runtimeMaterial;
        usingRuntimeMaterial = true;
      }
      else if (assignedMaterial == runtimeMaterial)
      {
        usingRuntimeMaterial = true;
      }

      ConfigureMaterial(assignedMaterial, usingRuntimeMaterial);

      if (lineRenderer != null)
      {
        lineRenderer.sharedMaterial = assignedMaterial;
      }

      if (tubeMeshRenderer != null)
      {
        tubeMeshRenderer.sharedMaterial = assignedMaterial;
      }
    }

    private static Shader FindPreferredTubeShader()
    {
      RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
      if (pipeline != null)
      {
        string pipelineType = pipeline.GetType().Name;

        if (pipelineType.Contains("HDRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "HDRP/Lit",
            "HDRP/Unlit",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Standard",
            "Legacy Shaders/Transparent/Diffuse",
            "Sprites/Default");
        }

        if (pipelineType.Contains("UniversalRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "HDRP/Lit",
            "Standard",
            "Legacy Shaders/Transparent/Diffuse",
            "Sprites/Default");
        }
      }

      return FindFirstSupportedShader(
        "Standard",
        "Legacy Shaders/Transparent/Diffuse",
        "HDRP/Lit",
        "Universal Render Pipeline/Lit",
        "Sprites/Default");
    }

    private static Shader FindFirstSupportedShader(params string[] shaderNames)
    {
      for (int i = 0; i < shaderNames.Length; i++)
      {
        Shader shader = Shader.Find(shaderNames[i]);
        if (shader != null && shader.isSupported)
        {
          return shader;
        }
      }

      return null;
    }

    private void ConfigureMaterial(Material material, bool configureRuntimeTransparency)
    {
      if (material == null)
      {
        return;
      }

      if (material.HasProperty("_Color"))
      {
        material.SetColor("_Color", lineColor);
      }

      if (material.HasProperty("_BaseColor"))
      {
        material.SetColor("_BaseColor", lineColor);
      }

      if (material.HasProperty("_Smoothness"))
      {
        material.SetFloat("_Smoothness", tubeSmoothness);
      }

      if (material.HasProperty("_Glossiness"))
      {
        material.SetFloat("_Glossiness", tubeSmoothness);
      }

      if (!configureRuntimeTransparency)
      {
        return;
      }

      if (material.HasProperty("_Metallic"))
      {
        material.SetFloat("_Metallic", 0f);
      }

      if (material.HasProperty("_Mode"))
      {
        material.SetFloat("_Mode", 3f);
      }

      if (material.HasProperty("_Surface"))
      {
        material.SetFloat("_Surface", 1f);
      }

      if (material.HasProperty("_SurfaceType"))
      {
        material.SetFloat("_SurfaceType", 1f);
      }

      if (material.HasProperty("_Blend"))
      {
        material.SetFloat("_Blend", 0f);
      }

      if (material.HasProperty("_SrcBlend"))
      {
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      }

      if (material.HasProperty("_DstBlend"))
      {
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      }

      if (material.HasProperty("_ZWrite"))
      {
        material.SetInt("_ZWrite", 0);
      }

      material.EnableKeyword("_ALPHABLEND_ON");
      material.DisableKeyword("_ALPHATEST_ON");
      material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
      material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void SetRenderersEnabled(bool enabled)
    {
      if (!enabled)
      {
        if (lineRenderer != null)
        {
          lineRenderer.enabled = false;
          lineRenderer.positionCount = 0;
        }

        if (tubeMeshRenderer != null)
        {
          tubeMeshRenderer.enabled = false;
        }

        if (runtimeTubeMesh != null)
        {
          runtimeTubeMesh.Clear();
        }

        return;
      }

      if (lineRenderer != null)
      {
        lineRenderer.enabled = renderMode == ConnectorRenderMode.LineRenderer;
      }

      if (tubeMeshRenderer != null)
      {
        tubeMeshRenderer.enabled = renderMode == ConnectorRenderMode.TubeMesh;
      }
    }

    private void RenderLine(Vector3 start, Vector3 end)
    {
      if (lineRenderer == null)
      {
        return;
      }

      int pointCount = BuildCenterLine(start, end);

      lineRenderer.positionCount = pointCount;
      for (int i = 0; i < pointCount; i++)
      {
        lineRenderer.SetPosition(i, centerPoints[i]);
      }
    }

    private void RenderTube(Vector3 start, Vector3 end)
    {
      if (runtimeTubeMesh == null || tubeVisual == null)
      {
        return;
      }

      int pointCount = BuildCenterLine(start, end);
      int sideCount = Mathf.Max(3, tubeSides);

      EnsureTubeBuffers(pointCount, sideCount);
      BuildTubeVertexData(pointCount, sideCount);
      BuildTubeTriangles(pointCount, sideCount);

      runtimeTubeMesh.Clear();
      runtimeTubeMesh.vertices = vertices;
      runtimeTubeMesh.normals = normals;
      runtimeTubeMesh.uv = uvs;
      runtimeTubeMesh.triangles = triangles;
      runtimeTubeMesh.RecalculateBounds();
    }

    private int BuildCenterLine(Vector3 start, Vector3 end)
    {
      int sourcePointCount = Mathf.Max(2, segments);
      EnsureSourcePointBuffer(sourcePointCount);

      if (usePhysicsSimulation && Application.isPlaying)
      {
        SimulateCenterLine(start, end, sourcePointCount, sourceCenterPoints);
      }
      else
      {
        BuildAnalyticCenterLine(start, end, sourcePointCount, sourceCenterPoints);
        simulationInitialized = false;
      }

      int sampleCount = Mathf.Max(1, renderSamplesPerSegment);
      if (sampleCount == 1 || sourcePointCount <= 2)
      {
        EnsureCenterPointBuffer(sourcePointCount);
        Array.Copy(sourceCenterPoints, centerPoints, sourcePointCount);
        return sourcePointCount;
      }

      int smoothedPointCount = ((sourcePointCount - 1) * sampleCount) + 1;
      EnsureCenterPointBuffer(smoothedPointCount);
      BuildSmoothCenterLine(sourceCenterPoints, sourcePointCount, sampleCount, centerPoints);

      return smoothedPointCount;
    }

    private void SimulateCenterLine(Vector3 start, Vector3 end, int pointCount, Vector3[] outputPoints)
    {
      EnsureSimulationBuffers(pointCount);

      bool needsReinitialize = !simulationInitialized ||
        (simulatedPoints[0] - start).sqrMagnitude > 0.5f ||
        (simulatedPoints[pointCount - 1] - end).sqrMagnitude > 0.5f;

      if (needsReinitialize)
      {
        InitializeSimulation(start, end, pointCount);
      }

      int last = pointCount - 1;
      simulatedPoints[0] = start;
      simulatedPoints[last] = end;
      previousSimulatedPoints[0] = start;
      previousSimulatedPoints[last] = end;

      float deltaTime = Mathf.Clamp(Time.deltaTime, 1f / 240f, 1f / 30f);
      int stepCount = Mathf.Max(1, simulationStepsPerFrame);
      float stepDt = deltaTime / stepCount;

      Vector3 baseGravity = Physics.gravity.sqrMagnitude > Epsilon
        ? Physics.gravity
        : Vector3.down * 9.81f;

      Vector3 gravityStep = baseGravity * gravityScale * stepDt * stepDt;
      float dampingFactor = 1f - Mathf.Clamp01(velocityDamping);

      float totalLength = Vector3.Distance(start, end) + slackLength;
      float segmentLength = totalLength / (pointCount - 1);

      for (int step = 0; step < stepCount; step++)
      {
        IntegrateSimulationPoints(pointCount, gravityStep, dampingFactor);

        for (int iteration = 0; iteration < Mathf.Max(1, solverIterations); iteration++)
        {
          SolveDistanceConstraints(pointCount, segmentLength, start, end);

          if (collideWithWorld)
          {
            ResolveWorldCollisions(pointCount);
          }
        }
      }

      Array.Copy(simulatedPoints, outputPoints, pointCount);
    }

    private void EnsureSimulationBuffers(int pointCount)
    {
      if (simulatedPoints.Length != pointCount)
      {
        simulatedPoints = new Vector3[pointCount];
        previousSimulatedPoints = new Vector3[pointCount];
        simulationInitialized = false;
      }
    }

    private void InitializeSimulation(Vector3 start, Vector3 end, int pointCount)
    {
      for (int i = 0; i < pointCount; i++)
      {
        float t = i / (pointCount - 1f);
        Vector3 point = Vector3.Lerp(start, end, t);

        float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
        point += Vector3.down * sag;

        simulatedPoints[i] = point;
        previousSimulatedPoints[i] = point;
      }

      simulationInitialized = true;
    }

    private void IntegrateSimulationPoints(int pointCount, Vector3 gravityStep, float dampingFactor)
    {
      for (int i = 1; i < pointCount - 1; i++)
      {
        Vector3 current = simulatedPoints[i];
        Vector3 velocity = (current - previousSimulatedPoints[i]) * dampingFactor;

        previousSimulatedPoints[i] = current;
        simulatedPoints[i] = current + velocity + gravityStep;
      }
    }

    private void SolveDistanceConstraints(int pointCount, float targetSegmentLength, Vector3 start, Vector3 end)
    {
      int last = pointCount - 1;
      simulatedPoints[0] = start;
      simulatedPoints[last] = end;

      for (int i = 0; i < pointCount - 1; i++)
      {
        Vector3 p1 = simulatedPoints[i];
        Vector3 p2 = simulatedPoints[i + 1];
        Vector3 delta = p2 - p1;
        float distance = delta.magnitude;
        if (distance < Epsilon)
        {
          continue;
        }

        float distanceError = distance - targetSegmentLength;
        Vector3 correction = delta * (distanceError / distance);

        if (i == 0)
        {
          p2 -= correction;
        }
        else if (i + 1 == last)
        {
          p1 += correction;
        }
        else
        {
          Vector3 half = correction * 0.5f;
          p1 += half;
          p2 -= half;
        }

        simulatedPoints[i] = p1;
        simulatedPoints[i + 1] = p2;
      }

      simulatedPoints[0] = start;
      simulatedPoints[last] = end;
    }

    private void ResolveWorldCollisions(int pointCount)
    {
      for (int i = 1; i < pointCount - 1; i++)
      {
        Vector3 previous = previousSimulatedPoints[i];
        Vector3 current = simulatedPoints[i];
        Vector3 movement = current - previous;
        float movementDistance = movement.magnitude;

        if (movementDistance > Epsilon)
        {
          if (Physics.SphereCast(
                previous,
                collisionRadius,
                movement / movementDistance,
                out RaycastHit hit,
                movementDistance,
                collisionMask,
                triggerInteraction))
          {
            current = hit.point + hit.normal * collisionRadius;
            previousSimulatedPoints[i] = Vector3.Lerp(previous, current, 0.5f);
          }
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(
          current,
          collisionRadius,
          overlapResults,
          collisionMask,
          triggerInteraction);

        for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
        {
          Collider collider = overlapResults[overlapIndex];
          if (collider == null || !collider.enabled)
          {
            continue;
          }

          Vector3 closest = collider.ClosestPoint(current);
          Vector3 toPoint = current - closest;
          float distance = toPoint.magnitude;

          if (distance < Epsilon)
          {
            Vector3 fallbackDirection = current - collider.bounds.center;
            if (fallbackDirection.sqrMagnitude < Epsilon)
            {
              fallbackDirection = Vector3.up;
            }

            current = closest + fallbackDirection.normalized * collisionRadius;
            continue;
          }

          if (distance < collisionRadius)
          {
            current = closest + (toPoint / distance) * collisionRadius;
          }
        }

        simulatedPoints[i] = current;
      }
    }

    private void BuildAnalyticCenterLine(Vector3 start, Vector3 end, int pointCount, Vector3[] outputPoints)
    {
      for (int i = 0; i < pointCount; i++)
      {
        float t = i / (pointCount - 1f);
        Vector3 point = Vector3.Lerp(start, end, t);

        if (sagAmount > 0f)
        {
          float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
          point += Vector3.down * sag;
        }

        outputPoints[i] = point;
      }
    }

    private void BuildSmoothCenterLine(
      Vector3[] sourcePoints,
      int sourcePointCount,
      int sampleCount,
      Vector3[] outputPoints)
    {
      int outputIndex = 0;

      for (int segmentIndex = 0; segmentIndex < sourcePointCount - 1; segmentIndex++)
      {
        int p0Index = Mathf.Max(segmentIndex - 1, 0);
        int p1Index = segmentIndex;
        int p2Index = segmentIndex + 1;
        int p3Index = Mathf.Min(segmentIndex + 2, sourcePointCount - 1);

        Vector3 p0 = sourcePoints[p0Index];
        Vector3 p1 = sourcePoints[p1Index];
        Vector3 p2 = sourcePoints[p2Index];
        Vector3 p3 = sourcePoints[p3Index];

        for (int step = 0; step < sampleCount; step++)
        {
          float t = step / (float)sampleCount;
          outputPoints[outputIndex++] = CatmullRom(p0, p1, p2, p3, t);
        }
      }

      outputPoints[outputIndex] = sourcePoints[sourcePointCount - 1];
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
      float t2 = t * t;
      float t3 = t2 * t;

      return 0.5f * (
        (2f * p1)
        + (-p0 + p2) * t
        + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
        + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void EnsureSourcePointBuffer(int pointCount)
    {
      if (sourceCenterPoints.Length != pointCount)
      {
        sourceCenterPoints = new Vector3[pointCount];
      }
    }

    private void EnsureCenterPointBuffer(int pointCount)
    {
      if (centerPoints.Length != pointCount)
      {
        centerPoints = new Vector3[pointCount];
      }
    }

    private void EnsureTubeBuffers(int pointCount, int sideCount)
    {
      int vertexCount = pointCount * sideCount;
      int triangleIndexCount = (pointCount - 1) * sideCount * 6;

      if (vertices.Length != vertexCount)
      {
        vertices = new Vector3[vertexCount];
      }

      if (normals.Length != vertexCount)
      {
        normals = new Vector3[vertexCount];
      }

      if (uvs.Length != vertexCount)
      {
        uvs = new Vector2[vertexCount];
      }

      if (triangles.Length != triangleIndexCount)
      {
        triangles = new int[triangleIndexCount];
      }
    }

    private void BuildTubeVertexData(int pointCount, int sideCount)
    {
      Transform visualTransform = tubeVisual;

      for (int i = 0; i < pointCount; i++)
      {
        Vector3 center = centerPoints[i];
        Vector3 tangent = GetCenterlineTangent(i, pointCount);

        Vector3 upReference = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.98f
          ? Vector3.right
          : Vector3.up;

        Vector3 normal = Vector3.Cross(tangent, upReference).normalized;
        Vector3 binormal = Vector3.Cross(normal, tangent).normalized;

        float v = (i / (pointCount - 1f)) * tubeUvTiling;
        int ringStart = i * sideCount;

        for (int side = 0; side < sideCount; side++)
        {
          float ratio = side / (float)sideCount;
          float angle = ratio * Mathf.PI * 2f;

          Vector3 radialWorld = (Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal) * tubeRadius;
          Vector3 vertexWorld = center + radialWorld;

          int vertexIndex = ringStart + side;
          vertices[vertexIndex] = visualTransform.InverseTransformPoint(vertexWorld);
          normals[vertexIndex] = visualTransform.InverseTransformDirection(radialWorld.normalized);
          uvs[vertexIndex] = new Vector2(ratio, v);
        }
      }
    }

    private Vector3 GetCenterlineTangent(int index, int pointCount)
    {
      if (pointCount <= 1)
      {
        return Vector3.forward;
      }

      Vector3 tangent;
      if (index == 0)
      {
        tangent = centerPoints[1] - centerPoints[0];
      }
      else if (index == pointCount - 1)
      {
        tangent = centerPoints[pointCount - 1] - centerPoints[pointCount - 2];
      }
      else
      {
        tangent = centerPoints[index + 1] - centerPoints[index - 1];
      }

      if (tangent.sqrMagnitude < Epsilon)
      {
        return Vector3.forward;
      }

      return tangent.normalized;
    }

    private void BuildTubeTriangles(int pointCount, int sideCount)
    {
      int triangleIndex = 0;

      for (int ring = 0; ring < pointCount - 1; ring++)
      {
        int currentRingStart = ring * sideCount;
        int nextRingStart = (ring + 1) * sideCount;

        for (int side = 0; side < sideCount; side++)
        {
          int nextSide = (side + 1) % sideCount;

          int current = currentRingStart + side;
          int currentNext = currentRingStart + nextSide;
          int next = nextRingStart + side;
          int nextNext = nextRingStart + nextSide;

          triangles[triangleIndex++] = current;
          triangles[triangleIndex++] = currentNext;
          triangles[triangleIndex++] = next;

          triangles[triangleIndex++] = currentNext;
          triangles[triangleIndex++] = nextNext;
          triangles[triangleIndex++] = next;
        }
      }
    }

    private static void SafeDestroy(UnityEngine.Object target)
    {
      if (target == null)
      {
        return;
      }

      if (Application.isPlaying)
      {
        Destroy(target);
      }
      else
      {
        DestroyImmediate(target);
      }
    }
  }
}
