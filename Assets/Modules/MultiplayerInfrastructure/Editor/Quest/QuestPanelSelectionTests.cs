using System.Reflection;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Quest
{
  public sealed class QuestPanelSelectionTests
  {
    [Test]
    public void ReopeningPanelSelectsTrackedQuestBeforePreviousSelection()
    {
      var panel = new QuestPanelElement();
      panel.SetQuests(new[]
      {
        new QuestData { Id = "untracked", Title = "Untracked", IsTracked = false },
        new QuestData { Id = "tracked", Title = "Tracked", IsTracked = true }
      });

      panel.SetOpen(true);
      SelectQuest(panel, "untracked");
      panel.SetOpen(false);
      panel.SetOpen(true);

      Assert.That(GetSelectedQuestId(panel), Is.EqualTo("tracked"));
    }

    private static void SelectQuest(QuestPanelElement panel, string questId)
    {
      typeof(QuestPanelElement)
        .GetMethod("SelectQuest", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.Invoke(panel, new object[] { questId });
    }

    private static string GetSelectedQuestId(QuestPanelElement panel)
    {
      return typeof(QuestPanelElement)
        .GetField("_selectedQuestId", BindingFlags.Instance | BindingFlags.NonPublic)
        ?.GetValue(panel) as string;
    }
  }
}
