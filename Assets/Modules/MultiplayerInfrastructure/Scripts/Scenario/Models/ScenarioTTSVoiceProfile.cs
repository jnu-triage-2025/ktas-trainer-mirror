using System;
using System.Collections.Generic;
using TextToSpeechService;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>시나리오 JSON에서 사용하는 음성 스타일 사전 선택지.</summary>
  public enum TTSVoiceStyle
  {
    F1, F2, F3, F4, F5, M1, M2, M3, M4, M5
  }

  /// <summary>
  /// 배포된 모든 voice_styles 파일의 사전 준비된 정의입니다. enum은 이 테이블의 키이며,
  /// TTS 처리 코드는 스타일 이름을 추측하지 않고 여기의 완성된 리터럴을 사용해야 합니다.
  /// </summary>
  public static class TTSVoiceProfileDefinitions
  {
    private static readonly IReadOnlyDictionary<TTSVoiceStyle, TTSVoiceProfile> Definitions =
      new Dictionary<TTSVoiceStyle, TTSVoiceProfile>
      {
        [TTSVoiceStyle.F1] = new TTSVoiceProfile { VoiceIdentifier = "F1", VoiceStyleName = "F1", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.F2] = new TTSVoiceProfile { VoiceIdentifier = "F2", VoiceStyleName = "F2", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.F3] = new TTSVoiceProfile { VoiceIdentifier = "F3", VoiceStyleName = "F3", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.F4] = new TTSVoiceProfile { VoiceIdentifier = "F4", VoiceStyleName = "F4", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.F5] = new TTSVoiceProfile { VoiceIdentifier = "F5", VoiceStyleName = "F5", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.M1] = new TTSVoiceProfile { VoiceIdentifier = "M1", VoiceStyleName = "M1", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.M2] = new TTSVoiceProfile { VoiceIdentifier = "M2", VoiceStyleName = "M2", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.M3] = new TTSVoiceProfile { VoiceIdentifier = "M3", VoiceStyleName = "M3", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.M4] = new TTSVoiceProfile { VoiceIdentifier = "M4", VoiceStyleName = "M4", Language = "ko", Speed = 1.05f, TotalStep = 5 },
        [TTSVoiceStyle.M5] = new TTSVoiceProfile { VoiceIdentifier = "M5", VoiceStyleName = "M5", Language = "ko", Speed = 1.05f, TotalStep = 5 }
      };

    public static TTSVoiceProfile Get(TTSVoiceStyle style)
    {
      var value = Definitions[style];
      return new TTSVoiceProfile
      {
        VoiceIdentifier = value.VoiceIdentifier, VoiceStyleName = value.VoiceStyleName,
        Language = value.Language, Speed = value.Speed, TotalStep = value.TotalStep
      };
    }

    public static IReadOnlyList<TTSVoiceProfileDefinition> All
    {
      get
      {
        var result = new List<TTSVoiceProfileDefinition>();
        foreach (var style in Enum.GetValues(typeof(TTSVoiceStyle)))
        {
          var key = (TTSVoiceStyle)style;
          var profile = Definitions[key];
          result.Add(new TTSVoiceProfileDefinition(key, profile.VoiceIdentifier, profile.VoiceStyleName,
            profile.Language, profile.Speed, profile.TotalStep));
        }
        return result;
      }
    }
  }

  /// <summary>내장 TTS 음성 리터럴을 노출하는 변경 불가 정의 객체입니다.</summary>
  public sealed class TTSVoiceProfileDefinition
  {
    public TTSVoiceStyle Style { get; }
    public string VoiceIdentifier { get; }
    public string VoiceStyleName { get; }
    public string Language { get; }
    public float Speed { get; }
    public int TotalStep { get; }

    public TTSVoiceProfileDefinition(TTSVoiceStyle style, string voiceIdentifier, string voiceStyleName,
      string language, float speed, int totalStep)
    {
      Style = style; VoiceIdentifier = voiceIdentifier; VoiceStyleName = voiceStyleName;
      Language = language; Speed = speed; TotalStep = totalStep;
    }
  }

  /// <summary>
  /// 노드와 시나리오 정의 탭에서 공유하는 TTS 프로필입니다. Preset을 지정하면 내장된
  /// voice_styles 값이 사용되며, null이면 아래 JSON 필드로 사용자 프로필을 정의합니다.
  /// </summary>
  [Serializable]
  public sealed class ScenarioTTSVoiceProfile
  {
    public TTSVoiceStyle? Preset { get; set; }
    public string VoiceIdentifier { get; set; }
    public string VoiceStyleName { get; set; }
    public string Language { get; set; }
    public float Speed { get; set; }
    public int TotalStep { get; set; }

    public TTSVoiceProfile ToServiceProfile()
    {
      var profile = Preset.HasValue
        ? TTSVoiceProfileDefinitions.Get(Preset.Value)
        : new TTSVoiceProfile { VoiceStyleName = "F1", Language = "ko", Speed = 1.05f, TotalStep = 5 };
      return new TTSVoiceProfile
      {
        VoiceIdentifier = string.IsNullOrWhiteSpace(VoiceIdentifier) ? profile.VoiceIdentifier : VoiceIdentifier,
        VoiceStyleName = string.IsNullOrWhiteSpace(VoiceStyleName) ? profile.VoiceStyleName : VoiceStyleName,
        Language = string.IsNullOrWhiteSpace(Language) ? profile.Language : Language,
        Speed = Speed > 0f ? Speed : profile.Speed,
        TotalStep = TotalStep > 0 ? TotalStep : profile.TotalStep
      };
    }
  }
}
