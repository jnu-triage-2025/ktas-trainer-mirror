using System.Collections;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 튜토리얼의 오답 배송 물품 소품. 실행 중인 메인 시나리오를 중단하지 않고 비점유 다이얼로그만 표시한다.
  ///
  /// <para>
  /// 인터렉션 정의(문구, 안내 대사)는 상시 카탈로그(<c>Resources/Interactions/global.json</c>)가
  /// handlerKey <c>tutorial_decoy</c> 로 선언하고, 이 컴포넌트는 엔티티 식별자(<c>DummyInteractTrainer/...</c>)로
  /// 등록해 핸들러를 만들어 준다. 프리팹·씬에는 인터렉션 데이터를 두지 않는다.
  /// </para>
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class TutorialDecoyInteractable : MonoBehaviour, IInteractable, IInteractionHandlerFactory
  {
    public const string HandlerKey = "tutorial_decoy";
    public const string EntityIdentifierPrefix = "DummyInteractTrainer/";

    [Header("Identity")]
    [Tooltip("레지스트리 주소에 쓰는 엔티티 식별자. 관례상 DummyInteractTrainer/ 로 시작한다.")]
    [SerializeField] private string _entityIdentifier;

    private const float FadeInDuration = 0.2f;
    private const float DisplayDuration = 3f;
    private const float FadeOutDuration = 0.2f;

    private static TutorialDecoyInteractable _activePresenter;
    private Coroutine _dialogueRoutine;
    private readonly List<IInteract> _interactBuffer = new();
    private DecoyInteract _handler;

    public string EntityIdentifier => _entityIdentifier;

    public IInteract[] Interacts
    {
      get
      {
        _interactBuffer.Clear();
        InteractionRegistry.CollectInteractsForEntity(_entityIdentifier, _interactBuffer);
        return _interactBuffer.ToArray();
      }
    }

    private sealed class DecoyInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly TutorialDecoyInteractable _owner;
      private readonly InteractionDefinition _definition;

      public DecoyInteract(TutorialDecoyInteractable owner, InteractionDefinition definition)
      {
        _owner = owner;
        _definition = definition;
      }

      public string PresentationEntityIdentifier => _owner._entityIdentifier;
      public string InteractionIdentifier => _definition.InteractionIdentifier;
      public string DisplayText => string.IsNullOrWhiteSpace(_definition.Display?.Text) ? "확인하기" : _definition.Display.Text;
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
        => interactor != null && interactor.GetComponentInParent<PlayerController>() != null;

      public void Interact(Transform interactor)
      {
        if (!CanInteract(interactor))
          return;
        _owner.PresentDialogue(_definition.GetExtra("dialogue") ?? "이 물건은 아닌 것 같다");
      }
    }

    private void Awake()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier))
        _entityIdentifier = MultiplayerInfrastructure.Registry.EntityId.Ensure(_entityIdentifier, gameObject, "DummyInteractTrainer");
    }

    private void OnEnable()
    {
      Registry.RegisterEntity(_entityIdentifier, EntityType.Prop, gameObject, displayName: gameObject.name);
      InteractionRegistry.Changed += HandleRegistryChanged;
    }

    private void OnDisable()
    {
      InteractionRegistry.Changed -= HandleRegistryChanged;
      if (_activePresenter == this)
      {
        var dialogue = Registry.Get<DialoguePanelUIController>(RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
        dialogue?.HideDisinteractableDialogue();
        StopCurrentDialogue();
      }
      if (Registry.TryGetEntity(_entityIdentifier, out var descriptor) && descriptor?.GameObject == gameObject)
        Registry.UnregisterEntity(_entityIdentifier);
    }

    private void HandleRegistryChanged() { }

    public bool TryCreateInteractionHandler(InteractionDefinition definition, out IInteract handler)
    {
      handler = null;
      if (definition == null || definition.HandlerKey != HandlerKey)
        return false;
      handler = _handler = new DecoyInteract(this, definition);
      return true;
    }

    private void PresentDialogue(string content)
    {
      var dialogue = Registry.Get<DialoguePanelUIController>(RegistryType.UI, Registry.TypeKey<DialoguePanelUIController>());
      if (dialogue == null)
      {
        Debug.LogWarning("[TutorialDecoyInteractable] DialoguePanelUIController를 찾지 못했습니다.", this);
        return;
      }

      if (_activePresenter != null && _activePresenter != this)
        _activePresenter.StopCurrentDialogue();

      StopCurrentDialogue();
      _activePresenter = this;
      _dialogueRoutine = StartCoroutine(DisplayDialogue(dialogue, content));
    }

    private IEnumerator DisplayDialogue(DialoguePanelUIController dialogue, string content)
    {
      dialogue.DisplayDisinteractableDialogue(null, content, null);
      yield return Fade(dialogue, 0f, 1f, FadeInDuration);
      yield return WaitRealtime(DisplayDuration);
      yield return Fade(dialogue, 1f, 0f, FadeOutDuration);
      dialogue.HideDisinteractableDialogue();

      _dialogueRoutine = null;
      if (_activePresenter == this)
        _activePresenter = null;
    }

    private static IEnumerator Fade(DialoguePanelUIController dialogue, float from, float to, float duration)
    {
      if (duration <= 0f)
      {
        dialogue.SetDisinteractableDialogueOpacity(to);
        yield break;
      }

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.unscaledDeltaTime;
        dialogue.SetDisinteractableDialogueOpacity(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
        yield return null;
      }

      dialogue.SetDisinteractableDialogueOpacity(to);
    }

    private static IEnumerator WaitRealtime(float duration)
    {
      float elapsed = 0f;
      while (elapsed < Mathf.Max(0f, duration))
      {
        elapsed += Time.unscaledDeltaTime;
        yield return null;
      }
    }

    private void StopCurrentDialogue()
    {
      if (_dialogueRoutine != null)
      {
        StopCoroutine(_dialogueRoutine);
        _dialogueRoutine = null;
      }

      if (_activePresenter == this)
        _activePresenter = null;
    }
  }
}
