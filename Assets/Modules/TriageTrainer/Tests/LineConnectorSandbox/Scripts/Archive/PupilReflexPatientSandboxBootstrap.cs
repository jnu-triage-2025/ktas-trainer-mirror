using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// 단순 2D 얼굴 + 3D 안구 기반 동공 반사 샌드박스를 자동 구성합니다.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilReflexPatientSandboxBootstrap : MonoBehaviour
  {
    [Serializable]
    public class ExamEvent : UnityEvent
    {
    }

    [Header("Auto Setup")]
    [SerializeField] private bool autoBuildOnAwake = true;
    [SerializeField] private bool ensureCameraIfMissing = true;

    [Header("Face Root")]
    [SerializeField] private Transform faceRoot;
    [SerializeField] private Vector3 faceLocalPosition = new Vector3(0f, 1.45f, 1.25f);
    [SerializeField] private Vector2 faceSize = new Vector2(0.32f, 0.36f);

    [Header("Eye Layout")]
    [SerializeField] private Vector3 leftEyeLocalPosition = new Vector3(-0.05f, 0.02f, -0.01f);
    [SerializeField] private Vector3 rightEyeLocalPosition = new Vector3(0.05f, 0.02f, -0.01f);
    [SerializeField] private bool swapEyeSidePlacement = false;
    [SerializeField] private bool forceEyesInFrontOfFace = true;
    [SerializeField] private bool orientIrisTowardMainCamera = true;
    [SerializeField, Min(0.01f)] private float eyeballDiameterMeters = 0.024f;
    [SerializeField, Range(0.7f, 1f)] private float scleraDiameterScale = 0.86f;
    [SerializeField, Min(0.005f)] private float irisDiameterMeters = 0.011f;
    [SerializeField, Min(0.0001f)] private float irisThicknessMeters = 0.0009f;
    [SerializeField, Min(0f)] private float irisSurfaceOffsetMeters = 0.00003f;

    [Header("Aperture Cavity")]
    [SerializeField, Range(0f, 0.45f)] private float scleraBackwardRatio = 0.32f;
    [SerializeField, Min(0f)] private float pupilChamberDepthMeters = 0.0032f;
    [SerializeField, Min(0.002f)] private float pupilChamberDiameterMeters = 0.010f;
    [SerializeField, Range(12, 128)] private int irisRingSegments = 64;

    [Header("Pupil Placement")]
    [SerializeField] private bool useSceneTunedPupilDefaults = true;
    [SerializeField] private Vector3 leftPupilLocalPosition = new Vector3(-0.0002f, 0.0001f, 0.0054f);
    [SerializeField] private Vector3 rightPupilLocalPosition = new Vector3(-0.0003f, 0.0001f, 0.0054f);

    [Header("Cornea (Transparent)")]
    [SerializeField, Min(1f)] private float corneaScale = 1.04f;
    [SerializeField, Range(0.02f, 0.6f)] private float corneaAlpha = 0.16f;
    [SerializeField, Range(0f, 1f)] private float corneaSmoothness = 0.98f;

    [Header("Pupil Reflex Controls (Intuitive)")]
    [SerializeField] private bool enablePupilChange = true;
    [SerializeField, Min(1f)] private float pupilDiameterBeforeLightMm = 5f;
    [SerializeField, Min(0.5f)] private float pupilDiameterAfterLightMm = 2f;
    [SerializeField, Min(0.01f)] private float constrictionDurationSeconds = 0.15f;
    [SerializeField, Range(0f, 1f)] private float constrictionTaperingSpeed = 0.5f;
    [SerializeField, Min(0.01f)] private float dilationDurationSeconds = 0.28f;

    [Header("Pathology Preset")]
    [SerializeField] private bool leftEyeReactsToLight = true;
    [SerializeField, Range(0f, 1f)] private float leftEyeConstrictionAmount = 1f;
    [SerializeField] private bool rightEyeReactsToLight = true;
    [SerializeField, Range(0f, 1f)] private float rightEyeConstrictionAmount = 1f;

    [Header("Probe")]
    [SerializeField] private PenlightCursorProbe penlightCursorProbe;
    [SerializeField] private bool enableProbeOnStart = true;

    [Header("Completion")]
    [SerializeField] private bool completeWhenBothEyesChecked = true;
    [SerializeField] private bool drawStatusGui = true;
    [SerializeField] private ExamEvent onBothEyesChecked;

    [Header("Runtime Status")]
    [SerializeField] private bool leftEyeChecked;
    [SerializeField] private bool rightEyeChecked;
    [SerializeField] private bool examCompleted;

    private const string FaceRootName = "PupilSandboxFaceRoot";
    private const string FacePlateName = "FacePlate";
    private const string LeftEyeName = "LeftEye";
    private const string RightEyeName = "RightEye";
    private const float StatusGuiStartX = 10f;
    private const float StatusGuiStartY = 10f;
    private const float StatusGuiWidth = 640f;
    private const float StatusGuiHeight = 82f;
    private const float StatusGuiSpacing = 8f;

    private static readonly List<PupilReflexPatientSandboxBootstrap> StatusGuiInstances =
      new List<PupilReflexPatientSandboxBootstrap>();

    private PupilReflexEye leftEye;
    private PupilReflexEye rightEye;

    private Material runtimeFaceMaterial;
    private Material runtimeScleraMaterial;
    private Material runtimeIrisMaterial;
    private Material runtimePupilMaterial;
    private Material runtimeCorneaMaterial;

    public bool ExamCompleted => examCompleted;
    public PupilReflexEye LeftEye => leftEye;
    public PupilReflexEye RightEye => rightEye;

    public event Action BothEyesChecked;

    private void OnEnable()
    {
      RegisterStatusGuiInstance();
    }

    private void OnDisable()
    {
      UnregisterStatusGuiInstance();
    }

    private void Awake()
    {
      if (!autoBuildOnAwake)
      {
        return;
      }

      BuildOrRefreshSandbox();
    }

    private void OnValidate()
    {
      faceSize.x = Mathf.Max(0.05f, faceSize.x);
      faceSize.y = Mathf.Max(0.05f, faceSize.y);
      eyeballDiameterMeters = Mathf.Max(0.01f, eyeballDiameterMeters);
      scleraDiameterScale = Mathf.Clamp(scleraDiameterScale, 0.7f, 1f);
      irisDiameterMeters = Mathf.Clamp(irisDiameterMeters, 0.005f, eyeballDiameterMeters * 0.95f);
      irisThicknessMeters = Mathf.Clamp(irisThicknessMeters, 0.0001f, irisDiameterMeters * 0.5f);
      irisSurfaceOffsetMeters = Mathf.Max(0f, irisSurfaceOffsetMeters);
      scleraBackwardRatio = Mathf.Clamp(scleraBackwardRatio, 0f, 0.45f);
      pupilChamberDepthMeters = Mathf.Max(0f, pupilChamberDepthMeters);
      pupilChamberDiameterMeters = Mathf.Clamp(pupilChamberDiameterMeters, 0.002f, irisDiameterMeters * 1.2f);
      irisRingSegments = Mathf.Clamp(irisRingSegments, 12, 128);
      corneaScale = Mathf.Max(1f, corneaScale);
      corneaAlpha = Mathf.Clamp(corneaAlpha, 0.02f, 0.6f);
      corneaSmoothness = Mathf.Clamp01(corneaSmoothness);

      pupilDiameterBeforeLightMm = Mathf.Max(1f, pupilDiameterBeforeLightMm);
      pupilDiameterAfterLightMm = Mathf.Clamp(pupilDiameterAfterLightMm, 0.5f, pupilDiameterBeforeLightMm);
      constrictionDurationSeconds = Mathf.Max(0.01f, constrictionDurationSeconds);
      constrictionTaperingSpeed = Mathf.Clamp01(constrictionTaperingSpeed);
      dilationDurationSeconds = Mathf.Max(0.01f, dilationDurationSeconds);
      leftEyeConstrictionAmount = Mathf.Clamp01(leftEyeConstrictionAmount);
      rightEyeConstrictionAmount = Mathf.Clamp01(rightEyeConstrictionAmount);

      // Keep pathology toggles responsive in Play Mode without requiring manual rebuild.
      if (Application.isPlaying)
      {
        ConfigurePathology();
        return;
      }

      if (autoBuildOnAwake)
      {
        BuildOrRefreshSandbox();
      }
      else
      {
        ConfigurePathology();
      }
    }

    private void OnDestroy()
    {
      UnregisterStatusGuiInstance();

      SafeDestroy(runtimeFaceMaterial);
      runtimeFaceMaterial = null;

      SafeDestroy(runtimeScleraMaterial);
      runtimeScleraMaterial = null;

      SafeDestroy(runtimeIrisMaterial);
      runtimeIrisMaterial = null;

      SafeDestroy(runtimePupilMaterial);
      runtimePupilMaterial = null;

      SafeDestroy(runtimeCorneaMaterial);
      runtimeCorneaMaterial = null;
    }

    private void OnGUI()
    {
      if (!drawStatusGui)
      {
        return;
      }

      string line1 = "Pupil Reflex Sandbox";
      string line2 = string.Format(
        "Left: {0} | Right: {1} | Completed: {2}",
        leftEyeChecked ? "Checked" : "Waiting",
        rightEyeChecked ? "Checked" : "Waiting",
        examCompleted ? "Yes" : "No");
      string line3 = string.Format(
        "Pupil(mm) L:{0:0.00} R:{1:0.00}",
        leftEye != null ? leftEye.CurrentPupilDiameterMeters * 1000f : 0f,
        rightEye != null ? rightEye.CurrentPupilDiameterMeters * 1000f : 0f);
      string line4 = "Tip: 커서를 동공 중심에 통과시키면 반응합니다.";

      float statusGuiY = StatusGuiStartY + (GetStatusGuiIndex() * (StatusGuiHeight + StatusGuiSpacing));

      GUI.Label(
        new Rect(StatusGuiStartX, statusGuiY, StatusGuiWidth, StatusGuiHeight),
        line1 + "\n" + line2 + "\n" + line3 + "\n" + line4);
    }

    [ContextMenu("Sandbox/Rebuild")]
    public void BuildOrRefreshSandbox()
    {
      EnsureCameraIfNeeded();
      EnsureRuntimeMaterials();
      EnsureFaceRoot();
      EnsureFacePlate();
      EnsureEyes();
      ConfigurePathology();
      EnsurePenlightProbe();
      ResetExamState();
    }

    public void ConfigurePathology(
      bool leftReacts,
      float leftConstriction,
      bool rightReacts,
      float rightConstriction)
    {
      leftEyeReactsToLight = leftReacts;
      leftEyeConstrictionAmount = Mathf.Clamp01(leftConstriction);

      rightEyeReactsToLight = rightReacts;
      rightEyeConstrictionAmount = Mathf.Clamp01(rightConstriction);

      ConfigurePathology();
    }

    public void SetProbeEnabled(bool enabled)
    {
      if (penlightCursorProbe == null)
      {
        return;
      }

      penlightCursorProbe.SetProbeEnabled(enabled);
    }

    private void EnsureCameraIfNeeded()
    {
      if (!ensureCameraIfMissing)
      {
        return;
      }

      if (Camera.main != null)
      {
        return;
      }

      GameObject cameraObject = new GameObject("PupilSandboxCamera");
      cameraObject.transform.position = new Vector3(0f, faceLocalPosition.y, 0f);
      cameraObject.transform.rotation = Quaternion.identity;

      Camera cameraComponent = cameraObject.AddComponent<Camera>();
      cameraComponent.tag = "MainCamera";
      cameraComponent.nearClipPlane = 0.01f;
      cameraComponent.farClipPlane = 50f;
      cameraComponent.fieldOfView = 50f;

      if (FindObjectOfType<AudioListener>() == null)
      {
        cameraObject.AddComponent<AudioListener>();
      }
    }

    private void EnsureFaceRoot()
    {
      if (faceRoot == null)
      {
        Transform found = transform.Find(FaceRootName);
        if (found != null)
        {
          faceRoot = found;
        }
      }

      if (faceRoot == null)
      {
        GameObject rootObject = new GameObject(FaceRootName);
        faceRoot = rootObject.transform;
        faceRoot.SetParent(transform, false);
      }

      faceRoot.localPosition = faceLocalPosition;
      faceRoot.localRotation = Quaternion.identity;
    }

    private void EnsureFacePlate()
    {
      if (faceRoot == null)
      {
        return;
      }

      Transform plate = faceRoot.Find(FacePlateName);
      if (plate == null)
      {
        GameObject plateObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plateObject.name = FacePlateName;
        plate = plateObject.transform;
        plate.SetParent(faceRoot, false);

        Collider plateCollider = plateObject.GetComponent<Collider>();
        if (plateCollider != null)
        {
          plateCollider.enabled = false;
        }
      }

      Collider plateColliderExisting = plate.GetComponent<Collider>();
      if (plateColliderExisting != null)
      {
        plateColliderExisting.enabled = false;
      }

      plate.localPosition = Vector3.zero;
      plate.localRotation = Quaternion.identity;
      plate.localScale = new Vector3(faceSize.x, faceSize.y, 1f);

      Renderer plateRenderer = plate.GetComponent<Renderer>();
      if (plateRenderer != null)
      {
        plateRenderer.sharedMaterial = runtimeFaceMaterial;
      }
    }

    private void EnsureEyes()
    {
      Vector3 resolvedLeftPosition = leftEyeLocalPosition;
      Vector3 resolvedRightPosition = rightEyeLocalPosition;

      if (swapEyeSidePlacement)
      {
        Vector3 temp = resolvedLeftPosition;
        resolvedLeftPosition = resolvedRightPosition;
        resolvedRightPosition = temp;
      }

      if (forceEyesInFrontOfFace)
      {
        resolvedLeftPosition.z = Mathf.Abs(resolvedLeftPosition.z);
        resolvedRightPosition.z = Mathf.Abs(resolvedRightPosition.z);
      }

      leftEye = EnsureSingleEye(
        eyeSide: PupilReflexEye.EyeSide.Left,
        eyeName: LeftEyeName,
        localPosition: resolvedLeftPosition,
        reactsToLight: leftEyeReactsToLight,
        constrictionAmount: leftEyeConstrictionAmount);

      rightEye = EnsureSingleEye(
        eyeSide: PupilReflexEye.EyeSide.Right,
        eyeName: RightEyeName,
        localPosition: resolvedRightPosition,
        reactsToLight: rightEyeReactsToLight,
        constrictionAmount: rightEyeConstrictionAmount);

      HookEyeEvents();
    }

    private PupilReflexEye EnsureSingleEye(
      PupilReflexEye.EyeSide eyeSide,
      string eyeName,
      Vector3 localPosition,
      bool reactsToLight,
      float constrictionAmount)
    {
      Transform eyeRoot = faceRoot.Find(eyeName);
      if (eyeRoot == null)
      {
        GameObject eyeObject = new GameObject(eyeName);
        eyeRoot = eyeObject.transform;
        eyeRoot.SetParent(faceRoot, false);
      }

      eyeRoot.localPosition = localPosition;

      if (orientIrisTowardMainCamera)
      {
        AlignEyeTowardMainCamera(eyeRoot);
      }
      else
      {
        eyeRoot.localRotation = Quaternion.identity;
      }

      PupilReflexEye eye = eyeRoot.GetComponent<PupilReflexEye>();
      if (eye == null)
      {
        eye = eyeRoot.gameObject.AddComponent<PupilReflexEye>();
      }

      float baselinePupilDiameter = Mathf.Max(0.001f, pupilDiameterBeforeLightMm * 0.001f);
      float corneaOuterRadius = eyeballDiameterMeters * 0.5f;
      float scleraDiameter = eyeballDiameterMeters * scleraDiameterScale;
      float scleraRadius = scleraDiameter * 0.5f;

      bool effectiveEyeReactionEnabled = enablePupilChange && reactsToLight;

      float effectiveScleraBackward = Mathf.Max(0.25f, scleraBackwardRatio);
      float effectiveChamberDepth = Mathf.Max(0.0022f, pupilChamberDepthMeters);
      float effectiveChamberDiameter = Mathf.Max(0.0095f, pupilChamberDiameterMeters);

      float scleraCenterZ = -(corneaOuterRadius * effectiveScleraBackward);
      float scleraFrontZ = scleraCenterZ + scleraRadius;
      float corneaFrontZ = scleraCenterZ + corneaOuterRadius;

      float irisCenterZ = corneaFrontZ + irisSurfaceOffsetMeters + (irisThicknessMeters * 0.5f);

      float pupilChamberCenterZ =
        irisCenterZ -
        (irisThicknessMeters * 0.5f) -
        (effectiveChamberDepth * 0.2f) -
        (effectiveChamberDiameter * 0.5f);

      // Keep a dark pupil surface in front of the white sclera so the center never looks washed out.
      float pupilFrontZ = pupilChamberCenterZ + (effectiveChamberDiameter * 0.5f);
      float minimumPupilFrontZ = scleraFrontZ + 0.0002f;
      if (pupilFrontZ < minimumPupilFrontZ)
      {
        pupilChamberCenterZ += minimumPupilFrontZ - pupilFrontZ;
      }

      Vector3 computedPupilLocalPosition = new Vector3(0f, 0f, pupilChamberCenterZ);
      Vector3 sceneTunedPupilLocalPosition =
        eyeSide == PupilReflexEye.EyeSide.Left
          ? leftPupilLocalPosition
          : rightPupilLocalPosition;
      Vector3 initialPupilLocalPosition =
        useSceneTunedPupilDefaults
          ? sceneTunedPupilLocalPosition
          : computedPupilLocalPosition;

      Transform sclera = EnsureVisualSphere(
        eyeRoot,
        "Sclera",
        Vector3.one * scleraDiameter,
        new Vector3(0f, 0f, scleraCenterZ),
        runtimeScleraMaterial);

      EnsureVisualSphere(
        eyeRoot,
        "Cornea",
        Vector3.one * (eyeballDiameterMeters * corneaScale),
        new Vector3(0f, 0f, scleraCenterZ),
        runtimeCorneaMaterial);

      Transform iris = EnsureIrisRing(
        eyeRoot,
        "Iris",
        irisDiameterMeters,
        irisThicknessMeters,
        baselinePupilDiameter,
        irisRingSegments,
        new Vector3(0f, 0f, irisCenterZ),
        runtimeIrisMaterial);

      Transform pupil = EnsureVisualSphere(
        eyeRoot,
        "Pupil",
        new Vector3(effectiveChamberDiameter, effectiveChamberDiameter, effectiveChamberDiameter),
        initialPupilLocalPosition,
        runtimePupilMaterial,
        preserveExistingLocalPosition: true);

      eye.Initialize(
        eyeSide,
        sclera,
        iris,
        pupil,
        effectiveEyeReactionEnabled,
        constrictionAmount);

      ApplyIntuitivePupilControls(eye, reactsToLight, constrictionAmount);

      return eye;
    }

    private void ApplyIntuitivePupilControls(PupilReflexEye eye, bool reactsToLight, float constrictionAmount)
    {
      if (eye == null)
      {
        return;
      }

      eye.ConfigurePupilSizeRangeMm(pupilDiameterBeforeLightMm, pupilDiameterAfterLightMm);
      eye.ConfigureConstrictionMotion(constrictionDurationSeconds, constrictionTaperingSpeed);
      eye.ConfigureDilationDuration(dilationDurationSeconds);

      bool effectiveEyeReactionEnabled = enablePupilChange && reactsToLight;
      eye.ConfigureResponse(effectiveEyeReactionEnabled, constrictionAmount);
    }

    private void AlignEyeTowardMainCamera(Transform eyeRoot)
    {
      if (eyeRoot == null)
      {
        return;
      }

      Camera referenceCamera = Camera.main;
      if (referenceCamera == null)
      {
        referenceCamera = FindObjectOfType<Camera>();
      }

      if (referenceCamera == null)
      {
        eyeRoot.localRotation = Quaternion.identity;
        return;
      }

      Vector3 directionToCamera = referenceCamera.transform.position - eyeRoot.position;
      if (directionToCamera.sqrMagnitude <= 0.000001f)
      {
        return;
      }

      eyeRoot.rotation = Quaternion.LookRotation(directionToCamera.normalized, Vector3.up);
    }

    private Transform EnsureVisualSphere(
      Transform parent,
      string visualName,
      Vector3 localScale,
      Vector3 localPosition,
      Material material,
      bool preserveExistingLocalPosition = false)
    {
      Transform visual = parent.Find(visualName);
      bool createdVisual = false;
      if (visual == null)
      {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = visualName;
        visual = sphere.transform;
        visual.SetParent(parent, false);
        createdVisual = true;

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
          collider.enabled = false;
        }
      }

      Collider existingCollider = visual.GetComponent<Collider>();
      if (existingCollider != null)
      {
        existingCollider.enabled = false;
      }

      if (!preserveExistingLocalPosition || createdVisual)
      {
        visual.localPosition = localPosition;
      }

      visual.localRotation = Quaternion.identity;
      visual.localScale = new Vector3(
        Mathf.Max(0.0001f, localScale.x),
        Mathf.Max(0.0001f, localScale.y),
        Mathf.Max(0.0001f, localScale.z));

      Renderer renderer = visual.GetComponent<Renderer>();
      if (renderer != null)
      {
        renderer.sharedMaterial = material;
      }

      return visual;
    }

    private Transform EnsureIrisRing(
      Transform parent,
      string visualName,
      float outerDiameterMeters,
      float thicknessMeters,
      float initialPupilDiameterMeters,
      int segments,
      Vector3 localPosition,
      Material material)
    {
      Transform iris = parent.Find(visualName);
      if (iris == null)
      {
        GameObject irisObject = new GameObject(visualName);
        iris = irisObject.transform;
        iris.SetParent(parent, false);
      }

      Collider collider = iris.GetComponent<Collider>();
      if (collider != null)
      {
        collider.enabled = false;
      }

      iris.localPosition = localPosition;
      iris.localRotation = Quaternion.identity;
      iris.localScale = Vector3.one;

      MeshFilter meshFilter = iris.GetComponent<MeshFilter>();
      if (meshFilter == null)
      {
        meshFilter = iris.gameObject.AddComponent<MeshFilter>();
      }

      MeshRenderer meshRenderer = iris.GetComponent<MeshRenderer>();
      if (meshRenderer == null)
      {
        meshRenderer = iris.gameObject.AddComponent<MeshRenderer>();
      }

      meshRenderer.sharedMaterial = material;

      PupilApertureVisual aperture = iris.GetComponent<PupilApertureVisual>();
      if (aperture == null)
      {
        aperture = iris.gameObject.AddComponent<PupilApertureVisual>();
      }

      aperture.Configure(meshFilter, outerDiameterMeters, thicknessMeters, segments);
      aperture.SetPupilDiameter(initialPupilDiameterMeters);

      return iris;
    }

    private void EnsurePenlightProbe()
    {
      if (penlightCursorProbe == null)
      {
        penlightCursorProbe = GetComponent<PenlightCursorProbe>();
      }

      if (penlightCursorProbe == null)
      {
        penlightCursorProbe = gameObject.AddComponent<PenlightCursorProbe>();
      }

      List<PupilReflexEye> eyeList = new List<PupilReflexEye>(2);
      if (leftEye != null)
      {
        eyeList.Add(leftEye);
      }

      if (rightEye != null)
      {
        eyeList.Add(rightEye);
      }

      penlightCursorProbe.SetTargetEyes(eyeList);
      penlightCursorProbe.SetProbeEnabled(enableProbeOnStart);
    }

    private void HookEyeEvents()
    {
      if (leftEye != null)
      {
        leftEye.FirstDirectLightDetected -= HandleEyeDetected;
        leftEye.FirstDirectLightDetected += HandleEyeDetected;
      }

      if (rightEye != null)
      {
        rightEye.FirstDirectLightDetected -= HandleEyeDetected;
        rightEye.FirstDirectLightDetected += HandleEyeDetected;
      }
    }

    private void ConfigurePathology()
    {
      if (leftEye != null)
      {
        ApplyIntuitivePupilControls(leftEye, leftEyeReactsToLight, leftEyeConstrictionAmount);
      }

      if (rightEye != null)
      {
        ApplyIntuitivePupilControls(rightEye, rightEyeReactsToLight, rightEyeConstrictionAmount);
      }
    }

    private void ResetExamState()
    {
      if (leftEye != null)
      {
        leftEye.ResetEyeExamState();
      }

      if (rightEye != null)
      {
        rightEye.ResetEyeExamState();
      }

      leftEyeChecked = false;
      rightEyeChecked = false;
      examCompleted = false;

      EvaluateCompletion();
    }

    private void HandleEyeDetected(PupilReflexEye eye)
    {
      if (eye == null)
      {
        return;
      }

      if (eye.Side == PupilReflexEye.EyeSide.Left)
      {
        leftEyeChecked = true;
      }
      else if (eye.Side == PupilReflexEye.EyeSide.Right)
      {
        rightEyeChecked = true;
      }

      EvaluateCompletion();
    }

    private void EvaluateCompletion()
    {
      if (!completeWhenBothEyesChecked)
      {
        return;
      }

      bool bothChecked = leftEyeChecked && rightEyeChecked;
      if (!bothChecked || examCompleted)
      {
        return;
      }

      examCompleted = true;
      BothEyesChecked?.Invoke();
      onBothEyesChecked?.Invoke();
    }

    private void EnsureRuntimeMaterials()
    {
      if (runtimeFaceMaterial == null)
      {
        runtimeFaceMaterial = CreateRuntimeMaterial("M_Runtime_FacePlate", new Color(0.93f, 0.80f, 0.72f, 1f));
      }

      if (runtimeScleraMaterial == null)
      {
        runtimeScleraMaterial = CreateRuntimeMaterial("M_Runtime_Sclera", new Color(1f, 1f, 1f, 1f));
      }

      if (runtimeIrisMaterial == null)
      {
        runtimeIrisMaterial = CreateRuntimeMaterial("M_Runtime_Iris", new Color(0.30f, 0.17f, 0.08f, 1f));
      }

      if (runtimePupilMaterial == null)
      {
        runtimePupilMaterial = CreateRuntimeMaterial("M_Runtime_Pupil", new Color(0f, 0f, 0f, 1f));
      }

      if (runtimeCorneaMaterial == null)
      {
        runtimeCorneaMaterial = CreateRuntimeTransparentMaterial(
          "M_Runtime_Cornea",
          new Color(0.88f, 0.95f, 1f, corneaAlpha),
          corneaSmoothness);
      }

      if (runtimeCorneaMaterial != null)
      {
        Color corneaColor = new Color(0.88f, 0.95f, 1f, corneaAlpha);

        if (runtimeCorneaMaterial.HasProperty("_Color"))
        {
          runtimeCorneaMaterial.SetColor("_Color", corneaColor);
        }

        if (runtimeCorneaMaterial.HasProperty("_BaseColor"))
        {
          runtimeCorneaMaterial.SetColor("_BaseColor", corneaColor);
        }

        if (runtimeCorneaMaterial.HasProperty("_Smoothness"))
        {
          runtimeCorneaMaterial.SetFloat("_Smoothness", corneaSmoothness);
        }

        if (runtimeCorneaMaterial.HasProperty("_Glossiness"))
        {
          runtimeCorneaMaterial.SetFloat("_Glossiness", corneaSmoothness);
        }
      }
    }

    private static Material CreateRuntimeMaterial(string materialName, Color color)
    {
      Shader shader = FindPreferredOpaqueShader();
      if (shader == null)
      {
        return null;
      }

      Material material = new Material(shader)
      {
        name = materialName
      };

      if (material.HasProperty("_Color"))
      {
        material.SetColor("_Color", color);
      }

      if (material.HasProperty("_BaseColor"))
      {
        material.SetColor("_BaseColor", color);
      }

      if (material.HasProperty("_Smoothness"))
      {
        material.SetFloat("_Smoothness", 0.2f);
      }

      if (material.HasProperty("_Glossiness"))
      {
        material.SetFloat("_Glossiness", 0.2f);
      }

      return material;
    }

    private static Material CreateRuntimeTransparentMaterial(string materialName, Color color, float smoothness)
    {
      Shader shader = FindPreferredTransparentShader();
      if (shader == null)
      {
        return null;
      }

      Material material = new Material(shader)
      {
        name = materialName
      };

      if (material.HasProperty("_Color"))
      {
        material.SetColor("_Color", color);
      }

      if (material.HasProperty("_BaseColor"))
      {
        material.SetColor("_BaseColor", color);
      }

      if (material.HasProperty("_Smoothness"))
      {
        material.SetFloat("_Smoothness", smoothness);
      }

      if (material.HasProperty("_Glossiness"))
      {
        material.SetFloat("_Glossiness", smoothness);
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

      return material;
    }

    private static Shader FindPreferredOpaqueShader()
    {
      RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
      if (pipeline != null)
      {
        string pipelineType = pipeline.GetType().Name;

        if (pipelineType.Contains("HDRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "HDRP/Unlit",
            "Universal Render Pipeline/Unlit",
            "Unlit/Color",
            "Standard",
            "Legacy Shaders/Diffuse");
        }

        if (pipelineType.Contains("UniversalRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "Universal Render Pipeline/Unlit",
            "HDRP/Unlit",
            "Unlit/Color",
            "Standard",
            "Legacy Shaders/Diffuse");
        }
      }

      return FindFirstSupportedShader(
        "Unlit/Color",
        "Sprites/Default",
        "Standard",
        "Universal Render Pipeline/Unlit",
        "HDRP/Unlit",
        "Legacy Shaders/Diffuse");
    }

    private static Shader FindPreferredTransparentShader()
    {
      RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
      if (pipeline != null)
      {
        string pipelineType = pipeline.GetType().Name;

        if (pipelineType.Contains("HDRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "HDRP/Unlit",
            "HDRP/Lit",
            "Universal Render Pipeline/Unlit",
            "Legacy Shaders/Transparent/Diffuse",
            "Standard");
        }

        if (pipelineType.Contains("UniversalRenderPipeline"))
        {
          return FindFirstSupportedShader(
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "HDRP/Unlit",
            "Legacy Shaders/Transparent/Diffuse",
            "Standard");
        }
      }

      return FindFirstSupportedShader(
        "Legacy Shaders/Transparent/Diffuse",
        "Standard",
        "Unlit/Color",
        "Sprites/Default");
    }

    private static Shader FindFirstSupportedShader(params string[] shaderNames)
    {
      if (shaderNames == null)
      {
        return null;
      }

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

    private void RegisterStatusGuiInstance()
    {
      if (StatusGuiInstances.Contains(this))
      {
        return;
      }

      StatusGuiInstances.Add(this);
    }

    private void UnregisterStatusGuiInstance()
    {
      StatusGuiInstances.Remove(this);
    }

    private int GetStatusGuiIndex()
    {
      int visibleIndex = 0;

      for (int i = 0; i < StatusGuiInstances.Count; i++)
      {
        PupilReflexPatientSandboxBootstrap instance = StatusGuiInstances[i];
        if (instance == null)
        {
          StatusGuiInstances.RemoveAt(i);
          i--;
          continue;
        }

        if (instance == this)
        {
          return visibleIndex;
        }

        if (instance.isActiveAndEnabled && instance.drawStatusGui)
        {
          visibleIndex++;
        }
      }

      return visibleIndex;
    }
  }
}
