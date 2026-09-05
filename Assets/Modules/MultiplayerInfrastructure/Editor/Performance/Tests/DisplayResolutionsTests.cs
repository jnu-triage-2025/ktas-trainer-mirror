using System.Collections.Generic;
using MultiplayerInfrastructure.Performance;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Editor.Performance.Tests
{
  public class DisplayResolutionsTests
  {
    [Test]
    public void Common_IsSortedAndHasNoDuplicates()
    {
      var seen = new HashSet<DisplayResolution>();
      for (int i = 0; i < DisplayResolutions.Common.Count; i++)
      {
        var current = DisplayResolutions.Common[i];
        Assert.That(seen.Add(current), Is.True, $"duplicate {current}");
        if (i == 0)
          continue;
        var previous = DisplayResolutions.Common[i - 1];
        Assert.That(previous.Width < current.Width ||
                    (previous.Width == current.Width && previous.Height < current.Height),
          Is.True, $"{previous} should come before {current}");
      }
    }

    [Test]
    public void Common_ContainsFullHd()
    {
      Assert.That(DisplayResolutions.TryFind(DisplayResolutions.Common, 1920, 1080, out var found), Is.True);
      Assert.That(found.AspectRatio, Is.EqualTo("16:9"));
    }

    [Test]
    public void BuildOptions_AddsAnUnlistedCurrentDisplayInSortedPosition()
    {
      var options = DisplayResolutions.BuildOptions(3024, 1964);

      Assert.That(options.Count, Is.EqualTo(DisplayResolutions.Common.Count + 1));
      Assert.That(DisplayResolutions.TryFind(options, 3024, 1964, out _), Is.True);
      int index = options.FindIndex(o => o.Matches(3024, 1964));
      Assert.That(options[index - 1].Width, Is.LessThanOrEqualTo(3024));
      Assert.That(options[index + 1].Width, Is.GreaterThanOrEqualTo(3024));
    }

    [Test]
    public void BuildOptions_DoesNotDuplicateAListedCurrentDisplay()
    {
      var options = DisplayResolutions.BuildOptions(1920, 1080);

      Assert.That(options.Count, Is.EqualTo(DisplayResolutions.Common.Count));
    }

    [Test]
    public void BuildOptions_IgnoresAnUnknownCurrentDisplay()
    {
      Assert.That(DisplayResolutions.BuildOptions(0, 0).Count, Is.EqualTo(DisplayResolutions.Common.Count));
    }

    [Test]
    public void LabelFor_UsesCustomLabelWhenNoPresetMatches()
    {
      var options = DisplayResolutions.BuildOptions(0, 0);

      Assert.That(DisplayResolutions.LabelFor(options, 1920, 1080), Is.EqualTo("1920 × 1080 (16:9)"));
      Assert.That(DisplayResolutions.LabelFor(options, 1921, 1080), Is.EqualTo(DisplayResolutions.CustomLabel));
    }

    [Test]
    public void Label_MarksTheCurrentDisplay()
    {
      var options = DisplayResolutions.BuildOptions(2560, 1440);

      var label = DisplayResolutions.LabelFor(options, 2560, 1440, 2560, 1440);

      Assert.That(label, Does.EndWith(DisplayResolutions.CurrentDisplaySuffix));
      Assert.That(DisplayResolutions.TryParseLabel(options, label, 2560, 1440, out var parsed), Is.True);
      Assert.That(parsed.Matches(2560, 1440), Is.True);
    }

    [Test]
    public void Labels_RoundTripForEveryOption()
    {
      var options = DisplayResolutions.BuildOptions(3024, 1964);
      foreach (var option in options)
      {
        var label = DisplayResolutions.Label(option, 3024, 1964);
        Assert.That(DisplayResolutions.TryParseLabel(options, label, 3024, 1964, out var parsed), Is.True, label);
        Assert.That(parsed, Is.EqualTo(option));
      }
      Assert.That(DisplayResolutions.TryParseLabel(options, DisplayResolutions.CustomLabel, 3024, 1964, out _), Is.False);
    }

    [TestCase(1920, 1080, "16:9")]
    [TestCase(1920, 1200, "16:10")]
    [TestCase(3440, 1440, "21:9")]
    [TestCase(1024, 768, "4:3")]
    [TestCase(3024, 1964, "756:491")]
    [TestCase(0, 1080, "")]
    public void DescribeAspectRatio_NamesCommonRatios(int width, int height, string expected)
    {
      Assert.That(DisplayResolutions.DescribeAspectRatio(width, height), Is.EqualTo(expected));
    }
  }
}
