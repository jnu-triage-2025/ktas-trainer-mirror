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
    private Label _outputRoutingNote;
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
      _outputRoutingNote = null;
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
        _outputRoutingNote = null;

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

      _outputRoutingNote = AddAudioNote(section, DescribeOutputRouting());
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

    /// <summary>
    /// 출력 경로 변경이 실제로 먹히는 상황인지 한 줄로 알려 줍니다.
    ///
    /// Unity에는 재생 장치를 고르는 API가 없어 운영체제 쪽 우회에 기대고 있습니다.
    /// 그 우회가 안 되는 자리에서는 값만 저장된다는 사실을 감추지 않습니다.
    /// </summary>
    private static string DescribeOutputRouting()
    {
      var service = AudioDevicePreferenceService.Instance;

      if (!AudioDevicePreferenceService.IsOutputRoutingSupported)
        return "이 플랫폼에서는 재생 경로를 바꿀 수 없어 고른 값을 저장만 합니다. "
             + "실제로 소리가 나가는 장치는 운영체제 설정을 따릅니다.";

      if (service != null && !service.IsOutputRoutingActive && !string.IsNullOrEmpty(service.OutputRoutingFailureReason))
        return $"출력 경로를 바꾸지 못해 시스템 설정을 따르고 있습니다. {service.OutputRoutingFailureReason}";

      return "장치를 바꾸면 지금 나오던 소리가 끝난 뒤에 새 장치로 옮겨 갑니다.";
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
      string target = AudioDeviceSelectionResolver.IsSystemDefault(deviceId)
        ? "시스템 설정"
        : $"'{AudioDeviceSelectionResolver.Find(deviceId, devices)?.DisplayName}'";

      // 출력은 저장 / 경로 변경 / 실제 전환 시점이 각각 다르다. 뭉뚱그리지 않는다.
      if (kind != AudioDeviceKind.Output)
      {
        SetStatusText($"{kindLabel} 장치를 {target}(으)로 저장했습니다.");
      }
      else if (AudioDevicePreferenceService.IsOutputRoutingSupported && !service.IsOutputRoutingActive)
      {
        SetStatusText($"출력 장치를 {target}(으)로 저장했지만 재생 경로는 바꾸지 못했습니다. "
                      + service.OutputRoutingFailureReason);
      }
      else if (service.IsOutputRestartPending)
      {
        SetStatusText($"출력 장치를 {target}(으)로 저장했습니다. 지금 나오는 소리가 끝나면 옮겨 갑니다.");
      }
      else
      {
        SetStatusText($"출력 장치를 {target}(으)로 저장했습니다.");
      }

      // 안내 문구가 경로 변경 결과에 따라 달라진다. 다만 지금은 드롭다운의 변경 콜백 안이라
      // 폼을 통째로 다시 지으면 이벤트가 흐르는 도중에 트리를 헐게 된다. 라벨만 갈아 끼운다.
      if (kind == AudioDeviceKind.Output && _outputRoutingNote != null)
        _outputRoutingNote.text = DescribeOutputRouting();
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

    private static Label AddAudioNote(VisualElement section, string text)
    {
      var note = new Label(text);
      note.AddToClassList("settings__field-note");
      note.pickingMode = PickingMode.Ignore;
      section.Add(note);
      return note;
    }
  }
}
