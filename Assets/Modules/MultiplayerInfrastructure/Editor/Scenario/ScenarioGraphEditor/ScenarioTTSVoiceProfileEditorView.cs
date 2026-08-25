using System;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  internal sealed class ScenarioTTSVoiceProfileEditorView : VisualElement
  {
    private readonly Func<ScenarioGraph> getGraph;
    private readonly Action onChanged;
    private readonly VisualElement list;

    public ScenarioTTSVoiceProfileEditorView(Func<ScenarioGraph> getGraph, Action onChanged)
    {
      this.getGraph = getGraph;
      this.onChanged = onChanged;
      style.flexGrow = 1f;

      var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
      header.Add(new Label("TTS Voice Profiles") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, flexGrow = 1f } });
      header.Add(new Button(AddProfile) { text = "Add Voice Profile" });
      Add(header);
      Add(new HelpBox("시나리오에서 사용할 음성 프로필입니다. 아래의 내장 프리셋 리터럴은 읽기 전용입니다.", HelpBoxMessageType.Info));
      list = new VisualElement();
      Add(list);
      Refresh();
    }

    public void Refresh()
    {
      list.Clear();
      var profiles = getGraph?.Invoke()?.TtsVoiceProfiles;
      if (profiles != null)
        for (var i = 0; i < profiles.Count; i++)
          if (profiles[i] != null)
            list.Add(BuildProfile(i, profiles[i]));

      var presets = new Foldout { text = "Built-in Presets (read-only)", value = true };
      foreach (var preset in TTSVoiceProfileDefinitions.All)
        presets.Add(BuildPreset(preset));
      list.Add(presets);
    }

    private VisualElement BuildProfile(int index, ScenarioTTSVoiceProfile value)
    {
      var foldout = new Foldout { text = $"Voice Profile {index + 1}", value = true };
      foldout.style.marginTop = 8;
      foldout.style.paddingLeft = 6;
      foldout.style.paddingRight = 6;
      foldout.style.paddingBottom = 6;
      foldout.Add(new EnumField("Preset") { value = value.Preset ?? TTSVoiceStyle.F1 });
      var preset = (EnumField)foldout.ElementAt(foldout.childCount - 1);
      preset.RegisterValueChangedCallback(evt => { value.Preset = (TTSVoiceStyle)evt.newValue; Changed(); });
      foldout.Add(Text("JSON Identifier", value.VoiceIdentifier, next => value.VoiceIdentifier = Normalize(next)));
      foldout.Add(Text("Voice Style Name", value.VoiceStyleName, next => value.VoiceStyleName = next));
      foldout.Add(Text("Language", value.Language, next => value.Language = next));
      foldout.Add(new FloatField("Speed") { value = value.Speed });
      var speed = (FloatField)foldout.ElementAt(foldout.childCount - 1);
      speed.RegisterValueChangedCallback(evt => { value.Speed = evt.newValue; Changed(); });
      foldout.Add(new IntegerField("Total Step") { value = value.TotalStep });
      var steps = (IntegerField)foldout.ElementAt(foldout.childCount - 1);
      steps.RegisterValueChangedCallback(evt => { value.TotalStep = evt.newValue; Changed(); });
      foldout.Add(new Button(() => RemoveProfile(value)) { text = "Remove Voice Profile" });
      return foldout;
    }

    private static VisualElement BuildPreset(TTSVoiceProfileDefinition value)
    {
      var foldout = new Foldout { text = value.Style.ToString(), value = false };
      foldout.Add(ReadOnly("Voice Identifier", value.VoiceIdentifier));
      foldout.Add(ReadOnly("Voice Style Name", value.VoiceStyleName));
      foldout.Add(ReadOnly("Language", value.Language));
      foldout.Add(ReadOnly("Speed", value.Speed.ToString("0.##")));
      foldout.Add(ReadOnly("Total Step", value.TotalStep.ToString()));
      return foldout;
    }

    private void AddProfile()
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;
      graph.TtsVoiceProfiles = (graph.TtsVoiceProfiles ?? Array.Empty<ScenarioTTSVoiceProfile>()).Concat(
        new[] { new ScenarioTTSVoiceProfile { Preset = TTSVoiceStyle.F1 } }).ToArray();
      Changed(true);
    }

    private void RemoveProfile(ScenarioTTSVoiceProfile value)
    {
      var graph = getGraph?.Invoke();
      if (graph == null)
        return;
      graph.TtsVoiceProfiles = graph.TtsVoiceProfiles.Where(each => !ReferenceEquals(each, value)).ToArray();
      Changed(true);
    }

    private TextField Text(string label, string value, Action<string> setter)
    {
      var field = new TextField(label) { value = value ?? string.Empty };
      field.RegisterValueChangedCallback(evt => { setter(evt.newValue); Changed(); });
      return field;
    }

    private static TextField ReadOnly(string label, string value)
    {
      var field = new TextField(label) { value = value ?? string.Empty };
      field.SetEnabled(false);
      return field;
    }

    private void Changed(bool refresh = false) { onChanged?.Invoke(); if (refresh) Refresh(); }
    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }
}
