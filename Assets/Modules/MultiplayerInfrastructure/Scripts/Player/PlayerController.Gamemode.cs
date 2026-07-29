using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Gamemode State")]
    [SerializeField] private PlayerGamemode _gamemode = PlayerGamemode.Player;
    [SerializeField] private bool _isSpectateFollowing = false;
    [SerializeField] private Transform _spectateFollowTarget;

    public PlayerGamemode CurrentGamemode => _gamemode;
    public bool IsSpectator => _gamemode == PlayerGamemode.Spectator;
    public bool IsSpectatingTarget => _isSpectateFollowing;

    private Renderer[] _bodyRenderers;
    private Renderer[] _spectatorMarkerRenderers;

    internal void ApplyGamemodeServer(PlayerGamemode mode)
    {
      if (!IsServerInitialized) return;

      _gamemode = mode;
      RpcApplyGamemode(mode);
      ApplyGamemodeLocal(mode);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcApplyGamemode(PlayerGamemode mode)
    {
      ApplyGamemodeLocal(mode);
    }

    private void ApplyGamemodeLocal(PlayerGamemode mode)
    {
      _gamemode = mode;

      if (_gamemode == PlayerGamemode.Spectator)
      {
        EnterSpectatorMode();
        ApplySpectatorVisibility();
      }
      else
      {
        ExitSpectatorMode();
        ApplyPlayerVisibility();
      }

      // Adjust local camera culling: players hide spectators, spectators see spectators.
      if (IsOwner && _camControl != null)
        _camControl.SetSpectatorLayerCulling(IsSpectator);
    }

    private void EnterSpectatorMode()
    {
      StopSpectateFollow();

      // PlayerController.GameObject
      SetMarkerVisibility(_spectatorMarkerObject, true, ref _spectatorMarkerRenderers, "SpectatorMarker");
      SetMarkerVisibility(_bodyObject, false, ref _bodyRenderers, "Body");

      _moveDirection = Vector3.zero;
      _characterController.enabled = true;
      canMove = true;
    }

    private void ExitSpectatorMode()
    {
      StopSpectateFollow();

      // PlayerController.GameObject
      SetMarkerVisibility(_spectatorMarkerObject, false, ref _spectatorMarkerRenderers, "SpectatorMarker");
      SetMarkerVisibility(_bodyObject, true, ref _bodyRenderers, "Body");

      _moveDirection = Vector3.zero;
      _characterController.enabled = true;
      canMove = true;
    }

    private void SetMarkerVisibility(GameObject markerRoot, bool visible, ref Renderer[] cache, string markerName)
    {
      if (markerRoot == null)
      {
        Debug.LogWarning($"[PlayerController] {markerName} object is not assigned.", this);
        return;
      }

      if (cache == null || cache.Length == 0)
      {
        cache = markerRoot.GetComponentsInChildren<Renderer>(true);
      }

      if (cache == null || cache.Length == 0)
      {
        return;
      }

      for (int i = 0; i < cache.Length; i++)
      {
        var renderer = cache[i];
        if (renderer != null)
          renderer.enabled = visible;
      }
    }

    private void BeginSpectateFollow(PlayerController target)
    {
      if (!IsSpectator) return;
      if (target == null || target == this) return;
      if (_camControl == null) return;

      _spectateFollowTarget = target.CameraAttachPoint?.PivotTransform;
      _isSpectateFollowing = _spectateFollowTarget != null;

      if (_isSpectateFollowing)
        _camControl.FollowingCameraHolder = _spectateFollowTarget;
    }

    private void StopSpectateFollow()
    {
      if (_camControl != null && CameraAttachPoint?.PivotTransform != null)
        _camControl.FollowingCameraHolder = CameraAttachPoint.PivotTransform;

      _isSpectateFollowing = false;
      _spectateFollowTarget = null;
    }
  }
}
