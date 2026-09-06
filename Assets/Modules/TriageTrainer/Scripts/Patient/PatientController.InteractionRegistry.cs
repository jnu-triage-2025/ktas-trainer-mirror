using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자의 인터렉션 레지스트리 연결. 코드 리터럴 정의 선언, 데이터 전용 정의의 핸들러 생성(의식 확인),
  /// 조건 절이 읽는 상태 키 노출을 담당한다.
  ///
  /// <para>
  /// 선언은 엔티티 등록 직후(<c>RegisterPatientEntity</c>)에 이루어지며, 스폰 식별자가 바뀌면 이전 식별자의
  /// 정의를 지우고 새 식별자로 다시 선언한다. 시나리오가 관리하는 항목은 <c>initialVisible=false</c> 로 두고,
  /// 시나리오 데이터가 조건이나 트리거로 연다.
  /// </para>
  /// </summary>
  public partial class PatientController : IInteractionDefinitionSource, IInteractionHandlerFactory, IConditionStateProvider
  {
    /// <summary>시나리오 데이터 없이도 항상 보이는(내재 능력 조건만 따르는) 인터렉션.</summary>
    private static readonly HashSet<string> AlwaysVisibleInteractionIdentifiers = new(StringComparer.Ordinal)
    {
      InteractIdCarry,
      InteractIdMonitorSelect,
      InteractIdItemApply,
      "patient_bc_nasal_cannula",
      InteractIdWallSuctionUse,
      InteractIdTriage,
      InteractIdIntravenousLineCannula,
      InteractIdNormalSalineConnect,
      InteractIdPatientANormalSalineConnect,
      InteractIdPatientAPlasmaSolutionConnect,
    };

    private void DeclarePatientInteractions()
    {
      string identifier = Identifier;
      if (string.IsNullOrWhiteSpace(identifier))
        return;
      if (_interacts.Count == 0)
        BuildInteractEntries();
      InteractionRegistry.RemoveCodeDefinitions(identifier);
      InteractionRegistry.DeclareCode(identifier, this);
    }

    public IEnumerable<InteractionDeclaration> DeclareInteractions()
    {
      string identifier = Identifier;
      for (int i = 0; i < _interacts.Count; i++)
      {
        var handler = _interacts[i];
        if (handler is not IQuestPresentationTarget target || string.IsNullOrWhiteSpace(target.InteractionIdentifier))
          continue;

        var definition = InteractionDefinition.Code(
          identifier,
          target.InteractionIdentifier,
          handler.DisplayText,
          initialVisible: AlwaysVisibleInteractionIdentifiers.Contains(target.InteractionIdentifier));
        yield return new InteractionDeclaration(definition, handler);
      }
    }

    public bool TryCreateInteractionHandler(InteractionDefinition definition, out IInteract handler)
      => TryCreateRecognitionCheckHandler(definition, out handler);

    // ── IConditionStateProvider ───────────────────────────────────────────

    private static readonly string[] PatientConditionKeys =
    {
      "patient_identifier",
      "has_bed",
      "player_attached",
      "bed_attached",
      "cardiac_arrest",
      "triage_assessable",
      "triage_assessed",
      "bc_nurse_c_stage",
      "bc_nurse_d_stage",
      "iv_cannula_supported",
      "iv_cannula_interactable",
      "cannula_inserted:left|right",
      "treatment:<TreatmentDisplay>",
      "recognition:<completionSignal>",
    };

    public IEnumerable<string> ConditionKeys => PatientConditionKeys;

    public bool TryGetConditionValue(string key, string qualifier, out ConditionValue value)
    {
      switch (key)
      {
        case "patient_identifier":
          value = ConditionValue.From(Identifier ?? string.Empty);
          return true;
        case "has_bed":
          value = ConditionValue.From(CurrentBed != null);
          return true;
        case "player_attached":
          value = ConditionValue.From(IsPlayerAttached);
          return true;
        case "bed_attached":
          value = ConditionValue.From(IsMovingPatientBedAttached);
          return true;
        case "cardiac_arrest":
          value = ConditionValue.From(MedicalStateIsCardiacArrest);
          return true;
        case "triage_assessable":
          value = ConditionValue.From(EffectiveAssessable);
          return true;
        case "triage_assessed":
          value = ConditionValue.From(AssessedTriage.ToString());
          return true;
        case "bc_nurse_c_stage":
          value = ConditionValue.From(_patientBCNurseCStage.Value.ToString());
          return true;
        case "bc_nurse_d_stage":
          value = ConditionValue.From(_patientBCNurseDStage.Value.ToString());
          return true;
        case "iv_cannula_supported":
          value = ConditionValue.From(IntravenousLineCannulaSupported);
          return true;
        case "iv_cannula_interactable":
          value = ConditionValue.From(CanInteractIntravenousLineCannula);
          return true;
        case "cannula_inserted":
          value = ConditionValue.From(string.Equals(qualifier, "right", StringComparison.OrdinalIgnoreCase)
            ? _cannulaRightArmInserted
            : _cannulaLeftArmInserted);
          return true;
        case "treatment":
          if (Enum.TryParse(qualifier, true, out TreatmentDisplay display))
          {
            value = ConditionValue.From(IsTreatmentDisplayActive(display));
            return true;
          }
          value = default;
          return false;
        case "recognition":
          value = ConditionValue.From(TryGetRecognitionCheck(qualifier ?? string.Empty, out _));
          return true;
        default:
          value = default;
          return false;
      }
    }
  }
}
