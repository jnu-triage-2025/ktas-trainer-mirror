using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Tag;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private const string LiftDisplayText = "환자를 들어올리기";
    private const string CarryDisplayText = "환자 들어올리기";
    private const string MonitorSelectDisplayText = "이 환자를 모니터링";

    private sealed class PatientLiftInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      public PatientLiftInteract(PatientController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdLiftFromBed;
      public string DisplayText
      {
        get
        {
          var name = _owner.GetPatientDisplayName(null);
          return !string.IsNullOrWhiteSpace(name)
            ? $"{name}{Josa.ObjectParticle(name)} 들어올리기"
            : LiftDisplayText;
        }
      }
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      // 노출(시나리오 조건)은 레지스트리가 판정한다. 여기서는 침대에 누워 있는지만 본다.
      public bool CanInteract(Transform interactor)
      {
        return _owner.CurrentBed != null;
      }
      public void Interact(Transform interactor)
      {
        _owner.TryLiftFromBed(interactor);
      }
    }

    private sealed class PatientCarryInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      public PatientCarryInteract(PatientController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdCarry;
      public string DisplayText
      {
        get
        {
          var name = _owner.GetPatientDisplayName(null);
          return !string.IsNullOrWhiteSpace(name)
            ? $"{name}{Josa.ObjectParticle(name)} 들어올리기"
            : CarryDisplayText;
        }
      }
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor)
      {
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
      public string DisplayText => MonitorSelectDisplayText;
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        return player.IsPatientSelectionMode;
      }
      public void Interact(Transform interactor)
      {
        _owner._activeMonitorSelectionRequester?.HandlePatientSelected(_owner, interactor);
        // 환자 선택(클릭) 완료 신호 — click_patient_* / select_patient_* 게이트용.
        _owner.RaisePatientInteractionSignals();
      }
    }

    private sealed class PatientItemApplyInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      public PatientItemApplyInteract(PatientController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdItemApply;
      public string DisplayText => "환자에게 들고 있는 처치 물품 적용";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        // 시나리오별 노출(환자 A 에서는 물품별 상호작용이 대신하므로 숨김)은 레지스트리의 데이터 정의가 정한다.
        // 여기서는 적용할 수 있는 물품을 들고 있는지만 본다.
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string itemIdentifier = _owner.FindApplicableTreatmentInventoryItem(player);
        if (_owner.IsPatientBCNasalCannulaItem(itemIdentifier))
          return false;
        return player != null && itemIdentifier != null;
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        string itemIdentifier = _owner.FindApplicableTreatmentInventoryItem(player);
        if (player?.PlayerEntity != null && _owner.CanApplyHeldTreatmentItem(itemIdentifier))
          _owner.OnItemUsed(player.PlayerEntity, itemIdentifier);
      }
    }

    private sealed class PatientBCNurseDNasalCannulaInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;

      public PatientBCNurseDNasalCannulaInteract(PatientController owner) => _owner = owner;
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => "patient_bc_nasal_cannula";
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

        string itemIdentifier = _owner.GetPatientBCNasalCannulaInventoryItemIdentifier(player);
        if (itemIdentifier == null)
        {
          var dialogue = Registry.Get<DialoguePanelUIController>(
            RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
          dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(비강 캐뉼라를 갖고 있지 않다.)");
          dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}", "(비강 캐뉼라를 찾자.)");
          return;
        }

        _owner.OnItemUsed(player.PlayerEntity, itemIdentifier);
      }
    }

    private sealed class PatientNormalSalineConnectInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      public PatientNormalSalineConnectInteract(PatientController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdNormalSalineConnect;
      public string DisplayText => "생리식염수 연결";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public bool CanInteract(Transform interactor) => _owner.CanConnectPatientBCNormalSaline();
      public void Interact(Transform interactor) => _owner.TryConnectPatientBCNormalSaline(interactor);
    }

    public interface IMonitorSelectionRequester
    {
      public void HandlePatientSelected(PatientController patient, Transform interactor);
    }

    public const string InteractIdLiftFromBed = "lift_from_bed";
    public const string InteractIdCarry = "carry_patient";
    public const string InteractIdMonitorSelect = "monitor_select";

    /// <summary>손에 든 처치 물품(거즈·플라스터 등) 적용 상호작용. 퀘스트 마크 바인딩에서 참조한다.</summary>
    public const string InteractIdItemApply = "item_apply";

    /// <summary>침대 걸이의 생리식염수를 환자 정맥로에 잇는 상호작용. 퀘스트 마크 바인딩에서 참조한다.</summary>
    public const string InteractIdNormalSalineConnect = "normal_saline_connect";

    private readonly List<IInteract> _interacts = new();
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

        // 비활성 네트워크 스폰 프리팹과 에디터가 인스턴스화한 환자는 Awake 전에
        // 조회될 수 있다. 빈 목록을 반환하면 다른 경로가 우연히 항목을 다시 만들기까지
        // 환자가 조용히 쓸 수 없는 상태로 남는다.
        if (_interacts.Count == 0)
          BuildInteractEntries();
        var resolved = new List<IInteract>();
        InteractionRegistry.CollectInteractsForEntity(Identifier, resolved);
        foreach (var interact in _interacts)
        {
          if (!resolved.Contains(interact))
            resolved.Add(interact);
        }
        return resolved.ToArray();
      }
    }

    private void BuildInteractEntries()
    {
      _interacts.Clear();
      _interacts.Add(new PatientLiftInteract(this));
      _interacts.Add(new PatientCarryInteract(this));
      _interacts.Add(new PatientMonitorSelectInteract(this));
      _interacts.Add(new PatientItemApplyInteract(this));
      _interacts.Add(new PatientBCNurseDNasalCannulaInteract(this));
      AddWallSuctionUseInteract();
      AddTriageInteract();
      AddAssessInteracts();
      AddRecognitionCheckInteract();
      AddIntravenousLineCannulaInteract();
      _interacts.Add(new PatientNormalSalineConnectInteract(this));
      AddPatientAFluidConnectInteracts();
      AddPatientAItemUseInteracts();

      // 레지스트리에 이미 선언되어 있으면(식별자 재등록 등) 새 핸들러 인스턴스로 다시 선언한다.
      if (!string.IsNullOrWhiteSpace(_registeredEntityIdentifier))
        DeclarePatientInteractions();
    }

    private bool IsPatientBCNasalCannulaItem(string itemIdentifier) =>
      IsPatientBC
      && (string.Equals(itemIdentifier, "nasalcannula", StringComparison.Ordinal)
          || string.Equals(itemIdentifier, "nasal_cannula", StringComparison.Ordinal)
          || string.Equals(itemIdentifier, "nasal", StringComparison.Ordinal));

    private string GetPatientBCNasalCannulaInventoryItemIdentifier(PlayerController player)
    {
      foreach (string itemIdentifier in new[] { "nasal_cannula", "nasalcannula", "nasal" })
        if (player.CountItemInInventory(itemIdentifier) > 0)
          return itemIdentifier;
      return null;
    }

    // 역할(nurse_d) 제한은 시나리오 데이터의 조건 절이 맡고, 서버 판정은 TryValidatePatientBCTreatmentActor 가 한다.
    private bool CanDisplayPatientBCNurseDNasalCannula(PlayerController player) =>
      IsPatientBC
      && _patientBCNurseDStage.Value == PatientBCTreatmentStage.AwaitingNasalCannula
      && player != null
      && IsWithinPatientBCTreatmentDistance(player);

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
