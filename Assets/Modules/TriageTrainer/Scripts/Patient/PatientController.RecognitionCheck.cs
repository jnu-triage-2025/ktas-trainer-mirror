using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>시나리오가 활성화한 동안에만 노출되는 환자 의식 확인(말 걸기/마이크) 인터랙션.</summary>
  public partial class PatientController
  {
    private const string RecognitionRoleTag = "nurse_a";
    private const float RecognitionInteractionDistance = 1.3f;
    private static readonly Vector3 RecognitionInteractionOffset = new(0f, 1f, 0f);

    private sealed class PatientRecognitionCheckInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientRecognitionCheckInteract(PatientController owner) => _owner = owner;
      public string DisplayText => RecognitionCheckMicrophoneInput.IsUnavailable
        ? "말 걸기"
        : string.IsNullOrWhiteSpace(_owner._recognitionDisplayText.Value)
          ? "말 걸기"
          : _owner._recognitionDisplayText.Value;
      public Sprite DisplayIcon => null;
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        bool microphoneFallback = _owner._recognitionMicrophoneEnabled.Value
                                  && RecognitionCheckMicrophoneInput.IsUnavailable;
        if (!_owner._recognitionCheckActive.Value
            || (!_owner._recognitionInteractionEnabled.Value && !microphoneFallback))
          return false;
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return IsRecognitionRolePlayer(player);
      }

      public void Interact(Transform interactor)
        => _owner.RequestRecognitionCheckCompletion(
          interactor?.GetComponentInParent<PlayerController>(),
          microphone: false);
    }

    private sealed class PatientRecognitionMicrophoneInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientRecognitionMicrophoneInteract(PatientController owner) => _owner = owner;
      public string DisplayText => "마이크 다시 사용하기";
      public Sprite DisplayIcon => null;
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        if (!_owner._recognitionCheckActive.Value
            || !_owner._recognitionMicrophoneEnabled.Value
            || !RecognitionCheckMicrophoneInput.IsUnavailable)
          return false;
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return IsRecognitionRolePlayer(player);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (!IsRecognitionRolePlayer(player))
          return;
        RecognitionCheckMicrophoneInput.Retry(_owner.OnRecognitionMicrophoneRetryCompleted);
      }
    }

    private readonly SyncVar<bool> _recognitionCheckActive = new(false);
    private readonly SyncVar<bool> _recognitionInteractionEnabled = new(true);
    private readonly SyncVar<bool> _recognitionMicrophoneEnabled = new(false);
    private readonly SyncVar<string> _recognitionCompletionSignal = new(string.Empty);
    private readonly SyncVar<string> _recognitionDisplayText = new("말 걸기");
    private RecognitionCheckMicrophoneInput.Availability _lastReportedMicrophoneAvailability =
      RecognitionCheckMicrophoneInput.Availability.Ready;

    private void AddRecognitionCheckInteract()
    {
      _interacts.Add(new PatientRecognitionCheckInteract(this));
      _interacts.Add(new PatientRecognitionMicrophoneInteract(this));
    }

    private void InitializeRecognitionCheckSync()
    {
      _recognitionCheckActive.OnChange += OnRecognitionCheckActiveChanged;
      RecognitionCheckMicrophoneInput.AvailabilityChanged += OnRecognitionMicrophoneAvailabilityChanged;
      RefreshRecognitionMicrophoneMonitoring();
    }

    private void TeardownRecognitionCheckSync()
    {
      _recognitionCheckActive.OnChange -= OnRecognitionCheckActiveChanged;
      RecognitionCheckMicrophoneInput.AvailabilityChanged -= OnRecognitionMicrophoneAvailabilityChanged;
      RecognitionCheckMicrophoneInput.StopMonitoring(this);
    }

    private void OnRecognitionCheckActiveChanged(bool previous, bool next, bool asServer)
    {
      if (!next)
        _lastReportedMicrophoneAvailability = RecognitionCheckMicrophoneInput.Availability.Ready;
      RefreshRecognitionMicrophoneMonitoring();
      OnRecognitionMicrophoneAvailabilityChanged();
    }

    private void OnRecognitionMicrophoneAvailabilityChanged()
    {
      RefreshRecognitionInteractableHints();
      if (!_recognitionCheckActive.Value || !_recognitionMicrophoneEnabled.Value)
        return;

      var availability = RecognitionCheckMicrophoneInput.CurrentAvailability;
      if (!RecognitionCheckMicrophoneInput.IsUnavailableState(availability)
          || availability == _lastReportedMicrophoneAvailability)
        return;

      ShowRecognitionMicrophoneGuidance(availability, false);
    }

    private static void RefreshRecognitionInteractableHints()
    {
      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      foreach (var player in players)
      {
        if (player != null && player.IsOwner)
          player.RefreshInteractableHintsNow();
      }
    }

    private void OnRecognitionMicrophoneRetryCompleted(RecognitionCheckMicrophoneInput.Availability availability)
    {
      if (!RecognitionCheckMicrophoneInput.IsUnavailableState(availability))
        return;

      ShowRecognitionMicrophoneGuidance(availability, true);
    }

    private void ShowRecognitionMicrophoneGuidance(
      RecognitionCheckMicrophoneInput.Availability availability,
      bool force)
    {
      if (!force && availability == _lastReportedMicrophoneAvailability)
        return;
      _lastReportedMicrophoneAvailability = availability;
      string guidance = RecognitionCheckMicrophoneInput.GetUnavailableGuidance(availability);
      if (string.IsNullOrWhiteSpace(guidance))
        return;
      if (_chatUI == null)
        _chatUI = Registry.Get<ChatUIController>(RegistryType.UI, Registry.TypeKey<ChatUIController>());
      if (_chatUI != null)
        _chatUI.AppendMessage($"<color=#FFD700>[System]</color> {guidance}", true);
      else
        Debug.LogWarning($"[PatientController] {guidance}", this);
    }

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
      bool interaction = ShouldEnableRecognitionInteraction(
        mic,
        ScenarioGameRules.DisableInteractionInRecognitionCheck);

      _recognitionCompletionSignal.Value = completionSignal?.Trim() ?? string.Empty;
      _recognitionDisplayText.Value = string.IsNullOrWhiteSpace(displayText) ? "말 걸기" : displayText.Trim();
      _recognitionMicrophoneEnabled.Value = mic;
      _recognitionInteractionEnabled.Value = interaction;
      _recognitionCheckActive.Value = true;
      RefreshRecognitionMicrophoneMonitoring();
    }

    private static bool ShouldEnableRecognitionInteraction(bool microphoneEnabled, bool interactionDisabled)
      => !microphoneEnabled || !interactionDisabled;

    internal void RequestRecognitionCheckCompletion(PlayerController requester = null, bool microphone = false)
    {
      if (IsServerStarted || InstanceFinder.IsOffline)
      {
        if (!InstanceFinder.IsOffline && !IsRecognitionRolePlayer(requester))
        {
          Debug.LogWarning("[PatientController] Rejected recognition completion from a player without nurse_a.", this);
          return;
        }

        TryCompleteRecognitionCheckAuthoritative(requester, microphone);
        return;
      }

      if (IsClientInitialized)
        CmdCompleteRecognitionCheck(microphone);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompleteRecognitionCheck(bool microphone, NetworkConnection sender = null)
    {
      if (!TryResolveRecognitionPlayer(sender, out var requester)
          || !IsRecognitionRolePlayer(requester))
      {
        Debug.LogWarning("[PatientController] Rejected recognition completion RPC from a player without nurse_a.", this);
        return;
      }

      TryCompleteRecognitionCheckAuthoritative(requester, microphone);
    }

    private static bool TryResolveRecognitionPlayer(NetworkConnection sender, out PlayerController player)
    {
      player = null;
      if (sender == null || !sender.IsValid
          || !Registry.TryGetEntityByClientId(sender.ClientId, out var descriptor)
          || descriptor?.GameObject == null)
        return false;

      player = descriptor.GameObject.GetComponent<PlayerController>()
               ?? descriptor.GameObject.GetComponentInChildren<PlayerController>(true);
      return player != null
             && player.Owner != null
             && player.Owner.IsValid
             && player.Owner.ClientId == sender.ClientId;
    }

    private bool TryCompleteRecognitionCheckAuthoritative(PlayerController requester, bool microphone)
    {
      if (!CanCompleteRecognitionCheck(requester, microphone))
      {
        Debug.LogWarning("[PatientController] Rejected unavailable or remote recognition completion.", this);
        return false;
      }

      string signal = _recognitionCompletionSignal.Value;
      _recognitionCheckActive.Value = false;
      RefreshRecognitionMicrophoneMonitoring();
      if (string.Equals(signal, $"{Identifier}_pupil_checked", StringComparison.Ordinal))
        NotifyPatientBCPupilCompleted();
      if (!string.IsNullOrWhiteSpace(signal))
        ScenarioInteractionSignals.Raise(signal);
      return true;
    }

    private bool CanCompleteRecognitionCheck(PlayerController requester, bool microphone)
    {
      if (!_recognitionCheckActive.Value
          || !isActiveAndEnabled
          || !gameObject.activeInHierarchy
          || _capsuleCollider == null
          || !_capsuleCollider.enabled
          || (microphone
            ? !_recognitionMicrophoneEnabled.Value
            : !_recognitionInteractionEnabled.Value
              && !_recognitionMicrophoneEnabled.Value))
        return false;

      return InstanceFinder.IsOffline
        ? requester == null || IsWithinRecognitionInteractionDistance(requester)
        : requester != null && IsWithinRecognitionInteractionDistance(requester);
    }

    private bool IsWithinRecognitionInteractionDistance(PlayerController requester)
    {
      if (requester == null || !requester.isActiveAndEnabled || !requester.gameObject.activeInHierarchy)
        return false;

      Vector3 detectionPosition = requester.transform.position + RecognitionInteractionOffset;
      Vector3 closestPoint = _capsuleCollider.ClosestPoint(detectionPosition);
      return (closestPoint - detectionPosition).sqrMagnitude
             <= RecognitionInteractionDistance * RecognitionInteractionDistance;
    }
  }
}
