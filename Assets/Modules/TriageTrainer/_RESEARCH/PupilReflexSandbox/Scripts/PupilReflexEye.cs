using UnityEngine;
using UnityEngine.Rendering;

namespace TriageTrainer.Tests.PupilReflexSandbox
{
  /// <summary>
  /// Models a single eye with sclera, iris, pupil and cornea layers.
  /// The iris and pupil are rendered as spherical patches that share
  /// the same curvature as the eyeball.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilReflexEye : MonoBehaviour
  {
    [Header("Anatomy")]
    [SerializeField, Min(0.5f)] private float eyeballDiameterCentimeters = 2.5f;
    [SerializeField, Min(1f)] private float irisDiameterMillimeters = 12f;
    [SerializeField, Range(1f, 10f)] private float pupilDiameterAtRestMillimeters = 5f;
    [SerializeField, Range(1f, 10f)] private float pupilDiameterAtMaxLightMillimeters = 2f;

    [Header("Reaction")]
    [SerializeField] private bool reactsToDirectLight = true;
    [SerializeField, Range(0f, 1f)] private float constrictionStrength = 1f;
    [SerializeField, Min(0.01f)] private float constrictionDurationSeconds = 0.15f;
    [SerializeField, Min(0.01f)] private float dilationDurationSeconds = 0.28f;
    [SerializeField, Range(0.01f, 0.99f)] private float exponentialDecayB = 0.6f;
    [SerializeField, Min(1f)] private float exponentialDomainX = 8f;

    [Header("Colors")]
    [SerializeField] private Color scleraColor = new Color(0.97f, 0.97f, 0.97f, 1f);
    [SerializeField] private Color irisColor = new Color(0.35f, 0.22f, 0.1f, 1f);
    [SerializeField] private Color pupilColor = new Color(0.03f, 0.03f, 0.03f, 1f);
    [SerializeField] private Color corneaColor = new Color(0.86f, 0.92f, 1f, 0.13f);

    [Header("Mesh Quality")]
    [SerializeField, Range(24, 128)] private int angularSegments = 64;
    [SerializeField, Range(2, 12)] private int radialSegments = 6;
    [SerializeField, Min(0f)] private float overlaySurfaceOffsetMillimeters = 0.02f;
    [SerializeField, Min(0f)] private float corneaThicknessMillimeters = 0.45f;

    private const string ScleraName = "Sclera";
    private const string IrisName = "Iris";
    private const string PupilName = "Pupil";
    private const string CorneaName = "Cornea";

    private Transform scleraTransform;
    private Transform corneaTransform;
    private MeshFilter irisMeshFilter;
    private MeshFilter pupilMeshFilter;
    private MeshRenderer irisMeshRenderer;
    private MeshRenderer pupilMeshRenderer;

    private Mesh irisMesh;
    private Mesh pupilMesh;

    private Material scleraMaterial;
    private Material irisMaterial;
    private Material pupilMaterial;
    private Material corneaMaterial;

    private float currentPupilDiameterMillimeters;
    private float targetPupilDiameterMillimeters;
    private float transitionStartDiameterMillimeters;
    private float transitionElapsedSeconds;
    private float transitionDurationSeconds;
    private bool isConstrictionTransition;
    private bool hasReceivedDirectLight;
    private float currentStimulus01;

    public float CurrentPupilDiameterMillimeters => currentPupilDiameterMillimeters;
    public float CurrentPupilRadiusMeters => currentPupilDiameterMillimeters * 0.0005f;
    public bool HasReceivedDirectLight => hasReceivedDirectLight;
    public bool ReactsToDirectLight => reactsToDirectLight;
    public float ConstrictionStrength => constrictionStrength;
    public float EyeballDiameterMeters => eyeballDiameterCentimeters * 0.01f;
    public Vector3 PupilCenterWorld => transform.position + transform.forward * EyeballRadiusMeters;

    private float EyeballRadiusMeters => eyeballDiameterCentimeters * 0.005f;
    private float IrisRadiusMeters => irisDiameterMillimeters * 0.0005f;

    private void Awake()
    {
      ClampConfig();
      EnsureHierarchy();
      BuildOrRefreshMeshes();

      currentPupilDiameterMillimeters = pupilDiameterAtRestMillimeters;
      targetPupilDiameterMillimeters = currentPupilDiameterMillimeters;
      transitionStartDiameterMillimeters = currentPupilDiameterMillimeters;
      transitionDurationSeconds = constrictionDurationSeconds;

      ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters);
    }

