using System;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 "정맥라인 캐뉼라(18G~20G)" 상호작용 지원/가능 여부 및 상호작용 항목 부분 구현.
  ///
  /// <para>
  /// 이 환자에게 정맥라인 캐뉼라 상호작용이 <b>지원되는지(Preset/Config)</b>와, 현재 시점에
  /// <b>실제로 가능한지(State)</b>를 구분해서 관리한다. 다른 인터랙션 계열
  /// (<see cref="InteractConfig"/>, <see cref="TriageAssessmentConfig"/>)과
  /// 동일한 관례를 따른다: Config(정적/시나리오 제어) + 런타임 조건을 결합해 상호작용 가능 여부를 판정한다.
  /// </para>
  ///
  /// <para>
  /// 상호작용 항목(<see cref="PatientIntravenousLineCannulaInteract"/>)은 <c>BuildInteractEntries</c> 에서
  /// <c>_interacts</c> 에 등록되며, 플레이어가 <b>캐뉼라(18G/20G)를 손에 든 경우에만</b> 힌트로 노출된다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    /// <summary>정맥라인 캐뉼라 상호작용으로 인정하는 손에 든 아이템 식별자(18G/20G).</summary>
    private static readonly string[] IntravenousLineCannulaItemIdentifiers =
    {
      TriageTrainer.ItemDefinitions.Cannula18g.Identifier,
      TriageTrainer.ItemDefinitions.Cannula20g.Identifier,
    };

    [Serializable]
    public struct IntravenousLineCannulaConfig
    {
      [Tooltip("이 환자(모델)가 18G~20G 캐뉼라를 이용한 정맥라인 확보 상호작용 기능을 지원하는지 여부. " +
               "환자 유형별로 다르게 설정 가능하며(팔 모델 유무 등), 상호작용 노출 게이팅의 기준 데이터로 사용된다.")]
      [SerializeField] private bool _supported;

      [Tooltip("상호작용 힌트에 표시할 문구.")]
      [SerializeField] private string _displayText;

      public bool Supported
      {
        get => _supported;
        set => _supported = value;
      }

      public string DisplayText => string.IsNullOrWhiteSpace(_displayText) ? "정맥 라인 확보" : _displayText;
    }

    [Header("Intravenous Line Cannula (정맥라인 캐뉼라)")]
    [SerializeField] private IntravenousLineCannulaConfig _intravenousLineCannulaConfig;

    /// <summary>
    /// State: 현재 시점에 이 환자에게 정맥라인 캐뉼라 상호작용을 수행할 수 있는지 여부(런타임).
    /// 예) 이미 양팔에 캐뉼라가 삽입되어 더 이상 삽입할 필요가 없는 경우 false로 전환하는 등 시나리오 진행에 따라 갱신된다.
    /// </summary>
    [SerializeField] private bool _intravenousLineCannulaInteractable = true;

    /// <summary>
    /// 정맥라인 캐뉼라 삽입 좌/우 팔 상태. 처치 표시 프리셋이 한쪽 팔만 지원하면 그 팔을 우선하고,
    /// 양쪽 모두 지원하면 좌측부터 결정론적으로 배정한다. 좌·우가 모두 채워지면 더 이상 삽입하지 않는다.
    /// </summary>
    private bool _cannulaLeftArmInserted;
    private bool _cannulaRightArmInserted;

    /// <summary>
    /// Preset/Config: 정맥라인 캐뉼라(18G~20G) 상호작용 기능 지원 여부.
    /// </summary>
    public bool IntravenousLineCannulaSupported
    {
      get => _intravenousLineCannulaConfig.Supported;
      set => _intravenousLineCannulaConfig.Supported = value;
    }

    /// <summary>
    /// State: 정맥라인 캐뉼라(18G~20G) 상호작용 가능 여부(런타임).
    /// </summary>
    public bool IntravenousLineCannulaInteractable
    {
      get => _intravenousLineCannulaInteractable;
      set => _intravenousLineCannulaInteractable = value;
    }

    /// <summary>
    /// 정맥라인 캐뉼라 상호작용이 실제로 가능한지(Config가 지원하고, 현재 State도 가능한 경우).
    /// 환자 B/C는 동공반사 확인을 끝낸 정맥로 확보 단계(<see cref="PatientBCTreatmentStage.AwaitingIv"/>)에서만 노출한다.
    /// </summary>
    public bool CanInteractIntravenousLineCannula =>
      IntravenousLineCannulaSupported
      && IntravenousLineCannulaInteractable
      && (!IsPatientBC
          || _patientBCNurseCStage.Value == PatientBCTreatmentStage.AwaitingIv);

    /// <summary>시나리오 진행에 따라 정맥라인 캐뉼라 상호작용 가능 여부(State)를 켜고 끈다.</summary>
    public void SetIntravenousLineCannulaInteractable(bool interactable)
    {
      _intravenousLineCannulaInteractable = interactable;
    }

    /// <summary>
    /// 정맥라인 캐뉼라 상호작용 항목. 대상 퀘스트가 활성화된 환자에게만 노출되며,
    /// 캐뉼라 보유 여부는 상호작용 실행 시 안내/완료를 판정한다.
    /// </summary>
    private sealed class PatientIntravenousLineCannulaInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;

      public PatientIntravenousLineCannulaInteract(PatientController owner) { _owner = owner; }

      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdIntravenousLineCannula;

      public string DisplayText => _owner._intravenousLineCannulaConfig.DisplayText;
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        // (1) 환자가 이 기능을 지원(Config)하고 현재 가능(State)한지.
        if (!_owner.CanInteractIntravenousLineCannula)
          return false;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        // 캐뉼라가 없어도 항목은 노출한다. Interact에서 필요한 물품 안내를 표시한다.
        return _owner.CanInteractIntravenousLineCannula;
      }

      public void Interact(Transform interactor)
      {
        _owner.PerformIntravenousLineCannulaInsertion(interactor);
      }
    }

    /// <summary>정맥라인 캐뉼라 상호작용 식별자. 퀘스트 표시 바인딩(퀘스트 마크)에서 참조한다.</summary>
    public const string InteractIdIntravenousLineCannula = "intravenous_line_cannula";

    /// <summary>정맥라인 캐뉼라 상호작용 항목을 등록한다(<c>BuildInteractEntries</c> 에서 호출).</summary>
    private void AddIntravenousLineCannulaInteract()
    {
      _interacts.Add(new PatientIntravenousLineCannulaInteract(this));
    }

    /// <summary>플레이어가 손에 캐뉼라(18G/20G)를 들고 있는지 판정한다.</summary>
    private bool IsHandlingIntravenousLineCannula(PlayerController player)
    {
      if (player == null)
        return false;

      string heldIdentifier = player.HandlingItem?.CurrentIdentifier;
      if (string.IsNullOrWhiteSpace(heldIdentifier))
        return false;

      return CanPerformPatientBCIv() && IsCannulaGaugeAllowed(heldIdentifier);
    }

    private bool IsCannulaGaugeAllowed(string itemIdentifier)
    {
      // patient_b_c_ct requires a 20G line. Scope this restriction to its two
      // runtime patient identifiers so existing 18G-capable scenarios keep working.
      if ((string.Equals(Identifier, "patient_b", StringComparison.Ordinal)
           || string.Equals(Identifier, "patient_c", StringComparison.Ordinal))
          && !string.Equals(itemIdentifier, TriageTrainer.ItemDefinitions.Cannula20g.Identifier, StringComparison.Ordinal))
        return false;

      for (int i = 0; i < IntravenousLineCannulaItemIdentifiers.Length; i++)
      {
        if (string.Equals(itemIdentifier, IntravenousLineCannulaItemIdentifiers[i], StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    /// <summary>
    /// 정맥라인 캐뉼라 삽입(상호작용 확정)을 처리한다.
    /// 좌/우 팔을 결정론적으로 배정하고, 게이지(18G/20G)에 맞는 처치 표현을 켜고,
    /// 좌/우별 시나리오 게이팅 신호를 올린다. 힌트를 즉시 갱신한다.
    /// </summary>
    private void PerformIntravenousLineCannulaInsertion(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      if (!IsHandlingIntravenousLineCannula(player))
      {
        var dialogue = Registry.Get<DialoguePanelUIController>(
          RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
        if (dialogue == null)
          dialogue = UnityEngine.Object.FindFirstObjectByType<DialoguePanelUIController>(
            FindObjectsInactive.Exclude);
        dialogue?.DisplayDialogue("{PLAYER_NAME}", "20G 캐뉼라가 필요하다.", null, interactionRequired: true);
        return;
      }

      // 상호작용 시점의 조건을 서버/클라이언트 공통으로 다시 방어한다.
      if (!CanInteractIntravenousLineCannula || !IsHandlingIntravenousLineCannula(player))
        return;

      // ── 게이지 판정(18G / 20G) ── 손에 든 아이템 식별자로 구분한다.
      string heldIdentifier = player.HandlingItem?.CurrentIdentifier;
      bool is18G = string.Equals(heldIdentifier, TriageTrainer.ItemDefinitions.Cannula18g.Identifier, StringComparison.Ordinal);
      var leftDisplay = is18G
        ? TreatmentDisplay.Syringe18GInsertedIntoLeftArm
        : TreatmentDisplay.Syringe20GInsertedIntoLeftArm;
      var rightDisplay = is18G
        ? TreatmentDisplay.Syringe18GInsertedIntoRightArm
        : TreatmentDisplay.Syringe20GInsertedIntoRightArm;

      // ── 좌/우 팔 배정 ── 환자 모델이 한쪽 표현만 지원하면 그 팔을 우선한다.
      bool supportsLeft = IsTreatmentDisplaySupported(leftDisplay);
      bool supportsRight = IsTreatmentDisplaySupported(rightDisplay);
      bool isLeft;
      if (supportsRight && !supportsLeft && !_cannulaRightArmInserted)
        isLeft = false;
      else if (supportsLeft && !supportsRight && !_cannulaLeftArmInserted)
        isLeft = true;
      else if (!_cannulaLeftArmInserted)
        isLeft = true;
      else if (!_cannulaRightArmInserted)
        isLeft = false;
      else
        return; // 양팔 모두 삽입 완료 → 추가 삽입 없음

      string side = isLeft ? "left" : "right";
      if (IsPatientBC && IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdCompletePatientBCIv(side, heldIdentifier);
        return;
      }

      if (IsPatientBC && !TryValidatePatientBCTreatmentActor(player, NurseCRoleTag))
        return;

      // Advance the authoritative stage before consuming the item. A stale
      // interaction must never destroy a cannula without completing the IV step.
      if (IsPatientBC && !TryAdvancePatientBCIvStageAuthoritative())
        return;

      // 캐뉼라는 팔 하나당 하나씩 소비한다. 첫 삽입 뒤에는 플레이어가 두 번째
      // 캐뉼라를 다시 획득해야 하므로, 두 팔 처치에 18G 2개를 사전 보유할 필요가 없다.
      if (player.RemoveItemFromInventory(heldIdentifier, 1) != 1)
      {
        if (IsPatientBC)
          RevertPatientBCIvStageAuthoritative();
        return;
      }

      // ── 처치 표현(게이지 + 좌/우) ──
      TreatmentDisplay display = isLeft ? leftDisplay : rightDisplay;
      ShowTreatmentDisplay(display);

      // 배정 상태 기록(다음 삽입은 반대 팔로).
      if (isLeft) _cannulaLeftArmInserted = true;
      else _cannulaRightArmInserted = true;

      CompletePatientBCIvOrRaiseExistingSignal(side, heldIdentifier);

      // 양팔 모두 채워지면 더 이상 상호작용을 노출하지 않는다.
      if (_cannulaLeftArmInserted && _cannulaRightArmInserted)
        _intravenousLineCannulaInteractable = false;

      player.RefreshInteractableHintsNow();
    }

    /// <summary>캐뉼라 신호 템플릿을 환자 Identifier 로 치환해 발신한다(빈 값이면 생략).</summary>
    private void RaiseCannulaSignal(string template)
    {
      string signal = ResolveSignalTemplate(template);
      if (!string.IsNullOrWhiteSpace(signal))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(signal);
    }

    private void CompletePatientBCIvOrRaiseExistingSignal(string side, string itemIdentifier)
    {
      if (!IsPatientBC)
      {
        RaiseCannulaSignal($"insert_iv_{{id}}_{side}");
        return;
      }

      if (IsFishNetServerStarted || FishNet.InstanceFinder.IsOffline)
      {
        RaiseCannulaSignal($"insert_iv_{{id}}_{side}");
      }
      else if (IsFishNetClientInitialized)
      {
        CmdCompletePatientBCIv(side, itemIdentifier);
      }
    }

    private bool TryAdvancePatientBCIvStageAuthoritative()
    {
      if (!TryAdvancePatientBCNurseCStage(PatientBCTreatmentStage.AwaitingIv,
            PatientBCTreatmentStage.AwaitingNormalSaline))
        return false;

      TryCreditPendingPatientBCNormalSalineConnection();
      return true;
    }

    [FishNet.Object.ServerRpc(RequireOwnership = false)]
    private void CmdCompletePatientBCIv(string side, string itemIdentifier, FishNet.Connection.NetworkConnection sender = null)
    {
      if (!IsPatientBC
          || (side != "left" && side != "right")
          || !string.Equals(itemIdentifier, TriageTrainer.ItemDefinitions.Cannula20g.Identifier,
            StringComparison.Ordinal)
           || !TryValidatePatientBCTreatmentActor(sender, NurseCRoleTag, out var player, out var actorIdentifier,
             out var actorDisplayName)
           || !CanPerformPatientBCIv()
           || player.CountItemInInventory(itemIdentifier) < 1
           || !TryAdvancePatientBCIvStageAuthoritative())
        return;

      if (player.RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        RevertPatientBCIvStageAuthoritative();
        return;
      }

      using (ScenarioSignalPlayerContext.Push(actorIdentifier, actorDisplayName))
      {
        SetTreatmentDisplayNetworked(side == "left"
          ? TreatmentDisplay.Syringe20GInsertedIntoLeftArm
          : TreatmentDisplay.Syringe20GInsertedIntoRightArm, true);
        RaiseCannulaSignal($"insert_iv_{{id}}_{side}");
      }
    }
  }
}
