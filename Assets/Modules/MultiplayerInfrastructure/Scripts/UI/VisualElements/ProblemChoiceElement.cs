using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class ProblemChoiceElement : VisualElement
  {
    private readonly List<Button> _buttons = new();

    public event Action<int> OnChoiceSelected;

    public ProblemChoiceElement()
    {
      style.flexDirection = FlexDirection.Column;
      style.display = DisplayStyle.None;
    }

    public void Bind(IReadOnlyList<string> options)
    {
      Clear();
      _buttons.Clear();

      if (options == null || options.Count == 0)
      {
        style.display = DisplayStyle.None;
        return;
      }

      style.display = DisplayStyle.Flex;
      for (int i = 0; i < options.Count; i++)
      {
        int index = i;
        var button = new Button(() => OnChoiceSelected?.Invoke(index))
        {
          text = options[i] ?? string.Empty
        };
        button.style.marginBottom = 6;
        Add(button);
        _buttons.Add(button);
      }
    }
  }
}