    private void OnValidate()
    {
      ClampConfig();

      if (!isActiveAndEnabled)
      {
        return;
      }

      EnsureHierarchy();
      BuildOrRefreshMeshes();

      if (!Application.isPlaying)
      {
        currentPupilDiameterMillimeters = pupilDiameterAtRestMillimeters;
        targetPupilDiameterMillimeters = currentPupilDiameterMillimeters;
        transitionStartDiameterMillimeters = currentPupilDiameterMillimeters;
        transitionDurationSeconds = constrictionDurationSeconds;
        ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters);
      }
      else
      {
        currentPupilDiameterMillimeters = Mathf.Clamp(
          currentPupilDiameterMillimeters,
          pupilDiameterAtMaxLightMillimeters,
          pupilDiameterAtRestMillimeters);

        targetPupilDiameterMillimeters = Mathf.Clamp(
          targetPupilDiameterMillimeters,
          pupilDiameterAtMaxLightMillimeters,
          pupilDiameterAtRestMillimeters);

        ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters);
      }
    }

    private void Update()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      AnimatePupil(Time.deltaTime);
    }

    private void OnDestroy()
    {
      DestroyRuntimeMaterial(ref scleraMaterial);
      DestroyRuntimeMaterial(ref irisMaterial);
      DestroyRuntimeMaterial(ref pupilMaterial);
      DestroyRuntimeMaterial(ref corneaMaterial);

      DestroyRuntimeMesh(ref irisMesh);
      DestroyRuntimeMesh(ref pupilMesh);
    }

    public void ConfigureAnatomy(
      float eyeballDiameterCm,
      float irisDiameterMm,
      float restPupilMm,
      float minPupilMm)
    {
      eyeballDiameterCentimeters = eyeballDiameterCm;
      irisDiameterMillimeters = irisDiameterMm;
      pupilDiameterAtRestMillimeters = restPupilMm;
      pupilDiameterAtMaxLightMillimeters = minPupilMm;
      ClampConfig();

      EnsureHierarchy();
      BuildOrRefreshMeshes();

      currentPupilDiameterMillimeters = Mathf.Clamp(
        currentPupilDiameterMillimeters,
        pupilDiameterAtMaxLightMillimeters,
        pupilDiameterAtRestMillimeters);
      targetPupilDiameterMillimeters = Mathf.Clamp(
        targetPupilDiameterMillimeters,
        pupilDiameterAtMaxLightMillimeters,
        pupilDiameterAtRestMillimeters);
      ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters);
    }

    public void ConfigureReaction(
      bool reacts,
      float strength,
      float constrictionSeconds,
      float dilationSeconds,
      float decayB)
    {
      reactsToDirectLight = reacts;
      constrictionStrength = Mathf.Clamp01(strength);
      constrictionDurationSeconds = Mathf.Max(0.01f, constrictionSeconds);
      dilationDurationSeconds = Mathf.Max(0.01f, dilationSeconds);
      exponentialDecayB = Mathf.Clamp(decayB, 0.01f, 0.99f);
      ClampConfig();

      float desired = ComputeTargetDiameterFromStimulus(currentStimulus01);
      BeginTransitionTo(desired);
    }

    public void SetDirectLightLevel(float stimulus01)
    {
      currentStimulus01 = Mathf.Clamp01(stimulus01);

      if (currentStimulus01 > 0f)
      {
        hasReceivedDirectLight = true;
      }

      float desiredDiameter = ComputeTargetDiameterFromStimulus(currentStimulus01);
      BeginTransitionTo(desiredDiameter);
    }

    public void ResetExamState()
    {
      hasReceivedDirectLight = false;
      currentStimulus01 = 0f;

      currentPupilDiameterMillimeters = pupilDiameterAtRestMillimeters;
      targetPupilDiameterMillimeters = pupilDiameterAtRestMillimeters;
      transitionStartDiameterMillimeters = currentPupilDiameterMillimeters;
      transitionElapsedSeconds = 0f;
      transitionDurationSeconds = constrictionDurationSeconds;
      isConstrictionTransition = false;

      ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters);
    }

    /// <summary>
    /// Samples how much direct light reaches the pupil.
    /// Returns true when the probe ray can be projected onto the pupil plane.
    /// </summary>
    public bool TrySampleLight(
      Ray probeRay,
      float probeOuterRadiusMeters,
      float probeInnerRadiusMeters,
      float lightIntensity01,
      out float stimulus01,
      out float radialDistanceMeters)
    {
      stimulus01 = 0f;
      radialDistanceMeters = 0f;

      Plane pupilPlane = new Plane(transform.forward, PupilCenterWorld);
      if (!pupilPlane.Raycast(probeRay, out float enter) || enter < 0f)
      {
        return false;
      }

      Vector3 projectedProbeCenter = probeRay.GetPoint(enter);
      radialDistanceMeters = Vector3.Distance(projectedProbeCenter, PupilCenterWorld);

      float pupilRadiusMeters = CurrentPupilRadiusMeters;
      float contactStartMeters = probeOuterRadiusMeters + pupilRadiusMeters;
      if (radialDistanceMeters > contactStartMeters)
      {
        return true;
      }

      float zoneFactor = radialDistanceMeters <= probeInnerRadiusMeters ? 1f : 0.5f;

      float overlap01 = 1f - Mathf.Clamp01(
        (radialDistanceMeters - pupilRadiusMeters) /
        Mathf.Max(0.0001f, probeOuterRadiusMeters));

      stimulus01 = Mathf.Clamp01(lightIntensity01 * zoneFactor * overlap01);
      return true;
    }

    public bool TrySampleLightFromAxis(
      Vector3 axisPoint,
      Vector3 axisDirection,
      float probeOuterRadiusMeters,
      float probeInnerRadiusMeters,
      float lightIntensity01,
      out float stimulus01,
      out float radialDistanceMeters)
    {
      stimulus01 = 0f;
      radialDistanceMeters = 0f;

      float axisDirSqr = axisDirection.sqrMagnitude;
      if (axisDirSqr < 0.0001f)
      {
        return false;
      }

      Vector3 axisDir = axisDirection / Mathf.Sqrt(axisDirSqr);
      Plane pupilPlane = new Plane(transform.forward, PupilCenterWorld);
      float denom = Vector3.Dot(pupilPlane.normal, axisDir);
      if (Mathf.Abs(denom) < 0.0001f)
      {
        return false;
      }

      float t = Vector3.Dot(PupilCenterWorld - axisPoint, pupilPlane.normal) / denom;
      Vector3 projectedCenter = axisPoint + axisDir * t;
      radialDistanceMeters = Vector3.Distance(projectedCenter, PupilCenterWorld);

      float pupilRadiusMeters = CurrentPupilRadiusMeters;
      float contactStartMeters = probeOuterRadiusMeters + pupilRadiusMeters;
      if (radialDistanceMeters > contactStartMeters)
      {
        return true;
      }

      float zoneFactor = radialDistanceMeters <= probeInnerRadiusMeters ? 1f : 0.5f;

      float overlap01 = 1f - Mathf.Clamp01(
        (radialDistanceMeters - pupilRadiusMeters) /
        Mathf.Max(0.0001f, probeOuterRadiusMeters));

      stimulus01 = Mathf.Clamp01(lightIntensity01 * zoneFactor * overlap01);
      return true;
    }

    public bool TryRaycastEyeball(Ray ray, out Vector3 hitPoint, out Vector3 hitNormal)
    {
      float radius = EyeballRadiusMeters;
      Vector3 scale = transform.lossyScale;
      float scaleFactor = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
      radius *= Mathf.Max(0.0001f, scaleFactor);

      Vector3 center = transform.position;
      Vector3 oc = ray.origin - center;
      float a = Vector3.Dot(ray.direction, ray.direction);
      float b = 2f * Vector3.Dot(oc, ray.direction);
      float c = Vector3.Dot(oc, oc) - (radius * radius);
      float discriminant = (b * b) - (4f * a * c);

      if (discriminant < 0f)
      {
        hitPoint = default;
        hitNormal = default;
        return false;
      }

      float sqrt = Mathf.Sqrt(discriminant);
      float t0 = (-b - sqrt) / (2f * a);
      float t1 = (-b + sqrt) / (2f * a);
      float t = t0 >= 0f ? t0 : (t1 >= 0f ? t1 : -1f);

      if (t < 0f)
      {
        hitPoint = default;
        hitNormal = default;
        return false;
      }

      hitPoint = ray.GetPoint(t);
      hitNormal = (hitPoint - center).normalized;
      return true;
    }

    private void BeginTransitionTo(float desiredDiameterMillimeters)
    {
      desiredDiameterMillimeters = Mathf.Clamp(
        desiredDiameterMillimeters,
        pupilDiameterAtMaxLightMillimeters,
        pupilDiameterAtRestMillimeters);

      if (Mathf.Approximately(targetPupilDiameterMillimeters, desiredDiameterMillimeters))
      {
        return;
      }

      transitionStartDiameterMillimeters = currentPupilDiameterMillimeters;
      targetPupilDiameterMillimeters = desiredDiameterMillimeters;
      transitionElapsedSeconds = 0f;

      isConstrictionTransition = targetPupilDiameterMillimeters < transitionStartDiameterMillimeters;
      transitionDurationSeconds = isConstrictionTransition
        ? constrictionDurationSeconds
        : dilationDurationSeconds;
    }

    private void AnimatePupil(float deltaTime)
    {
      if (Mathf.Abs(currentPupilDiameterMillimeters - targetPupilDiameterMillimeters) < 0.0001f)
      {
        currentPupilDiameterMillimeters = targetPupilDiameterMillimeters;
        return;
      }

      transitionElapsedSeconds += Mathf.Max(0f, deltaTime);
      float t = Mathf.Clamp01(transitionElapsedSeconds / Mathf.Max(0.01f, transitionDurationSeconds));

      float nextDiameter;
      if (isConstrictionTransition)
      {
        float a = transitionStartDiameterMillimeters - targetPupilDiameterMillimeters;
        float x = t * exponentialDomainX;
        float remaining = a * Mathf.Pow(1f - exponentialDecayB, x);
        nextDiameter = targetPupilDiameterMillimeters + remaining;
      }
      else
      {
        float eased = t * t * (3f - 2f * t);
        nextDiameter = Mathf.Lerp(
          transitionStartDiameterMillimeters,
          targetPupilDiameterMillimeters,
          eased);
      }

      currentPupilDiameterMillimeters = Mathf.Clamp(
        nextDiameter,
        pupilDiameterAtMaxLightMillimeters,
        pupilDiameterAtRestMillimeters);

      if (t >= 1f)
      {
        currentPupilDiameterMillimeters = targetPupilDiameterMillimeters;
      }

      ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters);
    }

    private float ComputeTargetDiameterFromStimulus(float stimulus01)
    {
      if (!reactsToDirectLight || constrictionStrength <= 0f)
      {
        return pupilDiameterAtRestMillimeters;
      }

      float effectiveStimulus = Mathf.Clamp01(stimulus01 * constrictionStrength);
      return Mathf.Lerp(
        pupilDiameterAtRestMillimeters,
        pupilDiameterAtMaxLightMillimeters,
        effectiveStimulus);
    }

    private void EnsureHierarchy()
    {
      scleraTransform = GetOrCreatePrimitiveChild(ScleraName, PrimitiveType.Sphere, out MeshRenderer scleraRenderer);
      corneaTransform = GetOrCreatePrimitiveChild(CorneaName, PrimitiveType.Sphere, out MeshRenderer corneaRenderer);

      Transform irisTransform = GetOrCreateMeshChild(IrisName, out irisMeshFilter, out irisMeshRenderer);
      Transform pupilTransform = GetOrCreateMeshChild(PupilName, out pupilMeshFilter, out pupilMeshRenderer);

      irisTransform.localPosition = Vector3.zero;
      irisTransform.localRotation = Quaternion.identity;
      irisTransform.localScale = Vector3.one;

      pupilTransform.localPosition = Vector3.zero;
      pupilTransform.localRotation = Quaternion.identity;
      pupilTransform.localScale = Vector3.one;

      if (scleraMaterial == null)
      {
        scleraMaterial = CreateRuntimeMaterial("M_Runtime_EyeSclera", transparent: false);
      }

      if (irisMaterial == null)
      {
        irisMaterial = CreateRuntimeMaterial("M_Runtime_EyeIris", transparent: false);
      }

      if (pupilMaterial == null)
      {
        pupilMaterial = CreateRuntimeMaterial("M_Runtime_EyePupil", transparent: false);
      }

      if (corneaMaterial == null)
      {
        corneaMaterial = CreateRuntimeMaterial("M_Runtime_EyeCornea", transparent: true);
      }

      if (scleraRenderer != null)
      {
        ApplyColor(scleraMaterial, scleraColor);
        scleraRenderer.sharedMaterial = scleraMaterial;
      }

      if (irisMeshRenderer != null)
      {
        ApplyColor(irisMaterial, irisColor);
        irisMeshRenderer.sharedMaterial = irisMaterial;
      }

      if (pupilMeshRenderer != null)
      {
        ApplyColor(pupilMaterial, pupilColor);
        pupilMeshRenderer.sharedMaterial = pupilMaterial;
      }

      if (corneaRenderer != null)
      {
        ApplyColor(corneaMaterial, corneaColor);
        corneaRenderer.sharedMaterial = corneaMaterial;
      }
    }

    private void BuildOrRefreshMeshes()
    {
      if (irisMeshFilter == null || pupilMeshFilter == null || scleraTransform == null || corneaTransform == null)
      {
        return;
      }

      float scleraDiameterMeters = eyeballDiameterCentimeters * 0.01f;
      scleraTransform.localPosition = Vector3.zero;
      scleraTransform.localRotation = Quaternion.identity;
      scleraTransform.localScale = Vector3.one * scleraDiameterMeters;

      float corneaDiameterMeters = scleraDiameterMeters + (corneaThicknessMillimeters * 0.001f);
      corneaTransform.localPosition = Vector3.zero;
      corneaTransform.localRotation = Quaternion.identity;
      corneaTransform.localScale = Vector3.one * corneaDiameterMeters;

      if (irisMesh == null)
      {
        irisMesh = CreateDynamicMesh("Msh_Runtime_Iris");
        irisMeshFilter.sharedMesh = irisMesh;
      }

      if (pupilMesh == null)
      {
        pupilMesh = CreateDynamicMesh("Msh_Runtime_Pupil");
        pupilMeshFilter.sharedMesh = pupilMesh;
      }

      ApplyPupilDiameterImmediate(currentPupilDiameterMillimeters <= 0f
        ? pupilDiameterAtRestMillimeters
        : currentPupilDiameterMillimeters);
    }

    private void ApplyPupilDiameterImmediate(float pupilDiameterMillimeters)
    {
      pupilDiameterMillimeters = Mathf.Clamp(
        pupilDiameterMillimeters,
        pupilDiameterAtMaxLightMillimeters,
        pupilDiameterAtRestMillimeters);

      currentPupilDiameterMillimeters = pupilDiameterMillimeters;

      if (irisMesh == null || pupilMesh == null)
      {
        return;
      }

      float sphereRadius = EyeballRadiusMeters + (overlaySurfaceOffsetMillimeters * 0.001f);
      float pupilRadius = pupilDiameterMillimeters * 0.0005f;
      float irisRadius = IrisRadiusMeters;

      BuildSphericalAnnulusMesh(
        irisMesh,
        sphereRadius,
        pupilRadius,
        irisRadius,
        angularSegments,
        radialSegments);

      BuildSphericalAnnulusMesh(
        pupilMesh,
        sphereRadius + 0.00001f,
        0f,
        pupilRadius,
        angularSegments,
        radialSegments);
    }

    private void ClampConfig()
    {
      eyeballDiameterCentimeters = Mathf.Max(0.5f, eyeballDiameterCentimeters);
      irisDiameterMillimeters = Mathf.Max(1f, irisDiameterMillimeters);

      pupilDiameterAtRestMillimeters = Mathf.Clamp(pupilDiameterAtRestMillimeters, 1f, 10f);
      pupilDiameterAtMaxLightMillimeters = Mathf.Clamp(pupilDiameterAtMaxLightMillimeters, 1f, 10f);

      if (pupilDiameterAtMaxLightMillimeters > pupilDiameterAtRestMillimeters)
      {
        pupilDiameterAtMaxLightMillimeters = pupilDiameterAtRestMillimeters;
      }

      if (irisDiameterMillimeters <= pupilDiameterAtRestMillimeters)
      {
        irisDiameterMillimeters = pupilDiameterAtRestMillimeters + 0.1f;
      }

      constrictionStrength = Mathf.Clamp01(constrictionStrength);
      constrictionDurationSeconds = Mathf.Max(0.01f, constrictionDurationSeconds);
      dilationDurationSeconds = Mathf.Max(0.01f, dilationDurationSeconds);
      exponentialDecayB = Mathf.Clamp(exponentialDecayB, 0.01f, 0.99f);
      exponentialDomainX = Mathf.Max(1f, exponentialDomainX);

      angularSegments = Mathf.Clamp(angularSegments, 24, 128);
      radialSegments = Mathf.Clamp(radialSegments, 2, 12);
      overlaySurfaceOffsetMillimeters = Mathf.Max(0f, overlaySurfaceOffsetMillimeters);
      corneaThicknessMillimeters = Mathf.Max(0f, corneaThicknessMillimeters);
    }

    private Transform GetOrCreatePrimitiveChild(string childName, PrimitiveType primitiveType, out MeshRenderer renderer)
    {
      Transform child = transform.Find(childName);
      if (child == null)
      {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = childName;
        child = primitive.transform;
        child.SetParent(transform, false);

        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
          if (Application.isPlaying)
          {
            Destroy(collider);
          }
          else
          {
            DestroyImmediate(collider);
          }
        }
      }

      child.localPosition = Vector3.zero;
      child.localRotation = Quaternion.identity;
      child.localScale = Vector3.one;

      renderer = child.GetComponent<MeshRenderer>();
      return child;
    }

    private Transform GetOrCreateMeshChild(
      string childName,
      out MeshFilter meshFilter,
      out MeshRenderer meshRenderer)
    {
      Transform child = transform.Find(childName);
      if (child == null)
      {
        GameObject childObject = new GameObject(childName);
        child = childObject.transform;
        child.SetParent(transform, false);
      }

      meshFilter = child.GetComponent<MeshFilter>();
      if (meshFilter == null)
      {
        meshFilter = child.gameObject.AddComponent<MeshFilter>();
      }

      meshRenderer = child.GetComponent<MeshRenderer>();
      if (meshRenderer == null)
      {
        meshRenderer = child.gameObject.AddComponent<MeshRenderer>();
      }

      return child;
    }

    private static Mesh CreateDynamicMesh(string meshName)
    {
      Mesh mesh = new Mesh();
      mesh.name = meshName;
      mesh.MarkDynamic();
      return mesh;
    }

    private static void BuildSphericalAnnulusMesh(
      Mesh mesh,
      float sphereRadiusMeters,
      float innerProjectedRadiusMeters,
      float outerProjectedRadiusMeters,
      int angularSegments,
      int radialSegments)
    {
      if (mesh == null)
      {
        return;
      }

      float radius = Mathf.Max(0.0001f, sphereRadiusMeters);
      float innerRadius = Mathf.Clamp(innerProjectedRadiusMeters, 0f, radius * 0.999f);
      float outerRadius = Mathf.Clamp(outerProjectedRadiusMeters, innerRadius + 0.00001f, radius * 0.999f);

      int rowCount = radialSegments + 1;
      int columnCount = angularSegments + 1;
      int vertexCount = rowCount * columnCount;
      int indexCount = radialSegments * angularSegments * 6;

      Vector3[] vertices = new Vector3[vertexCount];
      Vector3[] normals = new Vector3[vertexCount];
      Vector2[] uv = new Vector2[vertexCount];
      int[] triangles = new int[indexCount];

      int vertexIndex = 0;
      for (int radial = 0; radial <= radialSegments; radial++)
      {
        float radialT = radialSegments == 0 ? 0f : radial / (float)radialSegments;
        float projectedRadius = Mathf.Lerp(innerRadius, outerRadius, radialT);
        float polar = Mathf.Asin(Mathf.Clamp(projectedRadius / radius, 0f, 1f));
        float sinPolar = Mathf.Sin(polar);
        float cosPolar = Mathf.Cos(polar);

        for (int angular = 0; angular <= angularSegments; angular++)
        {
          float angularT = angularSegments == 0 ? 0f : angular / (float)angularSegments;
          float azimuth = angularT * Mathf.PI * 2f;
          float sinAzimuth = Mathf.Sin(azimuth);
          float cosAzimuth = Mathf.Cos(azimuth);

          Vector3 normal = new Vector3(cosAzimuth * sinPolar, sinAzimuth * sinPolar, cosPolar);
          vertices[vertexIndex] = normal * radius;
          normals[vertexIndex] = normal;
          uv[vertexIndex] = new Vector2((normal.x * 0.5f) + 0.5f, (normal.y * 0.5f) + 0.5f);
          vertexIndex++;
        }
      }

      int triangleIndex = 0;
      for (int radial = 0; radial < radialSegments; radial++)
      {
        int rowStart = radial * columnCount;
        int nextRowStart = (radial + 1) * columnCount;

        for (int angular = 0; angular < angularSegments; angular++)
        {
          int a = rowStart + angular;
          int b = nextRowStart + angular;
          int c = a + 1;
          int d = b + 1;

          triangles[triangleIndex++] = a;
          triangles[triangleIndex++] = b;
          triangles[triangleIndex++] = c;

          triangles[triangleIndex++] = c;
          triangles[triangleIndex++] = b;
          triangles[triangleIndex++] = d;
        }
      }

      mesh.Clear();
      mesh.vertices = vertices;
      mesh.normals = normals;
      mesh.uv = uv;
      mesh.triangles = triangles;
      mesh.RecalculateBounds();
    }

    private static Material CreateRuntimeMaterial(string materialName, bool transparent)
    {
      Shader shader = SelectLitShader();

      Material material = new Material(shader);
      material.name = materialName;

      if (transparent)
      {
        ConfigureMaterialAsTransparent(material);
      }
      else
      {
        ConfigureMaterialAsOpaque(material);
      }

      SetMaterialCullMode(material, 0f);
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

      Shader builtinShader = Shader.Find("Standard");
      if (builtinShader != null)
      {
        return builtinShader;
      }

      return Shader.Find("Sprites/Default");
    }

    private static void ApplyColor(Material material, Color color)
    {
      if (material == null)
      {
        return;
      }

      if (material.HasProperty("_BaseColor"))
      {
        material.SetColor("_BaseColor", color);
      }

      if (material.HasProperty("_Color"))
      {
        material.SetColor("_Color", color);
      }
    }

    private static void ConfigureMaterialAsTransparent(Material material)
    {
      if (material == null)
      {
        return;
      }

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
        return;
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

      if (material.HasProperty("_AlphaClip"))
      {
        material.SetFloat("_AlphaClip", 0f);
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

    private static void ConfigureMaterialAsOpaque(Material material)
    {
      if (material == null)
      {
        return;
      }

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
        return;
      }

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

    private static void SetMaterialCullMode(Material material, float cullValue)
    {
      if (material == null)
      {
        return;
      }

      if (material.HasProperty("_Cull"))
      {
        material.SetFloat("_Cull", cullValue);
      }

      if (material.HasProperty("_CullMode"))
      {
        material.SetFloat("_CullMode", cullValue);
      }

      if (material.HasProperty("_CullModeForward"))
      {
        material.SetFloat("_CullModeForward", cullValue);
      }
    }

    private static void DestroyRuntimeMaterial(ref Material material)
    {
      if (material == null)
      {
        return;
      }

      if (Application.isPlaying)
      {
        Object.Destroy(material);
      }
      else
      {
        Object.DestroyImmediate(material);
      }

      material = null;
    }

    private static void DestroyRuntimeMesh(ref Mesh mesh)
    {
      if (mesh == null)
      {
        return;
      }

      if (Application.isPlaying)
      {
        Object.Destroy(mesh);
      }
      else
      {
        Object.DestroyImmediate(mesh);
      }

      mesh = null;
    }
  }
}