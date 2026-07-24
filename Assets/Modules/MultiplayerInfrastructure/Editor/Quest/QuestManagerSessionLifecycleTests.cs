using MultiplayerInfrastructure.Quest;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace MultiplayerInfrastructure.Tests.Quest
{
  public sealed class QuestManagerSessionLifecycleTests
  {
    [Test]
    public void SessionEndRemovesDefaultQuestAndKeepsExplicitlyPersistentQuest()
    {
      var gameObject = new GameObject("quest-session-lifecycle-test");
      var manager = gameObject.AddComponent<QuestManager>();

      try
      {
        manager.AddOrUpdateQuest(new QuestData { Id = "reset", Title = "Reset" });
        manager.AddOrUpdateQuest(new QuestData
        {
          Id = "preserve",
          Title = "Preserve",
          PersistProgressOnSessionEnd = true
        });

        var reset = typeof(QuestManager).GetMethod(
          "ResetProgressForSessionEnd",
          BindingFlags.Instance | BindingFlags.Public);
        Assert.That(reset, Is.Not.Null);
        reset.Invoke(manager, null);

        Assert.That(manager.HasQuest("reset"), Is.False);
        Assert.That(manager.HasQuest("preserve"), Is.True);
      }
      finally
      {
        Object.DestroyImmediate(gameObject);
      }
    }
  }
}
