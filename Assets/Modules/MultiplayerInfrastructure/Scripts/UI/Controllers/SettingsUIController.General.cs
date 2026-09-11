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

    private const long ResetPreferencesConfirmWindowMs = 6000;
    private const string ResetPreferencesButtonText = "모든 설정 초기화";
    private const string ResetPreferencesConfirmText = "다시 누르면 초기화합니다";
    private Button _resetPreferencesButton;
    private bool _resetPreferencesArmed;
    private IVisualElementScheduledItem _resetPreferencesDisarmSchedule;

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

      var clearLogsButton = new Button(HandleClearAllLogsClicked) { text = "전체 로그 제거", name = "clear-logs-button" };
      clearLogsButton.AddToClassList("settings__danger-btn");
      actions.Add(clearLogsButton);

      logSection.Add(actions);
      content.Content.Add(logSection);

      BuildResetSection(content.Content);

      _generalTabContent = content;
      return _generalTabContent;
    }

    /// <summary>탭을 다시 열 때 저장된 설정 값을 화면에 되비춥니다. 확인 대기 중이던 초기화는 취소합니다.</summary>
    private void RefreshGeneralTab()
    {
      _dialogueSkipField?.SetValueWithoutNotify(DialogueSkipPreference.IsEnabled);
      DisarmResetPreferences();
    }

    private void DetachGeneralTab()
    {
      if (_dialogueSkipField != null)
        _dialogueSkipField.UnregisterValueChangedCallback(HandleDialogueSkipChanged);

      DisarmResetPreferences();
      _resetPreferencesButton = null;
      _dialogueSkipField = null;
      _generalTabContent = null;
    }

    /// <summary>
    /// 이 기기에 저장된 사용자 설정을 모두 지우고 기본값으로 되돌리는 항목입니다.
    /// 잘못 누르는 일을 막기 위해 한 번 누르면 확인 상태로 바뀌고, 정해진 시간 안에 다시 눌러야 실행합니다.
    /// 실제 삭제와 적용은 <see cref="UserPreferenceReset"/>이 맡습니다.
    /// </summary>
    private void BuildResetSection(VisualElement parent)
    {
      var section = new VisualElement();
      section.AddToClassList("settings__section");
      section.AddToClassList("settings__section--spaced");

      var title = new Label("설정 초기화");
      title.AddToClassList("settings__section-title");
      section.Add(title);

      var description = new Label(
        "이 기기에 저장된 사용자 설정(대화, 키, 그래픽, 오디오, UI 배율, 표시 이름)을 모두 지우고 기본값으로 되돌립니다.");
      description.AddToClassList("settings__section-desc");
      section.Add(description);

      var actions = new VisualElement();
      actions.AddToClassList("settings__actions");
      actions.AddToClassList("settings__actions--start");

      _resetPreferencesButton = new Button(HandleResetPreferencesClicked)
      {
        text = ResetPreferencesButtonText,
        name = "reset-preferences-button",
      };
      _resetPreferencesButton.AddToClassList("settings__danger-btn");
      actions.Add(_resetPreferencesButton);
      section.Add(actions);

      AddAudioNote(section,
        "해상도와 창 모드도 기본값으로 바뀌므로 화면이 잠시 깜빡일 수 있습니다. 세션 로그 파일은 지우지 않습니다.");

      parent.Add(section);
    }

    private void HandleResetPreferencesClicked()
    {
      if (!_resetPreferencesArmed)
      {
        ArmResetPreferences();
        return;
      }

      DisarmResetPreferences();
      ExecuteResetPreferences();
    }

    private void ArmResetPreferences()
    {
      _resetPreferencesArmed = true;
      if (_resetPreferencesButton != null)
        _resetPreferencesButton.text = ResetPreferencesConfirmText;
      SetStatusText("모든 설정을 기본값으로 되돌리려면 같은 버튼을 다시 누르세요.");

      _resetPreferencesDisarmSchedule?.Pause();
      _resetPreferencesDisarmSchedule = _resetPreferencesButton?.schedule
        .Execute(HandleResetPreferencesConfirmTimeout)
        .StartingIn(ResetPreferencesConfirmWindowMs);
    }

    private void HandleResetPreferencesConfirmTimeout()
    {
      if (!_resetPreferencesArmed)
        return;

      DisarmResetPreferences();
      SetStatusText("설정 초기화를 취소했습니다.");
    }

    private void DisarmResetPreferences()
    {
      _resetPreferencesDisarmSchedule?.Pause();
      _resetPreferencesDisarmSchedule = null;

      if (!_resetPreferencesArmed)
        return;

      _resetPreferencesArmed = false;
      if (_resetPreferencesButton != null)
        _resetPreferencesButton.text = ResetPreferencesButtonText;
    }

    private void ExecuteResetPreferences()
    {
      CancelRebinding();

      bool succeeded = true;
      try
      {
        // 키 바인딩의 메모리 값은 이 창이 들고 있으므로 여기서 되돌리고, 저장값은 아래에서 함께 지운다.
        RestoreDefaultKeyBindings();
        UserPreferenceReset.ResetAll(_bindings);
      }
      catch (System.Exception exception)
      {
        succeeded = false;
        UnityEngine.Debug.LogException(exception, this);
      }

      // 버튼 클릭 이벤트가 흐르는 도중에 탭 트리를 헐면 안 되므로 다음 프레임에 다시 짓는다.
      _root?.schedule.Execute(RebuildTabsAfterReset).StartingIn(0);

      SetStatusText(succeeded
        ? "모든 설정을 기본값으로 되돌렸습니다."
        : "일부 설정을 되돌리지 못했습니다. 저장된 값은 지웠으므로 다음 실행부터는 기본값으로 시작합니다.");
    }

    /// <summary>초기화된 값을 되비추도록 모든 탭을 버리고 현재 탭을 다시 만듭니다.</summary>
    private void RebuildTabsAfterReset()
    {
      if (this == null)
        return;

      DetachKeyTab();
      DetachGraphicsTab();
      DetachGeneralTab();
      DetachAudioTab();
      _keyTabContent = null;
      _graphicsTabContent = null;
      _generalTabContent = null;
      _audioTabContent = null;

      ShowTab(_activeTab, force: true);
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
      var button = _generalTabContent?.Q<Button>("clear-logs-button");
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
