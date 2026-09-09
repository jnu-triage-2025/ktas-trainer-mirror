using System;
using System.Collections.Generic;
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
using TriageTrainer.ItemDefinitions;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 시나리오가 연 의식 확인 항목 하나의 복제 상태.
  ///
  /// <para>
  /// 완료 신호(<see cref="CompletionSignal"/>)가 항목의 식별자다. 한 환자에게 여러 역할이 동시에
  /// 다른 확인 항목을 여는 흐름(예: nurse_a 의 말 걸기와 nurse_c 의 동공반사 확인)이 있으므로,
  /// 슬롯 하나를 덮어쓰지 않고 이 항목들을 목록으로 함께 보관한다.
  /// </para>
  ///
  /// FishNet 코드 생성기가 직렬화기를 만들 수 있도록 공개 필드만 사용한다.
  /// </summary>
  public struct PatientRecognitionCheckEntry : IEquatable<PatientRecognitionCheckEntry>
  {
    /// <summary>완료 시 올릴 시나리오 신호이자 이 항목의 식별자.</summary>
    public string CompletionSignal;

    /// <summary>상호작용 힌트에 표시할 문구.</summary>
    public string DisplayText;

    /// <summary>이 항목을 수행할 수 있는 역할 태그. 비어 있으면 역할을 제한하지 않는다.</summary>
    public string RequiredRoleTag;

    /// <summary>마이크 음량으로 완료할 수 있는 항목인지 여부.</summary>
    public bool MicrophoneEnabled;

    /// <summary>상호작용(말 걸기 등)으로 완료할 수 있는 항목인지 여부.</summary>
    public bool InteractionEnabled;

    public PatientRecognitionCheckEntry(
      string completionSignal,
      string displayText,
      string requiredRoleTag,
      bool microphoneEnabled,
      bool interactionEnabled)
    {
      CompletionSignal = completionSignal ?? string.Empty;
      DisplayText = displayText ?? string.Empty;
      RequiredRoleTag = requiredRoleTag ?? string.Empty;
      MicrophoneEnabled = microphoneEnabled;
      InteractionEnabled = interactionEnabled;
    }

    public bool Equals(PatientRecognitionCheckEntry other) =>
      string.Equals(CompletionSignal, other.CompletionSignal, StringComparison.Ordinal)
      && string.Equals(DisplayText, other.DisplayText, StringComparison.Ordinal)
      && string.Equals(RequiredRoleTag, other.RequiredRoleTag, StringComparison.Ordinal)
      && MicrophoneEnabled == other.MicrophoneEnabled
      && InteractionEnabled == other.InteractionEnabled;

    public override bool Equals(object obj) => obj is PatientRecognitionCheckEntry other && Equals(other);

    public override int GetHashCode()
    {
      unchecked
      {
        int hash = CompletionSignal != null ? CompletionSignal.GetHashCode() : 0;
        hash = (hash * 397) ^ (DisplayText != null ? DisplayText.GetHashCode() : 0);
        hash = (hash * 397) ^ (RequiredRoleTag != null ? RequiredRoleTag.GetHashCode() : 0);
        hash = (hash * 397) ^ MicrophoneEnabled.GetHashCode();
        hash = (hash * 397) ^ InteractionEnabled.GetHashCode();
        return hash;
      }
    }
  }

  /// <summary>시나리오가 활성화한 동안에만 노출되는 환자 의식 확인(말 걸기/마이크) 인터랙션.</summary>
  public partial class PatientController
  {
    private const float RecognitionInteractionDistance = 1.3f;
    private static readonly Vector3 RecognitionInteractionOffset = new(0f, 1f, 0f);

    /// <summary>
    /// 퀘스트 표시 주소는 (환자 식별자, 상호작용 식별자) 한 쌍이므로, 동시에 열린 확인 항목들은
    /// 모두 이 하나의 상호작용 식별자를 그대로 쓴다. 항목 구분은 완료 신호로만 하고 표시 주소는
    /// 나누지 않는다. 항목마다 다른 식별자를 쓰면 patient_b_c_ct.quests.quest 의
    /// "recognition_check" 바인딩(Quest_B_Recognition, Quest_B_Strength, Quest_B_Pupil_IV 등)이
    /// 어느 항목에도 닿지 않게 되기 때문이다.
    /// </summary>
    private const string RecognitionInteractionIdentifier = "recognition_check";

    private sealed class PatientRecognitionCheckInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;

      /// <summary>이 인터랙션이 담당하는 확인 항목의 완료 신호(항목 식별자).</summary>
      private readonly string _completionSignal;
      private readonly string _interactionIdentifier;

      public PatientRecognitionCheckInteract(PatientController owner, string completionSignal, string interactionIdentifier = null)
      {
        _owner = owner;
        _completionSignal = completionSignal ?? string.Empty;
        _interactionIdentifier = string.IsNullOrWhiteSpace(interactionIdentifier)
          ? RecognitionInteractionIdentifier
          : interactionIdentifier.Trim();
      }

      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => _interactionIdentifier;
      public string CompletionSignal => _completionSignal;

      public string DisplayText
      {
        get
        {
          if (!_owner.TryGetRecognitionCheck(_completionSignal, out var entry))
            return "말 걸기";

          // 마이크를 쓰는 항목은 마이크를 쓸 수 없을 때 대체 수단인 "말 걸기"로 안내한다.
          // 마이크와 무관한 항목(동공반사 확인 등)은 여러 항목이 동시에 열려 있어도
          // 서로 구분되도록 자기 문구를 유지한다.
          if (entry.MicrophoneEnabled && RecognitionCheckMicrophoneInput.IsUnavailable)
            return "말 걸기";

          return string.IsNullOrWhiteSpace(entry.DisplayText) ? "말 걸기" : entry.DisplayText;
        }
      }

      public Sprite DisplayIcon => null;
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        if (!_owner.TryGetRecognitionCheck(_completionSignal, out var entry))
          return false;

        bool microphoneFallback = entry.MicrophoneEnabled && RecognitionCheckMicrophoneInput.IsUnavailable;
        if (!entry.InteractionEnabled && !microphoneFallback)
          return false;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return IsRecognitionRolePlayer(player, entry.RequiredRoleTag);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor?.GetComponentInParent<PlayerController>();
        if (_owner.RequiresPenlightForRecognition(_completionSignal) && !HasPenlight(player))
        {
          _owner.ShowRequiredItemDialogue("펜라이트를 갖고 있지 않다.", "펜라이트를 찾자.");
          return;
        }

        _owner.RequestRecognitionCheckCompletion(player, microphone: false, completionSignal: _completionSignal);
      }
    }

    /// <summary>
    /// 마이크 장치를 다시 여는 인터랙션. 장치 단위 조치라 항목마다 만들지 않고 하나만 두되,
    /// 노출 조건은 "이 플레이어가 마이크로 완료할 수 있는 항목이 하나라도 열려 있는가"로 판정한다.
    /// </summary>
    private sealed class PatientRecognitionMicrophoneInteract : IInteract, IInteractorConditional, IInteractionRegistryExempt
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
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return _owner.HasMicrophoneRecognitionCheckForPlayer(player)
               && RecognitionCheckMicrophoneInput.IsUnavailable;
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (!_owner.HasMicrophoneRecognitionCheckForPlayer(player))
          return;
        RecognitionCheckMicrophoneInput.Retry(_owner.OnRecognitionMicrophoneRetryCompleted);
      }
    }

    /// <summary>
    /// 지금 열려 있는 의식 확인 항목들. 슬롯 하나가 아니라 목록으로 두어, 같은 환자에게 서로 다른
    /// 역할의 확인 항목이 동시에 열려도 나중 항목이 앞 항목을 지우지 않는다.
    /// </summary>
    private readonly SyncList<PatientRecognitionCheckEntry> _recognitionChecks = new();

    /// <summary>완료 신호별 인터랙션 인스턴스. 힌트 선택과 실행이 같은 인스턴스를 가리켜야 한다.</summary>
    private readonly Dictionary<string, PatientRecognitionCheckInteract> _recognitionCheckInteracts =
      new(StringComparer.Ordinal);

    private PatientRecognitionMicrophoneInteract _recognitionMicrophoneInteract;
    private ScenarioController _recognitionScenarioController;

    private RecognitionCheckMicrophoneInput.Availability _lastReportedMicrophoneAvailability =
      RecognitionCheckMicrophoneInput.Availability.Ready;

    private bool HasActiveRecognitionCheck => _recognitionChecks.Count > 0;

