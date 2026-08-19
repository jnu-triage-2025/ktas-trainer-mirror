using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Tag;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    [Serializable]
    public class InteractConfig
    {
      [SerializeField] private string _identifier;
      [SerializeField] private bool _enabled = true;

      public InteractConfig(string identifier, bool enabled = true)
      {
        _identifier = identifier;
        _enabled = enabled;
      }

      public string Identifier => _identifier;
      public bool Enabled
      {
        get => _enabled;
        set => _enabled = value;
      }
    }

    private sealed class PatientLiftInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientLiftInteract(PatientController owner) { _owner = owner; }
      public string DisplayText
      {
        get
        {
          var name = _owner.GetPatientDisplayName(null);
          return !string.IsNullOrWhiteSpace(name)
            ? $"{name}{Josa.ObjectParticle(name)} 들어올리기"
            : _owner._liftDisplayText;
        }
      }
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor)
      {
        return _owner.IsInteractEnabled(InteractIdLiftFromBed) && _owner.CurrentBed != null;
      }
      public void Interact(Transform interactor)
      {
        _owner.TryLiftFromBed(interactor);
      }
    }

    private sealed class PatientCarryInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientCarryInteract(PatientController owner) { _owner = owner; }
      public string DisplayText
      {
        get
        {
          var name = _owner.GetPatientDisplayName(null);
          return !string.IsNullOrWhiteSpace(name)
            ? $"{name}{Josa.ObjectParticle(name)} 들어올리기"
            : _owner._carryDisplayText;
        }
      }
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor)
      {
        if (!_owner.IsInteractEnabled(InteractIdCarry))
          return false;

        if (_owner.CurrentBed != null)
          return false;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        return !player.IsCarryingReposable;
      }
      public void Interact(Transform interactor)
      {
        _owner.TryCarryByInteractor(interactor);
      }
    }

    private sealed class PatientMonitorSelectInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      public PatientMonitorSelectInteract(PatientController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdMonitorSelect;
      public string DisplayText => _owner._monitorSelectDisplayText;
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        return _owner.IsInteractEnabled(InteractIdMonitorSelect)
               && player.IsPatientSelectionMode;
      }
      public void Interact(Transform interactor)
      {
        _owner._activeMonitorSelectionRequester?.HandlePatientSelected(_owner, interactor);
        // 환자 선택(클릭) 완료 신호 — click_patient_* / select_patient_* 게이트용.
        _owner.RaisePatientInteractionSignals();
      }
    }

    private sealed class PatientItemApplyInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientItemApplyInteract(PatientController owner) { _owner = owner; }
      public string DisplayText => "환자에게 들고 있는 처치 물품 적용";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string itemIdentifier = player?.HandlingItem?.CurrentIdentifier;
        if (_owner.IsPatientBCNasalCannulaItem(itemIdentifier))
          return false;
        return player != null
               && player.CountItemInInventory(itemIdentifier) > 0
               && _owner.CanApplyHeldTreatmentItem(itemIdentifier);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string itemIdentifier = player?.HandlingItem?.CurrentIdentifier;
        if (player?.PlayerEntity != null && _owner.CanApplyHeldTreatmentItem(itemIdentifier))
          _owner.OnItemUsed(player.PlayerEntity, itemIdentifier);
      }
    }

    private sealed class PatientBCNurseDNasalCannulaInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;

      public PatientBCNurseDNasalCannulaInteract(PatientController owner) => _owner = owner;
      public string DisplayText => "비강 캐뉼라 적용";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return _owner.CanDisplayPatientBCNurseDNasalCannula(player);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (!_owner.CanDisplayPatientBCNurseDNasalCannula(player))
          return;

        const string itemIdentifier = "nasal_cannula";
        if (player.CountItemInInventory(itemIdentifier) < 1)
        {
          var dialogue = Registry.Get<DialoguePanelUIController>(
            RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
          dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(비강 캐뉼라를 갖고 있지 않다.)");
          return;
        }

        _owner.OnItemUsed(player.PlayerEntity, itemIdentifier);
      }
    }

    private sealed class PatientNormalSalineConnectInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      public PatientNormalSalineConnectInteract(PatientController owner) { _owner = owner; }
      public string DisplayText => "생리식염수 연결";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor) => _owner.CanConnectPatientBCNormalSaline();
      public void Interact(Transform interactor) => _owner.TryConnectPatientBCNormalSaline(interactor);
    }

    public interface IMonitorSelectionRequester
    {
      void HandlePatientSelected(PatientController patient, Transform interactor);
    }

    public const string InteractIdLiftFromBed = "lift_from_bed";
    public const string InteractIdCarry = "carry_patient";
    public const string InteractIdMonitorSelect = "monitor_select";

    [Header("Interact")]
    [SerializeField] private List<InteractConfig> _interactConfigs = new();

    private readonly List<IInteract> _interacts = new();
    private readonly Dictionary<string, InteractConfig> _interactConfigMap = new(StringComparer.Ordinal);
    private IMonitorSelectionRequester _activeMonitorSelectionRequester;

    public IInteract[] Interacts
    {
      get
      {
        // 침대 조종 중에는 환자에게 붙은 모든 사정/처치/이송 인터랙션을 숨긴다.
        // 조종자가 모두 내리면 원래 목록을 그대로 반환하므로 별도 상태 복원 없이
        // 시나리오에서 설정한 활성/비활성 상태까지 보존된다.
        if (CurrentBed != null && CurrentBed.HasParticipants)
          return Array.Empty<IInteract>();

        // Inactive network-spawn prefabs and editor-instantiated patients can be queried
        // before Awake. Returning an empty list silently makes the patient unusable until
        // another path happens to rebuild the entries.
        if (_interacts.Count == 0)
          BuildInteractEntries();
        return _interacts.ToArray();
      }
    }

    private void BuildInteractEntries()
    {
      EnsureDefaultInteractConfigs();
      RebuildInteractConfigMap();

      _interacts.Clear();
      _interacts.Add(new PatientLiftInteract(this));
      _interacts.Add(new PatientCarryInteract(this));
      _interacts.Add(new PatientMonitorSelectInteract(this));
      _interacts.Add(new PatientItemApplyInteract(this));
      _interacts.Add(new PatientBCNurseDNasalCannulaInteract(this));
      AddTriageInteract();
      AddAssessInteracts();
      AddRecognitionCheckInteract();
      AddIntravenousLineCannulaInteract();
      _interacts.Add(new PatientNormalSalineConnectInteract(this));
    }

    private bool IsPatientBCNasalCannulaItem(string itemIdentifier) =>
      IsPatientBC
      && (string.Equals(itemIdentifier, "nasalcannula", StringComparison.Ordinal)
          || string.Equals(itemIdentifier, "nasal_cannula", StringComparison.Ordinal)
          || string.Equals(itemIdentifier, "nasal", StringComparison.Ordinal));

    private bool CanDisplayPatientBCNurseDNasalCannula(PlayerController player) =>
      IsPatientBC
      && _patientBCNurseDStage.Value == PatientBCTreatmentStage.AwaitingNasalCannula
      && player != null
      && !string.IsNullOrWhiteSpace(player.UserIdentifier)
      && PlayerTagService.HasTag(player.UserIdentifier, "nurse_d")
      && IsWithinPatientBCTreatmentDistance(player);

    private void RebuildInteractConfigMap()
    {
      _interactConfigMap.Clear();
      for (int i = 0; i < _interactConfigs.Count; i++)
      {
        var each = _interactConfigs[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _interactConfigMap[each.Identifier] = each;
      }
    }

    private void EnsureDefaultInteractConfigs()
    {
      EnsureInteractConfig(InteractIdLiftFromBed, true);
      EnsureInteractConfig(InteractIdCarry, true);
      // monitor_select는 항상 true로 유지합니다.
      // 표시/비표시는 PlayerController.IsPatientSelectionMode에서만 제어합니다.
      EnsureInteractConfig(InteractIdMonitorSelect, true);
    }

    private void EnsureInteractConfig(string identifier, bool enabled)
    {
      for (int i = 0; i < _interactConfigs.Count; i++)
      {
        var each = _interactConfigs[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        return;
      }

      _interactConfigs.Add(new InteractConfig(identifier, enabled));
    }

    public void SetInteractEnabled(string identifier, bool enabled)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      if (string.Equals(identifier, InteractIdMonitorSelect, StringComparison.Ordinal))
      {
        // monitor_select는 항상 true를 유지하고, 노출 제어는 플레이어 선택 모드 플래그로만 처리합니다.
        enabled = true;
      }

      for (int i = 0; i < _interactConfigs.Count; i++)
      {
        var each = _interactConfigs[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        each.Enabled = enabled;
        RebuildInteractConfigMap();
        return;
      }

      EnsureInteractConfig(identifier, enabled);
      RebuildInteractConfigMap();
    }

    public void AddInteract(string identifier, bool enabled = true)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      if (string.Equals(identifier, InteractIdMonitorSelect, StringComparison.Ordinal))
      {
        // monitor_select는 항상 true를 유지하고, 노출 제어는 플레이어 선택 모드 플래그로만 처리합니다.
        enabled = true;
      }

      EnsureInteractConfig(identifier, enabled);
      RebuildInteractConfigMap();
    }

    public void RemoveInteract(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      for (int i = _interactConfigs.Count - 1; i >= 0; i--)
      {
        var each = _interactConfigs[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        _interactConfigs.RemoveAt(i);
      }

      RebuildInteractConfigMap();
    }

    public bool IsInteractEnabled(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      if (string.Equals(identifier, InteractIdMonitorSelect, StringComparison.Ordinal))
      {
        // monitor_select는 항상 true로 간주됩니다.
        // 실질적인 활성/비활성은 PlayerController.IsPatientSelectionMode가 담당합니다.
        return true;
      }

      if (_interactConfigMap.TryGetValue(identifier, out var cfg))
        return cfg.Enabled;

      return false;
    }

    public void SetMonitorSelectionRequester(IMonitorSelectionRequester requester)
    {
      _activeMonitorSelectionRequester = requester;
    }

    public void ClearMonitorSelectionRequester(IMonitorSelectionRequester requester)
    {
      if (requester != null && !ReferenceEquals(_activeMonitorSelectionRequester, requester))
        return;

      _activeMonitorSelectionRequester = null;
    }

    private void TryLiftFromBed(Transform interactor)
    {
      if (CurrentBed == null)
        return;

      if (interactor == null)
        return;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player == null)
        return;

      if (player.IsCarryingReposable)
      {
        ShowThrottledMessage(interactor, "이미 다른 대상을 들고 있어 환자를 들어올릴 수 없습니다.");
        return;
      }

      if (!CurrentBed.TryLiftTarget(player, out _))
      {
        ShowThrottledMessage(interactor, "환자를 침대에서 들어올릴 수 없습니다.");
        return;
      }

      ShowThrottledMessage(interactor, "환자를 침대에서 들어올렸습니다.");
      RaisePatientInteractionSignals();
      player.RefreshInteractableHintsNow();
    }

    /// <summary>
    /// 환자 대상 인터랙션(클릭/들어올리기/이송) 완료 시 시나리오 게이팅용 완료 신호(sig.*)를 올린다.
    /// 환자 Identifier 를 그대로 신호로 사용하므로(sig.&lt;id&gt; / sig.click_&lt;id&gt; / sig.select_&lt;id&gt;),
    /// 환자 Identifier 를 시나리오 조건명 기반(예: patient_a, patient_b, patient_c)으로 지정하면
    /// click_patient_a / select_patient_b 등의 게이트가 별도 코드 없이 통과된다.
    /// </summary>
    private void RaisePatientInteractionSignals()
    {
      string id = Identifier;
      if (string.IsNullOrWhiteSpace(id))
      {
        return;
      }

      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(id);
      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise("click_" + id);
      MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise("select_" + id);
    }

    private void TryCarryByInteractor(Transform interactor)
    {
      if (CurrentBed != null)
        return;

      if (interactor == null)
        return;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (player == null)
        return;

      if (player.IsCarryingReposable)
      {
        ShowThrottledMessage(interactor, "이미 다른 대상을 들고 있어 환자를 들어올릴 수 없습니다.");
        return;
      }

      if (!player.TryPickUpReposable(this))
      {
        ShowThrottledMessage(interactor, "환자를 들어올릴 수 없습니다.");
        return;
      }

      ShowThrottledMessage(interactor, "환자를 들어올렸습니다.");
      RaisePatientInteractionSignals();
      player.RefreshInteractableHintsNow();
    }
  }
}
