using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 시나리오에서 요구하는 물체/부위 상호작용을 위한 경량 Interactable 입니다.
  /// 상호작용이 확정되면 완료 신호를 올리고, 필요하면 연결된 시각 오브젝트를 표시 또는 숨깁니다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class ScenarioActionInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional, IInteractToggleable, IInteractDisplayIcons, IInteractDisplayPriority, IQuestPresentationTarget
  {
    public static event Action<ScenarioActionInteractable, PlayerController> OnInteractionCompleted;

    [Header("Scenario Action")]
    [SerializeField] private string _displayText = "상호작용";
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private List<Sprite> _displayIcons = new();
    [SerializeField] private string _completionSignal;
    [Tooltip("퀘스트 표시 바인딩에 사용할 소유 엔티티 식별자입니다.")]
    [SerializeField] private string _presentationEntityIdentifier;
    [Tooltip("비워두면 completionSignal을 상호작용 식별자로 사용합니다.")]
    [SerializeField] private string _interactionIdentifier;
    [Tooltip("이 상호작용을 노출하기 전에 RuntimeState에 이미 있어야 하는 시나리오 신호입니다.")]
    [SerializeField] private string[] _requiredRaisedSignals = Array.Empty<string>();
    [Tooltip("이 상호작용을 수행할 수 있는 플레이어 역할 태그입니다. 비우면 역할을 제한하지 않습니다.")]
    [SerializeField] private string _requiredPlayerTag;
    [SerializeField] private bool _enabled = true;
    [SerializeField] private bool _consumeOnce = true;
    [Tooltip("상호작용에 필요한 인벤토리 아이템 식별자입니다. 비우면 아이템을 요구하지 않습니다.")]
    [SerializeField] private string _requiredItemIdentifier;
    [SerializeField, Min(0)] private int _consumeRequiredItemCount;
    [SerializeField] private string _missingItemDialogue;
    [SerializeField] private string _findItemDialogue;

    [Header("Visual State (optional)")]
    [Tooltip("상호작용 성공 시 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject[] _activateOnInteract;
    [Tooltip("상호작용 성공 시 숨길 오브젝트입니다.")]
    [SerializeField] private GameObject[] _deactivateOnInteract;

    private bool _completed;

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public string CompletionSignal => _completionSignal;
    public string PresentationEntityIdentifier => _presentationEntityIdentifier;
    public string InteractionIdentifier => string.IsNullOrWhiteSpace(_interactionIdentifier)
      ? _completionSignal
      : _interactionIdentifier;
    public string RequiredPlayerTag => _requiredPlayerTag;
    public Sprite DisplayIcon => _displayIcon;
    public IReadOnlyList<Sprite> DisplayIcons => _displayIcons;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;
    public int DisplayPriority => PatientACriticalQuestStateFlags.GetInteractionDisplayPriority(
      _presentationEntityIdentifier, InteractionIdentifier);

    public bool CanInteract(Transform interactor)
    {
      if ((_consumeOnce && _completed) || !AreRequiredSignalsRaised())
        return false;

      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return false;
      if (!string.IsNullOrWhiteSpace(_requiredPlayerTag)
          && (string.IsNullOrWhiteSpace(player.UserIdentifier)
              || !MultiplayerInfrastructure.Tag.PlayerTagService.HasTag(
                player.UserIdentifier, _requiredPlayerTag)))
        return false;

      // patient_a_critical 은 노출을 플레이어별 퀘스트 상태 플래그로 판정한다. 그 시나리오에서는
      // 이 컴포넌트에 저장된 _enabled 대신 플래그가 유일한 기준이 된다. 다른 시나리오는 그대로다.
      if (PatientACriticalQuestStateFlags.TryEvaluate(
            _presentationEntityIdentifier, InteractionIdentifier, player, out bool allowedByFlag))
      {
        if (!allowedByFlag)
          return false;

        if (PatientACriticalQuestStateFlags.RequiresActiveQuestBinding(
              _presentationEntityIdentifier, InteractionIdentifier))
        {
          var presentation = MultiplayerInfrastructure.Quest.QuestPresentationService.ActiveInstance;
          return presentation != null
                 && presentation.HasActiveInteractionBinding(
                   _presentationEntityIdentifier, InteractionIdentifier);
        }

        return true;
      }

      return _enabled;
    }

    public void Interact(Transform interactor)
    {
      if (!CanInteract(interactor))
        return;

      var player = interactor.GetComponentInParent<PlayerController>();
      if (!string.IsNullOrWhiteSpace(_requiredItemIdentifier)
          && player.CountItemInInventory(_requiredItemIdentifier)
          < Mathf.Max(1, _consumeRequiredItemCount))
      {
        ShowMissingItemDialogue();
        return;
      }
      if (!string.IsNullOrWhiteSpace(_requiredItemIdentifier)
          && _consumeRequiredItemCount > 0)
      {
        if (_consumeRequiredItemCount == 1)
        {
          if (!player.TryConsumeItemUse(_requiredItemIdentifier, out var receipt))
            return;
          player.CompleteConsumedItemUse(receipt, accepted: true);
        }
        else if (player.RemoveItemFromInventory(
                   _requiredItemIdentifier, _consumeRequiredItemCount)
                 != _consumeRequiredItemCount)
          return;
      }

      SetObjectsActive(_activateOnInteract, true);
      SetObjectsActive(_deactivateOnInteract, false);

      if (!string.IsNullOrWhiteSpace(_completionSignal))
        ScenarioInteractionSignals.Raise(_completionSignal);

      _completed = true;
      OnInteractionCompleted?.Invoke(this, player);
    }

    private void ShowMissingItemDialogue()
    {
      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<MultiplayerInfrastructure.UI.DialoguePanelUIController>(
        MultiplayerInfrastructure.Registry.RegistryType.UI,
        MultiplayerInfrastructure.Registry.Registry.TypeKey<MultiplayerInfrastructure.UI.DialoguePanelUIController>());
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}",
        string.IsNullOrWhiteSpace(_missingItemDialogue) ? "(필요한 물품을 갖고 있지 않다.)" : $"({_missingItemDialogue})");
      dialogue?.TryPresentTransientDialogue("{PLAYER_NAME}",
        string.IsNullOrWhiteSpace(_findItemDialogue) ? "(필요한 물품을 찾자.)" : $"({_findItemDialogue})");
    }

    public void SetEnabled(bool enabled)
    {
      _enabled = enabled;
      if (enabled)
        _completed = false;
    }

    /// <summary>
    /// 시나리오 수동 진입 준비 체인이 단계를 되돌릴 때 "이미 수행함" 표시만 지운다.
    /// <see cref="_consumeOnce"/> 상호작용은 한 번 수행하면 다시 노출되지 않으므로, 같은 세션에서
    /// 이전 단계를 다시 재생하면 그 단계의 상호작용을 수행할 수 없게 된다.
    /// <see cref="SetEnabled"/> 와 달리 노출 기준값(<see cref="_enabled"/>)은 건드리지 않는다.
    /// 노출 판정을 플래그 풀에 맡긴 시나리오에서 이 값을 함께 켜면 단계 밖 상호작용이 열린다.
    /// </summary>
    public void ResetCompletionForScenario()
    {
      _completed = false;
    }

    private bool AreRequiredSignalsRaised()
    {
      if (_requiredRaisedSignals == null)
        return true;

      for (int i = 0; i < _requiredRaisedSignals.Length; i++)
      {
        string signal = _requiredRaisedSignals[i];
        if (!string.IsNullOrWhiteSpace(signal) && !ScenarioInteractionSignals.IsRaised(signal))
          return false;
      }

      return true;
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
      if (targets == null)
        return;

      for (int i = 0; i < targets.Length; i++)
      {
        if (targets[i] != null)
          targets[i].SetActive(active);
      }
    }
  }
}
