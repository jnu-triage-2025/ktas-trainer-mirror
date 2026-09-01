using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace TriageTrainer.Tests.PupilReflexSandbox
{
  /// <summary>
  /// 마우스 커서를 펜라이트 광원으로 사용하여 각 대상 눈의 직접광 영향을 샘플링한다.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilReflexMouseProbe : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform faceRoot;
    [SerializeField] private Collider explicitFaceCollider;
    [SerializeField] private PupilReflexEye[] targetEyes = Array.Empty<PupilReflexEye>();

    [Header("Probe")]
    [SerializeField] private bool probeEnabled = true;
    [SerializeField] private bool requirePrimaryButtonHold;
    [SerializeField, Min(0.5f)] private float probeRadiusMillimeters = 10f;
    [SerializeField, Min(0.5f)] private float fullIntensityRadiusMillimeters = 6f;
    [SerializeField, Range(1f, 100f)] private float lightIntensityPercent = 100f;
    [SerializeField, Min(0.2f)] private float maxDistanceMeters = 4f;

    [Header("Visual")]
    [SerializeField] private Color amberFillColor = new Color(1f, 0.72f, 0.26f, 0.34f);
    [SerializeField] private Color amberBorderColor = new Color(1f, 0.82f, 0.35f, 0.92f);
    [SerializeField, Min(0f)] private float impactSurfaceOffsetMillimeters = 0.35f;
    [SerializeField, Range(64, 512)] private int impactTextureSize = 256;
    [SerializeField, Range(1f, 2.5f)] private float corneaIor = 1.376f;
    [SerializeField, Range(0f, 1f)] private float baseSpecular = 0.35f;
    [SerializeField, Range(0f, 4f)] private float stretchStrength = 1.6f;
    [SerializeField, Range(0.01f, 1f)] private float edgeSoftness = 0.25f;

    [Header("Raycast")]
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private bool autoDiscoverEyes;

    private readonly List<PupilReflexEye> eyes = new List<PupilReflexEye>();

    private Transform impactVisualTransform;
    private MeshRenderer impactVisualRenderer;
    private Material impactMaterial;
    private Texture2D impactTexture;

    public PupilReflexEye CurrentPrimaryEye { get; private set; }
    public bool ProbeEnabled
    {
      get => probeEnabled;
      set
      {
        probeEnabled = value;
        if (!probeEnabled)
        {
          CurrentPrimaryEye = null;
          ClearEyeStimulus();
          SetImpactVisible(false);
        }
      }
    }
    public float ProbeRadiusMillimeters => probeRadiusMillimeters;
    public float LightIntensityPercent => lightIntensityPercent;
    public bool HasTargetCamera => targetCamera != null;
    public bool HasFaceSurface => faceRoot != null || explicitFaceCollider != null;
    public bool HasFaceCollider => explicitFaceCollider != null;
    public bool HasTargetEyes => targetEyes != null && targetEyes.Length > 0;

    private void Awake()
    {
      ClampConfig();
      ResolveCamera();
      RebuildEyeCache();
      EnsureImpactVisual();
    }

    private void OnEnable()
    {
      EnsureImpactVisual();
      SetImpactVisible(false);
    }

    private void OnDisable()
    {
      CurrentPrimaryEye = null;
      ClearEyeStimulus();
      SetImpactVisible(false);
    }

    private void OnDestroy()
    {
      if (impactTexture != null)
      {
        if (Application.isPlaying)
        {
          Destroy(impactTexture);
        }
        else
        {
          DestroyImmediate(impactTexture);
        }

        impactTexture = null;
      }

      if (impactMaterial != null)
      {
        if (Application.isPlaying)
        {
          Destroy(impactMaterial);
        }
        else
        {
          DestroyImmediate(impactMaterial);
        }

        impactMaterial = null;
      }
    }

    private void OnValidate()
    {
      ClampConfig();

      if (!isActiveAndEnabled)
      {
        return;
      }

      ResolveCamera();
      if (!Application.isPlaying)
      {
        RebuildEyeCache();
      }

      EnsureImpactVisual();
      RefreshImpactTexture();
      RefreshImpactMaterial();
      UpdateImpactScale();
    }

    private void Update()
    {
      ResolveCamera();
      if (targetCamera == null)
      {
        CurrentPrimaryEye = null;
        ClearEyeStimulus();
        SetImpactVisible(false);
        return;
      }

      if (autoDiscoverEyes && eyes.Count == 0)
      {
        RebuildEyeCache();
      }

      bool shouldEmit = probeEnabled && (!requirePrimaryButtonHold || Input.GetMouseButton(0));
      if (!shouldEmit)
      {
        CurrentPrimaryEye = null;
        ClearEyeStimulus();
        SetImpactVisible(false);
        return;
      }

      Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
      Vector3 axisDirection = targetCamera.transform.forward;

      float outerRadiusMeters = probeRadiusMillimeters * 0.001f;
      float innerRadiusMeters = Mathf.Min(fullIntensityRadiusMillimeters, probeRadiusMillimeters) * 0.001f;
      float intensity01 = Mathf.Clamp01(lightIntensityPercent * 0.01f);

      CurrentPrimaryEye = null;
      PupilReflexEye bestImpactEye = null;
      Vector3 bestImpactPoint = Vector3.zero;
      Vector3 bestImpactNormal = Vector3.forward;
      float bestImpactDistance = float.MaxValue;
      float maxDistance = GetEffectiveMaxDistance();
      Vector3 cameraPosition = targetCamera.transform.position;
      float bestStimulus = 0f;

      for (int i = 0; i < eyes.Count; i++)
      {
        PupilReflexEye eye = eyes[i];
        if (eye == null)
        {
          continue;
        }

        float distanceToEye = Vector3.Distance(cameraPosition, eye.PupilCenterWorld);
        if (distanceToEye > maxDistance)
        {
          eye.SetDirectLightLevel(0f);
          continue;
        }

        Plane axisPlane = new Plane(axisDirection, eye.PupilCenterWorld);
        if (!axisPlane.Raycast(ray, out float axisEnter) || axisEnter < 0f)
        {
          eye.SetDirectLightLevel(0f);
          continue;
        }

        Vector3 axisPoint = ray.GetPoint(axisEnter);
        Ray axisRay = new Ray(axisPoint, axisDirection);

        if (eye.TryRaycastEyeball(axisRay, out Vector3 impactPoint, out Vector3 impactNormal))
        {
          float impactDistance = Vector3.Distance(cameraPosition, impactPoint);
          if (impactDistance < bestImpactDistance)
          {
            bestImpactDistance = impactDistance;
            bestImpactEye = eye;
            bestImpactPoint = impactPoint;
            bestImpactNormal = impactNormal;
          }
        }

        if (!eye.TrySampleLightFromAxis(
          axisPoint,
          axisDirection,
          outerRadiusMeters,
          innerRadiusMeters,
          intensity01,
          out float stimulus,
          out _))
        {
          eye.SetDirectLightLevel(0f);
          continue;
        }

        eye.SetDirectLightLevel(stimulus);
        if (stimulus > bestStimulus)
        {
          bestStimulus = stimulus;
          CurrentPrimaryEye = eye;
        }
      }

      if (CurrentPrimaryEye != null)
      {
        Plane primaryPlane = new Plane(axisDirection, CurrentPrimaryEye.PupilCenterWorld);
        if (primaryPlane.Raycast(ray, out float primaryEnter) && primaryEnter >= 0f)
        {
          Vector3 primaryAxisPoint = ray.GetPoint(primaryEnter);
          Ray primaryAxisRay = new Ray(primaryAxisPoint, axisDirection);

          UpdateImpactVisual(CurrentPrimaryEye, primaryAxisPoint, axisDirection);
          return;
        }
      }

      if (bestImpactEye != null)
      {
        Plane bestPlane = new Plane(axisDirection, bestImpactEye.PupilCenterWorld);
        if (bestPlane.Raycast(ray, out float bestEnter) && bestEnter >= 0f)
        {
          Vector3 bestAxisPoint = ray.GetPoint(bestEnter);
          UpdateImpactVisual(bestImpactEye, bestAxisPoint, axisDirection);
        }
        else
        {
          SetImpactVisible(false);
        }
      }
      else
      {
        SetImpactVisible(false);
      }
    }

    public void SetCamera(Camera camera)
    {
      targetCamera = camera;
    }

    public void SetProbeEnabled(bool enabled)
    {
      ProbeEnabled = enabled;
    }

    public void SetFaceSurface(Transform root, Collider faceCollider)
    {
      faceRoot = root;
      explicitFaceCollider = faceCollider;
    }

    public void SetRequirePrimaryButtonHold(bool requireHold)
    {
      requirePrimaryButtonHold = requireHold;
    }

    public void SetTargetEyes(PupilReflexEye[] eyesToTrack)
    {
      targetEyes = eyesToTrack ?? Array.Empty<PupilReflexEye>();
      RebuildEyeCache();
    }

    public void ConfigureProbe(float radiusMm, float fullIntensityRadiusMm, float intensityPercent)
    {
      probeRadiusMillimeters = radiusMm;
      fullIntensityRadiusMillimeters = fullIntensityRadiusMm;
      lightIntensityPercent = intensityPercent;
      ClampConfig();
      UpdateImpactScale();
    }

    private void ResolveCamera()
    {
      if (targetCamera == null)
      {
        targetCamera = Camera.main;
      }

      if (targetCamera == null)
      {
        targetCamera = FindFirstObjectByType<Camera>();
      }
    }

    private void RebuildEyeCache()
    {
      eyes.Clear();

      if (targetEyes != null)
      {
        for (int i = 0; i < targetEyes.Length; i++)
        {
          PupilReflexEye eye = targetEyes[i];
          if (eye != null && !eyes.Contains(eye))
          {
            eyes.Add(eye);
          }
        }
      }

      if (autoDiscoverEyes)
      {
        PupilReflexEye[] discovered = FindObjectsByType<PupilReflexEye>(FindObjectsSortMode.None);
        for (int i = 0; i < discovered.Length; i++)
        {
          PupilReflexEye eye = discovered[i];
          if (eye != null && !eyes.Contains(eye))
          {
            eyes.Add(eye);
          }
        }
      }
    }

    private void ClearEyeStimulus()
    {
      for (int i = 0; i < eyes.Count; i++)
      {
        PupilReflexEye eye = eyes[i];
        if (eye != null)
        {
          eye.SetDirectLightLevel(0f);
        }
      }
    }

    private bool TryGetFaceHit(Ray ray, out RaycastHit hit)
    {
      float maxDistance = GetEffectiveMaxDistance();
      if (explicitFaceCollider != null)
      {
        if (explicitFaceCollider.Raycast(ray, out hit, maxDistance))
        {
          return true;
        }
      }

      if (Physics.Raycast(ray, out hit, maxDistance, raycastMask, QueryTriggerInteraction.Ignore))
      {
        if (faceRoot == null)
        {
          return true;
        }

        Transform hitTransform = hit.collider != null ? hit.collider.transform : hit.transform;
        if (hitTransform != null && (hitTransform == faceRoot || hitTransform.IsChildOf(faceRoot)))
        {
          return true;
        }
      }

      return false;
    }

    private bool HasUsableFaceCollider()
    {
      if (explicitFaceCollider != null)
      {
        return true;
      }

      if (faceRoot == null)
      {
        return false;
      }

      return faceRoot.GetComponentInChildren<Collider>() != null;
    }

    private float GetEffectiveMaxDistance()
    {
      float effective = maxDistanceMeters;
      if (targetCamera != null && faceRoot != null)
      {
        float distanceToFace = Vector3.Distance(targetCamera.transform.position, faceRoot.position);
        effective = Mathf.Max(effective, distanceToFace + 0.5f);
      }

      return effective;
    }

    private void EnsureImpactVisual()
    {
      if (impactVisualTransform == null)
      {
        Transform existing = transform.Find("ProbeImpactVisual");
        if (existing != null)
        {
          impactVisualTransform = existing;
          impactVisualRenderer = existing.GetComponent<MeshRenderer>();
        }
      }

      if (impactVisualTransform == null)
      {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ProbeImpactVisual";
        impactVisualTransform = quad.transform;
        impactVisualTransform.SetParent(transform, false);

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null && Application.isPlaying)
        {
          Destroy(collider);
        }

        impactVisualRenderer = quad.GetComponent<MeshRenderer>();
      }

      if (impactVisualRenderer == null)
      {
        impactVisualRenderer = impactVisualTransform.GetComponent<MeshRenderer>();
      }

      if (impactMaterial == null)
      {
        impactMaterial = CreateTransparentMaterial("M_Runtime_PupilProbeImpact");
      }

      if (impactVisualRenderer != null)
      {
        impactVisualRenderer.sharedMaterial = impactMaterial;
        impactVisualRenderer.shadowCastingMode = ShadowCastingMode.Off;
        impactVisualRenderer.receiveShadows = false;
      }

      RefreshImpactTexture();
      RefreshImpactMaterial();
      UpdateImpactScale();
      SetImpactVisible(false);
    }

    private void RefreshImpactTexture()
    {
      if (impactTexture != null && impactTexture.width == impactTextureSize)
      {
        return;
      }

      if (impactTexture != null)
      {
        if (Application.isPlaying)
        {
          Destroy(impactTexture);
        }
        else
        {
          DestroyImmediate(impactTexture);
        }

        impactTexture = null;
      }

      int size = Mathf.Clamp(impactTextureSize, 64, 512);
      impactTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
      impactTexture.name = "T_Runtime_PupilProbeImpact";
      impactTexture.wrapMode = TextureWrapMode.Clamp;
      impactTexture.filterMode = FilterMode.Bilinear;

      float borderStart = 0.94f;
      float innerZone = Mathf.Clamp01(fullIntensityRadiusMillimeters / Mathf.Max(0.0001f, probeRadiusMillimeters));

      for (int y = 0; y < size; y++)
      {
        for (int x = 0; x < size; x++)
        {
          float nx = ((x + 0.5f) / size) * 2f - 1f;
          float ny = ((y + 0.5f) / size) * 2f - 1f;
          float radius01 = Mathf.Sqrt((nx * nx) + (ny * ny));

          Color pixel = Color.clear;
          if (radius01 <= 1f)
          {
            float zoneStrength = radius01 <= innerZone ? 1f : 0.5f;
            Color fill = amberFillColor;
            fill.a *= zoneStrength;

            float feather = Mathf.Clamp01((1f - radius01) / 0.15f);
            fill.a *= Mathf.Lerp(0.35f, 1f, feather);
            pixel = fill;

            if (radius01 >= borderStart)
            {
              float borderT = Mathf.InverseLerp(borderStart, 1f, radius01);
              Color border = Color.Lerp(amberBorderColor, Color.clear, borderT);
              pixel = Color.Lerp(pixel, border, 0.95f);
            }
          }

          impactTexture.SetPixel(x, y, pixel);
        }
      }

      impactTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
    }

    private void RefreshImpactMaterial()
    {
      if (impactMaterial == null)
      {
        return;
      }

      if (impactMaterial.HasProperty("_BaseMap"))
      {
        impactMaterial.SetTexture("_BaseMap", impactTexture);
      }

      if (impactMaterial.HasProperty("_MainTex"))
      {
        impactMaterial.SetTexture("_MainTex", impactTexture);
      }

      if (impactMaterial.HasProperty("_BaseColor"))
      {
        impactMaterial.SetColor("_BaseColor", Color.white);
      }

      if (impactMaterial.HasProperty("_Color"))
      {
        impactMaterial.SetColor("_Color", Color.white);
      }
    }

    private void UpdateImpactVisual(PupilReflexEye eye, Vector3 axisPoint, Vector3 axisDirection)
    {
      if (impactVisualTransform == null)
      {
        return;
      }

      Vector3 axisForward = axisDirection.sqrMagnitude > 0.00001f ? axisDirection.normalized : Vector3.forward;
      Vector3 axisUp = targetCamera != null ? targetCamera.transform.up : Vector3.up;

      float surfaceOffset = impactSurfaceOffsetMillimeters * 0.001f;
      if (eye != null)
      {
        surfaceOffset += eye.EyeballDiameterMeters * 0.5f;
      }

      impactVisualTransform.position = axisPoint - axisForward * surfaceOffset;
      impactVisualTransform.rotation = Quaternion.LookRotation(axisForward, axisUp);

      float diameterMeters = Mathf.Max(0.001f, probeRadiusMillimeters * 0.002f);
      UpdateImpactScale(diameterMeters);
      UpdateImpactMaterial(axisPoint, axisDirection, diameterMeters);
      SetImpactVisible(true);
    }

    private void UpdateImpactScale()
    {
      UpdateImpactScale(Mathf.Max(0.001f, probeRadiusMillimeters * 0.002f));
    }

    private void UpdateImpactScale(float diameterMeters)
    {
      if (impactVisualTransform == null)
      {
        return;
      }

      float clampedDiameter = Mathf.Max(0.001f, diameterMeters);
      impactVisualTransform.localScale = new Vector3(clampedDiameter, clampedDiameter, 1f);
    }

    private void SetImpactVisible(bool visible)
    {
      if (impactVisualRenderer != null)
      {
        impactVisualRenderer.enabled = visible;
      }
    }

    private void UpdateImpactMaterial(Vector3 center, Vector3 axisDirection, float diameterMeters)
    {
      if (impactMaterial == null)
      {
        return;
      }

      if (impactMaterial.HasProperty("_ProjectorCenterWS"))
      {
        impactMaterial.SetVector("_ProjectorCenterWS", center);
      }

      if (impactMaterial.HasProperty("_AxisDirectionWS"))
      {
        impactMaterial.SetVector("_AxisDirectionWS", axisDirection.normalized);
      }

      if (impactMaterial.HasProperty("_LightPosWS"))
      {
        Vector3 lightPos = targetCamera != null ? targetCamera.transform.position : center;
        impactMaterial.SetVector("_LightPosWS", lightPos);
      }

      if (impactMaterial.HasProperty("_ProjectorRadius"))
      {
        impactMaterial.SetFloat("_ProjectorRadius", Mathf.Max(0.0001f, diameterMeters * 0.5f));
      }

      if (impactMaterial.HasProperty("_CorneaIOR"))
      {
        impactMaterial.SetFloat("_CorneaIOR", corneaIor);
      }

      if (impactMaterial.HasProperty("_BaseSpecular"))
      {
        impactMaterial.SetFloat("_BaseSpecular", baseSpecular);
      }

      if (impactMaterial.HasProperty("_StretchStrength"))
      {
        impactMaterial.SetFloat("_StretchStrength", stretchStrength);
      }

      if (impactMaterial.HasProperty("_EdgeSoftness"))
      {
        impactMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
      }

      if (impactMaterial.HasProperty("_LightColor"))
      {
        impactMaterial.SetColor("_LightColor", amberFillColor);
      }
    }


    private void ClampConfig()
    {
      probeRadiusMillimeters = Mathf.Max(0.5f, probeRadiusMillimeters);
      fullIntensityRadiusMillimeters = Mathf.Clamp(fullIntensityRadiusMillimeters, 0.5f, probeRadiusMillimeters);
      lightIntensityPercent = Mathf.Clamp(lightIntensityPercent, 1f, 100f);
      maxDistanceMeters = Mathf.Max(0.2f, maxDistanceMeters);
      impactSurfaceOffsetMillimeters = Mathf.Max(0f, impactSurfaceOffsetMillimeters);
      impactTextureSize = Mathf.Clamp(impactTextureSize, 64, 512);
    }

    private static Material CreateTransparentMaterial(string materialName)
    {
      Shader shader = Shader.Find("PupilReflex/CorneaProjector");
      if (shader == null)
      {
        shader = SelectUnlitShader();
      }

      Material material = new Material(shader);
      material.name = materialName;

      if (material.shader != null && material.shader.name == "Standard")
      {
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
      }
      else
      {
        if (material.HasProperty("_Surface"))
        {
          material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SurfaceType"))
        {
          material.SetFloat("_SurfaceType", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
          material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
          material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
          material.SetFloat("_ZWrite", 0f);
        }

        material.renderQueue = (int)RenderQueue.Transparent;
      }

      if (material.HasProperty("_Cull"))
      {
        material.SetFloat("_Cull", 0f);
      }

      if (material.HasProperty("_CullMode"))
      {
        material.SetFloat("_CullMode", 0f);
      }
      return material;
    }

    private static Shader SelectUnlitShader()
    {
      RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
      string pipelineName = pipeline != null ? pipeline.GetType().Name : string.Empty;

      if (pipelineName.Contains("HDRenderPipeline"))
      {
        Shader hdrpShader = Shader.Find("HDRP/Unlit");
        if (hdrpShader != null)
        {
          return hdrpShader;
        }
      }

      if (pipelineName.Contains("UniversalRenderPipeline"))
      {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (urpShader != null)
        {
          return urpShader;
        }
      }

      Shader builtinShader = Shader.Find("Unlit/Transparent");
      if (builtinShader != null)
      {
        return builtinShader;
      }

      Shader spriteShader = Shader.Find("Sprites/Default");
      if (spriteShader != null)
      {
        return spriteShader;
      }

      return Shader.Find("Standard");
    }
  }
}
