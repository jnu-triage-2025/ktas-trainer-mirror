namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// 상호작용을 런타임에 활성/비활성 전환할 수 있는 Interactable 이 구현한다.
  /// 시나리오 그래프 노드(예: NpcInteractControl / ItemSubmissionConfig)가 이 인터페이스로
  /// 개별 Interactable 의 활성 상태를 제어한다.
  /// </summary>
  public interface IInteractToggleable
  {
    void SetEnabled(bool enabled);
  }
}
