using System;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class ProblemShortAnswerElement : VisualElement
  {
    private readonly TextField _input;
    private readonly Button _submit;

    public event Action<string> OnSubmit;

    public string Value => _input?.value ?? string.Empty;

    public ProblemShortAnswerElement()
    {
      style.flexDirection = FlexDirection.Column;
      style.display = DisplayStyle.None;

      _input = new TextField
      {
        multiline = false,
        isDelayed = false,
        maxLength = 256
      };
      _input.style.marginBottom = 6;
      Add(_input);

      _submit = new Button(() => OnSubmit?.Invoke(Value))
      {
        text = "Submit"
      };
      Add(_submit);
    }

    public void Bind(bool visible)
    {
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      if (visible)
        _input.value = string.Empty;
    }

    public void FocusInput()
    {
      _input?.Focus();
    }
  }
}