#if UNITY_E2E || UNITY_EDITOR
    /// <summary>E2E 관측용: 이 환자 인스턴스에 실제로 복제된 확인 항목을 노출한다.</summary>
    public string[] AutomationRecognitionCheckSignals()
    {
      var signals = new string[_recognitionChecks.Count];
      for (int i = 0; i < _recognitionChecks.Count; i++)
        signals[i] = _recognitionChecks[i].CompletionSignal ?? string.Empty;
      return signals;
    }
#endif

    /// <summary>
    /// 마이크 다시 사용 인터랙션만 엔티티 목록에 둔다. 확인 항목 인터랙션은 시나리오 데이터의
    /// interactions 정의(handlerKey "recognition_check")가 선언하고, 레지스트리가 <see cref="TryCreateInteractionHandler"/>
    /// 로 이 환자에게 핸들러를 만들어 붙인다.
    /// </summary>
    private void AddRecognitionCheckInteract()
    {
      _interacts.Add(_recognitionMicrophoneInteract ??= new PatientRecognitionMicrophoneInteract(this));
    }

    /// <summary>
    /// 시나리오 데이터가 선언한 의식 확인 인터랙션의 핸들러를 만든다. 정의의 completionSignal 이 확인 항목의
    /// 식별자이며, 같은 신호의 핸들러는 인스턴스를 재사용해 힌트 선택과 실행이 어긋나지 않게 한다.
    /// </summary>
    private bool TryCreateRecognitionCheckHandler(InteractionDefinition definition, out IInteract handler)
    {
      handler = null;
      if (definition == null || !string.Equals(definition.HandlerKey, RecognitionInteractionIdentifier, StringComparison.Ordinal))
        return false;
      string signal = definition.CompletionSignal?.Trim();
      if (string.IsNullOrWhiteSpace(signal))
      {
        Debug.LogWarning($"[PatientController] recognition_check definition '{definition.InteractionIdentifier}' requires completionSignal.", this);
        return false;
      }
      if (!_recognitionCheckInteracts.TryGetValue(signal, out var interact) || interact == null
          || !string.Equals(interact.InteractionIdentifier, definition.InteractionIdentifier, StringComparison.Ordinal))
      {
        interact = new PatientRecognitionCheckInteract(this, signal, definition.InteractionIdentifier);
        _recognitionCheckInteracts[signal] = interact;
      }
      handler = interact;
      return true;
    }

    /// <summary>
    /// 활성 항목을 환자 자체 상호작용 목록에도 반영한다. 시나리오 데이터 정의가 엔티티 스폰 뒤에
    /// 보류 해제되는 정상 경로에서는 레지스트리가 같은 핸들러를 제공한다. 그러나 역할 분기의
    /// 활성화 시점에는 그 보류 정의가 아직 적용되지 않을 수 있으므로, 이 목록을 함께 유지해야
    /// 실제 플레이어의 상호작용 힌트가 사라지지 않는다.
    /// </summary>
    private void RebuildRecognitionCheckInteracts()
    {
      foreach (var entry in _recognitionChecks)
      {
        string signal = entry.CompletionSignal ?? string.Empty;
        if (string.IsNullOrWhiteSpace(signal))
          continue;
        if (!_recognitionCheckInteracts.TryGetValue(signal, out var interact) || interact == null)
        {
          interact = new PatientRecognitionCheckInteract(this, signal);
          _recognitionCheckInteracts[signal] = interact;
        }
        if (!_interacts.Contains(interact))
          _interacts.Add(interact);
      }

      for (int i = _interacts.Count - 1; i >= 0; i--)
      {
        if (_interacts[i] is not PatientRecognitionCheckInteract interact)
          continue;
        if (!TryGetRecognitionCheck(interact.CompletionSignal, out _))
          _interacts.RemoveAt(i);
      }
      InteractionRegistry.RequestHintRefresh();
    }

    private void InitializeRecognitionCheckSync()
    {
      _recognitionChecks.OnChange += OnRecognitionChecksChanged;
      RecognitionCheckMicrophoneInput.AvailabilityChanged += OnRecognitionMicrophoneAvailabilityChanged;
      HandleRecognitionChecksChanged();
    }

    private void TeardownRecognitionCheckSync()
    {
      _recognitionChecks.OnChange -= OnRecognitionChecksChanged;
      RecognitionCheckMicrophoneInput.AvailabilityChanged -= OnRecognitionMicrophoneAvailabilityChanged;
      RecognitionCheckMicrophoneInput.StopMonitoring(this);
    }

    /// <summary>
    /// 시나리오가 끝나면 이전 회차에서 열어 둔 확인 항목을 정리한다. 네트워크 수명주기 콜백은
    /// 데디케이티드 서버·호스트·오프라인에서 서로 달라, 모든 실행 형태를 같은 방식으로 다루기 위해
    /// 컴포넌트 활성 수명주기에 건다.
    /// </summary>
    private void OnEnable()
    {
      ScenarioController.InstanceAvailable += HandleRecognitionScenarioControllerAvailable;
      BindRecognitionScenarioController(ScenarioController.Instance);
    }

    private void OnDisable()
    {
      ScenarioController.InstanceAvailable -= HandleRecognitionScenarioControllerAvailable;
      BindRecognitionScenarioController(null);
    }

    private void HandleRecognitionScenarioControllerAvailable(ScenarioController controller)
      => BindRecognitionScenarioController(controller);

    private void BindRecognitionScenarioController(ScenarioController controller)
    {
      if (ReferenceEquals(_recognitionScenarioController, controller))
        return;

      if (_recognitionScenarioController != null)
        _recognitionScenarioController.OnScenarioEnded -= HandleRecognitionScenarioEnded;
      _recognitionScenarioController = controller;
      if (_recognitionScenarioController != null)
        _recognitionScenarioController.OnScenarioEnded += HandleRecognitionScenarioEnded;
    }

    private void HandleRecognitionScenarioEnded() => ClearRecognitionChecks();

    private void OnRecognitionChecksChanged(
      SyncListOperation operation,
      int index,
      PatientRecognitionCheckEntry previous,
      PatientRecognitionCheckEntry next,
      bool asServer)
    {
      HandleRecognitionChecksChanged();
      OnRecognitionMicrophoneAvailabilityChanged();
    }

    /// <summary>
    /// 활성 항목 변화에 뒤따르는 로컬 반영. 권위 경로에서 직접 부르고, 복제로 값이 바뀐 피어에서는
    /// SyncList 콜백이 부른다(두 번 불려도 결과는 같다).
    /// </summary>
    private void HandleRecognitionChecksChanged()
    {
      if (!HasActiveRecognitionCheck)
        _lastReportedMicrophoneAvailability = RecognitionCheckMicrophoneInput.Availability.Ready;
      RebuildRecognitionCheckInteracts();
      RefreshRecognitionMicrophoneMonitoring();
    }

    private void OnRecognitionMicrophoneAvailabilityChanged()
    {
      RefreshRecognitionInteractableHints();
      if (!HasMicrophoneRecognitionCheckForRole(null, ignoreRole: true))
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

    /// <summary>마이크로 완료할 수 있는 항목이 하나도 없으면 감시를 끄고, 하나라도 있으면 켠다.</summary>
    private void RefreshRecognitionMicrophoneMonitoring()
    {
      if (ShouldMonitorRecognitionMicrophoneLocally())
        RecognitionCheckMicrophoneInput.StartMonitoring(this);
      else
        RecognitionCheckMicrophoneInput.StopMonitoring(this);
    }

    /// <summary>
    /// 역할 태그가 비어 있으면 역할을 제한하지 않는다는 뜻이다.
    /// 오프라인 단독 실행에서는 역할을 따지지 않는다.
    /// </summary>
    private static bool IsRecognitionRolePlayer(PlayerController player, string requiredRoleTag)
    {
      if (player == null)
        return false;
      if (InstanceFinder.IsOffline)
        return true;
      return IsRecognitionRoleIdentifier(player.UserIdentifier, requiredRoleTag);
    }

    private static bool IsRecognitionRoleIdentifier(string userIdentifier, string requiredRoleTag)
    {
      if (string.IsNullOrWhiteSpace(requiredRoleTag))
        return true;
      return !string.IsNullOrWhiteSpace(userIdentifier)
             && TriageTrainer.Utils.TriageRoleGate.IsAllowed(userIdentifier, requiredRoleTag);
    }

    /// <summary>
    /// 마이크로 완료할 수 있는 항목이 있는지 판정한다. <paramref name="ignoreRole"/> 가 true 면
    /// 역할을 따지지 않고 마이크 항목의 존재만 본다.
    /// </summary>
    private bool HasMicrophoneRecognitionCheckForRole(string userIdentifier, bool ignoreRole)
    {
      for (int i = 0; i < _recognitionChecks.Count; i++)
      {
        var entry = _recognitionChecks[i];
        if (!entry.MicrophoneEnabled)
          continue;
        if (ignoreRole || IsRecognitionRoleIdentifier(userIdentifier, entry.RequiredRoleTag))
          return true;
      }

      return false;
    }

    private bool HasMicrophoneRecognitionCheckForPlayer(PlayerController player)
    {
      if (player == null)
        return false;
      return InstanceFinder.IsOffline
        ? HasMicrophoneRecognitionCheckForRole(null, ignoreRole: true)
        : HasMicrophoneRecognitionCheckForRole(player.UserIdentifier, ignoreRole: false);
    }

    /// <summary>로컬 플레이어가 마이크로 완료할 수 있는 항목이 하나라도 열려 있는지 판정한다.</summary>
    private bool ShouldMonitorRecognitionMicrophoneLocally()
    {
      if (InstanceFinder.IsOffline)
        return HasMicrophoneRecognitionCheckForRole(null, ignoreRole: true);
      if (!InstanceFinder.IsClientStarted)
        return false;

      var local = InstanceFinder.ClientManager?.Connection;
      return local != null
             && UserDescriptorService.TryGetByClientId(local.ClientId, out var descriptor)
             && descriptor != null
             && HasMicrophoneRecognitionCheckForRole(descriptor.Identifier, ignoreRole: false);
    }

    /// <summary>
    /// 서버 시나리오 이벤트가 한 단계의 의식 확인을 활성화한다. 이미 같은 완료 신호의 항목이 열려
    /// 있으면 그 항목을 갱신하고, 아니면 항목을 하나 더 연다.
    /// </summary>
    /// <param name="requiredRoleTag">
    /// 이 항목을 수행할 역할 태그. 비워 두면 역할을 제한하지 않는다.
    /// </param>
    public void ActivateRecognitionCheck(
      string completionSignal,
      bool allowMicrophone,
      string requiredRoleTag,
      string displayText = "말 걸기")
    {
      if (IsFishNetServerStarted || InstanceFinder.IsOffline)
      {
        ActivateRecognitionCheckAuthoritative(
          completionSignal,
          allowMicrophone,
          requiredRoleTag,
          displayText);
        return;
      }

      // 역할 분기는 담당 클라이언트 한 곳에서 먼저 실행될 수 있다. 그 경우에도 활성 상태는
      // 서버 SyncList에 기록되어야 다른 모든 피어가 같은 인식 항목을 보게 된다.
      if (IsFishNetClientInitialized)
        CmdActivateRecognitionCheck(
          completionSignal ?? string.Empty,
          allowMicrophone,
          requiredRoleTag ?? string.Empty,
          displayText ?? string.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdActivateRecognitionCheck(
      string completionSignal,
      bool allowMicrophone,
      string requiredRoleTag,
      string displayText)
    {
      ActivateRecognitionCheckAuthoritative(
        completionSignal,
        allowMicrophone,
        requiredRoleTag,
        displayText);
    }

    private void ActivateRecognitionCheckAuthoritative(
      string completionSignal,
      bool allowMicrophone,
      string requiredRoleTag,
      string displayText)
    {

      bool mic = allowMicrophone && ScenarioGameRules.UseMicInRecognitionCheck;
      var entry = new PatientRecognitionCheckEntry(
        completionSignal?.Trim() ?? string.Empty,
        string.IsNullOrWhiteSpace(displayText) ? "말 걸기" : displayText.Trim(),
        requiredRoleTag?.Trim() ?? string.Empty,
        mic,
        ShouldEnableRecognitionInteraction(mic, ScenarioGameRules.DisableInteractionInRecognitionCheck));

      UpsertRecognitionCheck(entry);
      // PatientController is an entity spawned after the scenario starts. In
      // that ordering, a SyncList delta can be applied before the client has
      // installed its hint callback. Mirror the authoritative delta through
      // an observer RPC so every connected player rebuilds the visible
      // interaction immediately; the SyncList remains the late-join state.
      if (IsFishNetServerStarted)
        RpcUpsertRecognitionCheck(entry.CompletionSignal, entry.DisplayText,
          entry.RequiredRoleTag, entry.MicrophoneEnabled, entry.InteractionEnabled);
    }

    private void UpsertRecognitionCheck(PatientRecognitionCheckEntry entry)
    {
      int index = IndexOfRecognitionCheck(entry.CompletionSignal);
      if (index >= 0) _recognitionChecks[index] = entry;
      else _recognitionChecks.Add(entry);
      HandleRecognitionChecksChanged();
    }

    [ObserversRpc]
    private void RpcUpsertRecognitionCheck(string completionSignal, string displayText,
      string requiredRoleTag, bool microphoneEnabled, bool interactionEnabled)
    {
      if (IsFishNetServerStarted || InstanceFinder.IsOffline) return;
      UpsertRecognitionCheck(new PatientRecognitionCheckEntry(completionSignal,
        displayText, requiredRoleTag, microphoneEnabled, interactionEnabled));
    }

    /// <summary>완료되지 않은 확인 항목을 시나리오 쪽에서 닫는다.</summary>
    public bool DeactivateRecognitionCheck(string completionSignal)
    {
      if (!IsFishNetServerStarted && !InstanceFinder.IsOffline)
        return false;

      int index = IndexOfRecognitionCheck(completionSignal?.Trim() ?? string.Empty);
      if (index < 0)
        return false;

      _recognitionChecks.RemoveAt(index);
      HandleRecognitionChecksChanged();
      if (IsFishNetServerStarted) RpcRemoveRecognitionCheck(completionSignal?.Trim() ?? string.Empty);
      return true;
    }

    /// <summary>열려 있는 확인 항목을 모두 닫는다(시나리오 종료·재시작 정리).</summary>
    public void ClearRecognitionChecks()
    {
      if (!IsFishNetServerStarted && !InstanceFinder.IsOffline)
        return;
      if (_recognitionChecks.Count == 0)
        return;

      _recognitionChecks.Clear();
      HandleRecognitionChecksChanged();
      if (IsFishNetServerStarted) RpcClearRecognitionChecks();
    }

    private int IndexOfRecognitionCheck(string completionSignal)
    {
      string signal = completionSignal ?? string.Empty;
      for (int i = 0; i < _recognitionChecks.Count; i++)
      {
        if (string.Equals(_recognitionChecks[i].CompletionSignal ?? string.Empty, signal, StringComparison.Ordinal))
          return i;
      }

      return -1;
    }

    private bool TryGetRecognitionCheck(string completionSignal, out PatientRecognitionCheckEntry entry)
    {
      int index = IndexOfRecognitionCheck(completionSignal);
      if (index < 0)
      {
        entry = default;
        return false;
      }

      entry = _recognitionChecks[index];
      return true;
    }

    private static bool ShouldEnableRecognitionInteraction(bool microphoneEnabled, bool interactionDisabled)
      => !microphoneEnabled || !interactionDisabled;

    /// <summary>
    /// 어느 항목을 완료하는지 <paramref name="completionSignal"/> 로 지정한다. 비워 두면
    /// 요청자가 지금 완료할 수 있는 항목 하나를 서버가 고른다(마이크 경로처럼 항목을 지목할 수
    /// 없는 입력에서 쓴다).
    /// </summary>
    internal void RequestRecognitionCheckCompletion(
      PlayerController requester = null,
      bool microphone = false,
      string completionSignal = null)
    {
      if (IsFishNetServerStarted || InstanceFinder.IsOffline)
      {
        TryCompleteRecognitionCheckAuthoritative(requester, microphone, completionSignal);
        return;
      }

      if (IsFishNetClientInitialized)
        CmdCompleteRecognitionCheck(microphone, completionSignal ?? string.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompleteRecognitionCheck(
      bool microphone,
      string completionSignal,
      NetworkConnection sender = null)
    {
      if (!TryResolveRecognitionPlayer(sender, out var requester))
      {
        Debug.LogWarning("[PatientController] Rejected recognition completion RPC from an unresolved player.", this);
        return;
      }

      TryCompleteRecognitionCheckAuthoritative(requester, microphone, completionSignal);
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

    private bool TryCompleteRecognitionCheckAuthoritative(
      PlayerController requester,
      bool microphone,
      string completionSignal)
    {
      if (!TryResolveCompletableRecognitionCheck(requester, microphone, completionSignal, out int index))
      {
        Debug.LogWarning(
          "[PatientController] Rejected unavailable, mismatched or unauthorized recognition completion.", this);
        return false;
      }

      string signal = _recognitionChecks[index].CompletionSignal;
      _recognitionChecks.RemoveAt(index);
      HandleRecognitionChecksChanged();
      if (IsFishNetServerStarted) RpcRemoveRecognitionCheck(signal);
      if (string.Equals(signal, $"{Identifier}_pupil_checked", StringComparison.Ordinal))
        NotifyPatientBCPupilCompleted();
      if (!string.IsNullOrWhiteSpace(signal))
        ScenarioInteractionSignals.Raise(signal);
      return true;
    }

    [ObserversRpc]
    private void RpcRemoveRecognitionCheck(string completionSignal)
    {
      if (IsFishNetServerStarted || InstanceFinder.IsOffline) return;
      int index = IndexOfRecognitionCheck(completionSignal);
      if (index < 0) return;
      _recognitionChecks.RemoveAt(index);
      HandleRecognitionChecksChanged();
    }

    [ObserversRpc]
    private void RpcClearRecognitionChecks()
    {
      if (IsFishNetServerStarted || InstanceFinder.IsOffline) return;
      if (_recognitionChecks.Count == 0) return;
      _recognitionChecks.Clear();
      HandleRecognitionChecksChanged();
    }

    /// <summary>
    /// 요청을 받아들일 항목을 서버 권위로 고른다. 완료 신호가 주어지면 그 항목만 검사하고,
    /// 비어 있으면 요청자가 지금 완료할 수 있는 첫 항목을 고른다. 어느 경우든 항목의 역할 태그와
    /// 준비물·거리 조건을 모두 통과해야 한다.
    /// </summary>
    private bool TryResolveCompletableRecognitionCheck(
      PlayerController requester,
      bool microphone,
      string completionSignal,
      out int index)
    {
      index = -1;
      if (!isActiveAndEnabled
          || !gameObject.activeInHierarchy
          || _capsuleCollider == null
          || !_capsuleCollider.enabled)
        return false;

      bool targeted = !string.IsNullOrWhiteSpace(completionSignal);
      string signal = completionSignal?.Trim() ?? string.Empty;
      for (int i = 0; i < _recognitionChecks.Count; i++)
      {
        var entry = _recognitionChecks[i];
        if (targeted
            && !string.Equals(entry.CompletionSignal ?? string.Empty, signal, StringComparison.Ordinal))
          continue;
        if (!CanCompleteRecognitionCheck(entry, requester, microphone))
          continue;

        index = i;
        return true;
      }

      return false;
    }

    private bool CanCompleteRecognitionCheck(
      PatientRecognitionCheckEntry entry,
      PlayerController requester,
      bool microphone)
    {
      if (microphone
        ? !entry.MicrophoneEnabled
        : !entry.InteractionEnabled && !entry.MicrophoneEnabled)
        return false;

      if (!InstanceFinder.IsOffline && !IsRecognitionRolePlayer(requester, entry.RequiredRoleTag))
        return false;

      if (!microphone
          && RequiresPenlightForRecognition(entry.CompletionSignal)
          && !HasPenlight(requester))
        return false;

      return InstanceFinder.IsOffline
        ? requester == null || IsWithinRecognitionInteractionDistance(requester)
        : requester != null && IsWithinRecognitionInteractionDistance(requester);
    }

    private bool RequiresPenlightForRecognition(string completionSignal) =>
      string.Equals(completionSignal, $"{Identifier}_pupil_checked", StringComparison.Ordinal);

    private static bool HasPenlight(PlayerController player) =>
      player != null && player.CountItemInInventory(Penlight.Identifier) > 0;

    private void ShowRequiredItemDialogue(string firstLine, string secondLine)
    {
      if (_chatUI == null)
        _chatUI = Registry.Get<ChatUIController>(RegistryType.UI, Registry.TypeKey<ChatUIController>());

      var dialogue = Registry.Get<DialoguePanelUIController>(
        RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", $"({firstLine})");
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", $"({secondLine})");
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
