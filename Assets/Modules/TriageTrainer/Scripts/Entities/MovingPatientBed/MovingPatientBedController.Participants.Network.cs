using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 들것 손잡이 참가자를 서버에서 배정하고, 각 참가자의 입력으로만 서버가 침대를 이동시키는 부분.
  /// 슬롯은 client id를 저장하므로 한 클라이언트가 두 손잡이를 점유하는 것을 막을 수 있다.
  /// </summary>
  public partial class MovingPatientBedController
  {
    private const int InvalidParticipantClientId = -1;
    private readonly SyncVar<int> _participantHandle0ClientId = new(InvalidParticipantClientId);
    private readonly SyncVar<int> _participantHandle1ClientId = new(InvalidParticipantClientId);
    private readonly Dictionary<int, Vector2> _authoritativeParticipantInputs = new();
    private bool _participantTogglePending;

    private void OnStartClient_ParticipantNetwork()
    {
      _participantHandle0ClientId.OnChange += OnParticipantHandleChanged;
      _participantHandle1ClientId.OnChange += OnParticipantHandleChanged;
      ApplyLocalAuthoritativeParticipant();
    }

    private void OnStopClient_ParticipantNetwork()
    {
      _participantHandle0ClientId.OnChange -= OnParticipantHandleChanged;
      _participantHandle1ClientId.OnChange -= OnParticipantHandleChanged;
      ClearLocalAuthoritativeParticipant();
    }

    private void OnParticipantHandleChanged(int previous, int next, bool asServer)
    {
      ApplyLocalAuthoritativeParticipant();
    }

    private void RequestAuthoritativeParticipantToggle(PlayerController player, Transform interactor)
    {
      if (player == null)
        return;

      if (!IsClientStarted && !IsServerStarted)
      {
        ToggleParticipantOffline(player, interactor);
        return;
      }

      if (player.Owner == null || !player.Owner.IsValid)
        return;

      if (IsServerStarted)
      {
        ToggleParticipantOnServer(player.Owner.ClientId);
        return;
      }

      if (_participantTogglePending)
        return;

      _participantTogglePending = true;
      CmdToggleParticipant(sender: null);
    }

    private void ToggleParticipantOffline(PlayerController player, Transform interactor)
    {
      int key = interactor.GetInstanceID();
      if (_participants.TryGetValue(key, out var existing))
      {
        ExitMovingMode(player, existing);
        _participants.Remove(key);
        return;
      }

      if (!TryOccupyNextPlayerAttachPoint(player, out Transform attachPoint))
      {
        ShowThrottledMessage(interactor, "침대의 모든 이동 위치가 이미 사용 중입니다.");
        return;
      }

      var participant = new RidingParticipant
      {
        Player = player,
        Interactor = interactor,
        AttachPoint = attachPoint,
        LastActionbarRefreshAt = -100f,
      };
      _participants[key] = participant;
      EnterMovingMode(participant);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdToggleParticipant(NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid)
        return;

      ToggleParticipantOnServer(sender.ClientId);
    }

    private void ToggleParticipantOnServer(int clientId)
    {
      if (clientId < 0)
        return;

      int handle = FindParticipantHandle(clientId);
      if (handle >= 0)
      {
        SetParticipantHandle(handle, InvalidParticipantClientId);
        _authoritativeParticipantInputs.Remove(clientId);
      }
      else
      {
        handle = FindFreeParticipantHandle();
        if (handle < 0)
          return;

        SetParticipantHandle(handle, clientId);
        _authoritativeParticipantInputs[clientId] = Vector2.zero;
        RaiseAuthoritativeHandleSignal(handle);
      }

      ApplyLocalAuthoritativeParticipant();
    }

    private int FindParticipantHandle(int clientId)
    {
      if (_participantHandle0ClientId.Value == clientId) return 0;
      if (_participantHandle1ClientId.Value == clientId) return 1;
      return -1;
    }

    private int FindFreeParticipantHandle()
    {
      int capacity = Mathf.Min(Mathf.Max(1, _maximumPlayerParticipants), PlayerAttachPoints.Count, 2);
      if (capacity > 0 && _participantHandle0ClientId.Value == InvalidParticipantClientId) return 0;
      if (capacity > 1 && _participantHandle1ClientId.Value == InvalidParticipantClientId) return 1;
      return -1;
    }

    private void SetParticipantHandle(int handle, int clientId)
    {
      if (handle == 0) _participantHandle0ClientId.Value = clientId;
      else if (handle == 1) _participantHandle1ClientId.Value = clientId;
    }

    private void RaiseAuthoritativeHandleSignal(int handle)
    {
      string patientIdentifier = _reposedTargetIdentifier.Value;
      if (string.IsNullOrWhiteSpace(patientIdentifier))
        return;

      ScenarioInteractionSignals.Raise($"grab_stretcher_{patientIdentifier}_handle_{handle}");
    }

    private void ApplyLocalAuthoritativeParticipant()
    {
      _participantTogglePending = false;
      if (!TryGetLocalOwnerPlayer(out var player))
        return;

      int handle = FindParticipantHandle(player.Owner.ClientId);
      int key = player.transform.GetInstanceID();
      if (handle < 0)
      {
        if (_participants.TryGetValue(key, out var existing))
        {
          ExitMovingMode(player, existing);
          _participants.Remove(key);
        }
        return;
      }

      if (handle >= PlayerAttachPoints.Count || PlayerAttachPoints[handle] == null)
        return;

      if (_participants.TryGetValue(key, out var current) && current.AttachPoint == PlayerAttachPoints[handle])
        return;

      if (current != null)
      {
        ExitMovingMode(player, current);
        _participants.Remove(key);
      }

      while (_playerAttachPointOccupants.Count < PlayerAttachPoints.Count)
        _playerAttachPointOccupants.Add(null);
      _playerAttachPointOccupants[handle] = player;

      var participant = new RidingParticipant
      {
        Player = player,
        Interactor = player.transform,
        AttachPoint = PlayerAttachPoints[handle],
        LastActionbarRefreshAt = -100f,
      };
      _participants[key] = participant;
      EnterMovingMode(participant);
    }

    private void ClearLocalAuthoritativeParticipant()
    {
      foreach (var pair in _participants)
        ExitMovingMode(pair.Value?.Player, pair.Value);
      _participants.Clear();
    }

    private static bool TryGetLocalOwnerPlayer(out PlayerController player)
    {
      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        if (players[i] != null && players[i].IsOwner && players[i].Owner != null && players[i].Owner.IsValid)
        {
          player = players[i];
          return true;
        }
      }
      player = null;
      return false;
    }

    private void UpdateAuthoritativeParticipantMovement()
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        if (_participants.Count > 0)
          MoveBedFromParticipantsInput();
        return;
      }

      if (IsServerStarted)
      {
        if (MoveBedFromAuthoritativeInputs())
          RpcApplyAuthoritativeBedTransform(transform.position, transform.rotation);
        return;
      }

      if (TryGetLocalOwnerPlayer(out var player) && FindParticipantHandle(player.Owner.ClientId) >= 0)
      {
        var input = player.CurrentMoveInputVector;
        CmdReportParticipantInput(input.z, input.x, sender: null);
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportParticipantInput(float forward, float turn, NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid || FindParticipantHandle(sender.ClientId) < 0)
        return;
      _authoritativeParticipantInputs[sender.ClientId] = new Vector2(Mathf.Clamp(forward, -1f, 1f), Mathf.Clamp(turn, -1f, 1f));
    }

    private bool MoveBedFromAuthoritativeInputs()
    {
      int participants = 0;
      float forward = 0f;
      float turn = 0f;
      int[] clientIds = { _participantHandle0ClientId.Value, _participantHandle1ClientId.Value };
      for (int i = 0; i < clientIds.Length; i++)
      {
        int clientId = clientIds[i];
        if (clientId < 0) continue;
        participants++;

        if (TryGetLocalOwnerPlayer(out var hostPlayer) && hostPlayer.Owner.ClientId == clientId)
        {
          forward += hostPlayer.CurrentMoveInputVector.z;
          turn += hostPlayer.CurrentMoveInputVector.x;
        }
        else if (_authoritativeParticipantInputs.TryGetValue(clientId, out var input))
        {
          forward += input.x;
          turn += input.y;
        }
      }

      if (participants == 0)
        return false;

      int divisor = Mathf.Max(participants, RequiredInteractorCount, 1);
      float forwardRatio = Mathf.Clamp(forward / divisor, -1f, 1f);
      float turnRatio = Mathf.Clamp(turn / divisor, -1f, 1f);
      bool changed = false;
      if (Mathf.Abs(turnRatio) > 0.0001f)
      {
        transform.Rotate(0f, turnRatio * _turnSpeed * Time.deltaTime, 0f, Space.World);
        changed = true;
      }

      if (Mathf.Abs(forwardRatio) > 0.0001f)
      {
        Vector3 target = transform.position + GetBedForwardDirection() * (forwardRatio * _moveSpeed * Time.deltaTime);
        if (!Physics.Linecast(transform.position, target, _movementBlockingMask, QueryTriggerInteraction.Ignore))
        {
          transform.position = target;
          changed = true;
        }
      }
      return changed;
    }

    [ObserversRpc]
    private void RpcApplyAuthoritativeBedTransform(Vector3 position, Quaternion rotation)
    {
      if (IsServerStarted)
        return;
      transform.SetPositionAndRotation(position, rotation);
    }
  }
}
