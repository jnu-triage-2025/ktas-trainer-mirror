using System.Collections;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 튜토리얼의 오답 배송 물품에 사용하는 로컬 안내 상호작용.
  /// 실행 중인 메인 시나리오를 중단하지 않고 비점유 다이얼로그만 표시한다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class TutorialDecoyInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional
  {
    [SerializeField] private string _displayText = "확인하기";
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private Color _displayColor = Color.white;
    [SerializeField] private string _dialogueContent = "이 물건은 아닌 것 같다";
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _displayDuration = 3f;
    [SerializeField] private float _fadeOutDuration = 0.2f;

    private static TutorialDecoyInteractable _activePresenter;
    private Coroutine _dialogueRoutine;

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => _displayColor;

    public bool CanInteract(Transform interactor)
      => interactor != null && interactor.GetComponentInParent<PlayerController>() != null;

    public void Interact(Transform interactor)
    {
      if (!CanInteract(interactor))
        return;

      var dialogue = Registry.Get<DialoguePanelUIController>(
        RegistryType.UI,
        Registry.TypeKey<DialoguePanelUIController>());
      if (dialogue == null)
      {
        Debug.LogWarning("[TutorialDecoyInteractable] DialoguePanelUIController를 찾지 못했습니다.", this);
        return;
      }

      if (_activePresenter != null && _activePresenter != this)
        _activePresenter.StopCurrentDialogue();

      StopCurrentDialogue();
      _activePresenter = this;
      _dialogueRoutine = StartCoroutine(DisplayDialogue(dialogue));
    }

    private IEnumerator DisplayDialogue(DialoguePanelUIController dialogue)
    {
      dialogue.DisplayDisinteractableDialogue(null, _dialogueContent, null);
      yield return Fade(dialogue, 0f, 1f, _fadeInDuration);
      yield return WaitRealtime(_displayDuration);
      yield return Fade(dialogue, 1f, 0f, _fadeOutDuration);
      dialogue.HideDisinteractableDialogue();

      _dialogueRoutine = null;
      if (_activePresenter == this)
        _activePresenter = null;
    }

    private static IEnumerator Fade(
      DialoguePanelUIController dialogue,
      float from,
      float to,
      float duration)
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
        dialogue.SetDisinteractableDialogueOpacity(
          Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
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

    private void OnDisable()
    {
      if (_activePresenter != this)
        return;

      var dialogue = Registry.Get<DialoguePanelUIController>(
        RegistryType.UI,
        Registry.TypeKey<DialoguePanelUIController>());
      dialogue?.HideDisinteractableDialogue();
      StopCurrentDialogue();
    }
  }
}
