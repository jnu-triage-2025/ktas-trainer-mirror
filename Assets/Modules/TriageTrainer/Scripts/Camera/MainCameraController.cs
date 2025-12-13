using TriageTrainer.Camera;
using TriageTrainer.Player;
using TriageTrainer.Registry;
using Unity.VisualScripting;
using UnityEngine;

namespace TriageTrainer.System.Prefabs.Camera
{
  /// <summary>
  /// 메인 카메라 컨트롤러는 메인 카메라 제어를 위해 작성되었습니다.
  /// 이 컨트롤러는 게임 시작 시 자동으로 인스턴스화하여 싱글톤 오브젝트로 동작합니다.
  /// </summary>
  [RequireComponent(typeof(NearbyInteractablesDetector))]
  public class MainCameraController : MonoBehaviour
  {
    [Header("Configuration")]
    [SerializeField] private CameraViewMode _currentViewMode = CameraViewMode.ThirdPerson;

    [SerializeField] private float _firstPersonDistance = 0.5f;
    [SerializeField] private float _thirdPersonDistance = 5.0f;
    [SerializeField] private float _cameraModeTransitionSpeed = 5.0f;

    [Header("References")]
    [SerializeField] private Transform _followingCameraHolder;
    [SerializeField] private UnityEngine.Camera _camera;
    [SerializeField] private NearbyInteractablesDetector _nearbyInteractablesDetector;

    [Header("State")]
    [SerializeField] private float _currentDistance;
    [SerializeField] private float _targetDistance;
    
    private static MainCameraController _instance;

    public static MainCameraController Instance
    {
      get
      {
        if (_instance.IsUnityNull())
        {
          GameObject go = new GameObject("MainCameraController");
          DontDestroyOnLoad(go);
          _instance = go.AddComponent<MainCameraController>();
        }

        return _instance;
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

    public Transform FollowingCameraHolder
    {
      get => _followingCameraHolder;
      set => _followingCameraHolder = value;
    }
    
    void Awake()
    {
      _camera = UnityEngine.Camera.main;
      _nearbyInteractablesDetector = GetComponent<NearbyInteractablesDetector>();
      CurrentSessionPlayInfoRegistry.Register(this);
    }

    void Start()
    {
      UpdateCameraDistance();
      _currentDistance = _targetDistance;
    }

    void LateUpdate()
    {
      if (_followingCameraHolder.IsUnityNull()) return;

      _camera.transform.position = _followingCameraHolder.position;
      _camera.transform.rotation = _followingCameraHolder.rotation;

      SmoothDistanceTransition();
      ApplyDistanceOffset();
    }

    void OnValidate()
    {
      if (!Application.isPlaying) return;
      UpdateCameraDistance();
      SmoothDistanceTransition();
      ApplyDistanceOffset();
    }

    private void UpdateCameraDistance()
    {
      _targetDistance = _currentViewMode == CameraViewMode.FirstPerson
        ? _firstPersonDistance
        : _thirdPersonDistance;
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
      if (_currentDistance > 0.01f)
      {
        _camera.transform.position -= _camera.transform.forward * _currentDistance;
      }
    }
    
    public void SetTarget(PlayerController playerController)
    {
      if (playerController.IsUnityNull()) return;
      if (playerController.CameraHolderTransform.IsUnityNull()) return;
      _followingCameraHolder = playerController.CameraHolderTransform;
    }
  }
}
