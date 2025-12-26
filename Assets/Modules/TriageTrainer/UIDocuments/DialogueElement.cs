using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Text.RegularExpressions;

namespace TriageTrainer.UIDocuments
{
  [UxmlElement]
  public partial class DialogueElement : VisualElement
  {
    #region USS Classes

    private const string USS_BASE = "dialogue";
    private const string USS_CONTAINER = USS_BASE + "__container";
    private const string USS_HEADER = USS_BASE + "__header";
    private const string USS_SPEAKER_CONTAINER = USS_BASE + "__speaker-container";
    private const string USS_SPEAKER_NAME = USS_BASE + "__speaker-name";
    private const string USS_CONTENT_PANEL = USS_BASE + "__content-panel";
    private const string USS_TEXT_CONTAINER = USS_BASE + "__text-container";
    private const string USS_DIALOGUE_TEXT = USS_BASE + "__dialogue-text";
    private const string USS_CONTINUE_INDICATOR = USS_BASE + "__continue-indicator";
    private const string USS_VISIBLE = USS_BASE + "--visible";
    private const string USS_HIDDEN = USS_BASE + "--hidden";
    private const string USS_WAITING = USS_BASE + "--waiting";

    #endregion

    #region UI Elements

    private VisualElement _container;
    private VisualElement _header;
    private VisualElement _speakerContainer;
    private Label _speakerNameLabel;
    private VisualElement _contentPanel;
    private VisualElement _textContainer;
    private Label _dialogueTextLabel;
    private VisualElement _continueIndicator;

    #endregion

    #region UXML Attributes

    [UxmlAttribute]
    public string SpeakerName
    {
      get => _speakerNameLabel?.text ?? string.Empty;
      set
      {
        if (_speakerNameLabel != null)
          _speakerNameLabel.text = value;
      }
    }

    [UxmlAttribute]
    public string DialogueText
    {
      get => _currentRawText;
      set => SetDialogueText(value);
    }

    [UxmlAttribute]
    public bool EnableRichText { get; set; } = true;

    [UxmlAttribute]
    public bool HighlightKeywords { get; set; } = true;

    #endregion

    #region Internal State

    private string _currentRawText = string.Empty;
    private int _currentCharIndex = 0;
    private bool _isTyping = false;
    private bool _isWaitingForInput = false;

    #endregion

    #region Events

    public event Action OnDialogueClicked;
    public event Action OnTypingComplete;

    #endregion

    #region Properties

    public bool IsTyping => _isTyping;
    public bool IsWaitingForInput => _isWaitingForInput;
    public bool IsVisible => ClassListContains(USS_VISIBLE);

    #endregion

    #region Regex

    private static readonly Regex KeywordPattern = new Regex(@"「([^」]+)」");

    #endregion

    #region Constructor

    public DialogueElement()
    {
      BuildUI();
      RegisterCallbacks();
    }

    #endregion

    #region UI Build

    private void BuildUI()
    {
      AddToClassList(USS_BASE);
      AddToClassList(USS_HIDDEN);

      _container = new VisualElement { name = "dialogue-container" };
      _container.AddToClassList(USS_CONTAINER);
      Add(_container);

      _header = new VisualElement { name = "dialogue-header" };
      _header.AddToClassList(USS_HEADER);
      _container.Add(_header);

      _speakerContainer = new VisualElement { name = "speaker-container" };
      _speakerContainer.AddToClassList(USS_SPEAKER_CONTAINER);
      _header.Add(_speakerContainer);

      _speakerNameLabel = new Label { name = "speaker-name" };
      _speakerNameLabel.AddToClassList(USS_SPEAKER_NAME);
      _speakerContainer.Add(_speakerNameLabel);

      _contentPanel = new VisualElement { name = "content-panel" };
      _contentPanel.AddToClassList(USS_CONTENT_PANEL);
      _container.Add(_contentPanel);

      _textContainer = new VisualElement { name = "text-container" };
      _textContainer.AddToClassList(USS_TEXT_CONTAINER);
      _contentPanel.Add(_textContainer);

      _dialogueTextLabel = new Label { name = "dialogue-text" };
      _dialogueTextLabel.AddToClassList(USS_DIALOGUE_TEXT);
      _dialogueTextLabel.enableRichText = true;
      _textContainer.Add(_dialogueTextLabel);

      _continueIndicator = new VisualElement { name = "continue-indicator" };
      _continueIndicator.AddToClassList(USS_CONTINUE_INDICATOR);
      _continueIndicator.style.display = DisplayStyle.None;
      _contentPanel.Add(_continueIndicator);
    }

