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
      if (_instance == null)
      {
        _instance = this;
        ResolveNetworkManagerInHierarchy();
        return;
      }

      if (_instance == this)
      {
        ResolveNetworkManagerInHierarchy();
        return;
      }

      // Static singleton can remain when domain reload is disabled.
      // If previous instance is inactive or effectively missing, replace it.
      if (!_instance || !_instance.isActiveAndEnabled)
      {
        _instance = this;
        ResolveNetworkManagerInHierarchy();
        return;
      }

      if (_instance != null)
      {
        bool currentResolved = ResolveNetworkManagerInHierarchy();
        bool existingResolved = _instance.ResolveNetworkManagerInHierarchy();

        if (!existingResolved && currentResolved)
        {
          _instance.enabled = false;
          _instance = this;
        }
        else
        {
          Debug.LogWarning($"[FishNetSupport] Duplicate instance detected. Keeping existing instance '{_instance.gameObject.name}' and disabling '{gameObject.name}'.");
          enabled = false;
          return;
        }
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

      if (isOpeningServer)
      {
        PrepareDeferredPlayerSpawning();
        StartServer();
      }

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
