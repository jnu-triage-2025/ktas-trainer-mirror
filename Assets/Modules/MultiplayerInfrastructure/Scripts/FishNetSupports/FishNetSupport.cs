using UnityEngine;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.FishNetSupports
{
  /// <summary>
  /// Facade component for FishNet runtime controls from scene scripts.
  /// This class is split by concerns using partial declarations.
  /// </summary>
  [DisallowMultipleComponent]
  public partial class FishNetSupport : MonoBehaviour
  {
    [Header("References")]
    [Tooltip("If empty, FishNetSupport will query NetworkManager in hierarchy at runtime.")]
    [SerializeField] private FishNet.Managing.NetworkManager networkManager;

    [Header("Network HUD")]
    [Tooltip("When true, NetworkHudCanvas objects under NetworkManager are hidden when a session starts.")]
    [SerializeField] private bool hideNetworkHudCanvasOnSessionStart = true;

    [Header("Fallback")]
    [Tooltip("When IntroScene launch data exists and IngameSceneBootstrapper is missing, start session automatically.")]
    [SerializeField] private bool autoStartFromRegistryWhenNoBootstrapper = true;

    private static FishNetSupport _instance;
    public static FishNetSupport Instance => _instance;

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Debug.LogWarning("[FishNetSupport] Duplicate instance detected. Disabling duplicate component without destroying its GameObject.");
        enabled = false;
        return;
      }

      _instance = this;
      ResolveNetworkManagerInHierarchy();
    }

    private void OnDestroy()
    {
      if (_instance == this)
        _instance = null;
    }

    private void Start()
    {
      if (!autoStartFromRegistryWhenNoBootstrapper)
        return;

      if (FindAnyObjectByType<UnitySceneSupports.IngameScene.IngameSceneBootstrapper>() != null)
        return;

      HandleSessionInformationAlreadyConfigured();
    }

    /// <summary>
    /// Applies launch info and starts host/client according to the mode.
    /// </summary>
    public bool StartSession(SessionInformationModel sessionInformation, bool isOpeningServer)
    {
      if (sessionInformation == null)
      {
        Debug.LogWarning("[FishNetSupport] StartSession called with null sessionInformation.");
        return false;
      }

      if (!ResolveNetworkManagerInHierarchy())
      {
        Debug.LogWarning("[FishNetSupport] NetworkManager was not found in hierarchy.");
        return false;
      }

      ConfigureTransport(sessionInformation);
      PrepareDeferredPlayerSpawning();

      if (isOpeningServer)
        StartServer();

      StartClient();

      if (hideNetworkHudCanvasOnSessionStart)
        SetNetworkHudCanvasVisible(false);

      return true;
    }

    private void HandleSessionInformationAlreadyConfigured()
    {
      if (!Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene))
        return;

      var sessionInformation = Registry.Registry.Get<SessionInformationModel>(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.SessionInformation);
      if (sessionInformation == null)
      {
        Debug.LogWarning("[FishNetSupport] No session information found in runtime registry.");
        return;
      }

      var isOpeningServer = Registry.Registry.Get<bool>(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.IsOpeningServer);

      StartSession(sessionInformation, isOpeningServer);
    }
  }
}
