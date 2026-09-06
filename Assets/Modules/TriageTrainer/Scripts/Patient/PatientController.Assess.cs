using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Scenario;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 "사정(Assess)" 인터랙션 부분 구현.
  ///
  /// <para>
  /// 의식상태(AVPU/GCS)·활력징후·맥박 확인처럼 "환자를 클릭해 사정을 수행"하는 동작을, 데이터 기반
  /// 인터랙션으로 노출하고 완료 시 시나리오 게이팅 신호(sig.check_*)를 올린다. 별도 사정 UI 메커닉 없이
  /// 기존 환자 인터랙션 패턴(<see cref="IInteract"/>)을 재사용한다.
  /// </para>
  ///
  /// 사정 동작의 기본 정의(식별자, 문구, 신호 규칙)는 코드 리터럴이고, 시나리오별 문구·신호·요구 물품·대사는
  /// 시나리오 데이터의 interactions 정의가 덮어쓴다. 노출은 인터렉션 레지스트리가 판정한다.
  /// </summary>
  public partial class PatientController
  {
    /// <summary>사정 동작의 런타임 정의. 인스펙터에 직렬화하지 않는다.</summary>
    public sealed class AssessActionConfig
    {
      public string Identifier { get; }
      public string DisplayText { get; }

      public AssessActionConfig(string identifier, string displayText)
      {
        Identifier = identifier;
        DisplayText = displayText;
      }
    }

    private sealed class PatientAssessInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      private readonly string _actionIdentifier;

      public PatientAssessInteract(PatientController owner, string actionIdentifier)
      {
        _owner = owner;
        _actionIdentifier = actionIdentifier;
      }

      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => _actionIdentifier;

      private AssessActionConfig Config => _owner.GetAssessAction(_actionIdentifier);

      /// <summary>레지스트리의 유효 정의(시나리오 데이터 덮개 포함). 등록 전에는 null.</summary>
      private InteractionDefinition Definition
        => InteractionRegistry.TryGetDefinition(this, out var definition) ? definition : null;

      public string DisplayText => Config?.DisplayText ?? "사정";
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        var cfg = Config;
        if (cfg == null || !_owner.CanPerformTriageOrAssessment)
          return false;

        // 시나리오 노출 조건은 레지스트리가 판정한다. 여기서는 사정을 수행할 수 있는 상태인지만 본다.
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return player != null;
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        var definition = Definition;
        if (definition?.RequiredItems != null && player != null)
        {
          for (int i = 0; i < definition.RequiredItems.Count; i++)
          {
            var requirement = definition.RequiredItems[i];
            if (requirement == null || !requirement.IsValid
                || player.CountItemInInventory(requirement.ItemIdentifier) >= requirement.Count)
              continue;
            _owner.ShowRequiredItemDialogue(
              definition.GetExtra("missingItemDialogue") ?? "필요한 사정 도구를 갖고 있지 않다.",
              definition.GetExtra("findItemDialogue") ?? "필요한 사정 도구를 찾자.");
            return;
          }
        }
        _owner.PerformAssess(_actionIdentifier, player, definition);
      }
    }

    private readonly Dictionary<string, AssessActionConfig> _assessActionMap = new(StringComparer.Ordinal);

    private void RebuildAssessActionMap()
    {
      _assessActionMap.Clear();
      for (int i = 0; i < DefaultAssessActions.Length; i++)
      {
        var definition = DefaultAssessActions[i];
        _assessActionMap[definition.Id] = new AssessActionConfig(definition.Id, definition.DisplayText);
      }
    }

    private AssessActionConfig GetAssessAction(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      return _assessActionMap.TryGetValue(identifier, out var cfg) ? cfg : null;
    }

    /// <summary>
    /// 표준 사정 동작의 코드 리터럴(식별자 → 기본 표시문구). 모든 환자가 같은 목록을 갖고, 시나리오가
    /// 필요한 항목만 조건으로 연다. 시나리오별 문구는 interactions 정의의 display.text 가 덮어쓴다.
    /// </summary>
    private static readonly (string Id, string DisplayText)[] DefaultAssessActions =
    {
      ("assess_avpu_gcs", "의식상태 사정(AVPU/GCS)"),
      ("assess_pulse", "맥박 확인"),
      ("assess_pulse_r1", "맥박 확인"),
      ("assess_pulse_r2", "맥박 확인"),
      ("assess_gcs", "GCS 재사정"),
      ("assess_gcs_rosc", "의식상태 재사정"),
      ("assess_vital", "활력징후 사정"),
    };

    /// <summary>표준 사정 동작들을 IInteract 엔트리로 추가한다(BuildInteractEntries 에서 호출).</summary>
    private void AddAssessInteracts()
    {
      RebuildAssessActionMap();
      for (int i = 0; i < DefaultAssessActions.Length; i++)
        _interacts.Add(new PatientAssessInteract(this, DefaultAssessActions[i].Id));
    }

    private void PerformAssess(string actionIdentifier, PlayerController player, InteractionDefinition definition)
    {
      var cfg = GetAssessAction(actionIdentifier);
      if (cfg == null || !CanPerformTriageOrAssessment)
        return;

      // 시나리오 데이터의 completionSignal 우선, 없으면 식별자 규칙 기반 코드 기본값으로 폴백.
      string signal = !string.IsNullOrWhiteSpace(definition?.CompletionSignal)
          ? definition.CompletionSignal
          : ResolveDefaultAssessSignal(actionIdentifier);

      // 성공 독백을 먼저 표시한다. 신호가 퀘스트/UI를 갱신하면서 상호작용 표시를
      // 재구성할 수 있으므로, 신호를 먼저 올리면 독백이 유실될 수 있다.
      PresentAssessDialogue(definition, () => RaiseAssessSignal(signal));
    }

    private static void RaiseAssessSignal(string signal)
    {
      if (!string.IsNullOrWhiteSpace(signal))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(signal);
    }

    private static void PresentAssessDialogue(InteractionDefinition definition, Action onFinished)
    {
      string actionDialogue = definition?.GetExtra("actionDialogue");
      string resultDialogue = definition?.GetExtra("resultDialogue");
      if (string.IsNullOrWhiteSpace(actionDialogue) && string.IsNullOrWhiteSpace(resultDialogue))
      {
        onFinished?.Invoke();
        return;
      }

      var dialogue = Registry.Get<DialoguePanelUIController>(
        RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
      if (!string.IsNullOrWhiteSpace(actionDialogue))
        dialogue?.TryPresentTransientDialogue(DialoguePanelUIController.PlayerNamePlaceholder, actionDialogue);
      if (!string.IsNullOrWhiteSpace(resultDialogue))
        dialogue?.TryPresentTransientDialogue(string.Empty, resultDialogue, onFinished: onFinished);
      else
        onFinished?.Invoke();
    }

    // ── 사정 기본 신호(코드 하드코딩): 사정 동작 식별자 → 신호 템플릿 ──
    // AssessSignal 이 비어 있을 때 적용된다. {id} 는 환자 Identifier 로 치환(ResolveSignalTemplate, TreatmentDisplay.cs).
    private static readonly Dictionary<string, string> DefaultAssessSignals = new(StringComparer.Ordinal)
    {
      { "assess_avpu_gcs", "check_avpu_gcs_{id}" },
      { "assess_pulse", "check_pulse_{id}" },
      { "assess_pulse_r1", "check_pulse_{id}_r1" },
      { "assess_pulse_r2", "check_pulse_{id}_r2" },
      { "assess_gcs", "check_gcs_{id}" },
      { "assess_gcs_rosc", "check_gcs_a_rosc" },
      { "assess_vital", "check_vital_{id}" },
    };

    private string ResolveDefaultAssessSignal(string actionIdentifier)
    {
      if (string.IsNullOrWhiteSpace(actionIdentifier))
        return null;

      return DefaultAssessSignals.TryGetValue(actionIdentifier, out var template)
          ? ResolveSignalTemplate(template)
          : null;
    }
  }
}
