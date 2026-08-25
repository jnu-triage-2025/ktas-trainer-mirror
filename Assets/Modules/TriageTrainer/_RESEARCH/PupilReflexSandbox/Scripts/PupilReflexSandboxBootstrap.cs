using UnityEngine;
using UnityEngine.Rendering;

namespace TriageTrainer.Tests.PupilReflexSandbox
{
  /// <summary>
  /// Builds and configures a standalone direct pupil light reflex sandbox.
  /// For production integration, disable auto sandbox creation and assign
  /// anchors from an existing patient prefab.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilReflexSandboxBootstrap : MonoBehaviour
  {
    private const float DefaultMinPupilMillimeters = 2f;
    private const float MinPupilMillimeters = 1f;
    private const float MaxPupilMillimeters = 10f;

    [Header("Bootstrap Mode")]
    [SerializeField] private bool autoCreateSandboxPatient = true;
    [SerializeField] private bool alignPatientTowardCamera = false;
    [SerializeField] private bool autoPositionAnchors = true;
    [SerializeField] private Camera targetCamera;

    [Header("Editor")]
    [SerializeField] private bool persistPlayModeChanges = true;
    [SerializeField] private bool persistPlayModeTransforms;
    [SerializeField] private bool applyTransformUpdatesInPlay;

    [Header("Integration Targets")]
    [SerializeField] private Transform patientRoot;
    [SerializeField] private Transform faceRoot;
    [SerializeField] private Transform eyeRoot;
    [SerializeField] private Collider faceCollider;
    [SerializeField] private Transform leftEyeAnchor;
    [SerializeField] private Transform rightEyeAnchor;

    [Header("Sandbox Placement")]
    [SerializeField] private Vector3 patientWorldPosition = new Vector3(0f, 1.45f, 1.2f);
    [SerializeField, Min(10f)] private float faceWidthCentimeters = 26f;
    [SerializeField, Min(10f)] private float faceHeightCentimeters = 18f;
    [SerializeField] private Color faceColor = new Color(0.88f, 0.73f, 0.62f, 1f);

    [Header("Clinical Anatomy")]
    [SerializeField, Min(1f)] private float eyeSpacingCentimeters = 4f;
    [SerializeField, Min(0.5f)] private float eyeballDiameterCentimeters = 2.5f;
    [SerializeField, Min(1f)] private float irisDiameterMillimeters = 12f;
    [SerializeField, Range(MinPupilMillimeters, MaxPupilMillimeters)] private float pupilBeforeLightMillimeters = 5f;
    [SerializeField, Range(MinPupilMillimeters, MaxPupilMillimeters)] private float pupilAfterLightMillimeters = DefaultMinPupilMillimeters;

    [Header("Clinical Timing")]
    [SerializeField, Min(0.01f)] private float constrictionDurationSeconds = 0.15f;
    [SerializeField, Min(0.01f)] private float dilationDurationSeconds = 0.28f;
    [SerializeField, Range(0.01f, 0.99f)] private float constrictionExponentialB = 0.6f;

    [Header("Pathology")]
    [SerializeField] private bool leftEyeReactsToLight = true;
    [SerializeField] private bool rightEyeReactsToLight = true;
    [SerializeField, Range(MinPupilMillimeters, MaxPupilMillimeters)] private float leftEyeMinPupilMillimeters = DefaultMinPupilMillimeters;
    [SerializeField, Range(MinPupilMillimeters, MaxPupilMillimeters)] private float rightEyeMinPupilMillimeters = DefaultMinPupilMillimeters;
    [SerializeField, Range(0f, 1f)] private float leftEyeConstrictionStrength = 1f;
    [SerializeField, Range(0f, 1f)] private float rightEyeConstrictionStrength = 1f;

    [Header("Penlight")]
    [SerializeField] private bool requireMouseButtonHold;
    [SerializeField, Min(0.5f)] private float probeRadiusMillimeters = 10f;
    [SerializeField, Min(0.5f)] private float fullIntensityRadiusMillimeters = 6f;
    [SerializeField, Range(1f, 100f)] private float lightIntensityPercent = 100f;

    [Header("Runtime Components")]
    [SerializeField] private PupilReflexEye leftEye;
    [SerializeField] private PupilReflexEye rightEye;
    [SerializeField] private PupilReflexMouseProbe mouseProbe;
    [SerializeField] private PupilReflexExamController examController;

    private Material faceMaterial;

    public bool PersistPlayModeChanges => persistPlayModeChanges;
    public bool PersistPlayModeTransforms => persistPlayModeTransforms;
    private bool ShouldUpdateTransforms => !Application.isPlaying || applyTransformUpdatesInPlay;

    private void Awake()
    {
      ClampConfig();
      ResolveCamera();

      if (autoCreateSandboxPatient)
      {
        EnsureSandboxPatient();
      }

      EnsureEyes();
      EnsureRuntimeControllers();
      if (alignPatientTowardCamera && ShouldUpdateTransforms)
      {
        AlignPatientToCamera();
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
      if (!Application.isPlaying && autoCreateSandboxPatient)
      {
        EnsureSandboxPatient();
      }

      EnsureEyes();
      EnsureRuntimeControllers();
    }

    private void OnDestroy()
    {
      if (faceMaterial != null)
      {
        if (Application.isPlaying)
        {
          Destroy(faceMaterial);
        }
        else
        {
          DestroyImmediate(faceMaterial);
        }

        faceMaterial = null;
      }
    }

    private void EnsureSandboxPatient()
    {
      bool patientCreated = false;

      if (patientRoot == null)
      {
        Transform existing = transform.Find("PupilReflexPatientRoot");
        if (existing != null)
        {
          patientRoot = existing;
        }
        else
        {
          GameObject root = new GameObject("PupilReflexPatientRoot");
          patientRoot = root.transform;
          patientRoot.SetParent(transform, false);
          patientCreated = true;
        }
      }

      if (patientCreated || ShouldUpdateTransforms)
      {
        patientRoot.position = patientWorldPosition;
      }

      CleanupLegacyFaceProxy();

      EnsureEyeRoot();
      EnsureAnchors();
    }

    private void EnsureEyeRoot()
    {
      if (patientRoot == null)
      {
        return;
      }

      bool eyeRootCreated = false;

      if (eyeRoot == null)
      {
        Transform existing = patientRoot.Find("EyeRoot");
        if (existing != null)
        {
          eyeRoot = existing;
        }
        else
        {
          GameObject root = new GameObject("EyeRoot");
          eyeRoot = root.transform;
          eyeRoot.SetParent(patientRoot, false);
          eyeRootCreated = true;
        }
      }

      if (eyeRootCreated || ShouldUpdateTransforms)
      {
        eyeRoot.localPosition = Vector3.zero;
        eyeRoot.localRotation = Quaternion.identity;
        eyeRoot.localScale = Vector3.one;
      }
    }

    private void CleanupLegacyFaceProxy()
    {
      Transform legacyProxy = null;

      if (faceRoot != null && faceRoot.name == "FaceProxy")
      {
        legacyProxy = faceRoot;
      }
      else if (patientRoot != null)
      {
        legacyProxy = patientRoot.Find("FaceProxy");
      }
      else
      {
        legacyProxy = transform.Find("FaceProxy");
      }

      if (legacyProxy == null)
      {
        return;
      }

      if (faceRoot == legacyProxy)
      {
        faceRoot = null;
      }

      if (faceCollider != null && faceCollider.transform == legacyProxy)
      {
        faceCollider = null;
      }

      if (Application.isPlaying)
      {
        Destroy(legacyProxy.gameObject);
      }
      else
      {
        DestroyImmediate(legacyProxy.gameObject);
      }
    }


    private void EnsureAnchors()
    {
      Transform anchorParent = eyeRoot != null ? eyeRoot : faceRoot;
      if (anchorParent == null)
      {
        return;
      }
      bool leftCreated = false;
      bool rightCreated = false;

      if (leftEyeAnchor == null)
      {
        leftEyeAnchor = GetOrCreateAnchor(anchorParent, "LeftEyeAnchor", out leftCreated);
      }
      else if (autoCreateSandboxPatient && leftEyeAnchor.parent != anchorParent)
      {
        leftEyeAnchor.SetParent(anchorParent, true);
      }

      if (leftCreated)
      {
        ApplyDefaultAnchorRotation(leftEyeAnchor);
      }

      if (rightEyeAnchor == null)
      {
        rightEyeAnchor = GetOrCreateAnchor(anchorParent, "RightEyeAnchor", out rightCreated);
      }
      else if (autoCreateSandboxPatient && rightEyeAnchor.parent != anchorParent)
      {
        rightEyeAnchor.SetParent(anchorParent, true);
      }

      if (rightCreated)
      {
        ApplyDefaultAnchorRotation(rightEyeAnchor);
      }

      if (autoPositionAnchors && (leftCreated || rightCreated || ShouldUpdateTransforms))
      {
        float halfSpacingMeters = eyeSpacingCentimeters * 0.005f;
        float anchorY = faceHeightCentimeters * 0.00075f;
        float anchorZ = 0.0125f;

        leftEyeAnchor.localPosition = new Vector3(-halfSpacingMeters, anchorY, anchorZ);
        rightEyeAnchor.localPosition = new Vector3(halfSpacingMeters, anchorY, anchorZ);
      }
    }

    private void ApplyDefaultAnchorRotation(Transform anchor)
    {
      if (anchor == null)
      {
        return;
      }

      float currentYaw = anchor.localEulerAngles.y;
      if (Mathf.Abs(Mathf.DeltaAngle(currentYaw, 0f)) < 0.1f)
      {
        anchor.localRotation = Quaternion.Euler(0f, 180f, 0f);
      }
    }

    private void EnsureEyes()
    {
      if (leftEyeAnchor != null)
      {
        leftEye = GetOrCreateEye(leftEyeAnchor, "LeftEye", out _);
        ApplyEyeDefaults(leftEye, isLeftEye: true);
      }

      if (rightEyeAnchor != null)
      {
        rightEye = GetOrCreateEye(rightEyeAnchor, "RightEye", out _);
        ApplyEyeDefaults(rightEye, isLeftEye: false);
      }
    }

    private void EnsureRuntimeControllers()
    {
      bool mouseProbeCreated = false;
      bool examControllerCreated = false;

      if (mouseProbe == null)
      {
        mouseProbe = GetComponent<PupilReflexMouseProbe>();
        if (mouseProbe == null)
        {
          mouseProbe = gameObject.AddComponent<PupilReflexMouseProbe>();
          mouseProbeCreated = true;
        }
      }

      if (examController == null)
      {
        examController = GetComponent<PupilReflexExamController>();
        if (examController == null)
        {
          examController = gameObject.AddComponent<PupilReflexExamController>();
          examControllerCreated = true;
        }
      }

      if (mouseProbe != null)
      {
        if (mouseProbeCreated)
        {
          mouseProbe.SetCamera(targetCamera);
          mouseProbe.SetFaceSurface(faceRoot, faceCollider);
          mouseProbe.SetRequirePrimaryButtonHold(requireMouseButtonHold);
          mouseProbe.SetTargetEyes(new[] { leftEye, rightEye });
          mouseProbe.ConfigureProbe(probeRadiusMillimeters, fullIntensityRadiusMillimeters, lightIntensityPercent);
        }
        else
        {
          if (targetCamera != null && !mouseProbe.HasTargetCamera)
          {
            mouseProbe.SetCamera(targetCamera);
          }

          if (!mouseProbe.HasFaceSurface && (faceRoot != null || faceCollider != null))
          {
            mouseProbe.SetFaceSurface(faceRoot, faceCollider);
          }
          else if (!mouseProbe.HasFaceCollider && faceCollider != null)
          {
            mouseProbe.SetFaceSurface(faceRoot, faceCollider);
          }

          if (!mouseProbe.HasTargetEyes && leftEye != null && rightEye != null)
          {
            mouseProbe.SetTargetEyes(new[] { leftEye, rightEye });
          }
        }
      }

      if (examController != null)
      {
        if (examControllerCreated || !examController.HasAssignedEyes)
        {
          examController.ConfigureEyes(leftEye, rightEye);
        }
      }
    }

    private void ApplyEyeDefaults(PupilReflexEye eye, bool isLeftEye)
    {
      if (eye == null)
      {
        return;
      }

      float minPupilMillimeters = isLeftEye
        ? leftEyeMinPupilMillimeters
        : rightEyeMinPupilMillimeters;

      eye.ConfigureAnatomy(
        eyeballDiameterCentimeters,
        irisDiameterMillimeters,
        pupilBeforeLightMillimeters,
        minPupilMillimeters);

      eye.ConfigureReaction(
        isLeftEye ? leftEyeReactsToLight : rightEyeReactsToLight,
        isLeftEye ? leftEyeConstrictionStrength : rightEyeConstrictionStrength,
        constrictionDurationSeconds,
        dilationDurationSeconds,
        constrictionExponentialB);
    }

    private void AlignPatientToCamera()
    {
      if (!alignPatientTowardCamera || patientRoot == null)
      {
        return;
      }

      ResolveCamera();
      if (targetCamera == null)
      {
        return;
      }

      Vector3 toCamera = targetCamera.transform.position - patientRoot.position;
      if (toCamera.sqrMagnitude < 0.0001f)
      {
        return;
      }

      patientRoot.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
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

    private void ClampConfig()
    {
      faceWidthCentimeters = Mathf.Max(10f, faceWidthCentimeters);
      faceHeightCentimeters = Mathf.Max(10f, faceHeightCentimeters);

      eyeSpacingCentimeters = Mathf.Max(1f, eyeSpacingCentimeters);
      eyeballDiameterCentimeters = Mathf.Max(0.5f, eyeballDiameterCentimeters);

      irisDiameterMillimeters = Mathf.Max(1f, irisDiameterMillimeters);
      pupilBeforeLightMillimeters = Mathf.Clamp(pupilBeforeLightMillimeters, MinPupilMillimeters, MaxPupilMillimeters);
      pupilAfterLightMillimeters = Mathf.Clamp(pupilAfterLightMillimeters, MinPupilMillimeters, MaxPupilMillimeters);

      leftEyeMinPupilMillimeters = Mathf.Clamp(leftEyeMinPupilMillimeters, MinPupilMillimeters, MaxPupilMillimeters);
      rightEyeMinPupilMillimeters = Mathf.Clamp(rightEyeMinPupilMillimeters, MinPupilMillimeters, MaxPupilMillimeters);

      if (Mathf.Approximately(leftEyeMinPupilMillimeters, DefaultMinPupilMillimeters) &&
          Mathf.Approximately(rightEyeMinPupilMillimeters, DefaultMinPupilMillimeters) &&
          !Mathf.Approximately(pupilAfterLightMillimeters, DefaultMinPupilMillimeters))
      {
        leftEyeMinPupilMillimeters = pupilAfterLightMillimeters;
        rightEyeMinPupilMillimeters = pupilAfterLightMillimeters;
      }

      if (pupilAfterLightMillimeters > pupilBeforeLightMillimeters)
      {
        pupilAfterLightMillimeters = pupilBeforeLightMillimeters;
      }

      if (leftEyeMinPupilMillimeters > pupilBeforeLightMillimeters)
      {
        leftEyeMinPupilMillimeters = pupilBeforeLightMillimeters;
      }

      if (rightEyeMinPupilMillimeters > pupilBeforeLightMillimeters)
      {
        rightEyeMinPupilMillimeters = pupilBeforeLightMillimeters;
      }

      constrictionDurationSeconds = Mathf.Max(0.01f, constrictionDurationSeconds);
      dilationDurationSeconds = Mathf.Max(0.01f, dilationDurationSeconds);
      constrictionExponentialB = Mathf.Clamp(constrictionExponentialB, 0.01f, 0.99f);

      leftEyeConstrictionStrength = Mathf.Clamp01(leftEyeConstrictionStrength);
      rightEyeConstrictionStrength = Mathf.Clamp01(rightEyeConstrictionStrength);

      probeRadiusMillimeters = Mathf.Max(0.5f, probeRadiusMillimeters);
      fullIntensityRadiusMillimeters = Mathf.Clamp(fullIntensityRadiusMillimeters, 0.5f, probeRadiusMillimeters);
      lightIntensityPercent = Mathf.Clamp(lightIntensityPercent, 1f, 100f);
    }

    private static Transform GetOrCreateAnchor(Transform parent, string anchorName, out bool created)
    {
      Transform anchor = parent.Find(anchorName);
      created = anchor == null;
      if (anchor == null)
      {
        GameObject anchorObject = new GameObject(anchorName);
        anchor = anchorObject.transform;
        anchor.SetParent(parent, false);
      }

      return anchor;
    }

    private static PupilReflexEye GetOrCreateEye(Transform anchor, string eyeName, out bool created)
    {
      Transform eyeTransform = anchor.Find(eyeName);
      created = eyeTransform == null;
      if (eyeTransform == null)
      {
        GameObject eyeObject = new GameObject(eyeName);
        eyeTransform = eyeObject.transform;
        eyeTransform.SetParent(anchor, false);
      }

      eyeTransform.localPosition = Vector3.zero;
      eyeTransform.localRotation = Quaternion.identity;
      eyeTransform.localScale = Vector3.one;

      PupilReflexEye eye = eyeTransform.GetComponent<PupilReflexEye>();
      if (eye == null)
      {
        eye = eyeTransform.gameObject.AddComponent<PupilReflexEye>();
      }

      return eye;
    }

    private static Material CreateOpaqueMaterial(string materialName)
    {
      Shader shader = SelectLitShader();

      Material material = new Material(shader);
      material.name = materialName;

      if (material.shader != null && material.shader.name == "Standard")
      {
        material.SetFloat("_Mode", 0f);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Geometry;
      }
      else
      {
        if (material.HasProperty("_Surface"))
        {
          material.SetFloat("_Surface", 0f);
        }

        if (material.HasProperty("_SurfaceType"))
        {
          material.SetFloat("_SurfaceType", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
          material.SetFloat("_SrcBlend", (float)BlendMode.One);
        }

        if (material.HasProperty("_DstBlend"))
        {
          material.SetFloat("_DstBlend", (float)BlendMode.Zero);
        }

        if (material.HasProperty("_ZWrite"))
        {
          material.SetFloat("_ZWrite", 1f);
        }

        material.renderQueue = (int)RenderQueue.Geometry;
      }
      return material;
    }

    private static Shader SelectLitShader()
    {
      RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
      string pipelineName = pipeline != null ? pipeline.GetType().Name : string.Empty;

      if (pipelineName.Contains("HDRenderPipeline"))
      {
        Shader hdrpShader = Shader.Find("HDRP/Lit");
        if (hdrpShader != null)
        {
          return hdrpShader;
        }
      }

      if (pipelineName.Contains("UniversalRenderPipeline"))
      {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader != null)
        {
          return urpShader;
        }
      }

      return Shader.Find("Standard");
    }
  }
}
