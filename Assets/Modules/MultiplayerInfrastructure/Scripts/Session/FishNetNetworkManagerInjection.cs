using MultiplayerInfrastructure.Registry;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.VisualScripting;
using FishNet.Example;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Session
{
  public class FishNetNetworkManagerInjection : MonoBehaviour
  {
    private NetworkManager _networkManager;
    
    // 아래의 _serverStateAssumed, _clientStateAssumed 는 FishNet의 NetworkManager 프리팹에 부착된
    // 컴포넌트를 수정하지 않으면서 상태 변화를 관리하기 위해, 상태를 "추정"하는 용도로 활용됩니다.
    // NetworkHudCanvases의 코드에서 파생되었으며, NeetworkHudCanvases의 _serverState와 _clientState
    // 값이 변화되는 타이밍에 아래 값이 함께 바뀌도록 하여 동일한 상태를 따라가도록 합니다.

    private LocalConnectionState _serverStateAssumed = LocalConnectionState.Stopped;
    private LocalConnectionState _clientStateAssumed = LocalConnectionState.Stopped;

    private List<GameObject> _disabledHuds = new List<GameObject>();

    private static FishNetNetworkManagerInjection _instance;
    public static FishNetNetworkManagerInjection Instance => _instance;

    /// <summary>
    /// 이 컴포넌트는 NetworkManager 프리팹에 부착되는 것이 의도되었습니다.
    /// 하지만 NetworkManager 프리팹에 부착하는 것을 지양하고자 하여 별도의 게임 오브젝트에
    /// 이 컴포넌트를 부착하였다 하더라도, 인게임 하이어라키 내에서 NetworkManager를 찾아
    /// 참조하도록 설계되었습니다.
    /// </summary>
    void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(gameObject);
        return;
      }
      _instance = this;

      _networkManager = GetComponent<NetworkManager>();
      if (_networkManager.IsUnityNull())
        _networkManager = FindAnyObjectByType<NetworkManager>();
    }

    void Start()
    {
      if (FindAnyObjectByType<UnitySceneSupports.IngameScene.IngameSceneBootstrapper>() != null)
        return;

      HandleSessionInformationAlreadyConfigured();
    }

    private void HandleSessionInformationAlreadyConfigured()
    {
      if (!Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene)) return;
      
      var sessionInformation = Registry.Registry.Get<SessionInformationModel>(RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation);
      if (sessionInformation == null)
      {
        Debug.LogWarning("[FishNetNetworkManagerInjection] No session information found in Registry.Registry.");
        return;
      }

      ConfigureTransport(sessionInformation);

      StartConfiguredSession(Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer));
    }

    public void ConfigureTransport(SessionInformationModel sessionInformation)
    {
      if (_networkManager.IsUnityNull() || sessionInformation == null)
        return;

      var transport = _networkManager.TransportManager?.Transport;
      if (transport == null)
      {
        Debug.LogWarning("[FishNetNetworkManagerInjection] No transport found on NetworkManager.");
        return;
      }

      transport.SetClientAddress(sessionInformation.Address);
      transport.SetPort(sessionInformation.Port);

      Debug.Log($"[FishNetNetworkManagerInjection] Transport configured for {sessionInformation.Address}:{sessionInformation.Port}");
    }

    public void StartConfiguredSession(bool isOpeningServer)
    {
      if (isOpeningServer)
      {
        Debug.Log("[FishNetNetworkManagerInjection] Starting as Server based on session info from Intro Scene.");
        StartServer();
      }

      Debug.Log("[FishNetNetworkManagerInjection] Starting as Client based on session info from Intro Scene.");
      StartClient();

      DisableNetworkHudCanvases();
    }


    private void DisableNetworkHudCanvases()
    {
      if (_networkManager.IsUnityNull()) return;
      var huds = _networkManager.GetComponentsInChildren<NetworkHudCanvases>(true);
      foreach (var hud in huds)
      {
        hud.gameObject.SetActive(false);
        _disabledHuds.Add(hud.gameObject);
      }
    }

    private void RestoreDisabledNetworkHudCanvases()
    {
      foreach (var hudObj in _disabledHuds)
      {
        if (hudObj.IsUnityNull()) continue;
        hudObj.SetActive(true);
      }
      _disabledHuds.Clear();
    }

    public void StartServer()
    {
      if (_networkManager.IsUnityNull()) return;
      if (_serverStateAssumed == LocalConnectionState.Started)
      {
        Debug.LogWarning("[FishNetNetworkManagerInjection] StartServer called but server is already started.");
        return;
      }

      _networkManager.ServerManager.StartConnection();
      _serverStateAssumed = LocalConnectionState.Started;
    }

    public void StopServer()
    {
      if (_networkManager.IsUnityNull()) return;
      if (_serverStateAssumed == LocalConnectionState.Stopped)
      {
        Debug.LogWarning("[FishNetNetworkManagerInjection] StopServer called but server is already stopped.");
        return;
      }

      _networkManager.ServerManager.StopConnection(true);
      _serverStateAssumed = LocalConnectionState.Stopped;
    }
    
    public void StartClient()
    {
      if (_networkManager.IsUnityNull()) return;
      if (_clientStateAssumed == LocalConnectionState.Started)
      {
        Debug.LogWarning("[FishNetNetworkManagerInjection] StartClient called but client is already started.");
        return;
      }

      _networkManager.ClientManager.StartConnection();
      _clientStateAssumed = LocalConnectionState.Started;
    }

    public void StopClient()
    {
      if (_networkManager.IsUnityNull()) return;
      if (_clientStateAssumed == LocalConnectionState.Stopped)
      {
        Debug.LogWarning("[FishNetNetworkManagerInjection] StopClient called but client is already stopped.");
        return;
      }

      _networkManager.ClientManager.StopConnection();
      _clientStateAssumed = LocalConnectionState.Stopped;
    }
  }
}
