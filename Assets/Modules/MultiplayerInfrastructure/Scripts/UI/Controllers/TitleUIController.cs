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
    private string _currentTitle = string.Empty;
    private string _currentActionbar = string.Empty;
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
      StopAllRoutines();
      _titleActive = false;
      _actionbarActive = false;
      _currentTitle = string.Empty;
      _currentActionbar = string.Empty;
      _pendingSubtitle = string.Empty;
      EnsureElement();
      if (_titleElement != null)
        _titleElement.ClearAll();
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
      _currentTitle = title ?? string.Empty;
      _titleElement.TitleText = _currentTitle;
      _titleElement.SubtitleText = _pendingSubtitle;

      _titleActive = true;
      UpdateRootVisibility();
      _titleRoutine = StartCoroutine(RunFadeRoutine(
        isTitle: true,
        fadeInSeconds: _fadeInSeconds,
        staySeconds: _staySeconds,
        fadeOutSeconds: _fadeOutSeconds,
        onCompleted: () =>
        {
          _titleElement.TitleText = string.Empty;
          _titleElement.SubtitleText = string.Empty;
          _pendingSubtitle = string.Empty;
          _currentTitle = string.Empty;
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
        _pendingSubtitle = subtitle ?? string.Empty;
        _titleElement.SubtitleText = _pendingSubtitle;
        return;
      }

      _pendingSubtitle = subtitle ?? string.Empty;
    }

    public void ShowActionbar(string actionbar)
    {
      ShowActionbarInternal(actionbar, _fadeInSeconds, _staySeconds, _fadeOutSeconds);
    }

    /// <summary>
    /// 지정한 틱(20틱 = 1초) 타이밍으로 액션바를 한 번 표시한다.
    /// 이 표시에만 타이밍이 적용되며 <see cref="SetTimes"/>의 공유 기본값은 유지된다.
    /// </summary>
    public void ShowActionbar(string actionbar, int fadeInTicks, int stayTicks, int fadeOutTicks)
    {
      ShowActionbarInternal(
        actionbar,
        Mathf.Max(0f, fadeInTicks) / 20f,
        Mathf.Max(0f, stayTicks) / 20f,
        Mathf.Max(0f, fadeOutTicks) / 20f);
    }

    private void ShowActionbarInternal(
      string actionbar,
      float fadeInSeconds,
      float staySeconds,
      float fadeOutSeconds)
    {
      if (!EnsureElement())
        return;

      StopActionbarRoutine();
      _currentActionbar = actionbar ?? string.Empty;
      _titleElement.ActionbarText = _currentActionbar;

      _actionbarActive = true;
      UpdateRootVisibility();
      _actionbarRoutine = StartCoroutine(RunFadeRoutine(
        isTitle: false,
        fadeInSeconds: fadeInSeconds,
        staySeconds: staySeconds,
        fadeOutSeconds: fadeOutSeconds,
        onCompleted: () =>
        {
          _titleElement.ActionbarText = string.Empty;
          _currentActionbar = string.Empty;
          _actionbarActive = false;
          UpdateRootVisibility();
        }));
    }

    public void ClearTitle()
    {
      StopTitleRoutine();
      _titleActive = false;
      _currentTitle = string.Empty;
      _pendingSubtitle = string.Empty;
      EnsureElement();
      if (_titleElement != null)
      {
        _titleElement.TitleText = string.Empty;
        _titleElement.SubtitleText = string.Empty;
      }

      UpdateRootVisibility();
    }

    public void ClearActionbar()
    {
      StopActionbarRoutine();
      _actionbarActive = false;
      _currentActionbar = string.Empty;
      EnsureElement();
      if (_titleElement != null)
        _titleElement.ActionbarText = string.Empty;

      UpdateRootVisibility();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
      if (root == null)
        return;

      EnsureStyleSheet(root);
      _titleElement = root.Q<TitleUIElement>("title-ui-root");

      if (_titleElement == null)
      {
        _titleElement = new TitleUIElement();
        EnsureStyleSheet(_titleElement);
        root.Add(_titleElement);
      }

      _titleElement.TitleText = _currentTitle;
      _titleElement.SubtitleText = _pendingSubtitle;
      _titleElement.ActionbarText = _currentActionbar;
      _titleElement.CenterOpacity = _titleActive ? 1f : 0f;
      _titleElement.ActionbarOpacity = _actionbarActive ? 1f : 0f;
      _titleElement.SetVisible(_titleActive || _actionbarActive);
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
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var currentRoot = _uiDocument != null ? _uiDocument.rootVisualElement : null;
      if (currentRoot == null)
        return false;

      // UIDocument는 활성화 과정에서 rootVisualElement를 다시 만들 수 있다. 특히 이
      // 컨트롤러가 network-spawned 백엔드 프리팹의 자식일 때 Awake에서 잡은 요소는
      // detached tree가 되어 style/text를 바꿔도 화면에 렌더링되지 않는다.
      // 현재 live document root의 요소인지 매 표시 시점에 확인하고 다시 바인딩한다.
      var currentElement = currentRoot.Q<TitleUIElement>("title-ui-root");
      if (_titleElement != null &&
          _titleElement == currentElement)
        return true;

      BindElement();
      return _titleElement != null &&
             _titleElement == currentRoot.Q<TitleUIElement>("title-ui-root");
    }

    private void ApplyDefaultTimes()
    {
      SetTimes(_defaultFadeInTicks, _defaultStayTicks, _defaultFadeOutTicks);
    }

    private IEnumerator RunFadeRoutine(
      bool isTitle,
      float fadeInSeconds,
      float staySeconds,
      float fadeOutSeconds,
      System.Action onCompleted)
    {
      if (_titleElement == null)
        yield break;

      float fadeIn = fadeInSeconds;
      float stay = staySeconds;
      float fadeOut = fadeOutSeconds;

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
