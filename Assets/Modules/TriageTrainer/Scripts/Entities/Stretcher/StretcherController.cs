using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>들것 전용 운반 컨트롤러. 환자 이동 침대와 독립적으로 여섯 손잡이와 균형을 관리한다.</summary>
  public sealed class StretcherController : MonoBehaviour, IInteractable, IInteract, IInteractorConditional
  {
    [SerializeField] private string _displayText = "들것";
    [SerializeField] private Transform _visualRoot;
    [SerializeField, Min(0f)] private float _balancedVisualYOffset = 0.2f;
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _turnSpeed = 120f;

    private readonly Transform[] _handles = new Transform[6];
    private readonly PlayerController[] _occupants = new PlayerController[6];
    private readonly Dictionary<int, int> _playerHandleIndices = new();
    private Vector3 _visualBaseLocalPosition;

    public string DisplayText => _displayText;
    public Sprite DisplayIcon => null;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;
    public IInteract[] Interacts => new IInteract[] { this };
    public bool IsWeightBalanced { get; private set; }

    private void Awake()
    {
      CreateHandles();
      if (_visualRoot == null) _visualRoot = transform;
      _visualBaseLocalPosition = _visualRoot.localPosition;
    }

    private void Update()
    {
      HandlePositionSelection();
      MoveFromParticipants();
    }

    public bool CanInteract(Transform interactor) => interactor != null && interactor.GetComponentInParent<PlayerController>() != null;
    public void Interact(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null) return;
      int playerId = player.GetInstanceID();
      if (_playerHandleIndices.TryGetValue(playerId, out int current)) { Leave(player, current); return; }
      for (int i = 0; i < _occupants.Length; i++)
      {
        if (_occupants[i] != null) continue;
        Join(player, i);
        return;
      }
    }

    private void CreateHandles()
    {
      Vector3[] positions = { new(0.5f, 0f, 1f), new(-0.5f, 0f, 1f), new(-0.5f, 0f, -1f), new(0.5f, 0f, -1f), new(0f, 0f, 1.8f), new(0f, 0f, -1.8f) };
      for (int i = 0; i < positions.Length; i++)
      {
        var handle = new GameObject($"StretcherHandle{i + 1}").transform;
        handle.SetParent(transform, false);
        handle.localPosition = positions[i];
        _handles[i] = handle;
      }
    }

    private void HandlePositionSelection()
    {
      if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return;
      KeyCode[] keys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };
      for (int desired = 0; desired < keys.Length; desired++)
      {
        if (!Input.GetKeyDown(keys[desired])) continue;
        foreach (var pair in _playerHandleIndices)
        {
          var player = _occupants[pair.Value];
          if (player != null && player.IsOwner)
          {
            MoveParticipant(player, pair.Value, desired);
            return;
          }
        }
      }
    }

    private void Join(PlayerController player, int handle)
    {
      _occupants[handle] = player;
      _playerHandleIndices[player.GetInstanceID()] = handle;
      player.SetForcedFollowAnchor(_handles[handle]);
      UpdateWeightBalance();
    }

    private void Leave(PlayerController player, int handle)
    {
      _occupants[handle] = null;
      _playerHandleIndices.Remove(player.GetInstanceID());
      player.ClearForcedFollowAnchor(_handles[handle]);
      UpdateWeightBalance();
    }

    private void MoveParticipant(PlayerController player, int from, int to)
    {
      if (from == to || _occupants[to] != null) return;
      _occupants[from] = null;
      _occupants[to] = player;
      _playerHandleIndices[player.GetInstanceID()] = to;
      player.SetForcedFollowAnchor(_handles[to]);
      UpdateWeightBalance();
    }

    private void MoveFromParticipants()
    {
      int count = 0;
      float forward = 0f;
      float turn = 0f;
      for (int i = 0; i < _occupants.Length; i++)
      {
        var player = _occupants[i];
        if (player == null) continue;
        count++;
        forward += player.CurrentMoveInputVector.z;
        turn += player.CurrentMoveInputVector.x;
      }
      if (count == 0) return;
      transform.Rotate(0f, turn / count * _turnSpeed * Time.deltaTime, 0f, Space.World);
      transform.position += transform.forward * (forward / count * _moveSpeed * Time.deltaTime);
    }

    private void UpdateWeightBalance()
    {
      IsWeightBalanced = _occupants[0] != null && _occupants[3] != null || _occupants[4] != null && _occupants[5] != null || _occupants[1] != null && _occupants[2] != null && _occupants[4] != null && _occupants[5] != null;
      _visualRoot.localPosition = _visualBaseLocalPosition + (IsWeightBalanced ? Vector3.up * _balancedVisualYOffset : Vector3.zero);
    }
  }
}
