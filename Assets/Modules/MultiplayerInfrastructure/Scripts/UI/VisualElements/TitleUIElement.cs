using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class TitleUIElement : VisualElement
  {
    private const string HiddenClass = "is-hidden";

    private readonly VisualElement _centerContainer;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _actionbarLabel;

    public string TitleText
    {
      get => _titleLabel.text;
      set => SetLabelText(_titleLabel, value);
    }

    public string SubtitleText
    {
      get => _subtitleLabel.text;
      set => SetLabelText(_subtitleLabel, value);
    }

    public string ActionbarText
    {
      get => _actionbarLabel.text;
      set => SetLabelText(_actionbarLabel, value);
    }

    public float CenterOpacity
    {
      get => _centerContainer.style.opacity.value;
      set => _centerContainer.style.opacity = Mathf.Clamp01(value);
    }

    public float ActionbarOpacity
    {
      get => _actionbarLabel.style.opacity.value;
      set => _actionbarLabel.style.opacity = Mathf.Clamp01(value);
    }

    public TitleUIElement()
    {
      pickingMode = PickingMode.Ignore;
      AddToClassList("title-ui");

      _centerContainer = new VisualElement
      {
        name = "title-ui-center",
        pickingMode = PickingMode.Ignore
      };
      _centerContainer.AddToClassList("title-ui__center");
      Add(_centerContainer);

      _titleLabel = new Label
      {
        name = "title-label",
        pickingMode = PickingMode.Ignore
      };
      _titleLabel.AddToClassList("title-ui__title");
      _centerContainer.Add(_titleLabel);

      _subtitleLabel = new Label
      {
        name = "subtitle-label",
        pickingMode = PickingMode.Ignore
      };
      _subtitleLabel.AddToClassList("title-ui__subtitle");
      _centerContainer.Add(_subtitleLabel);

      _actionbarLabel = new Label
      {
        name = "actionbar-label",
        pickingMode = PickingMode.Ignore
      };
      _actionbarLabel.AddToClassList("title-ui__actionbar");
      Add(_actionbarLabel);

      ClearAll();
      SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
      EnableInClassList("title-ui--hidden", !visible);
    }

    public void ClearAll()
    {
      TitleText = string.Empty;
      SubtitleText = string.Empty;
      ActionbarText = string.Empty;
    }

    public void SetTitle(string title, string subtitle = null)
    {
      TitleText = title;
      SubtitleText = subtitle;
      SetVisible(true);
    }

    public void SetSubtitle(string subtitle)
    {
      SubtitleText = subtitle;
    }

    public void SetActionbar(string actionbar)
    {
      ActionbarText = actionbar;
    }

    private static void SetLabelText(Label label, string text)
    {
      var safeText = text ?? string.Empty;
      label.text = safeText;
      label.EnableInClassList(HiddenClass, string.IsNullOrWhiteSpace(safeText));
    }
  }
}
