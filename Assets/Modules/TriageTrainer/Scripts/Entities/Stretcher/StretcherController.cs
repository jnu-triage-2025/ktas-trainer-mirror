using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 들것 전용 운반 컨트롤러(서버 권위 네트워크 대응).
  ///
  /// <para>
  /// 최대 6명의 플레이어가 손잡이를 점유하여 협력 이동한다. 모든 점유 상태는
  /// <see cref="SyncVar{T}"/> 로 서버 권위 복제되며, 이동은 서버에서 입력을 집계한 뒤
  /// <see cref="ObserversRpc"/> 로 전 피어에 transform 을 브로드캐스트한다.
  /// </para>
  ///
  /// <para>
  /// 오프라인(네트워크 비활성) 환경에서는 기존과 동일하게 로컬에서만 동작한다.
  /// </para>
  /// </summary>
  [RequireComponent(typeof(NetworkObject))]
  public sealed class StretcherController : NetworkBehaviour, IInteractable, IInteract, IInteractorConditional
  {
    private const int HandleCount = 6;
    private const int InvalidClientId = -1;

    [SerializeField] private string _displayText = "들것";
    [SerializeField] private Transform _visualRoot;
    [SerializeField, Min(0f)] private float _balancedVisualYOffset = 0.2f;
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _turnSpeed = 120f;
    [SerializeField, Min(0f)] private float _maximumToggleDistance = 3f;

    private readonly Transform[] _handles = new Transform[HandleCount];
    private Vector3 _visualBaseLocalPosition;

    // ── SyncVar: 각 손잡이 점유 상태 (서버 권위, -1 = 비어 있음) ──
    private readonly SyncVar<int> _handle0 = new(InvalidClientId);
    private readonly SyncVar<int> _handle1 = new(InvalidClientId);
    private readonly SyncVar<int> _handle2 = new(InvalidClientId);
    private readonly SyncVar<int> _handle3 = new(InvalidClientId);
    private readonly SyncVar<int> _handle4 = new(InvalidClientId);
    private readonly SyncVar<int> _handle5 = new(InvalidClientId);

    // 서버: 각 클라이언트의 최신 입력(forward, turn)
    private readonly System.Collections.Generic.Dictionary<int, Vector2> _serverInputs = new();

    // 로컬 피어: 자신이 점유 중인 손잡이의 Transform 캐시
    private Transform _localFollowAnchor;

    public string DisplayText => _displayText;
    public Sprite DisplayIcon => null;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;
    public IInteract[] Interacts => new IInteract[] { this };
    public bool IsWeightBalanced { get; private set; }

    // ── Lifecycle ──

    private void Awake()
    {
      CreateHandles();
      if (_visualRoot == null)
        _visualRoot = transform;
      _visualBaseLocalPosition = _visualRoot.localPosition;
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _handle0.OnChange += OnHandleChanged;
      _handle1.OnChange += OnHandleChanged;
      _handle2.OnChange += OnHandleChanged;
      _handle3.OnChange += OnHandleChanged;
      _handle4.OnChange += OnHandleChanged;
      _handle5.OnChange += OnHandleChanged;
      ApplyLocalFollowAnchor();
    }

    public override void OnStopClient()
    {
      _handle0.OnChange -= OnHandleChanged;
      _handle1.OnChange -= OnHandleChanged;
      _handle2.OnChange -= OnHandleChanged;
      _handle3.OnChange -= OnHandleChanged;
      _handle4.OnChange -= OnHandleChanged;
      _handle5.OnChange -= OnHandleChanged;
      ClearLocalFollowAnchor();
      base.OnStopClient();
    }

    public override void OnStopServer()
    {
      for (int i = 0; i < HandleCount; i++)
        SetHandle(i, InvalidClientId);
      _serverInputs.Clear();
      base.OnStopServer();
    }

    // ── Update ──

    private void Update()
    {
      HandlePositionSelection();
      UpdateNetworkMovement();
    }

    // ── IInteractable / IInteract / IInteractorConditional ──

    public bool CanInteract(Transform interactor) =>
      interactor != null && interactor.GetComponentInParent<PlayerController>() != null;

    public void Interact(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      // 오프라인 폴백
      if (!IsClientStarted && !IsServerStarted)
      {
        ToggleOffline(player);
        return;
      }

      // 서버 컨텍스트(호스트)
      if (IsServerStarted)
      {
        if (player.Owner != null && player.Owner.IsValid)
          TryToggleOnServer(player.Owner.ClientId, player);
        return;
      }

      // 클라이언트 컨텍스트 → 서버에 토글 요청
      if (IsClientInitialized)
        CmdToggle();
    }

    // ── Server: Toggle (Join or Leave) ──

    [ServerRpc(RequireOwnership = false)]
    private void CmdToggle(NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid || !TryResolvePlayer(sender.ClientId, out var player))
        return;
      TryToggleOnServer(sender.ClientId, player);
    }

    private bool TryToggleOnServer(int clientId, PlayerController player)
    {
      int currentHandle = FindHandle(clientId);
      if (currentHandle >= 0)
      {
        // 이미 점유 중 → 퇴장
        SetHandle(currentHandle, InvalidClientId);
        _serverInputs.Remove(clientId);
      }
      else
      {
        // 신규 탑승: 거리 확인 후 빈 손잡이에 배치
        if (!IsWithinToggleDistance(player))
          return false;

        int freeHandle = FindFreeHandle();
        if (freeHandle < 0)
          return false;

        SetHandle(freeHandle, clientId);
        _serverInputs[clientId] = Vector2.zero;
      }

      ApplyLocalFollowAnchor();
      return true;
    }

    // ── Server: Move between handles ──

    [ServerRpc(RequireOwnership = false)]
    private void CmdMoveHandle(int fromHandle, int toHandle, NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid)
        return;
      if (fromHandle < 0 || fromHandle >= HandleCount)
        return;
      if (toHandle < 0 || toHandle >= HandleCount)
        return;
      if (GetHandle(fromHandle) != sender.ClientId)
        return;
      if (GetHandle(toHandle) != InvalidClientId)
        return;

      SetHandle(fromHandle, InvalidClientId);
      SetHandle(toHandle, sender.ClientId);
      ApplyLocalFollowAnchor();
    }

    // ── Server: Input reporting ──

    [ServerRpc(RequireOwnership = false)]
    private void CmdReportInput(float forward, float turn, NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid || FindHandle(sender.ClientId) < 0)
        return;
      if (!float.IsFinite(forward) || !float.IsFinite(turn))
        return;
      _serverInputs[sender.ClientId] =
        new Vector2(Mathf.Clamp(forward, -1f, 1f), Mathf.Clamp(turn, -1f, 1f));
    }

    // ── Observers: Transform & weight sync ──

    [ObserversRpc]
    private void RpcApplyTransform(Vector3 position, Quaternion rotation)
    {
      if (!IsServerStarted)
        transform.SetPositionAndRotation(position, rotation);
    }

    // ── Network Movement ──

    private void UpdateNetworkMovement()
    {
      // 오프라인: 기존 로컬 이동
      if (!IsClientStarted && !IsServerStarted)
      {
        MoveFromLocalParticipants();
        return;
      }

      // 서버: 입력 집계 → 이동 → 브로드캐스트
      if (IsServerStarted)
      {
        if (MoveFromServerInputs())
          RpcApplyTransform(transform.position, transform.rotation);
        return;
      }

      // 클라이언트: 로컬 소유 플레이어의 입력을 서버에 보고
      if (TryGetLocalOwnerPlayer(out var player) && FindHandle(player.Owner.ClientId) >= 0)
      {
        Vector3 input = player.CurrentMoveInputVector;
        CmdReportInput(input.z, input.x);
      }
    }

    private bool MoveFromServerInputs()
    {
      int count = 0;
      float forward = 0f;
      float turn = 0f;

      for (int i = 0; i < HandleCount; i++)
      {
        int clientId = GetHandle(i);
        if (clientId == InvalidClientId)
          continue;
        count++;

        // 호스트의 로컬 플레이어는 직접 입력 읽기
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

      for (int i = 0; i < HandleCount; i++)
      {
        var player = ResolveLocalPlayer(GetHandle(i));
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
      float forwardRatio = Mathf.Clamp(forward / participantCount, -1f, 1f);
      float turnRatio = Mathf.Clamp(turn / participantCount, -1f, 1f);
      bool changed = false;

      if (Mathf.Abs(turnRatio) > 0.0001f)
      {
        transform.Rotate(0f, turnRatio * _turnSpeed * Time.deltaTime, 0f, Space.World);
        changed = true;
      }

      if (Mathf.Abs(forwardRatio) > 0.0001f)
      {
        transform.position += transform.forward * (forwardRatio * _moveSpeed * Time.deltaTime);
        changed = true;
      }

      return changed;
    }

    // ── Handle Position Selection (Ctrl+1~6) ──

    private void HandlePositionSelection()
    {
      if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
        return;

      KeyCode[] keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };
      for (int desired = 0; desired < keys.Length; desired++)
      {
        if (!Input.GetKeyDown(keys[desired]))
          continue;

        // 오프라인
        if (!IsClientStarted && !IsServerStarted)
        {
          HandleLocalPositionSelection(desired);
          return;
        }

        // 서버 또는 클라이언트 → ServerRpc
        if (TryGetLocalOwnerPlayer(out var player))
        {
          int from = FindHandle(player.Owner.ClientId);
          if (from >= 0 && from != desired)
          {
            if (IsServerStarted)
            {
              if (GetHandle(desired) == InvalidClientId)
              {
                SetHandle(from, InvalidClientId);
                SetHandle(desired, player.Owner.ClientId);
                ApplyLocalFollowAnchor();
              }
            }
            else
            {
              CmdMoveHandle(from, desired);
            }
          }
        }
        return;
      }
    }

    private void HandleLocalPositionSelection(int desired)
    {
      foreach (var player in FindObjectsByType<PlayerController>(
                 FindObjectsInactive.Exclude, FindObjectsSortMode.None))
      {
        if (player == null || !player.IsOwner)
          continue;
        int key = player.GetInstanceID();
        for (int from = 0; from < HandleCount; from++)
        {
          if (GetHandleLocal(from) != key)
            continue;
          if (from == desired || GetHandleLocal(desired) != InvalidClientId)
            return;
          MoveParticipantLocal(player, from, desired);
          return;
        }
      }
    }

    // ── SyncVar helpers ──

    private int GetHandle(int index) => index switch
    {
      0 => _handle0.Value,
      1 => _handle1.Value,
      2 => _handle2.Value,
      3 => _handle3.Value,
      4 => _handle4.Value,
      5 => _handle5.Value,
      _ => InvalidClientId,
    };

    private void SetHandle(int index, int clientId)
    {
      switch (index)
      {
        case 0:
          _handle0.Value = clientId;
          break;
        case 1:
          _handle1.Value = clientId;
          break;
        case 2:
          _handle2.Value = clientId;
          break;
        case 3:
          _handle3.Value = clientId;
          break;
        case 4:
          _handle4.Value = clientId;
          break;
        case 5:
          _handle5.Value = clientId;
          break;
      }
    }

    /// <summary>오프라인 폴백용: InstanceID 기준 손잡이 값 읽기.</summary>
    private int GetHandleLocal(int index) => GetHandle(index);

    private int FindHandle(int clientId)
    {
      for (int i = 0; i < HandleCount; i++)
        if (GetHandle(i) == clientId)
          return i;
      return -1;
    }

    private int FindFreeHandle()
    {
      for (int i = 0; i < HandleCount; i++)
        if (GetHandle(i) == InvalidClientId)
          return i;
      return -1;
    }

    private void OnHandleChanged(int previous, int next, bool asServer)
    {
      ApplyLocalFollowAnchor();
      UpdateWeightBalance();
    }

    // ── Local follow anchor management ──

    private void ApplyLocalFollowAnchor()
    {
      if (!TryGetLocalOwnerPlayer(out var player))
        return;

      int key = player.GetInstanceID();
      int handle = IsServerStarted || IsClientStarted
        ? FindHandle(player.Owner != null && player.Owner.IsValid ? player.Owner.ClientId : InvalidClientId)
        : FindHandleLocal(key);

      if (handle < 0)
      {
        if (_localFollowAnchor != null)
        {
          player.ClearForcedFollowAnchor(_localFollowAnchor);
          _localFollowAnchor = null;
        }
        return;
      }

      if (_localFollowAnchor == _handles[handle])
        return;

      if (_localFollowAnchor != null)
        player.ClearForcedFollowAnchor(_localFollowAnchor);

      _localFollowAnchor = _handles[handle];
      player.SetForcedFollowAnchor(_localFollowAnchor);
    }

    private void ClearLocalFollowAnchor()
    {
      if (_localFollowAnchor == null)
        return;
      if (TryGetLocalOwnerPlayer(out var player))
        player.ClearForcedFollowAnchor(_localFollowAnchor);
      _localFollowAnchor = null;
    }

    /// <summary>오프라인: InstanceID 기준 로컬 핸들 검색.</summary>
    private int FindHandleLocal(int instanceId)
    {
      for (int i = 0; i < HandleCount; i++)
        if (GetHandle(i) == instanceId)
          return i;
      return -1;
    }

    // ── Weight Balance ──

    private void UpdateWeightBalance()
    {
      IsWeightBalanced =
        (GetHandle(0) != InvalidClientId && GetHandle(3) != InvalidClientId) ||
        (GetHandle(4) != InvalidClientId && GetHandle(5) != InvalidClientId) ||
        (GetHandle(1) != InvalidClientId && GetHandle(2) != InvalidClientId &&
         GetHandle(4) != InvalidClientId && GetHandle(5) != InvalidClientId);

      _visualRoot.localPosition = _visualBaseLocalPosition +
        (IsWeightBalanced ? Vector3.up * _balancedVisualYOffset : Vector3.zero);
    }

    // ── Offline fallback ──

    private void ToggleOffline(PlayerController player)
    {
      int key = player.GetInstanceID();
      int currentHandle = FindHandleLocal(key);
      if (currentHandle >= 0)
      {
        SetHandle(currentHandle, InvalidClientId);
        player.ClearForcedFollowAnchor(_handles[currentHandle]);
        UpdateWeightBalance();
        return;
      }

      int freeHandle = FindFreeHandle();
      if (freeHandle < 0)
        return;

      SetHandle(freeHandle, key);
      player.SetForcedFollowAnchor(_handles[freeHandle]);
      UpdateWeightBalance();
    }

    private void MoveParticipantLocal(PlayerController player, int from, int to)
    {
      if (from == to || GetHandleLocal(to) != InvalidClientId)
        return;
      SetHandle(from, InvalidClientId);
      SetHandle(to, player.GetInstanceID());
      player.SetForcedFollowAnchor(_handles[to]);
      UpdateWeightBalance();
    }

    // ── Helpers ──

    private void CreateHandles()
    {
      Vector3[] positions =
      {
        new(0.5f, 0f, 1f), new(-0.5f, 0f, 1f), new(-0.5f, 0f, -1f),
        new(0.5f, 0f, -1f), new(0f, 0f, 1.8f), new(0f, 0f, -1.8f),
      };
      for (int i = 0; i < positions.Length; i++)
      {
        var handle = new GameObject($"StretcherHandle{i + 1}").transform;
        handle.SetParent(transform, false);
        handle.localPosition = positions[i];
        _handles[i] = handle;
      }
    }

    private bool IsWithinToggleDistance(PlayerController player)
    {
      if (player == null)
        return false;
      float maxSqr = _maximumToggleDistance * _maximumToggleDistance;
      if ((player.transform.position - transform.position).sqrMagnitude <= maxSqr)
        return true;

      for (int i = 0; i < HandleCount; i++)
      {
        if (_handles[i] != null &&
            (player.transform.position - _handles[i].position).sqrMagnitude <= maxSqr)
          return true;
      }
      return false;
    }

    private static bool TryResolvePlayer(int clientId, out PlayerController player)
    {
      player = null;
      if (!MultiplayerInfrastructure.Registry.Registry.TryGetEntityByClientId(clientId, out var descriptor) ||
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

    /// <summary>ClientId → 로컬 PlayerController 해석(오프라인에서는 InstanceID 기반).</summary>
    private static PlayerController ResolveLocalPlayer(int clientIdOrInstanceId)
    {
      if (clientIdOrInstanceId == InvalidClientId)
        return null;

      // 네트워크 모드: 레지스트리에서 ClientId 로 검색
      if (MultiplayerInfrastructure.Registry.Registry.TryGetEntityByClientId(clientIdOrInstanceId, out var descriptor) &&
          descriptor?.GameObject != null)
      {
        var player = descriptor.GameObject.GetComponent<PlayerController>() ??
                     descriptor.GameObject.GetComponentInChildren<PlayerController>(true);
        if (player != null)
          return player;
      }

      // 오프라인 폴백: InstanceID 로 직접 검색
      var players = FindObjectsByType<PlayerController>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        if (players[i] != null && players[i].GetInstanceID() == clientIdOrInstanceId)
          return players[i];
      }
      return null;
    }
  }
}
