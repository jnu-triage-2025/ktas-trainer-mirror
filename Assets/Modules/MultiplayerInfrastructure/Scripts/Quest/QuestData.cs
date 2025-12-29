using System;

namespace MultiplayerInfrastructure.Quest
{
  [Serializable]
  public class QuestData
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string QuestContent { get; set; }
    public bool IsTracked { get; set; }

    public QuestData()
    {
    }

    public QuestData(string id, string title, string description, string questContent, bool isTracked = false)
    {
      Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
      Title = title ?? string.Empty;
      Description = description ?? string.Empty;
      QuestContent = questContent ?? string.Empty;
      IsTracked = isTracked;
    }

    public QuestData Clone()
    {
      return new QuestData(Id, Title, Description, QuestContent, IsTracked);
    }
  }
}
