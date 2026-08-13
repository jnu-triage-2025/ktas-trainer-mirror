using UnityEngine;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.Camera
{
  /// <summary>
  /// 실제 <see cref="UnityEngine.Camera"/>를 감싸고 특정 <see cref="CameraAttachPoint"/>에 부착되어
  /// 그 지점을 추종하는 "카메라 래퍼"입니다.
  ///
  /// 이전 구조에서는 <c>MainCameraController</c>가 플레이어 자식으로 생성된 "CameraHolder" Transform을
  /// 직접 <c>_followingCameraHolder</c>로 참조하여, "카메라가 붙는 대상"과 "카메라를 들고 다니는 주체"의
  /// 책임이 분리되어 있지 않았습니다.
  ///
  /// 이 클래스는 "카메라를 들고 다니는 주체" 책임만을 담당합니다.
  /// - <see cref="AttachTo"/> / <see cref="Detach"/>로 어느 부착점을 따라갈지 결정합니다.
  /// - 매 프레임 <see cref="Follow"/>에서 부착점의 위치/회전을 카메라에 복사하고,
  ///   시점 모드에 따른 거리 오프셋과 벽 충돌 보정을 적용합니다.
  ///
  /// <see cref="MonoBehaviour"/>가 아닌 직렬화 가능한 순수 클래스로 두어,
  /// 소유자(<c>MainCameraController</c>)의 인스펙터에서 설정값을 그대로 편집할 수 있게 합니다.
  /// </summary>
  [System.Serializable]
  public class CameraHolder
  {
    [Header("View Mode")]
    [SerializeField] private CameraViewMode _currentViewMode = CameraViewMode.ThirdPerson;
    [SerializeField] private float _firstPersonDistance = 0f;
    [SerializeField] private float _firstPersonEyeHeightOffset = 0.6f;
    [SerializeField] private float _thirdPersonDistance = 3.0f;
    [SerializeField] private float _cameraModeTransitionSpeed = 5.0f;

    [Header("Third Person Distance Zoom")]
    [Tooltip("3인칭 시점에서 사용자가 조정할 수 있는 최소 거리")]
    [SerializeField] private float _minThirdPersonDistance = 1.0f;
    [Tooltip("3인칭 시점에서 사용자가 조정할 수 있는 최대 거리")]
    [SerializeField] private float _maxThirdPersonDistance = 8.0f;
    [Tooltip("거리 조정(줌) 1스텝당 변화량")]
    [SerializeField] private float _distanceZoomStep = 0.5f;

    [Header("Camera Collision")]
    [Tooltip("카메라가 벽을 뚫고 보지 못하도록 충돌 처리를 활성화합니다.")]
    [SerializeField] private bool _enableCollision = true;
    [Tooltip("카메라 충돌 검사에 사용할 구체 반지름. 카메라 근평면 크기에 맞춥니다.")]
    [SerializeField] private float _collisionProbeRadius = 0.2f;
    [Tooltip("벽과 카메라 사이에 확보할 여유 간격")]
    [SerializeField] private float _collisionBuffer = 0.1f;
    [Tooltip("카메라 충돌 검사 대상 레이어. 플레이어/트리거 등은 제외해야 합니다.")]
    [SerializeField] private LayerMask _collisionMask = ~0;

    [Header("References")]
    [Tooltip("현재 부착되어 추종 중인 부착점의 피벗 Transform. 런타임에 결정됩니다.")]
    [SerializeField] private Transform _followingPivot;
    [SerializeField] private UnityEngine.Camera _camera;
    [SerializeField] private string _spectatorLayerName = "Spectator";

    [Header("State")]
    [SerializeField] private float _currentDistance;
    [SerializeField] private float _targetDistance;
    [Tooltip("사용자가 조정한 3인칭 목표 거리. 벽 충돌 전 원하는 거리입니다.")]
    [SerializeField] private float _desiredThirdPersonDistance;
    [SerializeField] private bool _desiredDistanceInitialized = false;
    [SerializeField] private int _baseCullingMask;
    [SerializeField] private bool _baseMaskInitialized = false;

    // 충돌 검사에서 무시할 콜라이더(추종 대상 플레이어 자신). 직렬화 대상이 아니다.
    private readonly System.Collections.Generic.HashSet<Collider> _ignoredColliders = new();
    // SphereCastAll 결과 버퍼(할당 최소화).
    private readonly RaycastHit[] _collisionHits = new RaycastHit[16];

    /// <summary>
    /// 현재 부착되어 추종 중인 부착점의 피벗 Transform입니다. 부착되지 않았다면 null입니다.
    /// 관전 추종 등에서 다른 플레이어의 부착점을 직접 지정할 때 사용합니다.
    /// 이 setter로 대상을 바꾸면 이전 대상의 충돌 무시 콜라이더 목록은 초기화됩니다.
    /// (관전 대상 등 자기 자신이 아닌 대상을 따라갈 때는 자기 콜라이더 무시가 불필요/부정확하므로)
    /// </summary>
    public Transform FollowingPivot
    {
      get => _followingPivot;
      set
      {
        if (_followingPivot != value)
          _ignoredColliders.Clear();
        _followingPivot = value;
      }
    }

    public UnityEngine.Camera Camera
    {
      get
      {
        EnsureCamera();
        return _camera;
      }
    }

    public CameraViewMode CurrentViewMode
    {
      get => _currentViewMode;
      set
      {
        _currentViewMode = value;
        UpdateCameraDistance();
      }
    }

    /// <summary>
    /// 지정한 부착점에 카메라를 부착합니다. 이후 <see cref="Follow"/>가 이 지점을 추종합니다.
    /// 해당 부착점을 소유한 플레이어의 콜라이더는 카메라 충돌 검사에서 무시됩니다.
    /// </summary>
    public void AttachTo(CameraAttachPoint attachPoint)
    {
      if (attachPoint.IsUnityNull()) return;
      _followingPivot = attachPoint.PivotTransform;

      _ignoredColliders.Clear();
      var colliders = attachPoint.OwnerColliders;
      if (colliders != null)
      {
        for (int i = 0; i < colliders.Length; i++)
        {
          if (colliders[i] != null)
            _ignoredColliders.Add(colliders[i]);
        }
      }
    }

    /// <summary>부착을 해제합니다. 이후 카메라는 추종을 멈춥니다.</summary>
    public void Detach()
    {
      _followingPivot = null;
      _ignoredColliders.Clear();
    }

    /// <summary>
    /// 소유자의 초기화 시점에 호출하여 거리 상태를 현재 모드에 맞춰 즉시 확정합니다.
    /// </summary>
    public void Initialize()
    {
      UpdateCameraDistance();
      _currentDistance = _targetDistance;
    }

    /// <summary>
    /// 매 프레임(LateUpdate) 호출하여 카메라를 부착점에 맞춰 이동/회전시키고
    /// 거리 오프셋 및 충돌 보정을 적용합니다.
    /// </summary>
    public void Follow()
    {
      if (_followingPivot.IsUnityNull()) return;

      EnsureCamera();
      if (_camera.IsUnityNull()) return;

      _camera.transform.position = _followingPivot.position;
      _camera.transform.rotation = _followingPivot.rotation;

      // 1인칭은 피벗(부착점) 기준 눈높이로 소폭 올린다.
      if (_currentViewMode == CameraViewMode.FirstPerson)
        _camera.transform.position += _camera.transform.up * _firstPersonEyeHeightOffset;

      SmoothDistanceTransition();
      ApplyDistanceOffset();
    }

    /// <summary>
    /// 에디터에서 인스펙터 값이 변경되었을 때(재생 중) 즉시 반영하기 위한 갱신입니다.
    /// </summary>
    public void RefreshFromInspector()
    {
      EnsureCamera();
      if (_camera.IsUnityNull()) return;
      UpdateCameraDistance();
      SmoothDistanceTransition();
      ApplyDistanceOffset();
    }

    private void UpdateCameraDistance()
    {
      EnsureDesiredDistanceInitialized();

      _targetDistance = _currentViewMode == CameraViewMode.FirstPerson
        ? Mathf.Max(0f, _firstPersonDistance)
        : _desiredThirdPersonDistance;
    }

    private void EnsureDesiredDistanceInitialized()
    {
      if (_desiredDistanceInitialized) return;

      _desiredThirdPersonDistance = Mathf.Clamp(
        _thirdPersonDistance,
        _minThirdPersonDistance,
        _maxThirdPersonDistance
      );
      _desiredDistanceInitialized = true;
    }

    /// <summary>
    /// 3인칭 POV 거리를 스텝 단위로 조정합니다.
    /// 양수(steps &gt; 0)는 카메라를 플레이어에 가깝게, 음수는 멀게 합니다.
    /// </summary>
    /// <param name="steps">스크롤 스텝 수. 각 스텝은 _distanceZoomStep 만큼 변화합니다.</param>
    public void AdjustThirdPersonDistance(float steps)
    {
      EnsureDesiredDistanceInitialized();

      _desiredThirdPersonDistance = Mathf.Clamp(
        _desiredThirdPersonDistance - steps * _distanceZoomStep,
        _minThirdPersonDistance,
        _maxThirdPersonDistance
      );

      UpdateViewModeForThirdPersonDistance();
      UpdateCameraDistance();
    }

    /// <summary>
    /// 3인칭 POV 거리를 절대값으로 설정합니다. 허용 범위로 clamp됩니다.
    /// </summary>
    public void SetThirdPersonDistance(float distance)
    {
      _desiredThirdPersonDistance = Mathf.Clamp(
        distance,
        _minThirdPersonDistance,
        _maxThirdPersonDistance
      );
      _desiredDistanceInitialized = true;

      UpdateViewModeForThirdPersonDistance();
      UpdateCameraDistance();
    }

    // 3인칭 POV를 최소 거리까지 당기면 1인칭으로 전환합니다.
    // 1인칭 상태에서 POV 값을 다시 늘리면 3인칭으로 복귀합니다.
    private void UpdateViewModeForThirdPersonDistance()
    {
      bool isAtMinimumDistance =
        _desiredThirdPersonDistance <= _minThirdPersonDistance + Mathf.Epsilon;

      if (isAtMinimumDistance)
        _currentViewMode = CameraViewMode.FirstPerson;
      else if (_currentViewMode == CameraViewMode.FirstPerson)
        _currentViewMode = CameraViewMode.ThirdPerson;
    }

    /// <summary>
    /// 현재 사용자가 설정한 3인칭 POV 거리(벽 충돌 반영 전)입니다.
    /// </summary>
    public float DesiredThirdPersonDistance
    {
      get
      {
        EnsureDesiredDistanceInitialized();
        return _desiredThirdPersonDistance;
      }
    }

    /// <summary>3인칭 POV 거리 조정의 허용 최소값입니다.</summary>
    public float MinThirdPersonDistance => _minThirdPersonDistance;

    /// <summary>3인칭 POV 거리 조정의 허용 최대값입니다.</summary>
    public float MaxThirdPersonDistance => _maxThirdPersonDistance;

    private void SmoothDistanceTransition()
    {
      _currentDistance = Mathf.Lerp(
        _currentDistance,
        _targetDistance,
        Time.deltaTime * _cameraModeTransitionSpeed
      );
    }

    private void ApplyDistanceOffset()
    {
      if (_camera.IsUnityNull()) return;
      if (_currentDistance <= 0.01f) return;

      float distance = ResolveCollisionAdjustedDistance(_currentDistance);
      if (distance <= 0.01f) return;

      _camera.transform.position -= _camera.transform.forward * distance;
    }

    /// <summary>
    /// 카메라 부착점(피벗)에서 뒤로 물러나는 경로에 벽/장애물이 있는지 검사하여,
    /// 카메라가 벽을 뚫지 않도록 실제 이동 가능한 거리로 제한합니다.
    /// </summary>
    private float ResolveCollisionAdjustedDistance(float desiredDistance)
    {
      if (!_enableCollision) return desiredDistance;
      if (_followingPivot.IsUnityNull()) return desiredDistance;

      Vector3 pivot = _followingPivot.position;
      Vector3 dir = -_camera.transform.forward;
      float castDistance = desiredDistance + _collisionProbeRadius;

      // 추종 대상 플레이어 자신의 콜라이더는 무시해야 한다.
      // (부착점이 플레이어 콜라이더 내부/근처에서 시작되므로, 무시하지 않으면
      //  카메라가 항상 0 거리로 당겨져 이동 중 1인칭으로 튀는 현상이 발생한다.)
      int count = Physics.SphereCastNonAlloc(
        pivot,
        _collisionProbeRadius,
        dir,
        _collisionHits,
        castDistance,
        _collisionMask,
        QueryTriggerInteraction.Ignore);

      float nearest = desiredDistance;
      for (int i = 0; i < count; i++)
      {
        RaycastHit hit = _collisionHits[i];

        // 시작점이 콜라이더와 겹쳐 hit.distance가 0인 경우는 무시한다.
        if (hit.distance <= 0f) continue;
        if (_ignoredColliders.Contains(hit.collider)) continue;

        float allowed = hit.distance - _collisionBuffer;
        if (allowed < nearest)
          nearest = allowed;
      }

      return Mathf.Clamp(nearest, 0f, desiredDistance);
    }

    /// <summary>
    /// 로컬 카메라에서 관전자 레이어 가시성을 토글합니다.
    /// 플레이어는 관전자를 볼 수 없고, 관전자는 볼 수 있어야 합니다.
    /// </summary>
    public void SetSpectatorLayerCulling(bool enableSpectator)
    {
      EnsureCamera();
      if (_camera == null) return;

      int spectatorLayer = LayerMask.NameToLayer(_spectatorLayerName);
      if (spectatorLayer < 0) return;

      int mask = _baseCullingMask;
      if (enableSpectator)
        mask |= 1 << spectatorLayer;
      else
        mask &= ~(1 << spectatorLayer);

      _camera.cullingMask = mask;
    }

    private void EnsureCamera()
    {
      if (_camera.IsUnityNull())
        _camera = UnityEngine.Camera.main ?? Object.FindFirstObjectByType<UnityEngine.Camera>();

      if (!_camera.IsUnityNull() && !_baseMaskInitialized)
      {
        _baseCullingMask = _camera.cullingMask;
        _baseMaskInitialized = true;
      }
    }
  }
}
