using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class ProblemPromptElement : VisualElement
  {
    private readonly Label _promptLabel;
    private readonly VisualElement _figure;

    public ProblemPromptElement()
    {
      style.flexDirection = FlexDirection.Column;
      style.marginBottom = 8;

      _promptLabel = new Label();
      _promptLabel.style.whiteSpace = WhiteSpace.Normal;
      _promptLabel.style.fontSize = 16;
      _promptLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
      _promptLabel.style.marginBottom = 6;
      Add(_promptLabel);

      _figure = new VisualElement();
      _figure.style.height = 180;
      _figure.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
      _figure.style.display = DisplayStyle.None;
      Add(_figure);
    }

    public void Bind(string prompt, UnityEngine.Texture2D figureTexture)
    {
      _promptLabel.text = prompt ?? string.Empty;
      if (figureTexture == null)
      {
        _figure.style.display = DisplayStyle.None;
        _figure.style.backgroundImage = null;
        return;
      }

      _figure.style.display = DisplayStyle.Flex;
      _figure.style.backgroundImage = new StyleBackground(figureTexture);
    }
  }
}
