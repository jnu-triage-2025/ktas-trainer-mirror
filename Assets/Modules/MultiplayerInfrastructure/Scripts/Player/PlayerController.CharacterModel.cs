using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.Serialization;

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

      ApplyPlayerModelByIdentifierServer(_defaultPlayerModelIdentifier);
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

      if (!applyCharacterControllerCenter)
        return;

      SetCharacterControllerCenter(playerCharacterModel.CharacterControllerCenter);
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
