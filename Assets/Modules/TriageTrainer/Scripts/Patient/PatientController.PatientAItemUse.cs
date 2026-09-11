using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using TriageTrainer.ItemDefinitions;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// `patient_a_critical` 전용 물품 사용 상호작용. "환자에게 들고 있는 처치 물품 적용"
  /// (<see cref="InteractIdItemApply"/>) 하나가 손에 든 물품을 알아서 골라 적용하던 방식을 대신한다.
  ///
  /// <para>
  /// 통합 상호작용은 지금 무엇을 해야 하는지 문구로 알려주지 못하고, 인벤토리에 다른 물품이 남아
  /// 있으면 서브목표가 요구하지 않은 처치까지 적용해 버린다. 그래서 서브목표가 요구하는 물품마다
  /// "환자에게 {아이템명} 사용" 상호작용을 따로 둔다.
  /// </para>
  ///
  /// <para>
  /// 노출 기준은 퀘스트가 그 서브목표를 안내 대상으로 올렸는지 하나뿐이다
  /// (<see cref="QuestPresentationService.HasActiveInteractionBinding"/>). 물품 보유 여부나 처치
  /// 순서를 노출 기준에 넣으면, 정작 무엇을 해야 하는지 알려 줘야 할 시점에 상호작용이 보이지 않는다.
  /// 그래서 그 조건들은 실행 시점에 확인하고 안내 대사로 되돌려 준다. 판정 근거인 퀘스트 목록이
  /// 피어마다 따로 있으므로 이 게이트도 플레이어별로 동작한다.
  /// </para>
  ///
  /// <para>
  /// 지혈과 기관내관 고정은 각각 별도 사용 경로로 실행한다. 서버 권위 판정과
  /// 아이템 소비, 신호 발신 규약은 달라지지 않는다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    /// <summary>경추 고정기 적용. 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAUseCervicalCollar = "patient_a_use_cervical_collar";

    /// <summary>출혈 부위 거즈 압박. 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAUseGauze = "patient_a_use_gauze";

    /// <summary>거즈를 플라스터로 고정. 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAUsePlasterOnGauze = "patient_a_use_plaster_on_gauze";

    /// <summary>기관내관을 플라스터로 고정. 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAUsePlasterOnIntubation = "patient_a_use_plaster_on_intubation";

    /// <summary>에피네프린 투여(1·2차 공용). 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAUseEpinephrineSyringe = "patient_a_use_epinephrine_5cc_syringe";

    /// <summary>루멘 내 잔여 약물 밀어넣기(1·2차 공용). 퀘스트 표시 바인딩에서 참조한다.</summary>
    public const string InteractIdPatientAUseNormalSalineSyringe = "patient_a_use_normal_saline_20cc_syringe";

    /// <summary>
    /// 물품 사용 상호작용 하나의 정의. <paramref name="RequiredTreatmentIdentifier"/> 는 같은 물품이
    /// 상태에 따라 서로 다른 처치가 되는 경우(플라스터: 거즈 고정 / 기관내관 고정)에만 채운다.
    /// </summary>
    private readonly struct PatientAItemUseSpec
    {
      public readonly string InteractionIdentifier;
      public readonly string ItemIdentifier;
      public readonly string ItemDisplayName;
      public readonly string RequiredTreatmentIdentifier;

      public PatientAItemUseSpec(
        string interactionIdentifier,
        string itemIdentifier,
        string itemDisplayName,
        string requiredTreatmentIdentifier = null)
      {
        InteractionIdentifier = interactionIdentifier;
        ItemIdentifier = itemIdentifier;
        ItemDisplayName = itemDisplayName;
        RequiredTreatmentIdentifier = requiredTreatmentIdentifier;
      }
    }

    private static readonly PatientAItemUseSpec[] PatientAItemUseSpecs =
    {
      new(InteractIdPatientAUseCervicalCollar, CervicalCollar.Identifier, CervicalCollar.DisplayName),
      new(InteractIdPatientAUseGauze, Gauze.Identifier, Gauze.DisplayName),
      new(InteractIdPatientAUsePlasterOnGauze, Plaster.Identifier, Plaster.DisplayName,
        TreatmentPlasterOnGauze),
      new(InteractIdPatientAUsePlasterOnIntubation, Plaster.Identifier, Plaster.DisplayName,
        TreatmentPlasterOnIntubation),
      new(InteractIdPatientAUseEpinephrineSyringe,
        Epinephrine5ccSyringe.Identifier, Epinephrine5ccSyringe.DisplayName),
      new(InteractIdPatientAUseNormalSalineSyringe,
        NormalSaline20ccSyringe.Identifier, NormalSaline20ccSyringe.DisplayName),
    };

    private sealed class PatientAItemUseInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      private readonly PatientAItemUseSpec _spec;

      public PatientAItemUseInteract(PatientController owner, PatientAItemUseSpec spec)
      {
        _owner = owner;
        _spec = spec;
      }

      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => _spec.InteractionIdentifier;
      public string DisplayText => $"환자에게 {_spec.ItemDisplayName} 사용";
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      // 노출(퀘스트 단계)은 레지스트리의 데이터 조건이 정한다. 물품을 아직 구하지 못했어도 노출하고,
      // 실행 조건은 Interact 에서 확인해 안내 대사로 돌려준다.
      public bool CanInteract(Transform interactor)
        => interactor != null && interactor.GetComponentInParent<PlayerController>() != null;

      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player?.PlayerEntity == null)
          return;

        string itemIdentifier = ResolveInventoryItemIdentifier(player);
        string particle = Josa.ObjectParticle(_spec.ItemDisplayName);
        if (itemIdentifier == null)
        {
          PresentHint(
            $"({_spec.ItemDisplayName}{particle} 갖고 있지 않다.)",
            $"({_spec.ItemDisplayName}{particle} 찾자.)");
          return;
        }

        if (!CanApplyNow(itemIdentifier))
        {
          PresentHint($"(지금은 {_spec.ItemDisplayName}{particle} 사용할 수 없다.)");
          return;
        }

        if (_spec.InteractionIdentifier == InteractIdPatientAUsePlasterOnIntubation)
          _owner.ApplyAndConsumeIntubationFixationItemUse(player.PlayerEntity, itemIdentifier);
        else if (_spec.InteractionIdentifier == InteractIdPatientAUseGauze
                 || _spec.InteractionIdentifier == InteractIdPatientAUsePlasterOnGauze)
          _owner.ApplyAndConsumeBleedingControlItemUse(player.PlayerEntity, itemIdentifier);
        else
          _owner.OnItemUsed(player.PlayerEntity, itemIdentifier);
      }

      private string ResolveInventoryItemIdentifier(PlayerController player)
      {
        if (string.Equals(_spec.InteractionIdentifier, InteractIdPatientAUseEpinephrineSyringe,
              System.StringComparison.Ordinal))
          return player.FindFirstInventoryItem(IsEpinephrineSyringeIdentifier);

        return player.CountItemInInventory(_spec.ItemIdentifier) > 0 ? _spec.ItemIdentifier : null;
      }

      /// <summary>
      /// 지금 이 물품을 적용하면 이 상호작용의 문구대로 처치가 이루어지는지 확인한다. 같은 물품이
      /// 상태에 따라 다른 처치가 되는 경우(플라스터)에는 처치 대상까지 일치해야 한다.
      /// </summary>
      private bool CanApplyNow(string itemIdentifier)
      {
        if (!_owner.CanApplyHeldTreatmentItem(itemIdentifier))
          return false;

        return _spec.RequiredTreatmentIdentifier == null
               || string.Equals(
                 _owner.ResolveTreatmentIdentifierForItem(itemIdentifier),
                 _spec.RequiredTreatmentIdentifier,
                 System.StringComparison.Ordinal);
      }

      /// <summary>실행 조건을 만족하지 못한 이유를 상호작용을 시도한 플레이어에게만 알린다.</summary>
      private static void PresentHint(string firstLine, string secondLine = null)
      {
        var dialogue = Registry.Get<DialoguePanelUIController>(
          RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
        if (dialogue == null)
          return;

        dialogue.TryPresentTransientDialogue("{PLAYER_NAME}", firstLine);
        if (!string.IsNullOrEmpty(secondLine))
          dialogue.TryPresentTransientDialogue("{PLAYER_NAME}", secondLine);
      }
    }

    /// <summary>환자 A 물품 사용 상호작용 항목을 등록한다(<c>BuildInteractEntries</c> 에서 호출).</summary>
    private void AddPatientAItemUseInteracts()
    {
      for (int i = 0; i < PatientAItemUseSpecs.Length; i++)
        _interacts.Add(new PatientAItemUseInteract(this, PatientAItemUseSpecs[i]));
    }
  }
}
