using MultiplayerInfrastructure.Logging;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>SettingsUIController의 "일반" 탭 구현.</summary>
  public partial class SettingsUIController
  {
    private int _clearLogsGeneration;
    private bool _isClearingLogs;
    private Toggle _dialogueSkipField;

    private VisualElement EnsureGeneralTabContent()
    {
      if (_generalTabContent != null)
        return _generalTabContent;

      var content = new OverflowScrollView();
      content.AddToClassList("settings__general-tab-wrapper");

      BuildDialogueSection(content.Content);

      var logSection = new VisualElement();
      logSection.AddToClassList("settings__section");
      logSection.AddToClassList("settings__section--spaced");

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

    /// <summary>탭을 다시 열 때 저장된 설정 값을 화면에 되비춥니다.</summary>
    private void RefreshGeneralTab()
    {
      _dialogueSkipField?.SetValueWithoutNotify(DialogueSkipPreference.IsEnabled);
    }

    private void DetachGeneralTab()
    {
      if (_dialogueSkipField != null)
        _dialogueSkipField.UnregisterValueChangedCallback(HandleDialogueSkipChanged);

      _dialogueSkipField = null;
      _generalTabContent = null;
    }

    /// <summary>
    /// 대화 재생 연출을 진행 입력으로 건너뛸 수 있게 할지 고르는 항목입니다.
    /// 값은 <see cref="DialogueSkipPreference"/>가 저장하고 대화창이 입력 시점에 읽습니다.
    /// </summary>
    private void BuildDialogueSection(VisualElement parent)
    {
      var section = new VisualElement();
      section.AddToClassList("settings__section");

      var title = new Label("대화");
      title.AddToClassList("settings__section-title");
      section.Add(title);

      var description = new Label("대화 문장이 한 글자씩 표시되는 동안의 입력 동작을 설정합니다.");
      description.AddToClassList("settings__section-desc");
      section.Add(description);

      _dialogueSkipField = new Toggle { value = DialogueSkipPreference.IsEnabled };
      _dialogueSkipField.RegisterValueChangedCallback(HandleDialogueSkipChanged);
      AddRow(section, "대화 연출 건너뛰기 허용", _dialogueSkipField);

      AddAudioNote(section,
        "켜면 클릭이나 스페이스바로 재생 중인 문장을 즉시 모두 표시합니다. "
        + "끄면 문장이 끝까지 표시된 뒤에만 다음으로 진행할 수 있습니다.");

      parent.Add(section);
    }

    private void HandleDialogueSkipChanged(ChangeEvent<bool> change)
    {
      DialogueSkipPreference.SetEnabled(change.newValue);
      SetStatusText(change.newValue
        ? "대화 연출 건너뛰기를 켰습니다. 재생 중 진행 입력으로 문장을 바로 표시합니다."
        : "대화 연출 건너뛰기를 껐습니다. 문장이 끝까지 표시된 뒤에 진행할 수 있습니다.");
    }

    private void HandleOpenLogFolderClicked()
    {
      GameLogService.OpenLogFolder();
      SetStatusText($"로그 폴더: {GameLogService.LogRootPath}");
    }

    private async void HandleClearAllLogsClicked()
    {
      if (_isClearingLogs)
        return;

      _isClearingLogs = true;
      int generation = ++_clearLogsGeneration;
      var button = _generalTabContent?.Q<Button>(className: "settings__danger-btn");
      if (button != null)
        button.SetEnabled(false);

      try
      {
        SetStatusText("세션 로그를 제거하는 중입니다...");
        var result = await GameLogService.ClearAllSessionLogsAsync();
        if (this == null || generation != _clearLogsGeneration)
          return;

        SetStatusText(result.Message);
        if (!result.Success)
          UnityEngine.Debug.LogWarning($"[SettingsUI] {result.Message}");
      }
      catch (System.Exception exception)
      {
        if (this != null && generation == _clearLogsGeneration)
          UnityEngine.Debug.LogException(exception, this);
      }
      finally
      {
        if (this != null && generation == _clearLogsGeneration)
        {
          _isClearingLogs = false;
          if (button != null && button.panel != null)
            button.SetEnabled(true);
        }
      }
    }
  }
}
