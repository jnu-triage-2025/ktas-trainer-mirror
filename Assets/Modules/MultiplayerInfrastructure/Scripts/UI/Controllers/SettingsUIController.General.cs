using MultiplayerInfrastructure.Logging;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>SettingsUIController의 "일반" 탭 구현.</summary>
  public partial class SettingsUIController
  {
    private VisualElement EnsureGeneralTabContent()
    {
      if (_generalTabContent != null)
        return _generalTabContent;

      var content = new OverflowScrollView();
      content.AddToClassList("settings__general-tab-wrapper");

      var logSection = new VisualElement();
      logSection.AddToClassList("settings__section");

      var title = new Label("세션 플레이 로그");
      title.AddToClassList("settings__section-title");
      logSection.Add(title);

      var description = new Label("플레이 세션 로그는 이 기기의 로컬 저장소에 보관됩니다.");
      description.AddToClassList("settings__section-desc");
      logSection.Add(description);

      var actions = new VisualElement();
      actions.AddToClassList("settings__actions");
      actions.AddToClassList("settings__actions--start");

      var openFolderButton = new Button(HandleOpenLogFolderClicked) { text = "로그 폴더 열기" };
      openFolderButton.AddToClassList("settings__secondary-btn");
      actions.Add(openFolderButton);

      var clearLogsButton = new Button(HandleClearAllLogsClicked) { text = "전체 로그 제거" };
      clearLogsButton.AddToClassList("settings__danger-btn");
      actions.Add(clearLogsButton);

      logSection.Add(actions);
      content.Content.Add(logSection);

      _generalTabContent = content;
      return _generalTabContent;
    }

    private void DetachGeneralTab()
    {
      _generalTabContent = null;
    }

    private void HandleOpenLogFolderClicked()
    {
      GameLogService.OpenLogFolder();
      SetStatusText($"로그 폴더: {GameLogService.LogRootPath}");
    }

    private async void HandleClearAllLogsClicked()
    {
      var button = _generalTabContent?.Q<Button>(className: "settings__danger-btn");
      if (button != null)
        button.SetEnabled(false);

      SetStatusText("세션 로그를 제거하는 중입니다...");
      var result = await GameLogService.ClearAllSessionLogsAsync();
      SetStatusText(result.Message);

      if (button != null)
        button.SetEnabled(true);

      if (!result.Success)
        UnityEngine.Debug.LogWarning($"[SettingsUI] {result.Message}");
    }
  }
}
