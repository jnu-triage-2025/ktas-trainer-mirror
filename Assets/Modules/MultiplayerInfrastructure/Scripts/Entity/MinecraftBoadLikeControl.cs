using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

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
    [SerializeField] private string _exitHint = "탈것 내리기 키를 누르면 조종을 종료합니다.";

    [Header("Attach points")]
    [SerializeField] private List<Transform> _playerAttachPoints = new();

    // FishNet이 서버의 슬롯 변경을 모든 관찰 클라이언트에 복제한다.
    // 슬롯 수는 서버 시작 시 Capacity와 동일하게 고정하고 값만 변경한다.
    private readonly SyncList<int> _participants = new();
    private readonly Dictionary<int, Vector2> _serverInputs = new();
    private readonly Dictionary<int, LocalParticipant> _localParticipants = new();
    private bool _togglePending;
    private int _minimumMovementDivisor = 1;
    private TitleUIController _titleUI;
    private int _lastLoggedMovementBlockerId;
    private float _lastMovementBlockerLogTime = float.NegativeInfinity;

    /// <summary>
    /// 종료 키 상태를 추적하여 탑승과 동시/직후의 입력으로 즉시 퇴장하는 것을 막는다.
    /// </summary>
    private bool _exitKeyReady;

    public event Action<int> ParticipantAssigned;

    public int Capacity => Mathf.Min(Mathf.Max(1, _maximumParticipants), _playerAttachPoints.Count);
    /// <summary>이 클라이언트에서 현재 조종 중인 참가자가 하나 이상 있는지 여부.</summary>
    public bool IsLocallyControlled => _localParticipants.Count > 0;

    protected void Awake_MinecraftBoadLikeControl()
    {
      ResolveAttachPoints();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _participants.OnChange += OnParticipantChanged;
      ApplyLocalParticipant();
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      InitializeParticipantSlots();
      if (NetworkManager?.ServerManager != null)
        NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    public override void OnStopServer()
    {
      if (NetworkManager?.ServerManager != null)
        NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;

      for (int i = 0; i < _participants.Count; i++)
        SetHandle(i, InvalidClientId);
      _serverInputs.Clear();
      base.OnStopServer();
    }

    public override void OnStopClient()
    {
      _participants.OnChange -= OnParticipantChanged;
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

    /// <summary>
    /// 현재 이 탈것에 붙어 있는 모든 참가자를 분리한다.
    /// 입력 기반 하차뿐 아니라 시나리오 그래프 같은 외부 시스템에서도 동일한
    /// 네트워크 상태 전이를 사용할 수 있도록 공개한다.
    /// </summary>
    public void DetachAllParticipants()
    {
      if (!IsClientStarted && !IsServerStarted)
      {
        ClearLocalParticipants();
        return;
      }

      // 참가자 SyncList는 서버 권위 상태이므로 클라이언트에서 직접 변경하지 않는다.
      if (!IsServerStarted)
        return;

      for (int i = 0; i < _participants.Count; i++)
      {
        int participant = _participants[i];
        if (participant >= 0)
          _serverInputs.Remove(participant);
        SetHandle(i, InvalidClientId);
      }
      ApplyLocalParticipant();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdToggle(NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid)
        return;

      if (TryResolvePlayer(sender.ClientId, out var player))
        TryToggleOnServer(sender.ClientId, player);

      // 거절된 요청도 반드시 응답하여 클라이언트의 요청 잠금을 해제한다.
      TargetToggleCompleted(sender);
    }

    [TargetRpc]
    private void TargetToggleCompleted(NetworkConnection connection)
    {
      CompleteToggleRequest();
    }

    private void CompleteToggleRequest()
    {
      _togglePending = false;
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
      for (int i = 0; i < _participants.Count; i++)
      {
        int clientId = _participants[i];
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

    /// <summary>
    /// 현재 서버 이동에 실제 입력을 제공하는 참가자가 정확히 한 명일 때만 그 연결을 반환한다.
    /// 협동 이동처럼 행동 주체가 여럿인 경우에는 임의의 한 명에게 후속 이벤트를 귀속하지 않는다.
    /// </summary>
    protected bool TryGetSingleMovingParticipantConnection(out NetworkConnection connection)
    {
      connection = null;
      if (!IsServerStarted)
        return false;

      int contributingClientId = InvalidClientId;
      for (int i = 0; i < _participants.Count; i++)
      {
        int clientId = _participants[i];
        if (clientId < 0)
          continue;

        Vector2 input = Vector2.zero;
        if (TryGetLocalOwnerPlayer(out var host) && host.Owner.ClientId == clientId)
          input = new Vector2(host.CurrentMoveInputVector.z, host.CurrentMoveInputVector.x);
        else if (_serverInputs.TryGetValue(clientId, out var reportedInput))
          input = reportedInput;

        if (input.sqrMagnitude <= 0.00000001f)
          continue;
        if (contributingClientId != InvalidClientId)
          return false;
        contributingClientId = clientId;
      }

      if (contributingClientId == InvalidClientId || !TryResolvePlayer(contributingClientId, out var player))
        return false;
      connection = player.Owner;
      return connection != null && connection.IsValid;
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
        if (!IsMovementBlocked(transform.position, target))
        {
          transform.position = target;
          changed = true;
        }
      }
      return changed;
    }

    private bool IsMovementBlocked(Vector3 origin, Vector3 target)
    {
      Vector3 offset = target - origin;
      float distance = offset.magnitude;
      if (distance <= Mathf.Epsilon)
        return false;

      RaycastHit[] hits = Physics.RaycastAll(
        origin,
        offset / distance,
        distance,
        _movementBlockingMask,
        QueryTriggerInteraction.Ignore);
      for (int i = 0; i < hits.Length; i++)
      {
        Collider collider = hits[i].collider;
        if (collider != null && !ShouldIgnoreMovementBlocker(collider))
        {
          LogMovementBlocker(hits[i], origin, target);
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Derived controls may exclude colliders that move as part of their controlled payload.
    /// </summary>
    protected virtual bool ShouldIgnoreMovementBlocker(Collider collider) => false;

    /// <summary>
    /// Enables collision-origin diagnostics for a specific derived controller.
    /// </summary>
    protected virtual bool ShouldLogMovementBlockers => false;

    private void LogMovementBlocker(RaycastHit hit, Vector3 origin, Vector3 target)
    {
      if (!ShouldLogMovementBlockers || hit.collider == null)
        return;

      int colliderId = hit.collider.GetInstanceID();
      if (colliderId == _lastLoggedMovementBlockerId
          && Time.unscaledTime - _lastMovementBlockerLogTime < 1f)
      {
        return;
      }

      _lastLoggedMovementBlockerId = colliderId;
      _lastMovementBlockerLogTime = Time.unscaledTime;

      Collider collider = hit.collider;
      Rigidbody body = collider.attachedRigidbody;
      Debug.LogWarning(
        $"[MovementBlocker] controller='{GetTransformPath(transform)}' " +
        $"blockerPath='{GetTransformPath(collider.transform)}' " +
        $"colliderType={collider.GetType().Name} layer={LayerMask.LayerToName(collider.gameObject.layer)}({collider.gameObject.layer}) " +
        $"trigger={collider.isTrigger} enabled={collider.enabled} " +
        $"rigidbody='{(body == null ? "none" : GetTransformPath(body.transform))}' " +
        $"hitPoint={hit.point} normal={hit.normal} distance={hit.distance:F3} " +
        $"origin={origin} target={target} boundsCenter={collider.bounds.center} boundsSize={collider.bounds.size}",
        collider);
    }

    private static string GetTransformPath(Transform target)
    {
      if (target == null)
        return "none";

      var names = new List<string>();
      for (Transform current = target; current != null; current = current.parent)
        names.Add(current.name);
      names.Reverse();
      return string.Join("/", names);
    }

    /// <summary>
    /// 서버(또는 오프라인 실행)가 조종 오브젝트의 위치를 즉시 보정할 때 사용한다.
    /// 서버에서 적용한 값은 모든 관찰 클라이언트에도 같은 프레임에 전달된다.
    /// </summary>
    protected void SetAuthoritativeTransform(Vector3 position, Quaternion rotation)
    {
      transform.SetPositionAndRotation(position, rotation);

      if (IsServerStarted)
        RpcApplyTransform(position, rotation);
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
      KeyCode exitKey = KeyBindingRepository.GetBoundKey("dismount", KeyCode.LeftShift);

      // 종료 키가 release 된 적이 있어야 다시 down edge 일 때만 퇴장을 허용한다.
      // 이렇게 하면:
      // 1) 보트/침대에 탑승한 동일/직후 프레임의 종료 키 down edge로 즉시 퇴장하는 문제,
      // 2) 탑승 직후 무의식적인 종료 키 입력으로 actionbar가 사라지는 문제를 막는다.
      // 종료 키가 눌린 뒤 떼어지는 순간 true로 전환되어, 다음 down edge부터 유효하다.
      if (!_exitKeyReady)
      {
        if (Input.GetKeyUp(exitKey))
        {
          _exitKeyReady = true;
        }
        return;
      }

      if (!Input.GetKeyDown(exitKey))
        return;

      foreach (var pair in _localParticipants)
      {
        if (pair.Value?.Player == null || !pair.Value.Player.IsOwner)
          continue;
        Toggle(pair.Value.Player.transform);
        break;
      }
    }

    protected void ClearLocalParticipants()
    {
      foreach (var pair in _localParticipants)
        ExitLocal(pair.Value);
      _localParticipants.Clear();
    }

    private bool IsLocalParticipant(PlayerController player) =>
      player != null && _localParticipants.ContainsKey(player.GetInstanceID());

    private int FindHandle(int clientId)
    {
      for (int i = 0; i < _participants.Count; i++)
        if (_participants[i] == clientId)
          return i;
      return -1;
    }

    private int FindFreeHandle()
    {
      int count = Mathf.Min(Capacity, _participants.Count);
      for (int i = 0; i < count; i++)
        if (_participants[i] == InvalidClientId)
          return i;
      return -1;
    }

    private void SetHandle(int handle, int clientId)
    {
      if (handle >= 0 && handle < _participants.Count)
        _participants[handle] = clientId;
    }

    private void OnParticipantChanged(
      SyncListOperation operation,
      int index,
      int previous,
      int next,
      bool asServer) =>
      ApplyLocalParticipant();

    private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
      if (args.ConnectionState != RemoteConnectionState.Stopped || connection == null)
        return;

      RemoveParticipant(connection.ClientId);
    }

    private void RemoveParticipant(int clientId)
    {
      RemoveParticipantState(_participants, _serverInputs, clientId);
      ApplyLocalParticipant();
    }

    private static void RemoveParticipantState(
      IList<int> participants,
      IDictionary<int, Vector2> serverInputs,
      int clientId)
    {
      for (int i = 0; i < participants.Count; i++)
      {
        if (participants[i] != clientId)
          continue;
        participants[i] = InvalidClientId;
        break;
      }
      serverInputs.Remove(clientId);
    }

    private void InitializeParticipantSlots()
    {
      if (!IsServerStarted)
        return;

      while (_participants.Count < Capacity)
        _participants.Add(InvalidClientId);
      while (_participants.Count > Capacity)
        _participants.RemoveAt(_participants.Count - 1);
    }

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

      // 명시 목록이 없을 때만 자식 마커를 기본 참가자 위치로 사용한다.
      // 명시된 일부 위치만 허용하려는 프리팹 구성을 자동 탐색으로 확장하지 않는다.
      if (_playerAttachPoints.Count == 0)
      {
        var points = GetComponentsInChildren<RidableAttachPointObject>(true);
        for (int i = 0; i < points.Length; i++)
        {
          Transform point = points[i] != null ? points[i].transform : null;
          if (point != null)
            _playerAttachPoints.Add(point);
        }
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
