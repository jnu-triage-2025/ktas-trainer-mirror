using System.Collections.Generic;
using FishNet.Component.Spawning;
using FishNet.Example;
using FishNet.Managing;
using FishNet.Transporting;
using MultiplayerInfrastructure.Session;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.FishNetSupports
{
  public partial class FishNetSupport
  {
    private LocalConnectionState _serverStateAssumed = LocalConnectionState.Stopped;
    private LocalConnectionState _clientStateAssumed = LocalConnectionState.Stopped;
    private readonly List<GameObject> _hiddenHudObjects = new List<GameObject>();

    /// <summary>
    /// Tries to resolve a NetworkManager from this object first, then scene hierarchy.
    /// </summary>
    public bool ResolveNetworkManagerInHierarchy()
    {
      if (!networkManager.IsUnityNull())
        return true;

      networkManager = GetComponent<NetworkManager>();
      if (!networkManager.IsUnityNull())
        return true;

      networkManager = FindAnyObjectByType<NetworkManager>();
      return !networkManager.IsUnityNull();
    }

    public void SetNetworkHudCanvasVisible(bool visible)
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (!visible)
      {
        HideNetworkHudCanvases();
        return;
      }

      RestoreHiddenNetworkHudCanvases();
    }

    public void ConfigureTransport(SessionInformationModel sessionInformation)
    {
      if (sessionInformation == null)
        return;

      if (!ResolveNetworkManagerInHierarchy())
        return;

      var transport = networkManager.TransportManager?.Transport;
      if (transport == null)
      {
        Debug.LogWarning("[FishNetSupport] No transport found on NetworkManager.");
        return;
      }

      transport.SetClientAddress(sessionInformation.Address);
      transport.SetPort(sessionInformation.Port);

      Debug.Log($"[FishNetSupport] Transport configured for {sessionInformation.Address}:{sessionInformation.Port}");
    }

    public void PrepareDeferredPlayerSpawning()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      var playerSpawners = networkManager.GetComponentsInChildren<PlayerSpawner>(true);
      for (int i = 0; i < playerSpawners.Length; i++)
      {
        var playerSpawner = playerSpawners[i];
        if (playerSpawner == null)
          continue;

        var deferredSpawner = playerSpawner.GetComponent<FishNetDeferredPlayerSpawner>();
        if (deferredSpawner == null)
          deferredSpawner = playerSpawner.gameObject.AddComponent<FishNetDeferredPlayerSpawner>();

        deferredSpawner.ConfigureFromPlayerSpawner(playerSpawner);
        playerSpawner.enabled = false;
      }
    }

    public void StartServer()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_serverStateAssumed == LocalConnectionState.Started)
      {
        Debug.LogWarning("[FishNetSupport] StartServer called but server is already started.");
        return;
      }

      networkManager.ServerManager.StartConnection();
      _serverStateAssumed = LocalConnectionState.Started;
    }

    public void StopServer()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_serverStateAssumed == LocalConnectionState.Stopped)
      {
        Debug.LogWarning("[FishNetSupport] StopServer called but server is already stopped.");
        return;
      }

      networkManager.ServerManager.StopConnection(true);
      _serverStateAssumed = LocalConnectionState.Stopped;
    }

    public void StartClient()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_clientStateAssumed == LocalConnectionState.Started)
      {
        Debug.LogWarning("[FishNetSupport] StartClient called but client is already started.");
        return;
      }

      networkManager.ClientManager.StartConnection();
      _clientStateAssumed = LocalConnectionState.Started;
    }

    public void StopClient()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_clientStateAssumed == LocalConnectionState.Stopped)
      {
        Debug.LogWarning("[FishNetSupport] StopClient called but client is already stopped.");
        return;
      }

      networkManager.ClientManager.StopConnection();
      _clientStateAssumed = LocalConnectionState.Stopped;
    }

    private void HideNetworkHudCanvases()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      var huds = networkManager.GetComponentsInChildren<NetworkHudCanvases>(true);
      foreach (var hud in huds)
      {
        var hudObject = hud.gameObject;
        if (!hudObject.activeSelf)
          continue;

        hudObject.SetActive(false);
        _hiddenHudObjects.Add(hudObject);
      }
    }

    private void RestoreHiddenNetworkHudCanvases()
    {
      foreach (var hudObject in _hiddenHudObjects)
      {
        if (hudObject.IsUnityNull())
          continue;

        hudObject.SetActive(true);
      }

      _hiddenHudObjects.Clear();
    }
  }
}
