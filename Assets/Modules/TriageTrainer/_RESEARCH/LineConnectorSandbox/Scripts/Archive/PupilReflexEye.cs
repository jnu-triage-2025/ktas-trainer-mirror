using System;

using UnityEngine;
using UnityEngine.Events;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// 단일 안구의 직접 대광 반사(동공 수축)를 담당합니다.
  /// </summary>
  [DisallowMultipleComponent]
  public class PupilReflexEye : MonoBehaviour
  {
    public enum EyeSide
    {
      Left = 0,
      Right = 1,
    }

    [Serializable]
    public class EyeEvent : UnityEvent<PupilReflexEye>
    {
    }

    [Header("Eye Identity")]
    [SerializeField] private EyeSide eyeSide = EyeSide.Left;

    [Header("Visual References")]
    [SerializeField] private Transform scleraVisual;
    [SerializeField] private Transform irisVisual;
    [SerializeField] private Transform pupilVisual;
    [SerializeField] private PupilApertureVisual apertureVisual;

    [Header("Pupil Diameter (mm)")]
    [SerializeField, Min(1f)] private float baselinePupilDiameterMm = 5f;
    [SerializeField, Min(0.5f)] private float minimumPupilDiameterMm = 2f;

    [Header("Direct Light Response")]
    [SerializeField] private bool reactsToLight = true;
    [SerializeField, Range(0f, 1f)] private float constrictionAmount = 1f;
    [SerializeField, Min(0.01f)] private float constrictionDurationSeconds = 0.15f;
    [SerializeField, Min(0.01f)] private float dilationDurationSeconds = 0.28f;
    [SerializeField, Range(0f, 1f)] private float constrictionTapering = 0.5f;
    [SerializeField, Min(0f)] private float retriggerCooldownSeconds = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool hasBeenChecked;
    [SerializeField] private bool isDirectLightOnPupil;

    [Header("Events")]
    [SerializeField] private EyeEvent onFirstDirectLightDetected;

    private float currentPupilDiameterMeters;
    private float animationStartDiameterMeters;
    private float animationTargetDiameterMeters;
    private float animationDurationSeconds;
    private float animationElapsedSeconds;
    private float animationTapering;
    private bool isAnimating;
    private bool hadLightLastFrame;
    private float lastTriggerTime;

    private Vector3 pupilScaleRatio = Vector3.one;

    private const float DefaultDilationTapering = 0.5f;

    public event Action<PupilReflexEye> FirstDirectLightDetected;

    public EyeSide Side => eyeSide;
    public bool HasBeenChecked => hasBeenChecked;
    public bool ReactsToLight => reactsToLight;

    public float CurrentPupilDiameterMeters => currentPupilDiameterMeters;

    public float BaselinePupilDiameterMeters => baselinePupilDiameterMm * 0.001f;

    public float MinimumPupilDiameterMeters => Mathf.Min(
      BaselinePupilDiameterMeters,
      minimumPupilDiameterMm * 0.001f);

    public Vector3 PupilWorldPosition => pupilVisual != null ? pupilVisual.position : transform.position;

    private void Awake()
    {
      ResolveVisualReferences();
      CachePupilScaleRatio();
      ClampConfiguration();
      ForceBaselineDiameter();

      lastTriggerTime = -1000f;
    }

    private void OnEnable()
    {
      ResolveVisualReferences();
      CachePupilScaleRatio();
      ClampConfiguration();
      ForceBaselineDiameter();
    }

    private void OnValidate()
    {
      ResolveVisualReferences();
      CachePupilScaleRatio();
      ClampConfiguration();

      if (!Application.isPlaying)
      {
        ForceBaselineDiameter();
      }
    }

    private void Update()
    {
      if (!isAnimating)
      {
        return;
      }

      animationElapsedSeconds += Time.deltaTime;
      float duration = Mathf.Max(0.0001f, animationDurationSeconds);
      float normalizedTime = Mathf.Clamp01(animationElapsedSeconds / duration);

      float taperedProgress = EvaluateDeceleratingProgress(normalizedTime, animationTapering);
      currentPupilDiameterMeters = Mathf.Lerp(
        animationStartDiameterMeters,
        animationTargetDiameterMeters,
        taperedProgress);

      ApplyPupilDiameter(currentPupilDiameterMeters);

      if (normalizedTime >= 1f)
      {
        isAnimating = false;
      }
    }

    public void Initialize(
      EyeSide side,
      Transform sclera,
      Transform iris,
      Transform pupil,
      bool shouldReact,
      float constrictionStrength)
    {
      eyeSide = side;
      scleraVisual = sclera;
      irisVisual = iris;
      pupilVisual = pupil;

      reactsToLight = shouldReact;
      constrictionAmount = Mathf.Clamp01(constrictionStrength);

      ResolveVisualReferences();
      CachePupilScaleRatio();
      ClampConfiguration();
      ForceBaselineDiameter();
    }

    public void ConfigureResponse(bool shouldReact, float constrictionStrength)
    {
      reactsToLight = shouldReact;
      constrictionAmount = Mathf.Clamp01(constrictionStrength);
      ClampConfiguration();

      if (!isDirectLightOnPupil)
      {
        StartAnimation(
          currentPupilDiameterMeters,
          BaselinePupilDiameterMeters,
          dilationDurationSeconds,
          DefaultDilationTapering);
      }
    }

    public void ConfigurePupilSizeRangeMm(float beforeLightMm, float afterLightMm)
    {
      baselinePupilDiameterMm = beforeLightMm;
      minimumPupilDiameterMm = afterLightMm;
      ClampConfiguration();

      if (!isDirectLightOnPupil)
      {
        ForceBaselineDiameter();
      }
    }

    public void ConfigureConstrictionMotion(float durationSeconds, float taperingStrength)
    {
      constrictionDurationSeconds = durationSeconds;
      constrictionTapering = taperingStrength;
      ClampConfiguration();
    }

    public void ConfigureDilationDuration(float durationSeconds)
    {
      dilationDurationSeconds = durationSeconds;
      ClampConfiguration();
    }

    public bool TryGetDirectLightHit(
      Ray ray,
      float lightRadiusMeters,
      float maxDistance,
      out float rayDepth,
      out float rayToPupilDistance)
    {
      Vector3 center = PupilWorldPosition;
      Vector3 toCenter = center - ray.origin;

      rayDepth = Vector3.Dot(toCenter, ray.direction);
      rayToPupilDistance = float.MaxValue;

      if (rayDepth < 0f || rayDepth > maxDistance)
      {
        return false;
      }

      Vector3 closestPoint = ray.origin + (ray.direction * rayDepth);
      rayToPupilDistance = Vector3.Distance(closestPoint, center);

      float acceptanceRadius = lightRadiusMeters + (currentPupilDiameterMeters * 0.5f);
      return rayToPupilDistance <= acceptanceRadius;
    }

    public void SetDirectLightOnPupil(bool lightOnPupil)
    {
      isDirectLightOnPupil = lightOnPupil;

      if (lightOnPupil)
      {
        TryTriggerConstriction();
      }
      else if (hadLightLastFrame)
      {
        StartAnimation(
          currentPupilDiameterMeters,
          BaselinePupilDiameterMeters,
          dilationDurationSeconds,
          DefaultDilationTapering);
      }

      hadLightLastFrame = lightOnPupil;
    }

    public void ForceBaselineDiameter()
    {
      isAnimating = false;
      currentPupilDiameterMeters = BaselinePupilDiameterMeters;
      ApplyPupilDiameter(currentPupilDiameterMeters);
    }

    public void ResetEyeExamState()
    {
      hasBeenChecked = false;
      isDirectLightOnPupil = false;
      hadLightLastFrame = false;
      lastTriggerTime = -1000f;
      ForceBaselineDiameter();
    }

    private void TryTriggerConstriction()
    {
      float now = Time.time;
      if ((now - lastTriggerTime) < retriggerCooldownSeconds)
      {
        return;
      }

      lastTriggerTime = now;

      if (!hasBeenChecked)
      {
        hasBeenChecked = true;
        FirstDirectLightDetected?.Invoke(this);
        onFirstDirectLightDetected?.Invoke(this);
      }

      if (!reactsToLight || constrictionAmount <= 0f)
      {
        return;
      }

      float targetDiameter = Mathf.Lerp(
        BaselinePupilDiameterMeters,
        MinimumPupilDiameterMeters,
        constrictionAmount);

      StartAnimation(
        currentPupilDiameterMeters,
        targetDiameter,
        constrictionDurationSeconds,
        constrictionTapering);
    }

    private void StartAnimation(float fromDiameter, float toDiameter, float durationSeconds, float tapering)
    {
      animationStartDiameterMeters = Mathf.Max(0.0001f, fromDiameter);
      animationTargetDiameterMeters = Mathf.Max(0.0001f, toDiameter);
      animationDurationSeconds = Mathf.Max(0.0001f, durationSeconds);
      animationTapering = Mathf.Clamp01(tapering);
      animationElapsedSeconds = 0f;
      isAnimating = true;
    }

    private void ResolveVisualReferences()
    {
      if (pupilVisual == null)
      {
        Transform foundPupil = transform.Find("Pupil");
        if (foundPupil != null)
        {
          pupilVisual = foundPupil;
        }
      }

      if (irisVisual == null)
      {
        Transform foundIris = transform.Find("Iris");
        if (foundIris != null)
        {
          irisVisual = foundIris;
        }
      }

      if (scleraVisual == null)
      {
        Transform foundSclera = transform.Find("Sclera");
        if (foundSclera != null)
        {
          scleraVisual = foundSclera;
        }
      }

      if (apertureVisual == null)
      {
        apertureVisual = GetComponentInChildren<PupilApertureVisual>(true);
      }
    }

    private void CachePupilScaleRatio()
    {
      if (pupilVisual == null)
      {
        pupilScaleRatio = Vector3.one;
        return;
      }

      Vector3 scale = pupilVisual.localScale;
      float pivot = Mathf.Abs(scale.x) > 0.0001f ? scale.x : 1f;
      pupilScaleRatio = scale / pivot;
    }

    private void ApplyPupilDiameter(float diameterMeters)
    {
      float clampedDiameter = Mathf.Max(0.0001f, diameterMeters);

      if (apertureVisual != null)
      {
        apertureVisual.SetPupilDiameter(clampedDiameter);
        return;
      }

      if (pupilVisual == null)
      {
        return;
      }

      pupilVisual.localScale = pupilScaleRatio * clampedDiameter;
    }

    private void ClampConfiguration()
    {
      baselinePupilDiameterMm = Mathf.Max(1f, baselinePupilDiameterMm);
      minimumPupilDiameterMm = Mathf.Clamp(minimumPupilDiameterMm, 0.5f, baselinePupilDiameterMm);
      constrictionAmount = Mathf.Clamp01(constrictionAmount);
      constrictionDurationSeconds = Mathf.Max(0.01f, constrictionDurationSeconds);
      dilationDurationSeconds = Mathf.Max(0.01f, dilationDurationSeconds);
      constrictionTapering = Mathf.Clamp01(constrictionTapering);
      retriggerCooldownSeconds = Mathf.Max(0f, retriggerCooldownSeconds);
    }

    private static float EvaluateDeceleratingProgress(float normalizedTime, float tapering)
    {
      float t = Mathf.Clamp01(normalizedTime);

      // 0이면 선형, 0.5는 기존 감속(2t - t^2)과 유사, 1이면 더 강한 테이퍼링.
      float exponent = Mathf.Lerp(1f, 3f, Mathf.Clamp01(tapering));
      return Mathf.Clamp01(1f - Mathf.Pow(1f - t, exponent));
    }
  }
}
