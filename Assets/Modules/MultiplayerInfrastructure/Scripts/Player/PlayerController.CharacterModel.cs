using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  [RequireComponent(typeof(CharacterController))]
  public partial class PlayerController
  {
    private const string BuiltInDefaultPlayerModelIdentifier = "jeb";
    private const string FallbackPlayerModelIdentifier = "fallback_capsule";

    [Header("Player Model")]
    [SerializeField] private PlayerCharacterModelAttachPoint playerCharacterModelAttachPoint;
    [SerializeField] private string _defaultPlayerModelIdentifier = BuiltInDefaultPlayerModelIdentifier;
    [SerializeField] private string _currentPlayerModelIdentifier = string.Empty;

    private readonly SyncVar<string> _playerModelIdentifier = new SyncVar<string>();

    // 배정 해제 시점(OnStopServer)에는 Owner가 이미 무효일 수 있으므로 배정에 사용한 ClientId를 보관한다.
    private int _assignedPlayerModelClientId = -1;

    public string CurrentPlayerModelIdentifier => _currentPlayerModelIdentifier;

    private void Awake_PlayerModel()
    {
      if (playerCharacterModelAttachPoint == null)
        playerCharacterModelAttachPoint = GetComponentInChildren<PlayerCharacterModelAttachPoint>(true);
    }

    private void OnStartServer_PlayerModel()
    {
      if (!string.IsNullOrWhiteSpace(_playerModelIdentifier.Value))
        return;

      if (string.IsNullOrWhiteSpace(_defaultPlayerModelIdentifier))
      {
        _defaultPlayerModelIdentifier = BuiltInDefaultPlayerModelIdentifier;
        Debug.LogWarning(
          $"[PlayerController] Default player model identifier is empty in serialized data. Falling back to built-in default '{BuiltInDefaultPlayerModelIdentifier}'.",
          this);
      }

      ApplyPlayerModelByIdentifierServer(ResolveServerAssignedPlayerModelIdentifier());
    }

    private void OnStopServer_PlayerModel()
    {
      if (_assignedPlayerModelClientId < 0)
        return;

      PlayerCharacterModelAssignmentService.Release(_assignedPlayerModelClientId);
      _assignedPlayerModelClientId = -1;
    }

    /// <summary>
    /// 입장 시 서버가 이 플레이어에게 사용할 기본 모델 식별자를 결정한다.
    /// 세션 전체에서 중복되지 않도록 <see cref="PlayerCharacterModelAssignmentService"/>가 무작위로 배정하며,
    /// 배정이 불가능하면 직렬화된 기본값으로 되돌아간다.
    /// </summary>
    private string ResolveServerAssignedPlayerModelIdentifier()
    {
      if (Owner == null || !Owner.IsValid)
        return _defaultPlayerModelIdentifier;

      string assignedIdentifier = PlayerCharacterModelAssignmentService.Assign(Owner.ClientId);
      if (string.IsNullOrWhiteSpace(assignedIdentifier))
      {
        Debug.LogWarning(
          $"[PlayerController] No player model assignment candidate is available. Falling back to '{_defaultPlayerModelIdentifier}'.",
          this);
        return _defaultPlayerModelIdentifier;
      }

      _assignedPlayerModelClientId = Owner.ClientId;
      return assignedIdentifier;
    }

    private void OnStartClient_AnyPeer_PlayerModel()
    {
      _playerModelIdentifier.OnChange += OnPlayerModelIdentifierChanged;

      if (!string.IsNullOrWhiteSpace(_playerModelIdentifier.Value))
        ApplyPlayerModelByIdentifierLocal(_playerModelIdentifier.Value);
    }

    private void OnStopClient_AnyPeer_PlayerModel()
    {
      _playerModelIdentifier.OnChange -= OnPlayerModelIdentifierChanged;
    }

    private void OnPlayerModelIdentifierChanged(string prev, string next, bool asServer)
    {
      if (string.IsNullOrWhiteSpace(next))
        return;

      ApplyPlayerModelByIdentifierLocal(next);
      // 모델이 바뀌면 CharacterController 캡슐 높이도 바뀌므로 이름표 앵커를 다시 잡는다.
      RefreshOverheadNameLabel();
    }

    public void RequestSetPlayerModel(string modelIdentifier)
    {
      if (!IsOwner || string.IsNullOrWhiteSpace(modelIdentifier))
        return;

      CmdSetPlayerModel(modelIdentifier);
    }

    [ServerRpc]
    private void CmdSetPlayerModel(string modelIdentifier)
    {
      ApplyPlayerModelByIdentifierServer(modelIdentifier);
    }

    internal bool ApplyPlayerModelByIdentifierServer(string modelIdentifier)
    {
      if (!IsServerStarted || string.IsNullOrWhiteSpace(modelIdentifier))
        return false;

      _playerModelIdentifier.Value = modelIdentifier;
      _currentPlayerModelIdentifier = modelIdentifier;

      // 명령어 등으로 모델이 바뀐 경우에도 세션 배정 상태가 실제 점유를 반영하도록 갱신한다.
      if (Owner != null && Owner.IsValid)
      {
        PlayerCharacterModelAssignmentService.NotifyIdentifierApplied(Owner.ClientId, modelIdentifier);
        _assignedPlayerModelClientId = Owner.ClientId;
      }

      ApplyPlayerModelByIdentifierLocal(modelIdentifier);
      return true;
    }

    private void ApplyPlayerModelByIdentifierLocal(string modelIdentifier)
    {
      if (string.IsNullOrWhiteSpace(modelIdentifier))
        return;

      _currentPlayerModelIdentifier = modelIdentifier;

      // Lite 클론은 네트워크상의 모델 선택 상태만 유지하고 시각 프리팹은 로드하지 않는다.
      if (MppmLiteMode.IsHeadless)
      {
        ClearResolvedPlayerModelLocal();
        return;
      }

      if (!TryResolvePlayerModelObject(modelIdentifier, out var resolvedModelObject))
      {
        if (TryResolvePlayerModelObject(FallbackPlayerModelIdentifier, out var fallbackModelObject))
        {
          Debug.LogWarning(
            $"[PlayerController] Failed to resolve player model '{modelIdentifier}'. Fallback '{FallbackPlayerModelIdentifier}' will be displayed locally.",
            this);
          ApplyResolvedPlayerModelLocal(fallbackModelObject, applyCharacterControllerCenter: false);
          return;
        }

        Debug.LogWarning(
          $"[PlayerController] Failed to resolve player model '{modelIdentifier}', and fallback '{FallbackPlayerModelIdentifier}' is also unavailable. Character model will be cleared locally.",
          this);
        ClearResolvedPlayerModelLocal();
        return;
      }

      ApplyResolvedPlayerModelLocal(
        resolvedModelObject,
        applyCharacterControllerCenter: true);
    }

    private void ApplyResolvedPlayerModelLocal(GameObject modelObject, bool applyCharacterControllerCenter)
    {
      if (playerCharacterModelAttachPoint == null)
      {
        Debug.LogWarning("[PlayerController] PlayerModelAttachPoint is not assigned.", this);
        return;
      }

      var modelInstance = playerCharacterModelAttachPoint.ReplaceAttachedModel(modelObject);
      if (modelInstance == null)
      {
        SetCharacterModelAnimator(null);
        return;
      }

      if (!TryResolvePlayerModelComponent(modelInstance, out var playerCharacterModel))
      {
        SetCharacterModelAnimator(null);
        return;
      }

      SetCharacterModelAnimator(playerCharacterModel.Animator);
      SetHeldItemAttachPoint(playerCharacterModel.HeldItemAttachPoint);

      if (!applyCharacterControllerCenter)
      {
        IgnoreCollisionsWithActivePlayers();
        return;
      }

      SetCharacterControllerCenter(playerCharacterModel.CharacterControllerCenter);
      // 새 모델이 자체 Collider를 포함할 수 있으므로, 모델 교체 뒤에도 플레이어 간 충돌 무시를 다시 적용한다.
      IgnoreCollisionsWithActivePlayers();
    }

    private void ClearResolvedPlayerModelLocal()
    {
      if (playerCharacterModelAttachPoint == null)
      {
        Debug.LogWarning("[PlayerController] PlayerModelAttachPoint is not assigned.", this);
        return;
      }

      playerCharacterModelAttachPoint.ClearAttachedModel();
      SetCharacterModelAnimator(null);
      SetHeldItemAttachPoint(null);
    }

    private static bool TryResolvePlayerModelObject(string modelIdentifier, out GameObject playerModelObject)
    {
      playerModelObject = null;
      if (string.IsNullOrWhiteSpace(modelIdentifier))
        return false;

      var raw = Registry.Registry.Get<object>(RegistryType.PlayerModel, modelIdentifier);
      if (raw == null)
        return false;

      if (raw is GameObject gameObject)
      {
        if (!gameObject.TryGetComponent<IPlayerCharacterModelObject>(out _))
          return false;

        playerModelObject = gameObject;
        return true;
      }

      if (raw is Component component && component is IPlayerCharacterModelObject)
      {
        playerModelObject = component.gameObject;
        return true;
      }

      return false;
    }

    private static bool TryResolvePlayerModelComponent(GameObject modelObject, out IPlayerCharacterModelObject playerCharacterModelObject)
    {
      playerCharacterModelObject = null;
      if (modelObject == null)
        return false;

      if (!modelObject.TryGetComponent<IPlayerCharacterModelObject>(out var modelComponent))
        return false;

      playerCharacterModelObject = modelComponent;
      return true;
    }

    private void SetCharacterControllerCenter(Vector3 center)
    {
      if (_characterController == null)
        _characterController = GetComponent<CharacterController>();

      if (_characterController == null)
      {
        Debug.LogWarning("[PlayerController] CharacterController is not assigned.", this);
        return;
      }

      _characterController.center = center;
    }
  }
}
