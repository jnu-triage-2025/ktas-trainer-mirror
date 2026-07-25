using System.Collections;
using MultiplayerInfrastructure.Definitions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class TitleUIController : UIControllerABC
  {
    [Header("References")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private StyleSheet _titleStyleSheet;

    [Header("Timing (ticks)")]
    [SerializeField] private int _defaultFadeInTicks = 10;
    [SerializeField] private int _defaultStayTicks = 70;
    [SerializeField] private int _defaultFadeOutTicks = 20;

    private TitleUIElement _titleElement;
    private Coroutine _titleRoutine;
    private Coroutine _actionbarRoutine;
    private bool _titleActive;
    private bool _actionbarActive;
    private string _pendingSubtitle = string.Empty;

    private float _fadeInSeconds;
    private float _staySeconds;
    private float _fadeOutSeconds;

    protected override void Awake()
    {
      base.Awake();

      if (_uiDocument.IsUnityNull())
        _uiDocument = GetComponent<UIDocument>();

      if (_uiDocument != null)
        _uiDocument.sortingOrder = DefaultsUIDocument.TitleUISortOrder;

      ApplyDefaultTimes();
      BindElement();
      HideAll();
    }

    public void SetTimes(int fadeInTicks, int stayTicks, int fadeOutTicks)
    {
      _fadeInSeconds = Mathf.Max(0f, fadeInTicks) / 20f;
      _staySeconds = Mathf.Max(0f, stayTicks) / 20f;
      _fadeOutSeconds = Mathf.Max(0f, fadeOutTicks) / 20f;
    }

    public void ResetTimesAndSubtitle()
    {
      ApplyDefaultTimes();
      _pendingSubtitle = string.Empty;
      if (_titleElement != null)
        _titleElement.SubtitleText = string.Empty;
    }

    public void ClearAll()
    {
      Debug.Log($"[TitleUI][DBG] ClearAll called stack={System.Environment.StackTrace}", this);
      StopAllRoutines();
      if (_titleElement != null)
        _titleElement.ClearAll();

      _titleActive = false;
      _actionbarActive = false;
      UpdateRootVisibility();
    }

    public void ShowTitle(string title, string subtitle = null)
    {
      if (!EnsureElement())
        return;

      StopTitleRoutine();

      if (subtitle == null)
        subtitle = _pendingSubtitle;

      _pendingSubtitle = subtitle ?? string.Empty;
      _titleElement.TitleText = title ?? string.Empty;
      _titleElement.SubtitleText = _pendingSubtitle;

      _titleActive = true;
      UpdateRootVisibility();
      _titleRoutine = StartCoroutine(RunFadeRoutine(
        isTitle: true,
        onCompleted: () =>
        {
          _titleElement.TitleText = string.Empty;
          _titleElement.SubtitleText = string.Empty;
          _pendingSubtitle = string.Empty;
          _titleActive = false;
          UpdateRootVisibility();
        }));
    }

    public void ShowSubtitle(string subtitle)
    {
      if (!EnsureElement())
        return;

      if (_titleActive || !string.IsNullOrWhiteSpace(_titleElement.TitleText))
      {
        _titleElement.SubtitleText = subtitle ?? string.Empty;
        return;
      }

      _pendingSubtitle = subtitle ?? string.Empty;
    }

    public void ShowActionbar(string actionbar)
    {
      if (!EnsureElement())
        return;

      Debug.Log($"[TitleUI][DBG] ShowActionbar (non-persistent) text=\"{actionbar}\"", this);
      StopActionbarRoutine();
      _titleElement.ActionbarText = actionbar ?? string.Empty;

      _actionbarActive = true;
      UpdateRootVisibility();
      _actionbarRoutine = StartCoroutine(RunFadeRoutine(
        isTitle: false,
        onCompleted: () =>
        {
          _titleElement.ActionbarText = string.Empty;
          _actionbarActive = false;
          UpdateRootVisibility();
        }));
    }

    /// <summary>
    /// Displays an actionbar message until the caller explicitly clears it.
    /// Use this for stateful controls whose exit instruction must remain visible.
    /// </summary>
    public void ShowPersistentActionbar(string actionbar)
    {
      if (!EnsureElement())
      {
        Debug.Log($"[TitleUI][DBG] ShowPersistentActionbar FAILED EnsureElement text=\"{actionbar}\"", this);
        return;
      }

      StopActionbarRoutine();
      _titleElement.ActionbarText = actionbar ?? string.Empty;
      _titleElement.ActionbarOpacity = 1f;
      _actionbarActive = !string.IsNullOrWhiteSpace(actionbar);
      UpdateRootVisibility();
      Debug.Log($"[TitleUI][DBG] ShowPersistentActionbar text=\"{actionbar}\" active={_actionbarActive}", this);
    }

    public void ClearTitle()
    {
      StopTitleRoutine();
      if (_titleElement != null)
      {
        _titleElement.TitleText = string.Empty;
        _titleElement.SubtitleText = string.Empty;
        _pendingSubtitle = string.Empty;
      }

      _titleActive = false;
      UpdateRootVisibility();
    }

    public void ClearActionbar()
    {
      Debug.Log($"[TitleUI][DBG] ClearActionbar called stack={System.Environment.StackTrace}", this);
      StopActionbarRoutine();
      if (_titleElement != null)
        _titleElement.ActionbarText = string.Empty;

      _actionbarActive = false;
      UpdateRootVisibility();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        return;

      var root = _uiDocument.rootVisualElement;
      EnsureStyleSheet(root);
      _titleElement = root.Q<TitleUIElement>("title-ui-root");

      if (_titleElement == null)
      {
        _titleElement = new TitleUIElement();
        EnsureStyleSheet(_titleElement);
        root.Add(_titleElement);
      }
    }

    private void EnsureStyleSheet(VisualElement element)
    {
      if (element == null)
        return;

      if (_titleStyleSheet != null && !element.styleSheets.Contains(_titleStyleSheet))
        element.styleSheets.Add(_titleStyleSheet);
    }

    private bool EnsureElement()
    {
      if (_titleElement != null)
        return true;

      BindElement();
      return _titleElement != null;
    }

    private void ApplyDefaultTimes()
    {
      SetTimes(_defaultFadeInTicks, _defaultStayTicks, _defaultFadeOutTicks);
    }

    private IEnumerator RunFadeRoutine(bool isTitle, System.Action onCompleted)
    {
      if (_titleElement == null)
        yield break;

      float fadeIn = _fadeInSeconds;
      float stay = _staySeconds;
      float fadeOut = _fadeOutSeconds;

      SetOpacity(isTitle, 0f);

      if (fadeIn > 0f)
      {
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
          elapsed += Time.unscaledDeltaTime;
          SetOpacity(isTitle, Mathf.Clamp01(elapsed / fadeIn));
          yield return null;
        }
      }

      SetOpacity(isTitle, 1f);

      if (stay > 0f)
        yield return new WaitForSecondsRealtime(stay);

      if (fadeOut > 0f)
      {
        float elapsed = 0f;
        while (elapsed < fadeOut)
        {
          elapsed += Time.unscaledDeltaTime;
          SetOpacity(isTitle, Mathf.Clamp01(1f - (elapsed / fadeOut)));
          yield return null;
        }
      }

      SetOpacity(isTitle, 0f);
      onCompleted?.Invoke();
    }

    private void SetOpacity(bool isTitle, float opacity)
    {
      if (_titleElement == null)
        return;

      if (isTitle)
        _titleElement.CenterOpacity = opacity;
      else
        _titleElement.ActionbarOpacity = opacity;
    }

    private void UpdateRootVisibility()
    {
      if (_titleElement == null)
        return;

      _titleElement.SetVisible(_titleActive || _actionbarActive);
    }

    private void HideAll()
    {
      if (_titleElement == null)
        return;

      _titleElement.CenterOpacity = 0f;
      _titleElement.ActionbarOpacity = 0f;
      _titleElement.SetVisible(false);
    }

    private void StopAllRoutines()
    {
      StopTitleRoutine();
      StopActionbarRoutine();
    }

    private void StopTitleRoutine()
    {
      if (_titleRoutine == null)
        return;

      StopCoroutine(_titleRoutine);
      _titleRoutine = null;
    }

    private void StopActionbarRoutine()
    {
      if (_actionbarRoutine == null)
        return;

      StopCoroutine(_actionbarRoutine);
      _actionbarRoutine = null;
    }
  }
}
