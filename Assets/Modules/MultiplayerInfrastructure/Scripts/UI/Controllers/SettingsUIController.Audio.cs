using System.Collections.Generic;
using MultiplayerInfrastructure.Audio;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// SettingsUIController의 "오디오 설정" 탭 구현입니다.
  ///
  /// 출력/입력 장치를 각각 드롭다운으로 고릅니다. 목록의 첫 항목은 언제나
  /// "시스템 설정(지금 쓰는 장치 이름)"이고, 그 뒤로 운영체제가 인식한 장치가 이어집니다.
  ///
  /// 장치 이름은 서로 겹칠 수 있어서(같은 모델을 두 개 꽂은 경우 등) 표시 문구가 아니라
  /// 드롭다운 인덱스로 장치를 짚습니다.
  /// </summary>
  public partial class SettingsUIController
  {
    private OverflowScrollView _audioScroll;
    private DropdownField _outputDeviceField;
    private DropdownField _inputDeviceField;
    private bool _audioFormInitializing;

    private readonly List<AudioDeviceDescriptor> _outputDevices = new();
    private readonly List<AudioDeviceDescriptor> _inputDevices = new();

    private VisualElement EnsureAudioTabContent()
    {
      if (_audioTabContent != null)
        return _audioTabContent;

      _audioScroll = new OverflowScrollView();
      _audioScroll.AddToClassList("settings__audio-tab-wrapper");
      _audioTabContent = _audioScroll;
      BuildAudioForm();
      return _audioTabContent;
    }

    private void DetachAudioTab()
    {
      if (_outputDeviceField != null)
        _outputDeviceField.UnregisterValueChangedCallback(HandleOutputDeviceChanged);
      if (_inputDeviceField != null)
        _inputDeviceField.UnregisterValueChangedCallback(HandleInputDeviceChanged);

      _audioScroll = null;
      _outputDeviceField = null;
      _inputDeviceField = null;
      _audioTabContent = null;
      _outputDevices.Clear();
      _inputDevices.Clear();
    }

    /// <summary>탭을 열 때마다 운영체제에 장치 목록을 다시 묻고 화면을 다시 그립니다.</summary>
    private void RefreshAudioTab()
    {
      if (_audioScroll == null)
        return;

      var service = AudioDevicePreferenceService.Instance;
      if (service != null)
        service.RefreshDevices();
      else
        AudioDeviceCatalog.Refresh();

      BuildAudioForm();
    }

    private void BuildAudioForm()
    {
      if (_audioScroll == null)
        return;

      _audioFormInitializing = true;
      try
      {
        if (_outputDeviceField != null)
          _outputDeviceField.UnregisterValueChangedCallback(HandleOutputDeviceChanged);
        if (_inputDeviceField != null)
          _inputDeviceField.UnregisterValueChangedCallback(HandleInputDeviceChanged);

        _audioScroll.Content.Clear();
        _outputDeviceField = null;
        _inputDeviceField = null;

        var settings = AudioDevicePreferenceService.Instance?.CurrentSettings ?? new AudioDeviceSettingsData();

        BuildOutputSection(settings);
        BuildInputSection(settings);
        BuildAudioActions();
      }
      finally
      {
        _audioFormInitializing = false;
      }
    }

    private void BuildOutputSection(AudioDeviceSettingsData settings)
    {
      var section = AddAudioSection(
        "출력 장치",
        "게임 소리를 내보낼 장치를 고릅니다. 목록은 지금 이 컴퓨터가 인식하고 있는 장치입니다.");

      _outputDevices.Clear();
      _outputDevices.AddRange(AudioDeviceCatalog.GetDevices(AudioDeviceKind.Output));

      _outputDeviceField = BuildDeviceField(
        section, "출력 장치", _outputDevices, settings.OutputDeviceId, HandleOutputDeviceChanged);

      if (!AudioDeviceCatalog.IsSupported(AudioDeviceKind.Output))
        AddAudioNote(section, "이 플랫폼에서는 출력 장치 목록을 읽을 수 없어 시스템 설정만 고를 수 있습니다.");
      else if (_outputDevices.Count == 0)
        AddAudioNote(section, "쓸 수 있는 출력 장치를 찾지 못했습니다. 장치를 연결한 뒤 새로 고침을 눌러 주세요.");

      // 없는 기능을 있는 것처럼 보이게 두지 않기 위한 안내.
      AddAudioNote(section,
        "Unity가 재생 장치를 직접 지정하는 길을 열어두지 않아, 고른 값은 저장해 두기만 하고 "
        + "실제 소리가 나가는 장치는 운영체제 설정을 따릅니다.");
    }

    private void BuildInputSection(AudioDeviceSettingsData settings)
    {
      var section = AddAudioSection(
        "입력 장치",
        "의식 확인처럼 목소리를 쓰는 기능에서 사용할 마이크를 고릅니다.");

      _inputDevices.Clear();
      _inputDevices.AddRange(AudioDeviceCatalog.GetDevices(AudioDeviceKind.Input));

      _inputDeviceField = BuildDeviceField(
        section, "입력 장치", _inputDevices, settings.InputDeviceId, HandleInputDeviceChanged);

      if (_inputDevices.Count == 0)
        AddAudioNote(section, "쓸 수 있는 마이크를 찾지 못했습니다. 마이크를 연결하고 권한을 허용한 뒤 새로 고침을 눌러 주세요.");
    }

    private DropdownField BuildDeviceField(
      VisualElement section,
      string label,
      List<AudioDeviceDescriptor> devices,
      string selectedId,
      EventCallback<ChangeEvent<string>> callback)
    {
      var choices = AudioDeviceSelectionResolver.BuildChoiceLabels(devices);
      var field = new DropdownField(choices, AudioDeviceSelectionResolver.IndexOfChoice(selectedId, devices));
      field.RegisterValueChangedCallback(callback);
      AddRow(section, label, field);
      return field;
    }

    private void BuildAudioActions()
    {
      var actions = new VisualElement();
      actions.AddToClassList("settings__actions");

      var reset = new Button(() =>
      {
        var service = AudioDevicePreferenceService.Instance;
        if (service == null)
        {
          SetStatusText("오류: 오디오 장치 서비스를 찾을 수 없습니다.");
          return;
        }

        service.ResetToSystemDefault();
        BuildAudioForm();
        SetStatusText("출력과 입력을 모두 시스템 설정으로 되돌렸습니다.");
      })
      { text = "시스템 설정으로" };
      reset.AddToClassList("settings__secondary-btn");

      var refresh = new Button(() =>
      {
        RefreshAudioTab();
        SetStatusText("장치 목록을 다시 읽었습니다.");
      })
      { text = "장치 목록 새로 고침" };
      refresh.AddToClassList("settings__primary-btn");

      actions.Add(reset);
      actions.Add(refresh);
      _audioScroll.Content.Add(actions);
    }

    private void HandleOutputDeviceChanged(ChangeEvent<string> _)
      => ApplyDeviceSelection(AudioDeviceKind.Output, _outputDeviceField, _outputDevices);

    private void HandleInputDeviceChanged(ChangeEvent<string> _)
      => ApplyDeviceSelection(AudioDeviceKind.Input, _inputDeviceField, _inputDevices);

    private void ApplyDeviceSelection(AudioDeviceKind kind, DropdownField field, List<AudioDeviceDescriptor> devices)
    {
      if (_audioFormInitializing || field == null)
        return;

      var service = AudioDevicePreferenceService.Instance;
      if (service == null)
      {
        SetStatusText("오류: 오디오 장치 서비스를 찾을 수 없습니다.");
        return;
      }

      string deviceId = AudioDeviceSelectionResolver.ChoiceIdAt(field.index, devices);
      service.SetDevice(kind, deviceId);

      string kindLabel = kind == AudioDeviceKind.Output ? "출력" : "입력";
      SetStatusText(AudioDeviceSelectionResolver.IsSystemDefault(deviceId)
        ? $"{kindLabel} 장치를 시스템 설정에 맡기고 저장했습니다."
        : $"{kindLabel} 장치를 '{AudioDeviceSelectionResolver.Find(deviceId, devices)?.DisplayName}'(으)로 저장했습니다.");
    }

    private VisualElement AddAudioSection(string title, string description)
    {
      var section = new VisualElement();
      section.AddToClassList("settings__section");
      if (_audioScroll.Content.childCount > 0)
        section.AddToClassList("settings__section--spaced");

      var titleLabel = new Label(title);
      titleLabel.AddToClassList("settings__section-title");
      section.Add(titleLabel);

      var descLabel = new Label(description);
      descLabel.AddToClassList("settings__section-desc");
      section.Add(descLabel);

      _audioScroll.Content.Add(section);
      return section;
    }

    private static void AddAudioNote(VisualElement section, string text)
    {
      var note = new Label(text);
      note.AddToClassList("settings__field-note");
      section.Add(note);
    }
  }
}
