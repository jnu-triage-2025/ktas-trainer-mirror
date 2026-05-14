using System.Collections;

using UnityEngine;

namespace TriageTrainer.Tests.LineConnectorSandbox
{
  /// <summary>
  /// Canvas Overlay 환경에서 동공 대광 반사를 2D 크기 변화로 시뮬레이션합니다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(RectTransform))]
  public class PupilReflex : MonoBehaviour
  {
    [Header("Clinical Settings")]
    [Tooltip("초기 동공 너비 (임상 기준 5mm를 UI 픽셀 단위로 환산)")]
    [SerializeField, Min(1f)] private float normalSize = 50f;
    [Tooltip("수축 시 동공 너비 (임상 기준 2mm)")]
    [SerializeField, Min(0.5f)] private float constrictedSize = 20f;
    [Tooltip("수축 소요 시간(초)")]
    [SerializeField, Min(0.01f)] private float reactionTime = 0.15f;

    [Header("Pathology (병리 상태)")]
    [Tooltip("체크 해제 시 빛에 반응하지 않음 (ex: 뇌출혈로 인한 동공 부동)")]
    [SerializeField] private bool isReactive = true;

    [Header("Physics Settings")]
    [Tooltip("초반 급격히 수축 후 후반 완만해지는 감속 곡선")]
    [SerializeField] private AnimationCurve constrictionCurve = new AnimationCurve(
      new Keyframe(0f, 0f, 2.8f, 2.8f),
      new Keyframe(1f, 1f, 0f, 0f));

    private RectTransform rectTransform;
    private Coroutine reflexCoroutine;
    private bool isConstricted;

    public RectTransform RectTransform => rectTransform;
    public bool IsReactive => isReactive;

    private void Awake()
    {
      rectTransform = GetComponent<RectTransform>();
      ClampSettings();
      ApplySize(normalSize);
    }

    private void OnValidate()
    {
      ClampSettings();

      if (!Application.isPlaying)
      {
        rectTransform = GetComponent<RectTransform>();
        ApplySize(normalSize);
      }
    }

    public void TriggerLightReflex(bool isLightOnPupil)
    {
      if (!isReactive || isLightOnPupil == isConstricted)
      {
        return;
      }

      isConstricted = isLightOnPupil;

      if (reflexCoroutine != null)
      {
        StopCoroutine(reflexCoroutine);
      }

      float targetSize = isConstricted ? constrictedSize : normalSize;
      reflexCoroutine = StartCoroutine(AnimatePupil(targetSize));
    }

    private IEnumerator AnimatePupil(float targetSize)
    {
      float startSize = rectTransform.sizeDelta.x;
      float elapsedTime = 0f;
      float duration = Mathf.Max(0.01f, reactionTime);

      while (elapsedTime < duration)
      {
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);
        float curveValue = constrictionCurve != null ? constrictionCurve.Evaluate(t) : t;
        float currentSize = Mathf.Lerp(startSize, targetSize, curveValue);
        ApplySize(currentSize);
        yield return null;
      }

      ApplySize(targetSize);
      reflexCoroutine = null;
    }

    private void ApplySize(float size)
    {
      if (rectTransform == null)
      {
        return;
      }

      float clamped = Mathf.Max(0.5f, size);
      rectTransform.sizeDelta = new Vector2(clamped, clamped);
    }

    private void ClampSettings()
    {
      normalSize = Mathf.Max(1f, normalSize);
      constrictedSize = Mathf.Clamp(constrictedSize, 0.5f, normalSize);
      reactionTime = Mathf.Max(0.01f, reactionTime);
    }
  }
}