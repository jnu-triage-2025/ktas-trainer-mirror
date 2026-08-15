namespace MultiplayerInfrastructure.Commons
{
  /// <summary>
  /// 한국어 조사(포스트포지션 파티클) 유틸리티.
  ///
  /// <para>
  /// Josa.js(https://github.com/e-/Josa.js)의 알고리즘을 참고하여 구현했습니다.
  /// 마지막 글자의 유니코드 코드 포인트에서 Hangul Syllable 기준점(0xAC00, '가')을 빼고
  /// 28로 나눈 나머지로 종성(받침) 유무를 판별합니다.
  /// </para>
  /// </summary>
  public static class Josa
  {
    private const char HangulSyllableBase = '가'; // '가'
    private const char HangulSyllableEnd = '힣';  // '힣'
    private const int JongseongCount = 28;

    /// <summary>
    /// 목적격 조사(을/를)를 판별합니다.
    ///
    /// <para>
    /// <list type="bullet">
    /// <item><description>마지막 글자가 한글이고 받침이 있으면 "을"을 반환합니다 (예: "김민수" → "을").</description></item>
    /// <item><description>마지막 글자가 한글이고 받침이 없으면 "를"을 반환합니다 (예: "김나리" → "를").</description></item>
    /// <item><description>마지막 글자가 한글이 아니거나 텍스트가 비어 있으면 "을(를)"을 반환합니다.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="text">조사를 붙일 대상 텍스트.</param>
    /// <returns>"을", "를", 또는 "을(를)".</returns>
    public static string ObjectParticle(string text)
    {
      if (string.IsNullOrEmpty(text))
        return "을(를)";

      char lastChar = text[text.Length - 1];

      // 한글 음절 범위(가 U+AC00 ~ 힣 U+D7A3) 밖이면 비한국어로 간주.
      if (lastChar < HangulSyllableBase || lastChar > HangulSyllableEnd)
        return "을(를)";

      // (코드포인트 - 0xAC00) % 28 > 0 이면 종성(받침) 있음 → "을", 아니면 "를".
      bool hasJongseong = (lastChar - HangulSyllableBase) % JongseongCount > 0;
      return hasJongseong ? "을" : "를";
    }
  }
}
