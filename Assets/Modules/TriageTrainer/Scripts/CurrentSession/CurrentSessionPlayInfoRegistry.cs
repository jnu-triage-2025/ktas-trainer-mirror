using System;
using TriageTrainer.Scripts.Player;
using TriageTrainer.Definitions;
using UnityEngine;

namespace TriageTrainer.Scripts.Connection
{
  /// <summary>
  /// SessionConnectionRegistry는 인게임 씬의 초기화 과정, 혹은 서버 접속 과정에서 접속을 시도할 서버 정보를 담습니다.
  ///
  /// 게임 실행 시 타이틀에서 서버 접속 정보를 입력하면 인게임 씬으로 전환하여 서버에 접속하는 것이 의도되고 있습니다.
  /// 이것은 개발 과정에서 인게임 씬에서 직접 실행하는 경우가 많으므로, 개발 편의성을 위한 것이기도 합니다.
  /// 만약 타이틀에서 서버 접속을 수행하도록 하면, 인게임 씬에서 직접 실행하는 경우에 대해 개별적인 NetworkManager가 필요할 수 있습니다.
  ///
  /// 따라서 모든 서버 연결 행동을 인게임 씬에서 이루어지도록 하고, 접속 정보를 이 클래스에 저장하도록 합니다.
  ///
  /// 이 컴포넌트는 항상 존재해야 하며, 프리팹에 컴포넌트로서 등록된 싱글톤 상태가 유지되어야 합니다.
  /// </summary>
  public class CurrentSessionPlayInfoRegistry : MonoBehaviour
  {
    private static CurrentSessionPlayInfoRegistry _instance;
    public static CurrentSessionPlayInfoRegistry Instance => _instance;

    /// <summary>
    /// PlayerCameraHolderTransform은 Start() 중에 RegisterLocalCameraHolder()에 의해 레지스터되어야 합니다.
    /// PlayerCameraHolderTransform에 의존성을 갖는 다른 요소가 Start() 중에 참조하도록 설계되었기 때문입니다.
    /// </summary>
    [Header("Player Information")]
    [SerializeField] private PlayerController _playerController;
    public PlayerController PlayerController => _playerController;

    [SerializeField]
    private Transform _playerCameraHolderTransform;
    public Transform PlayerCameraHolderTransform => _playerCameraHolderTransform;
    
    public event Action<PlayerController> OnPlayerRegistered;
    public event Action<PlayerController> OnPlayerUnregistered;
    public event Action<Transform> OnLocalCameraHolderRegistered;
    
    [Header("Connection Information")]
    [SerializeField]
    private SessionInformationModel _sessionInformation = new SessionInformationModel(
      DefaultsSessionInformationModel.address,
      DefaultsSessionInformationModel.port
      );

    bool _isValidatedOnce = false;
    public SessionInformationModel SessionInformation
    {
      get => _sessionInformation;
      set => _sessionInformation = value;
    }

    void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(this.gameObject);
        return;
      }
      _instance = this;
      DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
    }

    void Update()
    {
      // ValidateRegistration();
    }

    public void RegisterPlayerController(PlayerController controller)
    {
      _playerController = controller;
    }

    public void RegisterLocalCameraHolder(Transform t)
    {
      Debug.Log("Registering local camera holder");
      _playerCameraHolderTransform = t;
    }

    void ValidateRegistration()
    {
      if (_playerCameraHolderTransform == null)
      {
        Debug.LogWarning("PlayerCameraHolderTransform is not registered in CurrentSessionPlayInfoRegistry during Start(). Some functionalities may not work properly.");
      }
    }
  }
}
