using FishNet.Object;
using UnityEngine;

namespace TriageTrainer.Player
{
  public partial class PlayerController : NetworkBehaviour
  {
    [Header("Gamemode State")]
    [SerializeField] private PlayerGamemode _gamemode = PlayerGamemode.Player;
    [SerializeField] private bool _isSpectateFollowing = false;
    [SerializeField] private Transform _spectateFollowTarget;

    public PlayerGamemode CurrentGamemode => _gamemode;
    public bool IsSpectator => _gamemode == PlayerGamemode.Spectator;
    public bool IsSpectatingTarget => _isSpectateFollowing;

    internal void ApplyGamemodeServer(PlayerGamemode mode)
    {
      if (!IsServer) return;

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
        EnterSpectatorMode();
      else
        ExitSpectatorMode();

      if (_gamemode == PlayerGamemode.Spectator)
        ApplySpectatorVisibility();
      else
        ApplyPlayerVisibility();

      // Adjust local camera culling: players hide spectators, spectators see spectators.
      if (IsOwner && _camControl != null)
        _camControl.SetSpectatorLayerCulling(IsSpectator);
    }

    private void EnterSpectatorMode()
    {
      StopSpectateFollow();
      _moveDirection = Vector3.zero;
      _characterController.enabled = true;
      canMove = true;
    }

    private void ExitSpectatorMode()
    {
      StopSpectateFollow();
      _moveDirection = Vector3.zero;
      _characterController.enabled = true;
      canMove = true;
    }

    private void BeginSpectateFollow(PlayerController target)
    {
      if (!IsSpectator) return;
      if (target == null || target == this) return;
      if (_camControl == null) return;

      _spectateFollowTarget = target.CameraHolderTransform;
      _isSpectateFollowing = _spectateFollowTarget != null;

      if (_isSpectateFollowing)
        _camControl.FollowingCameraHolder = _spectateFollowTarget;
    }

    private void StopSpectateFollow()
    {
      if (_camControl != null && _cameraHolderTransform != null)
        _camControl.FollowingCameraHolder = _cameraHolderTransform;

      _isSpectateFollowing = false;
      _spectateFollowTarget = null;
    }
  }
}
