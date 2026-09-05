using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>널리 쓰이는 화면 해상도 한 항목입니다.</summary>
  public readonly struct DisplayResolution : IEquatable<DisplayResolution>
  {
    public readonly int Width;
    public readonly int Height;
    /// <summary>표시용 화면 비율 문자열입니다. (예: "16:9")</summary>
    public readonly string AspectRatio;

    public DisplayResolution(int width, int height, string aspectRatio)
    {
      Width = width;
      Height = height;
      AspectRatio = aspectRatio;
    }

    public bool Matches(int width, int height) => Width == width && Height == height;
    public bool Equals(DisplayResolution other) => Width == other.Width && Height == other.Height;
    public override bool Equals(object obj) => obj is DisplayResolution other && Equals(other);
    public override int GetHashCode() => (Width * 397) ^ Height;
    public override string ToString() => $"{Width}x{Height}";
  }

  /// <summary>
  /// 설정 UI의 해상도 목록입니다. 목록에 있는 값을 고르면 가로·세로 해상도 항목이 그 값으로 맞춰지고,
  /// 목록에 없는 조합은 <see cref="CustomLabel"/>로 표시합니다.
  /// </summary>
  public static class DisplayResolutions
  {
    /// <summary>가로·세로 항목이 목록의 어느 값과도 맞지 않을 때 드롭다운에 표시하는 문구입니다.</summary>
    public const string CustomLabel = "(사용자 지정)";

    /// <summary>현재 디스플레이의 해상도를 목록에 덧붙일 때 라벨 뒤에 붙이는 표시입니다.</summary>
    public const string CurrentDisplaySuffix = " (현재 디스플레이)";

    /// <summary>가로, 세로 순으로 정렬된 널리 쓰이는 해상도 목록입니다.</summary>
    public static readonly IReadOnlyList<DisplayResolution> Common = new[]
    {
      new DisplayResolution(1280, 720, "16:9"),
      new DisplayResolution(1280, 800, "16:10"),
      new DisplayResolution(1366, 768, "16:9"),
      new DisplayResolution(1440, 900, "16:10"),
      new DisplayResolution(1600, 900, "16:9"),
      new DisplayResolution(1680, 1050, "16:10"),
      new DisplayResolution(1920, 1080, "16:9"),
      new DisplayResolution(1920, 1200, "16:10"),
      new DisplayResolution(2560, 1080, "21:9"),
      new DisplayResolution(2560, 1440, "16:9"),
      new DisplayResolution(2560, 1600, "16:10"),
      new DisplayResolution(3440, 1440, "21:9"),
      new DisplayResolution(3840, 2160, "16:9"),
    };

    /// <summary>
    /// 설정 UI에 보여 줄 목록입니다. 현재 디스플레이 해상도가 <see cref="Common"/>에 없으면
    /// 정렬 순서를 지키며 끼워 넣어, 사용 중인 모니터의 원래 해상도를 항상 고를 수 있게 합니다.
    /// </summary>
    public static List<DisplayResolution> BuildOptions(int currentDisplayWidth, int currentDisplayHeight)
    {
      var options = new List<DisplayResolution>(Common);
      if (currentDisplayWidth <= 0 || currentDisplayHeight <= 0 || TryFind(options, currentDisplayWidth, currentDisplayHeight, out _))
        return options;

      options.Add(new DisplayResolution(currentDisplayWidth, currentDisplayHeight, DescribeAspectRatio(currentDisplayWidth, currentDisplayHeight)));
      options.Sort((a, b) => a.Width != b.Width ? a.Width.CompareTo(b.Width) : a.Height.CompareTo(b.Height));
      return options;
    }

    public static bool TryFind(IReadOnlyList<DisplayResolution> options, int width, int height, out DisplayResolution found)
    {
      foreach (var option in options)
      {
        if (option.Matches(width, height))
        {
          found = option;
          return true;
        }
      }
      found = default;
      return false;
    }

    /// <summary>드롭다운 라벨입니다. (예: "1920 × 1080 (16:9)")</summary>
    public static string Label(DisplayResolution resolution, int currentDisplayWidth = 0, int currentDisplayHeight = 0)
    {
      var label = string.IsNullOrEmpty(resolution.AspectRatio)
        ? $"{resolution.Width} × {resolution.Height}"
        : $"{resolution.Width} × {resolution.Height} ({resolution.AspectRatio})";
      if (resolution.Matches(currentDisplayWidth, currentDisplayHeight))
        label += CurrentDisplaySuffix;
      return label;
    }

    /// <summary>가로·세로 값에 맞는 라벨을 돌려주고, 목록에 없으면 <see cref="CustomLabel"/>을 돌려줍니다.</summary>
    public static string LabelFor(IReadOnlyList<DisplayResolution> options, int width, int height,
      int currentDisplayWidth = 0, int currentDisplayHeight = 0)
      => TryFind(options, width, height, out var found)
        ? Label(found, currentDisplayWidth, currentDisplayHeight)
        : CustomLabel;

    /// <summary>드롭다운 라벨에서 해상도를 찾습니다. <see cref="CustomLabel"/>이나 모르는 라벨이면 false입니다.</summary>
    public static bool TryParseLabel(IReadOnlyList<DisplayResolution> options, string label,
      int currentDisplayWidth, int currentDisplayHeight, out DisplayResolution found)
    {
      foreach (var option in options)
      {
        if (Label(option, currentDisplayWidth, currentDisplayHeight) == label)
        {
          found = option;
          return true;
        }
      }
      found = default;
      return false;
    }

    /// <summary>임의의 해상도를 흔히 부르는 화면 비율 이름으로 나타냅니다. 알 수 없는 비율은 약분한 정수비로 표시합니다.</summary>
    public static string DescribeAspectRatio(int width, int height)
    {
      if (width <= 0 || height <= 0)
        return string.Empty;

      float ratio = (float)width / height;
      if (Mathf.Abs(ratio - 16f / 9f) < 0.02f) return "16:9";
      if (Mathf.Abs(ratio - 16f / 10f) < 0.02f) return "16:10";
      if (Mathf.Abs(ratio - 21f / 9f) < 0.05f) return "21:9";
      if (Mathf.Abs(ratio - 4f / 3f) < 0.02f) return "4:3";
      if (Mathf.Abs(ratio - 32f / 9f) < 0.05f) return "32:9";

      int gcd = Gcd(width, height);
      return $"{width / gcd}:{height / gcd}";
    }

    private static int Gcd(int a, int b)
    {
      while (b != 0)
      {
        int t = a % b;
        a = b;
        b = t;
      }
      return a;
    }
  }
}
