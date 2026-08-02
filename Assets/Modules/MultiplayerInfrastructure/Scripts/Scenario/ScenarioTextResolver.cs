using System;
using System.Linq;
using System.Text;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>시나리오 표시 문자열의 플레이어 지정자(@s, @t=[tag, fallback])를 해석한다.</summary>
  public static class ScenarioTextResolver
  {
    private const string TagSelectorPrefix = "@t=[";

    public static string Resolve(string text, int? selfClientId = null)
    {
      if (string.IsNullOrEmpty(text))
        return text;

      var output = new StringBuilder(text.Length);
      for (int i = 0; i < text.Length;)
      {
        if (StartsWithAt(text, i, TagSelectorPrefix))
        {
          int close = FindClosingBracket(text, i + TagSelectorPrefix.Length);
          if (close >= 0)
          {
            string body = text.Substring(i + TagSelectorPrefix.Length, close - i - TagSelectorPrefix.Length);
            output.Append(ResolveTagSelector(body, selfClientId));
            i = close + 1;
            continue;
          }
        }

        if (i + 2 <= text.Length && text[i] == '@' && text[i + 1] == 's')
        {
          output.Append(ResolveSelf(selfClientId) ?? "@s");
          i += 2;
          continue;
        }

        output.Append(text[i++]);
      }

      return output.ToString();
    }

    private static string ResolveTagSelector(string body, int? selfClientId)
    {
      int comma = FindTopLevelComma(body);
      if (comma < 0)
        return TagSelectorPrefix + body + "]";

      string tag = body.Substring(0, comma).Trim();
      string fallback = body.Substring(comma + 1).Trim();
      var match = UserDescriptorService.GetAll().Values
        .Where(value => value != null && PlayerTagService.HasTag(value.Identifier, tag))
        .OrderBy(value => value.Identifier, StringComparer.Ordinal)
        .FirstOrDefault();

      if (match != null && !string.IsNullOrWhiteSpace(match.DisplayName))
        return match.DisplayName;

      return Resolve(fallback, selfClientId);
    }

    private static string ResolveSelf(int? selfClientId)
    {
      if (!selfClientId.HasValue
          || !UserDescriptorService.TryGetByClientId(selfClientId.Value, out var descriptor)
          || descriptor == null)
        return null;

      return string.IsNullOrWhiteSpace(descriptor.DisplayName) ? descriptor.Identifier : descriptor.DisplayName;
    }

    private static int FindClosingBracket(string text, int contentStart)
    {
      int depth = 1;
      for (int i = contentStart; i < text.Length; i++)
      {
        if (StartsWithAt(text, i, TagSelectorPrefix))
        {
          depth++;
          i += TagSelectorPrefix.Length - 1;
        }
        else if (text[i] == ']')
        {
          depth--;
          if (depth == 0)
            return i;
        }
      }

      return -1;
    }

    private static int FindTopLevelComma(string text)
    {
      int depth = 0;
      for (int i = 0; i < text.Length; i++)
      {
        if (StartsWithAt(text, i, TagSelectorPrefix))
        {
          depth++;
          i += TagSelectorPrefix.Length - 1;
        }
        else if (text[i] == ']' && depth > 0)
        {
          depth--;
        }
        else if (text[i] == ',' && depth == 0)
        {
          return i;
        }
      }

      return -1;
    }

    private static bool StartsWithAt(string text, int index, string value)
      => index >= 0
         && value != null
         && index + value.Length <= text.Length
         && string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
  }
}