    private void RegisterCallbacks()
    {
      RegisterCallback<ClickEvent>(OnClick);
    }

    private void OnClick(ClickEvent evt)
    {
      OnDialogueClicked?.Invoke();
      evt.StopPropagation();
    }

    #endregion

    #region Public Methods

    public void Show()
    {
      RemoveFromClassList(USS_HIDDEN);
      AddToClassList(USS_VISIBLE);
    }

    public void Hide()
    {
      RemoveFromClassList(USS_VISIBLE);
      AddToClassList(USS_HIDDEN);
      RemoveFromClassList(USS_WAITING);
      ResetState();
    }

    public void SetDialogueText(string text)
    {
      _currentRawText = text ?? string.Empty;
      _dialogueTextLabel.text = ProcessText(_currentRawText);
      _currentCharIndex = _currentRawText.Length;
      _isTyping = false;
    }

    public void StartTyping(string text)
    {
      _currentRawText = text ?? string.Empty;
      _currentCharIndex = 0;
      _isTyping = true;
      _isWaitingForInput = false;
      _dialogueTextLabel.text = string.Empty;
      HideContinueIndicator();
    }

    public bool TypeNextCharacter()
    {
      if (!_isTyping || _currentCharIndex >= _currentRawText.Length)
      {
        CompleteTyping();
        return true;
      }

      _currentCharIndex++;
      string partialText = _currentRawText.Substring(0, _currentCharIndex);
      _dialogueTextLabel.text = ProcessText(partialText);

      if (_currentCharIndex >= _currentRawText.Length)
      {
        CompleteTyping();
        return true;
      }

      return false;
    }

    public char GetCurrentChar()
    {
      if (_currentCharIndex > 0 && _currentCharIndex <= _currentRawText.Length)
      {
        return _currentRawText[_currentCharIndex - 1];
      }
      return '\0';
    }

    public void CompleteTyping()
    {
      if (_isTyping || _currentCharIndex < _currentRawText.Length)
      {
        _dialogueTextLabel.text = ProcessText(_currentRawText);
        _currentCharIndex = _currentRawText.Length;
        _isTyping = false;
        OnTypingComplete?.Invoke();
      }
    }

    public void SetWaitingForInput(bool waiting)
    {
      _isWaitingForInput = waiting;

      if (waiting)
      {
        AddToClassList(USS_WAITING);
        ShowContinueIndicator();
      }
      else
      {
        RemoveFromClassList(USS_WAITING);
        HideContinueIndicator();
      }
    }

    public void Reset()
    {
      SpeakerName = string.Empty;
      SetDialogueText(string.Empty);
      ResetState();
    }

    #endregion

    #region Private Methods

    private void ResetState()
    {
      _isTyping = false;
      _isWaitingForInput = false;
      _currentCharIndex = 0;
      _currentRawText = string.Empty;
      HideContinueIndicator();
    }

    private void ShowContinueIndicator()
    {
      if (_continueIndicator != null)
        _continueIndicator.style.display = DisplayStyle.Flex;
    }

    private void HideContinueIndicator()
    {
      if (_continueIndicator != null)
        _continueIndicator.style.display = DisplayStyle.None;
    }

    private string ProcessText(string text)
    {
      if (string.IsNullOrEmpty(text))
        return string.Empty;

      if (HighlightKeywords && EnableRichText)
      {
        return ApplyKeywordHighlight(text);
      }

      return text;
    }

    private string ApplyKeywordHighlight(string text)
    {
      return KeywordPattern.Replace(text, "<color=#78C8FF><b>「$1」</b></color>");
    }

    #endregion
  }
}
