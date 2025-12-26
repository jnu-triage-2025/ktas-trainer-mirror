namespace MultiplayerInfrastructure.Dialogue
{
  /// <summary>
  /// 대화 시나리오의 메타데이터입니다.
  /// </summary>
  public class DialogueMetadata
  {
    public string Author { get; }
    public string Version { get; }
    public string Description { get; }

    public DialogueMetadata(
        string author = "",
        string version = "1.0.0",
        string description = "")
    {
      Author = author;
      Version = version;
      Description = description;
    }
  }
}
