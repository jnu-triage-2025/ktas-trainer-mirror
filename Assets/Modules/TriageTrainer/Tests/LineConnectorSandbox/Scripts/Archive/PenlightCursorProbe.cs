using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// 마우스 커서를 펜라이트 광원처럼 사용해 동공 영역 통과 여부를 판정합니다.
  /// </summary>
  [DisallowMultipleComponent]
  public class PenlightCursorProbe : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private PupilReflexEye[] targetEyes = Array.Empty<PupilReflexEye>();

    [Header("Probe Settings")]
    [SerializeField] private bool probeEnabled = true;
    [SerializeField] private bool requirePrimaryButtonHold;
    [SerializeField, Min(0.5f)] private float lightRadiusMillimeters = 3.5f;
    [SerializeField, Min(0.1f)] private float maxProbeDistance = 30f;

    [Header("Penlight Visual")]
    [SerializeField] private bool useCursorSpotLight = true;
    [SerializeField, Min(0.01f)] private float spotLightDistanceFromCamera = 0.11f;
    [SerializeField, Min(0.1f)] private float spotLightRangeMeters = 6f;
    [SerializeField, Range(5f, 80f)] private float spotLightAngleDegrees = 22f;
    [SerializeField, Min(0f)] private float spotLightIntensity = 16f;
    [SerializeField] private Color spotLightColor = new Color(1f, 0.95f, 0.82f, 1f);
    [SerializeField] private bool useCursorBeamVisual = true;
    [SerializeField, Min(0.01f)] private float beamStartOffsetMeters = 0.1f;
    [SerializeField, Min(0.1f)] private float beamMaxDistanceMeters = 7f;
    [SerializeField, Min(0.001f)] private float beamStartWidthMeters = 0.02f;
    [SerializeField, Min(0.001f)] private float beamEndWidthMeters = 0.55f;
    [SerializeField] private Color beamColor = new Color(1f, 0.95f, 0.82f, 0.17f);
    [SerializeField, Range(0f, 1f)] private float beamLengthSmoothing = 0.35f;
    [SerializeField] private bool useImpactSpotVisual = true;
    [SerializeField, Min(0.01f)] private float impactSpotRadiusMeters = 0.14f;
    [SerializeField, Min(0f)] private float impactSpotSurfaceOffsetMeters = 0.003f;
    [SerializeField] private Color impactSpotColor = new Color(1f, 0.95f, 0.82f, 0.75f);

    [Header("Cursor")]
    [SerializeField] private bool hideSystemCursor = true;
    [SerializeField] private bool drawReticle = true;
    [SerializeField, Min(4f)] private float reticlePixelRadius = 12f;
    [SerializeField] private Color reticleColor = new Color(1f, 0.98f, 0.8f, 0.95f);

    [Header("Debug")]
    [SerializeField] private bool autoDiscoverEyes = true;
    [SerializeField] private bool debugShowHoveredEyeName = false;

    private readonly List<PupilReflexEye> eyes = new List<PupilReflexEye>();

    private Texture2D reticleTexture;
    private PupilReflexEye currentHoveredEye;
    private Light runtimeSpotLight;
    private Transform runtimeSpotLightTransform;
    private Transform runtimeBeamTransform;
    private Transform runtimeBeamQuadATransform;
    private Transform runtimeBeamQuadBTransform;
    private MeshRenderer runtimeBeamQuadARenderer;
    private MeshRenderer runtimeBeamQuadBRenderer;
    private Material runtimeBeamMaterial;
    private Texture2D runtimeBeamTexture;
    private Transform runtimeImpactSpotTransform;
    private MeshRenderer runtimeImpactSpotRenderer;
    private Material runtimeImpactSpotMaterial;
    private Texture2D runtimeImpactSpotTexture;
    private float smoothedBeamDistanceMeters;

    public bool ProbeEnabled => probeEnabled;
    public PupilReflexEye CurrentHoveredEye => currentHoveredEye;

    public event Action<PupilReflexEye> HoveredEyeChanged;

    private void Awake()
    {
      if (targetCamera == null)
      {
        targetCamera = Camera.main;
      }

      RebuildEyeCache();
      EnsureReticleTexture();
      EnsureRuntimeSpotLight();
      EnsureRuntimeBeamVisual();
      EnsureRuntimeImpactSpotVisual();
    }

    private void OnEnable()
    {
      if (hideSystemCursor)
      {
        Cursor.visible = false;
      }
    }

    private void OnDisable()
    {
      ClearAllEyeLighting();
      currentHoveredEye = null;
      SetRuntimeSpotLightEnabled(false);
      SetRuntimeBeamVisualEnabled(false);
      SetRuntimeImpactSpotVisualEnabled(false);

      if (hideSystemCursor)
      {
        Cursor.visible = true;
      }
    }

    private void OnDestroy()
    {
      if (reticleTexture != null)
      {
        if (Application.isPlaying)
        {
          Destroy(reticleTexture);
        }
        else
        {
          DestroyImmediate(reticleTexture);
        }

        reticleTexture = null;
      }

      if (runtimeSpotLightTransform != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeSpotLightTransform.gameObject);
        }
        else
        {
          DestroyImmediate(runtimeSpotLightTransform.gameObject);
        }

        runtimeSpotLight = null;
        runtimeSpotLightTransform = null;
      }

      if (runtimeBeamTransform != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeBeamTransform.gameObject);
        }
        else
        {
          DestroyImmediate(runtimeBeamTransform.gameObject);
        }

        runtimeBeamTransform = null;
        runtimeBeamQuadATransform = null;
        runtimeBeamQuadBTransform = null;
        runtimeBeamQuadARenderer = null;
        runtimeBeamQuadBRenderer = null;
      }

      if (runtimeBeamMaterial != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeBeamMaterial);
        }
        else
        {
          DestroyImmediate(runtimeBeamMaterial);
        }

        runtimeBeamMaterial = null;
      }

      if (runtimeBeamTexture != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeBeamTexture);
        }
        else
        {
          DestroyImmediate(runtimeBeamTexture);
        }

        runtimeBeamTexture = null;
      }

      if (runtimeImpactSpotTransform != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeImpactSpotTransform.gameObject);
        }
        else
        {
          DestroyImmediate(runtimeImpactSpotTransform.gameObject);
        }

        runtimeImpactSpotTransform = null;
        runtimeImpactSpotRenderer = null;
      }

      if (runtimeImpactSpotMaterial != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeImpactSpotMaterial);
        }
        else
        {
          DestroyImmediate(runtimeImpactSpotMaterial);
        }

        runtimeImpactSpotMaterial = null;
      }

      if (runtimeImpactSpotTexture != null)
      {
        if (Application.isPlaying)
        {
          Destroy(runtimeImpactSpotTexture);
        }
        else
        {
          DestroyImmediate(runtimeImpactSpotTexture);
        }

        runtimeImpactSpotTexture = null;
      }
    }

    private void OnValidate()
    {
      lightRadiusMillimeters = Mathf.Max(0.5f, lightRadiusMillimeters);
      maxProbeDistance = Mathf.Max(0.1f, maxProbeDistance);
      reticlePixelRadius = Mathf.Max(4f, reticlePixelRadius);
      spotLightDistanceFromCamera = Mathf.Max(0.01f, spotLightDistanceFromCamera);
      spotLightRangeMeters = Mathf.Max(0.1f, spotLightRangeMeters);
      spotLightAngleDegrees = Mathf.Clamp(spotLightAngleDegrees, 5f, 80f);
      spotLightIntensity = Mathf.Max(0f, spotLightIntensity);
      beamStartOffsetMeters = Mathf.Max(0.01f, beamStartOffsetMeters);
      beamMaxDistanceMeters = Mathf.Max(0.1f, beamMaxDistanceMeters);
      beamStartWidthMeters = Mathf.Max(0.001f, beamStartWidthMeters);
      beamEndWidthMeters = Mathf.Max(beamStartWidthMeters, beamEndWidthMeters);
      beamLengthSmoothing = Mathf.Clamp01(beamLengthSmoothing);
      impactSpotRadiusMeters = Mathf.Max(0.01f, impactSpotRadiusMeters);
      impactSpotSurfaceOffsetMeters = Mathf.Max(0f, impactSpotSurfaceOffsetMeters);

      ConfigureRuntimeSpotLight();
      ConfigureRuntimeBeamVisual();
      ConfigureRuntimeImpactSpotVisual();

      if (!Application.isPlaying)
      {
        RebuildEyeCache();
      }
    }

    private void Update()
    {
      if (targetCamera == null)
      {
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
          targetCamera = FindObjectOfType<Camera>();
        }

        if (targetCamera == null)
        {
          ClearAllEyeLighting();
          SetRuntimeSpotLightEnabled(false);
          SetRuntimeBeamVisualEnabled(false);
          SetRuntimeImpactSpotVisualEnabled(false);
          return;
        }
      }

      if (autoDiscoverEyes && eyes.Count == 0)
      {
        RebuildEyeCache();
      }

      bool shouldEmitLight = ShouldEmitLight();
      Ray cursorRay = targetCamera.ScreenPointToRay(Input.mousePosition);
      UpdateRuntimeSpotLight(cursorRay, shouldEmitLight);

      if (!shouldEmitLight)
      {
        UpdateRuntimeBeamVisual(cursorRay, false, -1f);
        SetHoveredEye(null);
        ClearAllEyeLighting();
        return;
      }

      float lightRadiusMeters = lightRadiusMillimeters * 0.001f;

      PupilReflexEye hoveredEye = null;
      float nearestDepth = float.MaxValue;

      for (int i = 0; i < eyes.Count; i++)
      {
        PupilReflexEye eye = eyes[i];
        if (eye == null)
        {
          continue;
        }

        if (!eye.TryGetDirectLightHit(
          cursorRay,
          lightRadiusMeters,
          maxProbeDistance,
          out float depth,
          out _))
        {
          continue;
        }

        if (depth < nearestDepth)
        {
          nearestDepth = depth;
          hoveredEye = eye;
        }
      }

      SetHoveredEye(hoveredEye);

      for (int i = 0; i < eyes.Count; i++)
      {
        PupilReflexEye eye = eyes[i];
        if (eye == null)
        {
          continue;
        }

        eye.SetDirectLightOnPupil(eye == hoveredEye);
      }

      float hoveredEyeDepth = hoveredEye != null ? nearestDepth : -1f;
      UpdateRuntimeBeamVisual(cursorRay, true, hoveredEyeDepth);
    }

    private void OnGUI()
    {
      if (!drawReticle || !probeEnabled)
      {
        return;
      }

      if (reticleTexture == null)
      {
        EnsureReticleTexture();
      }

      if (reticleTexture == null)
      {
        return;
      }

      Vector3 mouse = Input.mousePosition;
      float diameter = reticlePixelRadius * 2f;
      float guiY = Screen.height - mouse.y;
      Rect rect = new Rect(mouse.x - reticlePixelRadius, guiY - reticlePixelRadius, diameter, diameter);

      Color previousColor = GUI.color;
      GUI.color = reticleColor;
      GUI.DrawTexture(rect, reticleTexture);
      GUI.color = previousColor;

      if (debugShowHoveredEyeName && currentHoveredEye != null)
      {
        GUI.Label(
          new Rect(rect.x + diameter + 8f, rect.y - 2f, 220f, 20f),
          "Penlight: " + currentHoveredEye.Side);
      }
    }

    public void SetProbeEnabled(bool enabled)
    {
      probeEnabled = enabled;

      if (!probeEnabled)
      {
        SetHoveredEye(null);
        ClearAllEyeLighting();
      }

      SetRuntimeSpotLightEnabled(enabled && ShouldEmitLight());

      if (!enabled)
      {
        SetRuntimeBeamVisualEnabled(false);
        SetRuntimeImpactSpotVisualEnabled(false);
      }
    }

    public void SetTargetEyes(IReadOnlyList<PupilReflexEye> assignedEyes)
    {
      eyes.Clear();

      if (assignedEyes == null)
      {
        targetEyes = Array.Empty<PupilReflexEye>();
        return;
      }

      targetEyes = new PupilReflexEye[assignedEyes.Count];
      for (int i = 0; i < assignedEyes.Count; i++)
      {
        PupilReflexEye eye = assignedEyes[i];
        targetEyes[i] = eye;

        if (eye != null)
        {
          eyes.Add(eye);
        }
      }
    }

    public void RebuildEyeCache()
    {
      eyes.Clear();

      if (targetEyes != null && targetEyes.Length > 0)
      {
        for (int i = 0; i < targetEyes.Length; i++)
        {
          PupilReflexEye eye = targetEyes[i];
          if (eye != null)
          {
            eyes.Add(eye);
          }
        }
      }

      if (!autoDiscoverEyes || eyes.Count > 0)
      {
        return;
      }

      PupilReflexEye[] discoveredEyes = FindObjectsOfType<PupilReflexEye>(true);
      if (discoveredEyes == null || discoveredEyes.Length == 0)
      {
        return;
      }

      targetEyes = discoveredEyes;
      for (int i = 0; i < discoveredEyes.Length; i++)
      {
        if (discoveredEyes[i] != null)
        {
          eyes.Add(discoveredEyes[i]);
        }
      }
    }

    private bool ShouldEmitLight()
    {
      if (!probeEnabled)
      {
        return false;
      }

      if (!requirePrimaryButtonHold)
      {
        return true;
      }

      return Input.GetMouseButton(0);
    }

    private void ClearAllEyeLighting()
    {
      for (int i = 0; i < eyes.Count; i++)
      {
        PupilReflexEye eye = eyes[i];
        if (eye != null)
        {
          eye.SetDirectLightOnPupil(false);
        }
      }
    }

    private void SetHoveredEye(PupilReflexEye hoveredEye)
    {
      if (currentHoveredEye == hoveredEye)
      {
        return;
      }

      currentHoveredEye = hoveredEye;
      HoveredEyeChanged?.Invoke(currentHoveredEye);
    }

    private void EnsureRuntimeSpotLight()
    {
      if (runtimeSpotLightTransform != null)
      {
        ConfigureRuntimeSpotLight();
        return;
      }

      GameObject lightObject = new GameObject("RuntimePenlightSpotLight");
      runtimeSpotLightTransform = lightObject.transform;
      runtimeSpotLightTransform.SetParent(transform, false);

      runtimeSpotLight = lightObject.AddComponent<Light>();
      ConfigureRuntimeSpotLight();
      SetRuntimeSpotLightEnabled(false);
    }

    private void ConfigureRuntimeSpotLight()
    {
      if (runtimeSpotLight == null)
      {
        return;
      }

      runtimeSpotLight.type = LightType.Spot;
      runtimeSpotLight.color = spotLightColor;
      runtimeSpotLight.range = spotLightRangeMeters;
      runtimeSpotLight.spotAngle = spotLightAngleDegrees;
      runtimeSpotLight.intensity = spotLightIntensity;
      runtimeSpotLight.shadows = LightShadows.None;
      runtimeSpotLight.renderMode = LightRenderMode.Auto;
    }

    private void UpdateRuntimeSpotLight(Ray cursorRay, bool shouldEmitLight)
    {
      if (!useCursorSpotLight)
      {
        SetRuntimeSpotLightEnabled(false);
        return;
      }

      if (runtimeSpotLight == null)
      {
        EnsureRuntimeSpotLight();
      }

      if (runtimeSpotLight == null || runtimeSpotLightTransform == null)
      {
        return;
      }

      if (!shouldEmitLight)
      {
        SetRuntimeSpotLightEnabled(false);
        return;
      }

      runtimeSpotLightTransform.position = cursorRay.origin + (cursorRay.direction * spotLightDistanceFromCamera);
      runtimeSpotLightTransform.rotation = Quaternion.LookRotation(cursorRay.direction, Vector3.up);
      runtimeSpotLight.intensity = spotLightIntensity;
      runtimeSpotLight.color = spotLightColor;
      SetRuntimeSpotLightEnabled(true);
    }

    private void SetRuntimeSpotLightEnabled(bool enabled)
    {
      if (runtimeSpotLight == null)
      {
        return;
      }

      runtimeSpotLight.enabled = enabled;
    }

    private void EnsureRuntimeBeamVisual()
    {
      if (runtimeBeamTransform != null)
      {
        ConfigureRuntimeBeamVisual();
        return;
      }

      GameObject beamObject = new GameObject("RuntimePenlightBeam");
      runtimeBeamTransform = beamObject.transform;
      runtimeBeamTransform.SetParent(transform, false);

      if (runtimeBeamMaterial == null)
      {
        Shader beamShader = FindPreferredBeamShader();
        if (beamShader != null)
        {
          runtimeBeamMaterial = new Material(beamShader)
          {
            name = "M_Runtime_PenlightBeam"
          };

          SetupTransparentUnlitMaterial(runtimeBeamMaterial);
        }
      }

      EnsureRuntimeBeamTexture();

      if (runtimeBeamMaterial != null && runtimeBeamTexture != null)
      {
        runtimeBeamMaterial.mainTexture = runtimeBeamTexture;

        if (runtimeBeamMaterial.HasProperty("_BaseMap"))
        {
          runtimeBeamMaterial.SetTexture("_BaseMap", runtimeBeamTexture);
        }
      }

      runtimeBeamQuadATransform = CreateRuntimeQuad("RuntimePenlightBeamQuadA", runtimeBeamTransform);
      runtimeBeamQuadBTransform = CreateRuntimeQuad("RuntimePenlightBeamQuadB", runtimeBeamTransform);

      if (runtimeBeamQuadATransform != null)
      {
        runtimeBeamQuadARenderer = runtimeBeamQuadATransform.GetComponent<MeshRenderer>();
      }

      if (runtimeBeamQuadBTransform != null)
      {
        runtimeBeamQuadBRenderer = runtimeBeamQuadBTransform.GetComponent<MeshRenderer>();
      }

      if (runtimeBeamMaterial != null)
      {
        if (runtimeBeamQuadARenderer != null)
        {
          runtimeBeamQuadARenderer.sharedMaterial = runtimeBeamMaterial;
        }

        if (runtimeBeamQuadBRenderer != null)
        {
          runtimeBeamQuadBRenderer.sharedMaterial = runtimeBeamMaterial;
        }
      }

      ConfigureRuntimeBeamVisual();
      SetRuntimeBeamVisualEnabled(false);
    }

    private void EnsureRuntimeImpactSpotVisual()
    {
      if (runtimeImpactSpotTransform != null)
      {
        ConfigureRuntimeImpactSpotVisual();
        return;
      }

      GameObject impactObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
      impactObject.name = "RuntimePenlightImpactSpot";
      runtimeImpactSpotTransform = impactObject.transform;
      runtimeImpactSpotTransform.SetParent(transform, false);

      Collider impactCollider = impactObject.GetComponent<Collider>();
      if (impactCollider != null)
      {
        if (Application.isPlaying)
        {
          Destroy(impactCollider);
        }
        else
        {
          DestroyImmediate(impactCollider);
        }
      }

      runtimeImpactSpotRenderer = impactObject.GetComponent<MeshRenderer>();

      if (runtimeImpactSpotMaterial == null)
      {
        Shader spotShader = FindPreferredBeamShader();
        if (spotShader != null)
        {
          runtimeImpactSpotMaterial = new Material(spotShader)
          {
            name = "M_Runtime_PenlightImpactSpot"
          };

          SetupTransparentUnlitMaterial(runtimeImpactSpotMaterial);
        }
      }

      EnsureRuntimeImpactSpotTexture();

      if (runtimeImpactSpotMaterial != null && runtimeImpactSpotTexture != null)
      {
        runtimeImpactSpotMaterial.mainTexture = runtimeImpactSpotTexture;

        if (runtimeImpactSpotMaterial.HasProperty("_BaseMap"))
        {
          runtimeImpactSpotMaterial.SetTexture("_BaseMap", runtimeImpactSpotTexture);
        }
      }

      if (runtimeImpactSpotRenderer != null && runtimeImpactSpotMaterial != null)
      {
        runtimeImpactSpotRenderer.sharedMaterial = runtimeImpactSpotMaterial;
      }

      ConfigureRuntimeImpactSpotVisual();
      SetRuntimeImpactSpotVisualEnabled(false);
    }

    private void ConfigureRuntimeBeamVisual()
    {
      if (runtimeBeamMaterial != null)
      {
        if (runtimeBeamMaterial.HasProperty("_Color"))
        {
          runtimeBeamMaterial.SetColor("_Color", beamColor);
        }

        if (runtimeBeamMaterial.HasProperty("_BaseColor"))
        {
          runtimeBeamMaterial.SetColor("_BaseColor", beamColor);
        }
      }

      ConfigureRuntimeRenderer(runtimeBeamQuadARenderer);
      ConfigureRuntimeRenderer(runtimeBeamQuadBRenderer);
    }

    private void ConfigureRuntimeImpactSpotVisual()
    {
      if (runtimeImpactSpotMaterial != null)
      {
        if (runtimeImpactSpotMaterial.HasProperty("_Color"))
        {
          runtimeImpactSpotMaterial.SetColor("_Color", impactSpotColor);
        }

        if (runtimeImpactSpotMaterial.HasProperty("_BaseColor"))
        {
          runtimeImpactSpotMaterial.SetColor("_BaseColor", impactSpotColor);
        }
      }

      ConfigureRuntimeRenderer(runtimeImpactSpotRenderer);
    }

    private void UpdateRuntimeBeamVisual(Ray cursorRay, bool shouldEmitLight, float hoveredEyeDepth)
    {
      if (!useCursorBeamVisual && !useImpactSpotVisual)
      {
        SetRuntimeBeamVisualEnabled(false);
        SetRuntimeImpactSpotVisualEnabled(false);
        return;
      }

      if (useCursorBeamVisual && runtimeBeamTransform == null)
      {
        EnsureRuntimeBeamVisual();
      }

      if (useImpactSpotVisual && runtimeImpactSpotTransform == null)
      {
        EnsureRuntimeImpactSpotVisual();
      }

      if (runtimeBeamTransform == null && runtimeImpactSpotTransform == null)
      {
        return;
      }

      if (!shouldEmitLight)
      {
        SetRuntimeBeamVisualEnabled(false);
        SetRuntimeImpactSpotVisualEnabled(false);
        return;
      }

      float maxDistance = Mathf.Max(
        beamStartOffsetMeters + 0.02f,
        Mathf.Min(beamMaxDistanceMeters, maxProbeDistance));

      bool hasWorldHit = Physics.Raycast(
        cursorRay,
        out RaycastHit worldHit,
        maxDistance,
        Physics.DefaultRaycastLayers,
        QueryTriggerInteraction.Ignore);

      float targetBeamDepth = maxDistance;
      Vector3 targetImpactPosition = cursorRay.origin + (cursorRay.direction * maxDistance);
      Vector3 targetImpactNormal = -cursorRay.direction;

      if (hasWorldHit)
      {
        targetBeamDepth = Mathf.Max(worldHit.distance, beamStartOffsetMeters + 0.02f);
        targetImpactPosition = worldHit.point;
        targetImpactNormal = worldHit.normal;
      }
      else if (hoveredEyeDepth > 0f)
      {
        targetBeamDepth = Mathf.Clamp(hoveredEyeDepth, beamStartOffsetMeters + 0.02f, maxDistance);
        targetImpactPosition = cursorRay.origin + (cursorRay.direction * targetBeamDepth);
      }

      if (smoothedBeamDistanceMeters <= 0f || beamLengthSmoothing <= 0f || !Application.isPlaying)
      {
        smoothedBeamDistanceMeters = targetBeamDepth;
      }
      else
      {
        float smoothingSpeed = Mathf.Lerp(6f, 30f, beamLengthSmoothing);
        float smoothingLerp = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
        smoothedBeamDistanceMeters = Mathf.Lerp(smoothedBeamDistanceMeters, targetBeamDepth, smoothingLerp);
      }

      float clampedBeamDepth = Mathf.Clamp(smoothedBeamDistanceMeters, beamStartOffsetMeters + 0.02f, maxDistance);
      float beamLength = Mathf.Max(0.02f, clampedBeamDepth - beamStartOffsetMeters);
      float depthRatio = Mathf.InverseLerp(beamStartOffsetMeters, maxDistance, clampedBeamDepth);

      float angleWidth = 2f * Mathf.Tan(spotLightAngleDegrees * Mathf.Deg2Rad * 0.5f) * clampedBeamDepth;
      float serializedWidth = Mathf.Lerp(beamStartWidthMeters, beamEndWidthMeters, depthRatio);
      float beamWidth = Mathf.Max(serializedWidth, angleWidth * 0.35f);

      if (useCursorBeamVisual && runtimeBeamTransform != null)
      {
        runtimeBeamTransform.position = cursorRay.origin + (cursorRay.direction * beamStartOffsetMeters);
        runtimeBeamTransform.rotation = Quaternion.LookRotation(cursorRay.direction, Vector3.up);

        // Two crossed quads keep the beam silhouette stable as the camera moves.
        ConfigureBeamQuadTransform(
          runtimeBeamQuadATransform,
          Quaternion.Euler(-90f, 0f, 0f),
          beamLength,
          beamWidth);

        ConfigureBeamQuadTransform(
          runtimeBeamQuadBTransform,
          Quaternion.Euler(-90f, 0f, 90f),
          beamLength,
          beamWidth);

        SetRuntimeBeamVisualEnabled(true);
      }
      else
      {
        SetRuntimeBeamVisualEnabled(false);
      }

      if (useImpactSpotVisual && runtimeImpactSpotTransform != null)
      {
        Vector3 up = Mathf.Abs(Vector3.Dot(targetImpactNormal, Vector3.up)) > 0.97f
          ? Vector3.right
          : Vector3.up;

        runtimeImpactSpotTransform.position = targetImpactPosition + (targetImpactNormal * impactSpotSurfaceOffsetMeters);
        runtimeImpactSpotTransform.rotation = Quaternion.LookRotation(targetImpactNormal, up);

        float spotDiameter = impactSpotRadiusMeters * 2f;
        runtimeImpactSpotTransform.localScale = new Vector3(spotDiameter, spotDiameter, 1f);

        SetRuntimeImpactSpotVisualEnabled(true);
      }
      else
      {
        SetRuntimeImpactSpotVisualEnabled(false);
      }
    }

    private static void ConfigureRuntimeRenderer(Renderer renderer)
    {
      if (renderer == null)
      {
        return;
      }

      renderer.shadowCastingMode = ShadowCastingMode.Off;
      renderer.receiveShadows = false;
      renderer.lightProbeUsage = LightProbeUsage.Off;
      renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static void ConfigureBeamQuadTransform(
      Transform quadTransform,
      Quaternion localRotation,
      float beamLength,
      float beamWidth)
    {
      if (quadTransform == null)
      {
        return;
      }

      quadTransform.localRotation = localRotation;
      quadTransform.localPosition = new Vector3(0f, 0f, beamLength * 0.5f);
      quadTransform.localScale = new Vector3(beamWidth, beamLength, 1f);
    }

    private void SetRuntimeBeamVisualEnabled(bool enabled)
    {
      if (runtimeBeamQuadARenderer != null)
      {
        runtimeBeamQuadARenderer.enabled = enabled;
      }

      if (runtimeBeamQuadBRenderer != null)
      {
        runtimeBeamQuadBRenderer.enabled = enabled;
      }
    }

    private void SetRuntimeImpactSpotVisualEnabled(bool enabled)
    {
      if (runtimeImpactSpotRenderer == null)
      {
        return;
      }

      runtimeImpactSpotRenderer.enabled = enabled;
    }

    private void EnsureRuntimeBeamTexture()
    {
      if (runtimeBeamTexture != null)
      {
        return;
      }

      runtimeBeamTexture = CreateBeamTexture();
    }

    private void EnsureRuntimeImpactSpotTexture()
    {
      if (runtimeImpactSpotTexture != null)
      {
        return;
      }

      runtimeImpactSpotTexture = CreateImpactSpotTexture();
    }

    private static Texture2D CreateBeamTexture()
    {
      const int width = 64;
      const int height = 256;

      Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
      {
        name = "T_Runtime_PenlightBeam"
      };

      texture.wrapMode = TextureWrapMode.Clamp;
      texture.filterMode = FilterMode.Bilinear;

      for (int y = 0; y < height; y++)
      {
        float v = y / (height - 1f);
        float nearFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.03f, 0.22f, v));
        float farFade = Mathf.Pow(1f - v, 1.1f);

        for (int x = 0; x < width; x++)
        {
          float u = Mathf.Abs(((x / (width - 1f)) * 2f) - 1f);
          float radial = Mathf.Pow(Mathf.Clamp01(1f - u), 1.8f);
          float alpha = radial * nearFade * farFade;
          texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
      }

      texture.Apply(false, true);
      return texture;
    }

    private static Texture2D CreateImpactSpotTexture()
    {
      const int size = 128;

      Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
      {
        name = "T_Runtime_PenlightImpactSpot"
      };

      texture.wrapMode = TextureWrapMode.Clamp;
      texture.filterMode = FilterMode.Bilinear;

      float center = (size - 1f) * 0.5f;
      float invRadius = 1f / center;

      for (int y = 0; y < size; y++)
      {
        for (int x = 0; x < size; x++)
        {
          float dx = (x - center) * invRadius;
          float dy = (y - center) * invRadius;
          float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
          float clamped = Mathf.Clamp01(1f - distance);

          float core = Mathf.Pow(clamped, 2.4f);
          float halo = Mathf.Pow(
            Mathf.Clamp01(1f - (Mathf.Abs(distance - 0.58f) / 0.42f)),
            2f) * 0.24f;

          float alpha = Mathf.Clamp01(core + halo);
          texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
      }

      texture.Apply(false, true);
      return texture;
    }

    private static void SetupTransparentUnlitMaterial(Material material)
    {
      if (material == null)
      {
        return;
      }

      if (material.HasProperty("_Mode"))
      {
        material.SetFloat("_Mode", 3f);
      }

      if (material.HasProperty("_Surface"))
      {
        material.SetFloat("_Surface", 1f);
      }

      if (material.HasProperty("_Blend"))
      {
        material.SetFloat("_Blend", 0f);
      }

      if (material.HasProperty("_ZWrite"))
      {
        material.SetFloat("_ZWrite", 0f);
      }

      if (material.HasProperty("_SrcBlend"))
      {
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      }

      if (material.HasProperty("_DstBlend"))
      {
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      }

      material.DisableKeyword("_ALPHATEST_ON");
      material.EnableKeyword("_ALPHABLEND_ON");
      material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
      material.renderQueue = (int)RenderQueue.Transparent;
    }

    private Transform CreateRuntimeQuad(string objectName, Transform parent)
    {
      GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
      quadObject.name = objectName;
      quadObject.transform.SetParent(parent, false);

      Collider quadCollider = quadObject.GetComponent<Collider>();
      if (quadCollider != null)
      {
        if (Application.isPlaying)
        {
          Destroy(quadCollider);
        }
        else
        {
          DestroyImmediate(quadCollider);
        }
      }

      return quadObject.transform;
    }

    private static Shader FindPreferredBeamShader()
    {
      RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
      if (pipeline != null)
      {
        string pipelineType = pipeline.GetType().Name;

        if (pipelineType.Contains("HDRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "HDRP/Unlit",
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default",
            "HDRP/Lit",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Standard");
        }

        if (pipelineType.Contains("UniversalRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default",
            "Unlit/Color",
            "Standard");
        }
      }

      return FindFirstSupportedShader(
        "Particles/Standard Unlit",
        "Legacy Shaders/Particles/Additive",
        "Sprites/Default",
        "Unlit/Texture",
        "Unlit/Color",
        "Standard");
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

    private void EnsureReticleTexture()
    {
      if (reticleTexture != null)
      {
        return;
      }

      const int textureSize = 64;
      Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
      texture.name = "T_Runtime_PenlightReticle";
      texture.wrapMode = TextureWrapMode.Clamp;
      texture.filterMode = FilterMode.Bilinear;

      Color clear = new Color(0f, 0f, 0f, 0f);
      float center = (textureSize - 1f) * 0.5f;
      float outerRadius = textureSize * 0.46f;
      float innerRadius = outerRadius - 3.25f;

      for (int y = 0; y < textureSize; y++)
      {
        for (int x = 0; x < textureSize; x++)
        {
          float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
          bool isRing = distance >= innerRadius && distance <= outerRadius;
          texture.SetPixel(x, y, isRing ? Color.white : clear);
        }
      }

      texture.Apply(false, true);
      reticleTexture = texture;
    }
  }
}
