using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// Minecraft 보트 방식의 탑승/점유/이동을 제공하는 공통 네트워크 모듈.
  /// 도메인 객체(환자 침대, 의료 장비 등)의 상태나 상호작용은 소유하지 않는다.
  /// </summary>
  [RequireComponent(typeof(NetworkObject))]
  public abstract class MinecraftBoadLikeControl : NetworkBehaviour
  {
    private const int InvalidClientId = -1;

    private sealed class LocalParticipant
    {
      public PlayerController Player;
      public Transform AttachPoint;
    }

    [Header("Control")]
    [SerializeField, Min(1)] private int _maximumParticipants = 1;
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _turnSpeed = 120f;
    [SerializeField] private float _forwardYawOffsetDegrees = -90f;
    [SerializeField] private LayerMask _movementBlockingMask = ~0;
    [SerializeField, Min(0f)] private float _maximumToggleDistance = 3f;
    [SerializeField] private string _exitHint = "왼쪽 Shift 키를 누르면 조종을 종료합니다.";

    [Header("Attach points")]
    [FormerlySerializedAs("PlayerAttachPoints")]
    [SerializeField] private List<Transform> _playerAttachPoints = new();

    private readonly SyncVar<int> _participant0 = new(InvalidClientId);
    private readonly SyncVar<int> _participant1 = new(InvalidClientId);
    private readonly Dictionary<int, Vector2> _serverInputs = new();
    private readonly Dictionary<int, LocalParticipant> _localParticipants = new();
    private bool _togglePending;
    private int _minimumMovementDivisor = 1;
    private TitleUIController _titleUI;

    /// <summary>
    /// 종료 키(LeftShift) 상태를 추적하여 "탑승과 동시/직후에 무의미하게 잡힌 종료 키"로 인한 즉시 퇴장을 막는다.
    /// - <c>false</c>: 탑승 시 LeftShift 가 눌려 있었거나, 아직 한 번도 release 된 적이 없는 상태. LeftShift down 을 퇴장으로 처리하지 않는다.
    /// - <c>true</c>: LeftShift 가 release 된 이후 다시 down edge 가 들어와야 퇴장을 허용한다.
    /// </summary>
    private bool _exitKeyReady;

    public event Action<int> ParticipantAssigned;

    public int Capacity => Mathf.Min(Mathf.Max(1, _maximumParticipants), _playerAttachPoints.Count, 2);

    protected void Awake_MinecraftBoadLikeControl()
    {
      ResolveAttachPoints();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _participant0.OnChange += OnParticipantChanged;
      _participant1.OnChange += OnParticipantChanged;
      ApplyLocalParticipant();
    }

    public override void OnStopClient()
    {
      _participant0.OnChange -= OnParticipantChanged;
      _participant1.OnChange -= OnParticipantChanged;
      ClearLocalParticipants();
      base.OnStopClient();
    }

    protected void Update_MinecraftBoadLikeControl()
    {
      HandleLocalExitInput();
      UpdateNetworkMovement();
    }

    protected void Configure(int maximumParticipants, string exitHint = null)
    {
      _maximumParticipants = Mathf.Max(1, maximumParticipants);
      if (!string.IsNullOrWhiteSpace(exitHint))
        _exitHint = exitHint;
      ResolveAttachPoints();
    }

    public void SetMinimumMovementDivisor(int value)
    {
      _minimumMovementDivisor = Mathf.Max(1, value);
    }

    public bool CanToggle(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return false;
      if (IsLocalParticipant(player))
        return false;
      return FindFreeHandle() >= 0;
    }

    public void Toggle(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      if (!IsClientStarted && !IsServerStarted)
      {
        ToggleOffline(player);
        return;
      }

      if (player.Owner == null || !player.Owner.IsValid)
        return;
      if (IsServerStarted)
        TryToggleOnServer(player.Owner.ClientId, player);
      else if (!_togglePending)
      {
        _togglePending = true;
        CmdToggle();
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdToggle(NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid ||
          !TryResolvePlayer(sender.ClientId, out var player))
        return;

      TryToggleOnServer(sender.ClientId, player);
    }

    private bool TryToggleOnServer(int clientId, PlayerController player)
    {
      int handle = FindHandle(clientId);
      if (handle >= 0)
      {
        SetHandle(handle, InvalidClientId);
        _serverInputs.Remove(clientId);
      }
      else
      {
        if (!IsWithinToggleDistance(player))
          return false;

        handle = FindFreeHandle();
        if (handle < 0)
          return false;
        SetHandle(handle, clientId);
        _serverInputs[clientId] = Vector2.zero;
        ParticipantAssigned?.Invoke(handle);
      }
      ApplyLocalParticipant();
      return true;
    }

    private void UpdateNetworkMovement()
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        MoveFromLocalParticipants();
        return;
      }

      if (IsServerStarted)
      {
        if (MoveFromServerInputs())
          RpcApplyTransform(transform.position, transform.rotation);
        return;
      }

      if (TryGetLocalOwnerPlayer(out var player) && FindHandle(player.Owner.ClientId) >= 0)
      {
        Vector3 input = player.CurrentMoveInputVector;
        CmdReportInput(input.z, input.x);
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportInput(float forward, float turn, NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid || FindHandle(sender.ClientId) < 0)
        return;
      _serverInputs[sender.ClientId] =
        new Vector2(Mathf.Clamp(forward, -1f, 1f), Mathf.Clamp(turn, -1f, 1f));
    }

    private bool MoveFromServerInputs()
    {
      int count = 0;
      float forward = 0f;
      float turn = 0f;
      int[] clientIds = { _participant0.Value, _participant1.Value };
      for (int i = 0; i < clientIds.Length; i++)
      {
        int clientId = clientIds[i];
        if (clientId < 0)
          continue;
        count++;
        if (TryGetLocalOwnerPlayer(out var host) && host.Owner.ClientId == clientId)
        {
          forward += host.CurrentMoveInputVector.z;
          turn += host.CurrentMoveInputVector.x;
        }
        else if (_serverInputs.TryGetValue(clientId, out var input))
        {
          forward += input.x;
          turn += input.y;
        }
      }
      return count > 0 && ApplyMovement(forward, turn, count);
    }

    private void MoveFromLocalParticipants()
    {
      int count = 0;
      float forward = 0f;
      float turn = 0f;
      foreach (var pair in _localParticipants)
      {
        var player = pair.Value?.Player;
        if (player == null)
          continue;
        count++;
        forward += player.CurrentMoveInputVector.z;
        turn += player.CurrentMoveInputVector.x;
      }
      if (count > 0)
        ApplyMovement(forward, turn, count);
    }

    private bool ApplyMovement(float forward, float turn, int participantCount)
    {
      int divisor = Mathf.Max(participantCount, _minimumMovementDivisor, 1);
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
        Vector3 target = transform.position +
                         GetForwardDirection() * (forwardRatio * _moveSpeed * Time.deltaTime);
        if (!Physics.Linecast(transform.position, target, _movementBlockingMask, QueryTriggerInteraction.Ignore))
        {
          transform.position = target;
          changed = true;
        }
      }
      return changed;
    }

    [ObserversRpc]
    private void RpcApplyTransform(Vector3 position, Quaternion rotation)
    {
      if (!IsServerStarted)
        transform.SetPositionAndRotation(position, rotation);
    }

    private void ToggleOffline(PlayerController player)
    {
      int key = player.GetInstanceID();
      if (_localParticipants.TryGetValue(key, out var existing))
      {
        ExitLocal(existing);
        _localParticipants.Remove(key);
        return;
      }

      int handle = Mathf.Clamp(_localParticipants.Count, 0, Mathf.Max(0, Capacity - 1));
      if (_localParticipants.Count >= Capacity)
        return;
      EnterLocal(player, handle);
    }

    private void ApplyLocalParticipant()
    {
      _togglePending = false;
      if (!TryGetLocalOwnerPlayer(out var player))
        return;

      int key = player.GetInstanceID();
      int handle = FindHandle(player.Owner.ClientId);
      if (handle < 0)
      {
        if (_localParticipants.TryGetValue(key, out var existing))
        {
          ExitLocal(existing);
          _localParticipants.Remove(key);
        }
        return;
      }

      if (_localParticipants.ContainsKey(key))
        return;
      EnterLocal(player, handle);
    }

    private void EnterLocal(PlayerController player, int handle)
    {
      if (handle < 0 || handle >= _playerAttachPoints.Count)
        return;
      var participant = new LocalParticipant
      {
        Player = player,
        AttachPoint = _playerAttachPoints[handle]
      };
      _localParticipants[player.GetInstanceID()] = participant;
      player.AlignYawTo(GetForwardDirection());
      player.SetForcedFollowAnchor(participant.AttachPoint);
      player.SetRidableControlActive(this);
      player.RefreshInteractableHintsNow();
      _exitKeyReady = false;
      if (player.IsOwner)
      {
        _titleUI ??= Registry.Registry.Get<TitleUIController>(
          RegistryType.UI, Registry.Registry.TypeKey<TitleUIController>());
        _titleUI?.ShowPersistentActionbar(_exitHint);
      }
    }

    private void ExitLocal(LocalParticipant participant)
    {
      if (participant?.Player != null && participant.Player.IsOwner)
        _titleUI?.ClearActionbar();

      participant?.Player?.ClearForcedFollowAnchor(participant.AttachPoint);
      participant?.Player?.ClearRidableControlActive(this);
      participant?.Player?.RefreshInteractableHintsNow();
    }

    private void HandleLocalExitInput()
    {
      // LeftShift 가 release 된 적이 있어야 다시 down edge 일 때만 퇴장을 허용한다.
      // 이렇게 하면:
      // 1) 보트/침대에 탑승한 동일/직후 프레임에 이미 눌려 있던 LeftShift down edge 가 잡혀 즉시 퇴장(actionbar clear)되는 문제,
      // 2) 탑승 직후 무의식적으로 LeftShift 를 건드렸을 때 actionbar 가 사라지는 문제를 막는다.
      // LeftShift 가 눌려 있다가 떼어지는 순간 == true 로 전환되어 다음 down edge 부터 유효.
      if (!_exitKeyReady)
      {
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
          _exitKeyReady = true;
        }
        return;
      }

      if (!Input.GetKeyDown(KeyCode.LeftShift))
        return;

      foreach (var pair in _localParticipants)
      {
        if (pair.Value?.Player == null || !pair.Value.Player.IsOwner)
          continue;
        Toggle(pair.Value.Player.transform);
        break;
      }
    }

    private void ClearLocalParticipants()
    {
      foreach (var pair in _localParticipants)
        ExitLocal(pair.Value);
      _localParticipants.Clear();
    }

    private bool IsLocalParticipant(PlayerController player) =>
      player != null && _localParticipants.ContainsKey(player.GetInstanceID());

    private int FindHandle(int clientId)
    {
      if (_participant0.Value == clientId) return 0;
      if (_participant1.Value == clientId) return 1;
      return -1;
    }

    private int FindFreeHandle()
    {
      if (Capacity > 0 && _participant0.Value == InvalidClientId) return 0;
      if (Capacity > 1 && _participant1.Value == InvalidClientId) return 1;
      return -1;
    }

    private void SetHandle(int handle, int clientId)
    {
      if (handle == 0) _participant0.Value = clientId;
      else if (handle == 1) _participant1.Value = clientId;
    }

    private void OnParticipantChanged(int previous, int next, bool asServer) =>
      ApplyLocalParticipant();

    private Vector3 GetForwardDirection()
    {
      Vector3 forward =
        (Quaternion.Euler(0f, _forwardYawOffsetDegrees, 0f) * transform.forward).normalized;
      forward.y = 0f;
      return forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
    }

    private void ResolveAttachPoints()
    {
      _playerAttachPoints.RemoveAll(each => each == null);
      if (_playerAttachPoints.Count == 0)
      {
        var points = GetComponentsInChildren<RidableAttachPointObject>(true);
        for (int i = 0; i < points.Length; i++)
          if (points[i] != null)
            _playerAttachPoints.Add(points[i].transform);
      }

      if (_playerAttachPoints.Count == 0)
      {
        var go = new GameObject("PlayerAttachPoint");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.8f);
        go.AddComponent<RidableAttachPointObject>();
        _playerAttachPoints.Add(go.transform);
      }
    }

    private bool IsWithinToggleDistance(PlayerController player)
    {
      if (player == null)
        return false;

      float maximumSqrDistance = _maximumToggleDistance * _maximumToggleDistance;
      Vector3 playerPosition = player.transform.position;
      if ((playerPosition - transform.position).sqrMagnitude <= maximumSqrDistance)
        return true;

      for (int i = 0; i < _playerAttachPoints.Count; i++)
      {
        var attachPoint = _playerAttachPoints[i];
        if (attachPoint != null &&
            (playerPosition - attachPoint.position).sqrMagnitude <= maximumSqrDistance)
          return true;
      }

      return false;
    }

    private static bool TryResolvePlayer(int clientId, out PlayerController player)
    {
      player = null;
      if (!Registry.Registry.TryGetEntityByClientId(clientId, out var descriptor) ||
          descriptor?.GameObject == null)
        return false;

      player = descriptor.GameObject.GetComponent<PlayerController>() ??
               descriptor.GameObject.GetComponentInChildren<PlayerController>(true);
      return player != null &&
             player.Owner != null &&
             player.Owner.IsValid &&
             player.Owner.ClientId == clientId;
    }

    private static bool TryGetLocalOwnerPlayer(out PlayerController player)
    {
      var players = FindObjectsByType<PlayerController>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        if (players[i] != null && players[i].IsOwner &&
            players[i].Owner != null && players[i].Owner.IsValid)
        {
          player = players[i];
          return true;
        }
      }
      player = null;
      return false;
    }
  }
}
