namespace MultiplayerInfrastructure.Dialogue
{
  /// <summary>
  /// 대화 진행을 관리하는 러너의 기본 인터페이스
  /// </summary>
  public abstract class DialogueRunner
  {
    /// <summary>
    /// 다음 노드로 진행
    /// </summary>
    public abstract void Advance();

    /// <summary>
    /// 선택지 선택
    /// </summary>
    public abstract void SelectOption(int index);

    /// <summary>
    /// 대화 시작
    /// </summary>
    public abstract void StartDialogue();

    /// <summary>
    /// 대화 종료
    /// </summary>
    public abstract void EndDialogue();
  }
}
