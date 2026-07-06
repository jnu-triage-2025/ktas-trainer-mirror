using FishNet.Object;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Camera
{
  /// <summary>
  /// 메인 카메라 컨트롤러는 메인 카메라 제어를 위해 작성되었습니다.
  /// 이 컨트롤러는 게임 시작 시 자동으로 인스턴스화하여 싱글톤 오브젝트로 동작합니다.
  /// </summary>
  [RequireComponent(typeof(NearbyInteractablesDetector))]
  public class MainCameraController : NetworkBehaviour
  {
    [Header("Configuration")]
    [SerializeField] private CameraViewMode _currentViewMode = CameraViewMode.ThirdPerson;

    [SerializeField] private float _firstPersonDistance = 0.5f;
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
    [SerializeField] private Transform _followingCameraHolder;
    [SerializeField] private UnityEngine.Camera _camera;
    public UnityEngine.Camera Camera
    {
      get
      {
        EnsureCamera();
        return _camera;
      }
    }

    [SerializeField] private NearbyInteractablesDetector _nearbyInteractablesDetector;
    [SerializeField] private InteractableObjectHintUIController _interactableHintUIController;
    [SerializeField] private string _spectatorLayerName = "Spectator";

    [Header("State")]
    [SerializeField] private float _currentDistance;
    [SerializeField] private float _targetDistance;
    [Tooltip("사용자가 조정한 3인칭 목표 거리. 벽 충돌 전 원하는 거리입니다.")]
    [SerializeField] private float _desiredThirdPersonDistance;
    [SerializeField] private bool _desiredDistanceInitialized = false;
    [SerializeField] private int _baseCullingMask;
    [SerializeField] private bool _baseMaskInitialized = false;
    
    private static MainCameraController _instance;

    public static MainCameraController Instance => _instance;

    void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(this.gameObject);
        return;
      }

      _instance = this;
      Registry.Registry.Register(RegistryType.Service, Registry.Registry.TypeKey<MainCameraController>(), this);
    }

    private void OnDestroy()
    {
      if (_instance == this)
        _instance = null;

      Registry.Registry.Unregister(RegistryType.Service, Registry.Registry.TypeKey<MainCameraController>());
    }

    void Start()
    {
      _nearbyInteractablesDetector = GetComponent<NearbyInteractablesDetector>();
      _interactableHintUIController = GetComponent<InteractableObjectHintUIController>();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();

      if (!IsOwner) return;

      UpdateCameraDistance();
      _currentDistance = _targetDistance;
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

    public Transform FollowingCameraHolder
    {
      get => _followingCameraHolder;
      set => _followingCameraHolder = value;
    }

    void LateUpdate()
    {
      if (_followingCameraHolder.IsUnityNull()) return;

      EnsureCamera();
      if (_camera.IsUnityNull()) return;

      _camera.transform.position = _followingCameraHolder.position;
      _camera.transform.rotation = _followingCameraHolder.rotation;

      SmoothDistanceTransition();
      ApplyDistanceOffset();
    }

    new void OnValidate()
    {
      if (!Application.isPlaying) return;
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
        ? _firstPersonDistance
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

      UpdateCameraDistance();
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
    /// 카메라 홀더(피벗)에서 뒤로 물러나는 경로에 벽/장애물이 있는지 검사하여,
    /// 카메라가 벽을 뚫지 않도록 실제 이동 가능한 거리로 제한합니다.
    /// </summary>
    private float ResolveCollisionAdjustedDistance(float desiredDistance)
    {
      if (!_enableCollision) return desiredDistance;
      if (_followingCameraHolder.IsUnityNull()) return desiredDistance;

      Vector3 pivot = _followingCameraHolder.position;
      Vector3 dir = -_camera.transform.forward;
      float castDistance = desiredDistance + _collisionProbeRadius;

      if (Physics.SphereCast(
            pivot,
            _collisionProbeRadius,
            dir,
            out RaycastHit hit,
            castDistance,
            _collisionMask,
            QueryTriggerInteraction.Ignore))
      {
        float allowed = hit.distance - _collisionBuffer;
        return Mathf.Clamp(allowed, 0f, desiredDistance);
      }

      return desiredDistance;
    }


    /// <summary>
    /// Toggle spectator layer visibility on the local camera.
    /// Players should not see spectators; spectators should.
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
    
    public void SetTarget(PlayerController playerController)
    {
      if (playerController.IsUnityNull()) return;
      if (!playerController.IsOwner) return; // Only bind to the local owner's player
      if (playerController.CameraHolderTransform.IsUnityNull()) return;
      _followingCameraHolder = playerController.CameraHolderTransform;
    }

    private void EnsureCamera()
  {
      if (_camera.IsUnityNull())
        _camera = UnityEngine.Camera.main ?? FindFirstObjectByType<UnityEngine.Camera>();

      if (!_camera.IsUnityNull() && !_baseMaskInitialized)
      {
        _baseCullingMask = _camera.cullingMask;
        _baseMaskInitialized = true;
      }
    }
  }
}
