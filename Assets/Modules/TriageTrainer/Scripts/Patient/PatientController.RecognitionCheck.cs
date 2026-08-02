using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>시나리오가 활성화한 동안에만 노출되는 환자 의식 확인(말 걸기/마이크) 인터랙션.</summary>
  public partial class PatientController
  {
    private const string RecognitionRoleTag = "nurse_a";

    private sealed class PatientRecognitionCheckInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientRecognitionCheckInteract(PatientController owner) => _owner = owner;
      public string DisplayText => string.IsNullOrWhiteSpace(_owner._recognitionDisplayText.Value)
        ? "말 걸기"
        : _owner._recognitionDisplayText.Value;
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        if (!_owner._recognitionCheckActive.Value || !_owner._recognitionInteractionEnabled.Value)
          return false;
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return IsRecognitionRolePlayer(player);
      }

      public void Interact(Transform interactor)
        => _owner.RequestRecognitionCheckCompletion(interactor?.GetComponentInParent<PlayerController>());
    }

    private readonly SyncVar<bool> _recognitionCheckActive = new(false);
    private readonly SyncVar<bool> _recognitionInteractionEnabled = new(true);
    private readonly SyncVar<bool> _recognitionMicrophoneEnabled = new(false);
    private readonly SyncVar<string> _recognitionCompletionSignal = new(string.Empty);
    private readonly SyncVar<string> _recognitionDisplayText = new("말 걸기");

    private void AddRecognitionCheckInteract() => _interacts.Add(new PatientRecognitionCheckInteract(this));

    private void InitializeRecognitionCheckSync()
    {
      _recognitionCheckActive.OnChange += OnRecognitionCheckActiveChanged;
      RefreshRecognitionMicrophoneMonitoring();
    }

    private void TeardownRecognitionCheckSync()
    {
      _recognitionCheckActive.OnChange -= OnRecognitionCheckActiveChanged;
      RecognitionCheckMicrophoneInput.StopMonitoring(this);
    }

    private void OnRecognitionCheckActiveChanged(bool previous, bool next, bool asServer)
      => RefreshRecognitionMicrophoneMonitoring();

    private void RefreshRecognitionMicrophoneMonitoring()
    {
      if (_recognitionCheckActive.Value
          && _recognitionMicrophoneEnabled.Value
          && ShouldMonitorRecognitionMicrophoneLocally())
        RecognitionCheckMicrophoneInput.StartMonitoring(this);
      else
        RecognitionCheckMicrophoneInput.StopMonitoring(this);
    }

    private static bool IsRecognitionRolePlayer(PlayerController player)
    {
      if (InstanceFinder.IsOffline)
        return player != null;
      return player != null
             && !string.IsNullOrWhiteSpace(player.UserIdentifier)
             && PlayerTagService.HasTag(player.UserIdentifier, RecognitionRoleTag);
    }

    private static bool IsRecognitionRoleIdentifier(string userIdentifier)
      => !string.IsNullOrWhiteSpace(userIdentifier)
         && PlayerTagService.HasTag(userIdentifier, RecognitionRoleTag);

    private static bool ShouldMonitorRecognitionMicrophoneLocally()
    {
      if (InstanceFinder.IsOffline)
        return true;
      if (!InstanceFinder.IsClientStarted)
        return false;

      var local = InstanceFinder.ClientManager?.Connection;
      return local != null
             && UserDescriptorService.TryGetByClientId(local.ClientId, out var descriptor)
             && descriptor != null
             && IsRecognitionRoleIdentifier(descriptor.Identifier);
    }

    /// <summary>서버 시나리오 이벤트가 한 단계의 의식 확인을 활성화한다.</summary>
    public void ActivateRecognitionCheck(string completionSignal, bool allowMicrophone, string displayText = "말 걸기")
    {
      if (!IsServerStarted && !InstanceFinder.IsOffline)
        return;

      bool mic = allowMicrophone && ScenarioGameRules.UseMicInRecognitionCheck;
      bool interaction = !ScenarioGameRules.DisableInteractionInRecognitionCheck;
      if (!mic && !interaction)
      {
        Debug.LogError("[PatientController] Recognition check has no enabled input path.", this);
        return;
      }

      _recognitionCompletionSignal.Value = completionSignal?.Trim() ?? string.Empty;
      _recognitionDisplayText.Value = string.IsNullOrWhiteSpace(displayText) ? "말 걸기" : displayText.Trim();
      _recognitionMicrophoneEnabled.Value = mic;
      _recognitionInteractionEnabled.Value = interaction;
      _recognitionCheckActive.Value = true;
      RefreshRecognitionMicrophoneMonitoring();
    }

    internal void RequestRecognitionCheckCompletion(PlayerController requester = null)
    {
      if (IsServerStarted || InstanceFinder.IsOffline)
      {
        if (!InstanceFinder.IsOffline)
        {
          string requesterIdentifier = requester?.UserIdentifier;
          if (string.IsNullOrWhiteSpace(requesterIdentifier))
          {
            var local = InstanceFinder.ClientManager?.Connection;
            if (local != null
                && UserDescriptorService.TryGetByClientId(local.ClientId, out var descriptor)
                && descriptor != null)
              requesterIdentifier = descriptor.Identifier;
          }

          if (!IsRecognitionRoleIdentifier(requesterIdentifier))
          {
            Debug.LogWarning("[PatientController] Rejected recognition completion from a player without nurse_a.", this);
            return;
          }
        }

        CompleteRecognitionCheckAuthoritative();
        return;
      }

      if (IsClientInitialized)
        CmdCompleteRecognitionCheck();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompleteRecognitionCheck(NetworkConnection sender = null)
    {
      if (sender == null
          || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || descriptor == null
          || !IsRecognitionRoleIdentifier(descriptor.Identifier))
      {
        Debug.LogWarning("[PatientController] Rejected recognition completion RPC from a player without nurse_a.", this);
        return;
      }

      CompleteRecognitionCheckAuthoritative();
    }

    private void CompleteRecognitionCheckAuthoritative()
    {
      if (!_recognitionCheckActive.Value)
        return;

      string signal = _recognitionCompletionSignal.Value;
      _recognitionCheckActive.Value = false;
      RefreshRecognitionMicrophoneMonitoring();
      if (!string.IsNullOrWhiteSpace(signal))
        ScenarioInteractionSignals.Raise(signal);
    }
  }
}
