using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextToSpeechService
{
  /// <summary>
  /// SpeechTranscript의 basetext를 Static/Dynamic 세그먼트 목록으로 파싱합니다.
  ///
  /// 예:
  ///   basetext = "{patient-a}를 처치실로 이동해야 합니다."
  ///   → [ Dynamic("patient-a"), Static("를 처치실로 이동해야 합니다.") ]
  ///
  ///   basetext = "{player-b}, {player-c}, {player-d}는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오."
  ///   → [ Dynamic("player-b"), Static(","), Dynamic("player-c"), Static(","), Dynamic("player-d"),
  ///       Static("는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오.") ]
  /// </summary>
  public static class TranscriptParser
  {
    private static readonly Regex VariablePattern =
      new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>
    /// SpeechTranscript 하나를 파싱하여 재생 순서에 맞는 세그먼트 목록을 반환합니다.
    /// </summary>
    public static List<SpeechSegment> Parse(SpeechTranscript transcript)
    {
      return ParseBaseText(transcript.BaseText);
    }

    /// <summary>
    /// basetext 문자열을 파싱하여 세그먼트 목록을 반환합니다.
    /// </summary>
    public static List<SpeechSegment> ParseBaseText(string baseText)
    {
      var segments = new List<SpeechSegment>();
      int cursor = 0;

      foreach (Match match in VariablePattern.Matches(baseText))
      {
        // 변수 앞의 정적 텍스트
        if (match.Index > cursor)
        {
          string staticText = baseText.Substring(cursor, match.Index - cursor).Trim();
          if (!string.IsNullOrWhiteSpace(staticText))
          {
            segments.Add(new SpeechSegment
            {
              Type = SegmentType.Static,
              Text = staticText,
            });
          }
        }

        // 동적 변수 세그먼트
        string variableKey = match.Groups[1].Value;
        segments.Add(new SpeechSegment
        {
          Type = SegmentType.Dynamic,
          Text = variableKey,
        });

        cursor = match.Index + match.Length;
      }

      // 마지막 정적 텍스트
      if (cursor < baseText.Length)
      {
        string remaining = baseText.Substring(cursor).Trim();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
          segments.Add(new SpeechSegment
          {
            Type = SegmentType.Static,
            Text = remaining,
          });
        }
      }

      return segments;
    }
  }
}
