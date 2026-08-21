using FishNet.Object;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Camera
{
  /// <summary>
  /// 메인 카메라 컨트롤러는 메인 카메라 제어를 위해 작성되었습니다.
  /// 이 컨트롤러는 게임 시작 시 자동으로 인스턴스화하여 싱글톤 오브젝트로 동작합니다.
  ///
  /// 카메라를 실제로 들고 다니며 부착점을 추종하는 로직은 <see cref="CameraHolder"/>가 담당합니다.
  /// 이 컨트롤러는 네트워크/싱글톤 수명주기와 부착 대상(플레이어) 바인딩을 담당하고,
  /// 카메라 제어 API는 내부 <see cref="_holder"/>에 위임합니다.
  /// </summary>
  [RequireComponent(typeof(NearbyInteractablesDetector))]
  public class MainCameraController : NetworkBehaviour
  {
    [Header("Camera Holder")]
    [Tooltip("실제 카메라를 감싸고 부착점을 추종하는 래퍼입니다.")]
    [SerializeField] private CameraHolder _holder = new CameraHolder();

    [SerializeField] private NearbyInteractablesDetector _nearbyInteractablesDetector;
    [SerializeField] private InteractableObjectHintUIController _interactableHintUIController;

    private static MainCameraController _instance;

    public static MainCameraController Instance => _instance;

    /// <summary>카메라를 들고 다니는 래퍼입니다.</summary>
    public CameraHolder Holder => _holder;

    public UnityEngine.Camera Camera => _holder.Camera;

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

      _holder.Initialize();

      // 카메라는 네트워크 스폰 이후에 초기화되므로, 먼저 생성된 POV 설정 서비스를
      // 여기서 다시 적용해야 저장된 거리가 실제 소유자 카메라에 반영된다.
      Registry.Registry.Get<CameraDistancePreferenceService>(
          RegistryType.Service, Registry.Registry.TypeKey<CameraDistancePreferenceService>())
        ?.ReapplyToCamera();

      // 네트워크 스폰 뒤 생성된 소유자 카메라에도 저장된 FOV/후처리 설정을 적용한다.
      Registry.Registry.Get<TexturePerformanceService>(
          RegistryType.Service, Registry.Registry.TypeKey<TexturePerformanceService>())
        ?.ReapplySceneSettings();
    }

    public CameraViewMode CurrentViewMode
    {
      get => _holder.CurrentViewMode;
      set => _holder.CurrentViewMode = value;
    }

    /// <summary>
    /// 현재 카메라가 추종 중인 부착점의 Transform입니다.
    /// 관전 추종 등에서 다른 플레이어의 부착점으로 직접 지정할 수 있습니다.
    /// </summary>
    public Transform FollowingCameraHolder
    {
      get => _holder.FollowingPivot;
      set => _holder.FollowingPivot = value;
    }

    public float DesiredThirdPersonDistance => _holder.DesiredThirdPersonDistance;

    /// <summary>3인칭 POV 거리 조정의 허용 최소값입니다.</summary>
    public float MinThirdPersonDistance => _holder.MinThirdPersonDistance;

    /// <summary>3인칭 POV 거리 조정의 허용 최대값입니다.</summary>
    public float MaxThirdPersonDistance => _holder.MaxThirdPersonDistance;

    void LateUpdate()
    {
      _holder.Follow();
    }

    new void OnValidate()
    {
      if (!Application.isPlaying) return;
      _holder.RefreshFromInspector();
    }

    public void AdjustThirdPersonDistance(float steps) => _holder.AdjustThirdPersonDistance(steps);

    public void SetThirdPersonDistance(float distance) => _holder.SetThirdPersonDistance(distance);

    public void SetSpectatorLayerCulling(bool enableSpectator) => _holder.SetSpectatorLayerCulling(enableSpectator);

    /// <summary>
    /// 로컬 소유자 플레이어의 카메라 부착점에 카메라를 부착합니다.
    /// </summary>
    public void SetTarget(PlayerController playerController)
    {
      if (playerController.IsUnityNull()) return;
      if (!playerController.IsOwner) return; // Only bind to the local owner's player
      _holder.AttachTo(playerController.CameraAttachPoint);
    }
  }
}
